namespace Lagosystem.Modules.Calc.Financials;


public static partial class Financials
{
    /// <summary>Calculates French linear accounting depreciation with a prorated first period.</summary>
    /// <remarks>Implements the historical AMORLINC calculation. The first period uses the selected
    /// year fraction; later full periods use cost times the annual rate. Depreciation is capped at
    /// cost minus salvage. The accounting period is zero-based and fractional indices are truncated.
    /// This legacy method is not a substitute for current French or Brazilian accounting rules.</remarks>
    /// <param name="cost">Nonnegative acquisition cost.</param>
    /// <param name="purchaseDate">Acquisition date.</param>
    /// <param name="firstPeriodEnd">End of the first accounting period, later than acquisition.</param>
    /// <param name="salvageValue">Residual value between zero and cost.</param>
    /// <param name="period">Zero-based accounting period.</param>
    /// <param name="percentageDepreciationRate">Nonnegative annual depreciation percentage.</param>
    /// <param name="basis">Day-count convention for the initial proration.</param>
    /// <returns>The depreciation expense of the selected period, without currency rounding.</returns>
    public static decimal CalculateFrenchLinearDepreciation(decimal cost, DateTime purchaseDate, DateTime firstPeriodEnd,
        decimal salvageValue, decimal period, decimal percentageDepreciationRate,
        FinancialDayCountBasis basis = FinancialDayCountBasis.UnitedStatesThirtyThreeSixty)
    {
        ValidateDepreciation(cost, salvageValue, 1m);
        RequireNonNegative(percentageDepreciationRate, nameof(percentageDepreciationRate));
        RequireNonNegative(period, nameof(period));
        if (firstPeriodEnd.Date <= purchaseDate.Date) throw new ArgumentOutOfRangeException(nameof(firstPeriodEnd));
        int accountingPeriod = checked((int)decimal.Truncate(period));
        decimal annual = cost * percentageDepreciationRate / 100m;
        decimal maximum = cost - salvageValue;
        if (annual == 0m || maximum == 0m) return 0m;
        decimal first = Math.Min(maximum, annual * CalculateYearFraction(purchaseDate, firstPeriodEnd, basis));
        if (accountingPeriod == 0) return first;
        decimal remaining = maximum - first - annual * (accountingPeriod - 1);
        return Math.Max(0m, Math.Min(annual, remaining));
    }

    /// <summary>Calculates historical French declining-balance depreciation with a life-dependent coefficient.</summary>
    /// <remarks>Implements the AMORDEGRC coefficient schedule: 1.5 for lives from three through four
    /// years, 2 for five through six, and 2.5 for lives greater than six. The first period is
    /// prorated. The final two accounting periods use the historical 50 percent and 100 percent
    /// residual rules. This function is deprecated in modern French accounting and is retained
    /// solely for legacy financial modeling. Exact spreadsheet intermediate-rounding behavior is
    /// not guaranteed for every fractional-life edge case.</remarks>
    /// <param name="cost">Nonnegative acquisition cost.</param>
    /// <param name="purchaseDate">Acquisition date.</param>
    /// <param name="firstPeriodEnd">End of the first accounting period.</param>
    /// <param name="salvageValue">Residual value between zero and cost.</param>
    /// <param name="period">Zero-based accounting period; fractional indices are truncated.</param>
    /// <param name="percentageDepreciationRate">Positive annual depreciation percentage.</param>
    /// <param name="basis">Day-count convention.</param>
    /// <param name="excelCompatibleRounding">True to round each depreciation amount to whole units using AwayFromZero; false to retain decimal precision.</param>
    /// <returns>The depreciation expense for the requested accounting period.</returns>
    public static decimal CalculateFrenchDecliningBalanceDepreciation(decimal cost, DateTime purchaseDate,
        DateTime firstPeriodEnd, decimal salvageValue, decimal period, decimal percentageDepreciationRate,
        FinancialDayCountBasis basis = FinancialDayCountBasis.UnitedStatesThirtyThreeSixty,
        bool excelCompatibleRounding = true)
    {
        ValidateDepreciation(cost, salvageValue, 1m);
        RequirePositive(percentageDepreciationRate, nameof(percentageDepreciationRate));
        RequireNonNegative(period, nameof(period));
        if (firstPeriodEnd.Date <= purchaseDate.Date) throw new ArgumentOutOfRangeException(nameof(firstPeriodEnd));
        decimal rate = percentageDepreciationRate / 100m;
        decimal usefulLife = 1m / rate;
        decimal coefficient;
        if (usefulLife >= 3m && usefulLife <= 4m) coefficient = 1.5m;
        else if (usefulLife >= 5m && usefulLife <= 6m) coefficient = 2m;
        else if (usefulLife > 6m) coefficient = 2.5m;
        else throw new ArgumentOutOfRangeException(nameof(percentageDepreciationRate), "The depreciation rate produces an unsupported historical French useful life.");
        int accountingPeriod = checked((int)decimal.Truncate(period));
        int finalPeriod = checked((int)Math.Ceiling(usefulLife)) - 1;
        if (accountingPeriod > finalPeriod || cost == salvageValue) return 0m;
        decimal effectiveRate = rate * coefficient;
        decimal balance = cost;
        for (int index = 0; index <= accountingPeriod; index++)
        {
            if (balance <= salvageValue) return 0m;
            decimal expense;
            if (index == finalPeriod) expense = balance - salvageValue;
            else if (index == finalPeriod - 1) expense = (balance - salvageValue) / 2m;
            else
            {
                expense = balance * effectiveRate;
                if (index == 0) expense *= CalculateYearFraction(purchaseDate, firstPeriodEnd, basis);
                if (excelCompatibleRounding)
                    expense = Math.Round(expense, 0, MidpointRounding.AwayFromZero);
                expense = Math.Min(expense, balance - salvageValue);
            }
            if (index == accountingPeriod) return expense;
            balance -= expense;
        }
        return 0m;
    }

    private static decimal CalculateCumulativeUnswitchedDepreciation(decimal cost, decimal salvageValue,
        decimal usefulLife, decimal period, decimal factor)
    {
        if (period <= 0m || cost == salvageValue) return 0m;
        decimal rate = Math.Min(1m, factor / usefulLife);
        if (rate == 1m) return (cost - salvageValue) * Math.Min(1m, period);
        int wholePeriods = checked((int)decimal.Truncate(period));
        double remainingFactor = Math.Pow((double)(1m - rate), wholePeriods);
        decimal balance = CheckedDecimal((double)cost * remainingFactor);
        if (balance < salvageValue) balance = salvageValue;
        decimal accumulated = cost - balance;
        decimal fraction = period - wholePeriods;
        if (fraction > 0m && balance > salvageValue)
            accumulated += Math.Min(balance * rate, balance - salvageValue) * fraction;
        return accumulated;
    }

    /// <summary>Calculates variable declining-balance depreciation over any interval, including partial periods.</summary>
    /// <remarks>Equivalent to the VDB/BDV method. With switching disabled, the unswitched
    /// declining-balance calculation uses a closed-form cumulative balance. With switching enabled,
    /// the function changes to straight-line when that produces greater depreciation. Fractional
    /// boundaries are prorated over their corresponding full-period expense. No implicit currency
    /// rounding is performed. This is a financial-modeling convention, not an accounting posting policy.</remarks>
    /// <param name="cost">Nonnegative acquisition cost.</param>
    /// <param name="salvageValue">Residual value between zero and cost.</param>
    /// <param name="usefulLife">Positive useful life in accounting periods.</param>
    /// <param name="startPeriod">Nonnegative beginning of the interval.</param>
    /// <param name="endPeriod">End of the interval, not earlier than start and not beyond useful life.</param>
    /// <param name="factor">Positive declining-balance acceleration factor, normally two.</param>
    /// <param name="switchToStraightLine">Whether to switch to straight-line when it produces higher depreciation.</param>
    /// <returns>The accumulated depreciation expense in the requested interval.</returns>
    public static decimal CalculateVariableDecliningBalanceDepreciation(decimal cost, decimal salvageValue,
        decimal usefulLife, decimal startPeriod, decimal endPeriod, decimal factor = 2m,
        bool switchToStraightLine = true)
    {
        ValidateDepreciation(cost, salvageValue, usefulLife);
        RequirePositive(factor, nameof(factor));
        if (startPeriod < 0m || endPeriod < startPeriod || endPeriod > usefulLife)
            throw new ArgumentOutOfRangeException(nameof(startPeriod));
        if (startPeriod == endPeriod || cost == salvageValue) return 0m;
        if (!switchToStraightLine)
            return CalculateCumulativeUnswitchedDepreciation(cost, salvageValue, usefulLife, endPeriod, factor) -
                CalculateCumulativeUnswitchedDepreciation(cost, salvageValue, usefulLife, startPeriod, factor);
        decimal rate = Math.Min(1m, factor / usefulLife);
        decimal balance = cost;
        decimal result = 0m;
        bool switched = false;
        int lastPeriod = checked((int)Math.Ceiling(endPeriod));
        for (int index = 0; index < lastPeriod && balance > salvageValue; index++)
        {
            decimal remaining = balance - salvageValue;
            decimal declining = Math.Min(remaining, balance * rate);
            decimal straight = remaining / (usefulLife - index);
            if (!switched && straight > declining) switched = true;
            decimal expense = switched ? Math.Min(remaining, straight) : declining;
            decimal overlapStart = Math.Max(startPeriod, index);
            decimal overlapEnd = Math.Min(endPeriod, index + 1m);
            if (overlapEnd > overlapStart) result += expense * (overlapEnd - overlapStart);
            balance -= expense;
        }
        return result;
    }
}

