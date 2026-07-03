using ERPSystem.Application.DTOs.Capital;
using ERPSystem.Services.Capital;using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WpfApplication = System.Windows.Application;

namespace ERPSystem.Controls.Capital;

public sealed class CapitalPartnerCardControl : Border
{
  private readonly CapitalPartnerListDto _partner;
  private readonly TextBlock _name = new() { FontSize = 16, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis };
  private readonly TextBlock _code = new() { FontSize = 11, Margin = new Thickness(0, 2, 0, 0) };
  private readonly TextBlock _ownership = new() { FontSize = 22, FontWeight = FontWeights.Bold };
  private readonly TextBlock _capital = new() { FontSize = 13, FontWeight = FontWeights.SemiBold };
  private readonly TextBlock _invest = new() { FontSize = 12 };
  private readonly TextBlock _withdraw = new() { FontSize = 12 };
  private readonly Border _statusBadge = new() { CornerRadius = new CornerRadius(6), Padding = new Thickness(8, 3, 8, 3), VerticalAlignment = VerticalAlignment.Top };

  public CapitalPartnerCardControl(CapitalPartnerListDto partner)
  {
    _partner = partner;
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

    _name.Text = partner.FullName;
    _code.Text = partner.Code;
    _code.Foreground = Br("TextMutedBrush");
    _ownership.Foreground = Br("PrimaryBrush");
    _capital.Foreground = Br("TextPrimaryBrush");
    _invest.Foreground = Br("SuccessBrush");
    _withdraw.Foreground = Br("DangerBrush");

    var pct = partner.CompanyOwnershipPercentage;
    _ownership.Text = pct is decimal p ? $"{p:N2}%" : "—";
    _capital.Text = $"{partner.CurrentCapitalBase:N0} SAR";
    _invest.Text = $"استثمارات: {partner.TotalInvestmentsBase:N0}";
    _withdraw.Text = $"سحوبات: {partner.TotalWithdrawalsBase:N0}";

    _statusBadge.Background = Br(partner.Status.ToString() == "Archived" ? "WarningBgBrush" : "SuccessBgBrush");
    _statusBadge.Child = new TextBlock
    {
      Text = partner.StatusDisplay,
      FontSize = 11,
      FontWeight = FontWeights.SemiBold,
      Foreground = Br(partner.Status.ToString() == "Archived" ? "WarningBrush" : "SuccessBrush")
    };

    var header = new Grid();
    header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
    header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
    var nameStack = new StackPanel();
    nameStack.Children.Add(_name);
    nameStack.Children.Add(_code);
    Grid.SetColumn(nameStack, 0);
    Grid.SetColumn(_statusBadge, 1);
    header.Children.Add(nameStack);
    header.Children.Add(_statusBadge);

    var iconRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 12, 0, 8) };
    iconRow.Children.Add(CreateIconBadge("\uE8F1"));
    iconRow.Children.Add(new StackPanel
    {
      Margin = new Thickness(10, 0, 0, 0),
      VerticalAlignment = VerticalAlignment.Center,
      Children =
      {
        new TextBlock { Text = "نسبة الملكية", FontSize = 11, Foreground = Br("TextMutedBrush") },
        _ownership
      }
    });

    var metrics = new Grid { Margin = new Thickness(0, 4, 0, 0) };
    metrics.RowDefinitions.Add(new RowDefinition());
    metrics.RowDefinitions.Add(new RowDefinition { Height = new GridLength(6) });
    metrics.RowDefinitions.Add(new RowDefinition());
    metrics.Children.Add(_capital);
    Grid.SetRow(_invest, 2);
    metrics.Children.Add(_invest);

    var footer = new TextBlock
    {
      Text = "انقر يميناً للمهام • انقر للتفاصيل",
      FontSize = 10,
      Foreground = Br("TextMutedBrush"),
      Margin = new Thickness(0, 10, 0, 0)
    };

    Child = new StackPanel
    {
      Children = { header, iconRow, metrics, _withdraw, footer }
    };

    MouseEnter += (_, _) => BorderBrush = Br("PrimaryBrush");
    MouseLeave += (_, _) => BorderBrush = Br("BorderBrush");
    MouseLeftButtonUp += (_, e) =>
    {
      if (e.ChangedButton == MouseButton.Left)
        CapitalPartnerPopupService.ShowDetails(_partner);
    };
    PreviewMouseRightButtonDown += OnRightClick;
  }

  private void OnRightClick(object sender, MouseButtonEventArgs e)
  {
    e.Handled = true;
    CapitalPartnerContextMenuService.Show(_partner, this);
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
