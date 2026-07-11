using ERPSystem.Application.Common;
using ERPSystem.Application.Commands.Finance;
using ERPSystem.Application.DTOs.Finance;
using ERPSystem.Domain.Entities.Finance;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Finance;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ERPSystem.Controls.Customers;

/// <summary>استيراد Excel لأرصدة العملاء — معاينة، تحقق، ثم إنشاء مستند.</summary>
public sealed class CustomerOpeningBalanceImportControl : UserControl
{
    private readonly DatePicker _date = ErpUiFactory.FormDate(DateTime.Today);
    private readonly TextBox _currency = ErpUiFactory.FormField("USD");
    private readonly TextBox _reference = ErpUiFactory.FormField();
    private readonly TextBlock _fileLabel = new() { Foreground = Br("TextMutedBrush"), Margin = new Thickness(0, 8, 0, 0) };
    private readonly TextBlock _summaryLabel = new() { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 12, 0, 0) };
    private readonly DataGrid _previewGrid = ErpUiFactory.BuildGrid(null, false);
    private readonly DataGrid _issuesGrid = ErpUiFactory.BuildGrid(null, false);
    private readonly Button _btnImport = new()
    {
        Content = "استيراد وإنشاء المستند",
        Style = S("PrimaryButtonStyle"),
        Height = 36,
        Margin = new Thickness(0, 12, 0, 0),
        IsEnabled = false
    };

    private byte[]? _fileBytes;
    private string _fileName = "";
    private OpeningBalanceValidationReportDto? _validation;

    public CustomerOpeningBalanceImportControl()
    {
        var root = new StackPanel();
        root.Children.Add(ErpUiFactory.SectionTitle("استيراد أرصدة العملاء من Excel"));
        root.Children.Add(ErpUxFactory.InfoBanner(
            "اختر الملف → معاينة → تحقق → استيراد. يدعم مئات أو آلاف العملاء في مستند واحد جاهز للاعتماد.",
            "info"));

        var pickBtn = new Button
        {
            Content = "اختيار ملف Excel",
            Style = S("SecondaryButtonStyle"),
            Height = 34,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        pickBtn.Click += async (_, _) => await PickAndPreviewAsync();
        root.Children.Add(pickBtn);
        root.Children.Add(_fileLabel);

        root.Children.Add(ErpUiFactory.SectionTitle("إعدادات المستند"));
        root.Children.Add(ErpUiFactory.Card(ErpUiFactory.BuildFormGrid(
            ("تاريخ الافتتاح", _date),
            ("العملة الافتراضية", _currency),
            ("المرجع", _reference))));

        root.Children.Add(ErpUiFactory.SectionTitle("تقرير التحقق"));
        root.Children.Add(_summaryLabel);
        root.Children.Add(ErpUiFactory.Card(_issuesGrid, new Thickness(0, 8, 0, 0)));

        root.Children.Add(ErpUiFactory.SectionTitle("معاينة البيانات"));
        _previewGrid.MaxHeight = 220;
        root.Children.Add(ErpUiFactory.Card(_previewGrid));

        _btnImport.Click += async (_, _) => await ImportAsync();
        root.Children.Add(_btnImport);

        ConfigureIssuesGrid();
        Content = new ScrollViewer { Content = root, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
    }

    private void ConfigureIssuesGrid()
    {
        _issuesGrid.AutoGenerateColumns = false;
        _issuesGrid.MaxHeight = 160;
        ErpUiFactory.AddGridColumn(_issuesGrid, "السطر", nameof(ValidationRow.RowNumber), 60, null);
        ErpUiFactory.AddGridColumn(_issuesGrid, "الحقل", nameof(ValidationRow.Field), 100, null);
        ErpUiFactory.AddGridColumn(_issuesGrid, "الرسالة", nameof(ValidationRow.Message), "*", null);
        ErpUiFactory.AddGridColumn(_issuesGrid, "النوع", nameof(ValidationRow.Kind), 80, null);
    }

    private async Task PickAndPreviewAsync()
    {
        var dlg = new OpenFileDialog
        {
            Filter = "Excel|*.xlsx;*.xls",
            Title = "استيراد أرصدة العملاء"
        };
        if (dlg.ShowDialog() != true)
            return;

        _fileName = dlg.SafeFileName;
        _fileBytes = await File.ReadAllBytesAsync(dlg.FileName);
        _fileLabel.Text = $"الملف: {_fileName} ({_fileBytes.Length / 1024:N0} KB)";

        await ValidatePreviewAsync();
    }

    private async Task ValidatePreviewAsync()
    {
        if (_fileBytes is null)
            return;

        if (!AppServices.IsInitialized)
            return;

        var result = await OpeningBalanceUiService.Instance.ImportExcelAsync(new ImportOpeningBalanceExcelCommand
        {
            Type = OpeningBalanceType.CustomerReceivable,
            FileName = _fileName,
            Content = _fileBytes,
            OpeningDate = ApplicationDateNormalizer.ToUtcDate(_date.SelectedDate) ?? DateTime.UtcNow,
            CurrencyCode = _currency.Text.Trim().ToUpperInvariant(),
            ExchangeRate = 1m,
            Reference = _reference.Text.Trim(),
            PreviewOnly = true
        });

        if (!ApplicationResultPresenter.Present(result) || result.Value is null)
        {
            _btnImport.IsEnabled = false;
            return;
        }

        _validation = result.Value.Validation;
        var v = _validation;
        _summaryLabel.Text =
            $"إجمالي الصفوف: {v.TotalRows} — صالح: {v.ValidRows} — مكرر: {v.DuplicateRows} — " +
            $"أخطاء: {v.Errors.Count} — تحذيرات: {v.Warnings.Count}";

        var issues = v.Errors.Select(e => new ValidationRow(e.RowNumber, e.Field, e.Message, "خطأ"))
            .Concat(v.Warnings.Select(w => new ValidationRow(w.RowNumber, w.Field, w.Message, "تحذير")))
            .ToList();
        _issuesGrid.ItemsSource = issues;

        _previewGrid.ItemsSource = BuildPreviewRows(v.TotalRows);
        _previewGrid.AutoGenerateColumns = true;
        ErpAccountingColorHelper.ApplyDebitCreditColumnsByHeader(_previewGrid);
        _btnImport.IsEnabled = v.ValidRows > 0;
    }

    private List<object> BuildPreviewRows(int totalRows)
    {
        if (_fileBytes is null || totalRows == 0)
            return [];

        var (_, lines, _) = ERPSystem.Application.UseCases.Finance.OpeningBalanceExcelParser.Parse(
            OpeningBalanceType.CustomerReceivable, _fileBytes);

        return lines.Take(500).Select((l, i) => (object)new
        {
            السطر = i + 1,
            العميل = l.PartyName ?? "—",
            مدين = l.Debit,
            دائن = l.Credit,
            المرجع = l.Reference ?? "—",
            ملاحظات = l.Notes ?? "—"
        }).ToList();
    }

    private async Task ImportAsync()
    {
        if (_fileBytes is null)
        {
            MockInteractionService.ShowWarning("اختر ملفاً أولاً.");
            return;
        }

        if (_validation is not null && !_validation.IsValid)
        {
            var proceed = MockInteractionService.Confirm(
                "توجد أخطاء في بعض الصفوف. سيتم تخطي الصفوف غير الصالحة. هل تريد المتابعة؟",
                "تأكيد الاستيراد");
            if (!proceed)
                return;
        }

        var result = await OpeningBalanceUiService.Instance.ImportExcelAsync(new ImportOpeningBalanceExcelCommand
        {
            Type = OpeningBalanceType.CustomerReceivable,
            FileName = _fileName,
            Content = _fileBytes,
            OpeningDate = ApplicationDateNormalizer.ToUtcDate(_date.SelectedDate) ?? DateTime.UtcNow,
            CurrencyCode = _currency.Text.Trim().ToUpperInvariant(),
            ExchangeRate = 1m,
            Reference = string.IsNullOrWhiteSpace(_reference.Text) ? _fileName : _reference.Text.Trim(),
            SkipInvalidRows = true
        });

        if (!ApplicationResultPresenter.Present(result) || result.Value is null)
            return;

        OpeningBalanceListRefreshHub.RequestRefresh();
        CustomerOpeningBalanceRefreshHub.RequestRefresh();
        MockInteractionService.ShowSuccess(
            $"تم الاستيراد: {result.Value.ImportedRows} سطر — المستند {result.Value.DocumentNumber}");
    }

    private sealed record ValidationRow(int RowNumber, string Field, string Message, string Kind);

    private static Style S(string key) => (Style)System.Windows.Application.Current.Resources[key]!;
    private static Brush Br(string key) => (Brush)System.Windows.Application.Current.Resources[key]!;
}
