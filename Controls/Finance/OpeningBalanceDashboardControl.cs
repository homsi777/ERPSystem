using ERPSystem.Application.DTOs.Finance;
using ERPSystem.Application.Queries.Finance;
using ERPSystem.Core;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Finance;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ERPSystem.Controls.Finance;

public sealed class OpeningBalanceDashboardControl : UserControl
{
    private readonly StackPanel _root = new() { Margin = new Thickness(16) };
    private readonly StackPanel _kpiRow = new() { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 16) };
    private readonly WrapPanel _typeCards = new() { Margin = new Thickness(0, 8, 0, 0) };

    public OpeningBalanceDashboardControl()
    {
        _root.Children.Add(ErpUiFactory.SectionTitle("لوحة الأرصدة الافتتاحية"));
        _root.Children.Add(ErpUxFactory.InfoBanner(
            "مركز إدارة كل الأرصدة الافتتاحية قبل بدء التشغيل — إدخال يدوي، استيراد Excel، اعتماد وترحيل محاسبي موحّد.",
            "info"));
        _root.Children.Add(_kpiRow);
        _root.Children.Add(ErpUiFactory.SectionTitle("حسب النوع"));
        _root.Children.Add(_typeCards);
        Content = new ScrollViewer { Content = _root, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (!AppServices.IsInitialized) return;
        var result = await OpeningBalanceUiService.Instance.GetDashboardAsync();
        if (!ApplicationResultPresenter.Present(result) || result.Value is null) return;

        var d = result.Value;
        ErpUiFactory.SetSummaryCards(_kpiRow,
        [
            ("إجمالي المستندات", d.TotalDocuments.ToString(), "\uE8F1", Br("PrimaryBrush")),
            ("مسودة", d.DraftCount.ToString(), "\uE8A5", Br("TextMutedBrush")),
            ("بانتظار الاعتماد", d.PendingApprovalCount.ToString(), "\uE7BA", Br("WarningBrush")),
            ("مرحّل", d.PostedCount.ToString(), "\uE73E", Br("SuccessBrush")),
            ("إجمالي مرحّل", $"{d.TotalPostedBaseAmount:N2}", "\uE8C8", Br("InfoBrush"))
        ]);

        _typeCards.Children.Clear();
        foreach (var t in d.ByType)
        {
            var card = ErpUiFactory.Card(new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = t.TypeDisplay, FontWeight = FontWeights.SemiBold, FontSize = 14 },
                    new TextBlock { Text = $"{t.DocumentCount} مستند — {t.PostedCount} مرحّل", Margin = new Thickness(0, 4, 0, 0) },
                    new TextBlock { Text = $"{t.TotalBaseAmount:N2}", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 6, 0, 0) }
                }
            }, new Thickness(0, 0, 12, 12));
            card.Width = 220;
            _typeCards.Children.Add(card);
        }
    }

    private static SolidColorBrush Br(string key) =>
        (SolidColorBrush)System.Windows.Application.Current.Resources[key]!;
}
