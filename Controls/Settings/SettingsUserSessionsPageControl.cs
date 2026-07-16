using ERPSystem.Application.DTOs.Identity;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Identity;
using System.Windows;
using System.Windows.Controls;

namespace ERPSystem.Controls.Settings;

public sealed class SettingsUserSessionsPageControl : UserControl
{
    private readonly DataGrid _grid = new()
    {
        AutoGenerateColumns = false,
        IsReadOnly = true,
        Margin = new Thickness(8),
        CanUserAddRows = false,
        CanUserDeleteRows = false
    };

    public SettingsUserSessionsPageControl()
    {
        BuildColumns();
        Content = ErpUiFactory.Card(_grid);
        Loaded += OnLoaded;
    }

    private void BuildColumns()
    {
        _grid.Columns.Add(new DataGridTextColumn { Header = "المستخدم", Binding = new System.Windows.Data.Binding(nameof(UserSessionStatusDto.Username)), Width = 120 });
        _grid.Columns.Add(new DataGridTextColumn { Header = "الاسم", Binding = new System.Windows.Data.Binding(nameof(UserSessionStatusDto.FullNameAr)), Width = 160 });
        _grid.Columns.Add(new DataGridTextColumn { Header = "نوع الدخول", Binding = new System.Windows.Data.Binding(nameof(UserSessionStatusDto.ClientTypeDisplay)), Width = 130 });
        _grid.Columns.Add(new DataGridTextColumn { Header = "الحالة", Binding = new System.Windows.Data.Binding(nameof(UserSessionStatusDto.StatusDisplay)), Width = 80 });
        _grid.Columns.Add(new DataGridTextColumn
        {
            Header = "تسجيل الدخول",
            Binding = new System.Windows.Data.Binding(nameof(UserSessionStatusDto.LoginAt))
            {
                StringFormat = "yyyy-MM-dd HH:mm"
            },
            Width = 140
        });
        _grid.Columns.Add(new DataGridTextColumn
        {
            Header = "تسجيل الخروج",
            Binding = new System.Windows.Data.Binding(nameof(UserSessionStatusDto.LogoutAt))
            {
                StringFormat = "yyyy-MM-dd HH:mm"
            },
            Width = 140
        });
        _grid.Columns.Add(new DataGridTextColumn { Header = "الجهاز", Binding = new System.Windows.Data.Binding(nameof(UserSessionStatusDto.DeviceInfo)), Width = 180 });
        _grid.Columns.Add(new DataGridTextColumn { Header = "IP", Binding = new System.Windows.Data.Binding(nameof(UserSessionStatusDto.IpAddress)), Width = 120 });
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        if (!AppServices.IsInitialized)
            return;

        try
        {
            var rows = await UserSessionUiService.Instance.GetHistoryAsync();
            _grid.ItemsSource = rows;
        }
        catch (Exception ex)
        {
            _grid.ItemsSource = null;
            MockInteractionService.ShowWarning("تعذّر تحميل حالة المستخدمين: " + ex.Message, "حالة المستخدمين");
        }
    }
}
