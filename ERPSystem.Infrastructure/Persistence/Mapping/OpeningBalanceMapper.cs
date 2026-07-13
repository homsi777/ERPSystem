using ERPSystem.Domain.Entities.Finance;
using ERPSystem.Infrastructure.Persistence.Mapping;
using ERPSystem.Infrastructure.Persistence.Models.Finance;

namespace ERPSystem.Infrastructure.Persistence.Mapping;

internal static class OpeningBalanceMapper
{
    public static OpeningBalanceDocument ToAggregate(OpeningBalanceDocumentEntity entity)
    {
        var doc = DomainHydrator.Create<OpeningBalanceDocument>();
        DomainHydrator.Set(doc, nameof(OpeningBalanceDocument.Id), entity.Id);
        DomainHydrator.Set(doc, nameof(OpeningBalanceDocument.CompanyId), entity.CompanyId);
        DomainHydrator.Set(doc, nameof(OpeningBalanceDocument.BranchId), entity.BranchId);
        DomainHydrator.Set(doc, nameof(OpeningBalanceDocument.Number), entity.Number);
        DomainHydrator.Set(doc, nameof(OpeningBalanceDocument.Type), (OpeningBalanceType)entity.Type);
        DomainHydrator.Set(doc, nameof(OpeningBalanceDocument.Status), (OpeningBalanceStatus)entity.Status);
        DomainHydrator.Set(doc, nameof(OpeningBalanceDocument.Source), (OpeningBalanceSource)entity.Source);
        DomainHydrator.Set(doc, nameof(OpeningBalanceDocument.OpeningDate), entity.OpeningDate);
        DomainHydrator.Set(doc, nameof(OpeningBalanceDocument.CurrencyCode), entity.CurrencyCode);
        DomainHydrator.Set(doc, nameof(OpeningBalanceDocument.ExchangeRate), entity.ExchangeRate);
        DomainHydrator.Set(doc, nameof(OpeningBalanceDocument.Reference), entity.Reference);
        DomainHydrator.Set(doc, nameof(OpeningBalanceDocument.Description), entity.Description);
        DomainHydrator.Set(doc, nameof(OpeningBalanceDocument.Notes), entity.Notes);
        DomainHydrator.Set(doc, nameof(OpeningBalanceDocument.TotalDebit), entity.TotalDebit);
        DomainHydrator.Set(doc, nameof(OpeningBalanceDocument.TotalCredit), entity.TotalCredit);
        DomainHydrator.Set(doc, nameof(OpeningBalanceDocument.TotalBaseAmount), entity.TotalBaseAmount);
        DomainHydrator.Set(doc, nameof(OpeningBalanceDocument.JournalEntryNumber), entity.JournalEntryNumber);
        DomainHydrator.Set(doc, nameof(OpeningBalanceDocument.CreatedAt), entity.CreatedAt);
        DomainHydrator.Set(doc, nameof(OpeningBalanceDocument.CreatedByUserId), entity.CreatedByUserId);
        DomainHydrator.Set(doc, nameof(OpeningBalanceDocument.UpdatedAt), entity.UpdatedAt);
        DomainHydrator.Set(doc, nameof(OpeningBalanceDocument.ApprovedAt), entity.ApprovedAt);
        DomainHydrator.Set(doc, nameof(OpeningBalanceDocument.ApprovedByUserId), entity.ApprovedByUserId);
        DomainHydrator.Set(doc, nameof(OpeningBalanceDocument.ApprovalNotes), entity.ApprovalNotes);
        DomainHydrator.Set(doc, nameof(OpeningBalanceDocument.RejectionReason), entity.RejectionReason);
        DomainHydrator.Set(doc, nameof(OpeningBalanceDocument.PostedAt), entity.PostedAt);
        DomainHydrator.Set(doc, nameof(OpeningBalanceDocument.PostedByUserId), entity.PostedByUserId);
        DomainHydrator.Set(doc, nameof(OpeningBalanceDocument.LockedAt), entity.LockedAt);
        DomainHydrator.Set(doc, nameof(OpeningBalanceDocument.ArchivedAt), entity.ArchivedAt);

        var lines = entity.Lines.OrderBy(l => l.LineNumber).Select(ToLineAggregate).ToList();
        doc.HydrateLines(lines);
        return doc;
    }

    public static OpeningBalanceLine ToLineAggregate(OpeningBalanceLineEntity entity)
    {
        var line = OpeningBalanceLine.Create(
            entity.Debit, entity.Credit,
            entity.PartyId, entity.PartyName,
            entity.AccountId, entity.AccountName,
            entity.WarehouseId, entity.WarehouseName,
            entity.FabricItemId, entity.FabricColorId,
            entity.ItemName, entity.ColorName, entity.BatchNumber, entity.LocationCode,
            entity.RollCount, entity.Quantity, entity.UnitCost,
            entity.BankName, entity.BankAccountNumber, entity.InvestmentScope,
            entity.Reference, entity.Description, entity.Notes);
        DomainHydrator.Set(line, nameof(OpeningBalanceLine.Id), entity.Id);
        DomainHydrator.Set(line, nameof(OpeningBalanceLine.DocumentId), entity.DocumentId);
        DomainHydrator.Set(line, nameof(OpeningBalanceLine.LineNumber), entity.LineNumber);
        return line;
    }

    public static OpeningBalanceDocumentEntity ToEntity(OpeningBalanceDocument doc, Guid? userId)
    {
        var entity = new OpeningBalanceDocumentEntity
        {
            Id = doc.Id,
            CompanyId = doc.CompanyId,
            BranchId = doc.BranchId,
            Number = doc.Number,
            Type = (int)doc.Type,
            Status = (int)doc.Status,
            Source = (int)doc.Source,
            OpeningDate = doc.OpeningDate,
            CurrencyCode = doc.CurrencyCode,
            ExchangeRate = doc.ExchangeRate,
            Reference = doc.Reference,
            Description = doc.Description,
            Notes = doc.Notes,
            TotalDebit = doc.TotalDebit,
            TotalCredit = doc.TotalCredit,
            TotalBaseAmount = doc.TotalBaseAmount,
            JournalEntryNumber = doc.JournalEntryNumber,
            CreatedAt = doc.CreatedAt,
            CreatedByUserId = doc.CreatedByUserId,
            UpdatedAt = doc.UpdatedAt,
            ApprovedAt = doc.ApprovedAt,
            ApprovedByUserId = doc.ApprovedByUserId,
            ApprovalNotes = doc.ApprovalNotes,
            RejectionReason = doc.RejectionReason,
            PostedAt = doc.PostedAt,
            PostedByUserId = doc.PostedByUserId,
            LockedAt = doc.LockedAt,
            ArchivedAt = doc.ArchivedAt,
            IsActive = true,
            IsArchived = doc.Status == OpeningBalanceStatus.Archived
        };

        entity.Lines = doc.Lines.Select(l => new OpeningBalanceLineEntity
        {
            Id = l.Id,
            DocumentId = doc.Id,
            LineNumber = l.LineNumber,
            PartyId = l.PartyId,
            PartyName = l.PartyName,
            AccountId = l.AccountId,
            AccountName = l.AccountName,
            WarehouseId = l.WarehouseId,
            WarehouseName = l.WarehouseName,
            FabricItemId = l.FabricItemId,
            FabricColorId = l.FabricColorId,
            ItemName = l.ItemName,
            ColorName = l.ColorName,
            BatchNumber = l.BatchNumber,
            LocationCode = l.LocationCode,
            RollCount = l.RollCount,
            Quantity = l.Quantity,
            UnitCost = l.UnitCost,
            BankName = l.BankName,
            BankAccountNumber = l.BankAccountNumber,
            InvestmentScope = l.InvestmentScope,
            Debit = l.Debit,
            Credit = l.Credit,
            Reference = l.Reference,
            Description = l.Description,
            Notes = l.Notes
        }).ToList();

        if (entity.UpdatedAt is null && userId is not null)
            entity.UpdatedByUserId = userId;

        return entity;
    }

    public static OpeningBalanceEvent ToEventAggregate(OpeningBalanceEventEntity entity) =>
        OpeningBalanceEvent.Record(
            entity.DocumentId, entity.UserId, entity.UserName, entity.Action,
            entity.OldValues, entity.NewValues, entity.Notes, entity.MachineName, entity.IpAddress);

    public static string BuildLineKey(OpeningBalanceType type, OpeningBalanceLine line) => type switch
    {
        OpeningBalanceType.CustomerReceivable or OpeningBalanceType.SupplierPayable or OpeningBalanceType.Capital
            => line.PartyId?.ToString() ?? line.PartyName ?? "",
        OpeningBalanceType.Cash or OpeningBalanceType.Bank or OpeningBalanceType.GeneralLedger
            => line.AccountId?.ToString() ?? line.BankAccountNumber ?? line.AccountName ?? "",
        OpeningBalanceType.OpeningStock
            => $"{line.WarehouseId}:{line.FabricItemId}:{line.FabricColorId}:{line.BatchNumber}",
        _ => $"{line.PartyId}:{line.AccountId}:{line.Debit}:{line.Credit}"
    };
}
