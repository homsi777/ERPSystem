namespace ERPSystem.Application.Abstractions.Services;

public interface IDocumentPreviewService
{
    Task<byte[]?> RenderPreviewAsync(string templateCode, object model, CancellationToken cancellationToken = default);
}
