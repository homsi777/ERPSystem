using System.ComponentModel;

namespace ERPSystem.Core
{
    public enum AppModule
    {
        Dashboard,
        ChinaImport,
        Sales,
        Purchases,
        Inventory,
        Customers,
        Suppliers,
        Accounting,
        Reports,
        HR,
        Settings
    }

    public class NavigationRequest
    {
        public AppModule Module { get; init; }
        public string SubPage { get; init; } = "";

        public NavigationRequest(AppModule module, string subPage = "")
        {
            Module = module;
            SubPage = subPage;
        }

        public override string ToString() => string.IsNullOrEmpty(SubPage) ? $"{Module}" : $"{Module}/{SubPage}";
    }

    public class NavigationStateManager : INotifyPropertyChanged
    {
        private static NavigationStateManager? _instance;
        public static NavigationStateManager Instance => _instance ??= new NavigationStateManager();

        private AppModule _currentModule = AppModule.Dashboard;
        private string _currentSubPage = "";

        public AppModule CurrentModule
        {
            get => _currentModule;
            private set
            {
                if (_currentModule != value)
                {
                    _currentModule = value;
                    OnPropertyChanged(nameof(CurrentModule));
                }
            }
        }

        public string CurrentSubPage
        {
            get => _currentSubPage;
            private set
            {
                if (_currentSubPage != value)
                {
                    _currentSubPage = value;
                    OnPropertyChanged(nameof(CurrentSubPage));
                }
            }
        }

        private readonly Stack<NavigationRequest> _history = new();

        public event EventHandler<NavigationRequest>? Navigated;
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public void NavigateTo(AppModule module, string subPage = "")
        {
            if (CurrentModule != AppModule.Dashboard)
                _history.Push(new NavigationRequest(CurrentModule, CurrentSubPage));

            CurrentModule = module;
            CurrentSubPage = subPage;
            Navigated?.Invoke(this, new NavigationRequest(module, subPage));
        }

        public void NavigateBack()
        {
            if (_history.Count > 0)
            {
                var prev = _history.Pop();
                CurrentModule = prev.Module;
                CurrentSubPage = prev.SubPage;
                Navigated?.Invoke(this, prev);
            }
        }

        public bool CanNavigateBack => _history.Count > 0;

        public static string GetModuleIcon(AppModule module) => module switch
        {
            AppModule.Dashboard   => "\uE80F",
            AppModule.ChinaImport => "\uE7BF",
            AppModule.Sales       => "\uE8F1",
            AppModule.Purchases   => "\uE7BF",
            AppModule.Inventory   => "\uE821",
            AppModule.Customers   => "\uE716",
            AppModule.Suppliers   => "\uE779",
            AppModule.Accounting  => "\uE8C1",
            AppModule.Reports     => "\uE9D2",
            AppModule.HR          => "\uE716",
            AppModule.Settings    => "\uE713",
            _                     => "\uE80F"
        };
    }
}
