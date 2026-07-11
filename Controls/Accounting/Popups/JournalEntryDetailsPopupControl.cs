using ERPSystem.Application.DTOs.Accounting;
using ERPSystem.Domain.Enums;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Accounting;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfApplication = System.Windows.Application;

namespace ERPSystem.Controls.Accounting.Popups;

public sealed class JournalEntryDetailsPopupControl : UserControl
{
    private readonly Guid _entryId;
    private readonly StackPanel _root = new();
    private readonly StackPanel _actions = new() { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 12, 0, 0) };

    public JournalEntryDetailsPopupControl(Guid entryId)
    {
        _entryId = entryId;
        Content = _root;
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (!AppServices.IsInitialized) return;
        var result = await AccountingUiService.Instance.GetJournalDetailsAsync(_entryId);
        if (!ApplicationResultPresenter.Present(result) || result.Value is null) return;
        await RenderAsync(result.Value);
    }

    private async Task RenderAsync(JournalEntryDetailsDto data)
    {
        _root.Children.Clear();
        _actions.Children.Clear();

        _root.Children.Add(BuildKpiRow(
            ("الحالة", data.StatusDisplay, "SurfaceAltBrush", false),
            ("مدين", data.DebitTotal.ToString("N2", CultureInfo.InvariantCulture), ErpAccountingColorHelper.DebitTintBrushKey, true),
            ("دائن", data.CreditTotal.ToString("N2", CultureInfo.InvariantCulture), ErpAccountingColorHelper.CreditTintBrushKey, true),
            ("المصدر", data.SourceTypeDisplay ?? "—", "PrimaryBrush", false)));

        _root.Children.Add(ErpUiFactory.Card(ErpUiFactory.BuildFormGrid(
            ("رقم القيد", ReadOnly(data.EntryNumber)),
            ("التاريخ", ReadOnly(data.EntryDate.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture))),
            ("البيان", ReadOnly(data.Description)),
            ("تاريخ الترحيل", ReadOnly(data.PostedAt?.ToString("yyyy/MM/dd HH:mm", CultureInfo.InvariantCulture) ?? "—")))));

        _root.Children.Add(ErpUiFactory.SectionTitle("بنود القيد"));
        var grid = new DataGrid { AutoGenerateColumns = false, IsReadOnly = true, MaxHeight = 260 };
        ErpDataGridHelper.ApplyEnterpriseStyle(grid);
        ErpUiFactory.AddGridColumn(grid, "الحساب", nameof(JournalEntryLineDetailsDto.AccountCode), 90);
        ErpUiFactory.AddGridColumn(grid, "الاسم", nameof(JournalEntryLineDetailsDto.AccountName), "*");
        ErpAccountingColorHelper.AddDebitColumn(grid, "مدين", nameof(JournalEntryLineDetailsDto.Debit), 90, "N2");
        ErpAccountingColorHelper.AddCreditColumn(grid, "دائن", nameof(JournalEntryLineDetailsDto.Credit), 90, "N2");
        ErpUiFactory.AddGridColumn(grid, "البيان", nameof(JournalEntryLineDetailsDto.Narrative), "*");
        grid.ItemsSource = data.Lines.ToList();
        _root.Children.Add(ErpUiFactory.Card(grid));

        await BuildActionsAsync(data);
        _root.Children.Add(_actions);
    }

    private async Task BuildActionsAsync(JournalEntryDetailsDto data)
    {
        if (data.Status == JournalEntryStatus.Draft &&
            await AccountingUiService.Instance.CanPostJournalAsync())
        {
            AddAction("اعتماد", S("PrimaryButtonStyle"), async () =>
            {
                var result = await AccountingUiService.Instance.ApproveJournalAsync(data.Id);
                if (ApplicationResultPresenter.Present(result))
                {
                    AccountingListRefreshHub.RequestRefresh();
                    await LoadAsync();
                }
            });
        }

        if (data.Status == JournalEntryStatus.Approved &&
            await AccountingUiService.Instance.CanPostJournalAsync())
        {
            AddAction("ترحيل", S("PrimaryButtonStyle"), async () =>
            {
                var result = await AccountingUiService.Instance.PostJournalAsync(data.Id);
                if (ApplicationResultPresenter.Present(result))
                {
                    AccountingListRefreshHub.RequestRefresh();
                    await LoadAsync();
                }
            });
        }

        if (data.Status == JournalEntryStatus.Posted &&
            await AccountingUiService.Instance.CanReverseJournalAsync())
        {
            AddAction("عكس القيد", S("SecondaryButtonStyle"), async () =>
            {
                if (MessageBox.Show("إنشاء قيد عكسي لهذا القيد المرحّل؟", "تأكيد",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    return;

                var result = await AccountingUiService.Instance.ReverseJournalAsync(data.Id);
                if (ApplicationResultPresenter.Present(result))
                {
                    AccountingListRefreshHub.RequestRefresh();
                    MockInteractionService.ShowSuccess("تم إنشاء القيد العكسي.");
                    await LoadAsync();
                }
            });
        }

        if (data.Status is JournalEntryStatus.Draft or JournalEntryStatus.Approved &&
            await AccountingUiService.Instance.CanCreateJournalAsync())
        {
            AddAction("إلغاء", S("GhostButtonStyle"), async () =>
            {
                if (MessageBox.Show("إلغاء هذا القيد؟", "تأكيد",
                        MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                    return;

                var result = await AccountingUiService.Instance.CancelJournalAsync(data.Id);
                if (ApplicationResultPresenter.Present(result))
                {
                    AccountingListRefreshHub.RequestRefresh();
                    await LoadAsync();
                }
            });
        }
    }

    private void AddAction(string label, Style style, Func<Task> handler)
    {
        var btn = new Button
        {
            Content = label,
            Style = style,
            Height = 34,
            MinWidth = 100,
            Margin = new Thickness(0, 0, 8, 0)
        };
        btn.Click += async (_, _) => await handler();
        _actions.Children.Add(btn);
    }

    private static UIElement BuildKpiRow(params (string title, string value, string brush, bool tintedBackground)[] items)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
        foreach (var (title, value, brush, tintedBackground) in items)
        {
            row.Children.Add(new Border
            {
                Background = (Brush)WpfApplication.Current.Resources[tintedBackground ? brush : "SurfaceAltBrush"]!,
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 0, 8, 0),
                MinWidth = 100,
                Child = new StackPanel
                {
                    Children =
                    {
                        new TextBlock
                        {
                            Text = title,
                            FontSize = 11,
                            Foreground = (Brush)WpfApplication.Current.Resources["TextMutedBrush"]!
                        },
                        new TextBlock
                        {
                            Text = value,
                            FontSize = 14,
                            FontWeight = FontWeights.SemiBold,
                            Margin = new Thickness(0, 4, 0, 0),
                            Foreground = (Brush)WpfApplication.Current.Resources[tintedBackground ? "TextPrimaryBrush" : brush]!
                        }
                    }
                }
            });
        }
        return row;
    }

    private static TextBlock ReadOnly(string text) => new()
    {
        Text = text,
        FontSize = 13,
        Foreground = (Brush)WpfApplication.Current.Resources["TextPrimaryBrush"]!
    };

    private static Style S(string key) => (Style)WpfApplication.Current.Resources[key]!;
}
