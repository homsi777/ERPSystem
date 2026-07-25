using ERPSystem.Application.Documents;
using ERPSystem.Application.DTOs.Containers;
using ERPSystem.Application.DTOs.Inventory;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.Tests.Documents;

public sealed class ChinaContainerPdfGeneratorTests
{
    [Fact]
    public void Generate_WithArabicContainerData_ProducesPdf()
    {
        var apiRoot = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "ERPSystem.Api"));
        var generator = ChinaContainerPdfGenerator.FromContentRoot(apiRoot);

        var pdf = generator.Generate(new ContainerOperationsCenterDto
        {
            Container = new ContainerDetailsDto
            {
                Id = Guid.NewGuid(),
                ContainerNumber = "092",
                SupplierName = "مورد قوانغتشو",
                ShipmentDate = new DateTime(2026, 7, 22),
                ArrivalDate = new DateTime(2026, 8, 15),
                Status = ChinaContainerStatus.InWarehouse,
                // Legacy containers can have zero aggregate totals while their
                // posted inventory contains the authoritative figures.
                TotalRolls = 0,
                TotalMeters = 0m,
                ChinaInvoiceAmountUsd = 0m,
                ExchangeRateToLocalCurrency = 1m,
                DplQuantityUnit = DplQuantityUnit.Yards,
                FabricTypeLines = []
            },
            Inventory = new ContainerInventoryMetricsDto
            {
                TotalRolls = 500,
                TotalMeters = 41_148m,
                AvailableRolls = 460,
                AvailableMeters = 37_856.16m,
                ReservedRolls = 40,
                ReservedMeters = 3_291.84m,
                InventoryValuation = 0m,
                IsStockPosted = true
            },
            IsReadyForSale = true,
            LinkedPurchaseInvoiceNumber = "PUR-092"
        });

        Assert.True(pdf.Length > 10_000);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
    }
}
