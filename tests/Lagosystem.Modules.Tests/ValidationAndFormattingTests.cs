using Lagosystem.Modules.Formatting;
using Lagosystem.Modules.Validations;

namespace Lagosystem.Modules.Tests;

public sealed class ValidationAndFormattingTests
{
    [Theory]
    [InlineData("52998224725", true)]
    [InlineData("529.982.247-25", true)]
    [InlineData("52998224724", false)]
    [InlineData("529982247250", false)]
    [InlineData("", false)]
    public void CpfChecksDigitsAndLength(string input, bool expected)
    {
        Assert.Equal(expected, Validate.IsCpf(input));
    }

    [Theory]
    [InlineData('0')]
    [InlineData('1')]
    [InlineData('2')]
    [InlineData('3')]
    [InlineData('4')]
    [InlineData('5')]
    [InlineData('6')]
    [InlineData('7')]
    [InlineData('8')]
    [InlineData('9')]
    public void CpfRejectsRepeatedDigits(char digit)
    {
        Assert.False(Validate.IsCpf(new string(digit, 11)));
    }

    // Vetor alfanumérico publicado pelo Serpro:
    // https://www.serpro.gov.br/menu/noticias/videos/calculodvcnpjalfanaumerico.pdf
    [Theory]
    [InlineData("11222333000181", true)]
    [InlineData("11.222.333/0001-81", true)]
    [InlineData("12ABC34501DE35", true)]
    [InlineData("12.abc.345/01de-35", true)]
    [InlineData("11222333000180", false)]
    [InlineData("12ABC34501DE36", false)]
    [InlineData("12ABC34501DE3A", false)]
    [InlineData("12ÁBC34501DE35", false)]
    [InlineData("00000000000000", false)]
    [InlineData("112223330001810", false)]
    [InlineData("", false)]
    public void CnpjSupportsNumericAndAlphanumericCheckDigits(string input, bool expected)
    {
        Assert.Equal(expected, Validate.IsCnpj(input));
    }

    [Theory]
    [InlineData("96385074", true)]
    [InlineData("036000291452", true)]
    [InlineData("4006381333931", true)]
    [InlineData("10012345000017", true)]
    [InlineData("4006381333932", false)]
    [InlineData("123", false)]
    [InlineData("", false)]
    public void FiscalGtinChecksSupportedLengthsAndCheckDigit(string input, bool expected)
    {
        Assert.Equal(expected, Validate.IsValidGtin(input, isFiscal: true));
    }

    [Fact]
    public void DocumentMasksPreserveIdentifiers()
    {
        Assert.Equal("529.982.247-25", Format.FormatCpf("52998224725"));
        Assert.Equal("11.222.333/0001-81", Format.FormatCnpj("11222333000181"));
        Assert.Equal("01310-100", Format.FormatZipCode("01310100"));
        Assert.Equal("01310-100", Format.FormatZipCode(1310100L));
        Assert.Equal(string.Empty, Format.FormatCpf("00000000000"));
        Assert.Equal(string.Empty, Format.FormatZipCode("013101000"));
    }

    [Theory]
    [InlineData("789-123", false, "789123")]
    [InlineData("789-123", true, "00000000789123")]
    [InlineData("123456789012345", false, "")]
    [InlineData("abc", true, "")]
    public void EanFormattingDoesNotTruncateOverflow(string input, bool leftZero, string expected)
    {
        Assert.Equal(expected, Format.FormatEanCode(input, leftZero));
    }

    [Fact]
    public void FiscalCodesArePaddedAndRejectOverflow()
    {
        Assert.Equal("00123456", Format.FormatNcmCode("123.456"));
        Assert.Equal("12345678", Format.FormatNcmCode("12345678"));
        Assert.Equal(string.Empty, Format.FormatNcmCode("123456789"));
        Assert.Equal(string.Empty, Format.FormatNcmCode("00000000"));
        Assert.Equal("0012345", Format.FormatCestCode("12345"));
        Assert.Equal(string.Empty, Format.FormatCestCode("12345678"));
    }

    [Theory]
    [InlineData("0", true)]
    [InlineData(" -12,50 ", true)]
    [InlineData("12.50", true)]
    [InlineData("0xFF", true)]
    [InlineData("$FF", true)]
    [InlineData("&hA1", true)]
    [InlineData("FFh", true)]
    [InlineData("1,2.3", false)]
    [InlineData("12.", false)]
    [InlineData("-", false)]
    [InlineData("0x", false)]
    [InlineData("١٢", false)]
    [InlineData(null, false)]
    public void NumericValidationHandlesDecimalAndHexSyntax(string? input, bool expected)
    {
        Assert.Equal(expected, Validate.IsNumeric(input));
    }

    [Theory]
    [InlineData("usuario@example.com", true)]
    [InlineData("usuario+tag@gmail.com", true)]
    [InlineData("a@@example.com", false)]
    [InlineData("a..b@example.com", false)]
    [InlineData("a@-example.com", false)]
    [InlineData("a@example", false)]
    [InlineData("a b@example.com", false)]
    [InlineData(null, false)]
    public void EmailValidationRejectsMalformedAddresses(string? input, bool expected)
    {
        Assert.Equal(expected, Validate.IsValidEmail(input));
    }

    [Theory]
    [InlineData("kitten", "sitting", 0.5714285714285714)]
    [InlineData("ABC", "abc", 1.0)]
    [InlineData("", "", 1.0)]
    [InlineData("abc", "", 0.0)]
    public void LevenshteinHasKnownNormalizedDistances(string first, string second, double expected)
    {
        Assert.Equal(expected, Validate.CalculateStringSimilarity(first, second), 12);
    }

    [Fact]
    public void JaroWinklerMatchesPublishedExample()
    {
        Assert.Equal(0.9611111111111111,
            Validate.CalculateStringSimilarity("MARTHA", "MARHTA", StringSimilarityAlgorithm.JaroWinkler), 12);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Validate.CalculateStringSimilarity("a", "b", (StringSimilarityAlgorithm)999));
    }
}
