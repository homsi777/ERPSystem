using ERPSystem.Application.DTOs.Accounting;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Accounting;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfApplication = System.Windows.Application;
using ERPSystem.Diagnostics.Performance;

namespace ERPSystem.Controls.Accounting;

public sealed class JournalBookCardControl : Border
{
    public JournalBookCardControl(JournalBookListDto book)
    {
        Width = 280;
        MinHeight = 140;
        Margin = new Thickness(0, 0, 12, 12);
        CornerRadius = new CornerRadius(10);
        BorderThickness = new Thickness(1);
        BorderBrush = Br("BorderBrush");
        Background = Br("SurfaceBrush");
        Effect = (System.Windows.Media.Effects.Effect)WpfApplication.Current.Resources["CardShadow"]!;
        Padding = new Thickness(16);

        var typeBadge = new Border
        {
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(8, 3, 8, 3),
            VerticalAlignment = VerticalAlignment.Top,
            Background = Br("PrimaryVeryLightBrush"),
            Child = new TextBlock
            {
                Text = book.BookTypeDisplay,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = Br("PrimaryBrush")
            }
        };

        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var nameStack = new StackPanel();
        nameStack.Children.Add(new TextBlock
        {
            Text = book.NameAr,
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        nameStack.Children.Add(new TextBlock
        {
            Text = book.Code,
            FontSize = 11,
            Margin = new Thickness(0, 2, 0, 0),
            Foreground = Br("TextMutedBrush")
        });
        Grid.SetColumn(nameStack, 0);
        Grid.SetColumn(typeBadge, 1);
        header.Children.Add(nameStack);
        header.Children.Add(typeBadge);

        Child = new StackPanel
        {
            Children =
            {
                header,
                new TextBlock
                {
                    Text = "دفتر يومية — للعرض فقط",
                    FontSize = 12,
                    Margin = new Thickness(0, 12, 0, 0),
                    Foreground = Br("TextSecondaryBrush")
                }
            }
        };
    }

    private static Brush Br(string key) => (Brush)WpfApplication.Current.Resources[key]!;
}

public sealed class JournalBookListPageControl : UserControl
{
    private readonly WrapPanel _cardsHost = new() { Margin = new Thickness(16) };
    private readonly TextBlock _recordCount = new() { FontSize = 11, Foreground = Br("TextMutedBrush") };
    private readonly Border _emptyState = new() { Visibility = Visibility.Collapsed };

    public JournalBookListPageControl()
    {
        Background = Br("AppBgBrush");
        Content = BuildLayout();
        Loaded += async (_, _) => await LoadAsync();
    }

    private UIElement BuildLayout()
    {
        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var header = new Border
        {
            Background = Br("SurfaceBrush"),
            BorderBrush = Br("BorderBrush"),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(16, 12, 16, 12),
            Child = ErpUiFactory.SectionTitle("دفاتر اليومية")
        };
        Grid.SetRow(header, 0);

        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = _cardsHost };
        Grid.SetRow(scroll, 1);

        _emptyState.Background = Br("SurfaceBrush");
        _emptyState.Padding = new Thickness(32);
        _emptyState.Child = ErpUxFactory.InfoBanner("لا توجد دفاتر يومية.", "warning");
        Grid.SetRow(_emptyState, 1);

        var footer = new Border
        {
            Background = Br("SurfaceBrush"),
            BorderBrush = Br("BorderBrush"),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(16, 8, 16, 8),
            Child = _recordCount
        };
        Grid.SetRow(footer, 2);

        root.Children.Add(header);
        root.Children.Add(scroll);
        root.Children.Add(_emptyState);
        root.Children.Add(footer);
        return root;
    }

    private async Task LoadAsync()
    {
        using var perfScope = ScreenLoadProfiler.Begin("Accounting.JournalBooks");
        if (!AppServices.IsInitialized)
            return;

        var result = await ScreenLoadProfiler.MeasureLoadAsync(perfScope, () => AccountingUiService.Instance.GetJournalBooksAsync());
        perfScope?.IncrementServiceCalls();
        if (!ApplicationResultPresenter.Present(result) || result.Value is null)
            return;

        _cardsHost.Children.Clear();
        foreach (var book in result.Value)
            _cardsHost.Children.Add(new JournalBookCardControl(book));

        var count = result.Value.Count;
        _recordCount.Text = count == 0 ? "لا توجد دفاتر" : $"{count} دفتر يومية";
        _emptyState.Visibility = count == 0 ? Visibility.Visible : Visibility.Collapsed;
        _cardsHost.Visibility = count == 0 ? Visibility.Collapsed : Visibility.Visible;
    }

    private static Brush Br(string key) => (Brush)WpfApplication.Current.Resources[key]!;
}
