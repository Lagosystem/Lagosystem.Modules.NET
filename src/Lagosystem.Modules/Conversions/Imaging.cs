

namespace Lagosystem.Modules.Conversions;
using Lagosystem.Modules.Helpers;

public static class Imaging
{
    #region Colors
    
    /// <summary>Creates an instance of an opaque color implementing the <see cref="IRgb"/> interface.</summary>
    /// <param name="red">Red channel value (0-255).</param>
    /// <param name="green">Green channel value (0-255).</param>
    /// <param name="blue">Blue channel value (0-255).</param>
    /// <returns>An initialized <see cref="IRgb"/> abstraction holding the RGB components.</returns>
    public static IRgb Rgb(int red, int green, int blue)
    {
        ThrowHelper.ThrowIfNegativeValue(red);
        ThrowHelper.ThrowIfNegativeValue(green);
        ThrowHelper.ThrowIfNegativeValue(blue);

        return new Rgb(Math.Min(red, 255), Math.Min(green, 255), Math.Min(blue, 255));
    }

    /// <summary>Creates an instance of a transparent color implementing the <see cref="IRgb"/> interface.</summary>
    /// <param name="red">Red channel value (0-255).</param>
    /// <param name="green">Green channel value (0-255).</param>
    /// <param name="blue">Blue channel value (0-255).</param>
    /// <param name="alpha">Alpha transparency channel value (0-255).</param>
    /// <returns>An initialized <see cref="IRgb"/> abstraction holding the RGBA components.</returns>
    public static IArgb Argb(int red, int green, int blue, int alpha)
    {
        ThrowHelper.ThrowIfNegativeValue(red);
        ThrowHelper.ThrowIfNegativeValue(green);
        ThrowHelper.ThrowIfNegativeValue(blue);
        ThrowHelper.ThrowIfNegativeValue(alpha);
        return new Argb(Math.Min(red, 255), Math.Min(green, 255), Math.Min(blue, 255), Math.Min(alpha, 255));
    }
    
    /// <summary>
    /// Converts individual RGB (Red, Green, Blue) color components into a single packed 32-bit integer.
    /// Values above 255 are automatically clamped to 255.
    /// </summary>
    /// <param name="red">The red color component. Must be a non-negative value (0 to 255+).</param>
    /// <param name="green">The green color component. Must be a non-negative value (0 to 255+).</param>
    /// <param name="blue">The blue color component. Must be a non-negative value (0 to 255+).</param>
    /// <returns>
    /// A 32-bit signed integer representing the packed color in BGR format, 
    /// where Blue occupies the high byte (0x10000), Green the middle byte (0x100), and Red the low byte.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when any of the color components is a negative number.</exception>
    public static int FullRgb(int red, int green, int blue)
    {
        ThrowHelper.ThrowIfNegativeValue(red);
        ThrowHelper.ThrowIfNegativeValue(green);
        ThrowHelper.ThrowIfNegativeValue(blue);

        red = Math.Min(red, 255);
        green = Math.Min(green, 255);
        blue = Math.Min(blue, 255);

        return (blue << 16) | (green << 8) | red;
    }
    
    /// <summary>Converts standard ARGB components into a single packed 32-bit ARGB integer format (0xAARRGGBB).</summary>
    /// <remarks>
    /// This function shifts and merges individual 8-bit color channels into a single signed 32-bit block, 
    /// placing the Alpha component in the highest byte for universal graphic buffer compatibility.
    /// </remarks>
    /// <param name="red">Red channel value (0-255).</param>
    /// <param name="green">Green channel value (0-255).</param>
    /// <param name="blue">Blue channel value (0-255).</param>
    /// <param name="alpha">Alpha transparency channel value (0-255).</param>
    /// <exception cref="ArgumentException">Thrown when any of the color components is a negative number.</exception>
    /// <returns>A packed 32-bit signed integer representing the integrated ARGB color format.</returns>
    public static int FullArgb(int red, int green, int blue, int alpha)
    {
        ThrowHelper.ThrowIfNegativeValue(red);
        ThrowHelper.ThrowIfNegativeValue(green);
        ThrowHelper.ThrowIfNegativeValue(blue);
        ThrowHelper.ThrowIfNegativeValue(alpha);

        red = Math.Min(red, 255);
        green = Math.Min(green, 255);
        blue = Math.Min(blue, 255);
        alpha = Math.Min(alpha, 255);

        return (alpha << 24) | (red << 16) | (green << 8) | blue;
    }
    
    /// <summary>Converts CMYK subtractive color percentages into a read-only <see cref="Rgb"/> structure.</summary>
    /// <param name="cyan">Cyan channel percentage (0-100).</param>
    /// <param name="magenta">Magenta channel percentage (0-100).</param>
    /// <param name="yellow">Yellow channel percentage (0-100).</param>
    /// <param name="black">Black channel percentage (0-100).</param>
    /// <returns>An <see cref="Rgb"/> struct mapping the visual additive coordinates.</returns>
    public static IRgb CmykToRgb(int cyan, int magenta, int yellow, int black)
    {
        ThrowHelper.ThrowIfNegativeValue(cyan);
        ThrowHelper.ThrowIfNegativeValue(magenta);
        ThrowHelper.ThrowIfNegativeValue(yellow);
        ThrowHelper.ThrowIfNegativeValue(black);

        cyan = Math.Min(cyan, 100);
        magenta = Math.Min(magenta, 100);
        yellow = Math.Min(yellow, 100);
        black = Math.Min(black, 100);

        int red = (int)Math.Round(255 * (1 - cyan / 100.0) * (1 - black / 100.0));
        int green = (int)Math.Round(255 * (1 - magenta / 100.0) * (1 - black / 100.0));
        int blue = (int)Math.Round(255 * (1 - yellow / 100.0) * (1 - black / 100.0));

        return new Rgb(red, green, blue);
    }
    
    /// <summary>
    /// Converts CMYK (Cyan, Magenta, Yellow, Black) percentage values into a packed RGB/BGR integer.
    /// Values above 100% are automatically clamped to 100%.
    /// </summary>
    /// <param name="cyan">The Cyan component percentage. Must be a non-negative value (0 to 100+).</param>
    /// <param name="magenta">The Magenta component percentage. Must be a non-negative value (0 to 100+).</param>
    /// <param name="yellow">The Yellow component percentage. Must be a non-negative value (0 to 100+).</param>
    /// <param name="black">The Black (Key) component percentage. Must be a non-negative value (0 to 100+).</param>
    /// <returns>A 32-bit signed integer representing the calculated RGB color packed in BGR format.</returns>
    /// <exception cref="ArgumentException">Thrown when any of the CMYK components is a negative number.</exception>
    public static int CmykToFullRgb(int cyan, int magenta, int yellow, int black)
    {
        ThrowHelper.ThrowIfNegativeValue(cyan);
        ThrowHelper.ThrowIfNegativeValue(magenta);
        ThrowHelper.ThrowIfNegativeValue(yellow);
        ThrowHelper.ThrowIfNegativeValue(black);

        cyan = Math.Min(cyan, 100);
        magenta = Math.Min(magenta, 100);
        yellow = Math.Min(yellow, 100);
        black = Math.Min(black, 100);

        int red = (int)Math.Round(255 * (1 - cyan / 100.0) * (1 - black / 100.0));
        int green = (int)Math.Round(255 * (1 - magenta / 100.0) * (1 - black / 100.0));
        int blue = (int)Math.Round(255 * (1 - yellow / 100.0) * (1 - black / 100.0));

        return (blue << 16) | (green << 8) | red;
    }
    
    /// <summary>Converts CMYK components into an <see cref="Conversions.Argb"/> struct with an optional alpha transparency channel.</summary>
    /// <param name="cyan">Cyan channel percentage (0-100).</param>
    /// <param name="magenta">Magenta channel percentage (0-100).</param>
    /// <param name="yellow">Yellow channel percentage (0-100).</param>
    /// <param name="black">Black channel percentage (0-100).</param>
    /// <param name="alpha">Optional alpha transparency channel value (0-255). If null, defaults to 255 (opaque).</param>
    /// <returns>An <see cref="Conversions.Argb"/> struct mapping the resulting RGB coordinates.</returns>
    public static IArgb CmykToArgb(int cyan, int magenta, int yellow, int black, int alpha)
    {
        ThrowHelper.ThrowIfNegativeValue(cyan);
        ThrowHelper.ThrowIfNegativeValue(magenta);
        ThrowHelper.ThrowIfNegativeValue(yellow);
        ThrowHelper.ThrowIfNegativeValue(black);
        ThrowHelper.ThrowIfNegativeValue(alpha);

        cyan = Math.Min(cyan, 100);
        magenta = Math.Min(magenta, 100);
        yellow = Math.Min(yellow, 100);
        black = Math.Min(black, 100);
        int a = Math.Min(alpha, 255);

        int red = (int)Math.Round(255 * (1 - cyan / 100.0) * (1 - black / 100.0));
        int green = (int)Math.Round(255 * (1 - magenta / 100.0) * (1 - black / 100.0));
        int blue = (int)Math.Round(255 * (1 - yellow / 100.0) * (1 - black / 100.0));

        return new Argb(red, green, blue, a);
    }
    
    /// <summary>Converts CMYK components into an <see cref="Conversions.Argb"/> struct with an optional alpha transparency channel.</summary>
    /// <param name="cyan">Cyan channel percentage (0-100).</param>
    /// <param name="magenta">Magenta channel percentage (0-100).</param>
    /// <param name="yellow">Yellow channel percentage (0-100).</param>
    /// <param name="black">Black channel percentage (0-100).</param>
    /// <returns>An <see cref="Conversions.Argb"/> struct mapping the resulting RGB coordinates.</returns>
    public static IArgb CmykToArgb(int cyan, int magenta, int yellow, int black) =>
        CmykToArgb(cyan, magenta, yellow, black, 255);
    
    /// <summary>Converts CMYK components directly into a single packed 32-bit ARGB integer (0xAARRGGBB).</summary>
    /// <param name="cyan">Cyan channel percentage (0-100).</param>
    /// <param name="magenta">Magenta channel percentage (0-100).</param>
    /// <param name="yellow">Yellow channel percentage (0-100).</param>
    /// <param name="black">Black channel percentage (0-100).</param>
    /// <param name="alpha">Alpha transparency channel value (0-255).</param>
    /// <returns>A single 32-bit signed integer holding the ARGB byte arrangement sequence.</returns>
    public static int CmykToFullArgb(int cyan, int magenta, int yellow, int black, int alpha)
    {
        ThrowHelper.ThrowIfNegativeValue(cyan);
        ThrowHelper.ThrowIfNegativeValue(magenta);
        ThrowHelper.ThrowIfNegativeValue(yellow);
        ThrowHelper.ThrowIfNegativeValue(black);
        ThrowHelper.ThrowIfNegativeValue(alpha);

        cyan = Math.Min(cyan, 100);
        magenta = Math.Min(magenta, 100);
        yellow = Math.Min(yellow, 100);
        black = Math.Min(black, 100);
        alpha = Math.Min(alpha, 255);

        int red = (int)Math.Round(255 * (1 - cyan / 100.0) * (1 - black / 100.0));
        int green = (int)Math.Round(255 * (1 - magenta / 100.0) * (1 - black / 100.0));
        int blue = (int)Math.Round(255 * (1 - yellow / 100.0) * (1 - black / 100.0));

        return (alpha << 24) | (red << 16) | (green << 8) | blue;
    }
    
    /// <summary>Converts CMYK components directly into a single packed 32-bit ARGB integer (0xAARRGGBB).</summary>
    /// <param name="cyan">Cyan channel percentage (0-100).</param>
    /// <param name="magenta">Magenta channel percentage (0-100).</param>
    /// <param name="yellow">Yellow channel percentage (0-100).</param>
    /// <param name="black">Black channel percentage (0-100).</param>
    /// <returns>A single 32-bit signed integer holding the ARGB byte arrangement sequence.</returns>
    public static int CmykToFullArgb(int cyan, int magenta, int yellow, int black) =>
        CmykToFullArgb(cyan, magenta, yellow, black, 255);
    
    /// <summary>
    /// Converts RGB (Red, Green, Blue) components into a packed RGB/BGR integer calculated from CMYK percentages.
    /// Values above 255 are automatically clamped to 255.
    /// </summary>
    /// <param name="red">The Red color component. Must be a non-negative value (0 to 255+).</param>
    /// <param name="green">The Green color component. Must be a non-negative value (0 to 255+).</param>
    /// <param name="blue">The Blue color component. Must be a non-negative value (0 to 255+).</param>
    /// <returns>
    /// A 32-bit signed integer representing the color packed in BGR format, 
    /// generated after converting the inputs into CMYK percentages.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown via <c>ThrowHelper</c> when any of the RGB components is a negative number. 
    /// The localized error message follows the pattern: "The value for {0} cannot be negative! Received value: {1}".
    /// </exception>
    public static ICmyk RgbToCmyk(int red, int green, int blue)
    {
        ThrowHelper.ThrowIfNegativeValue(red);
        ThrowHelper.ThrowIfNegativeValue(green);
        ThrowHelper.ThrowIfNegativeValue(blue);

        double rPerc = Math.Min(red, 255) / 255.0;
        double gPerc = Math.Min(green, 255) / 255.0;
        double bPerc = Math.Min(blue, 255) / 255.0;

        double maxRgb = Math.Max(rPerc, Math.Max(gPerc, bPerc));
        double k = 1.0 - maxRgb;

        // Define uma tolerância minúscula para imprecisões de ponto flutuante
        const double tolerance = 1e-9;
        if (Math.Abs(k - 1.0) < tolerance) return new Cmyk(0, 0, 0, 100);

        int c = (int)Math.Round((1.0 - rPerc - k) / (1.0 - k) * 100);
        int m = (int)Math.Round((1.0 - gPerc - k) / (1.0 - k) * 100);
        int y = (int)Math.Round((1.0 - bPerc - k) / (1.0 - k) * 100);
        int black = (int)Math.Round(k * 100);

        return new Cmyk(c, m, y, black);
    }
    
    /// <summary>Converts ARGB color components into a <see cref="Cmyk"/> structure.</summary>
    /// <remarks>
    /// This method extracts the black key component first by evaluating the maximum value of the provided additive matrix.
    /// It includes floating-point precision protection to eliminate division-by-zero anomalies when processing dark tones.
    /// Note that the alpha channel is validated but not used in the CMYK subtractive calculation.
    /// </remarks>
    /// <param name="red">Red channel value (0-255).</param>
    /// <param name="green">Green channel value (0-255).</param>
    /// <param name="blue">Blue channel value (0-255).</param>
    /// <param name="alpha">Alpha transparency channel value (0-255).</param>
    /// <returns>A <see cref="Cmyk"/> structure representing subtractive ink volume ratios scaled from 0 to 100.</returns>
    public static ICmyk ArgbToCmyk(int red, int green, int blue, int alpha)
    {
        // Validações usando o seu ThrowHelper customizado
        ThrowHelper.ThrowIfNegativeValue(red);
        ThrowHelper.ThrowIfNegativeValue(green);
        ThrowHelper.ThrowIfNegativeValue(blue);
        ThrowHelper.ThrowIfNegativeValue(alpha);

        // Garante que os valores não ultrapassem o teto de 255
        double rPerc = Math.Min(red, 255) / 255.0;
        double gPerc = Math.Min(green, 255) / 255.0;
        double bPerc = Math.Min(blue, 255) / 255.0;

        double maxRgb = Math.Max(rPerc, Math.Max(gPerc, bPerc));
        double k = 1.0 - maxRgb;

        const double tolerance = 1e-9;

        if (Math.Abs(k - 1.0) < tolerance) return new Cmyk(0, 0, 0, 100);

        int c = (int)Math.Round((1.0 - rPerc - k) / (1.0 - k) * 100);
        int m = (int)Math.Round((1.0 - gPerc - k) / (1.0 - k) * 100);
        int y = (int)Math.Round((1.0 - bPerc - k) / (1.0 - k) * 100);
        int black = (int)Math.Round(k * 100);

        return new Cmyk(c, m, y, black);
    }

    /// <summary>Converts an <see cref="IRgb"/> color object into an <see cref="ICmyk"/> color object.</summary>
    /// <param name="rgb">The source RGB color object. Cannot be null.</param>
    /// <returns>An <see cref="ICmyk"/> instance representing the converted color.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="rgb"/> argument is null. </exception>
    /// <exception cref="ArgumentException">Thrown when any field/prop value from <paramref name="rgb"/> object is a negative value.</exception>
    public static ICmyk RgbToCmyk(IRgb rgb) => rgb is null
        ? throw new ArgumentNullException(nameof(rgb))
        : RgbToCmyk(rgb.Red, rgb.Green, rgb.Blue);

    /// <summary>Converts an <see cref="IArgb"/> color object into an <see cref="ICmyk"/> color object.</summary>
    /// <param name="argb">The source ARGB color object. Cannot be null.</param>
    /// <returns>An <see cref="ICmyk"/> instance representing the converted color, preserving transparency mapping if supported.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="argb"/> argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when any field/prop value from <paramref name="argb"/> object is a negative value.</exception>
    public static ICmyk ArgbToCmyk(IArgb argb) => argb is null
        ? throw new ArgumentNullException(nameof(argb))
        : ArgbToCmyk(argb.Red, argb.Green, argb.Blue, argb.Alpha);

    /// <summary>Converts an <see cref="ICmyk"/> color object into an <see cref="IRgb"/> color object.</summary>
    /// <param name="cmyk">The source CMYK color object. Cannot be null.</param>
    /// <returns>An <see cref="IRgb"/> instance representing the converted color.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="cmyk"/> argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when any field/prop value from <paramref name="cmyk"/> object is a negative value.</exception>
    public static IRgb CmykToRgb(ICmyk cmyk) => cmyk is null
        ? throw new ArgumentNullException(nameof(cmyk))
        : CmykToRgb(cmyk.Cyan, cmyk.Magenta, cmyk.Yellow, cmyk.Black);

    /// <summary>
    /// Converts an <see cref="ICmyk"/> color object into a single packed 32-bit ARGB integer.
    /// Uses the object's Alpha channel if present; otherwise, defaults to full opacity (255).
    /// </summary>
    /// <param name="cmyk">The source CMYK color object. Cannot be null.</param>
    /// <returns>A 32-bit signed integer representing the color packed in ARGB format.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="cmyk"/> argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when any field/prop value from <paramref name="cmyk"/> object is a negative value.</exception>
    public static int CmykToFullArgb(ICmyk cmyk) => cmyk is null 
        ? throw new ArgumentNullException(nameof(cmyk))
        : CmykToFullArgb(cmyk.Cyan, cmyk.Magenta, cmyk.Yellow, cmyk.Black, cmyk.Alpha ?? 255);

    /// <summary>
    /// Converts an <see cref="ICmyk"/> color object into an <see cref="IArgb"/> color object.
    /// Uses the object's Alpha channel if present; otherwise, defaults to full opacity (255).
    /// </summary>
    /// <param name="cmyk">The source CMYK color object. Cannot be null.</param>
    /// <returns>An <see cref="IArgb"/> instance representing the converted color with alpha transparency.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any field/prop from <paramref name="cmyk"/> argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when any field/prop value from <paramref name="cmyk"/> object is a negative value.</exception>
    public static IArgb CmykToArgb(ICmyk cmyk) => cmyk is null
        ? throw new ArgumentNullException(nameof(cmyk))
        : CmykToArgb(cmyk.Cyan, cmyk.Magenta, cmyk.Yellow, cmyk.Black, cmyk.Alpha ?? 255);

    /// <summary>Converts an <see cref="ICmyk"/> color object into a single packed 32-bit RGB/BGR integer.</summary>
    /// <param name="cmyk">The source CMYK color object. Cannot be null.</param>
    /// <returns>A 32-bit signed integer representing the color packed in the standard BGR format.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any field/prop from <paramref name="cmyk"/> argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when any field/prop value from <paramref name="cmyk"/> object is a negative value.</exception>
    public static int CmykToFullRgb(ICmyk cmyk) => cmyk is null
        ? throw new ArgumentNullException(nameof(cmyk))
        : CmykToFullRgb(cmyk.Cyan, cmyk.Magenta, cmyk.Yellow, cmyk.Black);

    /// <summary>Converts ARGB components into an opaque <see cref="IRgb"/> object by blending the transparency against a solid white background.</summary>
    /// <param name="r">The Red color component (0 to 255).</param>
    /// <param name="g">The Green color component (0 to 255).</param>
    /// <param name="b">The Blue color component (0 to 255).</param>
    /// <param name="a">The Alpha (transparency) component (0 to 255).</param>
    /// <returns>A new <see cref="Rgb"/> instance representing the flattened opaque color.</returns>
    public static IRgb ARgbToRgb(int r, int g, int b, int a)
    {
        ThrowHelper.ThrowIfNegativeValue(r, nameof(r));
        ThrowHelper.ThrowIfNegativeValue(g, nameof(g));
        ThrowHelper.ThrowIfNegativeValue(b, nameof(b));
        ThrowHelper.ThrowIfNegativeValue(a, nameof(a));

        if (r > 0xFF) r = 0xFF;
        if (g > 0xFF) g = 0xFF;
        if (b > 0xFF) b = 0xFF;
        if (a > 0xFF) a = 0xFF;

        switch (a)
        {
            case 0xFF: return new Rgb(r, g, b);
            case 0: return new Rgb(0xFF, 0xFF, 0xFF);
        }

        double alphaFactor = a / 255.0d;

        int finalR = (int)Math.Round((r * alphaFactor) + (0xFF * (1.0 - alphaFactor)));
        int finalG = (int)Math.Round((g * alphaFactor) + (0xFF * (1.0 - alphaFactor)));
        int finalB = (int)Math.Round((b * alphaFactor) + (0xFF * (1.0 - alphaFactor)));

        return new Rgb(finalR, finalG, finalB);
    }

    /// <summary>Converts ARGB components into a packed 32-bit BGR integer, blending the transparency against a solid white background.</summary>
    /// <param name="r">The Red color component (0 to 255).</param>
    /// <param name="g">The Green color component (0 to 255).</param>
    /// <param name="b">The Blue color component (0 to 255).</param>
    /// <param name="a">The Alpha (transparency) component (0 to 255).</param>
    /// <returns>A 32-bit signed integer representing the opaque color packed in BGR format.</returns>
    /// <exception cref="ArgumentException">Thrown when any values in parameters is a negative value.</exception>
    public static int ArgbToRgbPacked(int r, int g, int b, int a)
    {
        ThrowHelper.ThrowIfNegativeValue(r, nameof(r));
        ThrowHelper.ThrowIfNegativeValue(g, nameof(g));
        ThrowHelper.ThrowIfNegativeValue(b, nameof(b));
        ThrowHelper.ThrowIfNegativeValue(a, nameof(a));

        if (r > 0xFF) r = 0xFF;
        if (g > 0xFF) g = 0xFF;
        if (b > 0xFF) b = 0xFF;
        if (a > 0xFF) a = 0xFF;

        switch (a)
        {
            case 0xFF: return (b << 16) | (g << 8) | r;
            case 0: return (0xFF << 16) | (0xFF << 8) | 0xFF;
        }

        double alphaFactor = a / 255.0d;

        int finalR = (int)Math.Round((r * alphaFactor) + (0xFF * (1.0 - alphaFactor)));
        int finalG = (int)Math.Round((g * alphaFactor) + (0xFF * (1.0 - alphaFactor)));
        int finalB = (int)Math.Round((b * alphaFactor) + (0xFF * (1.0 - alphaFactor)));
        
        return (finalB << 16) | (finalG << 8) | finalR;
    }

    #endregion
    
}