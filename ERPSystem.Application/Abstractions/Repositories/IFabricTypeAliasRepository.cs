using ERPSystem.Application.DTOs.Containers;

namespace ERPSystem.Application.Abstractions.Repositories;

public interface IFabricTypeAliasRepository
{
    Task<IReadOnlyList<FabricTypeAliasDto>> GetBySupplierAsync(
        Guid companyId,
        Guid supplierId,
        CancellationToken cancellationToken = default);

    Task UpsertAsync(
        Guid companyId,
        Guid supplierId,
        Guid fabricItemId,
        Guid fabricColorId,
        string dplMatchKey,
        string invoiceDescriptionMatchKey,
        string invoiceDescription,
        CancellationToken cancellationToken = default);
}
