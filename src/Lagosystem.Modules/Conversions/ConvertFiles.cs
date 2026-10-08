

namespace Lagosystem.Modules.Conversions
{
    using System.Buffers;
    using Lagosystem.Modules.Extensions;
    using Lagosystem.Modules.Helpers;
    using Lagosystem.Modules.Validations;
    using System.Runtime.CompilerServices;
    using Lagosystem.Modules.Formatting;
    
    /// <summary>Provides high-performance data conversion operations.</summary>
    public static class Conversion
    {
        
        /// <summary>
        /// Converts an array of 8-bit unsigned integers to its equivalent string representation that is encoded with base-64 digits.
        /// </summary>
        /// <param name="data">An array of 8-bit unsigned integers to encode.</param>
        /// <returns>
        /// The string representation, in base 64, of the contents of <paramref name="data"/>, 
        /// or <see cref="string.Empty"/> if the array is null or empty.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToBase64(byte[]? data) => data is null || data.LongLength == 0 ? string.Empty : Convert.ToBase64String(data);

        /// <summary>
        /// Reads the contents of a stream and converts it to its equivalent string representation encoded with base-64 digits.
        /// Safely limits the read size according to <see cref="Settings.MaxBase64StreamSize"/> to prevent out-of-memory exceptions.
        /// </summary>
        /// <param name="stream">The input stream to read from.</param>
        /// <returns>
        /// The base-64 string representation of the stream's contents, 
        /// or <see cref="string.Empty"/> if the stream is null or contains no readable data.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the stream size exceeds the maximum allowed limit for Base64 conversion in memory.
        /// </exception>
        public static string ToBase64(Stream? stream)
        {
            try
            {
                if (stream is null) return string.Empty;

                long maxAllowedSize = Settings.MaxBase64StreamSize;
                if (stream.CanSeek)
                {
                    long remainingBytes = stream.Length - stream.Position;
                    if (remainingBytes <= 0) return string.Empty;
                
                    if (remainingBytes > maxAllowedSize) ThrowHelper.ThrowBase64StreamSizeLimitExceeded(remainingBytes);
                
                    byte[] buffer = new byte[remainingBytes];
                    int bytesRead = 0;
                    while (bytesRead < buffer.Length)
                    {
                        int currentRead = stream.Read(buffer, bytesRead, buffer.Length - bytesRead);
                        if (currentRead == 0) break;
                        bytesRead += currentRead;
                    }
                
                    return Convert.ToBase64String(buffer, 0, bytesRead);
                }
                using MemoryStream ms = new MemoryStream();
                byte[] poolBuffer = ArrayPool<byte>.Shared.Rent(Settings.DefaultStreamBufferSize);
            
                try
                {
                    int read;
                    long totalRead = 0;
                    
                    while ((read = stream.Read(poolBuffer, 0, poolBuffer.Length)) > 0)
                    {
                        totalRead += read;
                        if (totalRead > maxAllowedSize) ThrowHelper.ThrowBase64StreamLimitExceeded(totalRead);
                        ms.Write(poolBuffer, 0, read);
                    }

                    if (ms.Length == 0) return string.Empty;
                    
                    // GetBuffer() is used here instead of ToArray() to avoid creating a duplicated 
                    // byte array just for the conversion process. Highly memory-efficient.
                    return Convert.ToBase64String(ms.GetBuffer(), 0, (int)ms.Length);
                }
                finally { ArrayPool<byte>.Shared.Return(poolBuffer); }

            }
            catch (Exception exception) when (Diagnostics.ReportOperationalFailure(exception, nameof(ToBase64), nameof(Conversion)))
            {
                throw; // Preserve existing API behavior and the original stack.
            }
        }
        
        /// <summary>
        /// Asynchronously reads the contents of a stream and converts it to its equivalent string representation encoded with base-64 digits.
        /// Safely limits the read size according to <see cref="Settings.MaxBase64StreamSize"/> to prevent out-of-memory exceptions.
        /// </summary>
        /// <param name="stream">The input stream to read from.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous read operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the base-64 string representation of the stream's contents, 
        /// or <see cref="string.Empty"/> if the stream is null or contains no readable data.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the stream size exceeds the maximum allowed limit for Base64 conversion in memory.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// Thrown if the operation is canceled via the <paramref name="cancellationToken"/>.
        /// </exception>
        public static async Task<string> ToBase64Async(Stream? stream, CancellationToken cancellationToken = default)
        {
            try
            {
            if (stream is null) return string.Empty;
            long maxAllowedSize = Settings.MaxBase64StreamSize;
            if (stream.CanSeek)
            {
                long remainingBytes = stream.Length - stream.Position;
                if (remainingBytes <= 0) return string.Empty;
                if (remainingBytes > maxAllowedSize) ThrowHelper.ThrowBase64StreamSizeLimitExceeded(remainingBytes);
                byte[] buffer = new byte[remainingBytes];
                int bytesRead = 0;
                while (bytesRead < buffer.Length)
                {
                    int currentRead = await stream.ReadAsync(buffer, bytesRead, buffer.Length - bytesRead,
                        cancellationToken).ConfigureAwait(false);
                    if (currentRead == 0) break;
                    bytesRead += currentRead;
                }
                return Convert.ToBase64String(buffer, 0, bytesRead);
            }
            
            using MemoryStream ms = new();
            byte[] poolBuffer = ArrayPool<byte>.Shared.Rent(Settings.DefaultStreamBufferSize);
            try
            {
                int read;
                long totalRead = 0;
                while ((read = await stream.ReadAsync(poolBuffer, 0, poolBuffer.Length, cancellationToken).ConfigureAwait(false)) > 0)
                {
                    totalRead += read;
                    if (totalRead > maxAllowedSize) ThrowHelper.ThrowBase64StreamLimitExceeded(totalRead);
                    ms.Write(poolBuffer, 0, read);
                }
                if (ms.Length == 0) return string.Empty;
                return Convert.ToBase64String(ms.GetBuffer(), 0, (int)ms.Length);
            }
            finally { ArrayPool<byte>.Shared.Return(poolBuffer); }

            }
            catch (Exception exception) when (Diagnostics.ReportOperationalFailure(exception, nameof(ToBase64Async), nameof(Conversion)))
            {
                throw; // Preserve existing API behavior and the original stack.
            }
        }
        
        
        

        /// <summary>
        /// Converts the specified base-64 string into its equivalent 8-bit unsigned integer array.
        /// Safely limits the decoded size according to <see cref="Settings.MaxBase64StreamSize"/> to prevent memory overflow.
        /// </summary>
        /// <param name="base64">The base-64 string to convert.</param>
        /// <returns>An array of bytes representing the decoded data, or an empty array if the string is null or blank.</returns>
        /// <exception cref="FormatException">Thrown when the provided string is not a valid Base-64 format.</exception>
        /// <exception cref="ArgumentException">Thrown when the decoded payload exceeds the maximum allowed memory limit.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] FromBase64(string? base64)
        {
            if (base64.IsBlank()) return Array.Empty<byte>();
            if (!Validate.IsValidBase64(base64)) ThrowHelper.ThrowNowInvalidBase64();
            if ((((long)base64!.Length * 3) / 4) > Settings.MaxBase64StreamSize) 
                ThrowHelper.ThrowNowOverlimitBase64Memory(base64.Length);
            return Convert.FromBase64String(base64);
        }

        /// <summary>
        /// Converts the specified base-64 string into binary data and writes it synchronously to the provided destination stream.
        /// Safely limits the decoded size according to <see cref="Settings.MaxBase64StreamSize"/>.
        /// </summary>
        /// <param name="base64">The base-64 string to convert.</param>
        /// <param name="destination">The stream where the decoded bytes will be written (e.g., a FileStream or MemoryStream).</param>
        /// <exception cref="ArgumentNullException">Thrown when the destination stream is null.</exception>
        /// <exception cref="FormatException">Thrown when the provided string is not a valid Base-64 format.</exception>
        /// <exception cref="ArgumentException">Thrown when the decoded payload exceeds the maximum allowed memory limit.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FromBase64(string? base64, Stream destination)
        {
            try
            {
            if (destination is null) throw new ArgumentNullException(nameof(destination));
            if (base64.IsBlank()) return;
            if (!Validate.IsValidBase64(base64)) ThrowHelper.ThrowNowInvalidBase64();
            if ((((long)base64!.Length * 3) / 4) > Settings.MaxBase64StreamSize) 
                ThrowHelper.ThrowNowOverlimitBase64Memory(base64.Length);
            byte[] buffer = Convert.FromBase64String(base64);
            destination.Write(buffer, 0, buffer.Length);

            }
            catch (Exception exception) when (Diagnostics.ReportOperationalFailure(exception, nameof(FromBase64), nameof(Conversion)))
            {
                throw; // Preserve existing API behavior and the original stack.
            }
        }

        /// <summary>
        /// Converts the specified base-64 string into binary data and writes it asynchronously to the provided destination stream.
        /// Safely limits the decoded size according to <see cref="Settings.MaxBase64StreamSize"/>.
        /// </summary>
        /// <param name="base64">The base-64 string to convert.</param>
        /// <param name="destination">The stream where the decoded bytes will be written.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the asynchronous write operation.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the destination stream is null.</exception>
        /// <exception cref="FormatException">Thrown when the provided string is not a valid Base-64 format.</exception>
        /// <exception cref="ArgumentException">Thrown when the decoded payload exceeds the maximum allowed memory limit.</exception>
        public static async Task FromBase64Async(string? base64, Stream destination, CancellationToken cancellationToken = default)
        {
            try
            {
            if (destination is null) throw new ArgumentNullException(nameof(destination));
            if (base64.IsBlank()) return;
            if (!Validate.IsValidBase64(base64)) ThrowHelper.ThrowNowInvalidBase64();
            if ((((long)base64!.Length * 3) / 4) > Settings.MaxBase64StreamSize) 
                ThrowHelper.ThrowNowOverlimitBase64Memory(base64!.Length);
            byte[] buffer = Convert.FromBase64String(base64);
            await destination.WriteAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);

            }
            catch (Exception exception) when (Diagnostics.ReportOperationalFailure(exception, nameof(FromBase64Async), nameof(Conversion)))
            {
                throw; // Preserve existing API behavior and the original stack.
            }
        }

        
        
        
        /// <summary>Converts a digital storage value from one unit to another.</summary>
        /// <param name="value">The numeric value to convert.</param>
        /// <param name="fromUnit">The original unit of the value.</param>
        /// <param name="toUnit">The target unit to convert the value into.</param>
        /// <returns>A <see cref="double"/> representing the converted value in the target unit.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ConvertSize(double value, DataUnit fromUnit, DataUnit toUnit) => (value * (long)fromUnit) / (long)toUnit;
        
    }
}
