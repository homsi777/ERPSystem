using ClosedXML.Excel;
using Microsoft.Win32;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Data;

namespace ERPSystem.Services.Documents;

/// <summary>
/// Generic Excel exporter for any list page. Reads the visible columns and rows
/// straight off the bound <see cref="DataGrid"/> so every list "تصدير" button
/// produces a real .xlsx of exactly what the user sees.
/// </summary>
public static class ListExportService
{
    public static void ExportGrid(DataGrid grid, string moduleName)
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

        var columns = grid.Columns
            .Where(c => c.Visibility == System.Windows.Visibility.Visible)
            .OrderBy(c => c.DisplayIndex)
            .Select(c => (Header: c.Header?.ToString() ?? "", Path: ResolvePath(c)))
            .Where(c => !string.IsNullOrEmpty(c.Path))
            .ToList();

        if (columns.Count == 0)
        {
            MockInteractionService.ShowWarning("تعذر تحديد أعمدة قابلة للتصدير.", "تصدير");
            return;
        }

        var safeName = string.Concat((moduleName ?? "Export").Split(Path.GetInvalidFileNameChars()));
        var dlg = new SaveFileDialog
        {
            Filter = "Excel Workbook (*.xlsx)|*.xlsx",
            FileName = $"{safeName}_{DateTime.Now:yyyy-MM-dd}.xlsx"
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add(safeName.Length > 28 ? safeName[..28] : safeName);
            ws.RightToLeft = true;

            for (var c = 0; c < columns.Count; c++)
            {
                var cell = ws.Cell(1, c + 1);
                cell.Value = columns[c].Header;
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#E9EEF5");
            }

            for (var r = 0; r < items.Count; r++)
            {
                for (var c = 0; c < columns.Count; c++)
                {
                    var raw = GetValue(items[r], columns[c].Path);
                    SetCell(ws.Cell(r + 2, c + 1), raw);
                }
            }

            ws.Columns().AdjustToContents();
            ws.SheetView.FreezeRows(1);
            wb.SaveAs(dlg.FileName);

            try { Process.Start(new ProcessStartInfo(dlg.FileName) { UseShellExecute = true }); }
            catch { /* opening is optional */ }
        }
        catch (Exception ex)
        {
            MockInteractionService.ShowWarning("تعذر إنشاء ملف Excel:\n" + ex.Message, "تصدير");
        }
    }

    /// <summary>Export an explicit record collection (for card-based lists without a DataGrid).</summary>
    public static void ExportRecords<T>(IEnumerable<T> items, string moduleName, params (string Header, Func<T, object?> Value)[] columns)
    {
        var list = items?.ToList() ?? new List<T>();
        if (list.Count == 0)
        {
            MockInteractionService.ShowWarning("لا توجد بيانات للتصدير.", "تصدير");
            return;
        }

        var safeName = string.Concat((moduleName ?? "Export").Split(Path.GetInvalidFileNameChars()));
        var dlg = new SaveFileDialog
        {
            Filter = "Excel Workbook (*.xlsx)|*.xlsx",
            FileName = $"{safeName}_{DateTime.Now:yyyy-MM-dd}.xlsx"
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add(safeName.Length > 28 ? safeName[..28] : safeName);
            ws.RightToLeft = true;

            for (var c = 0; c < columns.Length; c++)
            {
                var cell = ws.Cell(1, c + 1);
                cell.Value = columns[c].Header;
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#E9EEF5");
            }

            for (var r = 0; r < list.Count; r++)
                for (var c = 0; c < columns.Length; c++)
                    SetCell(ws.Cell(r + 2, c + 1), columns[c].Value(list[r]));

            ws.Columns().AdjustToContents();
            ws.SheetView.FreezeRows(1);
            wb.SaveAs(dlg.FileName);
            try { Process.Start(new ProcessStartInfo(dlg.FileName) { UseShellExecute = true }); }
            catch { /* opening is optional */ }
        }
        catch (Exception ex)
        {
            MockInteractionService.ShowWarning("تعذر إنشاء ملف Excel:\n" + ex.Message, "تصدير");
        }
    }

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

    private static void SetCell(IXLCell cell, object? value)
    {
        switch (value)
        {
            case null:
                cell.Value = "";
                break;
            case DateTime dt:
                cell.Value = dt;
                cell.Style.DateFormat.Format = "yyyy-mm-dd";
                break;
            case decimal dec:
                cell.Value = dec;
                break;
            case double or float or int or long:
                cell.Value = Convert.ToDouble(value);
                break;
            case bool bl:
                cell.Value = bl ? "نعم" : "لا";
                break;
            default:
                cell.Value = value.ToString();
                break;
        }
    }
}
