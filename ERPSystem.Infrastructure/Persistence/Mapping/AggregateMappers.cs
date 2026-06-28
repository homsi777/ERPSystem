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
        Status = (int)aggregate.Status,
        SubTotal = aggregate.SubTotal.Amount,
        DiscountTotal = aggregate.DiscountTotal.Amount,
        TaxTotal = aggregate.TaxTotal.Amount,
        GrandTotal = aggregate.GrandTotal.Amount,
        CreatedByUserId = aggregate.CreatedByUserId,
        ApprovedByUserId = aggregate.ApprovedByUserId,
        SentToWarehouseAt = aggregate.SentToWarehouseAt,
        DetailedAt = aggregate.DetailedAt,
        ApprovedAt = aggregate.ApprovedAt,
        PrintedAt = aggregate.PrintedAt,
        DeliveredAt = aggregate.DeliveredAt,
        CancelledAt = aggregate.CancelledAt,
        CancelReason = aggregate.CancelReason,
        ReversedByJournalEntryId = aggregate.ReversedByJournalEntryId,
        IsArchived = aggregate.IsArchived
    };

    public static SalesInvoiceAggregate ToAggregate(
        SalesInvoiceEntity header,
        IReadOnlyList<SalesInvoiceItemEntity> items,
        IReadOnlyList<SalesInvoiceRollDetailEntity> rolls,
        WarehouseDetailingSessionEntity? session)
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
        DomainHydrator.Set(aggregate, nameof(SalesInvoiceAggregate.Status), (SalesInvoiceStatus)header.Status);
        DomainHydrator.Set(aggregate, nameof(SalesInvoiceAggregate.SubTotal), new Money(header.SubTotal));
        DomainHydrator.Set(aggregate, nameof(SalesInvoiceAggregate.DiscountTotal), new Money(header.DiscountTotal));
        DomainHydrator.Set(aggregate, nameof(SalesInvoiceAggregate.TaxTotal), new Money(header.TaxTotal));
        DomainHydrator.Set(aggregate, nameof(SalesInvoiceAggregate.GrandTotal), new Money(header.GrandTotal));
        DomainHydrator.Set(aggregate, nameof(SalesInvoiceAggregate.CreatedByUserId), header.CreatedByUserId);
        DomainHydrator.Set(aggregate, nameof(SalesInvoiceAggregate.ApprovedByUserId), header.ApprovedByUserId);
        DomainHydrator.Set(aggregate, nameof(SalesInvoiceAggregate.SentToWarehouseAt), header.SentToWarehouseAt);
        DomainHydrator.Set(aggregate, nameof(SalesInvoiceAggregate.DetailedAt), header.DetailedAt);
        DomainHydrator.Set(aggregate, nameof(SalesInvoiceAggregate.ApprovedAt), header.ApprovedAt);
        DomainHydrator.Set(aggregate, nameof(SalesInvoiceAggregate.PrintedAt), header.PrintedAt);
        DomainHydrator.Set(aggregate, nameof(SalesInvoiceAggregate.DeliveredAt), header.DeliveredAt);
        DomainHydrator.Set(aggregate, nameof(SalesInvoiceAggregate.CancelledAt), header.CancelledAt);
        DomainHydrator.Set(aggregate, nameof(SalesInvoiceAggregate.CancelReason), header.CancelReason);
        DomainHydrator.Set(aggregate, nameof(SalesInvoiceAggregate.ReversedByJournalEntryId), header.ReversedByJournalEntryId);
        DomainHydrator.Set(aggregate, nameof(SalesInvoiceAggregate.IsArchived), header.IsArchived);

        var domainItems = items.Select(i =>
        {
            var item = DomainHydrator.Create<SalesInvoiceItem>();
            DomainHydrator.Set(item, nameof(SalesInvoiceItem.Id), i.Id);
            DomainHydrator.Set(item, nameof(SalesInvoiceItem.LineNumber), i.LineNumber);
            DomainHydrator.Set(item, nameof(SalesInvoiceItem.FabricItemId), i.FabricItemId);
            DomainHydrator.Set(item, nameof(SalesInvoiceItem.FabricColorId), i.FabricColorId);
            DomainHydrator.Set(item, nameof(SalesInvoiceItem.RollCount), i.RollCount);
            DomainHydrator.Set(item, nameof(SalesInvoiceItem.UnitPrice), new Money(i.UnitPrice));
            DomainHydrator.Set(item, nameof(SalesInvoiceItem.LineTotal), new Money(i.LineTotal));
            return item;
        }).ToList();

        var domainRolls = rolls.Select(r =>
        {
            var roll = DomainHydrator.Create<SalesInvoiceRollDetail>();
            DomainHydrator.Set(roll, nameof(SalesInvoiceRollDetail.Id), r.Id);
            DomainHydrator.Set(roll, nameof(SalesInvoiceRollDetail.SalesInvoiceItemId), r.SalesInvoiceItemId);
            DomainHydrator.Set(roll, nameof(SalesInvoiceRollDetail.RollSequence), new RollNumber(r.RollSequence));
            DomainHydrator.Set(roll, nameof(SalesInvoiceRollDetail.FabricRollId), r.FabricRollId);
            DomainHydrator.Set(roll, nameof(SalesInvoiceRollDetail.LengthMeters), new LengthInMeters(r.LengthMeters));
            DomainHydrator.Set(roll, nameof(SalesInvoiceRollDetail.EnteredByUserId), r.EnteredByUserId);
            DomainHydrator.Set(roll, nameof(SalesInvoiceRollDetail.EnteredAt), r.EnteredAt);
            return roll;
        }).ToList();

        AggregateCollectionHelper.SetPrivateList(aggregate, "_items", domainItems);
        AggregateCollectionHelper.SetPrivateList(aggregate, "_rollDetails", domainRolls);

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
        ShipmentDate = aggregate.ShipmentDate,
        ExpectedArrival = aggregate.ExpectedArrival,
        ArrivalDate = aggregate.ArrivalDate,
        TotalRolls = aggregate.TotalRolls,
        TotalMeters = aggregate.TotalMeters.Value,
        TotalWeightKg = aggregate.TotalWeight?.Value,
        Port = aggregate.Port,
        Notes = aggregate.Notes,
        ExchangeRateToLocalCurrency = aggregate.ExchangeRateToLocalCurrency,
        ApprovedAt = aggregate.ApprovedAt,
        ApprovedByUserId = aggregate.ApprovedByUserId,
        IsArchived = aggregate.IsArchived
    };

    public static ContainerAggregate ToAggregate(
        ContainerEntity header,
        IReadOnlyList<ContainerItemEntity> items,
        LandingCostEntity? landingCost)
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
            DomainHydrator.Set(lc, nameof(LandingCost.Clearance), new Money(landingCost.Clearance));
            DomainHydrator.Set(lc, nameof(LandingCost.OtherExpenses), new Money(landingCost.OtherExpenses));
            DomainHydrator.Set(lc, nameof(LandingCost.Status), (LandingCostStatus)landingCost.Status);
            DomainHydrator.Set(lc, nameof(LandingCost.CalculatedAt), landingCost.CalculatedAt);
            DomainHydrator.Set(lc, nameof(LandingCost.CalculatedByUserId), landingCost.CalculatedByUserId);
            DomainHydrator.Set(aggregate, "_landingCost", lc);
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
            var location = WarehouseLocation.Create(loc.WarehouseId, loc.Zone, loc.BinCode);
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
            DomainHydrator.Set(balance, nameof(WarehouseStockBalance.ReservedMeters), new LengthInMeters(stock.ReservedMeters));
            DomainHydrator.Set(balance, nameof(WarehouseStockBalance.AvailableMeters), new LengthInMeters(stock.AvailableMeters));
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
        return cashbox;
    }
}

internal static class PurchaseMapper
{
    public static PurchaseInvoice ToDomain(PurchaseInvoiceEntity header, IReadOnlyList<PurchaseInvoiceItemEntity> items)
    {
        var invoice = DomainHydrator.Create<PurchaseInvoice>();
        DomainHydrator.Set(invoice, nameof(PurchaseInvoice.Id), header.Id);
        DomainHydrator.Set(invoice, nameof(PurchaseInvoice.InvoiceNumber), header.InvoiceNumber);
        DomainHydrator.Set(invoice, nameof(PurchaseInvoice.SupplierId), header.SupplierId);
        DomainHydrator.Set(invoice, nameof(PurchaseInvoice.InvoiceDate), header.InvoiceDate);
        DomainHydrator.Set(invoice, nameof(PurchaseInvoice.TotalAmount), new Money(header.TotalAmount));
        DomainHydrator.Set(invoice, nameof(PurchaseInvoice.Remaining), new Money(header.Remaining));
        DomainHydrator.Set(invoice, nameof(PurchaseInvoice.Status), (PurchaseInvoiceStatus)header.Status);

        foreach (var item in items)
        {
            var domainItem = PurchaseInvoiceItem.Create(
                item.FabricItemId,
                new LengthInMeters(item.QuantityMeters),
                new Money(item.UnitPrice));
            DomainHydrator.Set(domainItem, nameof(PurchaseInvoiceItem.Id), item.Id);
            invoice.AddItem(domainItem);
        }

        return invoice;
    }
}
