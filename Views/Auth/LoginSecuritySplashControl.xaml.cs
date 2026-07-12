using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ERPSystem.Views.Auth;

public partial class LoginSecuritySplashControl : UserControl
{
    private readonly List<StepRowViewModel> _rows = [];
    private int _activeIndex = -1;
    private DispatcherTimer? _timer;

    public event EventHandler? Completed;

    public LoginSecuritySplashControl()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public void BeginSequence()
    {
        _rows.Clear();
        foreach (var step in LoginSecuritySteps.All)
            _rows.Add(new StepRowViewModel(step.Text, step.Icon));

        StepsList.ItemsSource = _rows;
        _activeIndex = -1;
        UpdateProgress();
        StartNextStep();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        TryLoadLogo();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        Unloaded -= OnUnloaded;
        _timer?.Stop();
    }

    private void TryLoadLogo()
    {
        try
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Brand", "company-logo.png");
            if (File.Exists(path))
                ImgLogo.Source = new BitmapImage(new Uri(path, UriKind.Absolute));
        }
        catch
        {
            // Optional logo.
        }
    }

    private void StartNextStep()
    {
        _activeIndex++;
        if (_activeIndex >= LoginSecuritySteps.All.Length)
        {
            Completed?.Invoke(this, EventArgs.Empty);
            return;
        }

        for (var i = 0; i < _rows.Count; i++)
        {
            var row = _rows[i];
            if (i < _activeIndex)
                row.SetState(StepVisualState.Done);
            else if (i == _activeIndex)
                row.SetState(StepVisualState.Active);
            else
                row.SetState(StepVisualState.Pending);
        }

        UpdateProgress();
        RefreshList();

        var delay = _activeIndex == LoginSecuritySteps.All.Length - 1
            ? LoginSecuritySteps.StepDelayMs + LoginSecuritySteps.FinalStepExtraMs
            : LoginSecuritySteps.StepDelayMs;

        _timer?.Stop();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(delay) };
        _timer.Tick += (_, _) =>
        {
            _timer.Stop();
            if (_activeIndex < _rows.Count)
                _rows[_activeIndex].SetState(StepVisualState.Done);
            RefreshList();
            StartNextStep();
        };
        _timer.Start();
    }

    private void UpdateProgress()
    {
        var total = LoginSecuritySteps.All.Length;
        var done = Math.Max(0, _activeIndex);
        var ratio = total == 0 ? 0 : (double)done / total;
        var track = (ProgressFill.Parent as Border)?.ActualWidth ?? 420;
        if (track <= 0) track = 420;
        ProgressFill.Width = Math.Max(40, track * ratio);
    }

    private void RefreshList()
    {
        StepsList.ItemsSource = null;
        StepsList.ItemsSource = _rows;
    }

    private enum StepVisualState
    {
        Pending,
        Active,
        Done
    }

    private sealed class StepRowViewModel(string text, string icon)
    {
        public string Text { get; private set; } = text;
        public string Icon { get; private set; } = icon;
        public Brush ForegroundBrush { get; private set; } = new SolidColorBrush(Color.FromRgb(100, 116, 139));
        public Visibility PulseVisibility { get; private set; } = Visibility.Collapsed;

        public void SetState(StepVisualState state)
        {
            switch (state)
            {
                case StepVisualState.Active:
                    ForegroundBrush = Brushes.White;
                    PulseVisibility = Visibility.Visible;
                    break;
                case StepVisualState.Done:
                    ForegroundBrush = new SolidColorBrush(Color.FromRgb(167, 243, 208));
                    Icon = "✓";
                    PulseVisibility = Visibility.Collapsed;
                    break;
                default:
                    ForegroundBrush = new SolidColorBrush(Color.FromRgb(100, 116, 139));
                    PulseVisibility = Visibility.Collapsed;
                    break;
            }
        }
    }
}
