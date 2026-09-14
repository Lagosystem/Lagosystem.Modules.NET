// Lagosystem Financials - .NET Standard 2.1 - no external packages.
// Generated from the six modular source files. Compile this OR src/*.cs, never both.

namespace Lagosystem.Modules.Calc.Financials;

using Lagosystem.Modules.Helpers;
using System.Buffers;
using System.Runtime.CompilerServices;
using System;

/// <summary>Specifies when a periodic annuity payment is made.</summary>
public enum FinancialPaymentTiming
{
    /// <summary>Payments occur at the end of each period.</summary>
    EndOfPeriod = 0,
    /// <summary>Payments occur at the beginning of each period.</summary>
    BeginningOfPeriod = 1
}

/// <summary>Contains a numerical result and diagnostic information about its approximation.</summary>
/// <remarks>The error is an estimate, not a certified mathematical bound. A small residual does not
/// guarantee an accurate root when the objective is nearly flat. No accounting rounding is applied.</remarks>
public readonly struct FinancialCalculationResult
{
    /// <summary>The calculated result in the units documented by the operation.</summary>
    public decimal Value { get; }
    /// <summary>Estimated absolute numerical error, in the same units as <see cref="Value"/>.</summary>
    public decimal EstimatedAbsoluteError { get; }
    /// <summary>Number of numerical iterations performed.</summary>
    public int Iterations { get; }
    /// <summary>Absolute residual of the normalized objective; zero for non-iterative operations.</summary>
    public double Residual { get; }

    /// <summary>Creates a successful calculation diagnostic.</summary>
    public FinancialCalculationResult(decimal value, decimal estimatedAbsoluteError, int iterations, double residual)
    {
        Value = value;
        EstimatedAbsoluteError = estimatedAbsoluteError;
        Iterations = iterations;
        Residual = residual;
    }
}

/// <summary>Financial mathematics for cash flows, annuities, rates, depreciation and securities.</summary>
/// <remarks>
/// Public percentage inputs and outputs use the 0–100 scale: 1.5 means 1.5 percent.
/// Cash-flow methods use signed amounts: money received is positive and money paid is negative.
/// Existing magnitude-based methods retain their original contract. Rates and periods must use
/// matching units. Results are not rounded to currency minor units. The scalar core is allocation-free
/// on successful calls; transcendental and iterative calculations use double internally and convert
/// once to decimal. Excel compatibility is mathematical, not a promise of bit-for-bit identity.
/// </remarks>
public static partial class Financials
{
    private const double DefaultRateTolerance = 1e-13;
    private const int DefaultMaximumIterations = 128;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void RequirePositive(decimal value, string name)
    {
        if (value > 0m) return;
        if (value == 0m) ThrowHelper.ThrowDividedByZeroReceived(name);
        ThrowHelper.ThrowNegativeValue(name, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void RequirePositive(int value, string name)
    {
        if (value > 0) return;
        if (value == 0) ThrowHelper.ThrowDividedByZeroReceived(name);
        ThrowHelper.ThrowNegativeValue(name, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void RequireNonNegative(decimal value, string name)
    {
        if (value < 0m) ThrowHelper.ThrowNegativeValue(name, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void RequireNonNegative(int value, string name)
    {
        if (value < 0) ThrowHelper.ThrowNegativeValue(name, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void RequireRate(decimal percentageRate, string name)
    {
        if (percentageRate <= -100m) ThrowHelper.ThrowFinancialPeriodicRateOutOfRange(name);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void RequireTiming(FinancialPaymentTiming timing)
    {
        if (timing is not (FinancialPaymentTiming.EndOfPeriod or FinancialPaymentTiming.BeginningOfPeriod))
            ThrowHelper.ThrowFinancialPaymentTimingInvalid(nameof(timing));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static decimal CheckedDecimal(double value)
    {
        if (!double.IsFinite(value)) ThrowHelper.ThrowFinancialResultNotFinite();
        return checked((decimal)value);
    }

    // Compensated elementary functions prevent catastrophic cancellation near zero.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double LogOnePlus(double value)
    {
        if (Math.Abs(value) >= 1e-4) return Math.Log(1.0 + value);
        double term = value;
        double sum = term;
        for (int index = 2; index <= 6; index++)
        {
            term *= -value;
            sum += term / index;
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double ExponentialMinusOne(double value)
    {
        if (Math.Abs(value) >= 1e-5) return Math.Exp(value) - 1.0;
        double term = value;
        double sum = term;
        for (int index = 2; index <= 6; index++)
        {
            term *= value / index;
            sum += term;
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double GrowthFactor(double rate, double periods) => Math.Exp(periods * LogOnePlus(rate));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double AnnuityFactor(double rate, double periods)
    {
        if (rate == 0.0) return periods;
        return ExponentialMinusOne(periods * LogOnePlus(rate)) / rate;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double PresentAnnuityFactor(double rate, double periods)
    {
        if (rate == 0.0) return periods;
        return -ExponentialMinusOne(-periods * LogOnePlus(rate)) / rate;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double PaymentFactor(double rate, FinancialPaymentTiming timing) =>
        timing == FinancialPaymentTiming.BeginningOfPeriod ? 1.0 + rate : 1.0;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static decimal IntegerPower(decimal value, int exponent)
    {
        decimal result = 1m;
        while (exponent != 0)
        {
            if ((exponent & 1) != 0) result *= value;
            exponent >>= 1;
            if (exponent != 0) value *= value;
        }
        return result;
    }

    /// <summary>Compounds a decimal base by an integral exponent without a floating-point conversion.</summary>
    /// <remarks>Uses exponentiation by squaring in O(log n). This is useful for repeated, fixed-rate
    /// monetary computations. Decimal overflow is not silently replaced with an approximate result.</remarks>
    /// <param name="principal">Signed initial amount.</param>
    /// <param name="percentageRate">Periodic percentage rate greater than -100.</param>
    /// <param name="periods">Nonnegative integral number of periods.</param>
    /// <returns>The exact decimal multiplication result, subject to decimal's finite precision and range.</returns>
    public static decimal CalculateFutureValueWithDecimalPrecision(decimal principal, decimal percentageRate, int periods)
    {
        RequireRate(percentageRate, nameof(percentageRate));
        RequireNonNegative(periods, nameof(periods));
        return principal * IntegerPower(1m + percentageRate / 100m, periods);
    }

    /// <summary>Calculates the present value of a single future amount using compound discounting.</summary>
    /// <param name="futureValue">Future amount, preserving its sign.</param>
    /// <param name="percentageRate">Rate per period; 10 means 10 percent. Must exceed -100.</param>
    /// <param name="periods">Number of compounding periods, including zero.</param>
    /// <returns>The future amount divided by (1 + rate) raised to the number of periods.</returns>
    public static decimal CalculatePresentValue(decimal futureValue, decimal percentageRate, int periods)
    {
        RequireRate(percentageRate, nameof(percentageRate));
        RequireNonNegative(periods, nameof(periods));
        if (periods == 0 || futureValue == 0m || percentageRate == 0m) return futureValue;
        if (periods <= 32 && percentageRate > 0m)
        {
            decimal denominator = IntegerPower(1m + percentageRate / 100m, periods);
            ThrowHelper.ThrowIfZeroToDivideCalculated(denominator);
            return futureValue / denominator;
        }
        return CheckedDecimal((double)futureValue * Math.Exp(-periods * LogOnePlus((double)(percentageRate / 100m))));
    }

    /// <summary>Calculates the future value of a single principal with compound interest.</summary>
    /// <param name="presentValue">Initial principal or signed amount.</param>
    /// <param name="percentageRate">Rate per period, on the 0–100 scale; must exceed -100.</param>
    /// <param name="periods">Number of periods, including zero.</param>
    /// <returns>The principal multiplied by the compound growth factor.</returns>
    public static decimal CalculateFutureValue(decimal presentValue, decimal percentageRate, int periods)
    {
        RequireRate(percentageRate, nameof(percentageRate));
        RequireNonNegative(periods, nameof(periods));
        if (periods == 0 || presentValue == 0m || percentageRate == 0m) return presentValue;
        if (periods <= 32)
        {
            // Avoid an intermediate decimal overflow when the exact result would exceed its range.
            double factor = GrowthFactor((double)(percentageRate / 100m), periods);
            double approximate = (double)presentValue * factor;
            if (double.IsFinite(factor) && factor < (double)decimal.MaxValue / 2.0 &&
                double.IsFinite(approximate) && Math.Abs(approximate) < (double)decimal.MaxValue / 2.0)
                return presentValue * IntegerPower(1m + percentageRate / 100m, periods);
            return CheckedDecimal(approximate);
        }
        return CheckedDecimal((double)presentValue * GrowthFactor((double)(percentageRate / 100m), periods));
    }

    /// <summary>Calculates a percentage of an amount without currency rounding.</summary>
    /// <param name="totalValue">The base amount; zero and negative amounts are valid.</param>
    /// <param name="percentage">The percentage, where 15 means 15 percent.</param>
    /// <returns>The base amount multiplied by the percentage and divided by 100.</returns>
    public static decimal CalculatePercentageAmount(decimal totalValue, decimal percentage) =>
        totalValue * (percentage / 100m);

    /// <summary>Converts a fractional rate into a rounded percentage.</summary>
    /// <param name="rate">Fractional rate, where 0.125 means 12.5 percent.</param>
    /// <param name="decimalPlaces">Number of decimal places, from zero through 28.</param>
    /// <returns>The percentage rounded using <see cref="MidpointRounding.AwayFromZero"/>.</returns>
    public static decimal ConvertToPercentage(decimal rate, int decimalPlaces) =>
        Math.Round(rate * 100m, decimalPlaces, MidpointRounding.AwayFromZero);

    /// <summary>Calculates simple interest without capitalizing previously accrued interest.</summary>
    /// <param name="principal">Principal amount.</param>
    /// <param name="percentageRate">Interest rate per period on the 0–100 scale.</param>
    /// <param name="time">Number of periods, which may be fractional.</param>
    /// <returns>Principal multiplied by the rate and time.</returns>
    public static decimal CalculateSimpleInterest(decimal principal, decimal percentageRate, decimal time) =>
        principal * (percentageRate / 100m) * time;

    /// <summary>Calculates the percentage change from an original value to a new value.</summary>
    /// <param name="oldValue">Nonzero original value.</param>
    /// <param name="newValue">New value; zero and negative values are valid.</param>
    /// <returns>The signed relative change on the 0–100 percentage scale.</returns>
    public static decimal CalculatePercentageChange(decimal oldValue, decimal newValue)
    {
        ThrowHelper.ThrowIfZeroToDivideReceived(oldValue, nameof(oldValue));
        return ((newValue - oldValue) / oldValue) * 100m;
    }

    /// <summary>Converts a monetary amount using a supplied direct or inverse exchange rate.</summary>
    /// <param name="amount">Signed source amount.</param>
    /// <param name="exchangeRate">Positive conversion coefficient.</param>
    /// <param name="isInverseRate">True to divide by the coefficient instead of multiplying.</param>
    /// <returns>The converted amount without currency rounding or exchange-rate lookup.</returns>
    public static decimal ConvertCurrency(decimal amount, decimal exchangeRate, bool isInverseRate = false)
    {
        RequirePositive(exchangeRate, nameof(exchangeRate));
        return isInverseRate ? amount / exchangeRate : amount * exchangeRate;
    }

    /// <summary>Calculates the positive installment magnitude of an ordinary amortizing loan.</summary>
    /// <param name="percentageRate">Periodic interest rate on the 0–100 scale, including zero.</param>
    /// <param name="periods">Positive number of installments.</param>
    /// <param name="presentValue">Nonnegative loan principal.</param>
    /// <returns>The positive payment magnitude; use <see cref="CalculateAnnuityPayment"/> for signed Excel-style cash flows.</returns>
    public static decimal CalculatePeriodicPayment(decimal percentageRate, int periods, decimal presentValue)
    {
        RequireNonNegative(presentValue, nameof(presentValue));
        RequireRate(percentageRate, nameof(percentageRate));
        RequirePositive(periods, nameof(periods));
        if (presentValue == 0m) return 0m;
        if (percentageRate == 0m) return presentValue / periods;
        double rate = (double)(percentageRate / 100m);
        double denominator = PresentAnnuityFactor(rate, periods);
        if (denominator == 0.0) ThrowHelper.ThrowDividedByZeroCalculated();
        return CheckedDecimal((double)presentValue / denominator);
    }

    /// <summary>Calculates a signed constant annuity payment, equivalent to Excel's payment function.</summary>
    /// <remarks>Solves presentValue * growth + payment * annuityFactor * timingFactor + futureValue = 0.
    /// A positive loan principal normally produces a negative payment. No monetary rounding is applied.</remarks>
    /// <param name="percentageRate">Rate per period, where 2 means 2 percent; must exceed -100.</param>
    /// <param name="periods">Positive number of payment periods.</param>
    /// <param name="presentValue">Signed present value.</param>
    /// <param name="futureValue">Signed desired future balance, normally zero.</param>
    /// <param name="timing">Whether payments occur at the beginning or end of each period.</param>
    /// <returns>The signed periodic payment.</returns>
    public static decimal CalculateAnnuityPayment(decimal percentageRate, int periods, decimal presentValue,
        decimal futureValue = 0m, FinancialPaymentTiming timing = FinancialPaymentTiming.EndOfPeriod)
    {
        RequirePositive(periods, nameof(periods));
        RequireRate(percentageRate, nameof(percentageRate));
        RequireTiming(timing);
        if (percentageRate == 0m) return -(presentValue + futureValue) / periods;
        double rate = (double)(percentageRate / 100m);
        double growth = GrowthFactor(rate, periods);
        double denominator = AnnuityFactor(rate, periods) * PaymentFactor(rate, timing);
        if (denominator == 0.0) ThrowHelper.ThrowDividedByZeroCalculated();
        return CheckedDecimal(-((double)presentValue * growth + (double)futureValue) / denominator);
    }

    /// <summary>Calculates the signed future value of an annuity, equivalent to Excel's future-value function.</summary>
    /// <param name="percentageRate">Rate per period on the 0–100 scale.</param>
    /// <param name="periods">Nonnegative number of periods.</param>
    /// <param name="periodicPayment">Signed payment made each period.</param>
    /// <param name="presentValue">Signed initial value.</param>
    /// <param name="timing">Payment timing.</param>
    /// <returns>The future value under the opposite-sign cash-flow convention.</returns>
    public static decimal CalculateFutureValueOfAnnuity(decimal percentageRate, int periods, decimal periodicPayment,
        decimal presentValue = 0m, FinancialPaymentTiming timing = FinancialPaymentTiming.EndOfPeriod)
    {
        RequireRate(percentageRate, nameof(percentageRate));
        RequireNonNegative(periods, nameof(periods));
        RequireTiming(timing);
        if (periods == 0) return -presentValue;
        if (percentageRate == 0m) return -presentValue - periodicPayment * periods;
        double rate = (double)(percentageRate / 100m);
        return CheckedDecimal(-((double)presentValue * GrowthFactor(rate, periods) +
            (double)periodicPayment * AnnuityFactor(rate, periods) * PaymentFactor(rate, timing)));
    }

    /// <summary>Calculates the signed present value of an annuity, equivalent to Excel's present-value function.</summary>
    /// <param name="percentageRate">Rate per period on the 0–100 scale.</param>
    /// <param name="periods">Nonnegative number of periods.</param>
    /// <param name="periodicPayment">Signed periodic payment.</param>
    /// <param name="futureValue">Signed desired future value.</param>
    /// <param name="timing">Payment timing.</param>
    /// <returns>The signed present value.</returns>
    public static decimal CalculatePresentValueOfAnnuity(decimal percentageRate, int periods, decimal periodicPayment,
        decimal futureValue = 0m, FinancialPaymentTiming timing = FinancialPaymentTiming.EndOfPeriod)
    {
        RequireRate(percentageRate, nameof(percentageRate));
        RequireNonNegative(periods, nameof(periods));
        RequireTiming(timing);
        if (periods == 0) return -futureValue;
        if (percentageRate == 0m) return -futureValue - periodicPayment * periods;
        double rate = (double)(percentageRate / 100m);
        double discount = Math.Exp(-periods * LogOnePlus(rate));
        return CheckedDecimal(-((double)futureValue * discount +
            (double)periodicPayment * PresentAnnuityFactor(rate, periods) * PaymentFactor(rate, timing)));
    }

    /// <summary>Calculates the fractional number of payments needed to amortize a positive loan.</summary>
    /// <param name="percentageRate">Periodic rate on the 0–100 scale, including zero.</param>
    /// <param name="periodicPayment">Positive payment magnitude.</param>
    /// <param name="presentValue">Nonnegative principal.</param>
    /// <returns>The unrounded number of periods; an impossible amortization throws.</returns>
    public static double CalculateTotalPeriods(decimal percentageRate, decimal periodicPayment, decimal presentValue)
    {
        RequireNonNegative(presentValue, nameof(presentValue));
        RequirePositive(periodicPayment, nameof(periodicPayment));
        RequireRate(percentageRate, nameof(percentageRate));
        if (presentValue == 0m) return 0.0;
        if (percentageRate == 0m) return (double)(presentValue / periodicPayment);
        double rate = (double)(percentageRate / 100m);
        double payment = (double)periodicPayment;
        double principal = (double)presentValue;
        double remaining = payment - principal * rate;
        if (remaining <= 0.0) ThrowHelper.ThrowFinancialPaymentCannotAmortizeLoan(nameof(periodicPayment));
        double denominator = LogOnePlus(rate);
        if (denominator == 0.0) ThrowHelper.ThrowDividedByZeroCalculated();
        return -LogOnePlus(-principal * rate / payment) / denominator;
    }

    /// <summary>Calculates the fractional number of periods of a general signed annuity.</summary>
    /// <param name="percentageRate">Periodic rate on the 0–100 scale.</param>
    /// <param name="periodicPayment">Signed periodic payment.</param>
    /// <param name="presentValue">Signed present value.</param>
    /// <param name="futureValue">Signed future value.</param>
    /// <param name="timing">Payment timing.</param>
    /// <returns>The unrounded number of periods, including fractional periods.</returns>
    public static double CalculateAnnuityPeriods(decimal percentageRate, decimal periodicPayment, decimal presentValue,
        decimal futureValue = 0m, FinancialPaymentTiming timing = FinancialPaymentTiming.EndOfPeriod)
    {
        RequireRate(percentageRate, nameof(percentageRate));
        RequireTiming(timing);
        if (percentageRate == 0m)
        {
            ThrowHelper.ThrowIfZeroToDivideReceived(periodicPayment, nameof(periodicPayment));
            return (double)(-(presentValue + futureValue) / periodicPayment);
        }
        double rate = (double)(percentageRate / 100m);
        double payment = (double)periodicPayment * PaymentFactor(rate, timing);
        double denominator = (double)presentValue * rate + payment;
        if (denominator == 0.0) ThrowHelper.ThrowDividedByZeroCalculated();
        double difference = -rate * ((double)presentValue + (double)futureValue) / denominator;
        if (difference <= -1.0) ThrowHelper.ThrowFinancialPeriodsNotReal();
        return LogOnePlus(difference) / LogOnePlus(rate);
    }

    /// <summary>Calculates the periodic interest rate of a positive, ordinary amortizing loan.</summary>
    /// <param name="periods">Positive number of installments.</param>
    /// <param name="periodicPayment">Positive installment magnitude.</param>
    /// <param name="presentValue">Positive loan principal.</param>
    /// <returns>The periodic interest rate on the 0–100 percentage scale.</returns>
    public static decimal CalculateInterestRate(int periods, decimal periodicPayment, decimal presentValue) =>
        CalculateInterestRateWithDiagnostics(periods, periodicPayment, presentValue).Value;

    /// <summary>Calculates a loan rate and reports its numerical convergence diagnostics.</summary>
    /// <param name="periods">Positive number of installments.</param>
    /// <param name="periodicPayment">Positive installment magnitude.</param>
    /// <param name="presentValue">Positive loan principal.</param>
    /// <returns>The percentage rate, estimated absolute error, iterations and normalized residual.</returns>
    public static FinancialCalculationResult CalculateInterestRateWithDiagnostics(int periods, decimal periodicPayment, decimal presentValue)
    {
        RequirePositive(periods, nameof(periods));
        RequirePositive(periodicPayment, nameof(periodicPayment));
        RequirePositive(presentValue, nameof(presentValue));
        return CalculateAnnuityInterestRateWithDiagnostics(periods, -periodicPayment, presentValue);
    }

    /// <summary>Calculates the periodic rate of a general signed annuity.</summary>
    /// <param name="periods">Positive number of payment periods.</param>
    /// <param name="periodicPayment">Signed periodic payment.</param>
    /// <param name="presentValue">Signed present value.</param>
    /// <param name="futureValue">Signed future value.</param>
    /// <param name="timing">Payment timing.</param>
    /// <param name="percentageGuess">Initial percentage-rate estimate.</param>
    /// <returns>The rate on the 0–100 scale.</returns>
    public static decimal CalculateAnnuityInterestRate(int periods, decimal periodicPayment, decimal presentValue,
        decimal futureValue = 0m, FinancialPaymentTiming timing = FinancialPaymentTiming.EndOfPeriod, decimal percentageGuess = 10m) =>
        CalculateAnnuityInterestRateWithDiagnostics(periods, periodicPayment, presentValue, futureValue, timing, percentageGuess).Value;

    /// <summary>Calculates an annuity rate with a safeguarded numerical solver and diagnostic output.</summary>
    /// <param name="periods">Positive number of payment periods.</param>
    /// <param name="periodicPayment">Signed periodic payment.</param>
    /// <param name="presentValue">Signed present value.</param>
    /// <param name="futureValue">Signed future value.</param>
    /// <param name="timing">Payment timing.</param>
    /// <param name="percentageGuess">Initial percentage-rate estimate, greater than -100.</param>
    /// <returns>The percentage rate and an estimated numerical error, not a certified bound.</returns>
    public static FinancialCalculationResult CalculateAnnuityInterestRateWithDiagnostics(int periods, decimal periodicPayment,
        decimal presentValue, decimal futureValue = 0m, FinancialPaymentTiming timing = FinancialPaymentTiming.EndOfPeriod,
        decimal percentageGuess = 10m)
    {
        RequirePositive(periods, nameof(periods));
        RequireTiming(timing);
        RequireRate(percentageGuess, nameof(percentageGuess));
        if (periodicPayment == 0m && presentValue == 0m && futureValue == 0m) 
            ThrowHelper.ThrowFinancialInterestRateNotUnique();
        double scale = Math.Max(
            Math.Max(
                Math.Abs((double)presentValue), 
                Math.Abs((double)futureValue)
            ), 
            Math.Abs((double)periodicPayment) * periods
        );
        if (scale == 0.0) ThrowHelper.ThrowFinancialCashFlowsRateNotUnique();
        RateContext context = new RateContext
        {
            Kind = RateObjectiveKind.Annuity,
            Periods = periods,
            Payment = (double)periodicPayment / scale,
            PresentValue = (double)presentValue / scale,
            FutureValue = (double)futureValue / scale,
            Timing = timing
        };
        return SolveRate(ref context, (double)(percentageGuess / 100m));
    }

    /// <summary>Calculates the effective annual interest rate from a nominal annual rate.</summary>
    /// <param name="nominalAnnualPercentageRate">Nominal annual rate on the 0–100 scale.</param>
    /// <param name="compoundingPeriodsPerYear">Positive number of capitalization periods per year.</param>
    /// <returns>The effective annual percentage rate.</returns>
    public static decimal CalculateEffectiveAnnualInterestRate(decimal nominalAnnualPercentageRate, int compoundingPeriodsPerYear)
    {
        RequirePositive(compoundingPeriodsPerYear, nameof(compoundingPeriodsPerYear));
        decimal periodicRate = nominalAnnualPercentageRate / compoundingPeriodsPerYear;
        RequireRate(periodicRate, nameof(nominalAnnualPercentageRate));
        if (nominalAnnualPercentageRate == 0m) return 0m;
        if (compoundingPeriodsPerYear == 1) return nominalAnnualPercentageRate;
        return CheckedDecimal(100.0 * ExponentialMinusOne(compoundingPeriodsPerYear * LogOnePlus((double)(periodicRate / 100m))));
    }

    /// <summary>Calculates the nominal annual rate corresponding to an effective annual rate.</summary>
    /// <param name="effectiveAnnualPercentageRate">Effective annual rate on the 0–100 scale.</param>
    /// <param name="compoundingPeriodsPerYear">Positive number of capitalization periods per year.</param>
    /// <returns>The equivalent nominal annual percentage rate.</returns>
    public static decimal CalculateNominalAnnualInterestRate(decimal effectiveAnnualPercentageRate, int compoundingPeriodsPerYear)
    {
        RequirePositive(compoundingPeriodsPerYear, nameof(compoundingPeriodsPerYear));
        RequireRate(effectiveAnnualPercentageRate, nameof(effectiveAnnualPercentageRate));
        if (effectiveAnnualPercentageRate == 0m) return 0m;
        if (compoundingPeriodsPerYear == 1) return effectiveAnnualPercentageRate;
        return CheckedDecimal(100.0 * compoundingPeriodsPerYear *
            ExponentialMinusOne(LogOnePlus((double)(effectiveAnnualPercentageRate / 100m)) / compoundingPeriodsPerYear));
    }

    /// <summary>Calculates the periods required for a principal to reach a target value without contributions.</summary>
    /// <param name="percentageRate">Periodic compound rate, greater than -100 and different from zero.</param>
    /// <param name="presentValue">Positive initial value.</param>
    /// <param name="futureValue">Positive target value.</param>
    /// <returns>The unrounded number of periods; equivalent to Excel's investment-duration function.</returns>
    public static double CalculateInvestmentDuration(decimal percentageRate, decimal presentValue, decimal futureValue)
    {
        RequirePositive(presentValue, nameof(presentValue));
        RequirePositive(futureValue, nameof(futureValue));
        RequireRate(percentageRate, nameof(percentageRate));
        ThrowHelper.ThrowIfZeroToDivideReceived(percentageRate, nameof(percentageRate));
        if (futureValue == presentValue) return 0.0;
        double logarithmicGrowth = Math.Log((double)futureValue) - Math.Log((double)presentValue);
        return logarithmicGrowth / LogOnePlus((double)(percentageRate / 100m));
    }

    /// <summary>Calculates the equivalent periodic compound growth rate between two values.</summary>
    /// <param name="periods">Positive number of periods.</param>
    /// <param name="presentValue">Positive initial value.</param>
    /// <param name="futureValue">Positive final value.</param>
    /// <returns>The periodic percentage rate; equivalent to Excel's rate-of-return-on-investment function.</returns>
    public static decimal CalculateEquivalentGrowthRate(decimal periods, decimal presentValue, decimal futureValue)
    {
        RequirePositive(periods, nameof(periods));
        RequirePositive(presentValue, nameof(presentValue));
        RequirePositive(futureValue, nameof(futureValue));
        if (futureValue == presentValue) return 0m;
        double logarithmicGrowth = Math.Log((double)futureValue) - Math.Log((double)presentValue);
        return CheckedDecimal(100.0 * ExponentialMinusOne(logarithmicGrowth / (double)periods));
    }

    /// <summary>Calculates the future value of a principal under a sequence of different periodic rates.</summary>
    /// <param name="principal">Initial signed principal.</param>
    /// <param name="percentageRates">Rates on the 0–100 scale, applied in sequence.</param>
    /// <returns>The compounded future value; an empty schedule returns the original principal.</returns>
    public static decimal CalculateFutureValueWithVariableRates(decimal principal, ReadOnlySpan<decimal> percentageRates)
    {
        decimal result = principal;
        foreach (decimal percentageRate in percentageRates)
        {
            RequireRate(percentageRate, nameof(percentageRates));
            result *= 1m + percentageRate / 100m;
        }
        return result;
    }

    /// <summary>Rounds an amount explicitly for a currency or accounting posting.</summary>
    /// <param name="amount">The unrounded amount.</param>
    /// <param name="decimalPlaces">Number of decimal places, from zero through 28.</param>
    /// <param name="rounding">The rounding rule required by the business contract.</param>
    /// <returns>The rounded amount. This operation is intentionally separate from financial formulas.</returns>
    public static decimal RoundMonetaryAmount(decimal amount, int decimalPlaces = 2,
        MidpointRounding rounding = MidpointRounding.AwayFromZero) => Math.Round(amount, decimalPlaces, rounding);
}

/// <summary>One installment of a sequential, equal-payment loan amortization.</summary>
public readonly struct FinancialLoanInstallment
{
    /// <summary>One-based installment number.</summary>
    public int Period { get; }
    /// <summary>Outstanding principal before the installment accrues interest.</summary>
    public decimal OpeningBalance { get; }
    /// <summary>Interest accrued during the installment period.</summary>
    public decimal Interest { get; }
    /// <summary>Principal repaid by the installment.</summary>
    public decimal Principal { get; }
    /// <summary>Total amount paid by the borrower.</summary>
    public decimal Payment { get; }
    /// <summary>Outstanding principal after payment.</summary>
    public decimal ClosingBalance { get; }

    internal FinancialLoanInstallment(int period, decimal openingBalance, decimal interest, decimal principal, decimal payment, decimal closingBalance)
    {
        Period = period;
        OpeningBalance = openingBalance;
        Interest = interest;
        Principal = principal;
        Payment = payment;
        ClosingBalance = closingBalance;
    }
}

/// <summary>Allocation-free sequential cursor for a positive, end-of-period amortizing loan.</summary>
/// <remarks>Compute the contractual installment once, then call <see cref="MoveNext"/> in a loop.
/// Each step is O(1), uses decimal arithmetic, and does not recompute powers or allocate objects.
/// This is not a thread-safe shared cursor; keep a separate instance per schedule. Do not copy a
/// mutable cursor and expect the copies to share state. No intermediate monetary rounding is applied.
/// The final installment clears the remaining balance and may differ slightly from the regular amount.
/// If numerical rounding causes an earlier full payoff, the cursor stops at that installment.</remarks>
public struct FinancialLoanSchedule
{
    private readonly decimal _periodicRate;
    private readonly decimal _regularPayment;
    private readonly int _totalPeriods;
    private decimal _balance;
    private int _period;

    /// <summary>Creates a sequential loan amortization cursor.</summary>
    /// <param name="principal">Nonnegative initial principal.</param>
    /// <param name="percentageRate">Nonnegative periodic interest rate on the 0–100 scale.</param>
    /// <param name="periods">Positive number of installments.</param>
    public FinancialLoanSchedule(decimal principal, decimal percentageRate, int periods)
    {
        if (principal < 0m) ThrowHelper.ThrowNegativeValue(nameof(principal), principal);
        if (percentageRate < 0m) ThrowHelper.ThrowNegativeValue(nameof(percentageRate), percentageRate);
        if (periods <= 0) ThrowHelper.ThrowNegativeValue(nameof(periods), periods);
        _periodicRate = percentageRate / 100m;
        _regularPayment = Financials.CalculatePeriodicPayment(percentageRate, periods, principal);
        _totalPeriods = periods;
        _balance = principal;
        _period = 0;
    }

    /// <summary>The contractual regular payment before the final-balance adjustment.</summary>
    public decimal RegularPayment => _regularPayment;
    /// <summary>The number of installments already generated.</summary>
    public int CurrentPeriod => _period;
    /// <summary>The remaining unrounded principal.</summary>
    public decimal RemainingBalance => _balance;

    /// <summary>Advances the loan schedule by one installment.</summary>
    /// <param name="installment">The next installment, or default when the schedule is complete.</param>
    /// <returns>True when an installment was generated; otherwise false.</returns>
    public bool MoveNext(out FinancialLoanInstallment installment)
    {
        if (_period >= _totalPeriods || (_period > 0 && _balance == 0m))
        {
            installment = default(FinancialLoanInstallment);
            return false;
        }
        decimal opening = _balance;
        decimal interest = opening * _periodicRate;
        decimal payment = _regularPayment;
        decimal principal = payment - interest;
        if (_period + 1 == _totalPeriods || principal > opening)
        {
            principal = opening;
            payment = opening + interest;
        }
        _balance = opening - principal;
        _period++;
        installment = new FinancialLoanInstallment(_period, opening, interest, principal, payment, _balance);
        return true;
    }
}

