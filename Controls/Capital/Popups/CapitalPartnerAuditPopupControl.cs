using ERPSystem.Application.DTOs.Capital;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Capital;
using System.Windows;
using System.Windows.Controls;

namespace ERPSystem.Controls.Capital.Popups;

public sealed class CapitalPartnerAuditPopupControl : UserControl
{
    private readonly Guid _partnerId;
    private readonly DataGrid _grid = new() { AutoGenerateColumns = false, IsReadOnly = true, MinHeight = 280 };

    public CapitalPartnerAuditPopupControl(Guid partnerId)
    {
        _partnerId = partnerId;
        ErpUiFactory.AddGridColumn(_grid, "الوقت", nameof(PartnerAuditEntryDto.Timestamp), 130, "yyyy/MM/dd HH:mm");
        ErpUiFactory.AddGridColumn(_grid, "الإجراء", nameof(PartnerAuditEntryDto.Action), 120);
        ErpUiFactory.AddGridColumn(_grid, "الحقل", nameof(PartnerAuditEntryDto.FieldName), 100);
        ErpUiFactory.AddGridColumn(_grid, "قبل", nameof(PartnerAuditEntryDto.PreviousValue), 90);
        ErpUiFactory.AddGridColumn(_grid, "بعد", nameof(PartnerAuditEntryDto.NewValue), 90);
        ErpUiFactory.AddGridColumn(_grid, "المستخدم", nameof(PartnerAuditEntryDto.UserName), 100);
        Content = ErpUiFactory.Card(_grid);
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (!AppServices.IsInitialized) return;
        var result = await CapitalPartnerUiService.Instance.GetAuditTrailAsync(_partnerId);
        if (!ApplicationResultPresenter.Present(result)) return;
        _grid.ItemsSource = result.Value ?? Array.Empty<PartnerAuditEntryDto>();
    }
}
