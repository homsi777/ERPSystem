using ERPSystem.Domain.Entities.ChinaImport;
using ERPSystem.Domain.ValueObjects;

namespace ERPSystem.Domain.Services;

public static class LandingCostCalculator
{
    public static Money CalculateTotalImportExpenses(LandingCost landingCost) =>
        landingCost.TotalImportExpenses;

    public static decimal CalculateCustomsCostPerMeter(LandingCost landingCost) =>
        landingCost.CustomsCostPerMeter;

    public static decimal CalculateExpenseCostPerMeter(LandingCost landingCost) =>
        landingCost.ExpenseCostPerMeter;

    public static decimal CalculateAvgGramPerMeter(LandingCost landingCost) =>
        landingCost.AvgGramPerMeter;

    public static Money CalculateLandedCostPerMeter(LandingCost landingCost, Money fabricUnitCost)
    {
        var perMeter = landingCost.ExpenseCostPerMeter;
        return fabricUnitCost.Add(new Money(perMeter));
    }
}
