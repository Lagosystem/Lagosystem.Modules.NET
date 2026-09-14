namespace Lagosystem.Modules.Calc.Financials;


public static partial class Financials
{
    /// <summary>Calculates straight-line depreciation for one accounting period.</summary>
    /// <param name="cost">Nonnegative acquisition cost.</param>
    /// <param name="salvageValue">Nonnegative residual value, not greater than cost.</param>
    /// <param name="usefulLife">Positive number of accounting periods.</param>
    /// <returns>The equal depreciation expense per period.</returns>
    public static decimal CalculateStraightLineDepreciation(decimal cost, decimal salvageValue, decimal usefulLife)
    {
        ValidateDepreciation(cost, salvageValue, usefulLife);
        return (cost - salvageValue) / usefulLife;
    }

    /// <summary>Calculates depreciation using the sum-of-years' digits method.</summary>
    /// <param name="cost">Nonnegative acquisition cost.</param>
    /// <param name="salvageValue">Nonnegative residual value, not greater than cost.</param>
    /// <param name="usefulLife">Positive useful life in periods.</param>
    /// <param name="period">Period to calculate, greater than zero and not greater than useful life.</param>
    /// <returns>The depreciation expense for the selected period.</returns>
    public static decimal CalculateSumOfYearsDigitsDepreciation(decimal cost, decimal salvageValue, decimal usefulLife, decimal period)
    {
        ValidateDepreciation(cost, salvageValue, usefulLife);
        if (period <= 0m || period > usefulLife) throw new ArgumentOutOfRangeException(nameof(period));
        return (cost - salvageValue) * 2m * (usefulLife - period + 1m) / (usefulLife * (usefulLife + 1m));
    }

    /// <summary>Calculates depreciation using a declining balance with a configurable acceleration factor.</summary>
    /// <remarks>Equivalent to Excel's DDB/BDD. The period is rounded upward, matching Excel's treatment
    /// of fractional periods. The book value is never depreciated below the salvage value.</remarks>
    /// <param name="cost">Nonnegative acquisition cost.</param>
    /// <param name="salvageValue">Nonnegative residual value, not greater than cost.</param>
    /// <param name="usefulLife">Positive useful life in periods.</param>
    /// <param name="period">Selected period, greater than zero and not greater than useful life.</param>
    /// <param name="factor">Positive acceleration factor, normally two.</param>
    /// <returns>The depreciation expense for the selected period.</returns>
    public static decimal CalculateDoubleDecliningBalanceDepreciation(decimal cost, decimal salvageValue, decimal usefulLife,
        decimal period, decimal factor = 2m)
    {
        ValidateDepreciation(cost, salvageValue, usefulLife);
        RequirePositive(factor, nameof(factor));
        if (period <= 0m || period > usefulLife) throw new ArgumentOutOfRangeException(nameof(period));
        if (cost == 0m || cost == salvageValue) return 0m;
        double rate = Math.Min((double)(factor / usefulLife), 1.0);
        if (rate == 0.0) return 0m;
        if (rate == 1.0) return period <= 1m ? cost - salvageValue : 0m;
        double previousBookValue = (double)cost * Math.Pow(1.0 - rate, (double)Math.Ceiling(period) - 1.0);
        double expense = Math.Min(previousBookValue * rate, previousBookValue - (double)salvageValue);
        return CheckedDecimal(Math.Max(0.0, expense));
    }

    /// <summary>Calculates fixed-declining-balance depreciation using Excel's three-decimal depreciation rate.</summary>
    /// <remarks>Equivalent to Excel's DB/BD. The first year may contain fewer than twelve months;
    /// the final partial year is prorated. This method intentionally applies the Excel rate-rounding convention.</remarks>
    /// <param name="cost">Nonnegative acquisition cost.</param>
    /// <param name="salvageValue">Nonnegative residual value, not greater than cost.</param>
    /// <param name="usefulLife">Positive useful life in years.</param>
    /// <param name="period">One-based accounting period.</param>
    /// <param name="monthsInFirstYear">Number of months in the first year, from one through twelve.</param>
    /// <returns>The depreciation expense for the selected period.</returns>
    public static decimal CalculateFixedDecliningBalanceDepreciation(decimal cost, decimal salvageValue, decimal usefulLife,
        int period, int monthsInFirstYear = 12)
    {
        ValidateDepreciation(cost, salvageValue, usefulLife);
        RequirePositive(period, nameof(period));
        if (monthsInFirstYear < 1 || monthsInFirstYear > 12) throw new ArgumentOutOfRangeException(nameof(monthsInFirstYear));
        if (period > Math.Ceiling(usefulLife) + (monthsInFirstYear == 12 ? 0 : 1))
            throw new ArgumentOutOfRangeException(nameof(period));
        if (cost == 0m || cost == salvageValue) return 0m;
        double rate = (double)Math.Round(CheckedDecimal(1.0 - Math.Pow((double)(salvageValue / cost), 1.0 / (double)usefulLife)), 3, MidpointRounding.AwayFromZero);
        double bookValue = (double)cost;
        double expense = bookValue * rate * monthsInFirstYear / 12.0;
        if (period == 1) return CheckedDecimal(Math.Min(expense, bookValue - (double)salvageValue));
        bookValue -= expense;
        for (int index = 2; index <= period; index++)
        {
            expense = bookValue * rate;
            if (index == Math.Ceiling((double)usefulLife) + 1 && monthsInFirstYear < 12)
                expense *= (12.0 - monthsInFirstYear) / 12.0;
            expense = Math.Max(0.0, Math.Min(expense, bookValue - (double)salvageValue));
            bookValue -= expense;
        }
        return CheckedDecimal(expense);
    }

    private static void ValidateDepreciation(decimal cost, decimal salvageValue, decimal usefulLife)
    {
        RequireNonNegative(cost, nameof(cost));
        RequireNonNegative(salvageValue, nameof(salvageValue));
        RequirePositive(usefulLife, nameof(usefulLife));
        if (salvageValue > cost) throw new ArgumentOutOfRangeException(nameof(salvageValue));
    }
}

