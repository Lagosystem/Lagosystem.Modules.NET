
namespace Lagosystem.Modules.Validations
{

    using Lagosystem.Modules.Extensions;
    using Lagosystem.Modules.Helpers;
    using System.Numerics;
    using System.Runtime.CompilerServices;

    public static partial class Validate
    {
        #region Numeric Validations (IsNumeric)

        /// <summary>Checks if the provided value is a valid number.</summary>
        /// <param name="_">The 8-bit signed integer to evaluate.</param>
        /// <returns>Always returns <see langword="true"/> instantly for this primitive numeric type.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool IsNumeric(sbyte _) => true;
        
        /// <summary>Checks if the provided value is a valid number.</summary>
        /// <param name="_">The 8-bit unsigned integer to evaluate.</param>
        /// <returns>Always returns <see langword="true"/> instantly for this primitive numeric type.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool IsNumeric(byte _) => true;
        
        /// <summary>Checks if the provided value is a valid number.</summary>
        /// <param name="_">The 16-bit signed integer to evaluate.</param>
        /// <returns>Always returns <see langword="true"/> instantly for this primitive numeric type.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool IsNumeric(short _) => true;
        
        /// <summary>Checks if the provided value is a valid number.</summary>
        /// <param name="_">The 16-bit unsigned integer to evaluate.</param>
        /// <returns>Always returns <see langword="true"/> instantly for this primitive numeric type.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool IsNumeric(ushort _) => true;
        
        /// <summary>Checks if the provided value is a valid number.</summary>
        /// <param name="_">The 32-bit signed integer to evaluate.</param>
        /// <returns>Always returns <see langword="true"/> instantly for this primitive numeric type.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool IsNumeric(int _) => true;
        
        /// <summary>Checks if the provided value is a valid number.</summary>
        /// <param name="_">The 32-bit unsigned integer to evaluate.</param>
        /// <returns>Always returns <see langword="true"/> instantly for this primitive numeric type.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool IsNumeric(uint _) => true;
        
        /// <summary>Checks if the provided value is a valid number.</summary>
        /// <param name="_">The 64-bit signed integer to evaluate.</param>
        /// <returns>Always returns <see langword="true"/> instantly for this primitive numeric type.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool IsNumeric(long _) => true;
        
        /// <summary>Checks if the provided value is a valid number.</summary>
        /// <param name="_">The 64-bit unsigned integer to evaluate.</param>
        /// <returns>Always returns <see langword="true"/> instantly for this primitive numeric type.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool IsNumeric(ulong _) => true;
        
        /// <summary>Checks if the provided value is a valid number.</summary>
        /// <param name="_">The single-precision floating-point number to evaluate.</param>
        /// <returns>Always returns <see langword="true"/> instantly for this primitive numeric type.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool IsNumeric(float _) => true;
        
        /// <summary>Checks if the provided value is a valid number.</summary>
        /// <param name="_">The double-precision floating-point number to evaluate.</param>
        /// <returns>Always returns <see langword="true"/> instantly for this primitive numeric type.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool IsNumeric(double _) => true;
        
        /// <summary>Checks if the provided value is a valid number.</summary>
        /// <param name="_">The decimal number to evaluate.</param>
        /// <returns>Always returns <see langword="true"/> instantly for this primitive numeric type.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool IsNumeric(decimal _) => true;
        
        /// <summary>Checks if the provided value is a valid number.</summary>
        /// <param name="_">The BigInteger to evaluate.</param>
        /// <returns>Always returns <see langword="true"/> instantly for this primitive numeric type.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool IsNumeric(BigInteger _) => true;

        /// <summary>Checks if the given object is a numeric type or a valid numeric string.</summary>
        /// <param name="obj">The object to evaluate.</param>
        /// <returns><see langword="true"/> if the object is numeric or parses to a valid number; otherwise, <see langword="false"/>.</returns>
        public static bool IsNumeric(object? obj) => obj switch
        {
            null => false,
            sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal
                or BigInteger or Complex => true,
            char ch => (uint)(ch - '0') <= 9u,
            string str => IsNumeric(str),
            _ => IsNumeric(obj.ToString())
        };

        /// <summary>Performs a high-performance check to determine if a string represents a valid number.</summary>
        /// <param name="input">The string to evaluate.</param>
        /// <returns><see langword="true"/> if the string represents a valid number; otherwise, <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNumeric(string? input) => !input.IsBlank() && IsNumeric((input ?? string.Empty).AsSpan());

        /// <summary>
        /// Checks if the provided span represents a valid number without allocating memory (Zero Alloc).
        /// Supports standard decimals and Hexadecimals (e.g., 0xFF, $FF, &amp;HFF).
        /// </summary>
        /// <param name="input">A read-only span of characters representing the numeric value.</param>
        /// <returns><see langword="true"/> if the span represents a valid number; otherwise, <see langword="false"/>.</returns>
        public static bool IsNumeric(ReadOnlySpan<char> input)
        {
            input = FormatAndValidationHelper.TrimWhiteSpace(input);
            if (input.IsEmpty) return false;
            
            int startIndex = 0;
            int endIndex = input.Length - 1;
            
            if (input[startIndex] == '-')
            {
                startIndex++;
                if (startIndex > endIndex) return false;
            }

            int length = endIndex - startIndex + 1;
            
            // Hex checks
            if (length >= 3 && input[startIndex] == '0' && (input[startIndex + 1] == 'x' || input[startIndex + 1] == 'X'))
                return AllCharsAreHex(input, startIndex + 2, endIndex);
            if (input[startIndex] == '$') return AllCharsAreHex(input, startIndex + 1, endIndex);
            if (length >= 3 && input[startIndex] == '&' && (input[startIndex + 1] == 'h' || input[startIndex + 1] == 'H'))
                return AllCharsAreHex(input, startIndex + 2, endIndex);
            if (input[startIndex] == '#') return AllCharsAreHex(input, startIndex + 1, endIndex);
            if (length >= 2 && (input[endIndex] == 'h' || input[endIndex] == 'H')) return AllCharsAreHex(input, startIndex, endIndex - 1);
            
            return IsValidDecimalFormat(input, startIndex, endIndex);
        }

        private static bool IsValidDecimalFormat(ReadOnlySpan<char> input, int startIndex, int endIndex)
        {
            bool hasDigit = false;
            bool hasSeparator = false;
            for (int i = startIndex; i <= endIndex; i++)
            {
                char c = input[i];
                if ((uint)(c - '0') <= 9u)
                {
                    hasDigit = true;
                    continue;
                }
                if (hasSeparator || (c != '.' && c != ',')) return false;
                if (!hasDigit || i == endIndex) return false;
                hasSeparator = true;
            }
            return hasDigit;
        }

        private static bool AllCharsAreHex(ReadOnlySpan<char> input, int startIndex, int endIndex)
        {
            if (startIndex > endIndex) return false;
            for (int i = startIndex; i <= endIndex; ++i)
            {
                char c = input[i];
                if (!((uint)(c - '0') <= 9u || (uint)((c | 0x20) - 'a') <= 5u)) return false;
            }
            return true;
        }

        #endregion


    }
}
