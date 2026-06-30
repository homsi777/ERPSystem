namespace ERPSystem.Services.China;

public sealed class ChinaImportCostEntryInput
{
    public decimal ChinaInvoiceAmountUsd { get; init; }
    public decimal ContainerWeightKg { get; init; }
    public decimal CustomsAmountUsd { get; init; }
    public decimal ShippingUsd { get; init; }
    public decimal ClearanceUsd { get; init; }
    public decimal OtherExpensesUsd { get; init; }
}
