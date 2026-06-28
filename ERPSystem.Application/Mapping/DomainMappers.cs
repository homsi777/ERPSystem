using ERPSystem.Application.DTOs.Containers;
using ERPSystem.Application.DTOs.Customers;
using ERPSystem.Application.DTOs.Finance;
using ERPSystem.Application.DTOs.Sales;
using ERPSystem.Application.DTOs.Warehouses;
using ERPSystem.Domain.Aggregates;
using ERPSystem.Domain.Entities.ChinaImport;
using ERPSystem.Domain.Entities.Finance;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.Mapping;

public static class CustomerMapper
{
    public static CustomerListDto ToListDto(CustomerAggregate aggregate) => new()
    {
        Id = aggregate.Customer.Id,
        Code = aggregate.Customer.Code,
        NameAr = aggregate.Customer.NameAr,
        NameEn = aggregate.Customer.NameEn,
        Type = aggregate.Customer.Type,
        Status = aggregate.Customer.Status,
        Balance = aggregate.Customer.Balance.Amount,
        CreditLimit = aggregate.Customer.CreditLimit.Amount,
        IsActive = aggregate.Customer.IsActive
    };

    public static CustomerDetailsDto ToDetailsDto(CustomerAggregate aggregate) => new()
    {
        Id = aggregate.Customer.Id,
        Code = aggregate.Customer.Code,
        NameAr = aggregate.Customer.NameAr,
        NameEn = aggregate.Customer.NameEn,
        Type = aggregate.Customer.Type,
        Status = aggregate.Customer.Status,
        Balance = aggregate.Customer.Balance.Amount,
        CreditLimit = aggregate.Customer.CreditLimit.Amount,
        PaymentTermsDays = aggregate.Customer.PaymentTermsDays,
        Phone = aggregate.Customer.Phone?.Value,
        Email = aggregate.Customer.Email?.Value,
        IsActive = aggregate.Customer.IsActive
    };
}

public static class ContainerMapper
{
    public static ContainerListDto ToListDto(ContainerAggregate aggregate, string supplierName = "") => new()
    {
        Id = aggregate.Id,
        ContainerNumber = aggregate.ContainerNumber.Value,
        Status = aggregate.Status,
        ShipmentDate = aggregate.ShipmentDate,
        ExpectedArrival = aggregate.ExpectedArrival,
        TotalRolls = aggregate.TotalRolls,
        TotalMeters = aggregate.TotalMeters.Value,
        TotalWeightKg = aggregate.TotalWeight?.Value,
        CodeCount = aggregate.Items.Select(i => i.FabricItemId).Distinct().Count(),
        ColorCount = aggregate.Items.Select(i => i.FabricColorId).Distinct().Count(),
        ExchangeRateToLocalCurrency = aggregate.ExchangeRateToLocalCurrency,
        SupplierName = supplierName
    };

    public static ContainerDetailsDto ToDetailsDto(ContainerAggregate aggregate) => new()
    {
        Id = aggregate.Id,
        ContainerNumber = aggregate.ContainerNumber.Value,
        Status = aggregate.Status,
        SupplierId = aggregate.SupplierId,
        ShipmentDate = aggregate.ShipmentDate,
        ArrivalDate = aggregate.ArrivalDate,
        TotalRolls = aggregate.TotalRolls,
        TotalMeters = aggregate.TotalMeters.Value,
        TotalWeightKg = aggregate.TotalWeight?.Value,
        LandingCost = aggregate.LandingCost is null ? null : ToLandingCostDto(aggregate.LandingCost),
        Items = aggregate.Items.Select(i => new ContainerItemDto
        {
            LineNumber = i.LineNumber,
            FabricItemId = i.FabricItemId,
            FabricColorId = i.FabricColorId,
            RollCount = i.RollCount,
            LengthMeters = i.LengthMeters.Value,
            IsValid = i.IsValid
        }).ToList()
    };

    public static LandingCostDto ToLandingCostDto(LandingCost landingCost) => new()
    {
        TotalLengthMeters = landingCost.TotalLengthFromInvoice.Value,
        ContainerWeightKg = landingCost.ContainerWeight.Value,
        CustomsAmount = landingCost.CustomsAmountPaid.Amount,
        Shipping = landingCost.Shipping.Amount,
        Clearance = landingCost.Clearance.Amount,
        OtherExpenses = landingCost.OtherExpenses.Amount,
        TotalImportExpenses = landingCost.TotalImportExpenses.Amount,
        CustomsCostPerMeter = landingCost.CustomsCostPerMeter,
        ExpenseCostPerMeter = landingCost.ExpenseCostPerMeter,
        AvgGramPerMeter = landingCost.AvgGramPerMeter,
        Status = landingCost.Status
    };

    public static ContainerOperationsCenterDto ToOperationsCenterDto(ContainerAggregate aggregate) => new()
    {
        Container = ToDetailsDto(aggregate),
        CanApprove = aggregate.LandingCost?.Status == LandingCostStatus.Reviewed,
        CanMoveToWarehouse = aggregate.Status == ChinaContainerStatus.Approved,
        CanCalculateLandingCost = aggregate.TotalMeters.Value > 0 && aggregate.LandingCost is null
    };
}

public static class SalesInvoiceMapper
{
    public static SalesInvoiceDto ToDto(SalesInvoiceAggregate aggregate, string customerName = "") => new()
    {
        Id = aggregate.Id,
        InvoiceNumber = aggregate.InvoiceNumber.Value,
        Status = aggregate.Status,
        CustomerId = aggregate.CustomerId,
        CustomerName = customerName,
        WarehouseId = aggregate.WarehouseId,
        ChinaContainerId = aggregate.ChinaContainerId,
        InvoiceDate = aggregate.InvoiceDate,
        PaymentType = aggregate.PaymentType,
        SubTotal = aggregate.SubTotal.Amount,
        DiscountTotal = aggregate.DiscountTotal.Amount,
        TaxTotal = aggregate.TaxTotal.Amount,
        GrandTotal = aggregate.GrandTotal.Amount,
        Lines = aggregate.Items.Select(i => new SalesInvoiceLineDto
        {
            Id = i.Id,
            LineNumber = i.LineNumber,
            FabricItemId = i.FabricItemId,
            FabricColorId = i.FabricColorId,
            RollCount = i.RollCount,
            UnitPrice = i.UnitPrice.Amount,
            LineTotal = i.LineTotal.Amount
        }).ToList()
    };

    public static WarehouseDetailingDto ToDetailingDto(SalesInvoiceAggregate aggregate, string customerName = "") => new()
    {
        InvoiceId = aggregate.Id,
        InvoiceNumber = aggregate.InvoiceNumber.Value,
        CustomerName = customerName,
        Status = aggregate.DetailingSession?.Status ?? WarehouseDetailingStatus.Pending,
        Rolls = aggregate.RollDetails.Select(r => new WarehouseDetailingRollDto
        {
            RollDetailId = r.Id,
            SalesInvoiceItemId = r.SalesInvoiceItemId,
            RollSequence = r.RollSequence.Value,
            LengthMeters = r.LengthMeters.Value,
            HasValidLength = r.HasValidLength
        }).ToList()
    };

    public static SalesInvoiceOperationsCenterDto ToOperationsCenterDto(
        SalesInvoiceAggregate aggregate,
        string customerName = "") => new()
    {
        Invoice = ToDto(aggregate, customerName),
        Detailing = aggregate.Status is SalesInvoiceStatus.AwaitingDetailing or SalesInvoiceStatus.Detailed
            ? ToDetailingDto(aggregate, customerName)
            : null,
        CanSendToWarehouse = aggregate.Status == SalesInvoiceStatus.Draft && aggregate.Items.Count > 0,
        CanCompleteDetailing = aggregate.Status == SalesInvoiceStatus.AwaitingDetailing,
        CanApprove = aggregate.Status is SalesInvoiceStatus.Detailed or SalesInvoiceStatus.ReadyForApproval,
        CanCancel = aggregate.Status is not (
            SalesInvoiceStatus.Approved or
            SalesInvoiceStatus.Printed or
            SalesInvoiceStatus.Delivered)
    };
}

public static class WarehouseMapper
{
    public static WarehouseListDto ToListDto(WarehouseAggregate aggregate) => new()
    {
        Id = aggregate.Warehouse.Id,
        Code = aggregate.Warehouse.Code,
        NameAr = aggregate.Warehouse.NameAr,
        City = aggregate.Warehouse.City,
        IsActive = aggregate.Warehouse.IsActive
    };
}

public static class FinanceMapper
{
    public static ReceiptVoucherDto ToDto(ReceiptVoucher voucher, string customerName = "") => new()
    {
        Id = voucher.Id,
        VoucherNumber = voucher.VoucherNumber,
        CustomerId = voucher.CustomerId,
        CustomerName = customerName,
        CashboxId = voucher.CashboxId,
        Amount = voucher.Amount.Amount,
        VoucherDate = voucher.VoucherDate,
        Status = voucher.Status
    };

    public static PaymentVoucherDto ToDto(PaymentVoucher voucher, string supplierName = "") => new()
    {
        Id = voucher.Id,
        VoucherNumber = voucher.VoucherNumber,
        SupplierId = voucher.SupplierId,
        SupplierName = supplierName,
        CashboxId = voucher.CashboxId,
        Amount = voucher.Amount.Amount,
        VoucherDate = voucher.VoucherDate,
        Status = voucher.Status
    };

    public static JournalEntryDto ToDto(AccountingAggregate entry) => new()
    {
        Id = entry.Id,
        EntryNumber = entry.EntryNumber,
        EntryDate = entry.EntryDate,
        Description = entry.Description,
        Status = entry.Status,
        DebitTotal = entry.DebitTotal.Amount,
        CreditTotal = entry.CreditTotal.Amount,
        Lines = entry.Lines.Select(l => new JournalEntryLineDto
        {
            AccountId = l.AccountId,
            Debit = l.Debit.Amount,
            Credit = l.Credit.Amount,
            Narrative = l.Narrative
        }).ToList()
    };
}
