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
                TotalRolls = 5,
                TotalMeters = 5374m,
                ChinaInvoiceAmountUsd = 1511.81m,
                ExchangeRateToLocalCurrency = 1m,
                DplQuantityUnit = DplQuantityUnit.Meters,
                FabricTypeLines =
                [
                    new ContainerFabricTypeLineDto
                    {
                        LineNumber = 1,
                        TypeDisplayName = "قماش أسود",
                        RollCount = 5,
                        LengthMeters = 5374m,
                        NetWeightKg = 950m,
                        SalePricePerMeterUsd = 3.75m
                    }
                ]
            },
            Inventory = new ContainerInventoryMetricsDto
            {
                TotalRolls = 5,
                TotalMeters = 5374m,
                AvailableRolls = 5,
                AvailableMeters = 5374m,
                InventoryValuation = 1511.81m,
                IsStockPosted = true
            },
            IsReadyForSale = true,
            LinkedPurchaseInvoiceNumber = "PUR-092"
        });

        Assert.True(pdf.Length > 10_000);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
    }
}
