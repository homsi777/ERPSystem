using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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
        DetachFromParent(content);
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

        DetachFromParent(content);
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

    private static void DetachFromParent(UIElement content)
    {
        if (content is not FrameworkElement fe || fe.Parent is null)
            return;

        switch (fe.Parent)
        {
            case Panel panel:
                panel.Children.Remove(fe);
                break;
            case ContentControl cc when ReferenceEquals(cc.Content, fe):
                cc.Content = null;
                break;
            case Decorator decorator when ReferenceEquals(decorator.Child, fe):
                decorator.Child = null;
                break;
        }
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
