using ERPSystem.Application.DTOs.Customers;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.Tests.UseCases.Customers;

public sealed class CustomerAccountLedgerAccountingVerificationTests
{
    [Fact]
    public void RunningBalance_is_cumulative_for_each_movement_type()
    {
        var lines = new List<CustomerAccountLedgerLineDto>
        {
            new()
            {
                MovementType = CustomerAccountMovementType.SalesInvoice,
                LineAmount = 500m,
                RunningBalance = 500m
            },
            new()
            {
                MovementType = CustomerAccountMovementType.SalesInvoice,
                LineAmount = 500m,
                RunningBalance = 1000m
            },
            new()
            {
                MovementType = CustomerAccountMovementType.SalesInvoice,
                LineAmount = -100m,
                RunningBalance = 900m
            },
            new()
            {
                MovementType = CustomerAccountMovementType.ReceiptVoucher,
                LineAmount = -200m,
                RunningBalance = 700m
            },
            new()
            {
                MovementType = CustomerAccountMovementType.SalesReturn,
                LineAmount = 100m,
                RunningBalance = 600m
            }
        };

        AssertRunningBalanceIsCumulative(lines, opening: 0m);
        Assert.Equal(600m, lines[^1].RunningBalance);
    }

    [Fact]
    public void Legacy_vs_new_closing_formula_with_returns_and_discount()
    {
        const decimal opening = 0m;
        const decimal invoiceSubTotal = 1000m;
        const decimal invoiceDiscount = 100m;
        const decimal invoiceGrandTotal = invoiceSubTotal - invoiceDiscount;
        const decimal receiptAmount = 200m;
        const decimal returnAmount = 100m;

        var legacyClosing = opening + invoiceGrandTotal - receiptAmount;
        var newClosing = opening + invoiceGrandTotal - receiptAmount - returnAmount;

        Assert.Equal(700m, legacyClosing);
        Assert.Equal(600m, newClosing);
        Assert.Equal(returnAmount, legacyClosing - newClosing);
    }

    private static void AssertRunningBalanceIsCumulative(
        IReadOnlyList<CustomerAccountLedgerLineDto> lines,
        decimal opening)
    {
        var running = opening;
        foreach (var line in lines)
        {
            running += line.MovementType switch
            {
                CustomerAccountMovementType.SalesInvoice => line.LineAmount,
                CustomerAccountMovementType.SalesReturn => -line.LineAmount,
                CustomerAccountMovementType.ReceiptVoucher => line.LineAmount,
                _ => 0m
            };

            Assert.Equal(running, line.RunningBalance);
        }
    }
}
