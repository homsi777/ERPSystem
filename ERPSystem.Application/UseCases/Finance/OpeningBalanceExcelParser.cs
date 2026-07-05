using ClosedXML.Excel;
using ERPSystem.Application.Commands.Finance;
using ERPSystem.Domain.Entities.Finance;

namespace ERPSystem.Application.UseCases.Finance;

/// <summary>
/// Parses Excel workbooks into opening balance line inputs per balance type.
/// Template headers are validated before business rules run in the engine.
/// </summary>
public static class OpeningBalanceExcelParser
{
    public static (IReadOnlyList<string> Headers, IReadOnlyList<OpeningBalanceLineInput> Lines, IReadOnlyList<string> TemplateErrors)
        Parse(OpeningBalanceType type, byte[] content)
    {
        using var stream = new MemoryStream(content);
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheets.FirstOrDefault()
            ?? throw new InvalidOperationException("الملف لا يحتوي على أوراق عمل.");

        var headers = sheet.Row(1).CellsUsed().Select(c => c.GetString().Trim()).ToList();
        var expected = ExpectedHeaders(type);
        var templateErrors = new List<string>();
        foreach (var h in expected)
        {
            if (!headers.Any(x => x.Equals(h, StringComparison.OrdinalIgnoreCase)))
                templateErrors.Add($"العمود المطلوب غير موجود: {h}");
        }

        if (templateErrors.Count > 0)
            return (headers, [], templateErrors);

        var lines = new List<OpeningBalanceLineInput>();
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
        for (var r = 2; r <= lastRow; r++)
        {
            var row = sheet.Row(r);
            if (row.CellsUsed().All(c => string.IsNullOrWhiteSpace(c.GetString())))
                continue;

            lines.Add(ParseRow(type, headers, row));
        }

        return (headers, lines, templateErrors);
    }

    public static byte[] BuildTemplate(OpeningBalanceType type)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Import");
        var headers = ExpectedHeaders(type);
        for (var i = 0; i < headers.Count; i++)
            sheet.Cell(1, i + 1).Value = headers[i];
        sheet.Row(1).Style.Font.Bold = true;
        sheet.SheetView.FreezeRows(1);
        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    private static OpeningBalanceLineInput ParseRow(OpeningBalanceType type, IReadOnlyList<string> headers, IXLRow row)
    {
        string G(string name) => GetCell(headers, row, name);
        decimal D(string name) => decimal.TryParse(G(name), out var v) ? v : 0m;
        decimal? Dn(string name) => decimal.TryParse(G(name), out var v) ? v : null;

        return type switch
        {
            OpeningBalanceType.OpeningStock => new OpeningBalanceLineInput
            {
                WarehouseName = G("Warehouse"),
                ItemName = G("Fabric"),
                ColorName = G("Color"),
                BatchNumber = G("Batch"),
                LocationCode = G("Location"),
                RollCount = Dn("RollCount"),
                Quantity = Dn("Meters"),
                UnitCost = Dn("Cost"),
                Debit = (Dn("Meters") ?? 0) * (Dn("Cost") ?? 0),
                Description = G("Notes")
            },
            OpeningBalanceType.CustomerReceivable => new OpeningBalanceLineInput
            {
                PartyName = ResolveCustomerKey(headers, row),
                Reference = FirstNonEmpty(G("Reference"), G("Ref")),
                Debit = ResolveCustomerDebit(headers, row),
                Credit = ResolveCustomerCredit(headers, row),
                Description = FirstNonEmpty(G("Description"), G("Notes")),
                Notes = G("Notes")
            },
            OpeningBalanceType.SupplierPayable => new OpeningBalanceLineInput
            {
                PartyName = G("Supplier"),
                Credit = D("Amount"),
                Reference = G("Reference"),
                Description = G("Description"),
                Notes = G("Notes")
            },
            OpeningBalanceType.Cash => new OpeningBalanceLineInput
            {
                AccountName = G("CashAccount"),
                Debit = D("Amount"),
                Description = G("Notes")
            },
            OpeningBalanceType.Bank => new OpeningBalanceLineInput
            {
                BankName = G("Bank"),
                BankAccountNumber = G("AccountNumber"),
                Debit = D("Amount"),
                Description = G("Notes")
            },
            OpeningBalanceType.Capital => new OpeningBalanceLineInput
            {
                PartyName = G("Partner"),
                Credit = D("Contribution"),
                InvestmentScope = G("InvestmentScope"),
                Description = G("Notes")
            },
            OpeningBalanceType.GeneralLedger => new OpeningBalanceLineInput
            {
                AccountName = G("Account"),
                Debit = D("Debit"),
                Credit = D("Credit"),
                Description = G("Description")
            },
            _ => new OpeningBalanceLineInput
            {
                PartyName = G("Party"),
                AccountName = G("Account"),
                Debit = D("Debit"),
                Credit = D("Credit"),
                Description = G("Description")
            }
        };
    }

    private static string GetCell(IReadOnlyList<string> headers, IXLRow row, string header)
    {
        var idx = headers.ToList().FindIndex(h => h.Equals(header, StringComparison.OrdinalIgnoreCase));
        return idx < 0 ? "" : row.Cell(idx + 1).GetString().Trim();
    }

    private static string ResolveCustomerKey(IReadOnlyList<string> headers, IXLRow row)
    {
        string G(string name) => GetCell(headers, row, name);
        var code = G("CustomerCode");
        if (!string.IsNullOrWhiteSpace(code))
            return code;
        var legacy = G("Customer");
        if (!string.IsNullOrWhiteSpace(legacy))
            return legacy;
        return G("CustomerName");
    }

    private static decimal ResolveCustomerDebit(IReadOnlyList<string> headers, IXLRow row)
    {
        string G(string name) => GetCell(headers, row, name);
        decimal D(string name) => decimal.TryParse(G(name), out var v) ? v : 0m;
        var debit = D("Debit");
        return debit > 0 ? debit : D("OpeningAmount");
    }

    private static decimal ResolveCustomerCredit(IReadOnlyList<string> headers, IXLRow row)
    {
        string G(string name) => GetCell(headers, row, name);
        return decimal.TryParse(G("Credit"), out var v) ? v : 0m;
    }

    private static string FirstNonEmpty(params string[] values)
    {
        foreach (var v in values)
        {
            if (!string.IsNullOrWhiteSpace(v))
                return v;
        }
        return "";
    }

    public static IReadOnlyList<string> ExpectedHeaders(OpeningBalanceType type) => type switch
    {
        OpeningBalanceType.OpeningStock =>
            ["Warehouse", "Fabric", "Color", "Batch", "RollCount", "Meters", "Cost", "Location", "Notes"],
        OpeningBalanceType.CustomerReceivable =>
            ["CustomerCode", "CustomerName", "Debit", "Credit", "Currency", "Reference", "Notes"],
        OpeningBalanceType.SupplierPayable =>
            ["Supplier", "Amount", "Currency", "Reference", "Description", "Notes"],
        OpeningBalanceType.Cash =>
            ["CashAccount", "Currency", "Amount", "Notes"],
        OpeningBalanceType.Bank =>
            ["Bank", "AccountNumber", "Currency", "Amount", "Notes"],
        OpeningBalanceType.Capital =>
            ["Partner", "Contribution", "Currency", "InvestmentScope", "Notes"],
        OpeningBalanceType.GeneralLedger =>
            ["Account", "Debit", "Credit", "Description"],
        _ => ["Party", "Account", "Debit", "Credit", "Description"]
    };
}
