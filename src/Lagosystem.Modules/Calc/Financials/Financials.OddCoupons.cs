
namespace Lagosystem.Modules.Calc.Financials;

using System.Buffers;
using Lagosystem.Modules.Helpers;


public static partial class Financials
{
    private static int GetOddFirstCapacity(DateTime firstCoupon, DateTime maturity, int frequency)
    {
        int months = 12 / frequency;
        int monthDifference = (maturity.Year - firstCoupon.Year) * 12 + maturity.Month - firstCoupon.Month;
        int capacity = checked(monthDifference / months + 2);
        if (capacity < 2 || capacity > MaximumSecurityCouponCount)
            throw new ArgumentOutOfRangeException(nameof(maturity), "The coupon schedule exceeds the supported range.");
        return capacity;
    }

    private static SecurityPricingContext CreateOddFirstSecurity(DateTime settlement, DateTime maturity,
        DateTime issue, DateTime firstCoupon, decimal annualCouponPercentageRate, decimal redemptionValue,
        int frequency, FinancialDayCountBasis basis, Span<SecurityCashFlow> buffer)
    {
        ValidateSecurityDates(settlement, maturity);
        if (!(issue.Date < settlement.Date && settlement.Date < firstCoupon.Date && firstCoupon.Date < maturity.Date))
            throw new ArgumentOutOfRangeException(nameof(firstCoupon), "Required order: issue < settlement < firstCoupon < maturity.");
        RequireNonNegative(annualCouponPercentageRate, nameof(annualCouponPercentageRate));
        RequirePositive(redemptionValue, nameof(redemptionValue));
        ConvertBasis(basis);
        double coupon = 100.0 * Fraction(annualCouponPercentageRate) / frequency;
        decimal firstRatio = CouponFraction(issue.Date, firstCoupon.Date, firstCoupon.Date, frequency, basis);
        decimal accruedRatio = CouponFraction(issue.Date, settlement.Date, firstCoupon.Date, frequency, basis);
        decimal firstExponent = CouponFraction(settlement.Date, firstCoupon.Date, firstCoupon.Date, frequency, basis);
        int count = 0;
        buffer[count++] = new SecurityCashFlow(coupon * (double)firstRatio, (double)firstExponent);
        DateTime previous = firstCoupon.Date;
        int months = 12 / frequency;
        for (int index = 1; ; index++)
        {
            if (count >= buffer.Length) throw new ArgumentException("Insufficient coupon buffer.", nameof(buffer));
            DateTime scheduled = AddMonthsAnchored(firstCoupon.Date, checked(index * months));
            DateTime paymentDate = scheduled < maturity.Date ? scheduled : maturity.Date;
            decimal ratio = paymentDate == scheduled ? 1m :
                CouponFraction(previous, paymentDate, firstCoupon.Date, frequency, basis);
            double exponent = (double)firstExponent + index - 1.0 + (double)ratio;
            double amount = coupon * (double)ratio;
            if (paymentDate == maturity.Date) amount += (double)redemptionValue;
            buffer[count++] = new SecurityCashFlow(amount, exponent);
            if (paymentDate == maturity.Date) break;
            previous = paymentDate;
        }
        return new SecurityPricingContext
        {
            Frequency = frequency,
            Count = 0,
            CashFlows = buffer.Slice(0, count),
            Accrued = coupon * (double)accruedRatio
        };
    }

    private static FinancialCalculationResult CalculateOddFirstCouponCore(DateTime settlement, DateTime maturity,
        DateTime issue, DateTime firstCoupon, decimal annualCouponPercentageRate,
        decimal annualYieldPercentageRate, decimal redemptionValue, FinancialCouponFrequency frequency,
        FinancialDayCountBasis basis, decimal? targetPrice)
    {
        int payments = ConvertFrequency(frequency);
        int capacity = GetOddFirstCapacity(firstCoupon.Date, maturity.Date, payments);
        SecurityCashFlow[]? rented = null;
        Span<SecurityCashFlow> buffer = capacity <= 128
            ? stackalloc SecurityCashFlow[capacity]
            : (rented = ArrayPool<SecurityCashFlow>.Shared.Rent(capacity)).AsSpan(0, capacity);
        try
        {
            SecurityPricingContext context = CreateOddFirstSecurity(settlement, maturity, issue, firstCoupon,
                annualCouponPercentageRate, redemptionValue, payments, basis, buffer);
            if (targetPrice.HasValue) return SolveSecurityYield(ref context, (double)targetPrice.Value);
            RequireNonNegative(annualYieldPercentageRate, nameof(annualYieldPercentageRate));
            decimal result = CheckedDecimal(context.Evaluate(Fraction(annualYieldPercentageRate), out _));
            return new FinancialCalculationResult(result, 0m, 0, 0.0);
        }
        finally
        {
            if (rented is not null) ArrayPool<SecurityCashFlow>.Shared.Return(rented);
        }
    }

    /// <summary>Calculates the clean price of a security with a short or long first coupon.</summary>
    /// <remarks>Splits the initial period into nominal quasi-coupons, prorates the first coupon,
    /// discounts subsequent cash flows at the nominal periodic yield and subtracts accrued interest.
    /// An irregular final payment is also prorated when maturity does not align with the first-coupon
    /// schedule. This is an explicit cash-flow model, not a guarantee of identical Excel edge-case
    /// behavior for securities having both irregular first and last periods.</remarks>
    /// <param name="settlement">Settlement date.</param><param name="maturity">Maturity date.</param>
    /// <param name="issue">Issue date, earlier than settlement.</param>
    /// <param name="firstCoupon">First actual coupon date, later than settlement and earlier than maturity.</param>
    /// <param name="annualCouponPercentageRate">Nonnegative annual coupon percentage.</param>
    /// <param name="annualYieldPercentageRate">Nonnegative annual yield percentage.</param>
    /// <param name="redemptionValue">Positive redemption per 100 face value.</param>
    /// <param name="frequency">Annual coupon frequency.</param><param name="basis">Day-count convention.</param>
    /// <returns>The clean price per 100 face value.</returns>
    public static decimal CalculateOddFirstCouponPrice(DateTime settlement, DateTime maturity, DateTime issue,
        DateTime firstCoupon, decimal annualCouponPercentageRate, decimal annualYieldPercentageRate,
        decimal redemptionValue, FinancialCouponFrequency frequency = FinancialCouponFrequency.Semiannual,
        FinancialDayCountBasis basis = FinancialDayCountBasis.UnitedStatesThirtyThreeSixty) =>
        CalculateOddFirstCouponCore(settlement, maturity, issue, firstCoupon, annualCouponPercentageRate,
            annualYieldPercentageRate, redemptionValue, frequency, basis, null).Value;

    /// <summary>Calculates the annual yield of a security with an irregular first coupon.</summary>
    /// <param name="settlement">Settlement date.</param><param name="maturity">Maturity date.</param>
    /// <param name="issue">Issue date.</param><param name="firstCoupon">First actual coupon date.</param>
    /// <param name="annualCouponPercentageRate">Annual coupon percentage.</param>
    /// <param name="price">Positive clean price per 100 face value.</param>
    /// <param name="redemptionValue">Positive redemption per 100 face value.</param>
    /// <param name="frequency">Annual coupon frequency.</param><param name="basis">Day-count convention.</param>
    /// <returns>The annual percentage yield, using the same cash-flow model as the price method.</returns>
    public static decimal CalculateOddFirstCouponYield(DateTime settlement, DateTime maturity, DateTime issue,
        DateTime firstCoupon, decimal annualCouponPercentageRate, decimal price, decimal redemptionValue,
        FinancialCouponFrequency frequency = FinancialCouponFrequency.Semiannual,
        FinancialDayCountBasis basis = FinancialDayCountBasis.UnitedStatesThirtyThreeSixty) =>
        CalculateOddFirstCouponYieldWithDiagnostics(settlement, maturity, issue, firstCoupon,
            annualCouponPercentageRate, price, redemptionValue, frequency, basis).Value;

    /// <summary>Calculates an odd-first-coupon yield with a numerical error estimate and residual.</summary>
    /// <param name="settlement">Settlement date.</param><param name="maturity">Maturity date.</param>
    /// <param name="issue">Issue date.</param><param name="firstCoupon">First coupon date.</param>
    /// <param name="annualCouponPercentageRate">Annual coupon percentage.</param>
    /// <param name="price">Positive clean price.</param><param name="redemptionValue">Positive redemption.</param>
    /// <param name="frequency">Annual coupon frequency.</param><param name="basis">Day-count convention.</param>
    /// <returns>Yield in percent, estimated absolute error in percentage points, iterations and normalized residual.</returns>
    public static FinancialCalculationResult CalculateOddFirstCouponYieldWithDiagnostics(DateTime settlement, DateTime maturity,
        DateTime issue, DateTime firstCoupon, decimal annualCouponPercentageRate, decimal price, decimal redemptionValue,
        FinancialCouponFrequency frequency = FinancialCouponFrequency.Semiannual,
        FinancialDayCountBasis basis = FinancialDayCountBasis.UnitedStatesThirtyThreeSixty)
    {
        RequirePositive(price, nameof(price));
        return CalculateOddFirstCouponCore(settlement, maturity, issue, firstCoupon, annualCouponPercentageRate,
            0m, redemptionValue, frequency, basis, price);
    }

    // Validate and calculate quasi-coupon fractions once. The ordinary path allocates nothing.
    private static void GetOddLastCouponFractions(DateTime settlement, DateTime maturity,
        DateTime lastInterest, decimal annualCouponPercentageRate, decimal redemptionValue,
        int frequency, FinancialDayCountBasis basis, out decimal total, out decimal elapsed,
        out decimal remaining)
    {
        ValidateSecurityDates(settlement, maturity);
        if (lastInterest.Date >= settlement.Date) throw new ArgumentOutOfRangeException(nameof(lastInterest));
        RequireNonNegative(annualCouponPercentageRate, nameof(annualCouponPercentageRate));
        RequirePositive(redemptionValue, nameof(redemptionValue));
        ConvertBasis(basis);
        total = CouponFraction(lastInterest.Date, maturity.Date, lastInterest.Date, frequency, basis);
        elapsed = CouponFraction(lastInterest.Date, settlement.Date, lastInterest.Date, frequency, basis);
        remaining = total - elapsed;
        ThrowHelper.ThrowIfZeroToDivideCalculated(remaining);
    }

    /// <summary>Calculates the clean price of a security with an irregular final coupon period.</summary>
    /// <remarks>Uses the simple-yield odd-last-period convention: the final redemption and prorated
    /// coupon are divided by one plus periodic yield times the remaining quasi-coupon fraction,
    /// then accrued interest is subtracted. Short and long final periods are supported.</remarks>
    /// <param name="settlement">Settlement date.</param><param name="maturity">Maturity date.</param>
    /// <param name="lastInterest">Last actual coupon date, earlier than settlement.</param>
    /// <param name="annualCouponPercentageRate">Nonnegative annual coupon percentage.</param>
    /// <param name="annualYieldPercentageRate">Nonnegative annual yield percentage.</param>
    /// <param name="redemptionValue">Positive redemption per 100 face value.</param>
    /// <param name="frequency">Annual coupon frequency.</param><param name="basis">Day-count convention.</param>
    /// <returns>The clean price per 100 face value.</returns>
    public static decimal CalculateOddLastCouponPrice(DateTime settlement, DateTime maturity, DateTime lastInterest,
        decimal annualCouponPercentageRate, decimal annualYieldPercentageRate, decimal redemptionValue,
        FinancialCouponFrequency frequency = FinancialCouponFrequency.Semiannual,
        FinancialDayCountBasis basis = FinancialDayCountBasis.UnitedStatesThirtyThreeSixty)
    {
        RequireNonNegative(annualYieldPercentageRate, nameof(annualYieldPercentageRate));
        int payments = ConvertFrequency(frequency);
        GetOddLastCouponFractions(settlement, maturity, lastInterest, annualCouponPercentageRate,
            redemptionValue, payments, basis, out decimal total, out decimal elapsed, out decimal remaining);
        decimal coupon = annualCouponPercentageRate / payments;
        decimal divisor = 1m + annualYieldPercentageRate / (100m * payments) * remaining;
        ThrowHelper.ThrowIfZeroToDivideCalculated(divisor);
        return (redemptionValue + coupon * total) / divisor - coupon * elapsed;
    }

    /// <summary>Calculates the annual yield of a security with an irregular final coupon period.</summary>
    /// <remarks>Solves the one-payment simple-discount equation directly, without an iterative solver.</remarks>
    /// <param name="settlement">Settlement date.</param><param name="maturity">Maturity date.</param>
    /// <param name="lastInterest">Last actual coupon date.</param>
    /// <param name="annualCouponPercentageRate">Nonnegative annual coupon percentage.</param>
    /// <param name="price">Positive clean price per 100 face value.</param>
    /// <param name="redemptionValue">Positive redemption per 100 face value.</param>
    /// <param name="frequency">Annual coupon frequency.</param><param name="basis">Day-count convention.</param>
    /// <returns>The annual percentage yield.</returns>
    public static decimal CalculateOddLastCouponYield(DateTime settlement, DateTime maturity, DateTime lastInterest,
        decimal annualCouponPercentageRate, decimal price, decimal redemptionValue,
        FinancialCouponFrequency frequency = FinancialCouponFrequency.Semiannual,
        FinancialDayCountBasis basis = FinancialDayCountBasis.UnitedStatesThirtyThreeSixty)
    {
        ValidateSecurityAmounts(price, redemptionValue);
        int payments = ConvertFrequency(frequency);
        GetOddLastCouponFractions(settlement, maturity, lastInterest, annualCouponPercentageRate,
            redemptionValue, payments, basis, out decimal total, out decimal elapsed, out decimal remaining);
        decimal coupon = annualCouponPercentageRate / payments;
        decimal dirty = price + coupon * elapsed;
        ThrowHelper.ThrowIfZeroToDivideCalculated(dirty);
        decimal result = payments * ((redemptionValue + coupon * total) / dirty - 1m) / remaining * 100m;
        if (result < 0m) throw new ArgumentOutOfRangeException(nameof(price), "The price requires a negative yield.");
        return result;
    }
}

