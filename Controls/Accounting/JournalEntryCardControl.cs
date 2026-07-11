using ERPSystem.Application.DTOs.Accounting;
using ERPSystem.Domain.Enums;
using ERPSystem.Helpers;
using ERPSystem.Services.Accounting;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WpfApplication = System.Windows.Application;

namespace ERPSystem.Controls.Accounting;

public sealed class JournalEntryCardControl : Border
{
    private readonly JournalEntryListDto _entry;

    public JournalEntryCardControl(JournalEntryListDto entry)
    {
        _entry = entry;
        Width = 300;
        MinHeight = 168;
        Margin = new Thickness(0, 0, 12, 12);
        CornerRadius = new CornerRadius(10);
        BorderThickness = new Thickness(1);
        BorderBrush = Br("BorderBrush");
        Background = Br("SurfaceBrush");
        Effect = (System.Windows.Media.Effects.Effect)WpfApplication.Current.Resources["CardShadow"]!;
        Cursor = Cursors.Hand;
        Padding = new Thickness(16);

        var (statusBg, statusFg) = StatusBrushes(entry.Status);

        var statusBadge = new Border
        {
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(8, 3, 8, 3),
            VerticalAlignment = VerticalAlignment.Top,
            Background = Br(statusBg),
            Child = new TextBlock
            {
                Text = entry.StatusDisplay,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = Br(statusFg)
            }
        };

        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var titleStack = new StackPanel();
        titleStack.Children.Add(new TextBlock
        {
            Text = entry.EntryNumber,
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        titleStack.Children.Add(new TextBlock
        {
            Text = entry.EntryDate.ToString("yyyy/MM/dd"),
            FontSize = 11,
            Margin = new Thickness(0, 2, 0, 0),
            Foreground = Br("TextMutedBrush")
        });
        Grid.SetColumn(titleStack, 0);
        Grid.SetColumn(statusBadge, 1);
        header.Children.Add(titleStack);
        header.Children.Add(statusBadge);

        Child = new StackPanel
        {
            Children =
            {
                header,
                new TextBlock
                {
                    Text = string.IsNullOrWhiteSpace(entry.Description) ? "—" : entry.Description,
                    FontSize = 13,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    Margin = new Thickness(0, 10, 0, 8),
                    Foreground = Br("TextPrimaryBrush")
                },
                new Border
                {
                    Background = Br(ErpAccountingColorHelper.DebitTintBrushKey),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(8, 4, 8, 4),
                    Margin = new Thickness(0, 10, 0, 0),
                    Child = new TextBlock
                    {
                        Text = $"مدين: {entry.DebitTotal:N2}",
                        FontSize = 14,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = Br("TextPrimaryBrush")
                    }
                },
                new Border
                {
                    Background = Br(ErpAccountingColorHelper.CreditTintBrushKey),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(8, 4, 8, 4),
                    Margin = new Thickness(0, 4, 0, 0),
                    Child = new TextBlock
                    {
                        Text = $"دائن: {entry.CreditTotal:N2}",
                        FontSize = 14,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = Br("TextPrimaryBrush")
                    }
                },
                new TextBlock
                {
                    Text = entry.SourceTypeDisplay is null ? $"{entry.LineCount} سطر" : $"{entry.LineCount} سطر • {entry.SourceTypeDisplay}",
                    FontSize = 12,
                    Margin = new Thickness(0, 8, 0, 0),
                    Foreground = Br("TextSecondaryBrush")
                },
                new TextBlock
                {
                    Text = "انقر يميناً للمهام • انقر للتفاصيل",
                    FontSize = 10,
                    Foreground = Br("TextMutedBrush"),
                    Margin = new Thickness(0, 10, 0, 0)
                }
            }
        };

        MouseEnter += (_, _) => BorderBrush = Br("PrimaryBrush");
        MouseLeave += (_, _) => BorderBrush = Br("BorderBrush");
        MouseLeftButtonUp += (_, e) =>
        {
            if (e.ChangedButton == MouseButton.Left)
                AccountingPopupService.ShowJournalDetails(_entry);
        };
        PreviewMouseRightButtonDown += (_, e) =>
        {
            e.Handled = true;
            AccountingContextMenuService.ShowJournal(_entry, this);
        };
    }

    private static (string Bg, string Fg) StatusBrushes(JournalEntryStatus status) => status switch
    {
        JournalEntryStatus.Draft => ("WarningBgBrush", "WarningBrush"),
        JournalEntryStatus.Approved => ("InfoBgBrush", "InfoBrush"),
        JournalEntryStatus.Posted => ("SuccessBgBrush", "SuccessBrush"),
        JournalEntryStatus.Reversed => ("SurfaceAltBrush", "TextMutedBrush"),
        JournalEntryStatus.Cancelled => ("DangerBgBrush", "DangerBrush"),
        _ => ("SurfaceAltBrush", "TextSecondaryBrush")
    };

    private static Brush Br(string key) => (Brush)WpfApplication.Current.Resources[key]!;
}
