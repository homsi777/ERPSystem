using ERPSystem.Domain.Entities.Finance;

namespace ERPSystem.Application.Commands.Finance;

/// <summary>Raw line input shared by manual entry and Excel import.</summary>
public sealed class OpeningBalanceLineInput
{
    public Guid? PartyId { get; init; }
    public string? PartyName { get; init; }
    public Guid? AccountId { get; init; }
    public string? AccountName { get; init; }
    public Guid? WarehouseId { get; init; }
    public string? WarehouseName { get; init; }
    public Guid? FabricItemId { get; init; }
    public Guid? FabricColorId { get; init; }
    public string? ItemCode { get; init; }
    public string? ItemName { get; init; }
    public string? ColorName { get; init; }
    public string? BatchNumber { get; init; }
    public string? LocationCode { get; init; }
    public string? ContainerNumber { get; init; }
    public decimal? RollCount { get; init; }
    public decimal? Quantity { get; init; }
    public decimal? UnitCost { get; init; }
    public string? BankName { get; init; }
    public string? BankAccountNumber { get; init; }
    public string? InvestmentScope { get; init; }
    public decimal Debit { get; init; }
    public decimal Credit { get; init; }
    public string? Reference { get; init; }
    public string? Description { get; init; }
    public string? Notes { get; init; }
}

public sealed class CreateOpeningBalanceCommand
{
    public OpeningBalanceType Type { get; init; }
    public OpeningBalanceSource Source { get; init; } = OpeningBalanceSource.Manual;
    public DateTime OpeningDate { get; init; } = DateTime.UtcNow;
    public string CurrencyCode { get; init; } = "USD";
    public decimal ExchangeRate { get; init; } = 1m;
    public string? Reference { get; init; }
    public string? Description { get; init; }
    public string? Notes { get; init; }
    public IReadOnlyList<OpeningBalanceLineInput> Lines { get; init; } = [];
    /// <summary>Immediately submit for approval after creation.</summary>
    public bool SubmitForApproval { get; init; }
}

public sealed class UpdateOpeningBalanceCommand
{
    public Guid DocumentId { get; init; }
    public DateTime OpeningDate { get; init; }
    public string CurrencyCode { get; init; } = "USD";
    public decimal ExchangeRate { get; init; } = 1m;
    public string? Reference { get; init; }
    public string? Description { get; init; }
    public string? Notes { get; init; }
    public IReadOnlyList<OpeningBalanceLineInput> Lines { get; init; } = [];
}

public sealed class SubmitOpeningBalanceCommand
{
    public Guid DocumentId { get; init; }
}

public sealed class ApproveOpeningBalanceCommand
{
    public Guid DocumentId { get; init; }
    public string? Notes { get; init; }
}

public sealed class RejectOpeningBalanceCommand
{
    public Guid DocumentId { get; init; }
    public string Reason { get; init; } = "";
}

public sealed class PostOpeningBalanceCommand
{
    public Guid DocumentId { get; init; }
    /// <summary>Lock the document immediately after successful posting.</summary>
    public bool LockAfterPost { get; init; } = true;
}

public sealed class ArchiveOpeningBalanceCommand
{
    public Guid DocumentId { get; init; }
}

public sealed class DuplicateOpeningBalanceCommand
{
    public Guid DocumentId { get; init; }
}

public sealed class ValidateOpeningBalanceCommand
{
    public OpeningBalanceType Type { get; init; }
    public string CurrencyCode { get; init; } = "USD";
    public decimal ExchangeRate { get; init; } = 1m;
    public DateTime OpeningDate { get; init; } = DateTime.UtcNow;
    public IReadOnlyList<OpeningBalanceLineInput> Lines { get; init; } = [];
    /// <summary>Excluded from duplicate detection (when validating an edit).</summary>
    public Guid? ExcludeDocumentId { get; init; }
}

/// <summary>Quick post from legacy customer/supplier opening balance screens — routes through the unified engine.</summary>
public sealed class PostPartyOpeningBalanceCommand
{
    public OpeningBalanceType Type { get; init; }
    public Guid PartyId { get; init; }
    public string? PartyName { get; init; }
    public decimal Amount { get; init; }
    public DateTime OpeningDate { get; init; }
    public string? ReferenceNote { get; init; }
}

public sealed class ImportOpeningBalanceExcelCommand
{
    public OpeningBalanceType Type { get; init; }
    public string FileName { get; init; } = "";
    public byte[] Content { get; init; } = [];
    public DateTime OpeningDate { get; init; } = DateTime.UtcNow;
    public string CurrencyCode { get; init; } = "USD";
    public decimal ExchangeRate { get; init; } = 1m;
    public string? Reference { get; init; }
    /// <summary>Validate only — do not persist anything.</summary>
    public bool PreviewOnly { get; init; }
    /// <summary>Skip duplicate/warning rows instead of failing the import.</summary>
    public bool SkipInvalidRows { get; init; } = true;
}
