using ERPSystem.Application.DTOs.Expenses;
using ERPSystem.Services;
using ERPSystem.Services.Finance;
using System.Windows;
using System.Windows.Controls;

namespace ERPSystem.Controls.Finance;

internal static class CashboxDropdownBinder
{
    public static async Task<bool> LoadAsync(ComboBox combo, Guid? selectId = null)
    {
        var result = await FinanceUiService.Instance.GetCashboxesAsync();
        if (!ApplicationResultPresenter.Present(result))
            return false;

        var list = result.Value ?? [];
        var previous = selectId ?? combo.SelectedValue as Guid?;

        combo.ItemsSource = list;
        combo.DisplayMemberPath = nameof(CashboxOptionDto.Display);
        combo.SelectedValuePath = nameof(CashboxOptionDto.Id);

        if (previous is Guid id && list.Any(b => b.Id == id))
            combo.SelectedValue = id;
        else if (list.Count > 0)
            combo.SelectedIndex = 0;

        if (list.Count == 0)
            MockInteractionService.ShowWarning(
                "لا توجد صناديق نشطة. أنشئ صندوقاً من: المالية ← الصناديق.",
                "الصناديق");

        return list.Count > 0;
    }

    public static void WireRefresh(ComboBox combo)
    {
        void OnRefresh(object? _, EventArgs __) => _ = LoadAsync(combo, combo.SelectedValue as Guid?);

        void OnVisible(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is true && combo.IsLoaded)
                _ = LoadAsync(combo, combo.SelectedValue as Guid?);
        }

        CashboxListRefreshHub.RefreshRequested += OnRefresh;
        combo.IsVisibleChanged += OnVisible;
        combo.Unloaded += (_, _) =>
        {
            CashboxListRefreshHub.RefreshRequested -= OnRefresh;
            combo.IsVisibleChanged -= OnVisible;
        };
    }
}
