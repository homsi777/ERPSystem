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
        PostedAt = doc.PostedAt,
        PrimaryPartyDisplay = FormatPrimaryParty(doc),
        DisplayNotes = doc.Notes ?? doc.Lines.FirstOrDefault()?.Notes ?? doc.Description,
        StockItemsSummary = FormatStockItemsSummary(doc),
        TotalRollCount = SumRollCount(doc)
    };

    private static string FormatStockItemsSummary(OpeningBalanceDocument doc)
    {
        if (doc.Type != OpeningBalanceType.OpeningStock || doc.Lines.Count == 0)
            return "—";

        var names = doc.Lines
            .Select(l => l.ItemName?.Trim())
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (names.Count == 0)
            return "—";
        if (names.Count == 1)
            return names[0]!;
        return string.Join("، ", names.Take(2)) + (names.Count > 2 ? $" (+{names.Count - 2})" : "");
    }

    private static int SumRollCount(OpeningBalanceDocument doc) =>
        doc.Type == OpeningBalanceType.OpeningStock
            ? (int)doc.Lines.Sum(l => l.RollCount ?? 0m)
            : 0;

    private static string FormatPrimaryParty(OpeningBalanceDocument doc)
    {
        if (doc.Lines.Count == 0)
            return "—";
        if (doc.Lines.Count == 1)
            return doc.Lines[0].PartyName ?? "—";
        var first = doc.Lines[0].PartyName ?? "—";
        return $"{first} (+{doc.Lines.Count - 1})";
    }

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
        FabricItemId = line.FabricItemId,
        FabricColorId = line.FabricColorId,
        ItemCode = line.ItemCode,
        ItemName = line.ItemName,
        ColorName = line.ColorName,
        BatchNumber = line.BatchNumber,
        LocationCode = line.LocationCode,
        ContainerNumber = line.ContainerNumber,
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
            input.FabricItemId, input.FabricColorId,
            input.ItemCode, input.ItemName, input.ColorName, input.BatchNumber, input.LocationCode,
            input.ContainerNumber, input.RollCount, input.Quantity, input.UnitCost,
            input.BankName, input.BankAccountNumber, input.InvestmentScope,
            input.Reference, input.Description, input.Notes);
}
