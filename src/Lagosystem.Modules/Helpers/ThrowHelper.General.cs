
using Lagosystem.Modules.Formatting;
using Lagosystem.Modules.LanguagesResource;

namespace Lagosystem.Modules.Helpers
{
    using System.Runtime.CompilerServices;
    using System.Runtime.ExceptionServices;
    using Modules.Extensions;
    using Modules.LanguagesResource;
    using msca = System.Diagnostics.CodeAnalysis;
    using jba = JetBrains.Annotations;

    internal static partial class ThrowHelper
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static Exception ArgumentInsufficientBufferFromParam(
            [jba.InvokerParameterName, jba.LocalizationRequired(false)] string paramName) =>
            new ArgumentException(ExceptionsResource.InsufficientBuffer, paramName);

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowIfNull(
            [msca.NotNull, jba.NotNull] object? argument, 
            [jba.InvokerParameterName, jba.LocalizationRequired(false)] string? paramName = null) 
        {
            if (argument is not null) return;
            string message = string.Format(ExceptionsResource.GeneralArgumentNullException, paramName);
            throw new ArgumentNullException(paramName, message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowIfNull(
            [msca.NotNull] string? argument, 
            [jba.InvokerParameterName, jba.LocalizationRequired(false)] string? paramName = null) 
        {
#pragma warning disable CS8777 // Parameter must have a non-null value when exiting.
            if(!argument.IsBlank()) return;
#pragma warning restore CS8777 // Parameter must have a non-null value when exiting.
            string message = string.Format(ExceptionsResource.GeneralArgumentNullException, paramName);
            throw new ArgumentNullException(paramName, message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowIfInvalidArgumentType(string paramName, Type? gotTypeParam, params Type[] expectedTypes)
        {
            if (expectedTypes.Contains(gotTypeParam)) return;
            throw InvalidArgumentType(paramName, gotTypeParam, expectedTypes);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static Exception InvalidArgumentType(
            [jba.InvokerParameterName, jba.LocalizationRequired(false)] string paramName, 
            [jba.InvokerParameterName, jba.LocalizationRequired(false)] Type? gotTypeParam, params Type[] expectedTypes)
        {
            string message = string.Format(ExceptionsResource.InvalidArgumentType, gotTypeParam?.FullName ?? "[[null/Nothing/Nil]]", string.Join(" / ", expectedTypes.Select(t => t.FullName)));            
            return new ArgumentException(message, paramName);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static Exception DisposedObj(
            [jba.InvokerParameterName, jba.LocalizationRequired(false)] IDisposable objectName, bool throwNow = false)
        {
            Exception ex = new ObjectDisposedException(objectName.GetType().FullName);
            return throwNow ? throw ex : ex;
        }
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowIfReentrantCollectionChange(int handlerCount)
        {
            if (handlerCount > 1)
                throw new InvalidOperationException(
                    string.Format(ExceptionsResource.ReentrantCollectionChangeNotAllowed, handlerCount));
        }
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Rethrow(Exception exception)
        {
            ExceptionDispatchInfo.Capture(exception).Throw();
            throw new InvalidOperationException("Unreachable code.");
        }

        internal static void ThrowNegativeValue([jba.InvokerParameterName, jba.LocalizationRequired(false)] string paramName, object gotValue)
        {
            throw new ArgumentException(string.Format(ExceptionsResource.NegativeValueDennied, paramName, gotValue),paramName);
        }
        internal static void ThrowIfNegativeValue<T>(T value, [jba.InvokerParameterName, jba.LocalizationRequired(false)] string paramName = null) 
            where T : struct, IComparable<T>
        {
            // Se o valor for menor que zero (default), o CompareTo retornará um número menor que 0
            if (value.CompareTo(default) >= 0) return;
            string message = string.Format(ExceptionsResource.NegativeValueDennied, paramName, value);
            throw new ArgumentException(message, paramName);
        }

        internal static void ThrowDividedByZeroCalculated() => throw new DivideByZeroException(ExceptionsResource.DivideByZeroCalculated);
        internal static void ThrowDividedByZeroReceived(string paramName) => throw new DivideByZeroException(string.Format(ExceptionsResource.DivideByZeroReceived,paramName));

        internal static void ThrowIfZeroToDivideReceived<T>(T value,  string? paramName = null) 
            where T : struct, IComparable<T>
        {
            if (!EqualityComparer<T>.Default.Equals(value, default)) return;
            throw new DivideByZeroException(string.Format(ExceptionsResource.DivideByZeroReceived, paramName));
        }
        
        internal static void ThrowIfZeroToDivideCalculated<T>(T value,  string? paramName = null) 
            where T : struct, IComparable<T>
        {
            if (!EqualityComparer<T>.Default.Equals(value, default)) return;
            throw new DivideByZeroException(string.Format(ExceptionsResource.DivideByZeroCalculated, paramName));
        }

        internal static void ThrowInterestRateConvergenceFailed()
        {
            throw new InvalidOperationException(ExceptionsResource.InterestRateConvergenceFailed);
        }
        
        internal static void ThrowPaymentInsufficientForInterest(string paramName)
        {
            throw new ArgumentException(ExceptionsResource.PaymentInsufficientForInterest, paramName);
        }
    }
}