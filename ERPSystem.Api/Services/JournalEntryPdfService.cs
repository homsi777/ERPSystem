using ERPSystem.Application.Documents;
using ERPSystem.Application.DTOs.Accounting;

namespace ERPSystem.Api.Services;

public sealed class JournalEntryPdfService
{
    private readonly JournalEntryPdfGenerator _generator;

    public JournalEntryPdfService(IWebHostEnvironment environment) =>
        _generator = JournalEntryPdfGenerator.FromContentRoot(environment.ContentRootPath);

    public byte[] Generate(JournalEntryDetailsDto entry) => _generator.Generate(entry);
}
