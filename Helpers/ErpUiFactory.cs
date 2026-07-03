using ERPSystem.Controls;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace ERPSystem.Helpers
{
    /// <summary>Unified programmatic UI builders — all cards, badges, and sections share one visual language.</summary>
    public static class ErpUiFactory
    {
        public static TextBlock SectionTitle(string text) => new()
        {
            Text = text,
            FontSize = ErpDesignTokens.FontSectionTitle,
            FontWeight = FontWeights.SemiBold,
            Margin = ErpDesignTokens.SectionBottom,
            FontFamily = ErpDesignTokens.UiFont,
            Foreground = Br("TextPrimaryBrush")
        };

        /// <summary>Standard card — radius 8, padding 12, CardShadow, white surface.</summary>
        public static Border Card(UIElement content, Thickness? margin = null) => new()
        {
            Background = Br("SurfaceBrush"),
            BorderBrush = Br("BorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = ErpDesignTokens.Radius,
            Padding = ErpDesignTokens.CardPadding,
            Margin = margin ?? ErpDesignTokens.CardBottomMargin,
            Effect = CardEffect(),
            Child = content
        };

        /// <summary>Removes an element from its logical parent so it can be hosted elsewhere.</summary>
        public static void DetachFromVisualTree(UIElement element)
        {
            if (element is not FrameworkElement fe || fe.Parent is null)
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

        public static Border IconBadge(string glyph, Brush accent, Brush? accentLight = null, double size = ErpDesignTokens.IconBadgeSize)
            => new()
            {
                Width = size,
                Height = size,
                CornerRadius = new CornerRadius(ErpDesignTokens.IconBadgeRadius),
                Background = accentLight ?? Br("PrimaryVeryLightBrush"),
                Child = new TextBlock
                {
                    Text = glyph,
                    FontFamily = ErpDesignTokens.IconFont,
                    FontSize = size >= 44 ? 20 : 16,
                    Foreground = accent,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };

        public static StackPanel BuildFilterRow(params (string Label, UIElement Control)[] filters)
        {
            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, ErpDesignTokens.SpaceSm)
            };
            foreach (var (label, control) in filters)
            {
                var sp = new StackPanel { Margin = new Thickness(0, 0, ErpDesignTokens.SpaceMd, 0) };
                sp.Children.Add(new TextBlock
                {
                    Text = label,
                    FontSize = ErpDesignTokens.FontCaption,
                    Foreground = Br("TextMutedBrush"),
                    Margin = new Thickness(0, 0, 0, ErpDesignTokens.SpaceXs),
                    FontFamily = ErpDesignTokens.UiFont
                });
                sp.Children.Add(control);
                row.Children.Add(sp);
            }
            row.Children.Add(new Button
            {
                Content = "تصفير",
                Style = S("GhostButtonStyle"),
                VerticalAlignment = VerticalAlignment.Bottom
            });
            return row;
        }

        public static ComboBox FilterCombo(string[] items, double width = 130) => new()
        {
            Width = width,
            Height = ErpDesignTokens.ControlHeight,
            ItemsSource = items,
            SelectedIndex = 0,
            FontSize = ErpDesignTokens.FontBody - 1,
            Style = S("EnterpriseComboBoxStyle")
        };

        public static DataGrid BuildGrid(IEnumerable? items = null, bool autoColumns = true)
        {
            var dg = new DataGrid { AutoGenerateColumns = autoColumns, IsReadOnly = true, ItemsSource = items };
            ErpDataGridHelper.ApplyEnterpriseStyle(dg);
            return dg;
        }

        public static void AddGridColumn(DataGrid grid, string header, string path, object width, string? format = null)
        {
            var binding = new Binding(path);
            if (format != null) binding.StringFormat = format;
            grid.Columns.Add(new DataGridTextColumn
            {
                Header = header,
                Binding = binding,
                Width = width is string
                    ? new DataGridLength(1, DataGridLengthUnitType.Star)
                    : new DataGridLength(Convert.ToDouble(width))
            });
        }

        public static void SetSummaryCards(StackPanel panel, IEnumerable<(string title, string value, string icon, SolidColorBrush color)> cards)
        {
            panel.Children.Clear();
            foreach (var c in cards)
            {
                panel.Children.Add(new MetricCardControl
                {
                    CardTitle = c.title,
                    CardValue = c.value,
                    CardIcon = c.icon,
                    AccentColor = c.color,
                    Margin = new Thickness(0, 0, ErpDesignTokens.CardGap, 0)
                });
            }
        }

        public static Border StatusBadge(string text, string kind = "neutral")
        {
            var (bg, fg) = kind switch
            {
                "success" => ("SuccessBgBrush", "SuccessBrush"),
                "warning" => ("WarningBgBrush", "WarningBrush"),
                "danger" => ("DangerBgBrush", "DangerBrush"),
                "info" => ("InfoBgBrush", "InfoBrush"),
                _ => ("SurfaceAltBrush", "TextSecondaryBrush")
            };
            return new Border
            {
                Background = Br(bg),
                CornerRadius = new CornerRadius(100),
                Padding = new Thickness(8, 3, 8, 3),
                Child = new TextBlock
                {
                    Text = text,
                    Foreground = Br(fg),
                    FontSize = ErpDesignTokens.FontCaption - 1,
                    FontWeight = FontWeights.SemiBold,
                    FontFamily = ErpDesignTokens.UiFont
                }
            };
        }

        public static StackPanel EmptyState(string message, string? actionLabel = null, string icon = "\uE8A5")
        {
            var sp = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(ErpDesignTokens.SpaceXl)
            };
            sp.Children.Add(new TextBlock
            {
                Text = icon,
                FontFamily = ErpDesignTokens.IconFont,
                FontSize = 28,
                Foreground = Br("TextMutedBrush"),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, ErpDesignTokens.SpaceSm)
            });
            sp.Children.Add(new TextBlock
            {
                Text = message,
                FontSize = ErpDesignTokens.FontBody,
                Foreground = Br("TextSecondaryBrush"),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                FontFamily = ErpDesignTokens.UiFont
            });
            if (!string.IsNullOrEmpty(actionLabel))
            {
                sp.Children.Add(new Button
                {
                    Content = actionLabel,
                    Style = S("PrimaryButtonStyle"),
                    Margin = new Thickness(0, ErpDesignTokens.SpaceMd, 0, 0)
                });
            }
            return sp;
        }

        public static Grid BuildFormGrid(params (string Label, UIElement Field)[] fields)
        {
            var grid = new Grid { Margin = new Thickness(0, 0, 0, ErpDesignTokens.SpaceSm) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(ErpDesignTokens.SpaceLg) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            for (int i = 0; i < fields.Length; i++)
            {
                int row = i / 2;
                while (grid.RowDefinitions.Count <= row)
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var sp = new StackPanel { Margin = new Thickness(0, 0, 0, ErpDesignTokens.SpaceSm) };
                sp.Children.Add(new TextBlock
                {
                    Text = fields[i].Label,
                    FontSize = ErpDesignTokens.FontCaption,
                    Foreground = Br("TextSecondaryBrush"),
                    Margin = new Thickness(0, 0, 0, ErpDesignTokens.SpaceXs),
                    FontFamily = ErpDesignTokens.UiFont
                });
                sp.Children.Add(fields[i].Field);
                Grid.SetRow(sp, row);
                Grid.SetColumn(sp, i % 2 == 0 ? 0 : 2);
                grid.Children.Add(sp);
            }
            return grid;
        }

        public static TextBox FormField(string? text = null) => new()
        {
            Text = text ?? "",
            Height = ErpDesignTokens.ControlHeight,
            Style = S("EnterpriseInputStyle"),
            FontSize = ErpDesignTokens.FontBody - 1
        };

        public static DatePicker FormDate(DateTime? date = null) => new()
        {
            SelectedDate = date ?? DateTime.Today,
            Height = ErpDesignTokens.ControlHeight,
            Width = 160,
            Style = S("EnterpriseDatePickerStyle")
        };

        private static System.Windows.Media.Effects.Effect? CardEffect() =>
            System.Windows.Application.Current.Resources["CardShadow"] as System.Windows.Media.Effects.Effect;

        private static Brush Br(string k) => (Brush)System.Windows.Application.Current.Resources[k]!;
        private static Style S(string k) => (Style)System.Windows.Application.Current.Resources[k]!;
    }
}
