using ERPSystem.Core.Actions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace ERPSystem.Helpers
{
    public static class ErpUxFactory
    {
        public static (Brush Accent, Brush AccentLight, string Icon) EntityTheme(EntityType type) => type switch
        {
            EntityType.Customer => (B("AccentCustomersBrush"), B("PrimaryVeryLightBrush"), "\uE716"),
            EntityType.SalesInvoice => (B("AccentSalesBrush"), B("PrimaryVeryLightBrush"), "\uE9F9"),
            EntityType.FabricItem => (B("AccentInventoryBrush"), B("SuccessBgBrush"), "\uE821"),
            EntityType.Supplier => (B("AccentPayableBrush"), B("WarningBgBrush"), "\uE779"),
            EntityType.ImportContainer => (B("AccentOrdersBrush"), B("AccentOrdersLightBrush"), "\uE7BF"),
            EntityType.PurchaseInvoice => (B("AccentOrdersBrush"), B("WarningBgBrush"), "\uE7BF"),
            EntityType.Employee => (B("InfoBrush"), B("InfoBgBrush"), "\uE716"),
            EntityType.JournalEntry => (B("PrimaryBrush"), B("PrimaryVeryLightBrush"), "\uE8C1"),
            EntityType.Warehouse => (B("AccentInventoryBrush"), B("SuccessBgBrush"), "\uE8B7"),
            EntityType.Cashbox => (B("AccentReceivableBrush"), B("WarningBgBrush"), "\uE8C1"),
            EntityType.Expense => (B("AccentPayableBrush"), B("WarningBgBrush"), "\uE9D9"),
            _ => (B("PrimaryBrush"), B("PrimaryVeryLightBrush"), "\uE8A5")
        };

        /// <summary>Horizontal workflow stepper for import/sales pipelines.</summary>
        public static Border WorkflowStepper(params (string Label, bool Active, bool Done)[] steps)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, ErpDesignTokens.SpaceSm) };
            for (int i = 0; i < steps.Length; i++)
            {
                var (label, active, done) = steps[i];
                var dotBrush = done ? B("SuccessBrush") : active ? B("PrimaryBrush") : B("BorderBrush");
                var textBrush = active || done ? B("TextPrimaryBrush") : B("TextMutedBrush");

                var step = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 2, 0) };
                step.Children.Add(new Border
                {
                    Width = 24, Height = 24, CornerRadius = new CornerRadius(12),
                    Background = done ? B("SuccessBgBrush") : active ? B("PrimaryVeryLightBrush") : B("SurfaceAltBrush"),
                    BorderBrush = dotBrush, BorderThickness = new Thickness(2),
                    Child = new TextBlock
                    {
                        Text = done ? "\uE73E" : (i + 1).ToString(),
                        FontFamily = new FontFamily(done ? "Segoe MDL2 Assets" : "Segoe UI"),
                        FontSize = done ? 12 : 11,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = dotBrush,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                });
                step.Children.Add(new TextBlock
                {
                    Text = label,
                    Margin = new Thickness(8, 0, 16, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 12,
                    FontWeight = active ? FontWeights.SemiBold : FontWeights.Normal,
                    Foreground = textBrush,
                    FontFamily = new FontFamily("Segoe UI, Tahoma, Arial")
                });
                if (i < steps.Length - 1)
                    step.Children.Add(new TextBlock
                    {
                        Text = "\uE72A", FontFamily = new FontFamily("Segoe MDL2 Assets"),
                        Foreground = B("TextMutedBrush"), VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, 0, 8, 0), FontSize = 10
                    });
                row.Children.Add(step);
            }
            return ErpUiFactory.Card(row, new Thickness(0, 0, 0, 12));
        }

        public static StackPanel ActionToolbar(string documentTitle, params (string Label, bool Primary)[] actions)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, ErpDesignTokens.SpaceSm) };
            foreach (var (label, primary) in actions.Take(6))
            {
                var capturedLabel = label;
                var btn = new Button
                {
                    Content = label,
                    Style = (Style)System.Windows.Application.Current.Resources[primary ? "PrimaryButtonStyle" : "SecondaryButtonStyle"]!,
                    Height = ErpDesignTokens.ControlHeight,
                    Padding = new Thickness(10, 0, 10, 0),
                    Margin = new Thickness(0, 0, 6, 0),
                    FontSize = 12
                };
                btn.Click += (_, _) => Services.MockInteractionService.ShowDocumentPreview(documentTitle, capturedLabel);
                row.Children.Add(btn);
            }
            return row;
        }

        public static StackPanel ExportBar(string documentTitle = "تقرير") =>
            ActionToolbar(documentTitle,
                ("طباعة", false), ("PDF", false), ("Excel", false), ("معاينة", false));

        /// <summary>Report export bar wired to real handlers (print / pdf / excel).</summary>
        public static StackPanel ExportBar(string documentTitle, Action<string> onExport) =>
            ActionToolbar(
                ("طباعة", false, () => onExport("print")),
                ("PDF", false, () => onExport("pdf")),
                ("Excel", false, () => onExport("excel")));

        /// <summary>Toolbar wired to real callbacks instead of the mock preview.</summary>
        public static StackPanel ActionToolbar(params (string Label, bool Primary, Action OnClick)[] actions)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, ErpDesignTokens.SpaceSm) };
            foreach (var (label, primary, onClick) in actions.Take(6))
            {
                var btn = new Button
                {
                    Content = label,
                    Style = (Style)System.Windows.Application.Current.Resources[primary ? "PrimaryButtonStyle" : "SecondaryButtonStyle"]!,
                    Height = ErpDesignTokens.ControlHeight,
                    Padding = new Thickness(10, 0, 10, 0),
                    Margin = new Thickness(0, 0, 6, 0),
                    FontSize = 12
                };
                btn.Click += (_, _) => onClick();
                row.Children.Add(btn);
            }
            return row;
        }

        public static Border InfoBanner(string text, string tone = "info")
        {
            var (bg, border, fg) = tone switch
            {
                "warning" => ("WarningBgBrush", "WarningBorderBrush", "WarningBrush"),
                "success" => ("SuccessBgBrush", "SuccessBorderBrush", "SuccessBrush"),
                _ => ("InfoBgBrush", "InfoBorderBrush", "InfoBrush")
            };
            return new Border
            {
                Background = B(bg), BorderBrush = B(border), BorderThickness = new Thickness(1),
                CornerRadius = ErpDesignTokens.Radius, Padding = ErpDesignTokens.CardPadding,
                Margin = new Thickness(0, 0, 0, ErpDesignTokens.SpaceMd),
                Child = new TextBlock
                {
                    Text = text, TextWrapping = TextWrapping.Wrap,
                    Foreground = B(fg), FontSize = 13,
                    FontFamily = new FontFamily("Segoe UI, Tahoma, Arial")
                }
            };
        }

        public static Border KpiStrip(params (string Label, string Value)[] items)
        {
            var grid = new UniformGrid { Columns = items.Length, Margin = new Thickness(0) };
            foreach (var (label, value) in items)
            {
                var cell = new StackPanel { Margin = new Thickness(8) };
                cell.Children.Add(new TextBlock
                {
                    Text = label, FontSize = 11, Foreground = B("TextMutedBrush"),
                    FontFamily = new FontFamily("Segoe UI, Tahoma, Arial")
                });
                cell.Children.Add(new TextBlock
                {
                    Text = value, FontSize = 18, FontWeight = FontWeights.Bold,
                    Foreground = B("TextPrimaryBrush"),
                    FontFamily = new FontFamily("Segoe UI, Tahoma, Arial"),
                    Margin = new Thickness(0, 4, 0, 0)
                });
                grid.Children.Add(cell);
            }
            return ErpUiFactory.Card(grid, new Thickness(0, 0, 0, 12));
        }

        private static Brush B(string key) => (Brush)System.Windows.Application.Current.Resources[key]!;
        private static Style S(string key) => (Style)System.Windows.Application.Current.Resources[key]!;
    }
}
