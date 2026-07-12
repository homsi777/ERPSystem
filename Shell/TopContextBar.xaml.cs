using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Core;
using ERPSystem.Services;
using ERPSystem.Services.Search;
using ERPSystem.Services.Settings;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ERPSystem.Shell
{
    public partial class TopContextBar : UserControl
    {
        public event EventHandler? LanguageToggleRequested;
        public event EventHandler? SettingsRequested;

        private readonly DispatcherTimer _clockTimer;
        private bool _languageSubscribed;
        private List<BranchListItem> _branches = new();

        public TopContextBar()
        {
            InitializeComponent();
            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += (_, _) => UpdateClock();
            Loaded += OnLoaded;
            TxtSearch.KeyDown += TxtSearch_KeyDown;
            CmbBranch.SelectionChanged += CmbBranch_SelectionChanged;
        }

        private void CmbBranch_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var idx = CmbBranch.SelectedIndex;
            if (idx >= 0 && idx < _branches.Count)
                WpfCurrentBranchService.SelectBranch(_branches[idx].Id);
        }

        private Popup? _searchPopup;
        private ListBox? _searchList;
        private DispatcherTimer? _searchTimer;

        private void TxtSearch_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                CloseSearchPopup();
                return;
            }
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                _ = RunSearchAsync();
                return;
            }

            _searchTimer ??= new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            _searchTimer.Stop();
            _searchTimer.Tick -= OnSearchTick;
            _searchTimer.Tick += OnSearchTick;
            _searchTimer.Start();
        }

        private void OnSearchTick(object? sender, EventArgs e)
        {
            _searchTimer?.Stop();
            _ = RunSearchAsync();
        }

        private async Task RunSearchAsync()
        {
            var q = TxtSearch.Text?.Trim() ?? "";
            if (q.Length < 3)
            {
                CloseSearchPopup();
                return;
            }

            if (!AppServices.IsInitialized) return;

            IReadOnlyList<GlobalSearchResult> results;
            try { results = await GlobalSearchUiService.Instance.SearchAsync(q, 20); }
            catch { return; }

            ShowSearchResults(results);
        }

        private void EnsureSearchPopup()
        {
            if (_searchPopup is not null) return;

            _searchList = new ListBox { MaxHeight = 380, Width = 340 };

            _searchPopup = new Popup
            {
                PlacementTarget = TxtSearch,
                Placement = PlacementMode.Bottom,
                StaysOpen = false,
                AllowsTransparency = true,
                Child = new Border
                {
                    Background = (Brush)System.Windows.Application.Current.Resources["SurfaceBrush"]!,
                    BorderBrush = (Brush)System.Windows.Application.Current.Resources["BorderBrush"]!,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Child = _searchList
                }
            };
        }

        private void ShowSearchResults(IReadOnlyList<GlobalSearchResult> results)
        {
            EnsureSearchPopup();
            _searchList!.Items.Clear();

            if (results.Count == 0)
            {
                _searchList.Items.Add(new ListBoxItem { Content = "لا توجد نتائج", IsEnabled = false });
                _searchPopup!.IsOpen = true;
                return;
            }

            foreach (var r in results)
            {
                var panel = new StackPanel { Orientation = Orientation.Horizontal };
                panel.Children.Add(new TextBlock
                {
                    Text = r.Icon,
                    FontFamily = new FontFamily("Segoe MDL2 Assets"),
                    FontSize = 14,
                    Margin = new Thickness(0, 0, 8, 0),
                    VerticalAlignment = VerticalAlignment.Center
                });
                var texts = new StackPanel();
                texts.Children.Add(new TextBlock { Text = r.DisplayText, FontSize = 13 });
                texts.Children.Add(new TextBlock
                {
                    Text = r.SecondaryText,
                    FontSize = 11,
                    Foreground = (Brush)System.Windows.Application.Current.Resources["TextMutedBrush"]!
                });
                panel.Children.Add(texts);
                _searchList.Items.Add(new ListBoxItem { Content = panel, Tag = r, DataContext = r });
            }

            // Bind selection back to the GlobalSearchResult via item DataContext.
            _searchList.SelectionChanged -= SearchList_SelectionChanged;
            _searchList.SelectionChanged += SearchList_SelectionChanged;
            _searchPopup!.IsOpen = true;
        }

        private void SearchList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_searchList?.SelectedItem is ListBoxItem { DataContext: GlobalSearchResult r })
            {
                NavigateToResult(r);
                CloseSearchPopup();
            }
        }

        private static void NavigateToResult(GlobalSearchResult r)
        {
            switch (r.EntityType)
            {
                case "SalesInvoice":
                    MockInteractionService.OpenInvoiceOperationsCenter(r.DisplayText);
                    break;
                case "Customer":
                    MockInteractionService.Navigate(AppModule.Customers, "List");
                    break;
                case "Supplier":
                    MockInteractionService.Navigate(AppModule.Suppliers, "List");
                    break;
                case "PurchaseInvoice":
                    MockInteractionService.Navigate(AppModule.Purchases, "Invoices");
                    break;
                case "Container":
                    MockInteractionService.Navigate(AppModule.ChinaImport, "Containers");
                    break;
                case "Account":
                    MockInteractionService.Navigate(AppModule.Accounting, "Chart");
                    break;
            }
        }

        private void CloseSearchPopup()
        {
            if (_searchPopup is not null)
                _searchPopup.IsOpen = false;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!_languageSubscribed)
            {
                LocalizationManager.Instance.LanguageChanged += OnLanguageChanged;
                _languageSubscribed = true;
            }
            InitializeBranchSelector();
            LoadCompanyLogo();
            UpdateClock();
            _clockTimer.Start();
            UpdateLabels();
        }

        private async void InitializeBranchSelector()
        {
            CmbBranch.Items.Clear();

            if (!AppServices.IsInitialized)
            {
                CmbBranch.Items.Add(LocalizationManager.Instance.IsArabic ? "الفرع الرئيسي" : "Main Branch");
                CmbBranch.SelectedIndex = 0;
                CmbBranch.IsEnabled = false;
                return;
            }

            try
            {
                var branches = await SettingsUiService.Instance.GetBranchesAsync();
                _branches = branches.ToList();

                if (_branches.Count == 0)
                {
                    CmbBranch.Items.Add(LocalizationManager.Instance.IsArabic ? "الفرع الرئيسي" : "Main Branch");
                    CmbBranch.SelectedIndex = 0;
                    CmbBranch.IsEnabled = false;
                    return;
                }

                foreach (var b in _branches)
                {
                    var name = LocalizationManager.Instance.IsArabic || string.IsNullOrWhiteSpace(b.NameEn)
                        ? b.NameAr
                        : b.NameEn;
                    CmbBranch.Items.Add(name);
                }
                CmbBranch.SelectedIndex = 0;
                CmbBranch.IsEnabled = _branches.Count > 1;
            }
            catch
            {
                CmbBranch.Items.Add(LocalizationManager.Instance.IsArabic ? "الفرع الرئيسي" : "Main Branch");
                CmbBranch.SelectedIndex = 0;
                CmbBranch.IsEnabled = false;
            }
        }

        private void UpdateClock()
        {
            var now = DateTime.Now;
            var loc = LocalizationManager.Instance;

            if (loc.IsArabic)
            {
                TxtCurrentDate.Text = now.ToString("dddd، d MMMM yyyy", AppCulture.ArabicWithLatinDigits);
                TxtCurrentTime.Text = now.ToString("hh:mm:ss tt", AppCulture.ArabicWithLatinDigits);
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

        private void LoadCompanyLogo()
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Brand", "company-logo.png");
            if (!File.Exists(path))
            {
                ImgCompanyLogo.Visibility = Visibility.Collapsed;
                LogoFallback.Visibility = Visibility.Visible;
                return;
            }

            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(path, UriKind.Absolute);
            image.EndInit();
            image.Freeze();

            ImgCompanyLogo.Source = image;
            ImgCompanyLogo.Visibility = Visibility.Visible;
            LogoFallback.Visibility = Visibility.Collapsed;
        }

        private void UpdateLabels()
        {
            var loc = LocalizationManager.Instance;
            TxtCompanyName.Text = loc["CompanyName"];
            TxtCompanyTagline.Text = loc["CompanyTagline"];
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
            MockInteractionService.ShowInfo("لا توجد إشعارات حالياً.", "الإشعارات");

        private void BtnSettingsQuick_Click(object sender, RoutedEventArgs e) =>
            SettingsRequested?.Invoke(this, EventArgs.Empty);

        private void BtnUserProfile_Click(object sender, RoutedEventArgs e)
        {
            var company = LocalizationManager.Instance["CompanyName"];
            MockInteractionService.ShowInfo(
                $"مدير النظام\nمسؤول — {company}\n\nتعديل الملف الشخصي سيتم تفعيله مع إدارة المستخدمين.",
                "الملف الشخصي");
        }
    }
}
