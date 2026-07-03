using ERPSystem.Application.DTOs.Capital;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Capital;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfApplication = System.Windows.Application;

namespace ERPSystem.Controls.Capital.Popups;

public sealed class CapitalPartnerDetailsPopupControl : UserControl
{
    private readonly Guid _partnerId;
    private readonly StackPanel _root = new();

    public CapitalPartnerDetailsPopupControl(Guid partnerId)
    {
        _partnerId = partnerId;
        Content = _root;
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (!AppServices.IsInitialized) return;
        var result = await CapitalPartnerUiService.Instance.GetOperationsCenterAsync(_partnerId);
        if (!ApplicationResultPresenter.Present(result) || result.Value is null) return;
        Render(result.Value);
    }

    private void Render(CapitalOperationsCenterDto data)
    {
        _root.Children.Clear();
        var d = data.Details;
        var f = data.Financial;

        _root.Children.Add(BuildKpiRow(
            ("رأس المال", $"{f.CurrentCapitalBase:N0} {f.BaseCurrency}", "PrimaryBrush"),
            ("استثمارات", $"{f.TotalInvestmentsBase:N0}", "SuccessBrush"),
            ("سحوبات", $"{f.TotalWithdrawalsBase:N0}", "DangerBrush"),
            ("أرباح موزعة", $"{f.DistributedProfitBase:N0}", "InfoBrush")));

        var info = ErpUiFactory.Card(ErpUiFactory.BuildFormGrid(
            ("الهاتف", ReadOnly(d.Phone ?? "—")),
            ("الهوية", ReadOnly(d.NationalId ?? "—")),
            ("العملة", ReadOnly(d.DefaultCurrency)),
            ("الحالة", ReadOnly(d.StatusDisplay)),
            ("المخاطر", ReadOnly(d.RiskLevelDisplay)),
            ("تاريخ الإنشاء", ReadOnly(d.CreatedAt.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture)))));
        _root.Children.Add(info);

        if (d.Participations.Count > 0)
        {
            _root.Children.Add(ErpUiFactory.SectionTitle("المشاركات"));
            var grid = new DataGrid { AutoGenerateColumns = false, IsReadOnly = true, MaxHeight = 160 };
            ErpUiFactory.AddGridColumn(grid, "النطاق", nameof(PartnerParticipationDto.ScopeDisplay), 100);
            ErpUiFactory.AddGridColumn(grid, "النسبة %", nameof(PartnerParticipationDto.OwnershipPercentage), 80, "N2");
            ErpUiFactory.AddGridColumn(grid, "من", nameof(PartnerParticipationDto.EffectiveFrom), 95, "yyyy/MM/dd");
            grid.ItemsSource = d.Participations;
            _root.Children.Add(ErpUiFactory.Card(grid));
        }

        if (d.Transactions.Count > 0)
        {
            _root.Children.Add(ErpUiFactory.SectionTitle("آخر الحركات"));
            var grid = new DataGrid { AutoGenerateColumns = false, IsReadOnly = true, MaxHeight = 180 };
            ErpUiFactory.AddGridColumn(grid, "النوع", nameof(CapitalTransactionDto.TypeDisplay), 110);
            ErpUiFactory.AddGridColumn(grid, "المبلغ", nameof(CapitalTransactionDto.AmountBase), 100, "N2");
            ErpUiFactory.AddGridColumn(grid, "التاريخ", nameof(CapitalTransactionDto.TransactionDate), 95, "yyyy/MM/dd");
            ErpUiFactory.AddGridColumn(grid, "البيان", nameof(CapitalTransactionDto.Notes), "*");
            grid.ItemsSource = d.Transactions.Take(8).ToList();
            _root.Children.Add(ErpUiFactory.Card(grid));
        }
    }

    private static UIElement BuildKpiRow(params (string title, string value, string brush)[] items)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
        foreach (var (title, value, brush) in items)
        {
            var card = new Border
            {
                Background = (Brush)WpfApplication.Current.Resources["SurfaceAltBrush"]!,
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 0, 8, 0),
                MinWidth = 110,
                Child = new StackPanel
                {
                    Children =
                    {
                        new TextBlock
                        {
                            Text = title,
                            FontSize = 11,
                            Foreground = (Brush)WpfApplication.Current.Resources["TextMutedBrush"]!
                        },
                        new TextBlock
                        {
                            Text = value,
                            FontSize = 14,
                            FontWeight = FontWeights.SemiBold,
                            Margin = new Thickness(0, 4, 0, 0),
                            Foreground = (Brush)WpfApplication.Current.Resources[brush]!
                        }
                    }
                }
            };
            row.Children.Add(card);
        }
        return row;
    }

    private static TextBlock ReadOnly(string text) => new()
    {
        Text = text,
        FontSize = 13,
        Foreground = (Brush)WpfApplication.Current.Resources["TextPrimaryBrush"]!
    };
}
