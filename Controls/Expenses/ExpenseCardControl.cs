using ERPSystem.Application.DTOs.Expenses;
using ERPSystem.Services.Expenses;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WpfApplication = System.Windows.Application;

namespace ERPSystem.Controls.Expenses;

public sealed class ExpenseCardControl : Border
{
    private readonly ExpenseListDto _expense;

    public ExpenseCardControl(ExpenseListDto expense)
    {
        _expense = expense;
        Width = 300;
        MinHeight = 168;
        Margin = new Thickness(0, 0, 12, 12);
        CornerRadius = new CornerRadius(10);
        BorderThickness = new Thickness(1);
        BorderBrush = Br("BorderBrush");
        Background = Br("SurfaceBrush");
        Effect = (System.Windows.Media.Effects.Effect)WpfApplication.Current.Resources["CardShadow"]!;
        Cursor = Cursors.Hand;
        Padding = new Thickness(16);

        var statusBadge = new Border
        {
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(8, 3, 8, 3),
            VerticalAlignment = VerticalAlignment.Top,
            Background = Br(expense.IsArchived ? "WarningBgBrush" : "SuccessBgBrush"),
            Child = new TextBlock
            {
                Text = expense.StatusDisplay,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = Br(expense.IsArchived ? "WarningBrush" : "SuccessBrush")
            }
        };

        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var nameStack = new StackPanel();
        nameStack.Children.Add(new TextBlock
        {
            Text = expense.Name,
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        nameStack.Children.Add(new TextBlock
        {
            Text = expense.Code,
            FontSize = 11,
            Margin = new Thickness(0, 2, 0, 0),
            Foreground = Br("TextMutedBrush")
        });
        Grid.SetColumn(nameStack, 0);
        Grid.SetColumn(statusBadge, 1);
        header.Children.Add(nameStack);
        header.Children.Add(statusBadge);

        var categoryRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 12, 0, 8) };
        categoryRow.Children.Add(CreateIconBadge("\uE9D9"));
        categoryRow.Children.Add(new StackPanel
        {
            Margin = new Thickness(10, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                new TextBlock { Text = "الفئة", FontSize = 11, Foreground = Br("TextMutedBrush") },
                new TextBlock
                {
                    Text = expense.CategoryKindDisplay,
                    FontSize = 15,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Br("AccentPayableBrush")
                }
            }
        });

        Child = new StackPanel
        {
            Children =
            {
                header,
                categoryRow,
                new TextBlock
                {
                    Text = $"إجمالي المصروف: {expense.PaidAmountBase:N0} {expense.BaseCurrency}",
                    FontSize = 13,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Br("TextPrimaryBrush")
                },
                new TextBlock
                {
                    Text = $"منذ: {expense.StartDate:yyyy/MM/dd}",
                    FontSize = 12,
                    Margin = new Thickness(0, 6, 0, 0),
                    Foreground = Br("TextSecondaryBrush")
                },
                new TextBlock
                {
                    Text = "انقر يميناً للمهام • انقر للتفاصيل",
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
                ExpensePopupService.ShowDetails(_expense);
        };
        PreviewMouseRightButtonDown += (_, e) =>
        {
            e.Handled = true;
            ExpenseContextMenuService.Show(_expense, this);
        };
    }

    private static Border CreateIconBadge(string glyph) => new()
    {
        Width = 40,
        Height = 40,
        CornerRadius = new CornerRadius(8),
        Background = Br("WarningBgBrush"),
        Child = new TextBlock
        {
            Text = glyph,
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 18,
            Foreground = Br("AccentPayableBrush"),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        }
    };

    private static Brush Br(string key) => (Brush)WpfApplication.Current.Resources[key]!;
}
