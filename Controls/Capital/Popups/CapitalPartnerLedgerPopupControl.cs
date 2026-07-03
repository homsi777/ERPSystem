using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.DTOs.Capital;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Capital;
using System.Windows;
using System.Windows.Controls;

namespace ERPSystem.Controls.Capital.Popups;

public sealed class CapitalPartnerLedgerPopupControl : UserControl
{
    private readonly Guid _partnerId;
    private readonly DataGrid _grid = new() { AutoGenerateColumns = false, IsReadOnly = true, MinHeight = 280 };

    public CapitalPartnerLedgerPopupControl(Guid partnerId)
    {
        _partnerId = partnerId;
        ErpUiFactory.AddGridColumn(_grid, "التاريخ", nameof(CapitalTransactionListDto.TransactionDate), 95, "yyyy/MM/dd");
        ErpUiFactory.AddGridColumn(_grid, "النوع", nameof(CapitalTransactionListDto.TypeDisplay), 120);
        ErpUiFactory.AddGridColumn(_grid, "المبلغ", nameof(CapitalTransactionListDto.AmountOriginal), 100, "N2");
        ErpUiFactory.AddGridColumn(_grid, "العملة", nameof(CapitalTransactionListDto.Currency), 60);
        ErpUiFactory.AddGridColumn(_grid, "بالأساس", nameof(CapitalTransactionListDto.SignedBaseAmount), 110, "N2");
        ErpUiFactory.AddGridColumn(_grid, "البيان", nameof(CapitalTransactionListDto.Notes), "*");
        Content = ErpUiFactory.Card(_grid);
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (!AppServices.IsInitialized) return;
        var result = await CapitalPartnerUiService.Instance.GetTransactionsAsync(
            new CapitalTransactionListFilter { PartnerId = _partnerId }, 1, 500);
        if (!ApplicationResultPresenter.Present(result)) return;
        _grid.ItemsSource = result.Value?.Items ?? Array.Empty<CapitalTransactionListDto>();
    }
}
