using ERPSystem.Application.Common;
using ERPSystem.Application.DTOs.Containers;
using ERPSystem.Application.DTOs.Customers;
using ERPSystem.Application.DTOs.Suppliers;
using ERPSystem.Application.DTOs.Finance;
using ERPSystem.Application.DTOs.Sales;
using ERPSystem.Application.DTOs.Warehouses;
using ERPSystem.Domain.Aggregates;
using ERPSystem.Domain.Entities.ChinaImport;
using ERPSystem.Domain.Entities.Finance;
using ERPSystem.Domain.Enums;
using ERPSystem.Domain.Services;

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
        CreditLimitEnabled = aggregate.Customer.CreditLimitEnabled,
        IsActive = aggregate.Customer.IsActive,
        OpeningBalancePosted = aggregate.Customer.OpeningBalancePosted
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
        CreditLimitEnabled = aggregate.Customer.CreditLimitEnabled,
        PaymentTermsDays = aggregate.Customer.PaymentTermsDays,
        Phone = aggregate.Customer.Phone?.Value,
        Email = aggregate.Customer.Email?.Value,
        IsActive = aggregate.Customer.IsActive,
        OpeningBalancePosted = aggregate.Customer.OpeningBalancePosted
    };
}

public static class SupplierMapper
{
    public static SupplierListDto ToListDto(SupplierAggregate aggregate) => new()
    {
        Id = aggregate.Supplier.Id,
        Code = aggregate.Supplier.Code,
        NameAr = aggregate.Supplier.NameAr,
        NameEn = aggregate.Supplier.NameEn,
        Country = aggregate.Supplier.Country,
        Phone = aggregate.Supplier.Phone,
        Balance = aggregate.Supplier.Balance.Amount,
        PaymentTermsDays = aggregate.Supplier.PaymentTermsDays,
        PaymentTermsDisplay = SupplierPaymentTermsDisplay.Format(aggregate.Supplier.PaymentTermsDays),
        Status = aggregate.Supplier.Status,
        IsActive = aggregate.Supplier.IsActive,
        OpeningBalancePosted = aggregate.Supplier.OpeningBalancePosted
    };

    public static SupplierDetailsDto ToDetailsDto(SupplierAggregate aggregate, string? payablesAccountName = null) => new()
    {
        Id = aggregate.Supplier.Id,
        Code = aggregate.Supplier.Code,
        NameAr = aggregate.Supplier.NameAr,
        NameEn = aggregate.Supplier.NameEn,
        Phone = aggregate.Supplier.Phone,
        Email = aggregate.Supplier.Email,
        Address = aggregate.Supplier.Address,
        Country = aggregate.Supplier.Country,
        City = aggregate.Supplier.City,
        CurrencyCode = aggregate.Supplier.CurrencyCode,
        PaymentTermsDays = aggregate.Supplier.PaymentTermsDays,
        PaymentTermsDisplay = SupplierPaymentTermsDisplay.Format(aggregate.Supplier.PaymentTermsDays),
        CreditLimit = aggregate.Supplier.CreditLimit.Amount,
        TaxNumber = aggregate.Supplier.TaxNumber,
        PayablesAccountId = aggregate.Supplier.PayablesAccountId,
        PayablesAccountName = payablesAccountName,
        Notes = aggregate.Supplier.Notes,
        Balance = aggregate.Supplier.Balance.Amount,
        Status = aggregate.Supplier.Status,
        IsActive = aggregate.Supplier.IsActive,
        OpeningBalancePosted = aggregate.Supplier.OpeningBalancePosted
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
        SupplierName = supplierName,
        DplQuantityUnit = aggregate.DplQuantityUnit
    };

    public static ContainerDetailsDto ToDetailsDto(ContainerAggregate aggregate, string supplierName = "") => new()
    {
        Id = aggregate.Id,
        ContainerNumber = aggregate.ContainerNumber.Value,
        Status = aggregate.Status,
        SupplierId = aggregate.SupplierId,
        SupplierName = supplierName,
        ShipmentDate = aggregate.ShipmentDate,
        ArrivalDate = aggregate.ArrivalDate,
        TotalRolls = aggregate.TotalRolls,
        TotalMeters = aggregate.TotalMeters.Value,
        TotalWeightKg = aggregate.TotalWeight?.Value,
        ExchangeRateToLocalCurrency = aggregate.ExchangeRateToLocalCurrency,
        ChinaInvoiceAmountUsd = aggregate.ChinaInvoiceAmountUsd,
        DplQuantityUnit = aggregate.DplQuantityUnit,
        FinancialTaxReserveUsd = ChinaImportFinancials.TaxReserveUsd(aggregate.ChinaInvoiceAmountUsd),
        FinancialTaxReservePostedLocal = aggregate.FinancialTaxReservePostedLocal,
        LandingCost = aggregate.LandingCost is null ? null : ToLandingCostDto(aggregate.LandingCost),
        FabricTypeLines = aggregate.FabricTypeLines.Select(ToFabricTypeLineDto).ToList(),
        Items = aggregate.Items.Select(i => new ContainerItemDto
        {
            LineNumber = i.LineNumber,
            FabricItemId = i.FabricItemId,
            FabricColorId = i.FabricColorId,
            RollCount = i.RollCount,
            LengthMeters = i.LengthMeters.Value,
            DplQuantityNative = i.DplQuantityNative,
            DplQuantityUnit = i.DplQuantityUnit,
            IsValid = i.IsValid
        }).ToList()
    };

    public static LandingCostDto ToLandingCostDto(LandingCost landingCost) => new()
    {
        TotalLengthMeters = landingCost.TotalLengthFromInvoice.Value,
        ContainerWeightKg = landingCost.ContainerWeight.Value,
        CustomsAmount = landingCost.CustomsAmountPaid.Amount,
        Shipping = landingCost.Shipping.Amount,
        Insurance = landingCost.Insurance.Amount,
        Clearance = landingCost.Clearance.Amount,
        OtherExpenses = landingCost.OtherExpenses.Amount,
        OtherExpense1 = landingCost.OtherExpense1.Amount,
        OtherExpense2 = landingCost.OtherExpense2.Amount,
        OtherExpense3 = landingCost.OtherExpense3.Amount,
        OtherExpense4 = landingCost.OtherExpense4.Amount,
        UsesWeightedAllocation = landingCost.UsesWeightedAllocation,
        TotalImportExpenses = landingCost.TotalImportExpenses.Amount,
        CustomsCostPerMeter = landingCost.CustomsCostPerMeter,
        ExpenseCostPerMeter = landingCost.ExpenseCostPerMeter,
        AvgGramPerMeter = landingCost.AvgGramPerMeter,
        Status = landingCost.Status
    };

    public static ContainerFabricTypeLineDto ToFabricTypeLineDto(ContainerFabricTypeLine line) => new()
    {
        Id = line.Id,
        LineNumber = line.LineNumber,
        TypeDisplayName = line.TypeDisplayName,
        FabricItemId = line.FabricItemId,
        FabricColorId = line.FabricColorId,
        LengthMeters = line.LengthMeters,
        RollCount = line.RollCount,
        NetWeightKg = line.NetWeightKg,
        ChinaUnitPriceUsd = line.ChinaUnitPriceUsd,
        ExpenseShareUsd = line.ExpenseShareUsd,
        LandedCostPerMeterUsd = line.LandedCostPerMeterUsd,
        MarginPerMeterUsd = line.MarginPerMeterUsd,
        SalePricePerMeterUsd = line.SalePricePerMeterUsd
    };

    public static ContainerOperationsCenterDto ToOperationsCenterDto(ContainerAggregate aggregate, string supplierName = "") => new()
    {
        Container = ToDetailsDto(aggregate, supplierName),
        CanApprove = aggregate.LandingCost?.Status == LandingCostStatus.Reviewed
            && aggregate.Status == ChinaContainerStatus.LandingCostReviewed
            && (aggregate.FabricTypeLines.Count == 0
                || aggregate.FabricTypeLines.All(l => l.SalePricePerMeterUsd > 0)),
        CanSetSalePrices = aggregate.FabricTypeLines.Count > 0
            && aggregate.LandingCost?.Status == LandingCostStatus.Reviewed
            && aggregate.Status == ChinaContainerStatus.LandingCostReviewed,
        CanMoveToWarehouse = aggregate.Status == ChinaContainerStatus.Approved,
        CanCalculateLandingCost = aggregate.TotalMeters.Value > 0 && aggregate.LandingCost is null,
        IsReadyForSale = aggregate.Status == ChinaContainerStatus.InWarehouse
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
        PartialPaymentAmount = aggregate.PartialPaymentAmount?.Amount ?? 0,
        CashboxId = aggregate.CashboxId,
        SubTotal = aggregate.SubTotal.Amount,
        DiscountTotal = aggregate.DiscountTotal.Amount,
        TaxTotal = aggregate.TaxTotal.Amount,
        GrandTotal = aggregate.GrandTotal.Amount,
        RoundingDifference = aggregate.RoundingDifference,
        IsLegacyUntaxed = aggregate.IsLegacyUntaxed,
        SentToWarehouseAt = aggregate.SentToWarehouseAt,
        DetailedAt = aggregate.DetailedAt,
        ApprovedAt = aggregate.ApprovedAt,
        PrintedAt = aggregate.PrintedAt,
        DeliveredAt = aggregate.DeliveredAt,
        CancelledAt = aggregate.CancelledAt,
        DeliveredToName = aggregate.DeliveredToName,
        DeliveryDriverName = aggregate.DeliveryDriverName,
        DeliveryNotes = aggregate.DeliveryNotes,
        CancelReason = aggregate.CancelReason,
        Lines = aggregate.Items.Select(i =>
        {
            var lineRolls = aggregate.RollDetails.Where(r => r.SalesInvoiceItemId == i.Id);
            var totalLength = lineRolls.Where(r => r.HasValidLength).Sum(r => r.LengthMeters.Value);
            var snap = aggregate.ItemTaxSnapshots.FirstOrDefault(s => s.SalesInvoiceItemId == i.Id);
            return new SalesInvoiceLineDto
            {
                Id = i.Id,
                LineNumber = i.LineNumber,
                ChinaContainerId = i.ChinaContainerId,
                FabricItemId = i.FabricItemId,
                FabricColorId = i.FabricColorId,
                RollCount = i.RollCount,
                UnitPrice = i.UnitPrice.Amount,
                OriginalUnitPrice = i.OriginalUnitPrice.Amount,
                Unit = i.Unit,
                TotalLengthMeters = totalLength,
                LineTotal = i.LineTotal.Amount,
                DiscountAmount = i.DiscountAmount.Amount,
                DiscountReason = i.DiscountReason,
                TaxCodeId = i.TaxCodeId,
                TaxCode = snap?.TaxCode,
                TaxName = snap?.TaxName,
                TaxRate = snap?.TaxRate ?? 0m,
                TaxCategory = snap is null ? null : snap.TaxRate <= 0 && snap.TaxAmount.Amount == 0 ? TaxCategory.Exempt : TaxCategory.Standard,
                IsTaxInclusive = snap?.IsInclusive ?? false,
                TaxableAmount = snap?.TaxableAmount.Amount ?? 0m,
                TaxAmount = snap?.TaxAmount.Amount ?? 0m,
                Notes = i.Notes,
                RollLengths = lineRolls
                    .Where(r => r.HasValidLength)
                    .OrderBy(r => r.RollSequence.Value)
                    .Select(r => new SalesInvoiceRollLengthDto
                    {
                        RollSequence = r.RollSequence.Value,
                        LengthMeters = r.LengthMeters.Value
                    })
                    .ToList()
            };
        }).ToList()
    };

    public static WarehouseDetailingDto ToDetailingDto(SalesInvoiceAggregate aggregate, string customerName = "") => new()
    {
        InvoiceId = aggregate.Id,
        InvoiceNumber = aggregate.InvoiceNumber.Value,
        CustomerName = customerName,
        WarehouseId = aggregate.WarehouseId,
        ChinaContainerId = aggregate.ChinaContainerId,
        SentToWarehouseAt = aggregate.SentToWarehouseAt,
        RepresentativeUnitPrice = aggregate.Items.FirstOrDefault()?.UnitPrice.Amount,
        Status = aggregate.DetailingSession?.Status ?? WarehouseDetailingStatus.Pending,
        Rolls = aggregate.RollDetails.Select(r =>
        {
            var item = aggregate.Items.FirstOrDefault(i => i.Id == r.SalesInvoiceItemId);
            return new WarehouseDetailingRollDto
            {
                RollDetailId = r.Id,
                SalesInvoiceItemId = r.SalesInvoiceItemId,
                RollSequence = r.RollSequence.Value,
                FabricItemId = item?.FabricItemId ?? Guid.Empty,
                FabricColorId = item?.FabricColorId ?? Guid.Empty,
                LengthMeters = r.LengthMeters.Value,
                HasValidLength = r.HasValidLength,
                ChinaContainerId = item?.ChinaContainerId ?? aggregate.ChinaContainerId,
                DraftRollNumber = r.DraftRollNumber,
                DraftLengthMeters = r.DraftLengthMeters
            };
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
