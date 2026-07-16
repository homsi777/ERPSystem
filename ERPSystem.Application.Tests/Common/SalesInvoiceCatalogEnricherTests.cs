using ERPSystem.Application.Common;
using ERPSystem.Application.DTOs.Sales;

namespace ERPSystem.Application.Tests.Common;

public sealed class SalesInvoiceCatalogEnricherTests
{
    [Fact]
    public void WithEnrichedRolls_preserves_warehouse_id()
    {
        var warehouseId = Guid.NewGuid();
        var detailing = new WarehouseDetailingDto
        {
            InvoiceId = Guid.NewGuid(),
            InvoiceNumber = "INV-TEST",
            WarehouseId = warehouseId
        };

        var result = SalesInvoiceCatalogEnricher.WithEnrichedRolls(detailing, []);

        Assert.Equal(warehouseId, result.WarehouseId);
    }
}
