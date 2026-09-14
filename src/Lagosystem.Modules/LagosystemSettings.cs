namespace Lagosystem.Modules
{
    using Lagosystem.Modules.Helpers;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Provides global configuration settings for the Lagosystem library. 
    /// This class allows users to define internal behaviors, such as the maximum allocation limits for 
    /// <see langword="stackalloc"/> and read/write buffer sizes for <see cref="System.IO.Stream"/> operations.
    /// </summary>
    public static class LagosystemSettings
    {
        private static int _defaultSpanBufferSize = 1 * (int)DataUnit.Kilobyte;
        private static int _defaultStreamBufferSize = 80 * (int)DataUnit.Kilobyte;
        private static int _maxBase64StreamSize = 50 * (int)DataUnit.Megabyte;

        /// <summary>
        /// Gets or sets the maximum allowed size (in bytes) for a stream to be loaded into memory 
        /// for Base64 string conversion. The default is 50 MB.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the provided value is less than 1 byte or greater than 200 Megabytes.
        /// </exception>
        public static int MaxBase64StreamSize
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _maxBase64StreamSize;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                ThrowHelper.ThrowIfLessThan(value, 1);
                ThrowHelper.ThrowIfGreaterThan(value, 200 * (int)DataUnit.Megabyte);
                _maxBase64StreamSize = value;
            }
        }
        

        /// <summary>Gets or sets the default buffer size used for <see langword="stackalloc"/> operations within the library.</summary>
        /// <remarks>
        /// This value defines the threshold for stack allocations. If a required buffer exceeds this defined size, 
        /// the library methods will typically fall back to using heap allocation or array pools to prevent stack overflow exceptions.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the provided value is less than or equal to 127 bytes (<see cref="sbyte.MaxValue"/>), 
        /// or greater than 1 Kilobyte.
        /// </exception>
        public static int DefaultSpanBufferSize
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _defaultSpanBufferSize;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                ThrowHelper.ThrowIfLessOrEqualsThan(value, sbyte.MaxValue * (int)DataUnit.Byte);
                ThrowHelper.ThrowIfGreaterThan(value, 1 * (int)DataUnit.Kilobyte);
                _defaultSpanBufferSize = value;
            }
        }

        /// <summary>
        /// Gets or sets the default buffer size used for reading and writing operations across various 
        /// <see cref="System.IO.Stream"/> objects (e.g., MemoryStream, FileStream, NetworkStream).
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the provided value is less than 128 bytes, or greater than 1 Megabyte (1,048,576 bytes).
        /// </exception>
        public static int DefaultStreamBufferSize
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _defaultStreamBufferSize;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                ThrowHelper.ThrowIfLessThan(value, 128);
                ThrowHelper.ThrowIfGreaterThan(value, 1 * (int)DataUnit.Megabyte /*1mb = 1.048.576*/);
                _defaultStreamBufferSize = value;
            }
        }
    }
}