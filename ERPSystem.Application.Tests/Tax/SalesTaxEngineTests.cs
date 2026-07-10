using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Tax;

namespace ERPSystem.Application.Tests.Tax;

public sealed class SalesTaxEngineTests
{
    private readonly ISalesTaxEngine _engine = new SalesTaxEngine();

    [Fact]
    public void Tax_exclusive_15_percent_single_line()
    {
        var result = _engine.Calculate(new SalesTaxCalculationRequest
        {
            Currency = "USD",
            InvoiceDiscountTotal = 0m,
            Lines =
            [
                Line(1000m, rate: 0.15m, exclusive: true)
            ]
        });

        Assert.Equal(1000m, result.TaxableAmount);
        Assert.Equal(150m, result.TaxTotal);
        Assert.Equal(1150m, result.GrandTotal);
        Assert.Equal(0m, result.RoundingDifference);
    }

    [Fact]
    public void Tax_inclusive_15_percent_single_line()
    {
        var result = _engine.Calculate(new SalesTaxCalculationRequest
        {
            Currency = "USD",
            InvoiceDiscountTotal = 0m,
            Lines =
            [
                Line(1150m, rate: 0.15m, exclusive: false)
            ]
        });

        Assert.Equal(1000m, result.TaxableAmount);
        Assert.Equal(150m, result.TaxTotal);
        Assert.Equal(1150m, result.GrandTotal);
        Assert.Equal(0m, result.RoundingDifference);
    }

    [Fact]
    public void Exempt_line_has_zero_tax()
    {
        var result = _engine.Calculate(new SalesTaxCalculationRequest
        {
            Currency = "USD",
            InvoiceDiscountTotal = 0m,
            Lines =
            [
                new SalesTaxLineInput
                {
                    LineId = Guid.NewGuid(),
                    NetLineAmount = 500m,
                    LineDiscountTotal = 0m,
                    TaxCodeId = Guid.NewGuid(),
                    TaxCode = "EXEMPT",
                    TaxName = "Exempt",
                    TaxRate = 0m,
                    IsInclusive = false,
                    IsExempt = true,
                    IsZeroRated = false,
                    SalesTaxAccountId = null
                }
            ]
        });

        Assert.Equal(0m, result.TaxTotal);
        Assert.Equal(500m, result.GrandTotal);
    }

    [Fact]
    public void Invoice_discount_reduces_taxable_base()
    {
        var result = _engine.Calculate(new SalesTaxCalculationRequest
        {
            Currency = "USD",
            InvoiceDiscountTotal = 100m,
            Lines =
            [
                Line(1000m, rate: 0.15m, exclusive: true)
            ]
        });

        Assert.Equal(900m, result.TaxableAmount);
        Assert.Equal(135m, result.TaxTotal);
        Assert.Equal(1035m, result.GrandTotal);
    }

    [Fact]
    public void Multiple_rates_aggregate_correctly()
    {
        var result = _engine.Calculate(new SalesTaxCalculationRequest
        {
            Currency = "USD",
            InvoiceDiscountTotal = 0m,
            Lines =
            [
                Line(1000m, rate: 0.15m, exclusive: true, code: "VAT15"),
                Line(500m, rate: 0.05m, exclusive: true, code: "VAT5")
            ]
        });

        Assert.Equal(150m, result.TaxSummary.First(t => t.TaxCode == "VAT15").TaxAmount);
        Assert.Equal(25m, result.TaxSummary.First(t => t.TaxCode == "VAT5").TaxAmount);
        Assert.Equal(175m, result.TaxTotal);
        Assert.Equal(1675m, result.GrandTotal);
    }

    private static SalesTaxLineInput Line(
        decimal net,
        decimal rate,
        bool exclusive,
        bool exempt = false,
        string code = "VAT15") =>
        new()
        {
            LineId = Guid.NewGuid(),
            NetLineAmount = net,
            LineDiscountTotal = 0m,
            TaxCodeId = Guid.NewGuid(),
            TaxCode = code,
            TaxName = code,
            TaxRate = rate,
            IsInclusive = !exclusive,
            IsExempt = exempt,
            IsZeroRated = false,
            SalesTaxAccountId = Guid.NewGuid()
        };
}
