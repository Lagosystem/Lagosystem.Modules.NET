

namespace Lagosystem.Modules.Extensions;

using System.Diagnostics.CodeAnalysis;
using Lagosystem.Modules.Memory;
using sdc = System.Diagnostics.Contracts;
using jba = JetBrains.Annotations;

using System.Buffers;
using Lagosystem.Modules.Helpers;
using System.Runtime.CompilerServices;
using System.Globalization;
using System.Text;

/// <summary>Extension methods for working with collection types. </summary>
public static class StringsExtensions
{
    /// <summary>Extracts only numeric characters from the specified string.</summary>
    /// <param name="source">The input text from which numeric characters will be extracted.</param>
    /// <returns>
    /// A new string composed only of the numeric characters found in
    /// <paramref name="source"/>. If the input is null, empty or contains
    /// only whitespace, <see cref="string.Empty"/> is returned.
    /// </returns>
    public static string OnlyNumbers([NotNullWhen(false)] this string? source)
    {
        if (source.IsBlank()) return string.Empty;
        char[]? rented = null;
        int safeBufferSize = Settings.DefaultSpanBufferSize;
        if (source.Length > safeBufferSize) rented = ArrayPool<char>.Shared.Rent(source.Length);
        try
        {
            Span<char> buffer = source.Length <= safeBufferSize ? stackalloc char[source.Length] : rented;
            int charsWritten = 0;
            for (int indexChar = 0; indexChar < source.Length; ++indexChar)
            {
                char ch = source[indexChar];
                if ((uint)(ch - '0') <= 9u) buffer[charsWritten++] = ch;
            }

            string result = charsWritten == 0 ? string.Empty : new string(buffer[..charsWritten]);
            return result;
        }
        finally { if (rented is not null) ArrayPool<char>.Shared.Return(rented); }
    }

    /// <summary>
    /// Determines whether a string is null, empty, consists only of whitespace,  or has no visually meaningful
    /// characters according to the library's Unicode classification.
    /// </summary>
    /// <param name="source">The text to inspect, including a possible null value.</param>
    /// <returns>
    /// <see langword="true"/> when the input has no visible content; otherwise <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// Valid visible characters in emoji sequences keep the input nonblank.
    /// Isolated controls, invisible formatting characters and malformed UTF-16 surrogate code units do not count as visible content.
    /// The Databases translation registered in the API is an approximation: SQL does not replicate the .NET Unicode classification exactly.
    /// </remarks>
    [sdc.Pure]
    [jba.Pure]
    [jba.ContractAnnotation("null=>true", true)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsBlank([NotNullWhen(false)] this string? source) => source is null 
        || string.IsNullOrEmpty(source) || string.IsNullOrWhiteSpace(source) 
        || FormatAndValidationHelper.IsVisuallyBlank(source.AsSpan());

    /// <summary>
    /// Cleans an arbitrary text by removing unwanted control characters (except CR, LF and TAB),
    /// stripping BOM/zero-width characters, normalizing different whitespace characters to a single
    /// space and collapsing consecutive spaces. The result is then trimmed at the start and end.
    /// </summary>
    /// <param name="source">The original text to clean.</param>
    /// <param name="asUpperCase">Indicate if <paramref name="source"/> must return as UPPER CASE.</param>
    /// <returns>
    /// A normalized string with control and invisible characters removed, or
    /// <see cref="string.Empty"/> if the input is null or contains only whitespace.
    /// </returns>
    public static string Clean(this string? source, bool asUpperCase = true)
    {
        if (source is null || source.Length == 0) return string.Empty;
        int sourceLength = source.Length;
        // DefaultSpanBufferSize está definido em bytes.
        int stackCharacterLimit = Settings.DefaultSpanBufferSize / sizeof(char);
        char[]? rentedBuffer = null;
        if (sourceLength > stackCharacterLimit) rentedBuffer = ArrayPool<char>.Shared.Rent(sourceLength);
        Span<char> destination = (sourceLength <= stackCharacterLimit) ? stackalloc char[sourceLength] : rentedBuffer.AsSpan(0, sourceLength);
        try
        {
            int written = FormatAndValidationHelper.CleanString(source.AsSpan(), destination, asUpperCase, out bool changed);
            if (written == 0) return string.Empty;
            return !changed ? source : new string(destination[..written]);
        }
        finally { if (rentedBuffer is not null) ArrayPool<char>.Shared.Return(rentedBuffer, clearArray: false); }
    }

    /// <summary>
    /// Cleans the input string using <see cref="Clean(string, bool)"/> and normalizes it to a single line, removing
    /// carriage returns and line feeds, and optionally removing spaces and TAB characters.
    /// </summary>
    /// <param name="source">The original string to normalize.</param>
    /// <param name="asUpperCase">Indicates the <paramref name="source"/> must return in UPPER CASE or normal case.</param>
    /// <param name="noSpaces">
    /// When <see langword="true"/>, all space (" ") and TAB ("\t") characters are removed from
    /// the cleaned string. When <see langword="false"/>, spaces and TABs are preserved.
    /// </param>
    /// <returns>
    /// A single-line cleaned string, or <see cref="string.Empty"/> if the input is null, empty or
    /// becomes empty after cleaning.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string CleanToSingleLine(this string source, bool asUpperCase = false, bool noSpaces = false)
    {
        if (source.IsBlank())return string.Empty;
        int sourceLength = source.Length;
        char[]? rented = null;
        try
        {
            int safeBufferSize = Settings.DefaultSpanBufferSize;
            if (sourceLength > safeBufferSize) rented = ArrayPool<char>.Shared.Rent(sourceLength);
            Span<char> buffer = sourceLength <= safeBufferSize ? stackalloc char[sourceLength] : rented;
            int written = CleanToSingleLineCore(source, buffer, asUpperCase, noSpaces);
            string result = written == 0 ? string.Empty : new string(buffer[..written]);
            return result;
        }
        finally { if(rented is not null) ArrayPool<char>.Shared.Return(rented); }
    }
        
    private static int CleanToSingleLineCore(ReadOnlySpan<char> source, Span<char> destination, bool asUpperCase, bool noSpaces)
    {
        int written = 0;
        bool lastWasSpace = false;

        for (int indexChar = 0; indexChar < source.Length; indexChar++)
        {
            char ch = source[indexChar];
            if (ch is '\r' or '\n') continue;
            if (noSpaces && ch is ' ' or '\t') continue;
            if (IsForbiddenControl(ch)) continue;
            if (IsInvisibleJunk(ch)) continue;
            if (IsSpaceLike(ch))
            {
                if (noSpaces) continue;
                if (lastWasSpace) continue;
                destination[written++] = ' ';
                lastWasSpace = true;
                continue;
            }
            ch = asUpperCase ? char.ToUpperInvariant(ch) : ch;
            destination[written++] = ch;
            lastWasSpace = false;
        }

        return written;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsForbiddenControl(char ch) => char.IsControl(ch) && ch is not '\r' and not '\n' and not '\t';

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsInvisibleJunk(char ch) => ch is '\uFEFF' or '\u200B' or '\u200C' or '\u200D';

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsSpaceLike(char ch) => ch is ' ' or '\u00A0' or >= '\u2000' and <= '\u200A' or '\u202F' or '\u205F';

    /// <summary>
    /// Removes diacritical marks (accents) from the specified string, producing an ASCII-friendly
    /// representation while preserving the original characters as much as possible.
    /// </summary>
    /// <param name="source">
    /// The input text from which accents and non-spacing marks will be removed.
    /// </param>
    /// <returns>
    /// A cleaned, accent-free version of <paramref name="source"/>. If the input is null,
    /// empty or contains only whitespace, <see cref="string.Empty"/> is returned.
    /// </returns>
    /// <remarks>
    /// This method first calls <c>Clean()</c> to remove control and invisible characters,
    /// then uses Unicode normalization (FormD) and filters out characters classified as
    /// <see cref="UnicodeCategory.NonSpacingMark"/>, finally re-normalizing
    /// the result using FormC to produce a stable string.
    /// </remarks>
    public static string NoAccentsFast(this string source)
    {
        if (source.IsBlank()) return string.Empty;
        // 1. Normalize(FormD) is the only unwanted allocation, but necessary for the Unicode standard.
        string normalized = source.Normalize(NormalizationForm.FormD);
        int length = normalized.Length;
        char[]? pooledArray = null;
        // 2. Allocates on the Stack if small (safe limit ~512 chars / 1KB).
        // If it's a huge string (e.g., entire book text), uses the ArrayPool.
        int safeBufferSize = Settings.DefaultSpanBufferSize;
        try
        {
            if (length > safeBufferSize) pooledArray = ArrayPool<char>.Shared.Rent(length);
            Span<char> buffer = length <= safeBufferSize ? stackalloc char[length] : pooledArray;
            int charsWritten = 0;
            ReadOnlySpan<char> span = normalized.AsSpan();
            for (int i = 0; i < span.Length; i++)
            {
                char ch = span[i];
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                    buffer[charsWritten++] = ch;
            }

            string result = new string(buffer[..charsWritten]);
            return result;
        }
        finally { if (pooledArray != null) ArrayPool<char>.Shared.Return(pooledArray); }
    }

    /// <summary>Left-pads the specified string with zero characters ('0') until it reaches the desired total length, similar to a numeric zero-fill.</summary>
    /// <param name="source">The original string to be padded with leading zeros.</param>
    /// <param name="totalLength">The desired total length of the resulting string. If this value is less than or equal to zero, the original string is returned unchanged.</param>
    /// <returns>
    /// A new string padded on the left with zeros so that its length equals
    /// <paramref name="totalLength"/>. If the input is null, empty or contains only
    /// whitespace, <see cref="string.Empty"/> is returned. If the original length is
    /// greater than or equal to <paramref name="totalLength"/>, the original string
    /// is returned unchanged.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ZFill(this string source, int totalLength)
    {
        if (source.IsBlank()) return string.Empty;
    
        int sourceLength = source.Length;
        if (sourceLength >= totalLength) return source;
        int zeroCount = totalLength - sourceLength;
        short maxSizeBuffer = Settings.DefaultSpanBufferSize;

        char[]? rented = null;

        try
        {
            if (totalLength > maxSizeBuffer) rented = ArrayPool<char>.Shared.Rent(totalLength);
            Span<char> buffer = totalLength <= maxSizeBuffer ? stackalloc char[totalLength] : rented;
            buffer[..zeroCount].Fill('0');
            source.AsSpan().CopyTo(buffer.Slice(zeroCount, sourceLength));
            return new string(buffer[..totalLength]);
        }
        finally { if (rented is not null) ArrayPool<char>.Shared.Return(rented); }
    }
    
    /// <summary>
    /// Returns a safe substring from a <see cref="string"/>, without throwing exceptions,
    /// even when the provided parameters fall outside the expected bounds.
    /// </summary>
    /// <param name="str">
    /// The source string from which the desired segment will be extracted.
    /// If <see langword="null"/> or empty, <see cref="string.Empty"/> is returned.
    /// </param>
    /// <param name="startIndex">
    /// The starting index for the extraction.
    /// If less than zero, it is automatically adjusted to <c>0</c>.
    /// If greater than the string's total length, it adjusts safely without failing.
    /// </param>
    /// <param name="length">
    /// The desired number of characters.
    /// If less than or equal to zero, <see cref="string.Empty"/> is returned.
    /// If it exceeds the available characters starting from <paramref name="startIndex"/>, it is automatically truncated.
    /// </param>
    /// <returns>
    /// A <see cref="string"/> containing the safely calculated substring.
    /// Returns <see cref="string.Empty"/> when there is no valid content to extract.
    /// </returns>
    /// <remarks>
    /// This method internally utilizes the <see cref="ReadOnlySpan{Char}"/> based overload
    /// to reduce intermediate costs and centralize the safe adjustment logic.
    /// The behavior intentionally differs from the native <c>Substring</c> method,
    /// as the priority here is to suppress exceptions and maintain robustness in critical scenarios.
    /// </remarks>
    /// <example>
    /// Example usage:
    /// <code>
    /// string text = "ABCDE";
    ///
    /// string a = text.SafeSubstring(1, 2);   // "BC"
    /// string b = text.SafeSubstring(10, 3);  // ""
    /// string c = text.SafeSubstring(1, 10);  // "BCDE"
    /// string d = text.SafeSubstring(-2, 2);  // "AB"
    /// string e = ((string?)null).SafeSubstring(0, 2); // ""
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string SafeSubstring(this string? str, int startIndex, int length) => 
        str.IsBlank() ? string.Empty : new string(SafeSubstring(str.AsSpan(), startIndex, length, false));
        
    /// <summary>
    /// Trims leading and trailing spaces from a <see cref="string"/> and then returns a safe substring,
    /// without throwing exceptions, even when parameters fall outside the expected bounds.
    /// </summary>
    /// <param name="str">
    /// The source string to be trimmed and sliced.
    /// If <see langword="null"/> or empty, <see cref="string.Empty"/> is returned.
    /// </param>
    /// <param name="startIndex">
    /// The starting index for the extraction (applied after trimming).
    /// If less than zero, it is automatically adjusted to <c>0</c>.
    /// </param>
    /// <param name="length">
    /// The desired number of characters.
    /// If less than or equal to zero, <see cref="string.Empty"/> is returned.
    /// If it exceeds the available characters starting from <paramref name="startIndex"/>, it is automatically truncated.
    /// </param>
    /// <returns>
    /// A <see cref="string"/> containing the safely calculated and trimmed substring.
    /// Returns <see cref="string.Empty"/> when there is no valid content to extract.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string TrimAndSafeSubstring(this string? str, int startIndex, int length) => 
        str.IsBlank() ? string.Empty : new string(SafeSubstring(str.AsSpan(), startIndex, length,true));

    /// <summary>
    /// Returns a safe slice from a <see cref="ReadOnlySpan{Char}"/>, without throwing exceptions,
    /// even when the provided parameters fall outside the expected bounds.
    /// </summary>
    /// <param name="span">
    /// The source span from which the desired segment will be extracted.
    /// </param>
    /// <param name="startIndex">
    /// The starting index for the extraction.
    /// If less than zero, it is automatically adjusted to <c>0</c>.
    /// If it exceeds the size of the <paramref name="span"/>, it safely adjusts to an empty representation.
    /// </param>
    /// <param name="length">
    /// The desired number of characters.
    /// If less than or equal to zero, <see cref="ReadOnlySpan{Char}.Empty"/> is returned.
    /// If it exceeds the available characters starting from <paramref name="startIndex"/>, it is automatically truncated.
    /// </param>
    /// <param name="trimSpaces">Indicates whether to trim spaces from the span before computing the slice.</param>
    /// <returns>
    /// A <see cref="ReadOnlySpan{Char}"/> containing the safely calculated slice.
    /// Returns <see cref="ReadOnlySpan{Char}.Empty"/> when there is no valid content to extract.
    /// </returns>
    /// <remarks>
    /// This method does not precisely replicate the native <c>Slice</c> or <c>Substring</c> behavior.
    /// It is designed for high-fault-tolerance scenarios, avoiding exceptions
    /// and applying automatic adjustment rules for invalid indices and lengths.
    /// </remarks>
    /// <example>
    /// Example usage:
    /// <code>
    /// ReadOnlySpan&lt;char&gt; text = "ABCDE".AsSpan();
    /// 
    /// ReadOnlySpan&lt;char&gt; a = text.SafeSubstring(1, 2, false);   // "BC"
    /// ReadOnlySpan&lt;char&gt; b = text.SafeSubstring(10, 3, false);  // ""
    /// ReadOnlySpan&lt;char&gt; c = text.SafeSubstring(1, 10, false);  // "BCDE"
    /// ReadOnlySpan&lt;char&gt; d = text.SafeSubstring(-2, 2, false);  // "AB"
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]    
    public static ReadOnlySpan<char> SafeSubstring(this ReadOnlySpan<char> span, int startIndex, int length, bool trimSpaces)
    {
        if (span.IsEmpty || length <= 0)
            return ReadOnlySpan<char>.Empty;
        span = trimSpaces ? span.Trim() : span;
        int start = Math.Max(0, Math.Min(startIndex, span.Length));
        int take = Math.Min(length, span.Length - start);
        return take <= 0 ? ReadOnlySpan<char>.Empty : span.Slice(start, take);
    }
}

