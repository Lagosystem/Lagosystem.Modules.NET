namespace Lagosystem.Modules.Calc.Financials;

using System.Runtime.CompilerServices;
using Lagosystem.Modules.Helpers;


public static partial class Financials
{
    /// <summary>Calculates the interest portion of a signed annuity payment for a specific period.</summary>
    /// <param name="percentageRate">Periodic rate on the 0–100 scale.</param>
    /// <param name="period">One-based payment period.</param>
    /// <param name="periods">Total number of periods.</param>
    /// <param name="presentValue">Signed initial principal.</param>
    /// <param name="futureValue">Signed desired future balance.</param>
    /// <param name="timing">Payment timing.</param>
    /// <returns>The signed interest portion. For a positive loan it is normally negative.</returns>
    public static decimal CalculatePeriodicInterestPayment(decimal percentageRate, int period, int periods, decimal presentValue,
        decimal futureValue = 0m, FinancialPaymentTiming timing = FinancialPaymentTiming.EndOfPeriod)
    {
        RequirePositive(periods, nameof(periods));
        if (period < 1 || period > periods) throw new ArgumentOutOfRangeException(nameof(period));
        RequireRate(percentageRate, nameof(percentageRate));
        RequireTiming(timing);
        if (percentageRate == 0m || (timing == FinancialPaymentTiming.BeginningOfPeriod && period == 1)) return 0m;
        decimal payment = CalculateAnnuityPayment(percentageRate, periods, presentValue, futureValue, timing);
        decimal previousFutureValue = CalculateFutureValueOfAnnuity(percentageRate, period - 1, payment, presentValue, timing);
        decimal rate = percentageRate / 100m;
        return timing == FinancialPaymentTiming.BeginningOfPeriod
            ? previousFutureValue * rate / (1m + rate)
            : previousFutureValue * rate;
    }

    /// <summary>Calculates the principal portion of a signed annuity payment for one period.</summary>
    /// <param name="percentageRate">Periodic rate on the 0–100 scale.</param>
    /// <param name="period">One-based payment period.</param>
    /// <param name="periods">Total number of periods.</param>
    /// <param name="presentValue">Signed initial principal.</param>
    /// <param name="futureValue">Signed desired future balance.</param>
    /// <param name="timing">Payment timing.</param>
    /// <returns>Periodic payment minus its interest portion.</returns>
    public static decimal CalculatePeriodicPrincipalPayment(decimal percentageRate, int period, int periods, decimal presentValue,
        decimal futureValue = 0m, FinancialPaymentTiming timing = FinancialPaymentTiming.EndOfPeriod)
    {
        decimal payment = CalculateAnnuityPayment(percentageRate, periods, presentValue, futureValue, timing);
        return payment - CalculatePeriodicInterestPayment(percentageRate, period, periods, presentValue, futureValue, timing);
    }

    /// <summary>Calculates the signed cumulative principal repaid between two payment periods.</summary>
    /// <param name="percentageRate">Periodic rate on the 0–100 scale.</param>
    /// <param name="periods">Total number of payment periods.</param>
    /// <param name="presentValue">Signed initial principal.</param>
    /// <param name="startPeriod">First included payment, starting at one.</param>
    /// <param name="endPeriod">Last included payment.</param>
    /// <param name="timing">Payment timing.</param>
    /// <returns>The sum of principal portions, normally negative for a positive loan.</returns>
    public static decimal CalculateCumulativePrincipalPayment(decimal percentageRate, int periods, decimal presentValue,
        int startPeriod, int endPeriod, FinancialPaymentTiming timing = FinancialPaymentTiming.EndOfPeriod)
    {
        ValidatePaymentRange(periods, startPeriod, endPeriod);
        RequireRate(percentageRate, nameof(percentageRate));
        RequireTiming(timing);
        decimal payment = CalculateAnnuityPayment(percentageRate, periods, presentValue, 0m, timing);
        if (startPeriod == 1 && endPeriod == periods) return -presentValue;
        if (percentageRate == 0m) return payment * (endPeriod - startPeriod + 1);
        decimal rate = percentageRate / 100m;
        decimal before = -CalculateFutureValueOfAnnuity(percentageRate, startPeriod - 1, payment, presentValue, timing);
        decimal after = endPeriod == periods ? 0m :
            -CalculateFutureValueOfAnnuity(percentageRate, endPeriod, payment, presentValue, timing);
        if (timing == FinancialPaymentTiming.EndOfPeriod) return after - before;
        decimal growth = 1m + rate;
        if (startPeriod > 1) return (after - before) / growth;
        if (endPeriod == 1) return payment;
        decimal afterFirstPeriod = -CalculateFutureValueOfAnnuity(percentageRate, 1, payment, presentValue, timing);
        return payment + (after - afterFirstPeriod) / growth;
    }

    /// <summary>Calculates the signed cumulative interest paid between two payment periods.</summary>
    /// <param name="percentageRate">Periodic rate on the 0–100 scale.</param>
    /// <param name="periods">Total number of payment periods.</param>
    /// <param name="presentValue">Signed initial principal.</param>
    /// <param name="startPeriod">First included payment, starting at one.</param>
    /// <param name="endPeriod">Last included payment.</param>
    /// <param name="timing">Payment timing.</param>
    /// <returns>The sum of interest portions, normally negative for a positive loan.</returns>
    public static decimal CalculateCumulativeInterestPayment(decimal percentageRate, int periods, decimal presentValue,
        int startPeriod, int endPeriod, FinancialPaymentTiming timing = FinancialPaymentTiming.EndOfPeriod)
    {
        ValidatePaymentRange(periods, startPeriod, endPeriod);
        decimal payment = CalculateAnnuityPayment(percentageRate, periods, presentValue, 0m, timing);
        decimal principal = CalculateCumulativePrincipalPayment(percentageRate, periods, presentValue, startPeriod, endPeriod, timing);
        return payment * (endPeriod - startPeriod + 1) - principal;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ValidatePaymentRange(int periods, int startPeriod, int endPeriod)
    {
        RequirePositive(periods, nameof(periods));
        if (startPeriod < 1 || endPeriod < startPeriod || endPeriod > periods)
            throw new ArgumentOutOfRangeException(nameof(startPeriod), "The payment range must be within the loan term.");
    }

    /// <summary>Calculates the simple-interest portion of an equal-principal loan payment.</summary>
    /// <remarks>Equivalent to Excel's ISPMT. Unlike an amortizing annuity, principal is repaid evenly.
    /// The period index is zero-based, as in Excel.</remarks>
    /// <param name="percentageRate">Periodic interest rate on the 0–100 scale.</param>
    /// <param name="period">Zero-based period index.</param>
    /// <param name="periods">Positive total number of periods.</param>
    /// <param name="presentValue">Signed initial principal.</param>
    /// <returns>The signed interest payment.</returns>
    public static decimal CalculateEqualPrincipalInterestPayment(decimal percentageRate, int period, int periods, decimal presentValue)
    {
        RequirePositive(periods, nameof(periods));
        if (period < 0 || period >= periods) throw new ArgumentOutOfRangeException(nameof(period));
        return -presentValue * (percentageRate / 100m) * (periods - period) / periods;
    }

    /// <summary>Calculates the net present value of equally spaced end-of-period cash flows.</summary>
    /// <remarks>Equivalent to Excel's NPV/VPL. The first supplied cash flow is at the end of period one,
    /// not at time zero. The reverse Horner evaluation avoids a power operation for every element.</remarks>
    /// <param name="percentageRate">Discount rate per period on the 0–100 scale, greater than -100.</param>
    /// <param name="cashFlows">Signed cash flows at the ends of periods one, two, and so on.</param>
    /// <returns>The discounted sum. An empty sequence returns zero.</returns>
    public static decimal CalculateNetPresentValue(decimal percentageRate, ReadOnlySpan<decimal> cashFlows)
    {
        RequireRate(percentageRate, nameof(percentageRate));
        if (percentageRate == 0m)
        {
            decimal exact = 0m;
            foreach (decimal cashFlow in cashFlows) exact += cashFlow;
            return exact;
        }
        decimal discount = 1m / (1m + percentageRate / 100m);
        decimal result = 0m;
        for (int index = cashFlows.Length - 1; index >= 0; index--)
            result = (result + cashFlows[index]) * discount;
        return result;
    }

    /// <summary>Calculates net present value including an initial cash flow at time zero.</summary>
    /// <param name="percentageRate">Periodic discount rate on the 0–100 scale.</param>
    /// <param name="initialCashFlow">Signed immediate cash flow, such as a negative investment.</param>
    /// <param name="futureCashFlows">Signed end-of-period cash flows beginning at period one.</param>
    /// <returns>The initial cash flow plus the discounted future cash flows.</returns>
    public static decimal CalculateNetPresentValueIncludingInitialCashFlow(decimal percentageRate, decimal initialCashFlow,
        ReadOnlySpan<decimal> futureCashFlows) => initialCashFlow + CalculateNetPresentValue(percentageRate, futureCashFlows);

    /// <summary>Calculates the internal rate of return of equally spaced cash flows.</summary>
    /// <param name="cashFlows">Signed cash flows beginning at time zero, containing both positive and negative amounts.</param>
    /// <param name="percentageGuess">Initial percentage-rate estimate; different guesses can find different roots.</param>
    /// <returns>The periodic internal rate of return on the 0–100 scale.</returns>
    public static decimal CalculateInternalRateOfReturn(ReadOnlySpan<decimal> cashFlows, decimal percentageGuess = 10m) =>
        CalculateInternalRateOfReturnWithDiagnostics(cashFlows, percentageGuess).Value;

    /// <summary>Calculates periodic internal rate of return with convergence diagnostics.</summary>
    /// <param name="cashFlows">Signed cash flows beginning at time zero.</param>
    /// <param name="percentageGuess">Initial percentage-rate estimate, greater than -100.</param>
    /// <returns>The percentage rate and an estimated numerical error.</returns>
    public static FinancialCalculationResult CalculateInternalRateOfReturnWithDiagnostics(ReadOnlySpan<decimal> cashFlows,
        decimal percentageGuess = 10m)
    {
        RequireRate(percentageGuess, nameof(percentageGuess));
        ValidateCashFlowSigns(cashFlows);
        RateContext context = new RateContext { Kind = RateObjectiveKind.PeriodicCashFlows, CashFlows = cashFlows,
            Scale = CalculateScale(cashFlows) };
        return SolveRate(ref context, (double)(percentageGuess / 100m));
    }

    /// <summary>Calculates the modified internal rate of return using separate financing and reinvestment rates.</summary>
    /// <param name="cashFlows">Signed cash flows beginning at time zero, containing both signs.</param>
    /// <param name="financePercentageRate">Rate used to discount negative cash flows.</param>
    /// <param name="reinvestmentPercentageRate">Rate used to compound positive cash flows.</param>
    /// <returns>The periodic modified internal rate of return on the 0–100 scale.</returns>
    public static decimal CalculateModifiedInternalRateOfReturn(ReadOnlySpan<decimal> cashFlows,
        decimal financePercentageRate, decimal reinvestmentPercentageRate)
    {
        ValidateCashFlowSigns(cashFlows);
        RequireRate(financePercentageRate, nameof(financePercentageRate));
        RequireRate(reinvestmentPercentageRate, nameof(reinvestmentPercentageRate));
        decimal negativePresentValue = 0m;
        decimal positiveFutureValue = 0m;
        decimal financeDiscount = 1m / (1m + financePercentageRate / 100m);
        decimal reinvestmentGrowth = 1m + reinvestmentPercentageRate / 100m;
        for (int index = cashFlows.Length - 1; index >= 0; index--)
        {
            decimal cashFlow = cashFlows[index];
            negativePresentValue = negativePresentValue * financeDiscount + (cashFlow < 0m ? cashFlow : 0m);
        }
        // Fluxos positivos são capitalizados até o último período, em ordem cronológica.
        foreach (decimal cashFlow in cashFlows)
        {
            positiveFutureValue = positiveFutureValue * reinvestmentGrowth + (cashFlow > 0m ? cashFlow : 0m);
        }
        if (negativePresentValue == 0m) ThrowHelper.ThrowDividedByZeroCalculated();
        return CheckedDecimal(100.0 * ExponentialMinusOne(
            (Math.Log((double)positiveFutureValue) - Math.Log((double)-negativePresentValue)) / (cashFlows.Length - 1)));
    }

    /// <summary>Calculates the net present value of cash flows occurring on irregular dates.</summary>
    /// <remarks>Equivalent to Excel's XNPV/XVPL. Uses actual elapsed days divided by 365,
    /// with the first date as the valuation date. Dates need not be sorted but cannot precede the first date.</remarks>
    /// <param name="annualPercentageRate">Annual discount rate on the 0–100 scale, greater than -100.</param>
    /// <param name="cashFlows">Signed cash flows, including the initial cash flow.</param>
    /// <param name="dates">Corresponding dates, with the first date as the origin.</param>
    /// <returns>The discounted sum without monetary rounding.</returns>
    public static decimal CalculateNetPresentValueWithDates(decimal annualPercentageRate, ReadOnlySpan<decimal> cashFlows,
        ReadOnlySpan<DateTime> dates)
    {
        RequireRate(annualPercentageRate, nameof(annualPercentageRate));
        ValidateDatedCashFlows(cashFlows, dates);
        if (annualPercentageRate == 0m)
        {
            decimal exact = 0m;
            foreach (decimal cashFlow in cashFlows) exact += cashFlow;
            return exact;
        }
        double logarithmicRate = LogOnePlus((double)(annualPercentageRate / 100m));
        double sum = 0.0;
        double compensation = 0.0;
        DateTime origin = dates[0].Date;
        for (int index = 0; index < cashFlows.Length; index++)
        {
            double years = (dates[index].Date - origin).Days / 365.0;
            double term = (double)cashFlows[index] * Math.Exp(-years * logarithmicRate);
            double adjusted = term - compensation;
            double next = sum + adjusted;
            compensation = (next - sum) - adjusted;
            sum = next;
        }
        return CheckedDecimal(sum);
    }

    /// <summary>Calculates the annual internal rate of return of irregularly dated cash flows.</summary>
    /// <param name="cashFlows">Signed cash flows containing both positive and negative amounts.</param>
    /// <param name="dates">Corresponding dates, with the first date as the origin.</param>
    /// <param name="percentageGuess">Initial annual percentage-rate estimate.</param>
    /// <returns>The annual percentage rate using an actual/365 year.</returns>
    public static decimal CalculateInternalRateOfReturnWithDates(ReadOnlySpan<decimal> cashFlows,
        ReadOnlySpan<DateTime> dates, decimal percentageGuess = 10m) =>
        CalculateInternalRateOfReturnWithDatesWithDiagnostics(cashFlows, dates, percentageGuess).Value;

    /// <summary>Calculates dated internal rate of return with numerical diagnostics.</summary>
    /// <param name="cashFlows">Signed cash flows containing both signs.</param>
    /// <param name="dates">Corresponding dates, with the first date as the origin.</param>
    /// <param name="percentageGuess">Initial annual percentage-rate estimate.</param>
    /// <returns>The annual percentage rate, estimated error, iterations and normalized residual.</returns>
    public static FinancialCalculationResult CalculateInternalRateOfReturnWithDatesWithDiagnostics(ReadOnlySpan<decimal> cashFlows,
        ReadOnlySpan<DateTime> dates, decimal percentageGuess = 10m)
    {
        RequireRate(percentageGuess, nameof(percentageGuess));
        ValidateDatedCashFlows(cashFlows, dates);
        ValidateCashFlowSigns(cashFlows);
        bool hasLaterDate = false;
        for (int index = 1; index < dates.Length; index++)
            if (dates[index].Date > dates[0].Date) { hasLaterDate = true; break; }
        if (!hasLaterDate) throw new ArgumentException("At least one cash flow must occur after the first date.", nameof(dates));
        RateContext context = new RateContext { Kind = RateObjectiveKind.DatedCashFlows, CashFlows = cashFlows,
            Dates = dates, Scale = CalculateScale(cashFlows) };
        return SolveRate(ref context, (double)(percentageGuess / 100m));
    }

    private static void ValidateCashFlowSigns(ReadOnlySpan<decimal> cashFlows)
    {
        if (cashFlows.Length < 2) throw new ArgumentException("At least two cash flows are required.", nameof(cashFlows));
        bool positive = false;
        bool negative = false;
        foreach (decimal cashFlow in cashFlows)
        {
            positive |= cashFlow > 0m;
            negative |= cashFlow < 0m;
        }
        if (!positive || !negative) throw new ArgumentException("Cash flows must contain both positive and negative amounts.", nameof(cashFlows));
    }

    private static void ValidateDatedCashFlows(ReadOnlySpan<decimal> cashFlows, ReadOnlySpan<DateTime> dates)
    {
        if (cashFlows.IsEmpty || dates.Length != cashFlows.Length)
            throw new ArgumentException("Cash flows and dates must be nonempty and have the same length.", nameof(dates));
        DateTime origin = dates[0].Date;
        for (int index = 1; index < dates.Length; index++)
            if (dates[index].Date < origin) throw new ArgumentException("Dates cannot precede the first date.", nameof(dates));
    }

    private static double CalculateScale(ReadOnlySpan<decimal> cashFlows)
    {
        double scale = 0.0;
        foreach (decimal cashFlow in cashFlows) scale += Math.Abs((double)cashFlow);
        return scale;
    }

    private enum RateObjectiveKind { Annuity, PeriodicCashFlows, DatedCashFlows }

    // Ref struct: spans remain on the stack; no closures, delegates, LINQ, or per-iteration arrays.
    private ref struct RateContext
    {
        public RateObjectiveKind Kind;
        public ReadOnlySpan<decimal> CashFlows;
        public ReadOnlySpan<DateTime> Dates;
        public double Scale;
        public double Periods;
        public double Payment;
        public double PresentValue;
        public double FutureValue;
        public FinancialPaymentTiming Timing;

        public double Evaluate(double logarithmicRate, out double derivative)
        {
            double rate = ExponentialMinusOne(logarithmicRate);
            if (Kind == RateObjectiveKind.Annuity)
            {
                double discount = Math.Exp(-Periods * logarithmicRate);
                double factor;
                double factorDerivative;
                if (Math.Abs(rate) * Periods < 1e-4)
                {
                    double n = Periods;
                    double first = -n * (n + 1.0) / 2.0;
                    double second = n * (n + 1.0) * (n + 2.0) / 6.0;
                    double third = -n * (n + 1.0) * (n + 2.0) * (n + 3.0) / 24.0;
                    factor = n + rate * (first + rate * (second + rate * third));
                    factorDerivative = (1.0 + rate) * (first + rate * (2.0 * second + 3.0 * rate * third));
                }
                else
                {
                    factor = -ExponentialMinusOne(-Periods * logarithmicRate) / rate;
                    factorDerivative = (Periods * discount - factor * (1.0 + rate)) / rate;
                }
                double timingFactor = PaymentFactor(rate, Timing);
                derivative = -Periods * FutureValue * discount + Payment *
                    (factorDerivative * timingFactor + (Timing == FinancialPaymentTiming.BeginningOfPeriod ? factor * (1.0 + rate) : 0.0));
                return PresentValue + FutureValue * discount + Payment * factor * timingFactor;
            }
            if (Kind == RateObjectiveKind.PeriodicCashFlows)
            {
                double discount = Math.Exp(-logarithmicRate);
                double value = (double)CashFlows[CashFlows.Length - 1] / Scale;
                double derivativeByDiscount = 0.0;
                for (int index = CashFlows.Length - 2; index >= 0; index--)
                {
                    derivativeByDiscount = derivativeByDiscount * discount + value;
                    value = value * discount + (double)CashFlows[index] / Scale;
                }
                derivative = -discount * derivativeByDiscount;
                return value;
            }
            double sum = 0.0;
            double slope = 0.0;
            DateTime origin = Dates[0].Date;
            for (int index = 0; index < CashFlows.Length; index++)
            {
                double years = (Dates[index].Date - origin).Days / 365.0;
                double term = ((double)CashFlows[index] / Scale) * Math.Exp(-years * logarithmicRate);
                sum += term;
                slope -= years * term;
            }
            derivative = slope;
            return sum;
        }
    }

    private static FinancialCalculationResult SolveRate(ref RateContext context, double guess)
    {
        double initial = LogOnePlus(guess);
        double initialValue = context.Evaluate(initial, out double initialDerivative);
        if (!double.IsFinite(initialValue)) throw new ArithmeticException("The initial rate produces a non-finite objective.");
        if (initialValue == 0.0)
            return CreateRateResult(initial, 0.0, 0, 0.0);
        if (Math.Abs(initialValue) <= 1e-14 && double.IsFinite(initialDerivative) && Math.Abs(initialDerivative) > 1e-20)
        {
            double estimatedError = Math.Abs(initialValue / initialDerivative);
            if (estimatedError <= DefaultRateTolerance * (1.0 + Math.Abs(initial)))
                return CreateRateResult(initial, estimatedError, 0, Math.Abs(initialValue));
        }

        // Search in log(1 + rate) space to preserve the rate > -100% domain.
        double lowerLimit = -32.0;
        double upperLimit = 32.0;
        double lower = initial;
        double upper = initial;
        double lowerValue = initialValue;
        double upperValue = initialValue;
        double step = 0.05;
        bool bracketed = false;
        for (int index = 0; index < 80 && !bracketed; index++)
        {
            double nextLower = Math.Max(lowerLimit, initial - step);
            double nextUpper = Math.Min(upperLimit, initial + step);
            if (nextLower < lower)
            {
                double value = context.Evaluate(nextLower, out _);
                if (double.IsFinite(value))
                {
                    if (value == 0.0) return CreateRateResult(nextLower, 0.0, index + 1, 0.0);
                    if (OppositeSigns(value, lowerValue))
                    {
                        upper = lower;
                        upperValue = lowerValue;
                        lower = nextLower;
                        lowerValue = value;
                        bracketed = true;
                        break;
                    }
                    lower = nextLower;
                    lowerValue = value;
                }
            }
            if (nextUpper > upper)
            {
                double value = context.Evaluate(nextUpper, out _);
                if (double.IsFinite(value))
                {
                    if (value == 0.0) return CreateRateResult(nextUpper, 0.0, index + 1, 0.0);
                    if (OppositeSigns(value, upperValue))
                    {
                        lower = upper;
                        lowerValue = upperValue;
                        upper = nextUpper;
                        upperValue = value;
                        bracketed = true;
                        break;
                    }
                    upper = nextUpper;
                    upperValue = value;
                }
            }
            if (nextLower == lowerLimit && nextUpper == upperLimit) break;
            step *= 1.65;
        }
        if (!bracketed) throw new ArithmeticException("No rate root could be bracketed within the supported numerical domain.");

        double current = Math.Clamp(initial, lower, upper);
        if (current == lower || current == upper) current = lower + (upper - lower) / 2.0;
        for (int iteration = 1; iteration <= DefaultMaximumIterations; iteration++)
        {
            double value = context.Evaluate(current, out double derivative);
            if (!double.IsFinite(value))
            {
                current = lower + (upper - lower) / 2.0;
                continue;
            }
            if (value == 0.0) return CreateRateResult(current, 0.0, iteration, 0.0);
            if (OppositeSigns(value, lowerValue))
            {
                upper = current;
                upperValue = value;
            }
            else
            {
                lower = current;
                lowerValue = value;
            }
            double width = upper - lower;
            if (width <= DefaultRateTolerance * (1.0 + Math.Abs(current)))
            {
                double midpoint = lower + width / 2.0;
                double residual = Math.Abs(context.Evaluate(midpoint, out _));
                return CreateRateResult(midpoint, width * Math.Exp(midpoint) / 2.0, iteration, residual);
            }
            double next = double.NaN;
            if (double.IsFinite(derivative) && derivative != 0.0)
                next = current - value / derivative;
            double margin = width * 0.05;
            if (!double.IsFinite(next) || next <= lower + margin || next >= upper - margin)
                next = lower + width / 2.0;
            if (next == current) next = lower + width / 2.0;
            current = next;
        }
        throw new ArithmeticException("The financial rate solver failed to converge.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool OppositeSigns(double left, double right) =>
        (left < 0.0 && right >= 0.0) || (left > 0.0 && right <= 0.0);

    private static FinancialCalculationResult CreateRateResult(double logarithmicRate, double estimatedRateError,
        int iterations, double residual)
    {
        double percentage = 100.0 * ExponentialMinusOne(logarithmicRate);
        double error = 100.0 * Math.Abs(estimatedRateError) + 8.0 * 2.2204460492503131e-16 * Math.Max(1.0, Math.Abs(percentage));
        return new FinancialCalculationResult(CheckedDecimal(percentage), CheckedDecimal(error), iterations, residual);
    }
}
