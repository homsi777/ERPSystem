using ERPSystem.Application.Common;
using ERPSystem.Application.DTOs.Containers;
using ERPSystem.Controls;
using ERPSystem.Core;
using ERPSystem.Domain.Enums;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.China;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WpfApplication = System.Windows.Application;

namespace ERPSystem.Controls.China;

public sealed class PackingListGroupAnalysisRow
{
    public int GroupIndex { get; init; }
    public string FabricCode { get; init; } = "";
    public string Color { get; init; } = "";
    public int DeclaredRolls { get; init; }
    public decimal DeclaredMeters { get; init; }
    public int ParsedRolls { get; init; }
    public decimal ParsedMeters { get; init; }
    public string RollsIndicator { get; init; } = "";
    public string MetersIndicator { get; init; } = "";
    public string CatalogStatus { get; init; } = "";

    public static PackingListGroupAnalysisRow FromDto(PackingListGroupDto dto) => new()
    {
        GroupIndex = dto.GroupIndex,
        FabricCode = dto.FabricCode,
        Color = dto.Color,
        DeclaredRolls = dto.DeclaredTotalRolls,
        DeclaredMeters = dto.DeclaredTotalMeters,
        ParsedRolls = dto.ParsedTotalRolls,
        ParsedMeters = dto.ParsedTotalMeters,
        RollsIndicator = dto.RollsMatchIndicator,
        MetersIndicator = dto.MetersMatchIndicator,
        CatalogStatus = dto.FabricResolved && dto.ColorResolved
            ? "✅"
            : $"⚠️ {dto.ResolutionError}"
    };
}

public sealed class PackingListIssueRow
{
    public int GroupIndex { get; init; }
    public string FabricCode { get; init; } = "";
    public string Color { get; init; } = "";
    public string RollLabel { get; init; } = "";
    public string Reason { get; init; } = "";

    public static IEnumerable<PackingListIssueRow> FromParseResult(ContainerExcelParseResultDto result) =>
        result.Groups
            .SelectMany(g => g.ResolutionIssues.Select(i => new PackingListIssueRow
            {
                GroupIndex = i.GroupIndex,
                FabricCode = i.FabricCode,
                Color = i.Color,
                RollLabel = i.RollNumber.HasValue ? i.RollNumber.Value.ToString() : "—",
                Reason = i.Reason
            }));
}

public sealed class PackingListRollAnalysisRow
{
    public int GroupIndex { get; init; }
    public string FabricCode { get; init; } = "";
    public string Color { get; init; } = "";
    public int RollNumber { get; init; }
    public decimal Meters { get; init; }
    public decimal NativeQuantity { get; init; }
    public DplQuantityUnit QuantityUnit { get; init; }
    public string QuantityDisplay { get; init; } = "";
    public string LotCode { get; init; } = "";
    public string StatusDisplay { get; init; } = "";

    public static IEnumerable<PackingListRollAnalysisRow> FromParseResult(ContainerExcelParseResultDto result) =>
        result.Groups.SelectMany(g => g.Rolls.Select(r => new PackingListRollAnalysisRow
        {
            GroupIndex = g.GroupIndex,
            FabricCode = g.FabricCode,
            Color = g.Color,
            RollNumber = r.RollNumber,
            Meters = r.QuantityMeters,
            NativeQuantity = r.QuantityNative,
            QuantityUnit = r.QuantityUnit,
            QuantityDisplay = r.QuantityDisplay,
            LotCode = string.IsNullOrWhiteSpace(r.LotCode) ? "—" : r.LotCode,
            StatusDisplay = r.IsValid ? "صحيح" : (r.InvalidReason ?? "خطأ")
        }));
}

public sealed class PackingListAnalysisControl : UserControl
{
    private readonly StackPanel _stack = new();
    private Button? _continueButton;

    public PackingListAnalysisControl()
    {
        var root = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Padding = new Thickness(16),
            Content = _stack
        };
        Content = root;
        Background = (SolidColorBrush)WpfApplication.Current.Resources["AppBgBrush"]!;
        Loaded += async (_, _) => await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        _stack.Children.Clear();
        _continueButton = null;

        _stack.Children.Add(ErpUiFactory.SectionTitle("الخطوة 2: تحليل الملف"));
        _stack.Children.Add(ErpUxFactory.WorkflowStepper(
            ("وصول الحاوية", true, true),
            ("تحليل الملف", true, true),
            ("إدخال التكلفة", false, false),
            ("Landing Cost", false, false),
            ("اعتماد", false, false),
            ("تحويل للمخزن", false, false),
            ("جاهز للبيع", false, false)));

        var parseResult = ChinaImportNavigationContext.GetParseResult();
        if (parseResult is null)
        {
            _stack.Children.Add(ErpUxFactory.InfoBanner(
                "لم يتم تحميل ملف للتحليل. ارجع إلى شاشة الاستيراد وارفع ملف DPL.", "warning"));
            var backOnly = new Button
            {
                Content = "العودة إلى الاستيراد",
                Style = (Style)WpfApplication.Current.Resources["SecondaryButtonStyle"]!,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 12, 0, 0)
            };
            backOnly.Click += (_, _) => ChinaImportNavigation.Navigate("NewImport");
            _stack.Children.Add(backOnly);
            return;
        }

        if (!ChinaImportNavigationContext.IsDplQuantityUnitConfirmed)
        {
            _stack.Children.Add(ErpUxFactory.InfoBanner(
                "يجب تحديد وحدة DPL (متر أو يارد) قبل تحليل الملف.", "warning"));
            var unitBtn = new Button
            {
                Content = "الانتقال — تحديد وحدة DPL",
                Style = (Style)WpfApplication.Current.Resources["PrimaryButtonStyle"]!,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 12, 0, 0)
            };
            unitBtn.Click += (_, _) => ChinaImportNavigation.Navigate("DplUnitSelection");
            _stack.Children.Add(unitBtn);
            return;
        }

        if (AppServices.IsInitialized)
            await ContainerUiService.Instance.RefreshMultiFileSessionAsync();

        var session = ChinaImportNavigationContext.GetMultiFileSession();

        _stack.Children.Add(new TextBlock
        {
            Text = $"الملف: {parseResult.FileName}",
            Foreground = (Brush)WpfApplication.Current.Resources["TextSecondaryBrush"]!,
            Margin = new Thickness(0, 0, 0, 4)
        });

        _stack.Children.Add(ErpUxFactory.InfoBanner(
            $"وحدة DPL المختارة: {parseResult.SelectedQuantityUnitDisplay} (تحويل يارد → متر: × {DplQuantityConverter.YardsToMetersFactor}) — التخزين والتكلفة بالمتر؛ البيع حسب القيمة الأصلية + الوحدة.",
            parseResult.SelectedQuantityUnit == DplQuantityUnit.Yards ? "warning" : "success"));

        var crossValidation = ChinaImportNavigationContext.CrossValidationResults;
        if (crossValidation.Count > 0)
        {
            _stack.Children.Add(ErpUiFactory.SectionTitle("التحقق من المطابقة — DPL مقابل فاتورة/PL (بالمتر)"));
            _stack.Children.Add(ErpUiFactory.Card(BuildCrossValidationPanel(crossValidation)));
        }

        if (!string.IsNullOrWhiteSpace(parseResult.SupplierNameFromFile))
        {
            _stack.Children.Add(new TextBlock
            {
                Text = $"المورد (من الملف): {parseResult.SupplierNameFromFile}",
                Foreground = (Brush)WpfApplication.Current.Resources["TextMutedBrush"]!,
                Margin = new Thickness(0, 0, 0, 12)
            });
        }

        _stack.Children.Add(ErpUiFactory.SectionTitle("الإجمالي الكلي للملف"));
        _stack.Children.Add(ErpUiFactory.Card(BuildGrandTotalPanel(parseResult.GrandTotal)));

        if (session is not null)
        {
            _stack.Children.Add(ErpUiFactory.SectionTitle("مطابقة الملفات الثلاثة (فاتورة + PL + DPL)"));
            _stack.Children.Add(ErpUxFactory.InfoBanner(session.CostingModeDisplay,
                session.UsesWeightedAllocation ? "success" : "warning"));

            if (session.Invoice?.TotalValidationWarning is not null)
                _stack.Children.Add(ErpUxFactory.InfoBanner(session.Invoice.TotalValidationWarning, "warning"));

            if (session.UnmatchedDplGroups.Count > 0)
            {
                _stack.Children.Add(ErpUxFactory.InfoBanner(
                    $"يوجد {session.UnmatchedDplGroups.Count} مجموعة DPL غير مربوطة ببنود الفاتورة/PL. اختر الربط الصحيح لكل مجموعة — سيتم حفظه لهذا المورد للشحنات القادمة.",
                    "warning"));
                _stack.Children.Add(ErpUiFactory.Card(BuildDplLinkPanel(session)));
            }

            if (session.TypeLines.Count > 0)
                _stack.Children.Add(ErpUiFactory.Card(BuildTypeLinesGrid(session.TypeLines)));
        }

        _stack.Children.Add(ErpUiFactory.SectionTitle("تحليل المجموعات (كود + لون)"));
        _stack.Children.Add(ErpUiFactory.Card(BuildGroupsGrid(parseResult)));

        var rollRows = PackingListRollAnalysisRow.FromParseResult(parseResult).ToList();
        _stack.Children.Add(ErpUiFactory.SectionTitle($"تفاصيل الأثواب المحلّلة ({rollRows.Count:N0})"));
        _stack.Children.Add(ErpUiFactory.Card(BuildRollsGrid(rollRows)));

        var issues = PackingListIssueRow.FromParseResult(parseResult).ToList();
        _stack.Children.Add(ErpUiFactory.SectionTitle("مشاكل التحليل وربط الأكواد"));
        if (issues.Count == 0)
            _stack.Children.Add(ErpUxFactory.InfoBanner("لا توجد مشاكل في ربط الأكواد أو تحليل الصفوف.", "success"));
        else
            _stack.Children.Add(ErpUiFactory.Card(BuildIssuesGrid(issues)));

        _stack.Children.Add(ErpUxFactory.InfoBanner(
            "بعد إكمال ربط DPL (إن وُجد)، تابع إلى إدخال التكلفة.",
            "info"));

        var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 16, 0, 0) };
        var backButton = new Button
        {
            Content = "العودة — رفع ملف آخر",
            Style = (Style)WpfApplication.Current.Resources["SecondaryButtonStyle"]!,
            Margin = new Thickness(0, 0, 8, 0)
        };
        backButton.Click += (_, _) => ChinaImportNavigation.Navigate("NewImport");
        actions.Children.Add(backButton);

        var grand = parseResult.GrandTotal;
        var needsDplLink = session?.RequiresDplLinking == true;
        var crossValidationOk = ChinaImportNavigationContext.CanProceedFromAnalysis();
        var canContinue = grand.RollsMatch && grand.MetersMatch && !parseResult.HasUnresolvedGroups && !needsDplLink && crossValidationOk;

        _continueButton = new Button
        {
            Content = "التالي — إدخال التكلفة",
            Style = (Style)WpfApplication.Current.Resources["PrimaryButtonStyle"]!,
            IsEnabled = canContinue
        };

        if (needsDplLink)
            _continueButton.ToolTip = "أكمل ربط جميع مجموعات DPL ببنود الفاتورة قبل المتابعة.";
        else if (!canContinue && grand.DeclaredTotalRolls.HasValue && !grand.RollsMatch)
            _continueButton.ToolTip =
                $"تحذير: تم تحليل {grand.ParsedTotalRolls} توب فقط من أصل {grand.DeclaredTotalRolls.Value} المعلن في الملف.";
        else if (!canContinue && !crossValidationOk)
            _continueButton.ToolTip = "يوجد اختلاف بين مجموع DPL (بالمتر) وفاتورة/PL — أكّد يدوياً أو صحّح وحدة DPL.";
        else if (!canContinue && parseResult.HasUnresolvedGroups)
            _continueButton.ToolTip = "يوجد أكواد غير مربوطة في الكتالوج.";
        else
            _continueButton.Click += (_, _) => ChinaImportNavigation.Navigate("CostEntry");

        actions.Children.Add(_continueButton);
        _stack.Children.Add(actions);
    }

    private UIElement BuildDplLinkPanel(ChinaImportMultiFileSessionDto session)
    {
        var panel = new StackPanel { Margin = new Thickness(4) };

        foreach (var unmatched in session.UnmatchedDplGroups)
        {
            var row = new Grid { Margin = new Thickness(0, 6, 0, 6) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.2, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var label = new TextBlock
            {
                Text = unmatched.DisplayLabel,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };
            if (unmatched.HasSuggestion)
            {
                label.ToolTip = $"اقتراح تلقائي: {unmatched.SuggestedInvoiceDescription} (درجة {unmatched.SuggestionScore})";
            }

            Grid.SetColumn(label, 0);
            row.Children.Add(label);

            var combo = new ComboBox
            {
                MinWidth = 280,
                DisplayMemberPath = nameof(ChinaImportInvoiceLinkOptionDto.Display),
                SelectedValuePath = nameof(ChinaImportInvoiceLinkOptionDto.MatchKey),
                ItemsSource = session.InvoiceLinkOptions
            };

            if (!string.IsNullOrWhiteSpace(unmatched.SuggestedInvoiceMatchKey))
                combo.SelectedValue = unmatched.SuggestedInvoiceMatchKey;
            else if (session.InvoiceLinkOptions.Count > 0)
                combo.SelectedIndex = 0;

            Grid.SetColumn(combo, 1);
            row.Children.Add(combo);

            var confirm = new Button
            {
                Content = "تأكيد الربط",
                Style = (Style)WpfApplication.Current.Resources["SecondaryButtonStyle"]!,
                Margin = new Thickness(8, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            var captured = unmatched;
            confirm.Click += async (_, _) =>
            {
                if (combo.SelectedItem is not ChinaImportInvoiceLinkOptionDto selected)
                {
                    MessageBox.Show("يرجى اختيار بند من الفاتورة/PL.", "ربط DPL",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!AppServices.IsInitialized)
                    return;

                confirm.IsEnabled = false;
                Mouse.OverrideCursor = Cursors.Wait;
                try
                {
                    var result = await ContainerUiService.Instance.ConfirmDplLinkAsync(
                        captured.DplMatchKey,
                        selected.MatchKey,
                        selected.Description,
                        captured.FabricItemId ?? Guid.Empty,
                        captured.FabricColorId ?? Guid.Empty);

                    if (!ApplicationResultPresenter.Present(result))
                        return;

                    await RefreshAsync();
                }
                finally
                {
                    Mouse.OverrideCursor = null;
                    confirm.IsEnabled = true;
                }
            };

            Grid.SetColumn(confirm, 2);
            row.Children.Add(confirm);
            panel.Children.Add(row);
        }

        return panel;
    }

    private static UIElement BuildGrandTotalPanel(PackingListGrandTotalDto grand)
    {
        var grid = new Grid { Margin = new Thickness(4) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var summary = new TextBlock
        {
            Text = grand.SummaryText,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(summary, 0);
        grid.Children.Add(summary);

        var metersBadge = new TextBlock
        {
            Text = $"الأطوال {(grand.MetersMatch ? "✅" : "⚠️")}",
            Margin = new Thickness(12, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(metersBadge, 1);
        grid.Children.Add(metersBadge);

        var rollsBadge = new TextBlock
        {
            Text = $"الأثواب {(grand.RollsMatch ? "✅" : "⚠️")}",
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(rollsBadge, 2);
        grid.Children.Add(rollsBadge);

        return grid;
    }

    private static DataGrid BuildGroupsGrid(ContainerExcelParseResultDto result)
    {
        var rows = result.Groups.Select(PackingListGroupAnalysisRow.FromDto).ToList();
        var g = ErpUiFactory.BuildGrid(rows, false);
        g.AutoGenerateColumns = false;
        g.IsReadOnly = true;
        foreach (var (h, p, w, fmt) in new (string, string, object, string?)[]
        {
            ("#", nameof(PackingListGroupAnalysisRow.GroupIndex), 40, null),
            ("كود القماش", nameof(PackingListGroupAnalysisRow.FabricCode), 110, null),
            ("اللون", nameof(PackingListGroupAnalysisRow.Color), 90, null),
            ("أثواب معلنة", nameof(PackingListGroupAnalysisRow.DeclaredRolls), 90, null),
            ("أثواب محللة", nameof(PackingListGroupAnalysisRow.ParsedRolls), 90, null),
            ("تطابق الأثواب", nameof(PackingListGroupAnalysisRow.RollsIndicator), 90, null),
            ("أطوال معلنة", nameof(PackingListGroupAnalysisRow.DeclaredMeters), 100, "N2"),
            ("أطوال محللة", nameof(PackingListGroupAnalysisRow.ParsedMeters), 100, "N2"),
            ("تطابق الأطوال", nameof(PackingListGroupAnalysisRow.MetersIndicator), 90, null),
            ("الربط", nameof(PackingListGroupAnalysisRow.CatalogStatus), 140, null)
        })
        {
            ErpUiFactory.AddGridColumn(g, h, p, w, fmt);
        }
        return g;
    }

    private static DataGrid BuildRollsGrid(IReadOnlyList<PackingListRollAnalysisRow> rolls)
    {
        if (rolls.Count == 0)
        {
            return new DataGrid
            {
                IsReadOnly = true,
                Height = 48,
                ItemsSource = Array.Empty<object>()
            };
        }

        var g = ErpUiFactory.BuildGrid(rolls, false);
        g.AutoGenerateColumns = false;
        g.IsReadOnly = true;
        g.MaxHeight = 420;
        g.CanUserSortColumns = true;
        foreach (var (h, p, w, fmt) in new (string, string, object, string?)[]
        {
            ("المجموعة", nameof(PackingListRollAnalysisRow.GroupIndex), 70, null),
            ("كود القماش", nameof(PackingListRollAnalysisRow.FabricCode), 100, null),
            ("اللون", nameof(PackingListRollAnalysisRow.Color), 90, null),
            ("رقم التوب", nameof(PackingListRollAnalysisRow.RollNumber), 80, null),
            ("الكمية (DPL)", nameof(PackingListRollAnalysisRow.QuantityDisplay), 150, null),
            ("المتر (M)", nameof(PackingListRollAnalysisRow.Meters), 90, "N2"),
            ("اللوت", nameof(PackingListRollAnalysisRow.LotCode), 70, null),
            ("الحالة", nameof(PackingListRollAnalysisRow.StatusDisplay), 120, null)
        })
        {
            ErpUiFactory.AddGridColumn(g, h, p, w, fmt);
        }

        return g;
    }

    private static DataGrid BuildIssuesGrid(IReadOnlyList<PackingListIssueRow> issues)
    {
        var g = ErpUiFactory.BuildGrid(issues, false);
        g.AutoGenerateColumns = false;
        g.IsReadOnly = true;
        foreach (var (h, p, w) in new (string, string, object)[]
        {
            ("المجموعة", nameof(PackingListIssueRow.GroupIndex), 70),
            ("كود القماش", nameof(PackingListIssueRow.FabricCode), 110),
            ("اللون", nameof(PackingListIssueRow.Color), 90),
            ("رقم التوب", nameof(PackingListIssueRow.RollLabel), 80),
            ("السبب", nameof(PackingListIssueRow.Reason), "*")
        })
        {
            ErpUiFactory.AddGridColumn(g, h, p, w, null);
        }
        return g;
    }

    private static DataGrid BuildTypeLinesGrid(IReadOnlyList<ChinaImportTypeLineDto> lines)
    {
        var g = ErpUiFactory.BuildGrid(lines, false);
        g.AutoGenerateColumns = false;
        g.IsReadOnly = true;
        g.MaxHeight = 360;
        foreach (var (h, p, w, fmt) in new (string, string, object, string?)[]
        {
            ("النوع / اللون", nameof(ChinaImportTypeLineDto.TypeDisplayName), 180, null),
            ("فاتورة", nameof(ChinaImportTypeLineDto.HasInvoice), 55, null),
            ("PL", nameof(ChinaImportTypeLineDto.HasPackingSummary), 45, null),
            ("DPL", nameof(ChinaImportTypeLineDto.HasDpl), 45, null),
            ("الحالة", nameof(ChinaImportTypeLineDto.MatchStatusDisplay), 220, null),
            ("الأمتار", nameof(ChinaImportTypeLineDto.LengthMeters), 90, "N0"),
            ("الوزن (كغ)", nameof(ChinaImportTypeLineDto.NetWeightKg), 90, "N0"),
            ("سعر/م ($)", nameof(ChinaImportTypeLineDto.ChinaUnitPriceUsd), 90, "N4"),
            ("الأثواب", nameof(ChinaImportTypeLineDto.RollCount), 70, null)
        })
        {
            ErpUiFactory.AddGridColumn(g, h, p, w, fmt);
        }
        return g;
    }

    private UIElement BuildCrossValidationPanel(IReadOnlyList<DplGroupCrossValidationResult> results)
    {
        var panel = new StackPanel();
        foreach (var result in results)
        {
            var row = new Grid { Margin = new Thickness(0, 6, 0, 6) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var message = new TextBlock
            {
                Text = result.MessageArabic,
                TextWrapping = TextWrapping.Wrap,
                Foreground = (Brush)WpfApplication.Current.Resources[
                    result.Passed || result.UserConfirmed ? "TextSecondaryBrush" : "WarningBrush"]!
            };
            Grid.SetColumn(message, 0);
            row.Children.Add(message);

            if (!result.Passed && !result.UserConfirmed)
            {
                var confirm = new Button
                {
                    Content = "تأكيد والمتابعة رغم الاختلاف",
                    Style = (Style)WpfApplication.Current.Resources["SecondaryButtonStyle"]!,
                    Margin = new Thickness(8, 0, 0, 0),
                    Tag = result.GroupKey
                };
                confirm.Click += async (_, _) =>
                {
                    ChinaImportNavigationContext.ConfirmCrossValidationGroup(result.GroupKey);
                    await RefreshAsync();
                };
                Grid.SetColumn(confirm, 1);
                row.Children.Add(confirm);
            }

            panel.Children.Add(row);
        }

        if (results.All(r => r.Passed))
            panel.Children.Insert(0, ErpUxFactory.InfoBanner("✅ جميع مجموعات DPL تطابق فاتورة/PL ضمن الحد المسموح.", "success"));

        return panel;
    }
}
