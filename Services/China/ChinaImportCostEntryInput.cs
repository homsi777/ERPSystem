namespace ERPSystem.Services.China;

public sealed class ChinaImportCostEntryInput
{
    public decimal ChinaInvoiceAmountUsd { get; init; }
    public decimal ContainerWeightKg { get; init; }
    public decimal CustomsClearanceUsd { get; init; }
    public decimal ShippingUsd { get; init; }
    public decimal InsuranceUsd { get; init; }
    public decimal OtherExpense1Usd { get; init; }
    public decimal OtherExpense2Usd { get; init; }
    public decimal OtherExpense3Usd { get; init; }
    public decimal OtherExpense4Usd { get; init; }
    public bool UsesWeightedAllocation { get; init; }

    public string? ChinaInvoiceNote { get; init; }
    public string? ShippingNote { get; init; }
    public string? InsuranceNote { get; init; }
    public string? CustomsClearanceNote { get; init; }
    public string? OtherExpense1Note { get; init; }
    public string? OtherExpense2Note { get; init; }
    public string? OtherExpense3Note { get; init; }
    public string? OtherExpense4Note { get; init; }

    // Legacy DPL-only flat fallback
    public decimal CustomsAmountUsd { get; init; }
    public decimal ClearanceUsd { get; init; }
    public decimal OtherExpensesUsd { get; init; }
}
