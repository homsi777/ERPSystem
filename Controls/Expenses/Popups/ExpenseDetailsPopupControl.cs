using ERPSystem.Application.DTOs.Expenses;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Expenses;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfApplication = System.Windows.Application;

namespace ERPSystem.Controls.Expenses.Popups;

public sealed class ExpenseDetailsPopupControl : UserControl
{
    private readonly Guid _expenseId;
    private readonly StackPanel _root = new();

    public ExpenseDetailsPopupControl(Guid expenseId)
    {
        _expenseId = expenseId;
        Content = _root;
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (!AppServices.IsInitialized) return;
        var result = await ExpenseUiService.Instance.GetOperationsCenterAsync(_expenseId);
        if (!ApplicationResultPresenter.Present(result) || result.Value is null) return;
        Render(result.Value);
    }

    private void Render(ExpenseOperationsCenterDto data)
    {
        _root.Children.Clear();
        var d = data.Details;
        var f = data.Financial;

        _root.Children.Add(BuildKpiRow(
            ("المدفوع", $"{f.PaidAmountBase:N0} {f.BaseCurrency}", "SuccessBrush"),
            ("المتبقي", $"{f.RemainingBalanceBase:N0}", "WarningBrush"),
            ("الدفعات", f.CompletedPayments.ToString(), "InfoBrush"),
            ("الفئة", d.CategoryName, "AccentPayableBrush")));

        _root.Children.Add(ErpUiFactory.Card(ErpUiFactory.BuildFormGrid(
            ("الحالة", ReadOnly(d.StatusDisplay)),
            ("الفئة", ReadOnly(d.CategoryName)),
            ("مركز التكلفة", ReadOnly(d.CostCenterName ?? "—")),
            ("المستفيد", ReadOnly(d.PayeeName ?? "—")),
            ("تاريخ البداية", ReadOnly(d.StartDate.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture))),
            ("ملاحظات", ReadOnly(d.Notes ?? "—")))));

        if (d.Payments.Count > 0)
        {
            _root.Children.Add(ErpUiFactory.SectionTitle("آخر القيود"));
            var grid = new DataGrid { AutoGenerateColumns = false, IsReadOnly = true, MaxHeight = 180 };
            ErpUiFactory.AddGridColumn(grid, "التاريخ", nameof(ExpensePaymentDto.PaymentDate), 95, "yyyy/MM/dd");
            ErpUiFactory.AddGridColumn(grid, "المبلغ", nameof(ExpensePaymentDto.AmountBase), 100, "N2");
            ErpUiFactory.AddGridColumn(grid, "البيان", nameof(ExpensePaymentDto.Notes), "*");
            grid.ItemsSource = d.Payments.Take(8).ToList();
            _root.Children.Add(ErpUiFactory.Card(grid));
        }
    }

    private static UIElement BuildKpiRow(params (string title, string value, string brush)[] items)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
        foreach (var (title, value, brush) in items)
        {
            row.Children.Add(new Border
            {
                Background = (Brush)WpfApplication.Current.Resources["SurfaceAltBrush"]!,
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 0, 8, 0),
                MinWidth = 100,
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
            });
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
