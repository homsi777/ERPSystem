using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Services.Identity;
using System.Windows;
using System.Windows.Threading;

namespace ERPSystem.Services;

public sealed class WpfSessionMonitor
{
    private readonly WpfCurrentUserService _currentUser;
    private readonly DispatcherTimer _timer;
    private bool _isChecking;

    public WpfSessionMonitor(WpfCurrentUserService currentUser)
    {
        _currentUser = currentUser;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _timer.Tick += OnTick;
    }

    public void Start()
    {
        if (_currentUser.SessionId == Guid.Empty)
            return;

        _timer.Start();
    }

    public void Stop() => _timer.Stop();

    private async void OnTick(object? sender, EventArgs e)
    {
        if (_isChecking || _currentUser.SessionId == Guid.Empty)
            return;

        _isChecking = true;
        try
        {
            var active = await AuthUiService.Instance.IsSessionActiveAsync(_currentUser.SessionId);
            if (active)
                return;

            _timer.Stop();
            _currentUser.ClearSession();

            MessageBox.Show(
                "تم تسجيل الدخول إلى حسابك من جهاز أو متصفح آخر.\nسيتم إغلاق الجلسة الحالية.",
                "انتهت الجلسة",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            if (System.Windows.Application.Current.MainWindow is not null)
                System.Windows.Application.Current.Shutdown();
        }
        catch
        {
            // Ignore transient network/db errors; retry on next tick.
        }
        finally
        {
            _isChecking = false;
        }
    }
}
