
namespace Lagosystem.Modules.Helpers;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Lagosystem.Modules.LanguagesResource;


internal static partial class ThrowHelper
{
    /// <summary>Throws when a periodic interest rate is not greater than -100 percent.</summary>
    /// <param name="paramName">The name of the invalid rate parameter.</param>
    /// <exception cref="ArgumentOutOfRangeException">Always thrown.</exception>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowFinancialPeriodicRateOutOfRange(string paramName) =>
        throw new ArgumentOutOfRangeException(paramName, ExceptionsResource.FinancialPeriodicRateOutOfRange);

    /// <summary>Throws when a payment timing value is not a supported enum member.</summary>
    /// <param name="paramName">The name of the invalid timing parameter.</param>
    /// <exception cref="ArgumentOutOfRangeException">Always thrown.</exception>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowFinancialPaymentTimingInvalid(string paramName) =>
        throw new ArgumentOutOfRangeException(paramName, ExceptionsResource.FinancialPaymentTimingInvalid);

    /// <summary>Throws when a financial calculation produces a non-finite result.</summary>
    /// <exception cref="OverflowException">Always thrown.</exception>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowFinancialResultNotFinite() =>
        throw new OverflowException(ExceptionsResource.FinancialResultNotFinite);

    /// <summary>Throws when a periodic payment cannot amortise a loan at the supplied rate.</summary>
    /// <param name="paramName">The name of the payment parameter.</param>
    /// <exception cref="ArgumentException">Always thrown.</exception>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowFinancialPaymentCannotAmortizeLoan(string paramName) =>
        throw new ArgumentException(ExceptionsResource.FinancialPaymentCannotAmortizeLoan, paramName);

    /// <summary>Throws when the cash flows do not yield a real number of periods.</summary>
    /// <exception cref="ArgumentException">Always thrown.</exception>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowFinancialPeriodsNotReal() =>
        throw new ArgumentException(ExceptionsResource.FinancialPeriodsNotReal);

    /// <summary>Throws when an indeterminate cash-flow equation has no unique interest rate.</summary>
    /// <exception cref="ArgumentException">Always thrown.</exception>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowFinancialInterestRateNotUnique() =>
        throw new ArgumentException(ExceptionsResource.FinancialInterestRateNotUnique);

    /// <summary>Throws when the supplied cash flows do not identify a unique interest rate.</summary>
    /// <exception cref="ArgumentException">Always thrown.</exception>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowFinancialCashFlowsRateNotUnique() =>
        throw new ArgumentException(ExceptionsResource.FinancialCashFlowsRateNotUnique);
}
