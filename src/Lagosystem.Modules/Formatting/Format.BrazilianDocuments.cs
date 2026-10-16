

using System.Runtime.CompilerServices;

namespace Lagosystem.Modules.Formatting
{
    using Lagosystem.Modules.Helpers;
    using Extensions;

    using System;

    public static partial class Format
    {
      
        #region Memory

        /// <summary>Formats a CPF character sequence by applying the standard mask (000.000.000-00).</summary>
        /// <remarks>
        /// This method extracts only the numbers, pads with leading zeros (up to 11 positions if necessary), 
        /// and discards CPFs composed of repeated numbers. The process uses <see cref="string.Create"/> for memory optimization.
        /// </remarks>
        /// <param name="cpf">The <see cref="ReadOnlySpan{T}"/> containing the CPF to format.</param>
        /// <returns>The formatted <see cref="string"/> with the CPF mask, or <see cref="string.Empty"/> if invalid.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormatBrazilianCpf(ReadOnlySpan<char> cpf)
        {
            Span<char> buffer = stackalloc char[11];
            int len = FormatAndValidationHelper.ExtractOnlyNumbers(cpf, buffer);

            if (len is 0 or > 11) return string.Empty;
            FormatAndValidationHelper.FillStringZeroLeft(buffer, ref len, 11);
            if (FormatAndValidationHelper.IsRepeatedString(buffer)) return string.Empty;

            // String.Create allocates the final string directly, populating it through the Span
            return string.Create(14, buffer.ToArray(), static (span, state) =>
            {
                state.AsSpan(0, 3).CopyTo(span[..3]);
                span[3] = '.';
                state.AsSpan(3, 3).CopyTo(span.Slice(4, 3));
                span[7] = '.';
                state.AsSpan(6, 3).CopyTo(span.Slice(8, 3));
                span[11] = '-';
                state.AsSpan(9, 2).CopyTo(span.Slice(12, 2));
            });
        }

        /// <summary>Formats a CNPJ character sequence by applying the standard mask (00.000.000/0000-00).</summary>
        /// <remarks>
        /// This method extracts the numbers, pads with leading zeros (if the length is 12, 13, or 14), 
        /// and discards CNPJs with repeated numbers. It uses <see cref="string.Create"/> to minimize allocations.
        /// </remarks>
        /// <param name="cnpj">The <see cref="ReadOnlySpan{T}"/> containing the CNPJ to format.</param>
        /// <returns>The formatted <see cref="string"/> with the CNPJ mask, or <see cref="string.Empty"/> if invalid.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormatBrazilianCnpj(ReadOnlySpan<char> cnpj)
        {
            Span<char> buffer = stackalloc char[14];
            int len = FormatAndValidationHelper.ExtractOnlyNumbers(cnpj, buffer);

            if (len is 0 or > 14 or <= 11) return string.Empty;
            FormatAndValidationHelper.FillStringZeroLeft(buffer, ref len, 14);
            if (FormatAndValidationHelper.IsRepeatedString(buffer)) return string.Empty;

            return string.Create(18, buffer.ToArray(), static (span, state) =>
            {
                state.AsSpan(0, 2).CopyTo(span[..2]);
                span[2] = '.';
                state.AsSpan(2, 3).CopyTo(span.Slice(3, 3));
                span[6] = '.';
                state.AsSpan(5, 3).CopyTo(span.Slice(7, 3));
                span[10] = '/';
                state.AsSpan(8, 4).CopyTo(span.Slice(11, 4));
                span[15] = '-';
                state.AsSpan(12, 2).CopyTo(span.Slice(16, 2));
            });
        }

        /// <summary>Formats a ZIP Code (CEP) character sequence by applying the standard mask (00000-000).</summary>
        /// <remarks>
        /// Extracts the numbers, pads with leading zeros (up to 8 positions), and discards repeated sequences.
        /// </remarks>
        /// <param name="cep">The <see cref="ReadOnlySpan{T}"/> containing the ZIP Code to format.</param>
        /// <returns>The formatted <see cref="string"/> with the ZIP Code mask, or <see cref="string.Empty"/> if invalid.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormatBrazilianZipCode(ReadOnlySpan<char> cep)
        {
            Span<char> buffer = stackalloc char[8];
            int len = FormatAndValidationHelper.ExtractOnlyNumbers(cep, buffer);

            if (len is 0 or > 8) return string.Empty;
            FormatAndValidationHelper.FillStringZeroLeft(buffer, ref len, 8);
            if (FormatAndValidationHelper.IsRepeatedString(buffer))
                return string.Empty; // Equivalent to formatted cep == "00000-000"

            return string.Create(9, buffer.ToArray(), static (span, state) =>
            {
                state.AsSpan(0, 5).CopyTo(span[..5]);
                span[5] = '-';
                state.AsSpan(5, 3).CopyTo(span.Slice(6, 3));
            });
        }

        #endregion

        #region Raw String

        /// <summary>Formats a CPF string by applying the standard mask (000.000.000-00).</summary>
        /// <remarks>
        /// This method extracts only the numbers, pads with leading zeros (up to 11 positions if necessary), 
        /// and discards CPFs composed of repeated numbers. The process uses <see cref="string.Create"/> for memory optimization.
        /// </remarks>
        /// <param name="cpf">The raw string containing the CPF to format.</param>
        /// <returns>The formatted <see cref="string"/> with the CPF mask, or <see cref="string.Empty"/> if invalid.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormatBrazilianCpf(string cpf) => cpf.IsBlank() ? string.Empty : FormatBrazilianCpf(cpf.AsSpan());

        /// <summary>Formats a CNPJ string by applying the standard mask (00.000.000/0000-00).</summary>
        /// <remarks>
        /// This method extracts the numbers, pads with leading zeros (if the length is 12, 13, or 14), 
        /// and discards CNPJs with repeated numbers. It uses <see cref="string.Create"/> to minimize allocations.
        /// </remarks>
        /// <param name="cnpj">The raw string containing the CNPJ to format.</param>
        /// <returns>The formatted <see cref="string"/> with the CNPJ mask, or <see cref="string.Empty"/> if invalid.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormatBrazilianCnpj(string cnpj) => cnpj.IsBlank() ? string.Empty : FormatBrazilianCnpj(cnpj.AsSpan());

        /// <summary>Formats a ZIP Code (CEP) string by applying the standard mask (00000-000).</summary>
        /// <remarks>
        /// Extracts the numbers, pads with leading zeros (up to 8 positions), and discards repeated sequences.
        /// </remarks>
        /// <param name="cep">The raw string containing the ZIP Code to format.</param>
        /// <returns>The formatted <see cref="string"/> with the ZIP Code mask, or <see cref="string.Empty"/> if invalid.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormatBrazilianZipCode(string cep) => cep.IsBlank() ? string.Empty : FormatBrazilianZipCode(cep.AsSpan());

        #endregion

        #region RawNumbers

        /// <summary>Formats a numeric CPF value by applying the standard mask (000.000.000-00).</summary>
        /// <remarks>
        /// The method extracts the numbers, pads with leading zeros (up to 11 positions), 
        /// and discards CPFs composed of repeated numbers.
        /// </remarks>
        /// <param name="cpf">The <see cref="long"/> numeric value of the CPF to format.</param>
        /// <returns>The formatted <see cref="string"/> with the CPF mask, or <see cref="string.Empty"/> if invalid.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormatBrazilianCpf(long cpf)
        {
            Span<char> buffer = stackalloc char[20];
            int charsWritten = cpf.ToSpan(buffer);
            return FormatBrazilianCpf(buffer[..charsWritten]);
        }

        /// <summary>Formats a numeric CNPJ value by applying the standard mask (00.000.000/0000-00).</summary>
        /// <remarks>
        /// The method extracts the numbers, pads with leading zeros (up to 14 positions), 
        /// and discards CNPJs composed of repeated numbers.
        /// </remarks>
        /// <param name="cnpj">The <see cref="long"/> numeric value of the CNPJ to format.</param>
        /// <returns>The formatted <see cref="string"/> with the CNPJ mask, or <see cref="string.Empty"/> if invalid.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormatBrazilianCnpj(long cnpj)
        {
            Span<char> buffer = stackalloc char[20];
            int charsWritten = cnpj.ToSpan(buffer);
            return FormatBrazilianCnpj(buffer[..charsWritten]);
        }

        /// <summary>Formats a numeric ZIP Code (CEP) value by applying the standard mask (00000-000).</summary>
        /// <remarks>
        /// Extracts the numbers, pads with leading zeros (up to 8 positions), and discards repeated sequences.
        /// </remarks>
        /// <param name="cep">The <see cref="long"/> numeric value of the ZIP Code to format.</param>
        /// <returns>The formatted <see cref="string"/> with the ZIP Code mask, or <see cref="string.Empty"/> if invalid.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormatBrazilianZipCode(long cep)
        {
            Span<char> buffer = stackalloc char[20];
            int charsWritten = cep.ToSpan(buffer);
            return FormatBrazilianZipCode(buffer[..charsWritten]);
        }

        /// <summary>Formats a numeric CPF value by applying the standard mask (000.000.000-00).</summary>
        /// <remarks>
        /// The method extracts the numbers, pads with leading zeros (up to 11 positions), 
        /// and discards CPFs composed of repeated numbers.
        /// </remarks>
        /// <param name="cpf">The <see cref="ulong"/> numeric value of the CPF to format.</param>
        /// <returns>The formatted <see cref="string"/> with the CPF mask, or <see cref="string.Empty"/> if invalid.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormatBrazilianCpf(ulong cpf)
        {
            Span<char> buffer = stackalloc char[20];
            int charsWritten = cpf.ToSpan(buffer);
            return FormatBrazilianCpf(buffer[..charsWritten]);
        }

        /// <summary>Formats a numeric CNPJ value by applying the standard mask (00.000.000/0000-00).</summary>
        /// <remarks>
        /// The method extracts the numbers, pads with leading zeros (up to 14 positions), 
        /// and discards CNPJs composed of repeated numbers.
        /// </remarks>
        /// <param name="cnpj">The <see cref="ulong"/> numeric value of the CNPJ to format.</param>
        /// <returns>The formatted <see cref="string"/> with the CNPJ mask, or <see cref="string.Empty"/> if invalid.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormatBrazilianCnpj(ulong cnpj)
        {
            Span<char> buffer = stackalloc char[20];
            int charsWritten = cnpj.ToSpan(buffer);
            return FormatBrazilianCnpj(buffer[..charsWritten]);
        }

        /// <summary>Formats a numeric ZIP Code (CEP) value by applying the standard mask (00000-000).</summary>
        /// <remarks>
        /// Extracts the numbers, pads with leading zeros (up to 8 positions), and discards repeated sequences.
        /// </remarks>
        /// <param name="cep">The <see cref="ulong"/> numeric value of the ZIP Code to format.</param>
        /// <returns>The formatted <see cref="string"/> with the ZIP Code mask, or <see cref="string.Empty"/> if invalid.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormatBrazilianZipCode(ulong cep)
        {
            Span<char> buffer = stackalloc char[20];
            int charsWritten = cep.ToSpan(buffer);
            return FormatBrazilianZipCode(buffer[..charsWritten]);
        }

        #endregion

        #region Any Object

        /// <summary>Formats any generic object representing a CPF by applying the standard mask (000.000.000-00).</summary>
        /// <typeparam name="T">The type of the object being formatted.</typeparam>
        /// <param name="cpf">The object representing the CPF.</param>
        /// <returns>The formatted <see cref="string"/> with the CPF mask, or <see cref="string.Empty"/> if invalid.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormatBrazilianCpf<T>(T? cpf) => ApplyMask(cpf, 11, "000.000.000-00");

        /// <summary>Formats any generic object representing a CNPJ by applying the standard mask (00.000.000/0000-00).</summary>
        /// <typeparam name="T">The type of the object being formatted.</typeparam>
        /// <param name="cnpj">The object representing the CNPJ.</param>
        /// <returns>The formatted <see cref="string"/> with the CNPJ mask, or <see cref="string.Empty"/> if invalid.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormatBrazilianCnpj<T>(T? cnpj) => ApplyMask(cnpj, 14, "00.000.000/0000-00");

        /// <summary>Formats any generic object representing a ZIP Code (CEP) by applying the standard mask (00000-000).</summary>
        /// <typeparam name="T">The type of the object being formatted.</typeparam>
        /// <param name="zipCode">The object representing the ZIP Code.</param>
        /// <returns>The formatted <see cref="string"/> with the ZIP Code mask, or <see cref="string.Empty"/> if invalid.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormatBrazilianZipCode<T>(T? zipCode) => ApplyMask(zipCode, 8, "00000-000");

        // ---------------------------------------------------------
        // The high-performance engine that does the heavy lifting
        // ---------------------------------------------------------

        /// <summary>Applies a formatting mask to a given input object using zero-allocation span parsing.</summary>
        /// <typeparam name="T">The type of the source object.</typeparam>
        /// <param name="source">The input to be masked.</param>
        /// <param name="requiredDigits">The exact number of numeric digits required for the mask.</param>
        /// <param name="mask">A read-only span containing the template mask, where '0' represents a digit insertion point.</param>
        /// <returns>A newly allocated formatted <see cref="string"/>, or <see cref="string.Empty"/> if the source is invalid.</returns>
        private static string ApplyMask<T>(T? source, int requiredDigits, ReadOnlySpan<char> mask)
        {
            // Allocates the pure digits on the Stack
            Span<char> digits = stackalloc char[requiredDigits];
            if (!TryGetPaddedDigits(source, requiredDigits, digits)) return string.Empty;

            // Allocates the final result on the Stack based on the exact size of the mask
            Span<char> result = stackalloc char[mask.Length];

            int digitIndex = 0;

            // A direct, simple loop mapping digits into the Span memory
            for (int i = 0; i < mask.Length; i++)
                result[i] = mask[i] == '0' 
                    ? digits[digitIndex++] // If the mask requests a number, we pull from our digit span
                    : mask[i];             // If it is punctuation (., -, /), we copy it directly from the mask
            
            return new string(result); // The only actual memory allocation (Heap) at the very end of the process
        }

        /// <summary>
        /// Extracts purely numerical digits from a generic source object and pads them with leading zeros to meet a required length.
        /// </summary>
        /// <typeparam name="T">The type of the source object.</typeparam>
        /// <param name="source">The source object to parse.</param>
        /// <param name="requiredDigits">The minimum amount of digits required.</param>
        /// <param name="output">A span buffer to receive the extracted and padded digits.</param>
        /// <returns><see langword="true"/> if digits were successfully extracted; otherwise <see langword="false"/>.</returns>
        private static bool TryGetPaddedDigits<T>(T? source, int requiredDigits, Span<char> output)
        {
            if (source is null) return false;

            string str = source as string ?? source.ToString();
            if (str.IsBlank()) return false;

            Span<char> numbers = stackalloc char[str.Length];
            int count = 0;

            foreach (char c in str)
                if ((uint)(c - '0') <= 9u) 
                    numbers[count++] = c;

            if (count == 0) return false;

            if (count >= requiredDigits)
                numbers.Slice(count - requiredDigits, requiredDigits).CopyTo(output);
            else
            {
                int pad = requiredDigits - count;
                output[..pad].Fill('0');
                numbers[..count].CopyTo(output.Slice(pad));
            }

            return true;
        }

        #endregion
        
    }
}