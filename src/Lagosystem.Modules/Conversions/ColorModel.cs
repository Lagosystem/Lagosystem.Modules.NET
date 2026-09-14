namespace Lagosystem.Modules.Conversions;


/// <summary>Defines a standard contract for additive color models composed of Cyan, Magenta, Yellow and Black channels.</summary>
public interface ICmyk
{
    /// <summary>Gets the Cyan channel value (0-100).</summary>
    public int Cyan { get; }

    /// <summary>Gets the Magenta channel value (0-100).</summary>
    public int Magenta { get; }

    /// <summary>Gets the Yellow channel value (0-100).</summary>
    public int Yellow { get; }

    /// <summary>Gets the Black (Key) channel value (0-100).</summary>
    public int Black { get; }
    
    /// <summary>Optional alpha transparency channel value (0-255). If null, uses 255 (opaque).</summary>
    public int? Alpha { get; }
}

/// <summary>
/// Represents a color in the CMYK (Cyan, Magenta, Yellow, Black) subtractive color model, with channel values scaled from 0 to 100.
/// </summary>
public struct Cmyk : ICmyk
{
    internal Cmyk(int c, int m, int y, int k)
    {
        Cyan = c;
        Magenta = m;
        Yellow = y;
        Black = k;
        Alpha = null;
    }

    internal Cmyk(int c, int m, int y, int k, int? alpha) : this(c, m, y, k) => Alpha = alpha;

    /// <inheritdoc/>
    public int Cyan { get; }

    /// <inheritdoc/>
    public int Magenta { get; }

    /// <inheritdoc/>
    public int Yellow { get; }

    /// <inheritdoc/>
    public int Black { get; }

    /// <inheritdoc/>
    public int? Alpha { get; }
}

/// <summary>Defines a standard contract for additive color models composed of Red, Green, and Blue channels.</summary>
public interface IRgb
{
    /// <summary>Gets the Red channel value scaled from 0 to 255.</summary>
    int Red { get; }

    /// <summary>Gets the Green channel value scaled from 0 to 255.</summary>
    int Green { get; }

    /// <summary>Gets the Blue channel value scaled from 0 to 255.</summary>
    int Blue { get; }

    /// <summary>Packs the RGB or RGBA channels into a single 32-bit RGB or ARGB integer format (0xFFRRGGBB) assuming full opacity when Alpha is undefined.</summary>
    /// <returns>A 32-bit signed integer representing the color in RGB or ARGB format.</returns>
    int ToPacked();
}

/// <inheritdoc/>
public interface IArgb : IRgb
{
    /// <summary>Gets the Alpha (transparency) channel value (0-255).</summary>
    public int Alpha { get; }
}

/// <summary>Represents an opaque color in the RGB additive color model, with channel values scaled from 0 to 255.</summary>
public readonly struct Rgb : IRgb
{
    internal Rgb(int r, int g, int b)
    {
        Red = r;
        Green = g;
        Blue = b;
    }

    /// <inheritdoc />
    public int Red { get; }

    /// <inheritdoc />
    public int Green { get; }

    /// <inheritdoc />
    public int Blue { get; }

    /// <inheritdoc />
    public int ToPacked() => (255 << 24) | (Red << 16) | (Green << 8) | Blue;
}

/// <summary>Represents a color with alpha transparency in the RGBA additive color model, with channel values scaled from 0 to 255.</summary>
public readonly struct Argb : IArgb
{
    internal Argb(int r, int g, int b, int a)
    {
        Red = r;
        Green = g;
        Blue = b;
        Alpha = a;
    }
    
    /// <inheritdoc />
    public int Red { get; }

    /// <inheritdoc />
    public int Green { get; }

    /// <inheritdoc />
    public int Blue { get; }

    /// <inheritdoc />
    public int Alpha { get; }

    /// <inheritdoc />
    public int ToPacked() => (Alpha << 24) | (Red << 16) | (Green << 8) | Blue;
}
