using ERPSystem.DocumentEngine.Models;
using ERPSystem.DocumentEngine.Templates.Shared;

namespace ERPSystem.DocumentEngine.Templates.ContainerReport;

/// <summary>Container / Import Report — shipment costs, landed cost, items.</summary>
public sealed class ContainerReportTemplate : BaseDocumentTemplate
{
    public override DocumentType Type => DocumentType.ContainerReport;
}
