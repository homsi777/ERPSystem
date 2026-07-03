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

public sealed class CapitalPartnerTimelinePopupControl : UserControl
{
    private readonly Guid _partnerId;
    private readonly StackPanel _list = new();

    public CapitalPartnerTimelinePopupControl(Guid partnerId)
    {
        _partnerId = partnerId;
        Content = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = _list
        };
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (!AppServices.IsInitialized) return;
        var result = await CapitalPartnerUiService.Instance.GetTimelineAsync(_partnerId);
        if (!ApplicationResultPresenter.Present(result)) return;

        _list.Children.Clear();
        var events = result.Value ?? Array.Empty<PartnerTimelineEventDto>();
        if (events.Count == 0)
        {
            _list.Children.Add(new TextBlock
            {
                Text = "لا توجد أحداث مسجّلة.",
                Foreground = (Brush)WpfApplication.Current.Resources["TextMutedBrush"]!,
                Margin = new Thickness(4)
            });
            return;
        }

        foreach (var ev in events)
            _list.Children.Add(BuildEventCard(ev));
    }

    private static Border BuildEventCard(PartnerTimelineEventDto ev) => ErpUiFactory.Card(new StackPanel
    {
        Children =
        {
            new TextBlock
            {
                Text = ev.Title,
                FontWeight = FontWeights.SemiBold,
                FontSize = 13,
                Foreground = (Brush)WpfApplication.Current.Resources["TextPrimaryBrush"]!
            },
            new TextBlock
            {
                Text = ev.Description ?? "",
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 4, 0, 0),
                Foreground = (Brush)WpfApplication.Current.Resources["TextSecondaryBrush"]!
            },
            new TextBlock
            {
                Text = $"{ev.Timestamp.ToString("yyyy/MM/dd HH:mm", CultureInfo.InvariantCulture)} • {ev.UserName}",
                FontSize = 11,
                Margin = new Thickness(0, 8, 0, 0),
                Foreground = (Brush)WpfApplication.Current.Resources["TextMutedBrush"]!
            }
        }
    }, new Thickness(0, 0, 0, 8));
}
