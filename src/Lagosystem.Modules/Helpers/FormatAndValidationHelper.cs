
using Lagosystem.Modules.Formatting;

namespace Lagosystem.Modules.Helpers
{
    using System.Globalization;
    using System.Runtime.CompilerServices;
    
    internal static class FormatAndValidationHelper
    {
        internal static int ExtractOnlyNumbers(ReadOnlySpan<char> inputBuffer, Span<char> destinationBuffer)
        {
            int length = 0;
            int maxLength = destinationBuffer.Length;
            for (int indexChar = 0; indexChar < inputBuffer.Length; ++indexChar)
            {
                char character = inputBuffer[indexChar];
                if (character is < '0' or > '9') continue;
                if (length >= maxLength) return length + 1;
                destinationBuffer[length++] = character;
            }
            return length;
        }

        internal static bool IsRepeatedString(ReadOnlySpan<char> inputBuffer)
        {
            if (inputBuffer.Length == 0) return true;
            char c = inputBuffer[0];
            for (int i = 1; i < inputBuffer.Length; i++)
                if (inputBuffer[i] != c)
                    return false;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void FillStringZeroLeft(Span<char> destinationBuffer, ref int currentLen, int targetLen)
        {
            if (currentLen >= targetLen) return;
            int diff = targetLen - currentLen;
            destinationBuffer[..currentLen].CopyTo(destinationBuffer[diff..]);
            destinationBuffer[..diff].Fill('0');
            currentLen = targetLen;
        }

        internal static bool HasOnlyDigits(ReadOnlySpan<char> inputBuffer)
        {
            if (inputBuffer.IsEmpty) return false;
            for (int i = 0; i < inputBuffer.Length; i++)
                if ((uint)(inputBuffer[i] - '0') > 9u)
                    return false;
            return true;
        }

        internal static ReadOnlySpan<char> TrimWhiteSpace(ReadOnlySpan<char> inputBuffer)
        {
            int startIndexChar = 0;
            int endIndexChar = inputBuffer.Length - 1;
            while (startIndexChar <= endIndexChar && char.IsWhiteSpace(inputBuffer[startIndexChar])) ++startIndexChar;
            while (endIndexChar >= startIndexChar && char.IsWhiteSpace(inputBuffer[endIndexChar])) --endIndexChar;
            return startIndexChar > endIndexChar ? ReadOnlySpan<char>.Empty : inputBuffer[startIndexChar..(endIndexChar + 1)];
        }

        internal static bool HasAnyDigit(ReadOnlySpan<char> inputBufer)
        {
            for (int indexChar = 0; indexChar < inputBufer.Length; ++indexChar)
                if ((uint)(inputBufer[indexChar] - '0') <= 9u)
                    return true;
            return false;
        }

        internal static int CountDigitsUntil(ReadOnlySpan<char> inputBuffer, int limit)
        {
            int quantity = 0;
            for (int indexChar = 0; indexChar < inputBuffer.Length; ++indexChar)
            {
                if ((uint)(inputBuffer[indexChar] - '0') > 9u) continue;
                if (++quantity >= limit)
                    return quantity;
            }
            return quantity;
        }

        private const uint NonVisualMaskChar =
            (1u << (int)UnicodeCategory.SpaceSeparator) |
            (1u << (int)UnicodeCategory.LineSeparator) |
            (1u << (int)UnicodeCategory.ParagraphSeparator) |
            (1u << (int)UnicodeCategory.Control) |
            (1u << (int)UnicodeCategory.Format) |
            (1u << (int)UnicodeCategory.Surrogate) |
            (1u << (int)UnicodeCategory.PrivateUse) |
            (1u << (int)UnicodeCategory.OtherNotAssigned);

        internal static bool IsFullEmptyString(ReadOnlySpan<char> inputBuffer)
        {
            if (inputBuffer.Length == 0) return true;
            for (int index = 0; index < inputBuffer.Length; ++index)
            {
                char c = inputBuffer[index];
                if (c <= ' ' || c == sbyte.MaxValue) continue;
                return c > sbyte.MaxValue && HasOnlyNonVisualSlower(inputBuffer[index..]);
            }
            return true;
        }
        private static bool HasOnlyNonVisualSlower(ReadOnlySpan<char> inputBuffer)
        {
            for (int charIndex = 0; charIndex < inputBuffer.Length; ++charIndex)
            {
                char @char = inputBuffer[charIndex];
                if (@char <= 127)
                {
                    if (@char <= ' ' || @char == 127) continue;
                    return false;
                }
                UnicodeCategory category = char.IsHighSurrogate(@char) 
                                           && charIndex + 1 < inputBuffer.Length 
                                           && char.IsLowSurrogate(inputBuffer[charIndex + 1])
                    ? CharUnicodeInfo.GetUnicodeCategory(new string(inputBuffer.Slice(charIndex++, 2)), 0)
                    : CharUnicodeInfo.GetUnicodeCategory(@char);
                if ((NonVisualMaskChar & (1u << (int)category)) == 0) return false;
            }
            return true;
        }

        private static bool IsInvalidCharToDatabase(char c) => c switch
        {
            '\r' or '\n' or '\t' or '\f' or '\v' or ';' or '<' or '>'
                or '“' or '”' or '«' or '»' or '`' or '´' 
                or '\'' or '\\' or '\0' or '&' or '\xa0' => true,
            _ => false
        };

        internal static int CleanStringInMemory(ReadOnlySpan<char> inputSourceBuffer, Span<char> destinationBuffer)
        {
            if (inputSourceBuffer.IsEmpty || destinationBuffer.IsEmpty) return 0;
            int writeIndex = 0;
            bool lastWasSpace = false;
            for (int indexChar = 0; indexChar < inputSourceBuffer.Length; ++indexChar)
            {
                char character = inputSourceBuffer[indexChar];
                if (IsInvalidCharToDatabase(character)) continue;
                if (char.IsWhiteSpace(character))
                {
                    if (lastWasSpace || writeIndex == 0) continue;
                    if (writeIndex >= destinationBuffer.Length) break;
                    destinationBuffer[writeIndex++] = ' ';
                    lastWasSpace = true;
                }
                else
                {
                    if (writeIndex >= destinationBuffer.Length) break;
                    destinationBuffer[writeIndex++] = character;
                    lastWasSpace = false;
                }
            }

            while (writeIndex > 0 && destinationBuffer[writeIndex - 1] == ' ')
                writeIndex--;

            return writeIndex;
        }
        
        
        
    }
}