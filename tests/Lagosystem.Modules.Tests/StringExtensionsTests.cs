using System.Globalization;
using Lagosystem.Modules.Extensions;

namespace Lagosystem.Modules.Tests;

public sealed class StringExtensionsTests : ModuleTestScope
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("abc", "")]
    [InlineData("(11) 99999-1234", "11999991234")]
    [InlineData("A0١２9", "09")]
    public void OnlyNumbersRetainsAsciiDigits(string? input, string expected)
    {
        Assert.Equal(expected, input.OnlyNumbers());
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData(" \t\r\n", true)]
    [InlineData("\u200B\uFEFF\u200D", true)]
    [InlineData("\uD800", true)]
    [InlineData("\uFE0F", true)]
    [InlineData("0", false)]
    [InlineData("👩‍💻", false)]
    [InlineData("a\u200B", false)]
    public void IsBlankRecognizesInvisibleUnicode(string? input, bool expected)
    {
        Assert.Equal(expected, input.IsBlank());
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData(" \t\r\n", "")]
    [InlineData("  café\t com\r\nleite  ", "CAFÉ COM LEITE")]
    [InlineData("\uFEFFa\u200Bb\0c\u007F", "ABC")]
    [InlineData("a\u00A0b\u2028c", "A B C")]
    [InlineData("a\uD800b\uDC00c", "ABC")]
    [InlineData("👩‍💻 café", "👩‍💻 CAFÉ")]
    public void CleanNormalizesWhitespaceAndPreservesVisibleUnicode(string? input, string expected)
    {
        Assert.Equal(expected, input.Clean());
        Assert.Equal(expected, expected.Clean());
    }

    [Theory]
    [InlineData(127)]
    [InlineData(128)]
    [InlineData(1025)]
    public void CleanAndOnlyNumbersWorkAcrossBufferBoundaries(int length)
    {
        Settings.DefaultSpanBufferSize = 127;
        string input = new string('a', length);

        Assert.Equal(new string('A', length), ("  " + input + "  ").Clean());
        Assert.Equal(new string('1', length), (input + new string('1', length)).OnlyNumbers());
    }

    [Theory]
    [InlineData("pt-BR")]
    [InlineData("tr-TR")]
    public void CleanUsesInvariantUppercase(string cultureName)
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
        Assert.Equal("ISTANBUL", "istanbul".Clean());
        Assert.Equal("café", " café ".Clean(asUpperCase: false));
    }

    [Theory]
    [InlineData("ação", "acao")]
    [InlineData("cafe\u0301", "cafe")]
    [InlineData("👩‍💻", "👩‍💻")]
    public void NoAccentsRemovesCombiningMarks(string input, string expected)
    {
        Assert.Equal(expected, input.NoAccentsFast());
    }

    [Theory]
    [InlineData("abcdef", -2, 3, "abc")]
    [InlineData("abcdef", 2, 99, "cdef")]
    [InlineData("abcdef", 99, 2, "")]
    [InlineData("abcdef", 2, 0, "")]
    [InlineData("abcdef", 2, -1, "")]
    [InlineData("abcdef", 2, int.MaxValue, "cdef")]
    [InlineData(null, 0, 3, "")]
    public void SafeSubstringClampsOutOfBoundsInput(string? input, int start, int length, string expected)
    {
        Assert.Equal(expected, input.SafeSubstring(start, length));
    }

    [Fact]
    public void ZFillPadsWithoutTruncating()
    {
        Assert.Equal("00042", "42".ZFill(5));
        Assert.Equal("12345", "12345".ZFill(3));
        Assert.Equal(new string('0', 1022) + "42", "42".ZFill(1024));
    }
}
