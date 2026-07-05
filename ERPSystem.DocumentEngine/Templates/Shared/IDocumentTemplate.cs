using ERPSystem.DocumentEngine.Models;
using ERPSystem.DocumentEngine.Services;

namespace ERPSystem.DocumentEngine.Templates.Shared;

/// <summary>
/// A template produces ONLY the inner body of a document. The company header,
/// footer, watermark and page shell are supplied by <see cref="DocumentShell"/>,
/// guaranteeing a single shared structure across every template and render mode.
/// </summary>
public interface IDocumentTemplate
{
    DocumentType Type { get; }

    /// <summary>Renders the document body (everything inside <c>.doc-body</c>).</summary>
    string RenderBody(DocumentModel model, RenderContext ctx);
}
