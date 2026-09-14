
namespace Lagosystem.Modules.Validations
{
    using System.Runtime.CompilerServices;
    using Extensions;

    public static partial class Validate
    {

        #region NullIf

        /// <summary>
        /// Returns <see langword="null"/> if the specified current value is equal to the condition value; 
        /// otherwise, returns the current value.
        /// </summary>
        /// <typeparam name="T">The type of the objects to compare, restricted to reference types.</typeparam>
        /// <param name="currentValue">The current value to evaluate.</param>
        /// <param name="condition">The condition value to compare against the current value.</param>
        /// <returns><see langword="null"/> if <paramref name="currentValue"/> is null or equals <paramref name="condition"/>; otherwise, <paramref name="currentValue"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T? NullIf<T>(T? currentValue, T condition) where T : class =>
            currentValue is null || EqualityComparer<T>.Default.Equals(currentValue, condition)
                ? null
                : currentValue;

        /// <summary>
        /// Returns <see langword="null"/> if the specified current nullable value is equal to the condition value; 
        /// otherwise, returns the current value.
        /// </summary>
        /// <typeparam name="T">The type of the objects to compare, restricted to value types (<see langword="structs"/>).</typeparam>
        /// <param name="currentValue">The current nullable value to evaluate.</param>
        /// <param name="condition">The condition value to compare against the current value.</param>
        /// <returns><see langword="null"/> if <paramref name="currentValue"/> has no value or equals <paramref name="condition"/>; otherwise, <paramref name="currentValue"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T? NullIf<T>(T? currentValue, T condition) where T : struct =>
            !currentValue.HasValue || EqualityComparer<T>.Default.Equals(currentValue.Value, condition)
                ? null
                : currentValue;

        /// <summary>
        /// Returns <see langword="null"/> if the specified string is blank or equals the condition string 
        /// using an ordinal case-insensitive comparison; otherwise, returns the current string.
        /// </summary>
        /// <param name="currentValue">The current string to evaluate.</param>
        /// <param name="condition">The condition string to compare against the current string.</param>
        /// <returns><see langword="null"/> if <paramref name="currentValue"/> is blank or equals <paramref name="condition"/>; otherwise, <paramref name="currentValue"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string? NullIf(string? currentValue, string? condition) =>
            NullIf(currentValue, condition, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Returns <see langword="null"/> if the specified string is blank or equals the condition string 
        /// using the specified string comparison rule; otherwise, returns the current string.
        /// </summary>
        /// <param name="currentValue">The current string to evaluate.</param>
        /// <param name="condition">The condition string to compare against the current string.</param>
        /// <param name="comparison">One of the enumeration values that specifies the rules for the string comparison.</param>
        /// <returns><see langword="null"/> if <paramref name="currentValue"/> is blank or equals <paramref name="condition"/>; otherwise, <paramref name="currentValue"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string? NullIf(string? currentValue, string? condition, StringComparison comparison) =>
            currentValue.IsBlank() || string.Equals(currentValue, condition, comparison)
                ? null
                : currentValue;

        #endregion

        #region Base64

        /// <summary>
        /// Validates whether the specified character sequence is a structurally valid Base-64 format.
        /// Performs a blazing-fast, zero-allocation check using Spans.
        /// </summary>
        /// <param name="base64">The character sequence to validate.</param>
        /// <returns><see langword="true"/> if the sequence is valid Base-64; otherwise, <see langword="false"/>.</returns>
        public static bool IsValidBase64(ReadOnlySpan<char> base64)
        {
            if (base64.IsEmpty) return false;
            int validCharCount = 0;
            int paddingCount = 0;
            for (int i = 0; i < base64.Length; i++)
            {
                char c = base64[i];
                if (char.IsWhiteSpace(c)) continue; // Base64 specification ignores whitespaces (CR, LF, Space, Tab)
                if (c == '=') 
                {
                    paddingCount++;
                    validCharCount++;
                    continue;
                }
                if (paddingCount > 0) return false; // If we already encountered padding '=', any subsequent non-padding characters make the format invalid
                if ((c >= 'A' && c <= 'Z') ||
                    (c >= 'a' && c <= 'z') ||
                    (c >= '0' && c <= '9') ||
                    c == '+' || c == '/')
                    validCharCount++; // Valid Base64 chars: A-Z, a-z, 0-9, +, /
                else
                    return false; // Invalid character found
            }

            // A valid Base64 string must have a length divisible by 4 (excluding whitespaces)
            // and cannot have more than 2 padding characters at the very end.
            return validCharCount > 0 && validCharCount % 4 == 0 && paddingCount <= 2;
        }

        /// <summary>Validates whether the specified character sequence is a structurally valid Base-64 format.</summary>
        /// <param name="base64">The character sequence to validate.</param>
        /// <returns><see langword="true"/> if the sequence is valid Base-64; otherwise, <see langword="false"/>.</returns>
        public static bool IsValidBase64(string? base64) => !base64.IsBlank() && IsValidBase64(base64!.AsSpan());
        
        #endregion
        
    }

}
