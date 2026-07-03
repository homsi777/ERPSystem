using ERPSystem.Application.DTOs.Expenses;
using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Core.Domain;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Finance;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ERPSystem.Controls.Accounting;

public sealed class CashboxListPageControl : UserControl
{
    private readonly ErpListModuleControl _page = new();
    private bool _isLoading;

    public CashboxListPageControl()
    {
        _page.Configure(EntityType.Cashbox, AppModule.Accounting);
        _page.SetHeader("الصناديق", "مراكز عمليات الصناديق — تحصيل وصرف", "\uE8C1", B("AccentReceivableBrush"));
        _page.SetPrimaryButton("صندوق جديد");
        _page.SetEmptyState("لا توجد صناديق مضافة", "صندوق جديد", "\uE8C1");
        _page.PrimaryActionRequested += (_, _) =>
            MockInteractionService.ShowComingSoon("إضافة صندوق");

        var g = _page.Grid;
        g.AutoGenerateColumns = false;
        foreach (var (h, p, w) in new (string, string, object)[]
        {
            ("الكود", nameof(Cashbox.Code), 90),
            ("الاسم", nameof(Cashbox.Name), "*"),
            ("العملة", nameof(Cashbox.Currency), 80)
        })
        {
            ErpUiFactory.AddGridColumn(g, h, p, w, null);
        }

        Content = _page;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (_isLoading || !AppServices.IsInitialized)
        {
            _page.BindData([]);
            return;
        }

        _isLoading = true;
        _page.SetLoadingState(true);
        try
        {
            var result = await FinanceUiService.Instance.GetCashboxesAsync();
            if (!result.IsSuccess || result.Value is null || result.Value.Count == 0)
            {
                _page.BindData([]);
                return;
            }

            var rows = result.Value
                .Select(MapCashbox)
                .Cast<object>()
                .ToList();
            _page.BindData(rows);
        }
        finally
        {
            _page.SetLoadingState(false);
            _isLoading = false;
        }
    }

    private static Cashbox MapCashbox(CashboxOptionDto dto) => new()
    {
        Code = dto.Code,
        Name = dto.Name,
        Balance = 0,
        Currency = "ر.س"
    };

    private static SolidColorBrush B(string k) => (SolidColorBrush)System.Windows.Application.Current.Resources[k]!;
}
