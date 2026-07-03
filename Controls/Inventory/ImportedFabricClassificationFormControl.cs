using ERPSystem.Application.Commands.Catalog;
using ERPSystem.Application.DTOs.Catalog;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Inventory;
using System.Windows;
using System.Windows.Controls;

namespace ERPSystem.Controls.Inventory;

public sealed class ImportedFabricClassificationFormControl : UserControl
{
    private readonly TextBlock _header = new() { FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 8) };
    private readonly TextBox _fabricNameAr = ErpUiFactory.FormField("");
    private readonly TextBox _fabricNameEn = ErpUiFactory.FormField("");
    private readonly TextBox _colorNameAr = ErpUiFactory.FormField("");
    private readonly Button _save = new() { Content = "حفظ التعديل", Style = S("PrimaryButtonStyle"), MinWidth = 140, Height = 38 };
    private ImportedFabricClassificationDto? _row;
    private bool _popupMode;
    private bool _saving;

    public ImportedFabricClassificationFormControl()
    {
        var stack = new StackPanel { MaxWidth = 520 };
        stack.Children.Add(_header);
        stack.Children.Add(ErpUxFactory.InfoBanner("تعديل أسماء التصنيف فقط — الأكواد تُنشأ تلقائياً من استيراد الحاوية."));
        stack.Children.Add(ErpUiFactory.Card(ErpUiFactory.BuildFormGrid(
            ("اسم التوب (عربي)", _fabricNameAr),
            ("اسم التوب (إنجليزي)", _fabricNameEn),
            ("اسم اللون (عربي)", _colorNameAr))));
        var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 16, 0, 0) };
        actions.Children.Add(_save);
        stack.Children.Add(actions);
        Content = stack;
        Loaded += OnLoaded;
        _save.Click += async (_, _) => await SaveAsync();
    }

    public void BindPopupHost() => _popupMode = true;

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _row = InventoryCatalogNavigationContext.EditClassification;
        if (_row is null) return;

        _header.Text = $"{_row.ContainerNumber} — {_row.FabricCode} / {_row.ColorCode}";
        _fabricNameAr.Text = _row.NameAr;
        _fabricNameEn.Text = _row.NameEn ?? "";
        _colorNameAr.Text = _row.ColorNameAr;
    }

    private async Task SaveAsync()
    {
        if (_saving || _row is null || !AppServices.IsInitialized) return;
        if (string.IsNullOrWhiteSpace(_fabricNameAr.Text) || string.IsNullOrWhiteSpace(_colorNameAr.Text))
        {
            MessageBox.Show("اسم التوب واللون مطلوبان.", "تحقق", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _saving = true;
        _save.IsEnabled = false;
        try
        {
            var itemResult = await InventoryCatalogUiService.Instance.UpdateItemAsync(
                new UpdateFabricItemCommand(
                    _row.FabricItemId,
                    _row.CategoryId,
                    _fabricNameAr.Text.Trim(),
                    _fabricNameEn.Text.Trim()));
            if (!ApplicationResultPresenter.Present(itemResult))
                return;

            var colorResult = await InventoryCatalogUiService.Instance.UpdateColorAsync(
                new UpdateFabricColorCommand(
                    _row.FabricColorId,
                    _colorNameAr.Text.Trim(),
                    _colorNameAr.Text.Trim()));
            if (ApplicationResultPresenter.Present(colorResult) && _popupMode)
                InventoryCatalogPopupService.CompleteSuccess();
        }
        finally
        {
            _saving = false;
            _save.IsEnabled = true;
        }
    }

    private static Style S(string key) => (Style)System.Windows.Application.Current.Resources[key]!;
}
