using Microsoft.Win32;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System.Diagnostics;
using System.IO;
using WpfWindow = System.Windows.Window;
using WpfGrid = System.Windows.Controls.Grid;
using WpfRowDefinition = System.Windows.Controls.RowDefinition;
using WpfGridLength = System.Windows.GridLength;
using WpfGridUnitType = System.Windows.GridUnitType;
using WpfStackPanel = System.Windows.Controls.StackPanel;
using WpfTextBlock = System.Windows.Controls.TextBlock;
using WpfButton = System.Windows.Controls.Button;
using WpfOrientation = System.Windows.Controls.Orientation;
using WpfThickness = System.Windows.Thickness;
using WpfHAlign = System.Windows.HorizontalAlignment;
using WpfVAlign = System.Windows.VerticalAlignment;
using WpfTextAlign = System.Windows.TextAlignment;
using WpfFlowDirection = System.Windows.FlowDirection;
using WpfMessageBox = System.Windows.MessageBox;
using WpfMessageBoxButton = System.Windows.MessageBoxButton;
using WpfMessageBoxImage = System.Windows.MessageBoxImage;
using WpfFontWeights = System.Windows.FontWeights;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfStartLocation = System.Windows.WindowStartupLocation;

namespace ERPSystem.Services.Documents;

/// <summary>
/// Shared "generated → open/print/close" preview window for desktop QuestPDF documents.
/// Extracted from the sales invoice preview flow so every new print service (vouchers, reports)
/// reuses the same window instead of re-implementing it.
/// </summary>
public static class PdfPreviewWindow
{
    public static void Show(IDocument document, string title)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"erp-preview-{Guid.NewGuid():N}.pdf");
        document.GeneratePdf(tempPath);

        var win = new WpfWindow
        {
            Title = title,
            Width = 640,
            Height = 380,
            WindowStartupLocation = WpfStartLocation.CenterOwner,
            Owner = System.Windows.Application.Current?.MainWindow,
            FlowDirection = WpfFlowDirection.RightToLeft,
            Background = WpfBrushes.WhiteSmoke
        };

        var grid = new WpfGrid();
        grid.RowDefinitions.Add(new WpfRowDefinition { Height = new WpfGridLength(1, WpfGridUnitType.Star) });

        var host = new WpfStackPanel
        {
            Margin = new WpfThickness(24),
            VerticalAlignment = WpfVAlign.Center,
            HorizontalAlignment = WpfHAlign.Center
        };
        host.Children.Add(new WpfTextBlock
        {
            Text = title,
            FontSize = 18,
            FontWeight = WpfFontWeights.SemiBold,
            HorizontalAlignment = WpfHAlign.Center,
            Margin = new WpfThickness(0, 0, 0, 16)
        });
        host.Children.Add(new WpfTextBlock
        {
            Text = "تم إنشاء المستند وحُفظ مؤقتاً. اضغط «فتح» لعرضه في القارئ الافتراضي.",
            FontSize = 12,
            HorizontalAlignment = WpfHAlign.Center,
            TextAlignment = WpfTextAlign.Center,
            Margin = new WpfThickness(0, 0, 0, 12)
        });
        host.Children.Add(new WpfTextBlock
        {
            Text = tempPath,
            FontSize = 10,
            HorizontalAlignment = WpfHAlign.Center,
            Foreground = WpfBrushes.Gray,
            Margin = new WpfThickness(0, 0, 0, 20)
        });

        var btnRow = new WpfStackPanel { Orientation = WpfOrientation.Horizontal, HorizontalAlignment = WpfHAlign.Center };
        var btnOpen = new WpfButton { Content = "فتح المستند", Width = 130, Height = 34, Margin = new WpfThickness(0, 0, 8, 0) };
        btnOpen.Click += (_, _) =>
        {
            try { Process.Start(new ProcessStartInfo(tempPath) { UseShellExecute = true }); }
            catch (Exception ex) { WpfMessageBox.Show(ex.Message, "خطأ", WpfMessageBoxButton.OK, WpfMessageBoxImage.Error); }
        };
        var btnPrint = new WpfButton { Content = "طباعة", Width = 100, Height = 34, Margin = new WpfThickness(0, 0, 8, 0) };
        btnPrint.Click += (_, _) => Print(tempPath);
        var btnClose = new WpfButton { Content = "إغلاق", Width = 100, Height = 34, IsCancel = true };
        btnClose.Click += (_, _) => win.Close();
        btnRow.Children.Add(btnOpen);
        btnRow.Children.Add(btnPrint);
        btnRow.Children.Add(btnClose);
        host.Children.Add(btnRow);

        WpfGrid.SetRow(host, 0);
        grid.Children.Add(host);

        win.Content = grid;
        win.ShowDialog();
    }

    public static void SaveAndOpen(IDocument document, string suggestedFileName)
    {
        var dlg = new SaveFileDialog
        {
            Filter = "PDF Document (*.pdf)|*.pdf",
            FileName = Sanitize(suggestedFileName)
        };
        if (dlg.ShowDialog() != true) return;
        document.GeneratePdf(dlg.FileName);
        try { Process.Start(new ProcessStartInfo(dlg.FileName) { UseShellExecute = true }); }
        catch { /* opening is optional */ }
    }

    private static void Print(string path)
    {
        try
        {
            var psi = new ProcessStartInfo(path)
            {
                Verb = "print",
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            Process.Start(psi);
            MockInteractionService.ShowInfo("تم إرسال المستند إلى الطابعة الافتراضية.", "طباعة");
        }
        catch (Exception ex)
        {
            WpfMessageBox.Show(
                "لم تعثر Windows على طابعة قادرة على طباعة PDF مباشرة. استخدم زر «فتح» ثم اطبع من داخل القارئ.\n\n" + ex.Message,
                "طباعة",
                WpfMessageBoxButton.OK,
                WpfMessageBoxImage.Warning);
        }
    }

    private static string Sanitize(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = name.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (Array.IndexOf(invalid, chars[i]) >= 0)
                chars[i] = '-';
        }
        return new string(chars);
    }
}
