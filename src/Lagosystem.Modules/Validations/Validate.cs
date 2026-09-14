
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
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <param name="fieldName"></param>
    /// <param name="allowZero"></param>
    /// <param name="allowNegativeValue"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static T GrantNotNull<T>(T? value, [InvokerParameterName, LocalizationRequired(false)] string fieldName, bool allowZero = true, bool allowNegativeValue = true) where T : struct
    {
        if (!value.HasValue) throw new ArgumentNullException(fieldName);
        T result = value.Value;
        if (typeof(T) == typeof(bool)) return result;
        if (!allowZero || !allowNegativeValue) ValidateNumericValue(result, fieldName, allowZero, allowNegativeValue);
        return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <param name="fieldName"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static string GrantNotNullOrEmpty(string? value, [InvokerParameterName, LocalizationRequired(false)] string fieldName) => 
        value.IsBlank() 
            ? throw new ArgumentNullException(fieldName) 
            : value!;

    public static byte[] GrantNotNullOrEmpty(byte[]? value, [InvokerParameterName, LocalizationRequired(false)] string fieldName) =>
        (value is not null)
            ? (value.Length == 0 ? throw new ArgumentException($"The field '{fieldName}' cannot be empty.", fieldName) : value)
            : throw new ArgumentNullException(fieldName);

    private static readonly ConditionalWeakTable<Stream, object> StreamLocks = new();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <param name="fieldName"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public static byte[] GrantNotNullOrEmpty(Stream? value, [InvokerParameterName, LocalizationRequired(false)] string fieldName)
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

    /// <summary>
    /// 
    /// </summary>
    /// <param name="regex"></param>
    /// <returns></returns>
    public static bool IsValidRegexInput(string? regex)
    {
        if (regex.IsBlank()) return false;
        try
        {
            _ = new Regex(regex!, RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
            return true;
        }
        catch (ArgumentException) { return false; }
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="culture"></param>
    /// <returns></returns>
    public static bool IsValidSpecificCulture(string? culture)
    {
        if (culture.IsBlank()) return false; 
        try
        {
            CultureInfo cultureInfo = CultureInfo.GetCultureInfo(culture!);
            return !cultureInfo.IsNeutralCulture && cultureInfo != CultureInfo.InvariantCulture;
        }
        catch (CultureNotFoundException) { return false; }
    }
}