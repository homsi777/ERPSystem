using ERPSystem.Application.DTOs.Reports;
using ERPSystem.Helpers;
using ERPSystem.Services.Documents;
using System.Collections;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Data;

namespace ERPSystem.Services.Reports;

/// <summary>Export any report <see cref="DataGrid"/> to PDF/print/Excel via shared generators.</summary>
public static class GridReportExportService
{
    public static void Export(
        DataGrid grid,
        string title,
        string mode,
        DateTime? from = null,
        DateTime? to = null,
        IEnumerable<ModuleReportKpiDto>? kpis = null)
    {
        if (grid?.ItemsSource is not IEnumerable rows)
        {
            MockInteractionService.ShowWarning("لا توجد بيانات للتصدير.", "تصدير");
            return;
        }

        var items = rows.Cast<object>().ToList();
        if (items.Count == 0)
        {
            MockInteractionService.ShowWarning("لا توجد بيانات للتصدير.", "تصدير");
            return;
        }

        if (mode == "excel")
        {
            ListExportService.ExportGrid(grid, title);
            return;
        }

        var report = BuildReport(grid, title, items, from, to, kpis);
        ModuleReportDocumentService.ShowPreview(report, exportPdf: mode == "pdf");
    }

    public static ModuleReportResultDto BuildReport(
        DataGrid grid,
        string title,
        IReadOnlyList<object>? items = null,
        DateTime? from = null,
        DateTime? to = null,
        IEnumerable<ModuleReportKpiDto>? kpis = null)
    {
        items ??= grid.ItemsSource is IEnumerable rows
            ? rows.Cast<object>().ToList()
            : [];

        var columnDefs = grid.Columns
            .Where(c => c.Visibility == System.Windows.Visibility.Visible)
            .OrderBy(c => c.DisplayIndex)
            .Select(c => (Header: c.Header?.ToString() ?? "", Path: ResolvePath(c), IsStar: c.Width.IsStar))
            .Where(c => !string.IsNullOrEmpty(c.Path))
            .ToList();

        var columns = columnDefs.Select((c, i) => new ModuleReportColumnDto
        {
            Key = $"c{i}",
            HeaderAr = string.IsNullOrWhiteSpace(c.Header) ? c.Path : c.Header,
            IsStar = c.IsStar
        }).ToList();

        var reportRows = new List<Dictionary<string, object?>>();
        foreach (var item in items)
        {
            var cells = new Dictionary<string, object?>();
            for (var i = 0; i < columnDefs.Count; i++)
                cells[$"c{i}"] = GetValue(item, columnDefs[i].Path);
            reportRows.Add(cells);
        }

        return new ModuleReportResultDto
        {
            Title = title,
            GeneratedAt = DateTime.UtcNow,
            FromDate = from,
            ToDate = to,
            Columns = columns,
            Rows = reportRows,
            Kpis = kpis?.ToList() ?? []
        };
    }

    public static StackPanel CreateActionBar(string title, Action<string> onExport) =>
        ErpUxFactory.ActionToolbar(
            ("طباعة", false, () => onExport("print")),
            ("PDF", false, () => onExport("pdf")),
            ("Excel", false, () => onExport("excel")));

    private static string ResolvePath(DataGridColumn column) =>
        column is DataGridBoundColumn { Binding: Binding b } ? b.Path?.Path ?? "" : "";

    private static object? GetValue(object item, string path)
    {
        var current = item;
        foreach (var part in path.Split('.'))
        {
            if (current is null) return null;
            var prop = current.GetType().GetProperty(part,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop is null) return null;
            current = prop.GetValue(current);
        }
        return current;
    }
}
