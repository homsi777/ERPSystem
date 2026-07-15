using ERPSystem.Application.Commands.Containers;
using ERPSystem.Application.Common;
using ERPSystem.Application.DTOs.Containers;
using ERPSystem.Controls;
using ERPSystem.Core;
using ERPSystem.Domain.Enums;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.China;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfApplication = System.Windows.Application;

namespace ERPSystem.Controls.China;

public sealed class SalePriceEntryRow : INotifyPropertyChanged
{
    public Guid TypeLineId { get; init; }
    public string TypeDisplayName { get; init; } = "";
    public DplQuantityUnit Unit { get; init; }
    public decimal LandedCostPerMeterUsd { get; init; }

    public decimal LandedCostDisplay =>
        ChinaImportLengthDisplay.FromStoredRate(LandedCostPerMeterUsd, Unit);

    private string _marginText = "";
    public string MarginText
    {
        get => _marginText;
        set
        {
            _marginText = value;
            OnPropertyChanged(nameof(MarginText));
            OnPropertyChanged(nameof(SalePriceDisplay));
        }
    }

    public string SalePriceDisplay
    {
        get
        {
            if (!TryParseMargin(out var margin))
                return "—";
            return ChinaImportLengthDisplay.FormatRate(LandedCostPerMeterUsd + ToStoredMargin(margin), Unit);
        }
    }

    public bool TryParseMargin(out decimal margin) =>
        decimal.TryParse(_marginText.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out margin) ||
        decimal.TryParse(_marginText.Trim(), out margin);

    public decimal ToStoredMargin(decimal displayMargin) =>
        ChinaImportLengthDisplay.ToStoredRate(displayMargin, Unit);

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public sealed class ChinaImportSalePriceControl : UserControl
{
    private readonly ObservableCollection<SalePriceEntryRow> _rows = [];
    private readonly StackPanel _infoBannerHost = new();
    private readonly DataGrid _grid;
    private readonly Button _saveButton;
    private ContainerOperationsCenterDto? _loaded;
    private DplQuantityUnit _unit = DplQuantityUnit.Meters;

    public ChinaImportSalePriceControl()
    {
        var root = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(16) };
        var stack = new StackPanel();

        stack.Children.Add(ErpUiFactory.SectionTitle("الخطوة 5: أسعار البيع لكل نوع قماش"));
        stack.Children.Add(ErpUxFactory.WorkflowStepper(
            ("وصول الحاوية", true, true),
            ("تحليل الملف", true, true),
            ("إدخال التكلفة", true, true),
            ("Landing Cost", true, true),
            ("أسعار البيع", true, true),
            ("اعتماد", false, false),
            ("جاهز للبيع", false, false)));

        stack.Children.Add(_infoBannerHost);

        _grid = new DataGrid
        {
            AutoGenerateColumns = false,
            IsReadOnly = false,
            CanUserAddRows = false,
            ItemsSource = _rows,
            MinHeight = 120
        };

        stack.Children.Add(ErpUiFactory.Card(_grid));

        var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 16, 0, 0) };
        var back = new Button
        {
            Content = "العودة — Landing Cost",
            Style = (Style)WpfApplication.Current.Resources["SecondaryButtonStyle"]!
        };
        back.Click += (_, _) => ChinaImportNavigation.Navigate("LandingCost", _loaded?.Container.Status);
        actions.Children.Add(back);

        _saveButton = new Button
        {
            Content = "حفظ والمتابعة للاعتماد",
            Style = (Style)WpfApplication.Current.Resources["PrimaryButtonStyle"]!,
            Margin = new Thickness(8, 0, 0, 0)
        };
        _saveButton.Click += async (_, _) => await SaveAsync();
        actions.Children.Add(_saveButton);
        stack.Children.Add(actions);

        root.Content = stack;
        Content = root;
        Background = (SolidColorBrush)WpfApplication.Current.Resources["AppBgBrush"]!;

        Loaded += async (_, _) => await LoadAsync();
    }

    private void ConfigureGridColumns()
    {
        _grid.Columns.Clear();
        _grid.Columns.Add(new DataGridTextColumn
        {
            Header = "النوع / اللون",
            Binding = new System.Windows.Data.Binding(nameof(SalePriceEntryRow.TypeDisplayName)),
            IsReadOnly = true,
            Width = new DataGridLength(1, DataGridLengthUnitType.Star)
        });
        _grid.Columns.Add(new DataGridTextColumn
        {
            Header = $"{ChinaImportLengthDisplay.CostPerUnitLabel(_unit)} ($)",
            Binding = new System.Windows.Data.Binding(nameof(SalePriceEntryRow.LandedCostDisplay))
            {
                StringFormat = "N4"
            },
            IsReadOnly = true,
            Width = 100
        });
        _grid.Columns.Add(new DataGridTextColumn
        {
            Header = ChinaImportLengthDisplay.MarginPerUnitLabel(_unit),
            Binding = new System.Windows.Data.Binding(nameof(SalePriceEntryRow.MarginText))
            {
                UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged
            },
            Width = 120
        });
        _grid.Columns.Add(new DataGridTextColumn
        {
            Header = ChinaImportLengthDisplay.FinalSalePriceLabel(_unit),
            Binding = new System.Windows.Data.Binding(nameof(SalePriceEntryRow.SalePriceDisplay)),
            IsReadOnly = true,
            Width = 140
        });
    }

    private async Task LoadAsync()
    {
        _rows.Clear();
        if (!AppServices.IsInitialized)
            return;

        var containerId = ChinaImportNavigationContext.ResolveContainerId();
        if (containerId is null || containerId == Guid.Empty)
            return;

        var result = await ContainerUiService.Instance.GetOperationsCenterAsync(containerId.Value);
        if (!ApplicationResultPresenter.Present(result) || result.Value is null)
            return;

        _loaded = result.Value;
        _unit = ChinaImportLengthDisplay.Resolve(_loaded.Container.DplQuantityUnit);

        _infoBannerHost.Children.Clear();
        _infoBannerHost.Children.Add(ErpUxFactory.InfoBanner(
            ChinaImportLengthDisplay.SalePriceBanner(_unit),
            "info"));

        ConfigureGridColumns();

        foreach (var line in _loaded.Container.FabricTypeLines.OrderBy(l => l.LineNumber))
        {
            var marginDisplay = line.MarginPerMeterUsd > 0
                ? ChinaImportLengthDisplay.FromStoredRate(line.MarginPerMeterUsd, _unit)
                : 0m;

            _rows.Add(new SalePriceEntryRow
            {
                TypeLineId = line.Id,
                TypeDisplayName = line.TypeDisplayName,
                Unit = _unit,
                LandedCostPerMeterUsd = line.LandedCostPerMeterUsd,
                MarginText = marginDisplay > 0 ? marginDisplay.ToString("N4", CultureInfo.InvariantCulture) : ""
            });
        }

        _saveButton.IsEnabled = _rows.Count > 0;
    }

    private async Task SaveAsync()
    {
        if (_loaded is null)
            return;

        var commands = new List<ContainerTypeSalePriceCommand>();
        foreach (var row in _rows)
        {
            if (!row.TryParseMargin(out var margin) || margin < 0)
            {
                MessageBox.Show(
                    $"يرجى إدخال هامش ربح صالح لنوع «{row.TypeDisplayName}».",
                    "أسعار البيع",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            commands.Add(new ContainerTypeSalePriceCommand
            {
                TypeLineId = row.TypeLineId,
                MarginPerMeterUsd = row.ToStoredMargin(margin)
            });
        }

        _saveButton.IsEnabled = false;
        _saveButton.Content = "جاري الحفظ...";
        try
        {
            var result = await ContainerUiService.Instance.SetTypeSalePricesAsync(
                _loaded.Container.Id, commands);
            if (!ApplicationResultPresenter.Present(result))
                return;

            ChinaImportNavigation.Navigate("LandingCost", ChinaContainerStatus.LandingCostReviewed);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"تعذّر حفظ أسعار البيع.\n\n{ex.Message}", "أسعار البيع",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _saveButton.Content = "حفظ والمتابعة للاعتماد";
            _saveButton.IsEnabled = true;
        }
    }
}
