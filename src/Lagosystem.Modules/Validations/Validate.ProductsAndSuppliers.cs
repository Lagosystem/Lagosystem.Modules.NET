


using Lagosystem.Modules.Extensions;

namespace Lagosystem.Modules.Validations
{
    using System;
    using System.Runtime.CompilerServices;
    using Lagosystem.Modules.Helpers;
    using System.Text.RegularExpressions;

    public static partial class Validate
    {
        private static readonly Regex SupplierCodeRegex = new Regex(
            @"^(?!\d+$)(?=.*[A-Za-z$#+*/:,.|^~`´!?{}[\]()_%/=§¢£@-])[A-Za-z\d$#+*/:,.|^~`´!?{}[\]()_%/=§¢£@-]{1,50}$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline, TimeSpan.FromSeconds(2)
        );


        #region Product & Supplier Validations

        /// <summary>
        /// Validates a barcode (GTIN-8, UPC, EAN-13, DUN-14).
        /// Supports standard module 10 check digit and expands UPC-E to UPC-A automatically.
        /// </summary>
        /// <param name="code">The barcode string to validate.</param>
        /// <param name="isFiscal">Indicates whether strict fiscal length rules (8, 12, 13, or 14 digits) should be applied.</param>
        /// <returns><see langword="true"/> if the barcode is valid; otherwise, <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValidGtin(string? code, bool isFiscal = false) =>
            !code.IsBlank() && IsValidGtin(code.AsSpan(), isFiscal);

        /// <summary>
        /// Validates a barcode utilizing a character span, avoiding memory allocations (Zero Alloc).
        /// Supports standard module 10 check digit and expands UPC-E to UPC-A automatically.
        /// </summary>
        /// <param name="input">A read-only span of characters representing the barcode.</param>
        /// <param name="isFiscal">Indicates whether strict fiscal length rules (8, 12, 13, or 14 digits) should be applied.</param>
        /// <returns><see langword="true"/> if the barcode is valid; otherwise, <see langword="false"/>.</returns>
        public static bool IsValidGtin(ReadOnlySpan<char> input, bool isFiscal = false)
        {
            if (input.IsEmpty) return false;

            Span<char> extractedNumber = stackalloc char[32];
            int len = FormatAndValidationHelper.ExtractOnlyNumbers(input, extractedNumber);
            
            if (len > 32) return false; // Safety catch if input exceeded stack buffer

            if (isFiscal && len != 8 && len != 12 && len != 13 && len != 14) return false;
            if (!isFiscal && len < 7) return false;

            ReadOnlySpan<char> currentCode = extractedNumber[..len];

            if (ValidateBarcodeCheckDigit(currentCode)) return true;

            if (input.Length != 8) return false;

            // UPC-E to UPC-A Expansion
            Span<char> expanded = stackalloc char[12];
            expanded[0] = '0';
            char c7 = input[6];
            expanded[1] = input[1];
            expanded[2] = input[2];

            switch (c7)
            {
                case '0': case '1': case '2':
                    expanded[3] = c7; expanded[4] = '0'; expanded[5] = '0'; expanded[6] = '0';
                    expanded[7] = '0'; expanded[8] = input[3]; expanded[9] = input[4]; expanded[10] = input[5];
                    break;
                case '3':
                    expanded[3] = input[3]; expanded[4] = '0'; expanded[5] = '0'; expanded[6] = '0';
                    expanded[7] = '0'; expanded[8] = '0'; expanded[9] = input[4]; expanded[10] = input[5];
                    break;
                case '4':
                    expanded[3] = input[3]; expanded[4] = input[4]; expanded[5] = '0'; expanded[6] = '0';
                    expanded[7] = '0'; expanded[8] = '0'; expanded[9] = '0'; expanded[10] = input[5];
                    break;
                default:
                    expanded[3] = input[3]; expanded[4] = input[4]; expanded[5] = input[5]; expanded[6] = '0';
                    expanded[7] = '0'; expanded[8] = '0'; expanded[9] = '0'; expanded[10] = c7;
                    break;
            }
            expanded[11] = input[7];
            return ValidateBarcodeCheckDigit(expanded);
        }

        private static bool ValidateBarcodeCheckDigit(ReadOnlySpan<char> span)
        {
            if (span.IsEmpty) return false;

            int timesFactor = 3;
            int currentSum = 0;
            int length = span.Length;

            for (int i = length - 2; i >= 0; --i)
            {
                currentSum += (span[i] - '0') * timesFactor;
                timesFactor = (timesFactor == 3) ? 1 : 3;
            }

            int expectedDigit = (10 - (currentSum % 10)) % 10;
            int realDigit = span[length - 1] - '0';

            return expectedDigit == realDigit;
        }

        /// <summary>
        /// Validates if a string is a valid supplier code. 
        /// It cannot be purely numeric and must contain at least one letter or special character.
        /// </summary>
        /// <param name="supplierCode">The supplier code string to validate.</param>
        /// <returns><see langword="true"/> if the supplier code is valid; otherwise, <see langword="false"/>.</returns>
        public static bool IsValidSupplierCode(string? supplierCode)
        {
            try
            {
                return !supplierCode.IsBlank() && SupplierCodeRegex.IsMatch(supplierCode!);
            }
            catch (StackOverflowException) { throw; }
            catch (OutOfMemoryException) { throw; }
            catch (ThreadAbortException) { throw; }
            catch (Exception ex)
            {
                LagosystemModulesDiagnostic.ReportError(nameof(IsValidSupplierCode), nameof(Validate), ex, "Error validating supplier code");
                return false;
            }
        }

        #endregion

    }

}