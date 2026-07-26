using ERPSystem.Application.Common;
using ERPSystem.Application.DTOs.Customers;

namespace ERPSystem.Application.Tests.UseCases.Customers;

public sealed class CustomerLedgerReconciliationLayoutTests
{
    [Fact]
    public void Exact_reconciliation_entry_defines_green_cutoff_and_marker_position()
    {
        var first = Line(new DateTime(2026, 7, 20), Guid.NewGuid());
        var matched = Line(new DateTime(2026, 7, 22), Guid.NewGuid());
        var pending = Line(new DateTime(2026, 7, 26), Guid.NewGuid());

        var layout = CustomerLedgerReconciliationLayoutResolver.Resolve(
            [first, matched, pending],
            matched.EntryId,
            matched.TransactionDate);

        Assert.True(layout.HasReconciliation);
        Assert.Equal(1, layout.ReconciledCutoffIndex);
        Assert.Equal(2, layout.MarkerInsertIndex);
    }

    [Fact]
    public void Reconciliation_before_visible_period_places_marker_before_all_pending_rows()
    {
        var firstVisible = Line(new DateTime(2026, 7, 20), Guid.NewGuid());
        var secondVisible = Line(new DateTime(2026, 7, 22), Guid.NewGuid());

        var layout = CustomerLedgerReconciliationLayoutResolver.Resolve(
            [firstVisible, secondVisible],
            Guid.NewGuid(),
            new DateTime(2026, 7, 15));

        Assert.True(layout.HasReconciliation);
        Assert.Equal(-1, layout.ReconciledCutoffIndex);
        Assert.Equal(0, layout.MarkerInsertIndex);
    }

    [Fact]
    public void Missing_reconciliation_keeps_all_rows_pending_without_marker()
    {
        var layout = CustomerLedgerReconciliationLayoutResolver.Resolve(
            [Line(new DateTime(2026, 7, 20), Guid.NewGuid())],
            null,
            null);

        Assert.False(layout.HasReconciliation);
        Assert.Equal(-1, layout.ReconciledCutoffIndex);
        Assert.Equal(-1, layout.MarkerInsertIndex);
    }

    private static CustomerAccountLedgerLineDto Line(DateTime date, Guid entryId) => new()
    {
        EntryId = entryId,
        TransactionDate = date
    };
}
