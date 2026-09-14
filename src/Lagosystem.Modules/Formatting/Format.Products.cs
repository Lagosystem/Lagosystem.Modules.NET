

namespace Lagosystem.Modules.Formatting
{
    using System.Runtime.CompilerServices;
    using Modules.Helpers;
    using Extensions;

    public static partial class Format
    {
        #region RawString

        /// <summary>Formats an NCM (Nomenclatura Comum do Mercosul) code string.</summary>
        /// <param name="ncm">The raw string containing the NCM code to format.</param>
        /// <returns>The formatted NCM code, or <see cref="string.Empty"/> if invalid.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormatNcmCode(string? ncm) => FormatNcmCode((ncm ?? string.Empty).AsSpan());

        /// <summary>Formats a CEST (Código Especificador da Substituição Tributária) code string.</summary>
        /// <param name="cest">The raw string containing the CEST code to format.</param>
        /// <returns>The formatted CEST code, or <see cref="string.Empty"/> if invalid.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormatCestCode(string? cest) => FormatCestCode((cest ?? string.Empty).AsSpan());

        /// <summary>Formats an EAN (European Article Number) barcode string.</summary>
        /// <param name="ean">The raw string containing the EAN code to format.</param>
        /// <param name="leftZero">Determines whether to preserve or pad leading zeros.</param>
        /// <returns>The formatted EAN code, or <see cref="string.Empty"/> if invalid.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormatEanCode(string? ean, bool leftZero = false) => FormatEanCode((ean ?? string.Empty).AsSpan(), leftZero);

        #endregion

        #region Memory

        /// <summary>Formats an NCM (Nomenclatura Comum do Mercosul) code using a read-only character span.</summary>
        /// <remarks>
        /// Extracts numbers, pads with leading zeros up to 8 characters, and invalidates zero-only strings.
        /// Uses <see cref="string.Create"/> for minimal memory allocation.
        /// </remarks>
        /// <param name="ncm">The <see cref="ReadOnlySpan{T}"/> containing the NCM code.</param>
        /// <returns>The formatted NCM code, or <see cref="string.Empty"/> if invalid.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormatNcmCode(ReadOnlySpan<char> ncm)
        {
            if (ncm.IsEmpty) return string.Empty;
            
            Span<char> buffer = stackalloc char[8];
            int len = FormatAndValidationHelper.ExtractOnlyNumbers(ncm, buffer);
            
            if (len is 0 or > 8) return string.Empty;
            
            FormatAndValidationHelper.FillStringZeroLeft(buffer, ref len, 8);
            
            // If it is composed entirely of zeros
            if (FormatAndValidationHelper.IsRepeatedString(buffer) && buffer[0] == '0') 
                return string.Empty; 
                
            return string.Create(8, buffer.ToArray(), static (span, state) => state.AsSpan(0, 8).CopyTo(span[..8]));
        }

        /// <summary>Formats a CEST (Código Especificador da Substituição Tributária) code using a read-only character span.</summary>
        /// <remarks>
        /// Extracts numbers, pads with leading zeros up to 7 characters, and invalidates zero-only strings.
        /// </remarks>
        /// <param name="cest">The <see cref="ReadOnlySpan{T}"/> containing the CEST code.</param>
        /// <returns>The formatted CEST code, or <see cref="string.Empty"/> if invalid.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormatCestCode(ReadOnlySpan<char> cest)
        {
            if (cest.IsEmpty) return string.Empty;
            
            Span<char> buffer = stackalloc char[7];
            int len = FormatAndValidationHelper.ExtractOnlyNumbers(cest, buffer);
            
            if (len is 0 or > 7) return string.Empty;
            
            FormatAndValidationHelper.FillStringZeroLeft(buffer, ref len, 7);
            
            // If it is composed entirely of zeros
            if (FormatAndValidationHelper.IsRepeatedString(buffer) && buffer[0] == '0') 
                return string.Empty; 
                
            return string.Create(7, buffer.ToArray(), static (span, state) => state.AsSpan(0, 7).CopyTo(span[..7]));
        }

        /// <summary>Formats an EAN (European Article Number) barcode using a read-only character span.</summary>
        /// <param name="ean">The <see cref="ReadOnlySpan{T}"/> containing the EAN code.</param>
        /// <param name="leftZero">Determines whether to preserve or pad leading zeros.</param>
        /// <returns>The formatted EAN code, or <see cref="string.Empty"/> if invalid.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormatEanCode(ReadOnlySpan<char> ean, bool leftZero = false)
        {
            if (ean.IsEmpty) return string.Empty;
            
            const int maxSizeEan = 14;
            int sizeEan = Math.Min(ean.Length, maxSizeEan);
            Span<char> buffer = stackalloc char[sizeEan];
            
            int newLengthDest = !leftZero 
                ? FormatAndValidationHelper.ExtractOnlyNumbers(ean, buffer) 
                : FormatAndValidationHelper.CleanStringInMemory(ean, buffer);
                
            return string.Create(sizeEan, buffer.ToArray(), static (span, state) => state.AsSpan(0, state.Length).CopyTo(span[..state.Length]));
        }

        #endregion

        #region RawNumbers

        #region long

        /// <summary>Formats a numeric NCM code.</summary>
        /// <param name="ncm">The <see cref="long"/> numeric value of the NCM code.</param>
        /// <returns>The formatted NCM code, or <see cref="string.Empty"/> if invalid.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormatNcmCode(long ncm)
        {
            Span<char> buffer = stackalloc char[20];
            int charsWritten = ncm.ToSpan(buffer);
            return FormatNcmCode(buffer[..charsWritten]);
        }

        /// <summary>Formats a numeric CEST code.</summary>
        /// <param name="cest">The <see cref="long"/> numeric value of the CEST code.</param>
        /// <returns>The formatted CEST code, or <see cref="string.Empty"/> if invalid.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormatCestCode(long cest)
        {
            Span<char> buffer = stackalloc char[20];
            int charsWritten = cest.ToSpan(buffer);
            return FormatCestCode(buffer[..charsWritten]);
        }

        /// <summary>Formats a numeric EAN barcode.</summary>
        /// <param name="ean">The <see cref="long"/> numeric value of the EAN code.</param>
        /// <returns>The formatted EAN code, or <see cref="string.Empty"/> if invalid.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormatEanCode(long ean)
        {
            Span<char> buffer = stackalloc char[20];
            int charsWritten = ean.ToSpan(buffer);
            return FormatEanCode(buffer[..charsWritten]);
        }

        #endregion

        #region ulong

        /// <summary>Formats a numeric NCM code.</summary>
        /// <param name="ncm">The <see cref="ulong"/> numeric value of the NCM code.</param>
        /// <returns>The formatted NCM code, or <see cref="string.Empty"/> if invalid.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormatNcmCode(ulong ncm)
        {
            Span<char> buffer = stackalloc char[20];
            int charsWritten = ncm.ToSpan(buffer);
            return FormatNcmCode(buffer[..charsWritten]);
        }

        /// <summary>Formats a numeric CEST code.</summary>
        /// <param name="cest">The <see cref="ulong"/> numeric value of the CEST code.</param>
        /// <returns>The formatted CEST code, or <see cref="string.Empty"/> if invalid.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormatCestCode(ulong cest)
        {
            Span<char> buffer = stackalloc char[20];
            int charsWritten = cest.ToSpan(buffer);
            return FormatCestCode(buffer[..charsWritten]);
        }

        /// <summary>Formats a numeric EAN barcode.</summary>
        /// <param name="ean">The <see cref="ulong"/> numeric value of the EAN code.</param>
        /// <returns>The formatted EAN code, or <see cref="string.Empty"/> if invalid.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormatEanCode(ulong ean)
        {
            Span<char> buffer = stackalloc char[20];
            int charsWritten = ean.ToSpan(buffer);
            return FormatEanCode(buffer[..charsWritten]);
        }   

        #endregion

        #endregion

    }
}