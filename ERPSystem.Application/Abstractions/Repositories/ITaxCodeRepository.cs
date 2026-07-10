using ERPSystem.Domain.Entities.Sales;

namespace ERPSystem.Application.Abstractions.Repositories;

public interface ITaxCodeRepository
{
    Task<TaxCode?> GetByIdAsync(Guid companyId, Guid taxCodeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaxCode>> GetActiveForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);
}

public interface ISalesPostingProfileRepository
{
    Task<SalesPostingProfile?> GetForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);
}

public sealed class SalesPostingProfile
{
    public Guid CompanyId { get; init; }
    public Guid AccountsReceivableAccountId { get; init; }
    public Guid SalesRevenueAccountId { get; init; }
    public Guid SalesDiscountAccountId { get; init; }
    public Guid? VatPayableAccountId { get; init; }
    public Guid InventoryAccountId { get; init; }
    public Guid CogsAccountId { get; init; }
    public Guid? RoundingAccountId { get; init; }
}
