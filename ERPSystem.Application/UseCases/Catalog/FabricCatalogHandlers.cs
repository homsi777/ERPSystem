using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Commands.Catalog;
using ERPSystem.Application.DTOs.Catalog;
using ERPSystem.Application.Queries.Catalog;
using ERPSystem.Application.Results;
using ERPSystem.Domain.Entities.Catalog;

namespace ERPSystem.Application.UseCases.Catalog;

public sealed class GetFabricCategoryListHandler(IFabricCatalogRepository repository)
    : IQueryHandler<GetFabricCategoryListQuery, ApplicationResult<IReadOnlyList<FabricCategoryListDto>>>
{
    public async Task<ApplicationResult<IReadOnlyList<FabricCategoryListDto>>> HandleAsync(
        GetFabricCategoryListQuery query, CancellationToken cancellationToken = default) =>
        ApplicationResult<IReadOnlyList<FabricCategoryListDto>>.Success(
            await repository.GetCategoryListAsync(query.CompanyId, cancellationToken));
}

public sealed class GetFabricItemListHandler(IFabricCatalogRepository repository)
    : IQueryHandler<GetFabricItemListQuery, ApplicationResult<IReadOnlyList<FabricItemListDto>>>
{
    public async Task<ApplicationResult<IReadOnlyList<FabricItemListDto>>> HandleAsync(
        GetFabricItemListQuery query, CancellationToken cancellationToken = default) =>
        ApplicationResult<IReadOnlyList<FabricItemListDto>>.Success(
            await repository.GetItemListAsync(query.CompanyId, query.CategoryId, query.Search, cancellationToken));
}

public sealed class GetFabricColorListHandler(IFabricCatalogRepository repository)
    : IQueryHandler<GetFabricColorListQuery, ApplicationResult<IReadOnlyList<FabricColorListDto>>>
{
    public async Task<ApplicationResult<IReadOnlyList<FabricColorListDto>>> HandleAsync(
        GetFabricColorListQuery query, CancellationToken cancellationToken = default) =>
        ApplicationResult<IReadOnlyList<FabricColorListDto>>.Success(
            await repository.GetColorListAsync(query.FabricItemId, cancellationToken));
}

public sealed class GetImportedFabricClassificationListHandler(IFabricCatalogRepository repository)
    : IQueryHandler<GetImportedFabricClassificationListQuery, ApplicationResult<IReadOnlyList<ImportedFabricClassificationDto>>>
{
    public async Task<ApplicationResult<IReadOnlyList<ImportedFabricClassificationDto>>> HandleAsync(
        GetImportedFabricClassificationListQuery query, CancellationToken cancellationToken = default) =>
        ApplicationResult<IReadOnlyList<ImportedFabricClassificationDto>>.Success(
            await repository.GetImportedClassificationsAsync(query.CompanyId, query.ContainerId, cancellationToken));
}

public sealed class GetImportedFabricContainerFiltersHandler(IFabricCatalogRepository repository)
    : IQueryHandler<GetImportedFabricContainerFiltersQuery, ApplicationResult<IReadOnlyList<ImportedFabricContainerFilterDto>>>
{
    public async Task<ApplicationResult<IReadOnlyList<ImportedFabricContainerFilterDto>>> HandleAsync(
        GetImportedFabricContainerFiltersQuery query, CancellationToken cancellationToken = default) =>
        ApplicationResult<IReadOnlyList<ImportedFabricContainerFilterDto>>.Success(
            await repository.GetImportedFabricContainerFiltersAsync(query.CompanyId, cancellationToken));
}

public sealed class CreateFabricCategoryHandler(
    IFabricCatalogRepository repository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CreateFabricCategoryCommand, ApplicationResult<Guid>>
{
    public async Task<ApplicationResult<Guid>> HandleAsync(
        CreateFabricCategoryCommand command, CancellationToken cancellationToken = default)
    {
        var code = command.Code.Trim();
        if (string.IsNullOrWhiteSpace(code))
            return ApplicationResult<Guid>.ValidationFailed(nameof(command.Code), "Category code is required.");
        if (string.IsNullOrWhiteSpace(command.NameAr))
            return ApplicationResult<Guid>.ValidationFailed(nameof(command.NameAr), "Category name is required.");
        if (await repository.CategoryCodeExistsAsync(command.CompanyId, code, cancellationToken: cancellationToken))
            return ApplicationResult<Guid>.ValidationFailed(nameof(command.Code), "Category code already exists.");

        var category = FabricCategory.Create(code, command.NameAr.Trim(), command.NameEn?.Trim() ?? "");
        await repository.AddCategoryAsync(category, command.CompanyId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ApplicationResult<Guid>.Success(category.Id);
    }
}

public sealed class UpdateFabricCategoryHandler(
    IFabricCatalogRepository repository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateFabricCategoryCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        UpdateFabricCategoryCommand command, CancellationToken cancellationToken = default)
    {
        var category = await repository.GetCategoryByIdAsync(command.CategoryId, cancellationToken);
        if (category is null) return ApplicationResult.NotFound("Category not found.");
        category.Update(command.NameAr.Trim(), command.NameEn?.Trim() ?? "");
        await repository.UpdateCategoryAsync(category, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }
}

public sealed class DeactivateFabricCategoryHandler(
    IFabricCatalogRepository repository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<DeactivateFabricCategoryCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        DeactivateFabricCategoryCommand command, CancellationToken cancellationToken = default)
    {
        var category = await repository.GetCategoryByIdAsync(command.CategoryId, cancellationToken);
        if (category is null) return ApplicationResult.NotFound("Category not found.");
        category.Deactivate();
        await repository.UpdateCategoryAsync(category, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }
}

public sealed class CreateFabricItemHandler(
    IFabricCatalogRepository repository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CreateFabricItemCommand, ApplicationResult<Guid>>
{
    public async Task<ApplicationResult<Guid>> HandleAsync(
        CreateFabricItemCommand command, CancellationToken cancellationToken = default)
    {
        var code = command.Code.Trim();
        if (string.IsNullOrWhiteSpace(code))
            return ApplicationResult<Guid>.ValidationFailed(nameof(command.Code), "Fabric code is required.");
        if (string.IsNullOrWhiteSpace(command.NameAr))
            return ApplicationResult<Guid>.ValidationFailed(nameof(command.NameAr), "Fabric name is required.");
        if (await repository.ItemCodeExistsAsync(command.CompanyId, code, cancellationToken: cancellationToken))
            return ApplicationResult<Guid>.ValidationFailed(nameof(command.Code), "Fabric code already exists.");

        var item = FabricItem.Create(command.CategoryId, code, command.NameAr.Trim(), command.NameEn?.Trim() ?? "");
        await repository.AddItemAsync(item, command.CompanyId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ApplicationResult<Guid>.Success(item.Id);
    }
}

public sealed class UpdateFabricItemHandler(
    IFabricCatalogRepository repository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateFabricItemCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        UpdateFabricItemCommand command, CancellationToken cancellationToken = default)
    {
        var item = await repository.GetItemByIdAsync(command.ItemId, cancellationToken);
        if (item is null) return ApplicationResult.NotFound("Fabric item not found.");
        item.Update(command.CategoryId, command.NameAr.Trim(), command.NameEn?.Trim() ?? "");
        await repository.UpdateItemAsync(item, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }
}

public sealed class DeactivateFabricItemHandler(
    IFabricCatalogRepository repository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<DeactivateFabricItemCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        DeactivateFabricItemCommand command, CancellationToken cancellationToken = default)
    {
        var item = await repository.GetItemByIdAsync(command.ItemId, cancellationToken);
        if (item is null) return ApplicationResult.NotFound("Fabric item not found.");
        item.Deactivate();
        await repository.UpdateItemAsync(item, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }
}

public sealed class CreateFabricColorHandler(
    IFabricCatalogRepository repository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CreateFabricColorCommand, ApplicationResult<Guid>>
{
    public async Task<ApplicationResult<Guid>> HandleAsync(
        CreateFabricColorCommand command, CancellationToken cancellationToken = default)
    {
        var code = command.Code.Trim();
        if (string.IsNullOrWhiteSpace(code))
            return ApplicationResult<Guid>.ValidationFailed(nameof(command.Code), "Color code is required.");
        if (string.IsNullOrWhiteSpace(command.NameAr))
            return ApplicationResult<Guid>.ValidationFailed(nameof(command.NameAr), "Color name is required.");

        var color = FabricColor.Create(command.FabricItemId, code, command.NameAr.Trim(), command.NameEn?.Trim() ?? "");
        await repository.AddColorAsync(color, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ApplicationResult<Guid>.Success(color.Id);
    }
}

public sealed class UpdateFabricColorHandler(
    IFabricCatalogRepository repository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateFabricColorCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        UpdateFabricColorCommand command, CancellationToken cancellationToken = default)
    {
        var color = await repository.GetColorByIdAsync(command.ColorId, cancellationToken);
        if (color is null) return ApplicationResult.NotFound("Color not found.");
        color.Update(command.NameAr.Trim(), command.NameEn?.Trim() ?? "");
        await repository.UpdateColorAsync(color, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }
}

public sealed class DeactivateFabricColorHandler(
    IFabricCatalogRepository repository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<DeactivateFabricColorCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        DeactivateFabricColorCommand command, CancellationToken cancellationToken = default)
    {
        var color = await repository.GetColorByIdAsync(command.ColorId, cancellationToken);
        if (color is null) return ApplicationResult.NotFound("Color not found.");
        color.Deactivate();
        await repository.UpdateColorAsync(color, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }
}
