using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.China;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WpfApplication = System.Windows.Application;

namespace ERPSystem.Controls.China;

public sealed class ContainerDocumentationControl : UserControl
{
    private readonly Guid _containerId;
    private readonly string _containerNumber;
    private readonly DataGrid _grid = new() { AutoGenerateColumns = false, IsReadOnly = true };
    private readonly TextBlock _folderPath = new()
    {
        FontSize = 11,
        Foreground = Brushes.Gray,
        TextWrapping = TextWrapping.Wrap,
        Margin = new Thickness(0, 8, 0, 0)
    };
    private readonly TextBlock _status = new()
    {
        FontSize = 12,
        Margin = new Thickness(0, 10, 0, 0),
        Foreground = (Brush)WpfApplication.Current.Resources["TextSecondaryBrush"]!
    };

    public ContainerDocumentationControl(Guid containerId, string containerNumber)
    {
        _containerId = containerId;
        _containerNumber = containerNumber;

        var root = new StackPanel { Margin = new Thickness(4) };
        root.Children.Add(ErpUxFactory.InfoBanner(
            "ارفع ملفات PDF وWord وصور التخليص الجمركي — تُحفظ في مجلد «توثيق حاويات» للرجوع إليها لاحقاً.",
            "info"));

        var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 12, 0, 8) };
        var uploadBtn = new Button
        {
            Content = "رفع ملفات توثيق",
            Style = (Style)WpfApplication.Current.Resources["PrimaryButtonStyle"]!,
            Height = 34,
            Padding = new Thickness(14, 0, 14, 0)
        };
        uploadBtn.Click += async (_, _) => await UploadAsync();

        var folderBtn = new Button
        {
            Content = "فتح مجلد الحاوية",
            Style = (Style)WpfApplication.Current.Resources["SecondaryButtonStyle"]!,
            Height = 34,
            Padding = new Thickness(14, 0, 14, 0),
            Margin = new Thickness(8, 0, 0, 0)
        };
        folderBtn.Click += (_, _) =>
        {
            try
            {
                ContainerDocumentationService.Instance.OpenContainerFolder(_containerId, _containerNumber);
            }
            catch (Exception ex)
            {
                MockInteractionService.ShowWarning(ex.Message, "فتح المجلد");
            }
        };

        actions.Children.Add(uploadBtn);
        actions.Children.Add(folderBtn);
        root.Children.Add(actions);

        _folderPath.Text = $"مسار الحفظ: {ContainerDocumentationService.Instance.GetContainerDirectory(_containerId, _containerNumber)}";
        root.Children.Add(_folderPath);

        ErpUiFactory.AddGridColumn(_grid, "اسم الملف", nameof(ContainerDocumentationFileDto.OriginalFileName), "*", null);
        ErpUiFactory.AddGridColumn(_grid, "الحجم", nameof(ContainerDocumentationFileDto.SizeDisplay), 90, null);
        ErpUiFactory.AddGridColumn(_grid, "تاريخ الرفع", nameof(ContainerDocumentationFileDto.UploadedAt), 130, "yyyy/MM/dd HH:mm");
        _grid.MaxHeight = 320;
        _grid.MouseDoubleClick += (_, _) => OpenSelectedFile();
        root.Children.Add(_grid);

        root.Children.Add(new TextBlock
        {
            Text = "انقر مرتين على الملف لفتحه",
            FontSize = 10,
            Foreground = Brushes.Gray,
            Margin = new Thickness(0, 4, 0, 0)
        });
        root.Children.Add(_status);

        Content = root;
        Loaded += async (_, _) => await ReloadAsync();
    }

    private async Task UploadAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "اختر ملفات توثيق التخليص الجمركي",
            Filter = "ملفات التوثيق|*.pdf;*.doc;*.docx;*.jpg;*.jpeg;*.png;*.webp;*.tif;*.tiff|كل الملفات|*.*",
            Multiselect = true
        };

        if (dialog.ShowDialog() != true)
            return;

        var files = new List<(string FileName, byte[] Content)>();
        foreach (var path in dialog.FileNames)
        {
            files.Add((Path.GetFileName(path), await File.ReadAllBytesAsync(path)));
        }

        Mouse.OverrideCursor = Cursors.Wait;
        try
        {
            var uploaded = await ContainerDocumentationService.Instance.UploadFilesAsync(
                _containerId, _containerNumber, files);
            _grid.ItemsSource = uploaded;
            _status.Text = uploaded.Count == 0
                ? "لا توجد ملفات بعد."
                : $"تم رفع {files.Count} ملف — الإجمالي {uploaded.Count} ملف محفوظ.";
            _status.Foreground = (Brush)WpfApplication.Current.Resources["SuccessBrush"]!;
        }
        catch (Exception ex)
        {
            MockInteractionService.ShowWarning(ex.Message, "رفع الملفات");
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }

    private void OpenSelectedFile()
    {
        if (_grid.SelectedItem is not ContainerDocumentationFileDto file)
            return;

        try
        {
            ContainerDocumentationService.Instance.OpenFile(_containerId, _containerNumber, file.Id);
        }
        catch (Exception ex)
        {
            MockInteractionService.ShowWarning(ex.Message, "فتح الملف");
        }
    }

    private Task ReloadAsync()
    {
        var files = ContainerDocumentationService.Instance.ListFiles(_containerId, _containerNumber);
        _grid.ItemsSource = files;
        _status.Text = files.Count == 0
            ? "لا توجد ملفات توثيق بعد — ارفع PDF أو Word أو صور."
            : $"{files.Count} ملف توثيق محفوظ.";
        return Task.CompletedTask;
    }
}
