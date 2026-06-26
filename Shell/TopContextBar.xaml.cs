using ERPSystem.Core;
using ERPSystem.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ERPSystem.Shell
{
    public partial class TopContextBar : UserControl
    {
        public event EventHandler? LanguageToggleRequested;
        public event EventHandler? SettingsRequested;

        private readonly DispatcherTimer _clockTimer;
        private bool _languageSubscribed;

        public TopContextBar()
        {
            InitializeComponent();
            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += (_, _) => UpdateClock();
            Loaded += OnLoaded;
            TxtSearch.KeyDown += TxtSearch_KeyDown;
        }

        private void TxtSearch_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != System.Windows.Input.Key.Enter) return;
            var q = TxtSearch.Text.Trim();
            if (string.IsNullOrEmpty(q))
            {
                MockInteractionService.ShowWarning("اكتب كلمة البحث ثم اضغط Enter.");
                return;
            }
            MockInteractionService.ShowInfo(
                $"نتائج تجريبية لـ «{q}»:\n• فاتورة SINV-1026\n• عميل: محل الأناقة\n• حاوية CN-2026-001",
                "بحث سريع");
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!_languageSubscribed)
            {
                LocalizationManager.Instance.LanguageChanged += OnLanguageChanged;
                _languageSubscribed = true;
            }
            InitializeBranchSelector();
            UpdateClock();
            _clockTimer.Start();
            UpdateLabels();
        }

        private void InitializeBranchSelector()
        {
            CmbBranch.Items.Add(LocalizationManager.Instance.IsArabic ? "الفرع الرئيسي" : "Main Branch");
            CmbBranch.Items.Add(LocalizationManager.Instance.IsArabic ? "فرع الشمال" : "North Branch");
            CmbBranch.Items.Add(LocalizationManager.Instance.IsArabic ? "فرع الجنوب" : "South Branch");
            CmbBranch.SelectedIndex = 0;
        }

        private void UpdateClock()
        {
            var now = DateTime.Now;
            var loc = LocalizationManager.Instance;

            if (loc.IsArabic)
            {
                TxtCurrentDate.Text = now.ToString("dddd، d MMMM yyyy",
                    new System.Globalization.CultureInfo("ar-SA"));
                TxtCurrentTime.Text = now.ToString("hh:mm:ss tt",
                    new System.Globalization.CultureInfo("ar-SA"));
            }
            else
            {
                TxtCurrentDate.Text = now.ToString("dddd, MMMM d, yyyy");
                TxtCurrentTime.Text = now.ToString("hh:mm:ss tt");
            }
        }

        private void OnLanguageChanged(object? sender, EventArgs e)
        {
            UpdateLabels();
            CmbBranch.Items.Clear();
            InitializeBranchSelector();
            UpdateClock();
        }

        private void UpdateLabels()
        {
            var loc = LocalizationManager.Instance;
            TxtCompanyName.Text = loc["CompanyName"];
            TxtBranchLabel.Text = loc.IsArabic ? "الفرع:" : "Branch:";
            TxtLangToggle.Text = loc["Language"];
            TxtSearch.Text = string.Empty;
            TxtUserDisplay.Text = loc["AdminUser"];
            TxtUserRoleDisplay.Text = loc.IsArabic ? "مسؤول" : "Admin";

            // Search placeholder handled via watermark
            var searchPlaceholder = loc["Search"];
            TxtSearch.Tag = searchPlaceholder;
        }

        private void BtnLanguage_Click(object sender, RoutedEventArgs e)
        {
            LocalizationManager.Instance.ToggleLanguage();
            LanguageToggleRequested?.Invoke(this, EventArgs.Empty);
        }

        private void BtnNotifications_Click(object sender, RoutedEventArgs e) =>
            MockInteractionService.ShowInfo(
                "• فاتورة INV-2026-0088 بانتظار التفصيل\n• حاوية CN-2026-001 بانتظار Landing Cost\n• 3 عملاء تجاوزوا حد الائتمان",
                "الإشعارات");

        private void BtnSettingsQuick_Click(object sender, RoutedEventArgs e) =>
            SettingsRequested?.Invoke(this, EventArgs.Empty);

        private void BtnUserProfile_Click(object sender, RoutedEventArgs e) =>
            MockInteractionService.ShowInfo(
                "مدير النظام\nمسؤول — شركة الحمصي لاستيراد الأقمشة\n\nتعديل الملف الشخصي سيتم تفعيله مع إدارة المستخدمين.",
                "الملف الشخصي");
    }
}
