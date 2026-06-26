using System.Windows;
using System.Windows.Controls;

namespace ERPSystem.Controls
{
    public partial class SectionHeaderControl : UserControl
    {
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(SectionHeaderControl),
                new PropertyMetadata("Section", OnTitleChanged));

        public static readonly DependencyProperty SubtitleProperty =
            DependencyProperty.Register(nameof(Subtitle), typeof(string), typeof(SectionHeaderControl),
                new PropertyMetadata("", OnSubtitleChanged));

        public static readonly DependencyProperty ActionTextProperty =
            DependencyProperty.Register(nameof(ActionText), typeof(string), typeof(SectionHeaderControl),
                new PropertyMetadata("", OnActionChanged));

        public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
        public string Subtitle { get => (string)GetValue(SubtitleProperty); set => SetValue(SubtitleProperty, value); }
        public string ActionText { get => (string)GetValue(ActionTextProperty); set => SetValue(ActionTextProperty, value); }

        public event RoutedEventHandler? ActionClicked;

        public SectionHeaderControl()
        {
            InitializeComponent();
        }

        private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SectionHeaderControl ctrl)
                ctrl.TxtTitle.Text = (string)e.NewValue;
        }

        private static void OnSubtitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SectionHeaderControl ctrl)
            {
                ctrl.TxtSubtitle.Text = (string)e.NewValue;
                ctrl.TxtSubtitle.Visibility = string.IsNullOrEmpty((string)e.NewValue)
                    ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        private static void OnActionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SectionHeaderControl ctrl)
            {
                ctrl.BtnAction.Content = (string)e.NewValue;
                ctrl.BtnAction.Visibility = string.IsNullOrEmpty((string)e.NewValue)
                    ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        private void BtnAction_Click(object sender, RoutedEventArgs e)
        {
            ActionClicked?.Invoke(this, e);
        }
    }
}
