using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ERPSystem.Helpers
{
    public static class PlaceholderUi
    {
        public static UIElement DatabasePhase(string featureName) => new Border
        {
            Background = Br("InfoBgBrush"),
            BorderBrush = Br("InfoBorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(20),
            Margin = new Thickness(0, 0, 0, 12),
            Child = new StackPanel
            {
                Children =
                {
                    new TextBlock
                    {
                        Text = featureName,
                        FontWeight = FontWeights.SemiBold,
                        FontSize = 14,
                        Foreground = Br("TextPrimaryBrush"),
                        FontFamily = Ff()
                    },
                    new TextBlock
                    {
                        Text = "هذه الميزة مخططة — ستُفعّل في مرحلة ربط PostgreSQL",
                        Foreground = Br("TextSecondaryBrush"),
                        Margin = new Thickness(0, 6, 0, 0),
                        TextWrapping = TextWrapping.Wrap,
                        FontFamily = Ff()
                    }
                }
            }
        };

        public static UIElement TabContent(string title, UIElement? body = null)
        {
            var stack = new StackPanel();
            stack.Children.Add(ErpUiFactory.SectionTitle(title));
            stack.Children.Add(body ?? DatabasePhase(title));
            return new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = stack,
                Padding = new Thickness(4)
            };
        }

        public static UIElement MockGrid(object[] rows) =>
            ErpUiFactory.Card(ErpUiFactory.BuildGrid(rows));

        public static UIElement EmptyMessage(string message, string? subtitle = null)
        {
            var stack = new StackPanel { Margin = new Thickness(8) };
            stack.Children.Add(new TextBlock
            {
                Text = message,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = Br("TextPrimaryBrush"),
                TextWrapping = TextWrapping.Wrap,
                FontFamily = Ff()
            });
            if (!string.IsNullOrWhiteSpace(subtitle))
            {
                stack.Children.Add(new TextBlock
                {
                    Text = subtitle,
                    FontSize = 12,
                    Foreground = Br("TextSecondaryBrush"),
                    Margin = new Thickness(0, 6, 0, 0),
                    TextWrapping = TextWrapping.Wrap,
                    FontFamily = Ff()
                });
            }
            return ErpUiFactory.Card(stack);
        }

        public static UIElement DevelopmentPhase(string? featureName = null) =>
            EmptyMessage(
                featureName ?? "قيد التطوير",
                "سيتم ربط هذه الشاشة بقاعدة البيانات لاحقاً");

        private static Brush Br(string k) => (Brush)System.Windows.Application.Current.Resources[k]!;
        private static FontFamily Ff() => new("Segoe UI, Tahoma, Arial");
    }
}
