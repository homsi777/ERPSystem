using ERPSystem.Controls.Reports;
using ERPSystem.Core;
using System.Windows;
using System.Windows.Controls;

namespace ERPSystem.Views.Reports;

public static class ModuleReportsViews
{
    public static UserControl CreateHub(AppModule module) => Wrap(new ModuleReportsHubControl(module));

    private static UserControl Wrap(UIElement content)
    {
        if (content is FrameworkElement fe)
            fe.HorizontalAlignment = HorizontalAlignment.Stretch;

        return new UserControl
        {
            Content = content,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch
        };
    }
}
