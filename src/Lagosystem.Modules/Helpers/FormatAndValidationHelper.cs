
namespace Lagosystem.Modules.Helpers;

using Lagosystem.Modules.Formatting;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

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
        return startIndexChar > endIndexChar
            ? ReadOnlySpan<char>.Empty
            : inputBuffer[startIndexChar..(endIndexChar + 1)];
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

    internal static bool IsFullEmptyString0(ReadOnlySpan<char> inputBuffer)
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

    internal static int CleanStringInMemory0(ReadOnlySpan<char> inputSourceBuffer, Span<char> destinationBuffer)
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

        while (writeIndex > 0 && destinationBuffer[writeIndex - 1] == ' ') writeIndex--;

        return writeIndex;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetScalarValue(char highSurrogate, char lowSurrogate) =>
        0x10000 + ((highSurrogate - '\uD800') << 10) + (lowSurrogate - '\uDC00');

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteSurrogatePair(ReadOnlySpan<char> source, Span<char> destination, int readIndex, bool asUpperCase, ref int writeIndex, ref bool changed)
    {
        char highSurrogate = source[readIndex];
        char lowSurrogate = source[readIndex + 1];

        if (!asUpperCase)
        {
            WriteCharacter(source, destination, highSurrogate, ref writeIndex, ref changed);
            WriteCharacter(source, destination, lowSurrogate, ref writeIndex, ref changed);
            return;
        }

        Span<char> uppercaseDestination = destination.Slice(writeIndex, 2);

        int charactersWritten = source.Slice(readIndex, 2).ToUpperInvariant(uppercaseDestination);
        
        if (charactersWritten != 2)
        {
            WriteCharacter(source, destination, highSurrogate, ref writeIndex, ref changed);
            WriteCharacter(source, destination, lowSurrogate, ref writeIndex, ref changed);
            return;
        }

        char normalizedHighSurrogate = uppercaseDestination[0];
        char normalizedLowSurrogate = uppercaseDestination[1];

        WriteCharacter(source, destination, normalizedHighSurrogate, ref writeIndex, ref changed);
        WriteCharacter(source, destination, normalizedLowSurrogate, ref writeIndex, ref changed);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteCharacter(ReadOnlySpan<char> source, Span<char> destination, char character, ref int writeIndex, ref bool changed)
    { 
        if (!changed && (writeIndex >= source.Length || source[writeIndex] != character)) changed = true;
        destination[writeIndex++] = character;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ReadUnicodeScalar(string value, int index, out int scalar, out int charsConsumed, out UnicodeCategory category)
    {
        char first = value[index];

        if (char.IsHighSurrogate(first) && index + 1 < value.Length && char.IsLowSurrogate(value[index + 1]))
        {
            char second = value[index + 1];
            charsConsumed = 2;
            scalar = 0x10000 + ((first - '\uD800') << 10) + (second - '\uDC00');
            category = CharUnicodeInfo.GetUnicodeCategory(value, index);
            return;
        }

        charsConsumed = 1;
        scalar = first;
        category = CharUnicodeInfo.GetUnicodeCategory(first);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsVariationSelector(int scalarValue) => 
        (uint)(scalarValue - 0xFE00) <= 0x0Fu || (uint)(scalarValue - 0xE0100) <= 0xEFu;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsAsciiWhitespace(char character) => character is ' ' or '\t' or '\v' or '\f' or '\r' or '\n';
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsWhitespaceCategory(
        UnicodeCategory category) =>
        category is UnicodeCategory.SpaceSeparator or UnicodeCategory.LineSeparator or UnicodeCategory.ParagraphSeparator;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsRemovableInvisibleCharacter(int scalarValue) =>
        // ZERO WIDTH NO-BREAK SPACE/BOM e ZERO WIDTH SPACE.
        // Não remover U+200C ou U+200D: são importantes para idiomas e composição de emojis.
        scalarValue is 0xFEFF or 0x200B;
    
    
    private const uint NonVisualCategoryMask =
        (1u << (int)UnicodeCategory.SpaceSeparator) |
        (1u << (int)UnicodeCategory.LineSeparator) |
        (1u << (int)UnicodeCategory.ParagraphSeparator) |
        (1u << (int)UnicodeCategory.Control) |
        (1u << (int)UnicodeCategory.Format) |
        (1u << (int)UnicodeCategory.Surrogate);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsNonVisualCategory(UnicodeCategory category) => (NonVisualCategoryMask & (1u << (int)category)) != 0;

    internal static int CleanString(ReadOnlySpan<char> source, Span<char> destination, bool asUpperCase, out bool changed)
    {
        int writeIndex = 0;
        bool hasVisibleCharacter = false;
        bool pendingSpace = false;

        changed = false;

        for (int readIndex = 0; readIndex < source.Length; readIndex++)
        {
            char character = source[readIndex];

            // Fast path ASCII.
            if (character <= '\u007F')
            {
                switch (character)
                {
                    case <= ' ':
                        if (IsAsciiWhitespace(character) && hasVisibleCharacter) pendingSpace = true;
                        continue;
                    case '\u007F': continue;// ASCII DEL.
                }

                if (pendingSpace)
                {
                    WriteCharacter(source, destination, ' ', ref writeIndex, ref changed);
                    pendingSpace = false;
                }

                char outputCharacter = asUpperCase && character is >= 'a' and <= 'z' ? (char)(character - ('a' - 'A')) : character;
                WriteCharacter(source, destination, outputCharacter, ref writeIndex, ref changed);
                hasVisibleCharacter = true;
                continue;
            }

            // Surrogate pair válido: emojis e caracteres fora do BMP.
            if (char.IsHighSurrogate(character) && readIndex + 1 < source.Length && char.IsLowSurrogate(source[readIndex + 1]))
            {
                int surrogateStartIndex = readIndex;
                char lowSurrogate = source[++readIndex];
                int scalarValue = GetScalarValue(character, lowSurrogate);
                UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(scalarValue);
                if (IsWhitespaceCategory(category))
                {
                    if (hasVisibleCharacter) pendingSpace = true;
                    continue;
                }

                if (category == UnicodeCategory.Control) continue;
                if (IsRemovableInvisibleCharacter(scalarValue)) continue;

                if (pendingSpace)
                {
                    WriteCharacter(source, destination, ' ', ref writeIndex, ref changed);
                    pendingSpace = false;
                }

                WriteSurrogatePair(source, destination, surrogateStartIndex, asUpperCase, ref writeIndex, ref changed);
                if (!IsNonVisualCategory(category) && !IsVariationSelector(scalarValue)) hasVisibleCharacter = true;

                continue;
            }

            // Remove surrogate isolado ou UTF-16 inválido.
            if (char.IsSurrogate(character)) continue;
            UnicodeCategory unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(character);

            if (char.IsWhiteSpace(character) || IsWhitespaceCategory(unicodeCategory))
            {
                if (hasVisibleCharacter) pendingSpace = true;
                continue;
            }

            if (unicodeCategory == UnicodeCategory.Control) continue;
            if (IsRemovableInvisibleCharacter(character)) continue;

            if (pendingSpace)
            {
                WriteCharacter(source, destination, ' ', ref writeIndex, ref changed);
                pendingSpace = false;
            }

            char normalizedCharacter = asUpperCase ? char.ToUpperInvariant(character) : character;
            WriteCharacter(source, destination, normalizedCharacter, ref writeIndex, ref changed);
            if (!IsNonVisualCategory(unicodeCategory) && !IsVariationSelector(character)) hasVisibleCharacter = true;
        }

        if (!hasVisibleCharacter)
        {
            changed = source.Length != 0;
            return 0;
        }

        // Detecta remoção de lixo ou whitespace no final.
        if (writeIndex != source.Length) changed = true;
        return writeIndex;
    }

    internal static bool IsVisuallyBlank(ReadOnlySpan<char> source)
    {
        for (int index = 0; index < source.Length; index++)
        {
            char character = source[index];

            // Fast path ASCII.
            if (character <= '\u007F')
            {
                if (character is <= ' ' or '\u007F') continue;
                return false;
            }

            // Processa surrogate pair válido sem Rune e sem alocação.
            if (char.IsHighSurrogate(character) && index + 1 < source.Length && char.IsLowSurrogate(source[index + 1]))
            {
                char lowSurrogate = source[++index];

                int scalarValue = GetScalarValue(character, lowSurrogate);

                UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(scalarValue);
                if (!IsNonVisualCategory(category) && !IsVariationSelector(scalarValue)) return false;

                continue;
            }

            // UTF-16 inválido não é considerado conteúdo visual.
            if (char.IsSurrogate(character)) continue;
            UnicodeCategory unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(character);
            if (!IsNonVisualCategory(unicodeCategory) && !IsVariationSelector(character)) return false;
        }

        return true;
    }
    
    internal static string GetNameMethodWhoCalled(Type @class, [CallerMemberName] string callerName = "") => 
        $"global::{@class.FullName}.{callerName}";
}

