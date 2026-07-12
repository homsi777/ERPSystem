using ERPSystem.Application.Documents;
using ERPSystem.Application.DTOs.Suppliers;
using ERPSystem.Services.Documents;
using System.IO;

namespace ERPSystem.Services.Suppliers;

public static class SupplierStatementDocumentService
{
    private static PartyStatementPdfGenerator? _generator;

    private static PartyStatementPdfGenerator Generator =>
        _generator ??= PartyStatementPdfGenerator.FromContentRoot(AppContext.BaseDirectory);

    public static void ShowPreview(
        SupplierStatementDto statement,
        DateTime? from,
        DateTime? to,
        bool exportPdf)
    {
        ArgumentNullException.ThrowIfNull(statement);
        var pdfBytes = Generator.Generate(statement, from, to);
        var safeName = string.Join("_", statement.SupplierName.Split(Path.GetInvalidFileNameChars()));

        if (exportPdf)
        {
            PdfPreviewWindow.SaveAndOpenFromBytes(pdfBytes, $"كشف حساب مورد - {safeName} - {DateTime.Now:yyyy-MM-dd}.pdf");
            return;
        }

        PdfPreviewWindow.ShowFromBytes(pdfBytes, $"كشف حساب — {statement.SupplierName}");
    }
}
