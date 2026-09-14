
namespace Lagosystem.Modules.Calc.Financials;

using System.Runtime.CompilerServices;
using Lagosystem.Modules.Helpers;

/// <summary>Number of coupon payments made each year.</summary>
public enum FinancialCouponFrequency
{
    /// <summary>One coupon per year.</summary>
    Annual = 1,
    /// <summary>Two coupons per year.</summary>
    Semiannual = 2,
    /// <summary>Four coupons per year.</summary>
    Quarterly = 4
}

/// <summary>Day-count conventions used in security calculations.</summary>
public enum FinancialDayCountBasis
{
    /// <summary>United States NASD 30/360, including the February-end rules.</summary>
    UnitedStatesThirtyThreeSixty = 0,
    /// <summary>Actual days and the applicable actual-year denominator.</summary>
    ActualActual = 1,
    /// <summary>Actual days divided by 360.</summary>
    ActualThreeSixty = 2,
    /// <summary>Actual days divided by 365.</summary>
    ActualThreeSixtyFive = 3,
    /// <summary>European 30E/360.</summary>
    EuropeanThirtyThreeSixty = 4
}

public static partial class Financials
{
    private const int MaximumSecurityCouponCount = 40000;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double Fraction(decimal percentage) => (double)(percentage / 100m);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static decimal Percentage(double fraction) => CheckedDecimal(fraction * 100.0);

    private static int ConvertFrequency(FinancialCouponFrequency frequency)
    {
        if (frequency is not (FinancialCouponFrequency.Annual or FinancialCouponFrequency.Semiannual or FinancialCouponFrequency.Quarterly))
            throw new ArgumentOutOfRangeException(nameof(frequency));
        return (int)frequency;
    }

    private static void ConvertBasis(FinancialDayCountBasis basis)
    {
        if (basis < FinancialDayCountBasis.UnitedStatesThirtyThreeSixty ||
            basis > FinancialDayCountBasis.EuropeanThirtyThreeSixty)
            throw new ArgumentOutOfRangeException(nameof(basis));
    }

    private static void ValidateSecurityDates(DateTime settlement, DateTime maturity)
    {
        if (settlement.Date < maturity.Date) return;
        if (settlement.Date == maturity.Date) ThrowHelper.ThrowDividedByZeroCalculated();
        throw new ArgumentOutOfRangeException(nameof(maturity), "Maturity must follow settlement.");
    }

    private static void ValidateTreasuryBillDates(DateTime settlement, DateTime maturity)
    {
        ValidateSecurityDates(settlement, maturity);
        if ((maturity.Date - settlement.Date).Days > 365)
            throw new ArgumentOutOfRangeException(nameof(maturity), "Treasury bill maturity must be within 365 days.");
    }

    private static void ValidateSecurityAmounts(decimal price, decimal redemptionValue)
    {
        RequirePositive(price, nameof(price));
        RequirePositive(redemptionValue, nameof(redemptionValue));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsLastDayOfFebruary(DateTime date) =>
        date.Month == 2 && date.Day == DateTime.DaysInMonth(date.Year, 2);

    private static int CountThirtyThreeSixtyDays(DateTime startDate, DateTime endDate, bool european)
    {
        int startDay = startDate.Day;
        int endDay = endDate.Day;
        int endMonth = endDate.Month;
        int endYear = endDate.Year;
        if (european)
        {
            startDay = Math.Min(startDay, 30);
            endDay = Math.Min(endDay, 30);
        }
        else
        {
            bool startFebruaryEnd = IsLastDayOfFebruary(startDate);
            if (startFebruaryEnd && IsLastDayOfFebruary(endDate)) endDay = 30;
            if (startDay == 31 || startFebruaryEnd) startDay = 30;
            if (endDay == 31)
            {
                if (startDay >= 30) endDay = 30;
                else
                {
                    endDay = 1;
                    endMonth++;
                    if (endMonth == 13) { endMonth = 1; endYear++; }
                }
            }
        }
        return 360 * (endYear - startDate.Year) + 30 * (endMonth - startDate.Month) + endDay - startDay;
    }

    private static int CountDays(DateTime startDate, DateTime endDate, FinancialDayCountBasis basis)
    {
        if (basis == FinancialDayCountBasis.UnitedStatesThirtyThreeSixty)
            return CountThirtyThreeSixtyDays(startDate, endDate, false);
        if (basis == FinancialDayCountBasis.EuropeanThirtyThreeSixty)
            return CountThirtyThreeSixtyDays(startDate, endDate, true);
        return (endDate - startDate).Days;
    }

    private static bool ContainsLeapDay(DateTime startDate, DateTime endDate)
    {
        for (int year = startDate.Year; year <= endDate.Year; year++)
        {
            if (!DateTime.IsLeapYear(year)) continue;
            DateTime leapDay = new DateTime(year, 2, 29);
            if (startDate <= leapDay && leapDay < endDate) return true;
        }
        return false;
    }

    /// <summary>Calculates a signed fraction of a year using the selected day-count convention.</summary>
    /// <remarks>For Actual/Actual, uses the Excel-style average-year convention for multi-year
    /// intervals. For a short interval the denominator is 365 or 366 as applicable. This is
    /// not necessarily the same as Actual/Actual ISDA, ICMA, or the day-count rules of a Brazilian title.
    /// Dates are treated as calendar dates; time-of-day and time zones are ignored.</remarks>
    /// <param name="startDate">Beginning calendar date.</param>
    /// <param name="endDate">Ending calendar date.</param>
    /// <param name="basis">Selected day-count convention.</param>
    /// <returns>The signed year fraction; equal dates return zero.</returns>
    public static decimal CalculateYearFraction(DateTime startDate, DateTime endDate,
        FinancialDayCountBasis basis = FinancialDayCountBasis.UnitedStatesThirtyThreeSixty)
    {
        ConvertBasis(basis);
        DateTime start = startDate.Date;
        DateTime end = endDate.Date;
        if (start == end) return 0m;
        if (start > end) return -CalculateYearFraction(end, start, basis);
        int days = CountDays(start, end, basis);
        switch (basis)
        {
            case FinancialDayCountBasis.UnitedStatesThirtyThreeSixty:
            case FinancialDayCountBasis.ActualThreeSixty:
            case FinancialDayCountBasis.EuropeanThirtyThreeSixty:
                return days / 360m;
            case FinancialDayCountBasis.ActualThreeSixtyFive:
                return days / 365m;
            default:
                bool shortInterval = end.Year == start.Year ||
                    (end.Year == start.Year + 1 && end <= start.AddYears(1));
                if (shortInterval)
                {
                    int denominator = DateTime.IsLeapYear(start.Year) && end.Year == start.Year ||
                        ContainsLeapDay(start, end) ? 366 : 365;
                    return days / (decimal)denominator;
                }
                int years = end.Year - start.Year + 1;
                int totalYearDays = 365 * years;
                for (int year = start.Year; year <= end.Year; year++)
                    if (DateTime.IsLeapYear(year)) totalYearDays++;
                return (decimal)days * years / totalYearDays;
        }
    }

    // Anchor every date to the original coupon day. DateTime.AddMonths alone drifts after February.
    private static DateTime AddMonthsAnchored(DateTime anchor, int months)
    {
        long index = (long)(anchor.Year - 1) * 12 + anchor.Month - 1 + months;
        if (index < 0 || index >= 9999L * 12) throw new ArgumentOutOfRangeException(nameof(months));
        int year = (int)(index / 12) + 1;
        int month = (int)(index % 12) + 1;
        int day = anchor.Day == DateTime.DaysInMonth(anchor.Year, anchor.Month)
            ? DateTime.DaysInMonth(year, month)
            : Math.Min(anchor.Day, DateTime.DaysInMonth(year, month));
        return new DateTime(year, month, day);
    }

    private static void FindAnchoredPeriod(DateTime date, DateTime anchor, int months,
        out DateTime previous, out DateTime next, out int index)
    {
        int monthDifference = (date.Year - anchor.Year) * 12 + date.Month - anchor.Month;
        index = monthDifference / months;
        previous = AddMonthsAnchored(anchor, checked(index * months));
        if (previous > date)
        {
            index--;
            previous = AddMonthsAnchored(anchor, checked(index * months));
        }
        next = AddMonthsAnchored(anchor, checked((index + 1) * months));
        if (next <= date)
        {
            index++;
            previous = next;
            next = AddMonthsAnchored(anchor, checked((index + 1) * months));
        }
    }

    private static decimal CouponPeriodDays(DateTime previous, DateTime next, int frequency, FinancialDayCountBasis basis)
    {
        if (basis == FinancialDayCountBasis.ActualThreeSixtyFive) return 365m / frequency;
        if (basis is FinancialDayCountBasis.UnitedStatesThirtyThreeSixty or
            FinancialDayCountBasis.ActualThreeSixty or FinancialDayCountBasis.EuropeanThirtyThreeSixty)
            return 360m / frequency;
        return (next - previous).Days;
    }

    private static void GetCouponWindow(DateTime settlement, DateTime maturity, int frequency,
        out DateTime previous, out DateTime next, out int count)
    {
        int months = 12 / frequency;
        FindAnchoredPeriod(settlement, maturity, months, out previous, out next, out int index);
        count = -index;
        if (count < 1 || count > MaximumSecurityCouponCount)
            throw new ArgumentOutOfRangeException(nameof(maturity), "The coupon schedule exceeds the supported range.");
    }

    private static decimal CouponFraction(DateTime startDate, DateTime endDate, DateTime anchor,
        int frequency, FinancialDayCountBasis basis)
    {
        if (startDate >= endDate) return 0m;
        int months = 12 / frequency;
        FindAnchoredPeriod(startDate, anchor, months, out DateTime previous, out DateTime next, out int periodIndex);
        DateTime current = startDate;
        decimal result = 0m;
        for (int count = 0; current < endDate; count++)
        {
            if (count > MaximumSecurityCouponCount)
                throw new ArgumentOutOfRangeException(nameof(endDate), "The coupon schedule exceeds the supported range.");
            DateTime segmentEnd = next < endDate ? next : endDate;
            decimal denominator = CouponPeriodDays(previous, next, frequency, basis);
            ThrowHelper.ThrowIfZeroToDivideCalculated(denominator);
            result += CountDays(current, segmentEnd, basis) / denominator;
            current = segmentEnd;
            previous = next;
            if (current < endDate)
            {
                periodIndex++;
                next = AddMonthsAnchored(anchor, checked((periodIndex + 1) * months));
            }
        }
        return result;
    }

    private static void GetCouponDays(DateTime settlement, DateTime maturity, FinancialCouponFrequency frequency,
        FinancialDayCountBasis basis, out decimal elapsed, out decimal remaining, out decimal total,
        out DateTime previous, out DateTime next, out int count)
    {
        ValidateSecurityDates(settlement, maturity);
        int payments = ConvertFrequency(frequency);
        ConvertBasis(basis);
        GetCouponWindow(settlement.Date, maturity.Date, payments, out previous, out next, out count);
        total = CouponPeriodDays(previous, next, payments, basis);
        ThrowHelper.ThrowIfZeroToDivideCalculated(total);
        elapsed = CountDays(previous, settlement.Date, basis);
        remaining = CountDays(settlement.Date, next, basis);
    }

    /// <summary>Calculates accrued coupon interest from issue or the first interest date.</summary>
    /// <remarks>Interest is simple, using coupon-period fractions and the selected day-count basis.
    /// A long initial coupon is split into nominal quasi-coupon periods. No cash is posted or rounded.</remarks>
    /// <param name="issue">Original issue date.</param>
    /// <param name="firstInterest">First coupon date, later than issue.</param>
    /// <param name="settlement">Valuation date, later than issue.</param>
    /// <param name="annualCouponPercentageRate">Nonnegative annual coupon percentage.</param>
    /// <param name="faceValue">Positive face value.</param>
    /// <param name="frequency">Annual coupon frequency.</param>
    /// <param name="basis">Day-count convention.</param>
    /// <param name="fromIssueDate">True to accrue from issue; false to start at firstInterest when it precedes settlement.</param>
    /// <returns>The accrued monetary interest.</returns>
    public static decimal CalculateAccruedInterest(DateTime issue, DateTime firstInterest, DateTime settlement,
        decimal annualCouponPercentageRate, decimal faceValue,
        FinancialCouponFrequency frequency = FinancialCouponFrequency.Semiannual,
        FinancialDayCountBasis basis = FinancialDayCountBasis.UnitedStatesThirtyThreeSixty,
        bool fromIssueDate = true)
    {
        RequirePositive(faceValue, nameof(faceValue));
        RequireNonNegative(annualCouponPercentageRate, nameof(annualCouponPercentageRate));
        int payments = ConvertFrequency(frequency);
        ConvertBasis(basis);
        if (issue.Date >= settlement.Date || firstInterest.Date <= issue.Date)
            throw new ArgumentOutOfRangeException(nameof(firstInterest));
        DateTime start = !fromIssueDate && firstInterest.Date < settlement.Date ? firstInterest.Date : issue.Date;
        return faceValue * annualCouponPercentageRate / (100m * payments) *
            CouponFraction(start, settlement.Date, firstInterest.Date, payments, basis);
    }

    /// <summary>Calculates simple accrued interest on a security that pays interest at maturity.</summary>
    /// <param name="issue">Issue date.</param>
    /// <param name="settlement">Accrual end date.</param>
    /// <param name="annualPercentageRate">Nonnegative annual percentage rate.</param>
    /// <param name="faceValue">Positive face value.</param>
    /// <param name="basis">Day-count convention.</param>
    /// <returns>Face value times the annual rate and year fraction.</returns>
    public static decimal CalculateAccruedInterestAtMaturity(DateTime issue, DateTime settlement,
        decimal annualPercentageRate, decimal faceValue,
        FinancialDayCountBasis basis = FinancialDayCountBasis.UnitedStatesThirtyThreeSixty)
    {
        ValidateSecurityDates(issue, settlement);
        RequirePositive(faceValue, nameof(faceValue));
        RequireNonNegative(annualPercentageRate, nameof(annualPercentageRate));
        return faceValue * annualPercentageRate / 100m * CalculateYearFraction(issue, settlement, basis);
    }

    /// <summary>Returns the nominal number of days in the coupon period containing settlement.</summary>
    /// <param name="settlement">Settlement date.</param><param name="maturity">Maturity date.</param>
    /// <param name="frequency">Annual coupon frequency.</param><param name="basis">Day-count convention.</param>
    /// <returns>Actual period days for Actual/Actual; otherwise 360/frequency or 365/frequency.</returns>
    public static decimal CalculateCouponPeriodDays(DateTime settlement, DateTime maturity,
        FinancialCouponFrequency frequency = FinancialCouponFrequency.Semiannual,
        FinancialDayCountBasis basis = FinancialDayCountBasis.UnitedStatesThirtyThreeSixty)
    {
        GetCouponDays(settlement, maturity, frequency, basis, out _, out _, out decimal total, out _, out _, out _);
        return total;
    }

    /// <summary>Counts days from the previous coupon date to settlement.</summary>
    /// <param name="settlement">Settlement date.</param><param name="maturity">Maturity date.</param>
    /// <param name="frequency">Annual coupon frequency.</param><param name="basis">Day-count convention.</param>
    /// <returns>Elapsed coupon days.</returns>
    public static decimal CalculateCouponDaysFromPeriodStart(DateTime settlement, DateTime maturity,
        FinancialCouponFrequency frequency = FinancialCouponFrequency.Semiannual,
        FinancialDayCountBasis basis = FinancialDayCountBasis.UnitedStatesThirtyThreeSixty)
    {
        GetCouponDays(settlement, maturity, frequency, basis, out decimal elapsed, out _, out _, out _, out _, out _);
        return elapsed;
    }

    /// <summary>Counts days from settlement to the next coupon payment.</summary>
    /// <param name="settlement">Settlement date.</param><param name="maturity">Maturity date.</param>
    /// <param name="frequency">Annual coupon frequency.</param><param name="basis">Day-count convention.</param>
    /// <returns>Remaining coupon days, using actual days for the actual-day bases.</returns>
    public static decimal CalculateCouponDaysUntilNextPayment(DateTime settlement, DateTime maturity,
        FinancialCouponFrequency frequency = FinancialCouponFrequency.Semiannual,
        FinancialDayCountBasis basis = FinancialDayCountBasis.UnitedStatesThirtyThreeSixty)
    {
        GetCouponDays(settlement, maturity, frequency, basis, out _, out decimal remaining, out _, out _, out _, out _);
        return remaining;
    }

    /// <summary>Finds the next coupon date strictly after settlement.</summary>
    /// <param name="settlement">Settlement date.</param><param name="maturity">Maturity date.</param>
    /// <param name="frequency">Annual coupon frequency.</param><param name="basis">Day-count convention.</param>
    /// <returns>The next coupon date, anchored to maturity.</returns>
    public static DateTime CalculateNextCouponDate(DateTime settlement, DateTime maturity,
        FinancialCouponFrequency frequency = FinancialCouponFrequency.Semiannual,
        FinancialDayCountBasis basis = FinancialDayCountBasis.UnitedStatesThirtyThreeSixty)
    {
        GetCouponDays(settlement, maturity, frequency, basis, out _, out _, out _, out _, out DateTime next, out _);
        return next;
    }

    /// <summary>Counts coupon payments remaining through maturity.</summary>
    /// <param name="settlement">Settlement date.</param><param name="maturity">Maturity date.</param>
    /// <param name="frequency">Annual coupon frequency.</param><param name="basis">Day-count convention.</param>
    /// <returns>The positive integer number of remaining coupon payments.</returns>
    public static decimal CalculateCouponPaymentCount(DateTime settlement, DateTime maturity,
        FinancialCouponFrequency frequency = FinancialCouponFrequency.Semiannual,
        FinancialDayCountBasis basis = FinancialDayCountBasis.UnitedStatesThirtyThreeSixty)
    {
        GetCouponDays(settlement, maturity, frequency, basis, out _, out _, out _, out _, out _, out int count);
        return count;
    }

    /// <summary>Finds the coupon date at or immediately before settlement.</summary>
    /// <param name="settlement">Settlement date.</param><param name="maturity">Maturity date.</param>
    /// <param name="frequency">Annual coupon frequency.</param><param name="basis">Day-count convention.</param>
    /// <returns>The previous coupon date, anchored to maturity.</returns>
    public static DateTime CalculatePreviousCouponDate(DateTime settlement, DateTime maturity,
        FinancialCouponFrequency frequency = FinancialCouponFrequency.Semiannual,
        FinancialDayCountBasis basis = FinancialDayCountBasis.UnitedStatesThirtyThreeSixty)
    {
        GetCouponDays(settlement, maturity, frequency, basis, out _, out _, out _, out DateTime previous, out _, out _);
        return previous;
    }

    // A small, stack-only pricing context. Regular coupons use a closed geometric sum.
    // Odd coupons can use a caller-supplied span of cash flows without allocating an array.
    private readonly struct SecurityCashFlow
    {
        public readonly double Amount;
        public readonly double Time;
        public SecurityCashFlow(double amount, double time) { Amount = amount; Time = time; }
    }

    private ref struct SecurityPricingContext
    {
        public int Frequency;
        public int Count;
        public double Coupon;
        public double Redemption;
        public double FirstExponent;
        public double FirstCouponRatio;
        public double Accrued;
        public bool SimpleDiscount;
        public ReadOnlySpan<SecurityCashFlow> CashFlows;

        public double Evaluate(double annualYield, out double derivative)
        {
            double rate = annualYield / Frequency;
            double logarithm = LogOnePlus(rate);
            double denominator = 1.0 + rate;
            if (Count == 1 && SimpleDiscount)
            {
                double amount = Redemption + Coupon * FirstCouponRatio;
                double divisor = 1.0 + rate * FirstExponent;
                double dirty = amount / divisor;
                derivative = -amount * FirstExponent / (Frequency * divisor * divisor);
                return dirty - Accrued;
            }
            if (Count == 0)
            {
                double sum = 0.0;
                double weighted = 0.0;
                for (int index = 0; index < CashFlows.Length; index++)
                {
                    SecurityCashFlow cashFlow = CashFlows[index];
                    double present = cashFlow.Amount * Math.Exp(-cashFlow.Time * logarithm);
                    sum += present;
                    weighted += cashFlow.Time * present;
                }
                derivative = -weighted / (Frequency * denominator);
                return sum - Accrued;
            }
            // q^w [redemption*q^(n-1) + coupon*(sum(q^k) + firstRatio-1)] - accrued.
            double discount = Math.Exp(-FirstExponent * logarithm);
            double finalDiscount = Math.Exp(-(Count - 1.0) * logarithm);
            double geometric;
            double weightedGeometric;
            if (rate == 0.0)
            {
                geometric = Count;
                weightedGeometric = (double)Count * (Count - 1.0) / 2.0;
            }
            else
            {
                double oneMinusDiscount = -ExponentialMinusOne(-logarithm);
                geometric = -ExponentialMinusOne(-Count * logarithm) / oneMinusDiscount;
                if (Math.Abs(rate) < 1e-4)
                {
                    // Avoid cancellation in the closed weighted sum near zero.
                    double term = 1.0;
                    weightedGeometric = 0.0;
                    for (int index = 1; index < Count; index++)
                    {
                        term /= denominator;
                        weightedGeometric += index * term;
                    }
                }
                else
                {
                    double q = 1.0 / denominator;
                    weightedGeometric = (q - Count * finalDiscount * q + (Count - 1.0) * finalDiscount * q * q) /
                        (oneMinusDiscount * oneMinusDiscount);
                }
            }
            double couponSum = geometric + FirstCouponRatio - 1.0;
            double undiscounted = Redemption * finalDiscount + Coupon * couponSum;
            double weighted2 = FirstExponent * undiscounted +
                Redemption * (Count - 1.0) * finalDiscount + Coupon * weightedGeometric;
            derivative = -discount * weighted2 / (Frequency * denominator);
            return discount * undiscounted - Accrued;
        }
    }

    private static FinancialCalculationResult SolveSecurityYield(ref SecurityPricingContext context, double targetPrice)
    {
        if (!(targetPrice > 0.0) || !double.IsFinite(targetPrice)) throw new ArgumentOutOfRangeException(nameof(targetPrice));
        double zeroPrice = context.Evaluate(0.0, out _);
        double scale = Math.Max(1.0, Math.Max(targetPrice, Math.Abs(zeroPrice)));
        double residual = zeroPrice - targetPrice;
        if (Math.Abs(residual) <= 1e-14 * scale)
            return new FinancialCalculationResult(0m, 0m, 0, Math.Abs(residual) / scale);
        if (residual < 0.0)
            throw new ArgumentOutOfRangeException(nameof(targetPrice), "A nonnegative yield cannot produce this price.");
        double lower = 0.0;
        double upper = 0.1;
        double upperValue = context.Evaluate(upper, out _) - targetPrice;
        for (int index = 0; upperValue > 0.0 && index < 80; index++)
        {
            lower = upper;
            upper *= 2.0;
            upperValue = context.Evaluate(upper, out _) - targetPrice;
        }
        if (upperValue > 0.0 || !double.IsFinite(upperValue))
            throw new ArithmeticException("A finite security yield could not be bracketed.");
        double current = lower + (upper - lower) * 0.5;
        double previous = current;
        for (int iteration = 1; iteration <= 128; iteration++)
        {
            double value = context.Evaluate(current, out double derivative) - targetPrice;
            if (!double.IsFinite(value)) throw new ArithmeticException("The security price is not finite.");
            if (value > 0.0) lower = current;
            else upper = current;
            double width = upper - lower;
            double estimated = derivative != 0.0 ? Math.Abs(value / derivative) : width;
            if (Math.Abs(value) <= 1e-13 * scale && estimated <= 1e-12 * (1.0 + current))
                return new FinancialCalculationResult(Percentage(current), Percentage(estimated), iteration, Math.Abs(value) / scale);
            if (width <= 1e-13 * (1.0 + current))
            {
                double midpoint = lower + width * 0.5;
                double finalResidual = Math.Abs(context.Evaluate(midpoint, out _) - targetPrice) / scale;
                return new FinancialCalculationResult(Percentage(midpoint), Percentage(width * 0.5), iteration, finalResidual);
            }
            double next = derivative < 0.0 ? current - value / derivative : double.NaN;
            double margin = width * 0.05;
            if (!double.IsFinite(next) || next <= lower + margin || next >= upper - margin)
                next = lower + width * 0.5;
            if (next == current || next == previous) next = lower + width * 0.5;
            previous = current;
            current = next;
        }
        throw new ArithmeticException("The security yield did not converge within 128 iterations.");
    }

    private static SecurityPricingContext CreateRegularSecurity(DateTime settlement, DateTime maturity,
        decimal annualCouponPercentageRate, decimal redemptionValue, int frequency, FinancialDayCountBasis basis)
    {
        GetCouponDays(settlement, maturity, (FinancialCouponFrequency)frequency, basis,
            out decimal elapsed, out decimal remaining, out decimal total, out _, out _, out int count);
        double coupon = 100.0 * Fraction(annualCouponPercentageRate) / frequency;
        return new SecurityPricingContext
        {
            Frequency = frequency, Count = count, Coupon = coupon, Redemption = (double)redemptionValue,
            FirstExponent = (double)(remaining / total), FirstCouponRatio = 1.0,
            Accrued = coupon * (double)(elapsed / total), SimpleDiscount = true
        };
    }

    /// <summary>Calculates the annual discount rate implied by a security's price and redemption.</summary>
    /// <param name="settlement">Settlement date.</param><param name="maturity">Maturity date.</param>
    /// <param name="price">Positive clean price per 100 face value.</param>
    /// <param name="redemptionValue">Positive redemption per 100 face value.</param>
    /// <param name="basis">Day-count convention.</param>
    /// <returns>The annual bank-discount rate, as a percentage.</returns>
    public static decimal CalculateSecurityDiscountRate(DateTime settlement, DateTime maturity, decimal price,
        decimal redemptionValue, FinancialDayCountBasis basis = FinancialDayCountBasis.UnitedStatesThirtyThreeSixty)
    {
        ValidateSecurityDates(settlement, maturity);
        ValidateSecurityAmounts(price, redemptionValue);
        decimal years = CalculateYearFraction(settlement, maturity, basis);
        ThrowHelper.ThrowIfZeroToDivideCalculated(years);
        return (redemptionValue - price) / redemptionValue / years * 100m;
    }

    /// <summary>Calculates the clean price of a regular coupon-paying security per 100 face value.</summary>
    /// <remarks>Uses a geometric coupon sum and the standard simple-discount convention for a single
    /// remaining coupon. The annual yield is divided by coupon frequency. No monetary rounding occurs.</remarks>
    /// <param name="settlement">Settlement date.</param><param name="maturity">Maturity date.</param>
    /// <param name="annualCouponPercentageRate">Nonnegative annual coupon percentage.</param>
    /// <param name="annualYieldPercentageRate">Nonnegative annual yield percentage.</param>
    /// <param name="redemptionValue">Positive redemption per 100 face value.</param>
    /// <param name="frequency">Annual coupon frequency.</param><param name="basis">Day-count convention.</param>
    /// <returns>The clean security price per 100 face value.</returns>
    public static decimal CalculateSecurityPrice(DateTime settlement, DateTime maturity,
        decimal annualCouponPercentageRate, decimal annualYieldPercentageRate, decimal redemptionValue,
        FinancialCouponFrequency frequency = FinancialCouponFrequency.Semiannual,
        FinancialDayCountBasis basis = FinancialDayCountBasis.UnitedStatesThirtyThreeSixty)
    {
        RequireNonNegative(annualCouponPercentageRate, nameof(annualCouponPercentageRate));
        RequireNonNegative(annualYieldPercentageRate, nameof(annualYieldPercentageRate));
        RequirePositive(redemptionValue, nameof(redemptionValue));
        SecurityPricingContext context = CreateRegularSecurity(settlement, maturity, annualCouponPercentageRate, redemptionValue,
            ConvertFrequency(frequency), basis);
        if (annualYieldPercentageRate == 0m)
        {
            decimal coupon = annualCouponPercentageRate / ConvertFrequency(frequency);
            return redemptionValue + coupon * context.Count - CheckedDecimal(context.Accrued);
        }
        if (context.Count == 1)
        {
            decimal coupon = annualCouponPercentageRate / ConvertFrequency(frequency);
            decimal divisor = 1m + annualYieldPercentageRate / (100m * ConvertFrequency(frequency)) *
                CheckedDecimal(context.FirstExponent);
            return (redemptionValue + coupon) / divisor - CheckedDecimal(context.Accrued);
        }
        return CheckedDecimal(context.Evaluate(Fraction(annualYieldPercentageRate), out _));
    }

    /// <summary>Calculates the annual yield to maturity of a regular coupon-paying security.</summary>
    /// <remarks>Uses a safeguarded Newton/bisection solver on nonnegative yields. A price that requires
    /// a negative yield is rejected, matching the nonnegative-yield domain of the Excel function.</remarks>
    /// <param name="settlement">Settlement date.</param><param name="maturity">Maturity date.</param>
    /// <param name="annualCouponPercentageRate">Nonnegative annual coupon percentage.</param>
    /// <param name="price">Positive clean price per 100 face value.</param>
    /// <param name="redemptionValue">Positive redemption per 100 face value.</param>
    /// <param name="frequency">Annual coupon frequency.</param><param name="basis">Day-count convention.</param>
    /// <returns>The annual percentage yield.</returns>
    public static decimal CalculateSecurityYield(DateTime settlement, DateTime maturity,
        decimal annualCouponPercentageRate, decimal price, decimal redemptionValue,
        FinancialCouponFrequency frequency = FinancialCouponFrequency.Semiannual,
        FinancialDayCountBasis basis = FinancialDayCountBasis.UnitedStatesThirtyThreeSixty) =>
        CalculateSecurityYieldWithDiagnostics(settlement, maturity, annualCouponPercentageRate, price, redemptionValue, frequency, basis).Value;

    /// <summary>Calculates a regular security yield with convergence diagnostics.</summary>
    /// <param name="settlement">Settlement date.</param><param name="maturity">Maturity date.</param>
    /// <param name="annualCouponPercentageRate">Annual coupon percentage.</param>
    /// <param name="price">Positive clean price.</param><param name="redemptionValue">Positive redemption.</param>
    /// <param name="frequency">Annual coupon frequency.</param><param name="basis">Day-count convention.</param>
    /// <returns>The annual percentage yield, estimated absolute error, iteration count and normalized residual.</returns>
    public static FinancialCalculationResult CalculateSecurityYieldWithDiagnostics(DateTime settlement, DateTime maturity,
        decimal annualCouponPercentageRate, decimal price, decimal redemptionValue,
        FinancialCouponFrequency frequency = FinancialCouponFrequency.Semiannual,
        FinancialDayCountBasis basis = FinancialDayCountBasis.UnitedStatesThirtyThreeSixty)
    {
        RequireNonNegative(annualCouponPercentageRate, nameof(annualCouponPercentageRate));
        ValidateSecurityAmounts(price, redemptionValue);
        SecurityPricingContext context = CreateRegularSecurity(settlement, maturity, annualCouponPercentageRate, redemptionValue,
            ConvertFrequency(frequency), basis);
        return SolveSecurityYield(ref context, (double)price);
    }

    /// <summary>Calculates Macaulay duration, the present-value-weighted average coupon time in years.</summary>
    /// <param name="settlement">Settlement date.</param><param name="maturity">Maturity date.</param>
    /// <param name="annualCouponPercentageRate">Annual coupon percentage.</param>
    /// <param name="annualYieldPercentageRate">Nonnegative annual yield percentage.</param>
    /// <param name="frequency">Annual coupon frequency.</param><param name="basis">Day-count convention.</param>
    /// <returns>Duration in years, before any monetary or display rounding.</returns>
    public static decimal CalculateMacaulayDuration(DateTime settlement, DateTime maturity,
        decimal annualCouponPercentageRate, decimal annualYieldPercentageRate,
        FinancialCouponFrequency frequency = FinancialCouponFrequency.Semiannual,
        FinancialDayCountBasis basis = FinancialDayCountBasis.UnitedStatesThirtyThreeSixty)
    {
        RequireNonNegative(annualCouponPercentageRate, nameof(annualCouponPercentageRate));
        RequireNonNegative(annualYieldPercentageRate, nameof(annualYieldPercentageRate));
        int payments = ConvertFrequency(frequency);
        SecurityPricingContext context = CreateRegularSecurity(settlement, maturity, annualCouponPercentageRate, 100m, payments, basis);
        double yield = Fraction(annualYieldPercentageRate);
        double logarithm = LogOnePlus(yield / payments);
        double sum = 0.0;
        double weighted = 0.0;
        for (int index = 0; index < context.Count; index++)
        {
            double time = context.FirstExponent + index;
            double amount = context.Coupon + (index == context.Count - 1 ? 100.0 : 0.0);
            double present = amount * Math.Exp(-time * logarithm);
            sum += present;
            weighted += time * present;
        }
        if (sum == 0.0) ThrowHelper.ThrowDividedByZeroCalculated();
        return CheckedDecimal(weighted / sum / payments);
    }

    /// <summary>Calculates modified Macaulay duration, the first-order price sensitivity to annual yield.</summary>
    /// <param name="settlement">Settlement date.</param><param name="maturity">Maturity date.</param>
    /// <param name="annualCouponPercentageRate">Annual coupon percentage.</param>
    /// <param name="annualYieldPercentageRate">Nonnegative annual yield percentage.</param>
    /// <param name="frequency">Annual coupon frequency.</param><param name="basis">Day-count convention.</param>
    /// <returns>Macaulay duration divided by one plus annual yield divided by coupon frequency.</returns>
    public static decimal CalculateModifiedMacaulayDuration(DateTime settlement, DateTime maturity,
        decimal annualCouponPercentageRate, decimal annualYieldPercentageRate,
        FinancialCouponFrequency frequency = FinancialCouponFrequency.Semiannual,
        FinancialDayCountBasis basis = FinancialDayCountBasis.UnitedStatesThirtyThreeSixty)
    {
        decimal duration = CalculateMacaulayDuration(settlement, maturity, annualCouponPercentageRate,
            annualYieldPercentageRate, frequency, basis);
        return duration / (1m + annualYieldPercentageRate / (100m * ConvertFrequency(frequency)));
    }

    /// <summary>Calculates the annual simple return on a fully invested security.</summary>
    /// <param name="settlement">Investment date.</param><param name="maturity">Redemption date.</param>
    /// <param name="investment">Positive investment amount.</param><param name="redemptionValue">Positive redemption amount.</param>
    /// <param name="basis">Day-count convention.</param>
    /// <returns>The simple annual percentage return.</returns>
    public static decimal CalculateSecurityInvestmentRate(DateTime settlement, DateTime maturity, decimal investment,
        decimal redemptionValue, FinancialDayCountBasis basis = FinancialDayCountBasis.UnitedStatesThirtyThreeSixty)
    {
        ValidateSecurityDates(settlement, maturity);
        ValidateSecurityAmounts(investment, redemptionValue);
        decimal years = CalculateYearFraction(settlement, maturity, basis);
        ThrowHelper.ThrowIfZeroToDivideCalculated(years);
        return (redemptionValue - investment) / investment / years * 100m;
    }

    /// <summary>Calculates a discounted security's price per 100 face value.</summary>
    /// <param name="settlement">Settlement date.</param><param name="maturity">Maturity date.</param>
    /// <param name="annualDiscountPercentageRate">Nonnegative annual discount percentage.</param>
    /// <param name="redemptionValue">Positive redemption per 100 face value.</param>
    /// <param name="basis">Day-count convention.</param>
    /// <returns>Redemption multiplied by one minus discount rate times year fraction.</returns>
    public static decimal CalculateDiscountedSecurityPrice(DateTime settlement, DateTime maturity,
        decimal annualDiscountPercentageRate, decimal redemptionValue,
        FinancialDayCountBasis basis = FinancialDayCountBasis.UnitedStatesThirtyThreeSixty)
    {
        ValidateSecurityDates(settlement, maturity);
        RequireNonNegative(annualDiscountPercentageRate, nameof(annualDiscountPercentageRate));
        RequirePositive(redemptionValue, nameof(redemptionValue));
        decimal factor = 1m - annualDiscountPercentageRate / 100m * CalculateYearFraction(settlement, maturity, basis);
        if (factor < 0m) throw new ArgumentOutOfRangeException(nameof(annualDiscountPercentageRate), "The discounted price cannot be negative.");
        return redemptionValue * factor;
    }

    /// <summary>Calculates the annual yield of a discounted security.</summary>
    /// <param name="settlement">Settlement date.</param><param name="maturity">Maturity date.</param>
    /// <param name="price">Positive purchase price per 100 face value.</param>
    /// <param name="redemptionValue">Positive redemption per 100 face value.</param>
    /// <param name="basis">Day-count convention.</param>
    /// <returns>The annual percentage yield using purchase price as the denominator.</returns>
    public static decimal CalculateDiscountedSecurityYield(DateTime settlement, DateTime maturity, decimal price,
        decimal redemptionValue, FinancialDayCountBasis basis = FinancialDayCountBasis.UnitedStatesThirtyThreeSixty)
    {
        ValidateSecurityDates(settlement, maturity);
        ValidateSecurityAmounts(price, redemptionValue);
        decimal years = CalculateYearFraction(settlement, maturity, basis);
        ThrowHelper.ThrowIfZeroToDivideCalculated(years);
        return (redemptionValue - price) / price / years * 100m;
    }

    /// <summary>Calculates the price of a security paying interest only at maturity.</summary>
    /// <param name="settlement">Settlement date.</param><param name="maturity">Maturity date.</param>
    /// <param name="issue">Issue date, no later than settlement.</param>
    /// <param name="annualCouponPercentageRate">Nonnegative annual coupon percentage.</param>
    /// <param name="annualYieldPercentageRate">Nonnegative annual yield percentage.</param>
    /// <param name="basis">Day-count convention.</param>
    /// <returns>Clean price per 100 face value, excluding interest accrued before settlement.</returns>
    public static decimal CalculateSecurityPriceAtMaturity(DateTime settlement, DateTime maturity, DateTime issue,
        decimal annualCouponPercentageRate, decimal annualYieldPercentageRate,
        FinancialDayCountBasis basis = FinancialDayCountBasis.UnitedStatesThirtyThreeSixty)
    {
        ValidateSecurityDates(settlement, maturity);
        if (issue.Date > settlement.Date) throw new ArgumentOutOfRangeException(nameof(issue));
        RequireNonNegative(annualCouponPercentageRate, nameof(annualCouponPercentageRate));
        RequireNonNegative(annualYieldPercentageRate, nameof(annualYieldPercentageRate));
        decimal rate = annualCouponPercentageRate / 100m;
        decimal issueToMaturity = CalculateYearFraction(issue, maturity, basis);
        decimal issueToSettlement = CalculateYearFraction(issue, settlement, basis);
        decimal settlementToMaturity = CalculateYearFraction(settlement, maturity, basis);
        return 100m * (1m + rate * issueToMaturity) /
            (1m + annualYieldPercentageRate / 100m * settlementToMaturity) - 100m * rate * issueToSettlement;
    }

    /// <summary>Calculates the annual yield of a security paying interest at maturity.</summary>
    /// <param name="settlement">Settlement date.</param><param name="maturity">Maturity date.</param>
    /// <param name="issue">Issue date, no later than settlement.</param>
    /// <param name="annualCouponPercentageRate">Nonnegative annual coupon percentage.</param>
    /// <param name="price">Positive clean price per 100 face value.</param>
    /// <param name="basis">Day-count convention.</param>
    /// <returns>The annual percentage yield.</returns>
    public static decimal CalculateSecurityYieldAtMaturity(DateTime settlement, DateTime maturity, DateTime issue,
        decimal annualCouponPercentageRate, decimal price,
        FinancialDayCountBasis basis = FinancialDayCountBasis.UnitedStatesThirtyThreeSixty)
    {
        ValidateSecurityDates(settlement, maturity);
        if (issue.Date > settlement.Date) throw new ArgumentOutOfRangeException(nameof(issue));
        RequireNonNegative(annualCouponPercentageRate, nameof(annualCouponPercentageRate));
        RequirePositive(price, nameof(price));
        decimal rate = annualCouponPercentageRate / 100m;
        decimal accrued = 100m * rate * CalculateYearFraction(issue, settlement, basis);
        decimal dirty = price + accrued;
        ThrowHelper.ThrowIfZeroToDivideCalculated(dirty);
        decimal years = CalculateYearFraction(settlement, maturity, basis);
        ThrowHelper.ThrowIfZeroToDivideCalculated(years);
        return ((100m * (1m + rate * CalculateYearFraction(issue, maturity, basis)) / dirty) - 1m) / years * 100m;
    }

    /// <summary>Calculates redemption proceeds from a discounted purchase amount.</summary>
    /// <param name="settlement">Settlement date.</param><param name="maturity">Maturity date.</param>
    /// <param name="investment">Positive investment amount.</param>
    /// <param name="annualDiscountPercentageRate">Nonnegative annual discount percentage.</param>
    /// <param name="basis">Day-count convention.</param>
    /// <returns>The amount received at maturity.</returns>
    public static decimal CalculateSecurityRedemptionAmount(DateTime settlement, DateTime maturity, decimal investment,
        decimal annualDiscountPercentageRate,
        FinancialDayCountBasis basis = FinancialDayCountBasis.UnitedStatesThirtyThreeSixty)
    {
        RequirePositive(investment, nameof(investment));
        decimal price = CalculateDiscountedSecurityPrice(settlement, maturity, annualDiscountPercentageRate, 100m, basis);
        return investment * 100m / price;
    }

    /// <summary>Calculates the price of a Treasury bill per 100 face value.</summary>
    /// <remarks>Uses the United States actual/360 bank-discount convention, not a Brazilian Treasury convention.</remarks>
    /// <param name="settlement">Settlement date.</param><param name="maturity">Maturity date within 365 actual days.</param>
    /// <param name="annualDiscountPercentageRate">Nonnegative annual discount percentage.</param>
    /// <returns>The price per 100 face value.</returns>
    public static decimal CalculateTreasuryBillPrice(DateTime settlement, DateTime maturity, decimal annualDiscountPercentageRate)
    {
        ValidateTreasuryBillDates(settlement, maturity);
        RequireNonNegative(annualDiscountPercentageRate, nameof(annualDiscountPercentageRate));
        decimal factor = 1m - annualDiscountPercentageRate / 100m * (maturity.Date - settlement.Date).Days / 360m;
        if (factor <= 0m) throw new ArgumentOutOfRangeException(nameof(annualDiscountPercentageRate));
        return 100m * factor;
    }

    /// <summary>Calculates the annual investment yield of a United States Treasury bill.</summary>
    /// <param name="settlement">Settlement date.</param><param name="maturity">Maturity date within 365 actual days.</param>
    /// <param name="price">Positive price per 100 face value.</param>
    /// <returns>The annual percentage yield on the 360-day investment basis.</returns>
    public static decimal CalculateTreasuryBillYield(DateTime settlement, DateTime maturity, decimal price)
    {
        ValidateTreasuryBillDates(settlement, maturity);
        RequirePositive(price, nameof(price));
        return (100m - price) / price * 36000m / (maturity.Date - settlement.Date).Days;
    }

    /// <summary>Calculates Excel-style bond-equivalent yield of a United States Treasury bill.</summary>
    /// <remarks>Uses the spreadsheet's simple annualization: 365*d/(360-d*days).
    /// This is not the United States Treasury's separate quadratic investment-rate convention
    /// for bills longer than 182 days. See <see cref="CalculateTreasuryBillInvestmentYield"/>.</remarks>
    /// <param name="settlement">Settlement date.</param><param name="maturity">Maturity date within 365 actual days.</param>
    /// <param name="annualDiscountPercentageRate">Nonnegative annual bank-discount percentage.</param>
    /// <returns>The Excel-style annual percentage yield.</returns>
    public static decimal CalculateTreasuryBillEquivalentYield(DateTime settlement, DateTime maturity, decimal annualDiscountPercentageRate)
    {
        ValidateTreasuryBillDates(settlement, maturity);
        RequireNonNegative(annualDiscountPercentageRate, nameof(annualDiscountPercentageRate));
        decimal discount = annualDiscountPercentageRate / 100m;
        decimal denominator = 360m - discount * (maturity.Date - settlement.Date).Days;
        ThrowHelper.ThrowIfZeroToDivideCalculated(denominator);
        if (denominator < 0m) throw new ArgumentOutOfRangeException(nameof(annualDiscountPercentageRate));
        return 365m * discount / denominator * 100m;
    }

    /// <summary>Calculates the United States Treasury investment-rate convention for a Treasury bill.</summary>
    /// <remarks>For terms through 182 days, uses the simple investment yield. Longer terms use
    /// the semiannual coupon-equivalent quadratic. The day-year basis is an explicit parameter;
    /// choose 365 or 366 according to the applicable Treasury convention. This is intentionally
    /// distinct from Excel's TBILLEQ calculation.</remarks>
    /// <param name="settlement">Settlement date.</param><param name="maturity">Maturity date within 365 actual days.</param>
    /// <param name="annualDiscountPercentageRate">Nonnegative annual bank-discount percentage.</param>
    /// <param name="daysInYear">Applicable day-year basis, 365 or 366.</param>
    /// <returns>The annual investment yield as a percentage.</returns>
    public static decimal CalculateTreasuryBillInvestmentYield(DateTime settlement, DateTime maturity,
        decimal annualDiscountPercentageRate, int daysInYear = 365)
    {
        ValidateTreasuryBillDates(settlement, maturity);
        RequireNonNegative(annualDiscountPercentageRate, nameof(annualDiscountPercentageRate));
        if (daysInYear is not (365 or 366)) throw new ArgumentOutOfRangeException(nameof(daysInYear));
        decimal days = (maturity.Date - settlement.Date).Days;
        decimal price = CalculateTreasuryBillPrice(settlement, maturity, annualDiscountPercentageRate) / 100m;
        ThrowHelper.ThrowIfZeroToDivideCalculated(price);
        decimal holdingPeriodReturn = (1m - price) / price;
        if (days <= 182m) return holdingPeriodReturn * daysInYear / days * 100m;
        double time = (double)days / daysInYear;
        double gain = (double)holdingPeriodReturn;
        // (time/2 - 1/4)*yield^2 + time*yield - gain = 0.
        // Rationalized positive root avoids cancellation at small yields.
        double coefficient = time / 2.0 - 0.25;
        double discriminant = time * time + 4.0 * coefficient * gain;
        if (discriminant < 0.0) throw new ArithmeticException("The investment yield has no real solution.");
        return Percentage(2.0 * gain / (time + Math.Sqrt(discriminant)));
    }

    /// <summary>Converts a fractional security quote to an ordinary decimal price.</summary>
    /// <remarks>For example, 1.02 with denominator 16 means 1 + 2/16 = 1.125.</remarks>
    /// <param name="fractionalPrice">Signed fractional quotation.</param><param name="denominator">Positive integer denominator.</param>
    /// <returns>The decimal price without monetary rounding.</returns>
    public static decimal ConvertFractionalSecurityPriceToDecimal(decimal fractionalPrice, int denominator)
    {
        RequirePositive(denominator, nameof(denominator));
        decimal whole = decimal.Truncate(fractionalPrice);
        return whole + (fractionalPrice - whole) * FractionalPriceScale(denominator) / denominator;
    }

    /// <summary>Converts an ordinary decimal price to a fractional security quotation.</summary>
    /// <param name="decimalPrice">Signed decimal price.</param><param name="denominator">Positive integer denominator.</param>
    /// <returns>The fractional quotation, using the denominator's decimal digit scale.</returns>
    public static decimal ConvertDecimalSecurityPriceToFractional(decimal decimalPrice, int denominator)
    {
        RequirePositive(denominator, nameof(denominator));
        decimal whole = decimal.Truncate(decimalPrice);
        return whole + (decimalPrice - whole) * denominator / FractionalPriceScale(denominator);
    }

    private static decimal FractionalPriceScale(int denominator)
    {
        decimal scale = 1m;
        while (denominator >= 10) { denominator /= 10; scale *= 10m; }
        return scale;
    }
}
