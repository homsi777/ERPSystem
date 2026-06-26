using System.Windows;
using System.Windows.Media;

namespace ERPSystem.Dialogs
{
    public enum MockFeedbackKind { Success, Warning, Info, ComingSoon }

    public partial class MockFeedbackDialog : Window
    {
        public MockFeedbackDialog()
        {
            InitializeComponent();
        }

        public static void Show(MockFeedbackKind kind, string message, string title, string? subtitle = null)
        {
            var dlg = new MockFeedbackDialog();
            dlg.TxtTitle.Text = title;
            dlg.TxtMessage.Text = message;
            dlg.TxtSubtitle.Text = subtitle ?? "";
            dlg.TxtSubtitle.Visibility = string.IsNullOrEmpty(subtitle) ? Visibility.Collapsed : Visibility.Visible;

            switch (kind)
            {
                case MockFeedbackKind.Success:
                    dlg.ApplyIcon("\uE73E", "#ECFDF5", "#10B981");
                    break;
                case MockFeedbackKind.Warning:
                    dlg.ApplyIcon("\uE7BA", "#FFFBEB", "#F59E0B");
                    break;
                case MockFeedbackKind.ComingSoon:
                    dlg.ApplyIcon("\uE823", "#EFF6FF", "#2563EB");
                    dlg.TxtSubtitle.Text = "سيتم تفعيلها في المرحلة التالية";
                    dlg.TxtSubtitle.Visibility = Visibility.Visible;
                    break;
                default:
                    dlg.ApplyIcon("\uE946", "#EFF6FF", "#2563EB");
                    break;
            }

            dlg.ShowDialog();
        }

        private void ApplyIcon(string glyph, string bgHex, string fgHex)
        {
            TxtIcon.Text = glyph;
            IconBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bgHex)!);
            TxtIcon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(fgHex)!);
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e) => Close();
    }
}
