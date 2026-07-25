using ERPSystem.Application.Documents;
using ERPSystem.Application.DTOs.Containers;

namespace ERPSystem.Api.Services;

/// <summary>API adapter over the branded China-container PDF generator.</summary>
public sealed class ChinaContainerPdfService
{
    private readonly ChinaContainerPdfGenerator _generator;

    public ChinaContainerPdfService(IWebHostEnvironment environment) =>
        _generator = ChinaContainerPdfGenerator.FromContentRoot(environment.ContentRootPath);

    public byte[] Generate(ContainerOperationsCenterDto operations) =>
        _generator.Generate(operations);
}
