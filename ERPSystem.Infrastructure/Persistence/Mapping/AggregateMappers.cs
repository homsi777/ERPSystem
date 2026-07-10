using ERPSystem.Application.Common;
using System.Reflection;
using ERPSystem.Domain.Aggregates;
using ERPSystem.Domain.Entities.Accounting;
using ERPSystem.Domain.Entities.ChinaImport;
using ERPSystem.Domain.Entities.Finance;
using ERPSystem.Domain.Entities.Inventory;
using ERPSystem.Domain.Entities.Purchasing;
using ERPSystem.Domain.Entities.Sales;
using ERPSystem.Domain.Enums;
using ERPSystem.Domain.ValueObjects;
using ERPSystem.Infrastructure.Persistence.Models.Accounting;
using ERPSystem.Infrastructure.Persistence.Models.ChinaImport;
using ERPSystem.Infrastructure.Persistence.Models.Finance;
using ERPSystem.Infrastructure.Persistence.Models.Inventory;
using ERPSystem.Infrastructure.Persistence.Models.Purchasing;
using ERPSystem.Infrastructure.Persistence.Models.Sales;

namespace ERPSystem.Infrastructure.Persistence.Mapping;

internal static class AggregateCollectionHelper
{
    public static void SetPrivateList<TAggregate, TItem>(TAggregate aggregate, string fieldName, IEnumerable<TItem> items)
    {
        var field = typeof(TAggregate).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        var list = (IList<TItem>)field!.GetValue(aggregate)!;
        list.Clear();
        foreach (var item in items)
            list.Add(item);
    }
}

internal static class SalesInvoiceMapper
{
    public static SalesInvoiceEntity ToHeaderEntity(SalesInvoiceAggregate aggregate) => new()
    {
        Id = aggregate.Id,
        CompanyId = aggregate.CompanyId,
        BranchId = aggregate.BranchId,
        InvoiceNumber = aggregate.InvoiceNumber.Value,
        CustomerId = aggregate.CustomerId,
        WarehouseId = aggregate.WarehouseId,
        ChinaContainerId = aggregate.ChinaContainerId,
        InvoiceDate = aggregate.InvoiceDate,
        PaymentType = (int)aggregate.PaymentType,
        PartialPaymentAmount = aggregate.PartialPaymentAmount?.Amount,
        CashboxId = aggregate.CashboxId,
        Status = (int)aggregate.Status,
        SubTotal = aggregate.SubTotal.Amount,
        DiscountTotal = aggregate.DiscountTotal.Amount,
        TaxTotal = aggregate.TaxTotal.Amount,
        GrandTotal = aggregate.GrandTotal.Amount,
        RoundingDifference = aggregate.RoundingDifference,
        IsLegacyUntaxed = aggregate.IsLegacyUntaxed,
        CreatedByUserId = aggregate.CreatedByUserId,
        ApprovedByUserId = aggregate.ApprovedByUserId,
        SentToWarehouseAt = aggregate.SentToWarehouseAt,
        DetailedAt = aggregate.DetailedAt,
        ApprovedAt = aggregate.ApprovedAt,
        PrintedAt = aggregate.PrintedAt,
        DeliveredAt = aggregate.DeliveredAt,
        DeliveredToName = aggregate.DeliveredToName,
        DeliveryDriverName = aggregate.DeliveryDriverName,
        DeliveryNotes = aggregate.DeliveryNotes,
        CancelledAt = aggregate.CancelledAt,
        CancelReason = aggregate.CancelReason,
        ReversedByJournalEntryId = aggregate.ReversedByJournalEntryId,
        IsArchived = aggregate.IsArchived
    };

    public static SalesInvoiceAggregate ToAggregate(
        SalesInvoiceEntity header,
        IReadOnlyList<SalesInvoiceItemEntity> items,
        IReadOnlyList<SalesInvoiceRollDetailEntity> rolls,
        WarehouseDetailingSessionEntity? session,
        IReadOnlyList<SalesInvoiceItemTaxEntity> itemTaxes)
    {
        var aggregate = DomainHydrator.Create<SalesInvoiceAggregate>();
        DomainHydrator.Set(aggregate, nameof(SalesInvoiceAggregate.Id), header.Id);
        DomainHydrator.Set(aggregate, nameof(SalesInvoiceAggregate.InvoiceNumber), new InvoiceNumber(header.InvoiceNumber));
        DomainHydrator.Set(aggregate, nameof(SalesInvoiceAggregate.CompanyId), header.CompanyId);
        DomainHydrator.Set(aggregate, nameof(SalesInvoiceAggregate.BranchId), header.BranchId);
        DomainHydrator.Set(aggregate, nameof(SalesInvoiceAggregate.CustomerId), header.CustomerId);
        DomainHydrator.Set(aggregate, nameof(SalesInvoiceAggregate.WarehouseId), header.WarehouseId);
        DomainHydrator.Set(aggregate, nameof(SalesInvoiceAggregate.ChinaContainerId), header.ChinaContainerId);
        DomainHydrator.Set(aggregate, nameof(SalesInvoiceAggregate.InvoiceDate), header.InvoiceDate);
        DomainHydrator.Set(aggregate, nameof(SalesInvoiceAggregate.PaymentType), (PaymentType)header.PaymentType);
        DomainHydrator.Set(aggregate, nameof(SalesInvoiceAggregate.PartialPaymentAmount),
            header.PartialPaymentAmount is > 0 ? new Money(header.PartialPaymentAmount.Value) : null);
        DomainHydrator.Set(aggregate, nameof(SalesInvoiceAggregate.CashboxId), header.CashboxId);
        DomainHydrator.Set(aggregate, nameof(SalesInvoiceAggregate.Status), (SalesInvoiceStatus)header.Status);
        DomainHydrator.Set(aggregate, nameof(SalesInvoiceAggregate.SubTotal), new Money(header.SubTotal));
        DomainHydrator.Set(aggregate, nameof(SalesInvoiceAggregate.DiscountTotal), new Money(header.DiscountTotal));
        DomainHydrator.Set(aggregate, nameof(SalesInvoiceAggregate.TaxTotal), new Money(header.TaxTotal));
        DomainHydrator.Set(aggregate, nameof(SalesInvoiceAggregate.GrandTotal), new Money(header.GrandTotal));
        DomainHydrator.Set(aggregate, nameof(SalesInvoiceAggregate.RoundingDifference), header.RoundingDifference);
        DomainHydrator.Set(aggregate, nameof(SalesInvoiceAggregate.IsLegacyUntaxed), header.IsLegacyUntaxed);
        DomainHydrator.Set(aggregate, nameof(SalesInvoiceAggregate.CreatedByUserId), header.CreatedByUserId);
        DomainHydrator.Set(aggregate, nameof(SalesInvoiceAggregate.ApprovedByUserId), header.ApprovedByUserId);
        DomainHydrator.Set(aggregate, nameof(SalesInvoiceAggregate.SentToWarehouseAt), header.SentToWarehouseAt);
        DomainHydrator.Set(aggregate, nameof(SalesInvoiceAggregate.DetailedAt), header.DetailedAt);
        DomainHydrator.Set(aggregate, nameof(SalesInvoiceAggregate.ApprovedAt), header.ApprovedAt);
        DomainHydrator.Set(aggregate, nameof(SalesInvoiceAggregate.PrintedAt), header.PrintedAt);
        DomainHydrator.Set(aggregate, nameof(SalesInvoiceAggregate.DeliveredAt), header.DeliveredAt);
        DomainHydrator.Set(aggregate, nameof(SalesInvoiceAggregate.DeliveredToName), header.DeliveredToName);
        DomainHydrator.Set(aggregate, nameof(SalesInvoiceAggregate.DeliveryDriverName), header.DeliveryDriverName);
        DomainHydrator.Set(aggregate, nameof(SalesInvoiceAggregate.DeliveryNotes), header.DeliveryNotes);
        DomainHydrator.Set(aggregate, nameof(SalesInvoiceAggregate.CancelledAt), header.CancelledAt);
        DomainHydrator.Set(aggregate, nameof(SalesInvoiceAggregate.CancelReason), header.CancelReason);
        DomainHydrator.Set(aggregate, nameof(SalesInvoiceAggregate.ReversedByJournalEntryId), header.ReversedByJournalEntryId);
        DomainHydrator.Set(aggregate, nameof(SalesInvoiceAggregate.IsArchived), header.IsArchived);

        var domainItems = items.Select(i =>
        {
            var item = DomainHydrator.Create<SalesInvoiceItem>();
            DomainHydrator.Set(item, nameof(SalesInvoiceItem.Id), i.Id);
            DomainHydrator.Set(item, nameof(SalesInvoiceItem.LineNumber), i.LineNumber);
            DomainHydrator.Set(item, nameof(SalesInvoiceItem.ChinaContainerId),
                i.ChinaContainerId == Guid.Empty ? header.ChinaContainerId : i.ChinaContainerId);
            DomainHydrator.Set(item, nameof(SalesInvoiceItem.FabricItemId), i.FabricItemId);
            DomainHydrator.Set(item, nameof(SalesInvoiceItem.FabricColorId), i.FabricColorId);
            DomainHydrator.Set(item, nameof(SalesInvoiceItem.RollCount), i.RollCount);
            DomainHydrator.Set(item, nameof(SalesInvoiceItem.UnitPrice), new Money(i.UnitPrice));
            DomainHydrator.Set(item, nameof(SalesInvoiceItem.OriginalUnitPrice),
                new Money(i.OriginalUnitPrice > 0 ? i.OriginalUnitPrice : i.UnitPrice));
            DomainHydrator.Set(item, nameof(SalesInvoiceItem.LineTotal), new Money(i.LineTotal));
            DomainHydrator.Set(item, nameof(SalesInvoiceItem.DiscountAmount), new Money(i.DiscountAmount));
            DomainHydrator.Set(item, nameof(SalesInvoiceItem.DiscountReason), i.DiscountReason);
            DomainHydrator.Set(item, nameof(SalesInvoiceItem.PriceModifiedByUserId), i.PriceModifiedByUserId);
            DomainHydrator.Set(item, nameof(SalesInvoiceItem.PriceModifiedAt), i.PriceModifiedAt);
            DomainHydrator.Set(item, nameof(SalesInvoiceItem.Notes), i.Notes);
            DomainHydrator.Set(item, nameof(SalesInvoiceItem.TaxCodeId), i.TaxCodeId);
            return item;
        }).ToList();

        var domainTaxSnapshots = itemTaxes.Select(t =>
        {
            var snap = DomainHydrator.Create<SalesInvoiceItemTaxSnapshot>();
            DomainHydrator.Set(snap, nameof(SalesInvoiceItemTaxSnapshot.Id), t.Id);
            DomainHydrator.Set(snap, nameof(SalesInvoiceItemTaxSnapshot.SalesInvoiceItemId), t.SalesInvoiceItemId);
            DomainHydrator.Set(snap, nameof(SalesInvoiceItemTaxSnapshot.TaxCodeId), t.TaxCodeId);
            DomainHydrator.Set(snap, nameof(SalesInvoiceItemTaxSnapshot.TaxCode), t.TaxCode);
            DomainHydrator.Set(snap, nameof(SalesInvoiceItemTaxSnapshot.TaxName), t.TaxName);
            DomainHydrator.Set(snap, nameof(SalesInvoiceItemTaxSnapshot.TaxRate), t.TaxRate);
            DomainHydrator.Set(snap, nameof(SalesInvoiceItemTaxSnapshot.TaxableAmount), new Money(t.TaxableAmount));
            DomainHydrator.Set(snap, nameof(SalesInvoiceItemTaxSnapshot.TaxAmount), new Money(t.TaxAmount));
            DomainHydrator.Set(snap, nameof(SalesInvoiceItemTaxSnapshot.IsInclusive), t.IsInclusive);
            DomainHydrator.Set(snap, nameof(SalesInvoiceItemTaxSnapshot.SalesTaxAccountId), t.SalesTaxAccountId);
            DomainHydrator.Set(snap, nameof(SalesInvoiceItemTaxSnapshot.IsFrozen), t.IsFrozen);
            return snap;
        }).ToList();

        var domainRolls = rolls.Select(r =>
        {
            var roll = DomainHydrator.Create<SalesInvoiceRollDetail>();
            DomainHydrator.Set(roll, nameof(SalesInvoiceRollDetail.Id), r.Id);
            DomainHydrator.Set(roll, nameof(SalesInvoiceRollDetail.SalesInvoiceItemId), r.SalesInvoiceItemId);
            DomainHydrator.Set(roll, nameof(SalesInvoiceRollDetail.RollSequence), new RollNumber(r.RollSequence));
            DomainHydrator.Set(roll, nameof(SalesInvoiceRollDetail.FabricRollId), r.FabricRollId);
            DomainHydrator.Set(roll, nameof(SalesInvoiceRollDetail.LengthMeters), LengthInMeters.FromDecimal(r.LengthMeters));
            DomainHydrator.Set(roll, nameof(SalesInvoiceRollDetail.EnteredByUserId), r.EnteredByUserId);
            DomainHydrator.Set(roll, nameof(SalesInvoiceRollDetail.EnteredAt), r.EnteredAt);
            DomainHydrator.Set(roll, nameof(SalesInvoiceRollDetail.DraftRollNumber), r.DraftRollNumber);
            DomainHydrator.Set(roll, nameof(SalesInvoiceRollDetail.DraftLengthMeters), r.DraftLengthMeters);
            return roll;
        }).ToList();

        AggregateCollectionHelper.SetPrivateList(aggregate, "_items", domainItems);
        AggregateCollectionHelper.SetPrivateList(aggregate, "_rollDetails", domainRolls);
        AggregateCollectionHelper.SetPrivateList(aggregate, "_itemTaxSnapshots", domainTaxSnapshots);

        if (session is not null)
        {
            var detailing = DomainHydrator.Create<WarehouseDetailingSession>();
            DomainHydrator.Set(detailing, nameof(WarehouseDetailingSession.Id), session.Id);
            DomainHydrator.Set(detailing, nameof(WarehouseDetailingSession.SalesInvoiceId), session.SalesInvoiceId);
            DomainHydrator.Set(detailing, nameof(WarehouseDetailingSession.Status), (WarehouseDetailingStatus)session.Status);
            DomainHydrator.Set(detailing, nameof(WarehouseDetailingSession.AssignedOfficerUserId), session.AssignedOfficerUserId);
            DomainHydrator.Set(detailing, nameof(WarehouseDetailingSession.StartedAt), session.StartedAt);
            DomainHydrator.Set(detailing, nameof(WarehouseDetailingSession.CompletedAt), session.CompletedAt);
            DomainHydrator.Set(detailing, nameof(WarehouseDetailingSession.RejectionReason), session.RejectionReason);
            DomainHydrator.Set(aggregate, "_detailingSession", detailing);
        }

        return aggregate;
    }
}

internal static class SalesReturnMapper
{
    public static SalesReturnEntity ToHeaderEntity(SalesReturnAggregate aggregate) => new()
    {
        Id = aggregate.Id,
        CompanyId = aggregate.CompanyId,
        BranchId = aggregate.BranchId,
        ReturnNumber = aggregate.ReturnNumber,
        OriginalInvoiceId = aggregate.OriginalInvoiceId,
        OriginalInvoiceNumber = aggregate.OriginalInvoiceNumber,
        CustomerId = aggregate.CustomerId,
        WarehouseId = aggregate.WarehouseId,
        ReturnDate = aggregate.ReturnDate,
        Reason = (int)aggregate.Reason,
        ReasonNotes = aggregate.ReasonNotes,
        Notes = aggregate.Notes,
        Status = (int)aggregate.Status,
        TotalAmount = aggregate.TotalAmount.Amount,
        TaxTotal = aggregate.TaxTotal.Amount,
        IsLegacyUntaxedReturn = aggregate.IsLegacyUntaxedReturn,
        CreatedByUserId = aggregate.CreatedByUserId,
        PostedByUserId = aggregate.PostedByUserId,
        PostedAt = aggregate.PostedAt,
        JournalEntryNumber = aggregate.JournalEntryNumber
    };

    public static SalesReturnAggregate ToAggregate(SalesReturnEntity header, IReadOnlyList<SalesReturnLineEntity> lines)
    {
        var aggregate = DomainHydrator.Create<SalesReturnAggregate>();
        DomainHydrator.Set(aggregate, nameof(SalesReturnAggregate.Id), header.Id);
        DomainHydrator.Set(aggregate, nameof(SalesReturnAggregate.ReturnNumber), header.ReturnNumber);
        DomainHydrator.Set(aggregate, nameof(SalesReturnAggregate.CompanyId), header.CompanyId);
        DomainHydrator.Set(aggregate, nameof(SalesReturnAggregate.BranchId), header.BranchId);
        DomainHydrator.Set(aggregate, nameof(SalesReturnAggregate.OriginalInvoiceId), header.OriginalInvoiceId);
        DomainHydrator.Set(aggregate, nameof(SalesReturnAggregate.OriginalInvoiceNumber), header.OriginalInvoiceNumber);
        DomainHydrator.Set(aggregate, nameof(SalesReturnAggregate.CustomerId), header.CustomerId);
        DomainHydrator.Set(aggregate, nameof(SalesReturnAggregate.WarehouseId), header.WarehouseId);
        DomainHydrator.Set(aggregate, nameof(SalesReturnAggregate.ReturnDate), header.ReturnDate);
        DomainHydrator.Set(aggregate, nameof(SalesReturnAggregate.Reason), (SalesReturnReason)header.Reason);
        DomainHydrator.Set(aggregate, nameof(SalesReturnAggregate.ReasonNotes), header.ReasonNotes);
        DomainHydrator.Set(aggregate, nameof(SalesReturnAggregate.Notes), header.Notes);
        DomainHydrator.Set(aggregate, nameof(SalesReturnAggregate.Status), (VoucherStatus)header.Status);
        DomainHydrator.Set(aggregate, nameof(SalesReturnAggregate.TotalAmount), new Money(header.TotalAmount));
        DomainHydrator.Set(aggregate, nameof(SalesReturnAggregate.TaxTotal), new Money(header.TaxTotal));
        DomainHydrator.Set(aggregate, nameof(SalesReturnAggregate.IsLegacyUntaxedReturn), header.IsLegacyUntaxedReturn);
        DomainHydrator.Set(aggregate, nameof(SalesReturnAggregate.CreatedByUserId), header.CreatedByUserId);
        DomainHydrator.Set(aggregate, nameof(SalesReturnAggregate.PostedByUserId), header.PostedByUserId);
        DomainHydrator.Set(aggregate, nameof(SalesReturnAggregate.PostedAt), header.PostedAt);
        DomainHydrator.Set(aggregate, nameof(SalesReturnAggregate.JournalEntryNumber), header.JournalEntryNumber);

        var domainLines = lines.OrderBy(l => l.LineNumber).Select(l =>
        {
            var line = DomainHydrator.Create<SalesReturnLine>();
            DomainHydrator.Set(line, nameof(SalesReturnLine.Id), l.Id);
            DomainHydrator.Set(line, nameof(SalesReturnLine.LineNumber), l.LineNumber);
            DomainHydrator.Set(line, nameof(SalesReturnLine.OriginalInvoiceItemId), l.OriginalInvoiceItemId);
            DomainHydrator.Set(line, nameof(SalesReturnLine.FabricItemId), l.FabricItemId);
            DomainHydrator.Set(line, nameof(SalesReturnLine.FabricColorId), l.FabricColorId);
            DomainHydrator.Set(line, nameof(SalesReturnLine.OriginalMeters), l.OriginalMeters);
            DomainHydrator.Set(line, nameof(SalesReturnLine.ReturnMeters), l.ReturnMeters);
            DomainHydrator.Set(line, nameof(SalesReturnLine.UnitPrice), new Money(l.UnitPrice));
            DomainHydrator.Set(line, nameof(SalesReturnLine.LineTotal), new Money(l.LineTotal));
            return line;
        }).ToList();

        AggregateCollectionHelper.SetPrivateList(aggregate, "_lines", domainLines);
        return aggregate;
    }
}

internal static class ContainerMapper
{
    public static ContainerEntity ToHeaderEntity(ContainerAggregate aggregate) => new()
    {
        Id = aggregate.Id,
        CompanyId = aggregate.CompanyId,
        BranchId = aggregate.BranchId,
        SupplierId = aggregate.SupplierId,
        ChinaOrderId = aggregate.ChinaOrderId,
        ContainerNumber = aggregate.ContainerNumber.Value,
        Status = (int)aggregate.Status,
        ShipmentDate = UtcDateTimeNormalizer.ToUtc(aggregate.ShipmentDate),
        ExpectedArrival = aggregate.ExpectedArrival.HasValue
            ? UtcDateTimeNormalizer.ToUtc(aggregate.ExpectedArrival.Value)
            : null,
        ArrivalDate = aggregate.ArrivalDate.HasValue
            ? UtcDateTimeNormalizer.ToUtc(aggregate.ArrivalDate.Value)
            : null,
        TotalRolls = aggregate.TotalRolls,
        TotalMeters = aggregate.TotalMeters.Value,
        TotalWeightKg = aggregate.TotalWeight?.Value,
        Port = aggregate.Port,
        Notes = aggregate.Notes,
        ExchangeRateToLocalCurrency = aggregate.ExchangeRateToLocalCurrency,
        ChinaInvoiceAmountUsd = aggregate.ChinaInvoiceAmountUsd,
        FinancialTaxReservePostedLocal = aggregate.FinancialTaxReservePostedLocal,
        ApprovedAt = aggregate.ApprovedAt,
        ApprovedByUserId = aggregate.ApprovedByUserId,
        IsArchived = aggregate.IsArchived
    };

    public static ContainerAggregate ToAggregate(
        ContainerEntity header,
        IReadOnlyList<ContainerItemEntity> items,
        LandingCostEntity? landingCost,
        IReadOnlyList<ContainerFabricTypeLineEntity> fabricTypeLines)
    {
        var aggregate = DomainHydrator.Create<ContainerAggregate>();
        DomainHydrator.Set(aggregate, nameof(ContainerAggregate.Id), header.Id);
        DomainHydrator.Set(aggregate, nameof(ContainerAggregate.ContainerNumber), new ContainerNumber(header.ContainerNumber));
        DomainHydrator.Set(aggregate, nameof(ContainerAggregate.CompanyId), header.CompanyId);
        DomainHydrator.Set(aggregate, nameof(ContainerAggregate.BranchId), header.BranchId);
        DomainHydrator.Set(aggregate, nameof(ContainerAggregate.SupplierId), header.SupplierId);
        DomainHydrator.Set(aggregate, nameof(ContainerAggregate.ChinaOrderId), header.ChinaOrderId);
        DomainHydrator.Set(aggregate, nameof(ContainerAggregate.Status), (ChinaContainerStatus)header.Status);
        DomainHydrator.Set(aggregate, nameof(ContainerAggregate.ShipmentDate), header.ShipmentDate);
        DomainHydrator.Set(aggregate, nameof(ContainerAggregate.ExpectedArrival), header.ExpectedArrival);
        DomainHydrator.Set(aggregate, nameof(ContainerAggregate.ArrivalDate), header.ArrivalDate);
        DomainHydrator.Set(aggregate, nameof(ContainerAggregate.TotalRolls), header.TotalRolls);
        DomainHydrator.Set(aggregate, nameof(ContainerAggregate.TotalMeters), new LengthInMeters(header.TotalMeters));
        if (header.TotalWeightKg.HasValue)
            DomainHydrator.Set(aggregate, nameof(ContainerAggregate.TotalWeight), new WeightInKg(header.TotalWeightKg.Value));
        DomainHydrator.Set(aggregate, nameof(ContainerAggregate.Port), header.Port);
        DomainHydrator.Set(aggregate, nameof(ContainerAggregate.Notes), header.Notes);
        DomainHydrator.Set(aggregate, nameof(ContainerAggregate.ExchangeRateToLocalCurrency), header.ExchangeRateToLocalCurrency);
        DomainHydrator.Set(aggregate, nameof(ContainerAggregate.ChinaInvoiceAmountUsd), header.ChinaInvoiceAmountUsd);
        DomainHydrator.Set(aggregate, nameof(ContainerAggregate.FinancialTaxReservePostedLocal), header.FinancialTaxReservePostedLocal);
        DomainHydrator.Set(aggregate, nameof(ContainerAggregate.ApprovedAt), header.ApprovedAt);
        DomainHydrator.Set(aggregate, nameof(ContainerAggregate.ApprovedByUserId), header.ApprovedByUserId);
        DomainHydrator.Set(aggregate, nameof(ContainerAggregate.IsArchived), header.IsArchived);

        var domainItems = items.Select(i =>
        {
            var item = DomainHydrator.Create<ChinaContainerItem>();
            DomainHydrator.Set(item, nameof(ChinaContainerItem.Id), i.Id);
            DomainHydrator.Set(item, nameof(ChinaContainerItem.LineNumber), i.LineNumber);
            DomainHydrator.Set(item, nameof(ChinaContainerItem.FabricItemId), i.FabricItemId);
            DomainHydrator.Set(item, nameof(ChinaContainerItem.FabricColorId), i.FabricColorId);
            DomainHydrator.Set(item, nameof(ChinaContainerItem.RollCount), i.RollCount);
            DomainHydrator.Set(item, nameof(ChinaContainerItem.LengthMeters), new LengthInMeters(i.LengthMeters));
            if (i.WeightKg.HasValue)
                DomainHydrator.Set(item, nameof(ChinaContainerItem.WeightKg), new WeightInKg(i.WeightKg.Value));
            DomainHydrator.Set(item, nameof(ChinaContainerItem.LotCode), i.LotCode);
            DomainHydrator.Set(item, nameof(ChinaContainerItem.BuyerCustomerId), i.BuyerCustomerId);
            DomainHydrator.Set(item, nameof(ChinaContainerItem.SupplierRollNumber), i.SupplierRollNumber);
            DomainHydrator.Set(item, nameof(ChinaContainerItem.RowStatus), i.RowStatus);
            return item;
        }).ToList();
        AggregateCollectionHelper.SetPrivateList(aggregate, "_items", domainItems);

        if (landingCost is not null)
        {
            var lc = DomainHydrator.Create<LandingCost>();
            DomainHydrator.Set(lc, nameof(LandingCost.Id), landingCost.Id);
            DomainHydrator.Set(lc, nameof(LandingCost.TotalLengthFromInvoice), new LengthInMeters(landingCost.TotalLengthMeters));
            DomainHydrator.Set(lc, nameof(LandingCost.ContainerWeight), new WeightInKg(landingCost.ContainerWeightKg));
            DomainHydrator.Set(lc, nameof(LandingCost.CustomsAmountPaid), new Money(landingCost.CustomsAmount));
            DomainHydrator.Set(lc, nameof(LandingCost.Shipping), new Money(landingCost.Shipping));
            DomainHydrator.Set(lc, nameof(LandingCost.Insurance), new Money(landingCost.Insurance));
            DomainHydrator.Set(lc, nameof(LandingCost.Clearance), new Money(landingCost.Clearance));
            DomainHydrator.Set(lc, nameof(LandingCost.OtherExpenses), new Money(landingCost.OtherExpenses));
            DomainHydrator.Set(lc, nameof(LandingCost.OtherExpense1), new Money(landingCost.OtherExpense1));
            DomainHydrator.Set(lc, nameof(LandingCost.OtherExpense2), new Money(landingCost.OtherExpense2));
            DomainHydrator.Set(lc, nameof(LandingCost.OtherExpense3), new Money(landingCost.OtherExpense3));
            DomainHydrator.Set(lc, nameof(LandingCost.OtherExpense4), new Money(landingCost.OtherExpense4));
            DomainHydrator.Set(lc, nameof(LandingCost.UsesWeightedAllocation), landingCost.UsesWeightedAllocation);
            DomainHydrator.Set(lc, nameof(LandingCost.Status), (LandingCostStatus)landingCost.Status);
            DomainHydrator.Set(lc, nameof(LandingCost.CalculatedAt), landingCost.CalculatedAt);
            DomainHydrator.Set(lc, nameof(LandingCost.CalculatedByUserId), landingCost.CalculatedByUserId);
            DomainHydrator.Set(aggregate, "LandingCost", lc);
        }

        if (fabricTypeLines.Count > 0)
        {
            var domainTypeLines = fabricTypeLines.Select(t =>
            {
                var line = DomainHydrator.Create<ContainerFabricTypeLine>();
                DomainHydrator.Set(line, nameof(ContainerFabricTypeLine.Id), t.Id);
                DomainHydrator.Set(line, nameof(ContainerFabricTypeLine.LineNumber), t.LineNumber);
                DomainHydrator.Set(line, nameof(ContainerFabricTypeLine.TypeDisplayName), t.TypeDisplayName);
                DomainHydrator.Set(line, nameof(ContainerFabricTypeLine.MatchKey), t.MatchKey);
                DomainHydrator.Set(line, nameof(ContainerFabricTypeLine.FabricItemId), t.FabricItemId);
                DomainHydrator.Set(line, nameof(ContainerFabricTypeLine.FabricColorId), t.FabricColorId);
                DomainHydrator.Set(line, nameof(ContainerFabricTypeLine.LengthMeters), t.LengthMeters);
                DomainHydrator.Set(line, nameof(ContainerFabricTypeLine.RollCount), t.RollCount);
                DomainHydrator.Set(line, nameof(ContainerFabricTypeLine.NetWeightKg), t.NetWeightKg);
                DomainHydrator.Set(line, nameof(ContainerFabricTypeLine.Cbm), t.Cbm);
                DomainHydrator.Set(line, nameof(ContainerFabricTypeLine.ChinaUnitPriceUsd), t.ChinaUnitPriceUsd);
                DomainHydrator.Set(line, nameof(ContainerFabricTypeLine.InvoiceLineAmountUsd), t.InvoiceLineAmountUsd);
                DomainHydrator.Set(line, nameof(ContainerFabricTypeLine.ExpenseShareUsd), t.ExpenseShareUsd);
                DomainHydrator.Set(line, nameof(ContainerFabricTypeLine.LandedCostPerMeterUsd), t.LandedCostPerMeterUsd);
                DomainHydrator.Set(line, nameof(ContainerFabricTypeLine.MarginPerMeterUsd), t.MarginPerMeterUsd);
                DomainHydrator.Set(line, nameof(ContainerFabricTypeLine.SalePricePerMeterUsd), t.SalePricePerMeterUsd);
                DomainHydrator.Set(line, nameof(ContainerFabricTypeLine.HasInvoiceMatch), t.HasInvoiceMatch);
                DomainHydrator.Set(line, nameof(ContainerFabricTypeLine.HasPlMatch), t.HasPlMatch);
                DomainHydrator.Set(line, nameof(ContainerFabricTypeLine.HasDplMatch), t.HasDplMatch);
                DomainHydrator.Set(line, nameof(ContainerFabricTypeLine.MatchWarnings), t.MatchWarnings);
                DomainHydrator.Set(line, nameof(ContainerFabricTypeLine.UsesWeightedAllocation), t.UsesWeightedAllocation);
                return line;
            }).ToList();
            AggregateCollectionHelper.SetPrivateList(aggregate, "_fabricTypeLines", domainTypeLines);
        }

        return aggregate;
    }
}

internal static class WarehouseMapper
{
    public static WarehouseAggregate ToAggregate(
        WarehouseEntity warehouse,
        IReadOnlyList<WarehouseLocationEntity> locations,
        IReadOnlyList<WarehouseStockEntity> stocks)
    {
        var domainWarehouse = DomainHydrator.Create<Warehouse>();
        DomainHydrator.Set(domainWarehouse, nameof(Warehouse.Id), warehouse.Id);
        DomainHydrator.Set(domainWarehouse, nameof(Warehouse.BranchId), warehouse.BranchId);
        DomainHydrator.Set(domainWarehouse, nameof(Warehouse.Code), warehouse.Code);
        DomainHydrator.Set(domainWarehouse, nameof(Warehouse.NameAr), warehouse.NameAr);
        DomainHydrator.Set(domainWarehouse, nameof(Warehouse.City), warehouse.City);
        DomainHydrator.Set(domainWarehouse, nameof(Warehouse.IsActive), warehouse.IsActive);
        DomainHydrator.Set(domainWarehouse, nameof(Warehouse.CapacityRolls), warehouse.CapacityRolls);

        var aggregate = WarehouseAggregate.Create(domainWarehouse);

        foreach (var loc in locations)
        {
            var location = WarehouseLocation.Create(
                loc.WarehouseId,
                (StorageLocationType)loc.LocationType,
                string.IsNullOrWhiteSpace(loc.Code) ? loc.BinCode : loc.Code,
                string.IsNullOrWhiteSpace(loc.Name) ? loc.Zone : loc.Name,
                loc.ParentId,
                loc.Zone,
                loc.BinCode,
                loc.CapacityMeters,
                loc.Priority);
            DomainHydrator.Set(location, nameof(WarehouseLocation.Id), loc.Id);
            aggregate.AddLocation(location);
        }

        foreach (var stock in stocks)
        {
            var balance = WarehouseStockBalance.Create(
                stock.WarehouseId,
                stock.FabricItemId,
                stock.FabricColorId,
                stock.ContainerId,
                stock.RollCount,
                new LengthInMeters(stock.TotalMeters));
            DomainHydrator.Set(balance, nameof(WarehouseStockBalance.Id), stock.Id);
            DomainHydrator.Set(balance, nameof(WarehouseStockBalance.ReservedMeters), LengthInMeters.FromDecimal(stock.ReservedMeters));
            DomainHydrator.Set(balance, nameof(WarehouseStockBalance.AvailableMeters), LengthInMeters.FromDecimal(stock.AvailableMeters));
            aggregate.AddOrUpdateBalance(balance);
        }

        return aggregate;
    }
}

internal static class AccountingMapper
{
    public static AccountingAggregate ToAggregate(JournalEntryEntity header, IReadOnlyList<JournalEntryLineEntity> lines)
    {
        var aggregate = DomainHydrator.Create<AccountingAggregate>();
        DomainHydrator.Set(aggregate, nameof(AccountingAggregate.Id), header.Id);
        DomainHydrator.Set(aggregate, nameof(AccountingAggregate.EntryNumber), header.EntryNumber);
        DomainHydrator.Set(aggregate, nameof(AccountingAggregate.EntryDate), header.EntryDate);
        DomainHydrator.Set(aggregate, nameof(AccountingAggregate.Description), header.Description);
        DomainHydrator.Set(aggregate, nameof(AccountingAggregate.Status), (JournalEntryStatus)header.Status);
        DomainHydrator.Set(aggregate, nameof(AccountingAggregate.SourceType), header.SourceType.HasValue ? (DocumentType?)header.SourceType.Value : null);
        DomainHydrator.Set(aggregate, nameof(AccountingAggregate.SourceId), header.SourceId);
        DomainHydrator.Set(aggregate, nameof(AccountingAggregate.CreatedByUserId), header.CreatedByUserId);
        DomainHydrator.Set(aggregate, nameof(AccountingAggregate.PostedAt), header.PostedAt);
        DomainHydrator.Set(aggregate, nameof(AccountingAggregate.PostedByUserId), header.PostedByUserId);
        DomainHydrator.Set(aggregate, nameof(AccountingAggregate.ReversalOfEntryId), header.ReversalOfEntryId);
        DomainHydrator.Set(aggregate, nameof(AccountingAggregate.CancelledAt), header.CancelledAt);
        DomainHydrator.Set(aggregate, nameof(AccountingAggregate.JournalBookId), header.JournalBookId);

        var domainLines = lines.Select(l => JournalEntryLine.Create(
            l.AccountId,
            new Money(l.Debit),
            new Money(l.Credit),
            l.Narrative,
            l.PartyId)).ToList();

        for (var i = 0; i < lines.Count; i++)
            DomainHydrator.Set(domainLines[i], nameof(JournalEntryLine.Id), lines[i].Id);

        AggregateCollectionHelper.SetPrivateList(aggregate, "_lines", domainLines);
        return aggregate;
    }
}

internal static class FinanceMapper
{
    public static ReceiptVoucher ToDomain(ReceiptVoucherEntity entity)
    {
        var voucher = DomainHydrator.Create<ReceiptVoucher>();
        DomainHydrator.Set(voucher, nameof(ReceiptVoucher.Id), entity.Id);
        DomainHydrator.Set(voucher, nameof(ReceiptVoucher.VoucherNumber), entity.VoucherNumber);
        DomainHydrator.Set(voucher, nameof(ReceiptVoucher.CustomerId), entity.CustomerId);
        DomainHydrator.Set(voucher, nameof(ReceiptVoucher.CashboxId), entity.CashboxId);
        DomainHydrator.Set(voucher, nameof(ReceiptVoucher.Amount), new Money(entity.Amount));
        DomainHydrator.Set(voucher, nameof(ReceiptVoucher.VoucherDate), entity.VoucherDate);
        DomainHydrator.Set(voucher, nameof(ReceiptVoucher.Status), (VoucherStatus)entity.Status);
        DomainHydrator.Set(voucher, nameof(ReceiptVoucher.PostedAt), entity.PostedAt);
        DomainHydrator.Set(voucher, nameof(ReceiptVoucher.CancelledAt), entity.CancelledAt);
        DomainHydrator.Set(voucher, nameof(ReceiptVoucher.CancelReason), entity.CancelReason);
        return voucher;
    }

    public static PaymentVoucher ToDomain(PaymentVoucherEntity entity)
    {
        var voucher = DomainHydrator.Create<PaymentVoucher>();
        DomainHydrator.Set(voucher, nameof(PaymentVoucher.Id), entity.Id);
        DomainHydrator.Set(voucher, nameof(PaymentVoucher.VoucherNumber), entity.VoucherNumber);
        DomainHydrator.Set(voucher, nameof(PaymentVoucher.SupplierId), entity.SupplierId);
        DomainHydrator.Set(voucher, nameof(PaymentVoucher.CashboxId), entity.CashboxId);
        DomainHydrator.Set(voucher, nameof(PaymentVoucher.Amount), new Money(entity.Amount));
        DomainHydrator.Set(voucher, nameof(PaymentVoucher.VoucherDate), entity.VoucherDate);
        DomainHydrator.Set(voucher, nameof(PaymentVoucher.Status), (VoucherStatus)entity.Status);
        return voucher;
    }

    public static Cashbox ToDomain(CashboxEntity entity)
    {
        var cashbox = DomainHydrator.Create<Cashbox>();
        DomainHydrator.Set(cashbox, nameof(Cashbox.Id), entity.Id);
        DomainHydrator.Set(cashbox, nameof(Cashbox.BranchId), entity.BranchId);
        DomainHydrator.Set(cashbox, nameof(Cashbox.Code), entity.Code);
        DomainHydrator.Set(cashbox, nameof(Cashbox.Name), entity.Name);
        DomainHydrator.Set(cashbox, nameof(Cashbox.Balance), new Money(entity.Balance, entity.Currency));
        DomainHydrator.Set(cashbox, nameof(Cashbox.Currency), entity.Currency);
        DomainHydrator.Set(cashbox, nameof(Cashbox.IsActive), entity.IsActive);
        DomainHydrator.Set(cashbox, nameof(Cashbox.AccountId), entity.AccountId);
        return cashbox;
    }

    public static CashboxTransfer ToDomain(CashboxTransferEntity entity)
    {
        var transfer = DomainHydrator.Create<CashboxTransfer>();
        DomainHydrator.Set(transfer, nameof(CashboxTransfer.Id), entity.Id);
        DomainHydrator.Set(transfer, nameof(CashboxTransfer.Number), entity.TransferNumber);
        DomainHydrator.Set(transfer, nameof(CashboxTransfer.FromCashboxId), entity.FromCashboxId);
        DomainHydrator.Set(transfer, nameof(CashboxTransfer.ToCashboxId), entity.ToCashboxId);
        DomainHydrator.Set(transfer, nameof(CashboxTransfer.Amount), new Money(entity.Amount, entity.Currency));
        DomainHydrator.Set(transfer, nameof(CashboxTransfer.Status), (VoucherStatus)entity.Status);
        DomainHydrator.Set(transfer, nameof(CashboxTransfer.TransferDate), entity.TransferDate);
        DomainHydrator.Set(transfer, nameof(CashboxTransfer.Notes), entity.Notes);
        return transfer;
    }

    public static CashboxTransferEntity ToEntity(CashboxTransfer transfer, Guid companyId, Guid branchId) => new()
    {
        Id = transfer.Id,
        CompanyId = companyId,
        BranchId = branchId,
        TransferNumber = transfer.Number,
        FromCashboxId = transfer.FromCashboxId,
        ToCashboxId = transfer.ToCashboxId,
        Amount = transfer.Amount.Amount,
        Currency = transfer.Amount.Currency,
        TransferDate = transfer.TransferDate,
        Status = (int)transfer.Status,
        Notes = transfer.Notes,
        PostedAt = transfer.Status == VoucherStatus.Posted ? DateTime.UtcNow : null
    };
}

internal static class PurchaseMapper
{
    public static PurchaseInvoice ToDomain(PurchaseInvoiceEntity header, IReadOnlyList<PurchaseInvoiceItemEntity> items)
    {
        var invoice = DomainHydrator.Create<PurchaseInvoice>();
        DomainHydrator.Set(invoice, nameof(PurchaseInvoice.Id), header.Id);
        DomainHydrator.Set(invoice, nameof(PurchaseInvoice.CompanyId), header.CompanyId);
        DomainHydrator.Set(invoice, nameof(PurchaseInvoice.BranchId), header.BranchId);
        DomainHydrator.Set(invoice, nameof(PurchaseInvoice.InvoiceNumber), header.InvoiceNumber);
        DomainHydrator.Set(invoice, nameof(PurchaseInvoice.SupplierId), header.SupplierId);
        DomainHydrator.Set(invoice, nameof(PurchaseInvoice.SupplierReference), header.SupplierReference);
        DomainHydrator.Set(invoice, nameof(PurchaseInvoice.InvoiceDate), header.InvoiceDate);
        DomainHydrator.Set(invoice, nameof(PurchaseInvoice.DueDate), header.DueDate);
        DomainHydrator.Set(invoice, nameof(PurchaseInvoice.WarehouseId), header.WarehouseId);
        DomainHydrator.Set(invoice, nameof(PurchaseInvoice.CurrencyCode), header.CurrencyCode);
        DomainHydrator.Set(invoice, nameof(PurchaseInvoice.SubTotal), new Money(header.SubTotal, header.CurrencyCode));
        DomainHydrator.Set(invoice, nameof(PurchaseInvoice.DiscountAmount), new Money(header.DiscountAmount, header.CurrencyCode));
        DomainHydrator.Set(invoice, nameof(PurchaseInvoice.TaxAmount), new Money(header.TaxAmount, header.CurrencyCode));
        DomainHydrator.Set(invoice, nameof(PurchaseInvoice.TotalAmount), new Money(header.TotalAmount, header.CurrencyCode));
        DomainHydrator.Set(invoice, nameof(PurchaseInvoice.PaidAmount), new Money(header.PaidAmount, header.CurrencyCode));
        DomainHydrator.Set(invoice, nameof(PurchaseInvoice.Remaining), new Money(header.Remaining, header.CurrencyCode));
        DomainHydrator.Set(invoice, nameof(PurchaseInvoice.Status), (PurchaseInvoiceStatus)header.Status);
        DomainHydrator.Set(invoice, nameof(PurchaseInvoice.PurchaseOrderId), header.PurchaseOrderId);
        DomainHydrator.Set(invoice, nameof(PurchaseInvoice.Notes), header.Notes);
        DomainHydrator.Set(invoice, nameof(PurchaseInvoice.PostedAt), header.PostedAt);
        DomainHydrator.Set(invoice, nameof(PurchaseInvoice.PostedByUserId), header.PostedByUserId);

        var domainItems = new List<PurchaseInvoiceItem>();
        foreach (var item in items)
        {
            PurchaseInvoiceItem domainItem = (PurchaseLineType)item.LineType == PurchaseLineType.Expense
                ? PurchaseInvoiceItem.CreateExpenseLine(
                    item.ExpenseAccountId ?? AccountingAccountIds.OperatingExpenses,
                    new Money(item.LineTotal, header.CurrencyCode),
                    item.Description)
                : PurchaseInvoiceItem.CreateInventoryLine(
                    item.FabricItemId ?? Guid.Empty,
                    item.FabricColorId,
                    new LengthInMeters(item.QuantityMeters),
                    item.RollCount,
                    new Money(item.UnitPrice, header.CurrencyCode),
                    item.Description);
            DomainHydrator.Set(domainItem, nameof(PurchaseInvoiceItem.Id), item.Id);
            domainItems.Add(domainItem);
        }
        invoice.HydrateItems(domainItems);

        return invoice;
    }

    public static PurchaseInvoiceEntity ToEntity(PurchaseInvoice invoice) => new()
    {
        Id = invoice.Id,
        CompanyId = invoice.CompanyId,
        BranchId = invoice.BranchId,
        InvoiceNumber = invoice.InvoiceNumber,
        SupplierId = invoice.SupplierId,
        SupplierReference = invoice.SupplierReference,
        InvoiceDate = invoice.InvoiceDate,
        DueDate = invoice.DueDate,
        WarehouseId = invoice.WarehouseId,
        CurrencyCode = invoice.CurrencyCode,
        SubTotal = invoice.SubTotal.Amount,
        DiscountAmount = invoice.DiscountAmount.Amount,
        TaxAmount = invoice.TaxAmount.Amount,
        TotalAmount = invoice.TotalAmount.Amount,
        PaidAmount = invoice.PaidAmount.Amount,
        Remaining = invoice.Remaining.Amount,
        Status = (int)invoice.Status,
        PurchaseOrderId = invoice.PurchaseOrderId,
        Notes = invoice.Notes,
        PostedAt = invoice.PostedAt,
        PostedByUserId = invoice.PostedByUserId,
        CreatedAt = DateTime.UtcNow,
        IsActive = true
    };

    public static void UpdateEntity(PurchaseInvoiceEntity entity, PurchaseInvoice invoice)
    {
        entity.SupplierId = invoice.SupplierId;
        entity.SupplierReference = invoice.SupplierReference;
        entity.InvoiceDate = invoice.InvoiceDate;
        entity.DueDate = invoice.DueDate;
        entity.WarehouseId = invoice.WarehouseId;
        entity.CurrencyCode = invoice.CurrencyCode;
        entity.SubTotal = invoice.SubTotal.Amount;
        entity.DiscountAmount = invoice.DiscountAmount.Amount;
        entity.TaxAmount = invoice.TaxAmount.Amount;
        entity.TotalAmount = invoice.TotalAmount.Amount;
        entity.PaidAmount = invoice.PaidAmount.Amount;
        entity.Remaining = invoice.Remaining.Amount;
        entity.Status = (int)invoice.Status;
        entity.Notes = invoice.Notes;
        entity.PostedAt = invoice.PostedAt;
        entity.PostedByUserId = invoice.PostedByUserId;
        entity.UpdatedAt = DateTime.UtcNow;
    }

    public static PurchaseInvoiceItemEntity ToItemEntity(Guid invoiceId, PurchaseInvoiceItem item) => new()
    {
        Id = item.Id,
        PurchaseInvoiceId = invoiceId,
        LineType = (int)item.LineType,
        FabricItemId = item.FabricItemId,
        FabricColorId = item.FabricColorId,
        ExpenseAccountId = item.ExpenseAccountId,
        Description = item.Description,
        QuantityMeters = item.Quantity.Value,
        RollCount = item.RollCount,
        UnitPrice = item.UnitPrice.Amount,
        LineTotal = item.LineTotal.Amount,
        CreatedAt = DateTime.UtcNow,
        IsActive = true
    };

    public static PurchaseOrder ToOrderDomain(PurchaseOrderEntity header, IReadOnlyList<PurchaseOrderLineEntity> lines)
    {
        var order = DomainHydrator.Create<PurchaseOrder>();
        DomainHydrator.Set(order, nameof(PurchaseOrder.Id), header.Id);
        DomainHydrator.Set(order, nameof(PurchaseOrder.CompanyId), header.CompanyId);
        DomainHydrator.Set(order, nameof(PurchaseOrder.BranchId), header.BranchId);
        DomainHydrator.Set(order, nameof(PurchaseOrder.OrderNumber), header.OrderNumber);
        DomainHydrator.Set(order, nameof(PurchaseOrder.SupplierId), header.SupplierId);
        DomainHydrator.Set(order, nameof(PurchaseOrder.OrderDate), header.OrderDate);
        DomainHydrator.Set(order, nameof(PurchaseOrder.ExpectedDeliveryDate), header.ExpectedDeliveryDate);
        DomainHydrator.Set(order, nameof(PurchaseOrder.Status), (PurchaseOrderStatus)header.Status);
        DomainHydrator.Set(order, nameof(PurchaseOrder.TotalAmount), new Money(header.TotalAmount));
        DomainHydrator.Set(order, nameof(PurchaseOrder.Notes), header.Notes);
        var domainLines = new List<PurchaseOrderLine>();
        foreach (var line in lines)
        {
            var domainLine = PurchaseOrderLine.Create(line.FabricItemId, line.Description, line.Quantity, new Money(line.UnitCost));
            DomainHydrator.Set(domainLine, nameof(PurchaseOrderLine.Id), line.Id);
            domainLines.Add(domainLine);
        }
        order.HydrateLines(domainLines);
        return order;
    }

    public static PurchaseOrderEntity ToOrderEntity(PurchaseOrder order) => new()
    {
        Id = order.Id,
        CompanyId = order.CompanyId,
        BranchId = order.BranchId,
        OrderNumber = order.OrderNumber,
        SupplierId = order.SupplierId,
        OrderDate = order.OrderDate,
        ExpectedDeliveryDate = order.ExpectedDeliveryDate,
        TotalAmount = order.TotalAmount.Amount,
        Status = (int)order.Status,
        Notes = order.Notes,
        CreatedAt = DateTime.UtcNow,
        IsActive = true
    };

    public static void UpdateOrderEntity(PurchaseOrderEntity entity, PurchaseOrder order)
    {
        entity.SupplierId = order.SupplierId;
        entity.ExpectedDeliveryDate = order.ExpectedDeliveryDate;
        entity.TotalAmount = order.TotalAmount.Amount;
        entity.Status = (int)order.Status;
        entity.Notes = order.Notes;
        entity.UpdatedAt = DateTime.UtcNow;
    }

    public static PurchaseOrderLineEntity ToOrderLineEntity(Guid orderId, PurchaseOrderLine line) => new()
    {
        Id = line.Id,
        PurchaseOrderId = orderId,
        FabricItemId = line.FabricItemId,
        Description = line.Description,
        Quantity = line.Quantity,
        UnitCost = line.UnitCost.Amount,
        LineTotal = line.LineTotal.Amount,
        CreatedAt = DateTime.UtcNow,
        IsActive = true
    };

    public static PurchaseReturn ToReturnDomain(PurchaseReturnEntity header, IReadOnlyList<PurchaseReturnLineEntity> lines)
    {
        var ret = DomainHydrator.Create<PurchaseReturn>();
        DomainHydrator.Set(ret, nameof(PurchaseReturn.Id), header.Id);
        DomainHydrator.Set(ret, nameof(PurchaseReturn.CompanyId), header.CompanyId);
        DomainHydrator.Set(ret, nameof(PurchaseReturn.BranchId), header.BranchId);
        DomainHydrator.Set(ret, nameof(PurchaseReturn.ReturnNumber), header.ReturnNumber);
        DomainHydrator.Set(ret, nameof(PurchaseReturn.OriginalInvoiceId), header.OriginalInvoiceId);
        DomainHydrator.Set(ret, nameof(PurchaseReturn.ReturnDate), header.ReturnDate);
        DomainHydrator.Set(ret, nameof(PurchaseReturn.TotalAmount), new Money(header.TotalAmount));
        DomainHydrator.Set(ret, nameof(PurchaseReturn.Status), (PurchaseReturnStatus)header.Status);
        DomainHydrator.Set(ret, nameof(PurchaseReturn.Notes), header.Notes);
        DomainHydrator.Set(ret, nameof(PurchaseReturn.PostedAt), header.PostedAt);
        var domainLines = new List<PurchaseReturnLine>();
        foreach (var line in lines)
        {
            var domainLine = PurchaseReturnLine.Create(
                line.OriginalInvoiceItemId,
                (PurchaseLineType)line.LineType,
                line.FabricItemId,
                line.FabricColorId,
                line.QuantityMeters,
                new Money(line.UnitPrice));
            DomainHydrator.Set(domainLine, nameof(PurchaseReturnLine.Id), line.Id);
            domainLines.Add(domainLine);
        }
        ret.HydrateLines(domainLines);
        return ret;
    }

    public static PurchaseReturnEntity ToReturnEntity(PurchaseReturn purchaseReturn) => new()
    {
        Id = purchaseReturn.Id,
        CompanyId = purchaseReturn.CompanyId,
        BranchId = purchaseReturn.BranchId,
        ReturnNumber = purchaseReturn.ReturnNumber,
        OriginalInvoiceId = purchaseReturn.OriginalInvoiceId,
        ReturnDate = purchaseReturn.ReturnDate,
        TotalAmount = purchaseReturn.TotalAmount.Amount,
        Status = (int)purchaseReturn.Status,
        Notes = purchaseReturn.Notes,
        PostedAt = purchaseReturn.PostedAt,
        CreatedAt = DateTime.UtcNow,
        IsActive = true
    };

    public static void UpdateReturnEntity(PurchaseReturnEntity entity, PurchaseReturn purchaseReturn)
    {
        entity.TotalAmount = purchaseReturn.TotalAmount.Amount;
        entity.Status = (int)purchaseReturn.Status;
        entity.Notes = purchaseReturn.Notes;
        entity.PostedAt = purchaseReturn.PostedAt;
        entity.UpdatedAt = DateTime.UtcNow;
    }

    public static PurchaseReturnLineEntity ToReturnLineEntity(Guid returnId, PurchaseReturnLine line) => new()
    {
        Id = line.Id,
        PurchaseReturnId = returnId,
        OriginalInvoiceItemId = line.OriginalInvoiceItemId,
        LineType = (int)line.LineType,
        FabricItemId = line.FabricItemId,
        FabricColorId = line.FabricColorId,
        QuantityMeters = line.QuantityMeters,
        UnitPrice = line.UnitPrice.Amount,
        LineTotal = line.LineTotal.Amount,
        CreatedAt = DateTime.UtcNow,
        IsActive = true
    };
}
