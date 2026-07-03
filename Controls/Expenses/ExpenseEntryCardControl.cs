using ERPSystem.Application.DTOs.Expenses;
using ERPSystem.Services.Expenses;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WpfApplication = System.Windows.Application;

namespace ERPSystem.Controls.Expenses;

public sealed class ExpenseEntryCardControl : Border
{
    private readonly ExpenseEntryListDto _entry;

    public ExpenseEntryCardControl(ExpenseEntryListDto entry)
    {
        _entry = entry;
        Width = 300;
        MinHeight = 150;
        Margin = new Thickness(0, 0, 12, 12);
        CornerRadius = new CornerRadius(10);
        BorderThickness = new Thickness(1);
        BorderBrush = Br("BorderBrush");
        Background = Br("SurfaceBrush");
        Effect = (System.Windows.Media.Effects.Effect)WpfApplication.Current.Resources["CardShadow"]!;
        Cursor = Cursors.Hand;
        Padding = new Thickness(16);

        var amountText = string.Equals(entry.Currency, "USD", StringComparison.OrdinalIgnoreCase)
            ? $"{entry.AmountBase:N2} USD"
            : $"{entry.AmountOriginal:N0} {entry.Currency}";

        var subAmount = string.Equals(entry.Currency, "USD", StringComparison.OrdinalIgnoreCase)
            ? null
            : $"≈ {entry.AmountBase:N2} USD";

        Child = new StackPanel
        {
            Children =
            {
                new TextBlock
                {
                    Text = entry.ExpenseName,
                    FontSize = 15,
                    FontWeight = FontWeights.SemiBold,
                    TextTrimming = TextTrimming.CharacterEllipsis
                },
                new TextBlock
                {
                    Text = entry.PaymentDate.ToString("yyyy/MM/dd"),
                    FontSize = 11,
                    Margin = new Thickness(0, 2, 0, 10),
                    Foreground = Br("TextMutedBrush")
                },
                new TextBlock
                {
                    Text = amountText,
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    Foreground = Br("DangerBrush")
                },
                new TextBlock
                {
                    Text = subAmount ?? "",
                    FontSize = 12,
                    Margin = new Thickness(0, 2, 0, 0),
                    Foreground = Br("PrimaryBrush"),
                    Visibility = subAmount is null ? Visibility.Collapsed : Visibility.Visible
                },
                new TextBlock
                {
                    Text = $"الصندوق: {entry.CashboxName ?? "—"}",
                    FontSize = 12,
                    Margin = new Thickness(0, 8, 0, 0),
                    Foreground = Br("TextSecondaryBrush")
                },
                new TextBlock
                {
                    Text = entry.Description ?? "—",
                    FontSize = 12,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    Margin = new Thickness(0, 4, 0, 0),
                    Foreground = Br("TextPrimaryBrush")
                },
                new TextBlock
                {
                    Text = "انقر يميناً لمهام المصروف",
                    FontSize = 10,
                    Foreground = Br("TextMutedBrush"),
                    Margin = new Thickness(0, 10, 0, 0)
                }
            }
        };

        MouseEnter += (_, _) => BorderBrush = Br("AccentPayableBrush");
        MouseLeave += (_, _) => BorderBrush = Br("BorderBrush");
        MouseLeftButtonUp += (_, e) =>
        {
            if (e.ChangedButton == MouseButton.Left)
                ExpensePopupService.ShowOperationsCenter(ExpensePopupService.FromEntry(_entry), "Payments");
        };
        PreviewMouseRightButtonDown += (_, e) =>
        {
            e.Handled = true;
            ExpenseContextMenuService.Show(ExpensePopupService.FromEntry(_entry), this);
        };
    }

    private static Brush Br(string key) => (Brush)WpfApplication.Current.Resources[key]!;
}
