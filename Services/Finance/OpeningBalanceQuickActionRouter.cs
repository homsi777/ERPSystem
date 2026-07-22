using ERPSystem.Application.DTOs.Finance;
using ERPSystem.Application.DTOs.Reports;
using ERPSystem.Controls.Customers;
using ERPSystem.Controls.OperationsCenter;
using ERPSystem.Core;
using ERPSystem.Dialogs;
using ERPSystem.Domain.Entities.Finance;
using ERPSystem.Services.Documents;
using ERPSystem.Services.Reports;

namespace ERPSystem.Services.Finance;

public static class OpeningBalanceQuickActionRouter
{
    public static bool TryHandleQuickAction(string? actionKey, OperationsCenterContext ctx)
    {
        if (string.IsNullOrEmpty(actionKey) || ctx.EntityRow is not OpeningBalanceListDto row)
            return false;

        return TryHandle(actionKey, row);
    }

    public static bool TryHandle(string actionKey, OpeningBalanceListDto row) => actionKey switch
    {
        "ob:submit" => Run(() => SubmitAsync(row)),
        "ob:approve" => Run(() => ApproveAsync(row)),
        "ob:post" => Run(() => PostAsync(row)),
        "ob:archive" => Run(() => ArchiveAsync(row)),
        "ob:delete" => Run(() => DeleteAsync(row)),
        "ob:export" => Run(() => ExportAsync(row)),
        "ob:pdf" => Run(() => ExportPdfAsync(row)),
        "ob:open" => Run(() => OpenAsync(row)),
        "ob:edit" => Run(() => EditAsync(row)),
        "ob:audit" => Run(() => AuditAsync(row)),
        "nav:Accounting:OpeningBalances" => NavigateList(),
        "nav:Accounting:OpeningBalanceWorkspace" => NavigateWorkspace(row),
        _ => false
    };

    private static bool Run(Func<Task> action)
    {
        _ = action();
        return true;
    }

    private static bool NavigateList()
    {
        MockInteractionService.Navigate(AppModule.Accounting, "OpeningBalances");
        return true;
    }

    private static bool NavigateWorkspace(OpeningBalanceListDto row)
    {
        OpeningBalanceNavigationContext.BeginWorkspace(row.Id);
        MockInteractionService.Navigate(AppModule.Accounting, "OpeningBalanceWorkspace");
        return true;
    }

    private static Task OpenAsync(OpeningBalanceListDto row)
    {
        OpeningBalancePopupService.ShowOperationsCenter(row);
        return Task.CompletedTask;
    }

    private static Task EditAsync(OpeningBalanceListDto row)
    {
        if (row.Status is not (OpeningBalanceStatus.Draft or OpeningBalanceStatus.Rejected))
        {
            MockInteractionService.ShowWarning("لا يمكن تعديل المستند في حالته الحالية.");
            return Task.CompletedTask;
        }

        if (row.LineCount <= 1)
        {
            CustomerOpeningBalanceNavigationContext.BeginEdit(row.Id);
            ErpModalWindow.Show(
                "تعديل رصيد افتتاحي",
                row.Number,
                new CustomerOpeningBalanceFormControl(),
                "\uE70F", 640, 720);
        }
        else
        {
            OpeningBalancePopupService.ShowOperationsCenter(row);
        }

        return Task.CompletedTask;
    }

    private static Task AuditAsync(OpeningBalanceListDto row)
    {
        OpeningBalancePopupService.ShowOperationsCenter(row, "Audit");
        return Task.CompletedTask;
    }

    private static async Task SubmitAsync(OpeningBalanceListDto row)
    {
        if (row.Status is not (OpeningBalanceStatus.Draft or OpeningBalanceStatus.Rejected))
        {
            MockInteractionService.ShowWarning("لا يمكن إرسال هذا المستند للاعتماد في حالته الحالية.");
            return;
        }

        var result = await OpeningBalanceUiService.Instance.SubmitAsync(row.Id);
        if (ApplicationResultPresenter.Present(result))
        {
            OpeningBalanceListRefreshHub.RequestRefresh();
            CustomerOpeningBalanceRefreshHub.RequestRefresh();
            MockInteractionService.ShowSuccess("تم إرسال المستند للاعتماد.");
        }
    }

    private static async Task ApproveAsync(OpeningBalanceListDto row)
    {
        if (row.Status is not (OpeningBalanceStatus.PendingApproval or OpeningBalanceStatus.Draft))
        {
            MockInteractionService.ShowWarning("لا يمكن اعتماد هذا المستند في حالته الحالية.");
            return;
        }

        var result = await OpeningBalanceUiService.Instance.ApproveAsync(row.Id, null);
        if (ApplicationResultPresenter.Present(result))
        {
            OpeningBalanceListRefreshHub.RequestRefresh();
            CustomerOpeningBalanceRefreshHub.RequestRefresh();
            MockInteractionService.ShowSuccess("تم اعتماد المستند.");
        }
    }

    private static async Task PostAsync(OpeningBalanceListDto row)
    {
        if (row.Status != OpeningBalanceStatus.Approved)
        {
            MockInteractionService.ShowWarning("يجب اعتماد المستند قبل الترحيل.");
            return;
        }

        var result = await OpeningBalanceUiService.Instance.PostAsync(row.Id);
        if (ApplicationResultPresenter.Present(result))
        {
            OpeningBalanceListRefreshHub.RequestRefresh();
            CustomerOpeningBalanceRefreshHub.RequestRefresh();
            var isStock = row.Type == OpeningBalanceType.OpeningStock;
            MockInteractionService.ShowSuccess(
                result.Value?.JournalEntryNumber is { Length: > 0 } num
                    ? (isStock ? $"تم الترحيل إلى المخزون — القيد {num}" : $"تم الترحيل — القيد {num}")
                    : (isStock ? "تم الترحيل إلى المخزون." : "تم ترحيل المستند."));
        }
    }

    private static async Task ArchiveAsync(OpeningBalanceListDto row)
    {
        if (row.Status == OpeningBalanceStatus.Archived)
        {
            MockInteractionService.ShowWarning("المستند مؤرشف مسبقاً.");
            return;
        }

        var result = await OpeningBalanceUiService.Instance.ArchiveAsync(row.Id);
        if (ApplicationResultPresenter.Present(result))
        {
            OpeningBalanceListRefreshHub.RequestRefresh();
            CustomerOpeningBalanceRefreshHub.RequestRefresh();
            MockInteractionService.ShowSuccess("تم أرشفة المستند.");
        }
    }

    private static async Task DeleteAsync(OpeningBalanceListDto row)
    {
        if (row.Status is OpeningBalanceStatus.Posted or OpeningBalanceStatus.Locked or OpeningBalanceStatus.Archived)
        {
            MockInteractionService.ShowWarning("لا يمكن حذف مستند مرحّل أو مؤرشف. استخدم الأرشفة للمستندات المقفلة.");
            return;
        }

        var result = await OpeningBalanceUiService.Instance.DeleteBeforePostAsync(row.Id);
        if (ApplicationResultPresenter.Present(result))
        {
            OpeningBalanceListRefreshHub.RequestRefresh();
            CustomerOpeningBalanceRefreshHub.RequestRefresh();
            MockInteractionService.ShowSuccess($"تم حذف المستند {row.Number}.");
        }
    }

    private static async Task ExportAsync(OpeningBalanceListDto row)
    {
        var result = await OpeningBalanceUiService.Instance.GetDetailsAsync(row.Id);
        if (!ApplicationResultPresenter.Present(result) || result.Value is null)
            return;

        var oc = result.Value;
        if (row.Type == OpeningBalanceType.OpeningStock)
        {
            ListExportService.ExportRecords(
                oc.Lines,
                $"OpeningStock-{oc.Header.Number}",
                ("السطر", l => l.LineNumber),
                ("الصنف", l => l.ItemName),
                ("اللون", l => l.ColorName),
                ("الحاوية", l => l.ContainerNumber),
                ("الكمية", l => l.Quantity),
                ("الأثواب", l => l.RollCount),
                ("التكلفة", l => l.UnitCost),
                ("مدين", l => l.Debit),
                ("الوصف", l => l.Description));
            return;
        }

        ListExportService.ExportRecords(
            oc.Lines,
            $"OpeningBalance-{oc.Header.Number}",
            ("السطر", l => l.LineNumber),
            ("الطرف", l => l.PartyName),
            ("الحساب", l => l.AccountName),
            ("مدين", l => l.Debit),
            ("دائن", l => l.Credit),
            ("الوصف", l => l.Description));
    }

    private static async Task ExportPdfAsync(OpeningBalanceListDto row)
    {
        var result = await OpeningBalanceUiService.Instance.GetDetailsAsync(row.Id);
        if (!ApplicationResultPresenter.Present(result) || result.Value is null)
            return;

        var oc = result.Value;
        var isStock = row.Type == OpeningBalanceType.OpeningStock;
        var report = new ModuleReportResultDto
        {
            ReportKey = isStock ? "opening-stock" : "opening-balance",
            Title = isStock ? $"مواد أول المدة — {oc.Header.Number}" : $"رصيد افتتاحي — {oc.Header.Number}",
            Description = $"{oc.Header.StatusDisplay} • {oc.Header.OpeningDate:yyyy/MM/dd} • {oc.Header.Reference}",
            GeneratedAt = DateTime.Now,
            Kpis =
            [
                new ModuleReportKpiDto { Label = "الحالة", Value = oc.Header.StatusDisplay, IconGlyph = "\uE8FB" },
                new ModuleReportKpiDto { Label = "القيمة", Value = oc.Header.TotalBaseAmount.ToString("N2"), IconGlyph = "\uE8C1" },
                new ModuleReportKpiDto { Label = "الأسطر", Value = oc.Lines.Count.ToString(), IconGlyph = "\uE8A5" }
            ],
            Columns = isStock
                ?
                [
                    new ModuleReportColumnDto { Key = "line", HeaderAr = "سطر", Width = 50 },
                    new ModuleReportColumnDto { Key = "item", HeaderAr = "المادة", Width = 160, IsStar = true },
                    new ModuleReportColumnDto { Key = "color", HeaderAr = "اللون", Width = 100 },
                    new ModuleReportColumnDto { Key = "container", HeaderAr = "الحاوية", Width = 90 },
                    new ModuleReportColumnDto { Key = "qty", HeaderAr = "الكمية", Width = 90, Format = "N2" },
                    new ModuleReportColumnDto { Key = "rolls", HeaderAr = "أثواب", Width = 70 },
                    new ModuleReportColumnDto { Key = "cost", HeaderAr = "تكلفة", Width = 90, Format = "N2" },
                    new ModuleReportColumnDto { Key = "debit", HeaderAr = "القيمة", Width = 100, Format = "N2" }
                ]
                :
                [
                    new ModuleReportColumnDto { Key = "line", HeaderAr = "سطر", Width = 50 },
                    new ModuleReportColumnDto { Key = "party", HeaderAr = "الطرف", Width = 140, IsStar = true },
                    new ModuleReportColumnDto { Key = "account", HeaderAr = "الحساب", Width = 140 },
                    new ModuleReportColumnDto { Key = "debit", HeaderAr = "مدين", Width = 100, Format = "N2" },
                    new ModuleReportColumnDto { Key = "credit", HeaderAr = "دائن", Width = 100, Format = "N2" }
                ],
            Rows = oc.Lines.Select(l => isStock
                ? new Dictionary<string, object?>
                {
                    ["line"] = l.LineNumber,
                    ["item"] = l.ItemName ?? l.ItemCode,
                    ["color"] = l.ColorName,
                    ["container"] = l.ContainerNumber,
                    ["qty"] = l.Quantity,
                    ["rolls"] = l.RollCount,
                    ["cost"] = l.UnitCost,
                    ["debit"] = l.Debit
                }
                : new Dictionary<string, object?>
                {
                    ["line"] = l.LineNumber,
                    ["party"] = l.PartyName,
                    ["account"] = l.AccountName,
                    ["debit"] = l.Debit,
                    ["credit"] = l.Credit
                }).ToList()
        };

        ModuleReportDocumentService.ShowPreview(report, exportPdf: false);
    }
}
