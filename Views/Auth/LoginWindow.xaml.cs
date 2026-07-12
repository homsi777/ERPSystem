using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Services;
using ERPSystem.Services.Identity;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace ERPSystem.Views.Auth;

public partial class LoginWindow : Window
{
    private bool _isSubmitting;

    public LoginWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        TryLoadLogo();
        TxtUsername.Focus();
    }

    private void TryLoadLogo()
    {
        try
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Brand", "company-logo.png");
            if (File.Exists(path))
                ImgLogo.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(path, UriKind.Absolute));
        }
        catch
        {
            // Logo is optional for login.
        }
    }

    private async void BtnLogin_OnClick(object sender, RoutedEventArgs e) => await SubmitAsync();

    private async void TxtPassword_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            await SubmitAsync();
    }

    private async Task SubmitAsync()
    {
        if (_isSubmitting || !AppServices.IsInitialized)
            return;

        var username = TxtUsername.Text?.Trim() ?? "";
        var password = TxtPassword.Password;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            ShowError("اسم المستخدم وكلمة المرور مطلوبان.");
            return;
        }

        _isSubmitting = true;
        BtnLogin.IsEnabled = false;
        TxtError.Visibility = Visibility.Collapsed;

        try
        {
            var result = await AuthUiService.Instance.LoginAsync(username, password);
            if (!result.IsSuccess || result.Value is null)
            {
                ShowError(result.ErrorMessage ?? "اسم المستخدم أو كلمة المرور غير صحيحة.");
                return;
            }

            var user = result.Value;
            var currentUser = AppServices.GetRequiredService<ICurrentUserService>() as WpfCurrentUserService;
            currentUser?.SetSession(user.UserId, user.Username, user.FullNameAr);

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            ShowError("تعذّر تسجيل الدخول: " + ex.Message);
        }
        finally
        {
            _isSubmitting = false;
            BtnLogin.IsEnabled = true;
        }
    }

    private void ShowError(string message)
    {
        TxtError.Text = message;
        TxtError.Visibility = Visibility.Visible;
    }
}
