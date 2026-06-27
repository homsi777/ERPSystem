using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ERPSystem.Controls
{
    public enum MetricTrend { Up, Down, Neutral }

    public partial class MetricCardControl : UserControl
    {
        public static readonly DependencyProperty CardTitleProperty =
            DependencyProperty.Register(nameof(CardTitle), typeof(string), typeof(MetricCardControl),
                new PropertyMetadata("Title", OnPropertyUpdated));

        public static readonly DependencyProperty CardValueProperty =
            DependencyProperty.Register(nameof(CardValue), typeof(string), typeof(MetricCardControl),
                new PropertyMetadata("0", OnPropertyUpdated));

        public static readonly DependencyProperty CardIconProperty =
            DependencyProperty.Register(nameof(CardIcon), typeof(string), typeof(MetricCardControl),
                new PropertyMetadata("\uE80F", OnPropertyUpdated));

        public static readonly DependencyProperty CardDescriptionProperty =
            DependencyProperty.Register(nameof(CardDescription), typeof(string), typeof(MetricCardControl),
                new PropertyMetadata("", OnPropertyUpdated));

        public static readonly DependencyProperty TrendValueProperty =
            DependencyProperty.Register(nameof(TrendValue), typeof(string), typeof(MetricCardControl),
                new PropertyMetadata("", OnPropertyUpdated));

        public static readonly DependencyProperty TrendDirectionProperty =
            DependencyProperty.Register(nameof(TrendDirection), typeof(MetricTrend), typeof(MetricCardControl),
                new PropertyMetadata(MetricTrend.Neutral, OnPropertyUpdated));

        public static readonly DependencyProperty AccentColorProperty =
            DependencyProperty.Register(nameof(AccentColor), typeof(Brush), typeof(MetricCardControl),
                new PropertyMetadata(new SolidColorBrush(Color.FromRgb(37, 99, 235)), OnPropertyUpdated));

        public string CardTitle { get => (string)GetValue(CardTitleProperty); set => SetValue(CardTitleProperty, value); }
        public string CardValue { get => (string)GetValue(CardValueProperty); set => SetValue(CardValueProperty, value); }
        public string CardIcon { get => (string)GetValue(CardIconProperty); set => SetValue(CardIconProperty, value); }
        public string CardDescription { get => (string)GetValue(CardDescriptionProperty); set => SetValue(CardDescriptionProperty, value); }
        public string TrendValue { get => (string)GetValue(TrendValueProperty); set => SetValue(TrendValueProperty, value); }
        public MetricTrend TrendDirection { get => (MetricTrend)GetValue(TrendDirectionProperty); set => SetValue(TrendDirectionProperty, value); }
        public Brush AccentColor { get => (Brush)GetValue(AccentColorProperty); set => SetValue(AccentColorProperty, value); }

        public MetricCardControl()
        {
            InitializeComponent();
            Loaded += (_, _) => UpdateDisplay();
        }

        private static void OnPropertyUpdated(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MetricCardControl card)
                card.UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            TxtTitle.Text = CardTitle;
            TxtValue.Text = CardValue;
            TxtIcon.Text = CardIcon;
            TxtDescription.Text = CardDescription;

            // Apply accent color
            var iconBg = new SolidColorBrush(GetLightColor(AccentColor));
            IconBorder.Background = iconBg;
            TxtIcon.Foreground = AccentColor;
            AccentBar.Background = AccentColor;

            // Trend
            if (!string.IsNullOrEmpty(TrendValue))
            {
                TrendBorder.Visibility = Visibility.Visible;
                TxtTrend.Text = TrendValue;

                switch (TrendDirection)
                {
                    case MetricTrend.Up:
                        TrendBorder.Background = (SolidColorBrush)System.Windows.Application.Current.Resources["SuccessBgBrush"]!;
                        TxtTrend.Foreground = (SolidColorBrush)System.Windows.Application.Current.Resources["SuccessBrush"]!;
                        break;
                    case MetricTrend.Down:
                        TrendBorder.Background = (SolidColorBrush)System.Windows.Application.Current.Resources["DangerBgBrush"]!;
                        TxtTrend.Foreground = (SolidColorBrush)System.Windows.Application.Current.Resources["DangerBrush"]!;
                        break;
                    default:
                        TrendBorder.Background = (SolidColorBrush)System.Windows.Application.Current.Resources["BorderLightBrush"]!;
                        TxtTrend.Foreground = (SolidColorBrush)System.Windows.Application.Current.Resources["TextMutedBrush"]!;
                        break;
                }
            }
            else
            {
                TrendBorder.Visibility = Visibility.Collapsed;
            }
        }

        private static Color GetLightColor(Brush brush)
        {
            if (brush is SolidColorBrush solid)
            {
                var c = solid.Color;
                // Mix with white to get 10% opacity effect
                return Color.FromRgb(
                    (byte)(c.R + (255 - c.R) * 0.88),
                    (byte)(c.G + (255 - c.G) * 0.88),
                    (byte)(c.B + (255 - c.B) * 0.88)
                );
            }
            return Colors.LightBlue;
        }
    }
}
