using ERPSystem.Application.Common;
using ERPSystem.Application.Commands.Finance;
using ERPSystem.Application.Mapping;
using ERPSystem.Domain.Enums;
using ERPSystem.Domain.Exceptions;
using ERPSystem.Infrastructure.Persistence.Models.Catalog;
using ERPSystem.Infrastructure.Services;

namespace ERPSystem.Application.Tests;

public sealed class InventoryOpeningBalancePolicyTests
{
    [Fact]
    public void Opening_stock_line_preserves_catalog_ids_at_entry_time()
    {
        var itemId = Guid.NewGuid();
        var colorId = Guid.NewGuid();

        var line = OpeningBalanceMappers.ToDomainLine(new OpeningBalanceLineInput
        {
            FabricItemId = itemId,
            FabricColorId = colorId,
            ItemName = "fabric",
            ColorName = "color",
            Quantity = 100m,
            RollCount = 5,
            UnitCost = 2m,
            Debit = 200m
        });

        Assert.Equal(itemId, line.FabricItemId);
        Assert.Equal(colorId, line.FabricColorId);
    }

    [Fact]
    public void Unified_opening_balances_use_finance_source_type()
    {
        Assert.Equal(DocumentType.FinanceOpeningBalance, OpeningBalanceDocumentTypePolicy.SourceType);
        Assert.NotEqual(DocumentType.CustomerOpeningBalance, OpeningBalanceDocumentTypePolicy.SourceType);
        Assert.NotEqual(DocumentType.SupplierOpeningBalance, OpeningBalanceDocumentTypePolicy.SourceType);
    }

    [Fact]
    public void First_real_length_can_exceed_legacy_provisional_length()
    {
        var roll = Roll(isLegacy: true, confirmed: false, length: 20m);

        var applied = LegacyOpeningBalanceRollLengthPolicy.TryApplyFirstRealLength(roll, 27.5m);

        Assert.True(applied);
        Assert.Equal(27.5m, roll.LengthMeters);
        Assert.Equal(27.5m, roll.RemainingLengthMeters);
        Assert.True(roll.LegacyLengthConfirmed);
    }

    [Fact]
    public void China_roll_never_receives_legacy_override()
    {
        var roll = Roll(isLegacy: false, confirmed: true, length: 20m);

        var applied = LegacyOpeningBalanceRollLengthPolicy.TryApplyFirstRealLength(roll, 27.5m);

        Assert.False(applied);
        Assert.Equal(20m, roll.LengthMeters);
        Assert.Equal(20m, roll.RemainingLengthMeters);
    }

    [Fact]
    public void China_roll_still_rejects_length_above_remaining()
    {
        var roll = Roll(isLegacy: false, confirmed: true, length: 20m);

        Assert.Throws<InventoryException>(() =>
            LegacyOpeningBalanceRollLengthPolicy.ResolveAndValidateSaleLength(roll, 27.5m));

        Assert.Equal(20m, roll.LengthMeters);
        Assert.Equal(20m, roll.RemainingLengthMeters);
    }

    [Fact]
    public void Confirmed_legacy_roll_cannot_be_overridden_again()
    {
        var roll = Roll(isLegacy: true, confirmed: true, length: 20m);

        var applied = LegacyOpeningBalanceRollLengthPolicy.TryApplyFirstRealLength(roll, 27.5m);

        Assert.False(applied);
        Assert.Equal(20m, roll.LengthMeters);
        Assert.Equal(20m, roll.RemainingLengthMeters);
    }

    private static FabricRollEntity Roll(bool isLegacy, bool confirmed, decimal length) => new()
    {
        Id = Guid.NewGuid(),
        ContainerId = isLegacy ? Guid.Empty : Guid.NewGuid(),
        FabricItemId = Guid.NewGuid(),
        FabricColorId = Guid.NewGuid(),
        WarehouseId = Guid.NewGuid(),
        LengthMeters = length,
        RemainingLengthMeters = length,
        IsLegacyOpeningBalance = isLegacy,
        LegacyLengthConfirmed = confirmed
    };
}
