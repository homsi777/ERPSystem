using ERPSystem.Application.Commands.Finance;
using ERPSystem.Application.DTOs.Finance;
using ERPSystem.Application.Results;
using ERPSystem.Domain.Entities.Finance;

namespace ERPSystem.Application.Mapping;

public static class OpeningBalanceMappers
{
    public static OpeningBalanceListDto ToListDto(OpeningBalanceDocument doc) => new()
    {
        Id = doc.Id,
        Number = doc.Number,
        Type = doc.Type,
        TypeDisplay = OpeningBalanceDisplay.TypeName(doc.Type),
        Status = doc.Status,
        StatusDisplay = OpeningBalanceDisplay.StatusName(doc.Status),
        Source = doc.Source,
        SourceDisplay = OpeningBalanceDisplay.SourceName(doc.Source),
        OpeningDate = doc.OpeningDate,
        CurrencyCode = doc.CurrencyCode,
        ExchangeRate = doc.ExchangeRate,
        TotalDebit = doc.TotalDebit,
        TotalCredit = doc.TotalCredit,
        TotalBaseAmount = doc.TotalBaseAmount,
        LineCount = doc.Lines.Count,
        Reference = doc.Reference,
        Description = doc.Description,
        JournalEntryNumber = doc.JournalEntryNumber,
        CreatedAt = doc.CreatedAt,
        UpdatedAt = doc.UpdatedAt,
        PostedAt = doc.PostedAt
    };

    public static OpeningBalanceLineDto ToLineDto(OpeningBalanceLine line) => new()
    {
        Id = line.Id,
        LineNumber = line.LineNumber,
        PartyId = line.PartyId,
        PartyName = line.PartyName,
        AccountId = line.AccountId,
        AccountName = line.AccountName,
        WarehouseId = line.WarehouseId,
        WarehouseName = line.WarehouseName,
        ItemName = line.ItemName,
        ColorName = line.ColorName,
        BatchNumber = line.BatchNumber,
        LocationCode = line.LocationCode,
        RollCount = line.RollCount,
        Quantity = line.Quantity,
        UnitCost = line.UnitCost,
        BankName = line.BankName,
        BankAccountNumber = line.BankAccountNumber,
        InvestmentScope = line.InvestmentScope,
        Debit = line.Debit,
        Credit = line.Credit,
        Amount = line.Amount,
        Reference = line.Reference,
        Description = line.Description,
        Notes = line.Notes
    };

    public static OpeningBalanceEventDto ToEventDto(OpeningBalanceEvent e) => new()
    {
        OccurredAt = e.OccurredAt,
        UserName = e.UserName,
        Action = e.Action,
        OldValues = e.OldValues,
        NewValues = e.NewValues,
        Notes = e.Notes,
        MachineName = e.MachineName,
        IpAddress = e.IpAddress
    };

    public static OpeningBalanceLine ToDomainLine(OpeningBalanceLineInput input) =>
        OpeningBalanceLine.Create(
            input.Debit, input.Credit,
            input.PartyId, input.PartyName,
            input.AccountId, input.AccountName,
            input.WarehouseId, input.WarehouseName,
            input.ItemName, input.ColorName, input.BatchNumber, input.LocationCode,
            input.RollCount, input.Quantity, input.UnitCost,
            input.BankName, input.BankAccountNumber, input.InvestmentScope,
            input.Reference, input.Description, input.Notes);
}
