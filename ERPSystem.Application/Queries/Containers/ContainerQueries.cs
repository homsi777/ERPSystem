using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.Queries.Containers;

public sealed class GetChinaContainerListQuery
{
    public Guid CompanyId { get; init; }
    public Guid? BranchId { get; init; }
    public ChinaContainerStatus? Status { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}

public sealed class GetContainerOperationsCenterQuery
{
    public Guid ContainerId { get; init; }
}

public sealed class ParseContainerExcelQuery
{
    public Guid CompanyId { get; init; }
    public string FileName { get; init; } = "";
    public byte[] FileContent { get; init; } = [];
}

public sealed class ParseChinaInvoiceExcelQuery
{
    public string FileName { get; init; } = "";
    public byte[] FileContent { get; init; } = [];
}

public sealed class ParseChinaPackingSummaryExcelQuery
{
    public string FileName { get; init; } = "";
    public byte[] FileContent { get; init; } = [];
}
