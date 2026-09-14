
using Lagosystem.Modules.LanguagesResource;

namespace Lagosystem.Modules.Helpers
{
    using System.Runtime.CompilerServices;
    using Modules.Extensions;
    using Modules.LanguagesResource;
    using jba = JetBrains.Annotations;

    internal static partial class ThrowHelper
    {
        // ==========================================
        // IGUALDADE E DIFERENÇA
        // ==========================================

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowIfEquals<T>(T value, T comparand, 
            [jba.InvokerParameterName, jba.LocalizationRequired(false)] string? paramName = null)
        {
            if (!EqualityComparer<T>.Default.Equals(value, comparand)) return;
            throw ThrowEquals<T>(value, comparand, paramName);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowIfDifferent<T>(T value, T comparand, 
            [jba.InvokerParameterName, jba.LocalizationRequired(false)] string? paramName = null)
        {
            if (EqualityComparer<T>.Default.Equals(value, comparand)) return;
            throw ThrowDifferent(value, comparand, paramName);
        }

        // Overload específico para String com StringComparison
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowIfDifferent(string? value, string? comparand, StringComparison comparisonType,
            
            [jba.InvokerParameterName, jba.LocalizationRequired(false)] string? paramName = null)
        {
            if (string.Equals(value, comparand, comparisonType)) return;
            throw ThrowDifferent(value, comparand, comparisonType, paramName);
        }

        // ==========================================
        // GRANDEZAS E LIMITES (Usa IComparable<T>)
        // ==========================================

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowIfGreaterThan<T>(T value, T limit, 
            [jba.InvokerParameterName, jba.LocalizationRequired(false)] string? paramName = null) where T : IComparable<T>
        {
            // CompareTo retorna > 0 se o valor for maior que o limite
            if (value.CompareTo(limit) <= 0) return;
            throw ThrowGreaterThan(value, limit, paramName);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowIfGreaterOrEqualsThan<T>(T value, T limit, 
            [jba.InvokerParameterName, jba.LocalizationRequired(false)] string? paramName = null)
            where T : IComparable<T>
        {
            // CompareTo retorna ≥ 0 se o valor for maior ou igual ao limite
            if (value.CompareTo(limit) < 0) return;
            throw ThrowGreaterOrEqualsThan(value, limit, paramName);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowIfLessThan<T>(T value, T limit, 
            [jba.InvokerParameterName, jba.LocalizationRequired(false)] string? paramName = null) where T : IComparable<T>
        {
            // CompareTo retorna < 0 se o valor for menor que o limite
            if (value.CompareTo(limit) >= 0) return;
            throw ThrowLessThan(value, limit, paramName);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowIfLessOrEqualsThan<T>(T value, T limit, 
            [jba.InvokerParameterName, jba.LocalizationRequired(false)] string? paramName = null)
            where T : IComparable<T>
        {
            // CompareTo retorna <= 0 se o valor for menor ou igual ao limite
            if (value.CompareTo(limit) > 0) return;
            throw ThrowLessOrEqualsThan(value, limit, paramName);
        }






        // ==========================================
        // IGUALDADE E DIFERENÇA
        // ==========================================

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static Exception ThrowEquals<T>(T value, T comparand, 
            [jba.InvokerParameterName, jba.LocalizationRequired(false)] string? paramName = null)
        {
            string message = string.Format(ExceptionsResource.ValueCannotBeEqualTo, paramName, comparand);
            return paramName.IsBlank() ? new ArgumentException(message) : new ArgumentException(message, paramName);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static Exception ThrowDifferent<T>(T value, T comparand, 
            [jba.InvokerParameterName, jba.LocalizationRequired(false)] string? paramName = null)
        {
            string message = string.Format(ExceptionsResource.ValueCannotBeDifferentFrom, paramName, comparand);
            return new ArgumentException(message, paramName);
        }

        // Overload específico para String com StringComparison
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static Exception ThrowDifferent(string? value, string? comparand, StringComparison comparisonType,
            [jba.InvokerParameterName, jba.LocalizationRequired(false)] string? paramName = null)
        {
            string message = string.Format(ExceptionsResource.ValueCannotBeDifferentFrom, paramName, comparand);
            return new ArgumentException(message, paramName);
        }

        // ==========================================
        // GRANDEZAS E LIMITES (Usa IComparable<T>)
        // ==========================================

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static Exception ThrowGreaterThan<T>(T value, T limit, 
            [jba.InvokerParameterName, jba.LocalizationRequired(false)] string? paramName = null) where T : IComparable<T>
        {
            string message = string.Format(ExceptionsResource.ValueCannotBeGreatherThan, paramName, limit);
            return new ArgumentOutOfRangeException(paramName, value, message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static Exception ThrowGreaterOrEqualsThan<T>(T value, T limit, 
            [jba.InvokerParameterName, jba.LocalizationRequired(false)] string? paramName = null)
            where T : IComparable<T>
        {
            string message = string.Format(ExceptionsResource.ValueCannotBeGreatherOrEqualTo, paramName, limit);
            return new ArgumentOutOfRangeException(paramName, value, message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static Exception ThrowLessThan<T>(T value, T limit, 
            [jba.InvokerParameterName, jba.LocalizationRequired(false)] string? paramName = null) where T : IComparable<T>
        {
            string message = string.Format(ExceptionsResource.ValueCannotBeLessThan, paramName, limit);
            return new ArgumentOutOfRangeException(paramName, value, message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static Exception ThrowLessOrEqualsThan<T>(T value, T limit, 
            [jba.InvokerParameterName, jba.LocalizationRequired(false)] string? paramName = null)
            where T : IComparable<T>
        {
            string message = string.Format(ExceptionsResource.ValueCannotBeLessOrEqualTo, paramName, limit);
            return new ArgumentOutOfRangeException(paramName, value, message);
        }



    }
}