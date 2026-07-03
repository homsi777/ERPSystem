using ERPSystem.Helpers;
using System.Windows;
using System.Windows.Controls;

namespace ERPSystem.Dialogs;

public partial class ErpModalWindow : Window
{
    public ErpModalWindow()
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
    }

    public void Configure(string title, string subtitle, string iconGlyph, double width, double maxHeight = 680)
    {
        Width = width;
        MaxHeight = maxHeight;
        TxtTitle.Text = title;
        TxtSubtitle.Text = subtitle;
        TxtSubtitle.Visibility = string.IsNullOrWhiteSpace(subtitle)
            ? Visibility.Collapsed
            : Visibility.Visible;
        TxtIcon.Text = iconGlyph;
    }

    public void SetBody(UIElement content)
    {
        ErpUiFactory.DetachFromVisualTree(content);
        if (content is FrameworkElement fe)
            fe.Margin = new Thickness(20, 16, 20, 20);

        BodyHost.Content = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = content
        };
    }

    public static bool? Show(
        string title,
        string subtitle,
        UIElement content,
        string iconGlyph = "\uE8F1",
        double width = 520,
        double? maxHeight = 680)
    {
        var dlg = new ErpModalWindow
        {
            Width = width,
            MaxHeight = maxHeight ?? 680
        };
        dlg.TxtTitle.Text = title;
        dlg.TxtSubtitle.Text = subtitle;
        dlg.TxtSubtitle.Visibility = string.IsNullOrWhiteSpace(subtitle)
            ? Visibility.Collapsed
            : Visibility.Visible;
        dlg.TxtIcon.Text = iconGlyph;

        ErpUiFactory.DetachFromVisualTree(content);
        if (content is FrameworkElement fe)
            fe.Margin = new Thickness(20, 16, 20, 20);

        dlg.BodyHost.Content = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = content
        };

        return dlg.ShowDialog();
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
