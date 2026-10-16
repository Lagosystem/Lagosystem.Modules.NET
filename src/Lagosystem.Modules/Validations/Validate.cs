
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace Lagosystem.Modules.Validations;
using Lagosystem.Modules.Extensions;

/// <summary>
/// Provides high-performance validation helpers for documents, e-mail addresses, numeric values, 
/// and other domain-specific checks used across the Lagosnack modules.
/// Optimized for Zero Allocation (Zero Alloc) in critical database loops.
/// </summary>
public static partial class Validate
{
    /// <summary>
    /// Ensures that a nullable value type is not null and optionally validates numeric constraints.
    /// </summary>
    /// <param name="value">The nullable value to check.</param>
    /// <param name="fieldName">The name of the field or parameter being validated, used in exception messages.</param>
    /// <param name="allowZero">If <see langword="false"/>, throws an exception if the unwrapped numeric value is zero. Defaults to <see langword="true"/>.</param>
    /// <param name="allowNegativeValue">If <see langword="false"/>, throws an exception if the unwrapped numeric value is negative. Defaults to <see langword="true"/>.</param>
    /// <typeparam name="T">The underlying value type.</typeparam>
    /// <returns>The unwrapped, non-null value.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the value violates the zero or negative constraints.</exception>
    public static T GrantNotNull<T>(T? value, [InvokerParameterName, LocalizationRequired(false)] string fieldName, bool allowZero = true, bool allowNegativeValue = true) where T : struct
    {
        if (!value.HasValue) throw new ArgumentNullException(fieldName);
        T result = value.Value;
        if (typeof(T) == typeof(bool)) return result;
        if (!allowZero || !allowNegativeValue) ValidateNumericValue(result, fieldName, allowZero, allowNegativeValue);
        return result;
    }

    /// <summary>
    /// Ensures that a string is neither null, empty, nor consists only of white-space characters.
    /// </summary>
    /// <param name="value">The string value to check.</param>
    /// <param name="fieldName">The name of the field or parameter being validated, used in exception messages.</param>
    /// <returns>The valid, non-blank string.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null, empty, or white-space.</exception>
    public static string GrantNotNullOrEmpty(string? value, [InvokerParameterName, LocalizationRequired(false)] string fieldName) => 
        value.IsBlank() 
            ? throw new ArgumentNullException(fieldName) 
            : value;

    /// <summary>
    /// Ensures that a string is neither null, empty, nor consists only of white-space characters.
    /// </summary>
    /// <param name="value">The string value to check.</param>
    /// <param name="fieldName">The name of the field or parameter being validated, used in exception messages.</param>
    /// <returns>The valid, non-blank string.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null, empty, or white-space.</exception>
    public static byte[] GrantNotNullOrEmpty(byte[]? value, [InvokerParameterName, LocalizationRequired(false)] string fieldName) =>
        (value is not null)
            ? (value.Length == 0 ? throw new ArgumentException($"The field '{fieldName}' cannot be empty.", fieldName) : value)
            : throw new ArgumentNullException(fieldName);

    private static readonly ConditionalWeakTable<Stream, object> StreamLocks = new();

    /// <summary>
    /// Ensures that a stream is not null, can be read, is not empty, and safely extracts its content as a byte array.
    /// Uses lock synchronization to handle concurrent reads on the same stream instance.
    /// </summary>
    /// <param name="value">The stream to read and validate.</param>
    /// <param name="fieldName">The name of the field or parameter being validated, used in exception messages.</param>
    /// <returns>A byte array containing the stream's data.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown if the stream cannot be read or is empty.</exception>
    public static byte[] GrantNotNullOrEmpty(Stream? value, [InvokerParameterName, LocalizationRequired(false)] string fieldName)
    {
        try
        {
            if (value is null) throw new ArgumentNullException(fieldName);
            object syncRoot = StreamLocks.GetValue(value, static _ => new object());
            lock (syncRoot)
            {
                if (!value.CanRead) throw new ArgumentException($"The stream from field '{fieldName}' cannot be read.", fieldName);
                long? originalPosition = value.CanSeek ? value.Position : null;
                try
                {
                    if (value.CanSeek)
                    {
                        if (value.Length == 0) throw new ArgumentException($"The field '{fieldName}' cannot be empty.", fieldName);
                        value.Position = 0;
                    }
                    using MemoryStream destination = new();
                    value.CopyTo(destination);
                    return destination.Length == 0 ? throw new ArgumentException($"The field '{fieldName}' cannot contain an empty stream.", fieldName) : destination.ToArray();
                }
                finally { if (originalPosition.HasValue) value.Position = originalPosition.Value; }
            }
        }
        catch (Exception exception) when (Diagnostics.ReportOperationalFailure(exception, nameof(GrantNotNullOrEmpty), nameof(Validate)))
        { throw; }
    }

    private static void ValidateNumericValue<T>(T value, [InvokerParameterName, LocalizationRequired(false)] string fieldName, bool allowZero, bool allowNegativeValue) where T : struct
    {
        Type type = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
        if (type.IsEnum) return;
        
        TypeCode typeCode = Type.GetTypeCode(type);
        
        bool isNumeric = typeCode is
            TypeCode.Byte or TypeCode.SByte or
            TypeCode.Int16 or TypeCode.Int32 or TypeCode.Int64 or
            TypeCode.UInt16 or TypeCode.UInt32 or TypeCode.UInt64 or
            TypeCode.Single or TypeCode.Double or TypeCode.Decimal;
        
        if (!isNumeric) return;
        
        decimal numericValue;
        try { numericValue = Convert.ToDecimal(value, CultureInfo.InvariantCulture); }
        catch (Exception exception) when (exception is InvalidCastException or FormatException or OverflowException)
        { throw new ArgumentException($"The field '{fieldName}' contains an invalid numeric value.", fieldName, exception); }

        if (!allowZero && numericValue == decimal.Zero) 
            throw new ArgumentOutOfRangeException(fieldName, value, $"The field '{fieldName}' cannot be zero.");

        if (!allowNegativeValue && numericValue < decimal.Zero)
            throw new ArgumentOutOfRangeException(fieldName, value, $"The field '{fieldName}' cannot contain a negative value.");
    }

    /// <summary>Determines whether the specified string is a valid Regular Expression pattern.</summary>
    /// <param name="regex">The string pattern to validate.</param>
    /// <returns>
    /// <see langword="true"/> if the string is a valid regex pattern and can be compiled; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool IsValidRegexInput(string? regex)
    {
        if (regex.IsBlank()) return false;
        try
        {
            _ = new Regex(regex, RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
            return true;
        }
        catch (ArgumentException) { return false; }
    }
    
    /// <summary>Determines whether the specified string represents a valid, specific (non-neutral) cultureName.</summary>
    /// <param name="cultureName">The cultureCode code to validate (e.g., "pt-BR", "en-US", "es-AR").</param>
    /// <returns>
    /// <see langword="true"/> if the cultureName name is valid and represents a specific country/region; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool IsValidSpecificCulture(string? cultureName)
    {
        if (cultureName.IsBlank()) return false; 
        try
        {
            CultureInfo cultureInfo = CultureInfo.GetCultureInfo(cultureName);
            return !cultureInfo.IsNeutralCulture && !Equals(cultureInfo, CultureInfo.InvariantCulture);
        }
        catch (CultureNotFoundException) { return false; }
    }
    
    /// <summary>Determines whether the specified string represents a valid, specific (non-neutral) cultureCode.</summary>
    /// <param name="cultureCode">The cultureCode code to validate (e.g., 1046, 1033, 11274).</param>
    /// <returns>
    /// <see langword="true"/> if the cultureCode name is valid and represents a specific country/region; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool IsValidSpecificCulture(int? cultureCode)
    {
        if (!cultureCode.HasValue) return false; 
        try
        {
            CultureInfo cultureInfo = CultureInfo.GetCultureInfo(cultureCode.Value);
            return !cultureInfo.IsNeutralCulture && !Equals(cultureInfo, CultureInfo.InvariantCulture);
        }
        catch (CultureNotFoundException) { return false; }
    }
}
