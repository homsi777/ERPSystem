using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Commands.Purchases;
using ERPSystem.Application.Common;
using ERPSystem.Application.Results;
using ERPSystem.Domain.Entities.Purchasing;
using ERPSystem.Domain.Enums;
using ERPSystem.Domain.ValueObjects;

namespace ERPSystem.Application.UseCases.Purchases;

public sealed class CreatePurchaseInvoiceDraftHandler(
    IPurchaseInvoiceRepository invoiceRepository,
    ISupplierRepository supplierRepository,
    INumberingService numberingService,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService)
    : ICommandHandler<CreatePurchaseInvoiceDraftCommand, ApplicationResult<Guid>>
{
    public async Task<ApplicationResult<Guid>> HandleAsync(
        CreatePurchaseInvoiceDraftCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!await permissionService.CanAsync("purchases.create", cancellationToken))
            return ApplicationResult<Guid>.PermissionDenied("Not allowed.");

        if (command.SupplierId == Guid.Empty)
            return ApplicationResult<Guid>.ValidationFailed(nameof(command.SupplierId), "Supplier is required.");
        if (command.Lines.Count == 0)
            return ApplicationResult<Guid>.ValidationFailed("Lines", "At least one line is required.");

        var supplier = await supplierRepository.GetByIdAsync(command.SupplierId, cancellationToken);
        if (supplier is null)
            return ApplicationResult<Guid>.NotFound("Supplier not found.");

        try
        {
            var number = string.IsNullOrWhiteSpace(command.InvoiceNumber)
                ? await numberingService.NextPurchaseInvoiceNumberAsync(command.BranchId, cancellationToken)
                : command.InvoiceNumber.Trim();

            var invoice = PurchaseInvoice.CreateDraft(
                command.CompanyId,
                command.BranchId,
                number,
                command.SupplierId,
                command.InvoiceDate,
                command.DueDate,
                command.CurrencyCode,
                command.WarehouseId,
                command.PurchaseOrderId);

            invoice.UpdateHeader(
                command.SupplierId,
                command.InvoiceDate,
                command.DueDate,
                command.SupplierReference,
                command.WarehouseId,
                command.CurrencyCode,
                command.DiscountAmount,
                command.TaxAmount,
                command.Notes);

            invoice.ReplaceItems(MapLines(command.Lines, command.CurrencyCode));
            await invoiceRepository.AddAsync(invoice, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationResult<Guid>.Success(invoice.Id);
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult<Guid>();
        }
    }

    internal static List<PurchaseInvoiceItem> MapLines(IReadOnlyList<PurchaseInvoiceLineInput> lines, string currency)
    {
        var result = new List<PurchaseInvoiceItem>();
        foreach (var line in lines)
        {
            if (line.LineType == (int)PurchaseLineType.Expense)
            {
                result.Add(PurchaseInvoiceItem.CreateExpenseLine(
                    line.ExpenseAccountId ?? AccountingAccountIds.OperatingExpenses,
                    new Money(line.UnitPrice, currency),
                    line.Description));
            }
            else if (line.QuantityMeters > 0 && line.UnitPrice > 0 && line.FabricItemId.HasValue)
            {
                result.Add(PurchaseInvoiceItem.CreateInventoryLine(
                    line.FabricItemId.Value,
                    line.FabricColorId,
                    new LengthInMeters(line.QuantityMeters),
                    line.RollCount,
                    new Money(line.UnitPrice, currency),
                    line.Description));
            }
        }
        return result;
    }
}

public sealed class UpdatePurchaseInvoiceDraftHandler(
    IPurchaseInvoiceRepository invoiceRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService)
    : ICommandHandler<UpdatePurchaseInvoiceDraftCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        UpdatePurchaseInvoiceDraftCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!await permissionService.CanAsync("purchases.create", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed.");

        var invoice = await invoiceRepository.GetByIdAsync(command.InvoiceId, cancellationToken);
        if (invoice is null)
            return ApplicationResult.NotFound("Invoice not found.");

        try
        {
            invoice.UpdateHeader(
                command.SupplierId,
                command.InvoiceDate,
                command.DueDate,
                command.SupplierReference,
                command.WarehouseId,
                command.CurrencyCode,
                command.DiscountAmount,
                command.TaxAmount,
                command.Notes);
            invoice.ReplaceItems(CreatePurchaseInvoiceDraftHandler.MapLines(command.Lines, command.CurrencyCode));
            await invoiceRepository.UpdateAsync(invoice, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationResult.Success();
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult();
        }
    }
}

public sealed class PostPurchaseInvoiceHandler(
    IPurchaseInvoiceRepository invoiceRepository,
    ISupplierRepository supplierRepository,
    IPurchaseInventoryService inventoryService,
    IIntegratedAccountingService accountingService,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService)
    : ICommandHandler<PostPurchaseInvoiceCommand, ApplicationResult<string>>
{
    public async Task<ApplicationResult<string>> HandleAsync(
        PostPurchaseInvoiceCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!await permissionService.CanAsync("purchases.post", cancellationToken))
            return ApplicationResult<string>.PermissionDenied("Not allowed.");

        var invoice = await invoiceRepository.GetByIdAsync(command.InvoiceId, cancellationToken);
        if (invoice is null)
            return ApplicationResult<string>.NotFound("Invoice not found.");

        var supplierAgg = await supplierRepository.GetByIdAsync(invoice.SupplierId, cancellationToken);
        if (supplierAgg is null)
            return ApplicationResult<string>.NotFound("Supplier not found.");

        if (supplierAgg.Supplier.PayablesAccountId == Guid.Empty)
            return ApplicationResult<string>.ValidationFailed("Supplier", "Supplier has no payables account.");

        if (invoice.Items.Any(i => i.LineType == PurchaseLineType.Inventory) && !invoice.WarehouseId.HasValue)
            return ApplicationResult<string>.ValidationFailed("Warehouse", "Warehouse is required for inventory lines.");

        try
        {
            invoice.Post(command.UserId);
            await inventoryService.PostPurchaseInvoiceStockAsync(invoice, cancellationToken);
            var entryNumber = await accountingService.PostPurchaseInvoiceAsync(
                invoice, supplierAgg.Supplier.PayablesAccountId, cancellationToken);
            supplierAgg.Supplier.ApplyPostedPurchase(invoice.TotalAmount);
            await invoiceRepository.UpdateAsync(invoice, cancellationToken);
            await supplierRepository.UpdateAsync(supplierAgg, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationResult<string>.Success(entryNumber);
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult<string>();
        }
    }
}

public sealed class CancelPurchaseInvoiceHandler(
    IPurchaseInvoiceRepository invoiceRepository,
    ISupplierRepository supplierRepository,
    IPurchaseInventoryService inventoryService,
    IIntegratedAccountingService accountingService,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService)
    : ICommandHandler<CancelPurchaseInvoiceCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        CancelPurchaseInvoiceCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!await permissionService.CanAsync("purchases.post", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed.");

        var invoice = await invoiceRepository.GetByIdAsync(command.InvoiceId, cancellationToken);
        if (invoice is null)
            return ApplicationResult.NotFound("Invoice not found.");

        // Draft invoices are simply voided — no posted effects to reverse.
        if (invoice.Status == Domain.Enums.PurchaseInvoiceStatus.Draft)
        {
            try
            {
                invoice.Cancel();
                await invoiceRepository.UpdateAsync(invoice, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return ApplicationResult.Success();
            }
            catch (Exception ex)
            {
                return ex.ToFailureResult();
            }
        }

        // Posted invoices require a full reversal chain (GL + inventory + supplier balance).
        if (invoice.Status is Domain.Enums.PurchaseInvoiceStatus.PartiallyPaid or Domain.Enums.PurchaseInvoiceStatus.Paid)
            return ApplicationResult.Failure("لا يمكن إلغاء فاتورة تم سدادها جزئياً أو كلياً.");

        var supplierAgg = await supplierRepository.GetByIdAsync(invoice.SupplierId, cancellationToken);
        if (supplierAgg is null)
            return ApplicationResult.NotFound("Supplier not found.");

        try
        {
            invoice.Reverse();

            await inventoryService.ReversePurchaseInvoiceStockAsync(invoice, cancellationToken);
            await accountingService.ReversePurchaseInvoiceAsync(
                invoice, supplierAgg.Supplier.PayablesAccountId, cancellationToken);
            supplierAgg.Supplier.ApplyPostedPayment(invoice.TotalAmount);

            await invoiceRepository.UpdateAsync(invoice, cancellationToken);
            await supplierRepository.UpdateAsync(supplierAgg, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationResult.Success();
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult();
        }
    }
}

public sealed class CreatePurchaseOrderHandler(
    IPurchaseOrderRepository orderRepository,
    INumberingService numberingService,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService)
    : ICommandHandler<CreatePurchaseOrderCommand, ApplicationResult<Guid>>
{
    public async Task<ApplicationResult<Guid>> HandleAsync(
        CreatePurchaseOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!await permissionService.CanAsync("purchases.create", cancellationToken))
            return ApplicationResult<Guid>.PermissionDenied("Not allowed.");

        try
        {
            var number = await numberingService.NextPurchaseOrderNumberAsync(command.BranchId, cancellationToken);
            var order = PurchaseOrder.CreateDraft(command.CompanyId, command.BranchId, number, command.SupplierId, command.OrderDate);
            order.Update(command.SupplierId, command.ExpectedDeliveryDate, command.Notes);
            var lines = command.Lines.Select(l =>
                PurchaseOrderLine.Create(l.FabricItemId, l.Description, l.Quantity, new Money(l.UnitCost))).ToList();
            order.ReplaceLines(lines);
            await orderRepository.AddAsync(order, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationResult<Guid>.Success(order.Id);
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult<Guid>();
        }
    }
}

public sealed class ConvertPurchaseOrderToInvoiceHandler(
    IPurchaseOrderRepository orderRepository,
    ISupplierRepository supplierRepository,
    IPurchaseInvoiceRepository invoiceRepository,
    INumberingService numberingService,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService)
    : ICommandHandler<ConvertPurchaseOrderToInvoiceCommand, ApplicationResult<Guid>>
{
    public async Task<ApplicationResult<Guid>> HandleAsync(
        ConvertPurchaseOrderToInvoiceCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!await permissionService.CanAsync("purchases.create", cancellationToken))
            return ApplicationResult<Guid>.PermissionDenied("Not allowed.");

        var order = await orderRepository.GetByIdAsync(command.OrderId, cancellationToken);
        if (order is null)
            return ApplicationResult<Guid>.NotFound("Purchase order not found.");

        var supplier = await supplierRepository.GetByIdAsync(order.SupplierId, cancellationToken);
        if (supplier is null)
            return ApplicationResult<Guid>.NotFound("Supplier not found.");

        try
        {
            var number = await numberingService.NextPurchaseInvoiceNumberAsync(command.BranchId, cancellationToken);
            var dueDate = DateTime.UtcNow.Date.AddDays(supplier.Supplier.PaymentTermsDays);
            var invoice = PurchaseInvoice.CreateDraft(
                command.CompanyId, command.BranchId, number, order.SupplierId,
                DateTime.UtcNow, dueDate, purchaseOrderId: order.Id);
            var lines = order.Lines
                .Where(l => l.FabricItemId.HasValue)
                .Select(l => PurchaseInvoiceItem.CreateInventoryLine(
                    l.FabricItemId!.Value, null, new LengthInMeters(l.Quantity), 1,
                    l.UnitCost, l.Description))
                .ToList();
            invoice.ReplaceItems(lines);
            await invoiceRepository.AddAsync(invoice, cancellationToken);
            order.MarkReceived();
            await orderRepository.UpdateAsync(order, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationResult<Guid>.Success(invoice.Id);
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult<Guid>();
        }
    }
}

public sealed class CreatePurchaseReturnHandler(
    IPurchaseReturnRepository returnRepository,
    IPurchaseInvoiceRepository invoiceRepository,
    INumberingService numberingService,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService)
    : ICommandHandler<CreatePurchaseReturnCommand, ApplicationResult<Guid>>
{
    public async Task<ApplicationResult<Guid>> HandleAsync(
        CreatePurchaseReturnCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!await permissionService.CanAsync("purchases.create", cancellationToken))
            return ApplicationResult<Guid>.PermissionDenied("Not allowed.");

        var original = await invoiceRepository.GetByIdAsync(command.OriginalInvoiceId, cancellationToken);
        if (original is null || original.Status is not (PurchaseInvoiceStatus.Posted or PurchaseInvoiceStatus.PartiallyPaid or PurchaseInvoiceStatus.Paid))
            return ApplicationResult<Guid>.ValidationFailed("OriginalInvoiceId", "Original posted invoice is required.");
        if (string.IsNullOrWhiteSpace(command.Notes))
            return ApplicationResult<Guid>.ValidationFailed("Notes", "Return reason is required.");

        try
        {
            var number = await numberingService.NextPurchaseReturnNumberAsync(command.BranchId, cancellationToken);
            var ret = PurchaseReturn.CreateDraft(command.CompanyId, command.BranchId, number, command.OriginalInvoiceId, command.ReturnDate);
            ret.UpdateNotes(command.Notes);
            var lines = new List<PurchaseReturnLine>();
            foreach (var l in command.Lines)
            {
                var originalLine = original.Items.FirstOrDefault(i => i.Id == l.OriginalInvoiceItemId);
                if (originalLine is null)
                    return ApplicationResult<Guid>.ValidationFailed("Lines", "Invalid invoice line reference.");
                if (l.QuantityMeters <= 0)
                    return ApplicationResult<Guid>.ValidationFailed("Lines", "Return quantity must be positive.");
                if (l.QuantityMeters > originalLine.Quantity.Value)
                    return ApplicationResult<Guid>.ValidationFailed("Lines", $"Cannot return more than invoiced quantity for line {originalLine.Description}.");
                lines.Add(PurchaseReturnLine.Create(
                    l.OriginalInvoiceItemId,
                    (PurchaseLineType)l.LineType,
                    l.FabricItemId,
                    l.FabricColorId,
                    l.QuantityMeters,
                    new Money(l.UnitPrice)));
            }
            ret.ReplaceLines(lines);
            await returnRepository.AddAsync(ret, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationResult<Guid>.Success(ret.Id);
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult<Guid>();
        }
    }
}

public sealed class PostPurchaseReturnHandler(
    IPurchaseReturnRepository returnRepository,
    IPurchaseInvoiceRepository invoiceRepository,
    ISupplierRepository supplierRepository,
    IPurchaseInventoryService inventoryService,
    IIntegratedAccountingService accountingService,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService)
    : ICommandHandler<PostPurchaseReturnCommand, ApplicationResult<string>>
{
    public async Task<ApplicationResult<string>> HandleAsync(
        PostPurchaseReturnCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!await permissionService.CanAsync("purchases.post", cancellationToken))
            return ApplicationResult<string>.PermissionDenied("Not allowed.");

        var ret = await returnRepository.GetByIdAsync(command.ReturnId, cancellationToken);
        if (ret is null)
            return ApplicationResult<string>.NotFound("Return not found.");

        var original = await invoiceRepository.GetByIdAsync(ret.OriginalInvoiceId, cancellationToken);
        if (original is null)
            return ApplicationResult<string>.NotFound("Original invoice not found.");

        var supplierAgg = await supplierRepository.GetByIdAsync(original.SupplierId, cancellationToken);
        if (supplierAgg is null)
            return ApplicationResult<string>.NotFound("Supplier not found.");

        try
        {
            ret.Post();
            await inventoryService.ReversePurchaseReturnStockAsync(ret, original, cancellationToken);
            var entryNumber = await accountingService.PostPurchaseReturnAsync(
                ret, supplierAgg.Supplier.PayablesAccountId, cancellationToken);
            supplierAgg.Supplier.ApplyPostedPayment(ret.TotalAmount);
            await returnRepository.UpdateAsync(ret, cancellationToken);
            await supplierRepository.UpdateAsync(supplierAgg, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationResult<string>.Success(entryNumber);
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult<string>();
        }
    }
}

public sealed class UpdatePurchaseOrderHandler(
    IPurchaseOrderRepository orderRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService)
    : ICommandHandler<UpdatePurchaseOrderCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        UpdatePurchaseOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!await permissionService.CanAsync("purchases.create", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed.");

        var order = await orderRepository.GetByIdAsync(command.OrderId, cancellationToken);
        if (order is null)
            return ApplicationResult.NotFound("Purchase order not found.");

        try
        {
            order.Update(command.SupplierId, command.ExpectedDeliveryDate, command.Notes);
            var lines = command.Lines.Select(l =>
                PurchaseOrderLine.Create(l.FabricItemId, l.Description, l.Quantity, new Money(l.UnitCost))).ToList();
            order.ReplaceLines(lines);
            await orderRepository.UpdateAsync(order, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationResult.Success();
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult();
        }
    }
}

public sealed class SendPurchaseOrderHandler(
    IPurchaseOrderRepository orderRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService)
    : ICommandHandler<SendPurchaseOrderCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        SendPurchaseOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!await permissionService.CanAsync("purchases.create", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed.");

        var order = await orderRepository.GetByIdAsync(command.OrderId, cancellationToken);
        if (order is null)
            return ApplicationResult.NotFound("Purchase order not found.");
        if (order.Status != PurchaseOrderStatus.Draft)
            return ApplicationResult.ValidationFailed("Status", "Only draft orders can be sent.");
        if (order.Lines.Count == 0)
            return ApplicationResult.ValidationFailed("Lines", "Order must have at least one line.");

        try
        {
            order.MarkSent();
            await orderRepository.UpdateAsync(order, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationResult.Success();
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult();
        }
    }
}

public sealed class CancelPurchaseOrderHandler(
    IPurchaseOrderRepository orderRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService)
    : ICommandHandler<CancelPurchaseOrderCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        CancelPurchaseOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!await permissionService.CanAsync("purchases.create", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed.");

        var order = await orderRepository.GetByIdAsync(command.OrderId, cancellationToken);
        if (order is null)
            return ApplicationResult.NotFound("Purchase order not found.");
        if (order.Status is not (PurchaseOrderStatus.Draft or PurchaseOrderStatus.Sent))
            return ApplicationResult.ValidationFailed("Status", "Only draft or sent orders can be cancelled.");

        try
        {
            order.Cancel();
            await orderRepository.UpdateAsync(order, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationResult.Success();
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult();
        }
    }
}

public sealed class UpdatePurchaseReturnDraftHandler(
    IPurchaseReturnRepository returnRepository,
    IPurchaseInvoiceRepository invoiceRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService)
    : ICommandHandler<UpdatePurchaseReturnDraftCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        UpdatePurchaseReturnDraftCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!await permissionService.CanAsync("purchases.create", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed.");
        if (string.IsNullOrWhiteSpace(command.Notes))
            return ApplicationResult.ValidationFailed("Notes", "Return reason is required.");

        var ret = await returnRepository.GetByIdAsync(command.ReturnId, cancellationToken);
        if (ret is null)
            return ApplicationResult.NotFound("Return not found.");

        var original = await invoiceRepository.GetByIdAsync(ret.OriginalInvoiceId, cancellationToken);
        if (original is null)
            return ApplicationResult.NotFound("Original invoice not found.");

        try
        {
            ret.UpdateNotes(command.Notes);
            var lines = new List<PurchaseReturnLine>();
            foreach (var l in command.Lines)
            {
                var originalLine = original.Items.FirstOrDefault(i => i.Id == l.OriginalInvoiceItemId);
                if (originalLine is null)
                    return ApplicationResult.ValidationFailed("Lines", "Invalid invoice line reference.");
                if (l.QuantityMeters <= 0)
                    return ApplicationResult.ValidationFailed("Lines", "Return quantity must be positive.");
                if (l.QuantityMeters > originalLine.Quantity.Value)
                    return ApplicationResult.ValidationFailed("Lines", "Cannot return more than invoiced quantity.");
                lines.Add(PurchaseReturnLine.Create(
                    l.OriginalInvoiceItemId,
                    (PurchaseLineType)l.LineType,
                    l.FabricItemId,
                    l.FabricColorId,
                    l.QuantityMeters,
                    new Money(l.UnitPrice)));
            }
            ret.ReplaceLines(lines);
            await returnRepository.UpdateAsync(ret, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationResult.Success();
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult();
        }
    }
}
