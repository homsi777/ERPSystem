using ERPSystem.Application.DTOs.Accounting;
using ERPSystem.Services.Accounting;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WpfApplication = System.Windows.Application;

namespace ERPSystem.Controls.Accounting;

public sealed class AccountCardControl : Border
{
    private readonly AccountListDto _account;

    public AccountCardControl(AccountListDto account)
    {
        _account = account;
        Width = 300;
        MinHeight = 148;
        Margin = new Thickness(account.Level * 16, 0, 12, 12);
        CornerRadius = new CornerRadius(10);
        BorderThickness = new Thickness(1);
        BorderBrush = Br("BorderBrush");
        Background = Br("SurfaceBrush");
        Effect = (System.Windows.Media.Effects.Effect)WpfApplication.Current.Resources["CardShadow"]!;
        Cursor = Cursors.Hand;
        Padding = new Thickness(16);

        var postableBadge = new Border
        {
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(8, 3, 8, 3),
            VerticalAlignment = VerticalAlignment.Top,
            Background = Br(account.IsPostable ? "SuccessBgBrush" : "SurfaceAltBrush"),
            Child = new TextBlock
            {
                Text = account.IsPostable ? "قابل للترحيل" : "تجميعي",
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = Br(account.IsPostable ? "SuccessBrush" : "TextMutedBrush")
            }
        };

        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var nameStack = new StackPanel();
        nameStack.Children.Add(new TextBlock
        {
            Text = _account.NameAr,
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        nameStack.Children.Add(new TextBlock
        {
            Text = _account.Code,
            FontSize = 11,
            Margin = new Thickness(0, 2, 0, 0),
            Foreground = Br("TextMutedBrush")
        });
        Grid.SetColumn(nameStack, 0);
        Grid.SetColumn(postableBadge, 1);
        header.Children.Add(nameStack);
        header.Children.Add(postableBadge);

        var typeRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 12, 0, 8) };
        typeRow.Children.Add(CreateIconBadge("\uE8C3"));
        typeRow.Children.Add(new StackPanel
        {
            Margin = new Thickness(10, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                new TextBlock { Text = "نوع الحساب", FontSize = 11, Foreground = Br("TextMutedBrush") },
                new TextBlock
                {
                    Text = _account.AccountTypeDisplay,
                    FontSize = 15,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Br("PrimaryBrush")
                }
            }
        });

        Child = new StackPanel
        {
            Children =
            {
                header,
                typeRow,
                new TextBlock
                {
                    Text = _account.ParentName is null ? "حساب رئيسي" : $"تحت: {_account.ParentName}",
                    FontSize = 12,
                    Foreground = Br("TextSecondaryBrush")
                },
                new TextBlock
                {
                    Text = _account.IsActive ? $"الحالة: نشط • {_account.ChildCount} حساب فرعي" : "الحالة: معطّل",
                    FontSize = 12,
                    Margin = new Thickness(0, 6, 0, 0),
                    Foreground = Br(_account.IsActive ? "TextPrimaryBrush" : "WarningBrush")
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

        MouseEnter += (_, _) => BorderBrush = Br("PrimaryBrush");
        MouseLeave += (_, _) => BorderBrush = Br("BorderBrush");
        MouseLeftButtonUp += (_, e) =>
        {
            if (e.ChangedButton == MouseButton.Left)
                AccountingPopupService.ShowAccountDetails(_account);
        };
        PreviewMouseRightButtonDown += (_, e) =>
        {
            e.Handled = true;
            AccountingContextMenuService.ShowAccount(_account, this);
        };
    }

    private static Border CreateIconBadge(string glyph) => new()
    {
        Width = 40,
        Height = 40,
        CornerRadius = new CornerRadius(8),
        Background = Br("PrimaryVeryLightBrush"),
        Child = new TextBlock
        {
            Text = glyph,
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 18,
            Foreground = Br("PrimaryBrush"),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        }
    };

    private static Brush Br(string key) => (Brush)WpfApplication.Current.Resources[key]!;
}
