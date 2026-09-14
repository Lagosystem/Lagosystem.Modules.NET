

namespace Lagosystem.Modules.Helpers
{
    using System.Runtime.CompilerServices;
    using Lagosystem.Modules.Formatting;
    using Lagosystem.Modules.LanguagesResource;

    internal static partial class ThrowHelper
    {
        /// <summary>
        /// Helper method to estimate the decoded byte size and validate it against the configured system memory limit.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowNowOverlimitBase64Memory(int base64StringLength)
        {
            long estimatedBytes = (long)base64StringLength * 3 / 4;
            long maxAllowedSize = LagosystemSettings.MaxBase64StreamSize;
            //$"The estimated decoded size ({0} bytes / {1}) exceeds the maximum allowed limit for Base64 conversion ({2} bytes / {3})."
            string message = string.Format(
                ExceptionsResource.OverlimitBase64Memory,
                estimatedBytes,
                Format.FormatSize(estimatedBytes, DataUnit.Byte),
                maxAllowedSize,
                Format.FormatSize(maxAllowedSize, DataUnit.Byte)
            );
            throw new ArgumentException(message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowNowInvalidBase64()
        {
            throw new FormatException("The provided string is not a valid Base-64 format.");
        }
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowBase64StreamSizeLimitExceeded(long remainingBytes)
        {
            long maxAllowedSize = LagosystemSettings.MaxBase64StreamSize;
            // The stream size ({0} bytes / {1}) exceeds the maximum allowed limit for Base64 conversion ({2} bytes / {3}).
            // O tamanho do stream ({0} bytes / {1}) excedeu o limite máximo permitido para conversão de Base64 ({2} bytes / {3}).
            string message = string.Format(
                ExceptionsResource.Base64StreamSizeLimitExceeded,
                remainingBytes,
                Format.FormatSize(remainingBytes, DataUnit.Byte),
                maxAllowedSize,
                Format.FormatSize(maxAllowedSize, DataUnit.Byte)
            );
            throw new ArgumentException(message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowBase64StreamLimitExceeded(long accumulatedBytes)
        {
            long maxAllowedSize = LagosystemSettings.MaxBase64StreamSize;
            // The accumulated stream size ({0} bytes / {1}) exceeded the maximum allowed limit for Base64 conversion ({2} bytes / {3}).
            // O tamanho acumulado do stream ({0} bytes / {1}) excedeu o limite máximo permitido para conversão Base64 ({2} bytes / {3}).
            string message = string.Format(
                ExceptionsResource.Base64StreamLimitExceeded,
                accumulatedBytes,                                      // {0} - Ex: 52428801
                Format.FormatSize(accumulatedBytes, DataUnit.Byte),    // {1} - Ex: "50,01 Megabyte"
                maxAllowedSize,                                        // {2} - Ex: 52428800
                Format.FormatSize(maxAllowedSize, DataUnit.Byte)       // {3} - Ex: "50 Megabyte"
            );
    
            throw new ArgumentException(message);
        }
        
    }
}