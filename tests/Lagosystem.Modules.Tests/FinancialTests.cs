using Lagosystem.Modules.Calc.Financials;

namespace Lagosystem.Modules.Tests;

public sealed class FinancialTests
{
    [Fact]
    public void CompoundInterestAndPresentValueHaveKnownValues()
    {
        Assert.Equal(1210m, Financials.CalculateFutureValueWithDecimalPrecision(1000m, 10m, 2));
        Assert.Equal(1210m, Financials.CalculateFutureValue(1000m, 10m, 2));
        Assert.Equal(1000m, Financials.CalculatePresentValue(1210m, 10m, 2));
        Assert.Equal(1000m, Financials.CalculateFutureValue(1000m, 10m, 0));
        Assert.Equal(1000m, Financials.CalculatePresentValue(1000m, 0m, 20));
        Assert.Equal(900m, Financials.CalculateFutureValue(1000m, -10m, 1));
        Assert.Equal(1320m, Financials.CalculateFutureValueWithVariableRates(1000m, new decimal[] { 10m, 20m }));
    }

    [Fact]
    public void PercentagesCurrencyAndRoundingUseDocumentedScales()
    {
        Assert.Equal(25m, Financials.CalculatePercentageAmount(200m, 12.5m));
        Assert.Equal(50m, Financials.CalculatePercentageChange(100m, 150m));
        Assert.Equal(200m, Financials.CalculateSimpleInterest(1000m, 10m, 2m));
        Assert.Equal(12.35m, Financials.ConvertToPercentage(0.12345m, 2));
        Assert.Equal(50m, Financials.ConvertCurrency(10m, 5m));
        Assert.Equal(10m, Financials.ConvertCurrency(50m, 5m, true));
        Assert.Equal(1.01m, Financials.RoundMonetaryAmount(1.005m));
        Assert.Equal(-1.01m, Financials.RoundMonetaryAmount(-1.005m));
        Assert.Equal(1.00m, Financials.RoundMonetaryAmount(1.005m, rounding: MidpointRounding.ToEven));
    }

    [Fact]
    public void LoanAndAnnuityPaymentsKeepCashFlowSigns()
    {
        Assert.Equal(100m, Financials.CalculatePeriodicPayment(0m, 12, 1200m));
        Assert.Equal(-100m, Financials.CalculateAnnuityPayment(0m, 12, 1200m));
        AssertClose(576.190476190476m, Financials.CalculatePeriodicPayment(10m, 2, 1000m));
        AssertClose(-576.190476190476m, Financials.CalculateAnnuityPayment(10m, 2, 1000m));
        AssertClose(-523.809523809524m,
            Financials.CalculateAnnuityPayment(10m, 2, 1000m, timing: FinancialPaymentTiming.BeginningOfPeriod));
        Assert.Equal(1200m, Financials.CalculateFutureValueOfAnnuity(0m, 12, -100m));
        Assert.Equal(1200m, Financials.CalculatePresentValueOfAnnuity(0m, 12, -100m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(10)]
    public void LoanScheduleConservesPrincipalAndEndsAtZero(int percentageRate)
    {
        FinancialLoanSchedule schedule = new FinancialLoanSchedule(1000m, percentageRate, 12);
        decimal principalPaid = 0m;
        decimal previousBalance = 1000m;
        int count = 0;

        while (schedule.MoveNext(out FinancialLoanInstallment installment))
        {
            count++;
            Assert.Equal(count, installment.Period);
            Assert.Equal(previousBalance, installment.OpeningBalance);
            AssertClose(installment.Principal + installment.Interest, installment.Payment);
            Assert.Equal(installment.OpeningBalance - installment.Principal, installment.ClosingBalance);
            Assert.InRange(installment.ClosingBalance, 0m, previousBalance);
            principalPaid += installment.Principal;
            previousBalance = installment.ClosingBalance;
        }

        Assert.Equal(12, count);
        AssertClose(1000m, principalPaid);
        Assert.Equal(0m, schedule.RemainingBalance);
        Assert.False(schedule.MoveNext(out FinancialLoanInstallment _));
    }

    [Fact]
    public void PrincipalAndInterestPartsReconcileWithPayment()
    {
        decimal payment = Financials.CalculateAnnuityPayment(10m, 2, 1000m);
        for (int period = 1; period <= 2; period++)
        {
            decimal interest = Financials.CalculatePeriodicInterestPayment(10m, period, 2, 1000m);
            decimal principal = Financials.CalculatePeriodicPrincipalPayment(10m, period, 2, 1000m);
            AssertClose(payment, interest + principal);
        }
        AssertClose(-1000m, Financials.CalculateCumulativePrincipalPayment(10m, 2, 1000m, 1, 2));
        Assert.Equal(0m, Financials.CalculatePeriodicInterestPayment(10m, 1, 2, 1000m,
            timing: FinancialPaymentTiming.BeginningOfPeriod));
    }

    [Fact]
    public void NetPresentValueDiscountsFirstCashFlowAtPeriodOne()
    {
        decimal[] futureCashFlows = new decimal[] { 110m, 121m };
        AssertClose(200m, Financials.CalculateNetPresentValue(10m, futureCashFlows));
        AssertClose(0m, Financials.CalculateNetPresentValueIncludingInitialCashFlow(10m, -200m, futureCashFlows));
        Assert.Equal(231m, Financials.CalculateNetPresentValue(0m, futureCashFlows));
    }

    [Fact]
    public void InternalRatesSolveKnownCashFlows()
    {
        decimal[] cashFlows = new decimal[] { -100m, 0m, 121m };
        FinancialCalculationResult result = Financials.CalculateInternalRateOfReturnWithDiagnostics(cashFlows);
        AssertClose(10m, result.Value);
        Assert.InRange(Math.Abs(result.Residual), 0.0, 1e-8);
        Assert.True(result.EstimatedAbsoluteError >= 0m);
        AssertClose(10m, Financials.CalculateModifiedInternalRateOfReturn(cashFlows, 5m, 5m));
    }

    [Fact]
    public void DatedCashFlowsUseActualDaysWith365DayYear()
    {
        decimal[] cashFlows = new decimal[] { -100m, 110m };
        DateTime[] dates = new DateTime[] { new DateTime(2025, 1, 1), new DateTime(2026, 1, 1) };
        AssertClose(0m, Financials.CalculateNetPresentValueWithDates(10m, cashFlows, dates));
        AssertClose(10m, Financials.CalculateInternalRateOfReturnWithDates(cashFlows, dates));
        Assert.Equal(10m, Financials.CalculateNetPresentValueWithDates(0m, cashFlows, dates));
        Assert.Throws<ArgumentException>(() => Financials.CalculateNetPresentValueWithDates(10m, cashFlows,
            new DateTime[] { dates[0] }));
    }

    [Fact]
    public void EffectiveAndNominalRatesAreConsistent()
    {
        decimal effective = Financials.CalculateEffectiveAnnualInterestRate(12m, 12);
        AssertClose(12.682503013197m, effective);
        AssertClose(12m, Financials.CalculateNominalAnnualInterestRate(effective, 12));
    }

    [Fact]
    public void DepreciationUsesKnownValuesAndRespectsSalvage()
    {
        Assert.Equal(300m, Financials.CalculateStraightLineDepreciation(1000m, 100m, 3m));
        Assert.Equal(450m, Financials.CalculateSumOfYearsDigitsDepreciation(1000m, 100m, 3m, 1m));
        Assert.Equal(300m, Financials.CalculateSumOfYearsDigitsDepreciation(1000m, 100m, 3m, 2m));
        Assert.Equal(150m, Financials.CalculateSumOfYearsDigitsDepreciation(1000m, 100m, 3m, 3m));
        decimal total = 0m;
        for (int period = 1; period <= 5; period++)
        {
            decimal expense = Financials.CalculateDoubleDecliningBalanceDepreciation(1000m, 100m, 5m, period);
            Assert.InRange(expense, 0m, 900m - total + 0.00000001m);
            total += expense;
        }
        AssertClose(900m, total);
    }

    [Theory]
    [InlineData(-100)]
    [InlineData(-101)]
    public void CompoundingRejectsRatesAtOrBelowMinus100(int rate)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Financials.CalculateFutureValue(100m, rate, 1));
    }

    [Fact]
    public void InvalidFinancialInputsFailExplicitly()
    {
        Assert.Throws<DivideByZeroException>(() => Financials.CalculatePeriodicPayment(10m, 0, 100m));
        Assert.Throws<DivideByZeroException>(() => Financials.CalculatePercentageChange(0m, 10m));
        Assert.Throws<ArgumentException>(() => Financials.CalculateFutureValue(100m, 10m, -1));
        Assert.Throws<ArgumentException>(() => Financials.CalculateInternalRateOfReturn(new decimal[] { 10m, 20m }));
        Assert.Throws<ArgumentOutOfRangeException>(() => Financials.CalculateStraightLineDepreciation(100m, 101m, 2m));
        Assert.Throws<ArgumentOutOfRangeException>(() => Financials.CalculateAnnuityPayment(1m, 12, 100m,
            timing: (FinancialPaymentTiming)999));
    }

    private static void AssertClose(decimal expected, decimal actual, decimal tolerance = 0.00000001m) =>
        Assert.InRange(Math.Abs(expected - actual), 0m, tolerance);
}
