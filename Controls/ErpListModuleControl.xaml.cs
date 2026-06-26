using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ERPSystem.Controls
{
    /// <summary>Table-first list page — no KPI strips. Analytics belong in Operations Centers.</summary>
    public partial class ErpListModuleControl : UserControl
    {
        public event EventHandler? PrimaryActionRequested;
        public event EventHandler<string>? SearchChanged;

        private Func<object, bool>? _searchFilter;
        private Func<object, string, bool>? _searchMatcher;
        private Func<object, bool>? _extraFilter;
        private List<object>? _allItems;
        private string _emptyMessage = "لا توجد سجلات";
        private string? _emptyAction;

        public ErpListModuleControl()
        {
            InitializeComponent();
        }

        public DataGrid Grid => MainGrid;

        public void Configure(EntityType entityType, AppModule sourceModule)
        {
            RowContextMenuService.SetEntityType(MainGrid, entityType);
            RowContextMenuService.SetSourceModule(MainGrid, sourceModule);
        }

        public void SetHeader(string title, string subtitle, string iconGlyph, Brush accent) { }

        /// <summary>Deprecated — KPIs belong in Operations Centers, not list pages.</summary>
        public void SetSummaryCards(IReadOnlyList<(string title, string value, string icon, SolidColorBrush color)> cards) { }

        public void SetExtraFilter(Func<object, bool>? filter)
        {
            _extraFilter = filter;
            ApplyFilters();
        }

        public void SetPrimaryButton(string text)
        {
            TxtPrimaryBtn.Text = text;
            _emptyAction = text;
        }

        public void SetFilterExtras(params UIElement[] extras)
        {
            FilterExtrasPanel.Children.Clear();
            foreach (var el in extras)
            {
                if (el is FrameworkElement fe)
                    fe.Margin = new Thickness(0, 0, 8, 0);
                FilterExtrasPanel.Children.Add(el);
            }
        }

        public void SetEmptyState(string message, string? primaryAction = null, string icon = "\uE8A5")
        {
            _emptyMessage = message;
            _emptyAction = primaryAction;
            TxtEmptyMessage.Text = message;
            TxtEmptyIcon.Text = icon;
            UpdateEmptyActionButton();
        }

        public void SetFilterSummary(string? text) => TxtFilterSummary.Text = text ?? "";

        public void SetSearchFilter(Func<object, bool> filter) => _searchFilter = filter;

        public void SetSearchMatcher(Func<object, string, bool> matcher) => _searchMatcher = matcher;

        public void BindData(IEnumerable<object> items)
        {
            _allItems = items.ToList();
            ApplyFilters();
        }

        public void RefreshBinding(IEnumerable<object> items) => BindData(items);

        public void ApplyFilters()
        {
            if (_allItems == null) return;

            var term = TxtSearch.Text.Trim();
            IEnumerable<object> result = _allItems;

            if (!string.IsNullOrEmpty(term))
            {
                if (_searchMatcher != null)
                    result = result.Where(i => _searchMatcher(i, term));
                else
                    result = result.Where(i =>
                        (_searchFilter?.Invoke(i) ?? false) ||
                        i.ToString()?.Contains(term, StringComparison.OrdinalIgnoreCase) == true);
            }

            if (_extraFilter != null)
                result = result.Where(_extraFilter);

            var list = result.ToList();
            MainGrid.ItemsSource = list;
            UpdateRecordCount(list.Count, _allItems.Count);
            UpdateEmptyState(list.Count);
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            SearchChanged?.Invoke(this, TxtSearch.Text);
            ApplyFilters();
        }

        private void UpdateEmptyState(int count)
        {
            EmptyStatePanel.Visibility = count == 0 ? Visibility.Visible : Visibility.Collapsed;
            MainGrid.Visibility = count == 0 ? Visibility.Collapsed : Visibility.Visible;
            if (count == 0)
            {
                TxtEmptyMessage.Text = _emptyMessage;
                UpdateEmptyActionButton();
            }
        }

        private void UpdateEmptyActionButton()
        {
            if (!string.IsNullOrEmpty(_emptyAction))
            {
                BtnEmptyAction.Content = _emptyAction;
                BtnEmptyAction.Visibility = Visibility.Visible;
            }
            else
            {
                BtnEmptyAction.Visibility = Visibility.Collapsed;
            }
        }

        public void UpdateRecordCount(int? shown = null, int? total = null)
        {
            var count = MainGrid.Items?.Count ?? 0;
            TxtRecordCount.Text = shown.HasValue && total.HasValue
                ? $"عرض {shown} من {total} سجل"
                : $"عرض {count} سجل";
        }

        private void BtnPrimary_Click(object sender, RoutedEventArgs e) =>
            PrimaryActionRequested?.Invoke(this, EventArgs.Empty);

        private void BtnExport_Click(object sender, RoutedEventArgs e) =>
            MockInteractionService.ShowDocumentPreview("قائمة البيانات", "Excel");
    }
}
