namespace ERPSystem.DocumentEngine.Models;

/// <summary>A labelled value shown in the document header meta grid.</summary>
public sealed class InfoField
{
    public InfoField() { }
    public InfoField(string label, string? value, bool emphasize = false)
    {
        Label = label;
        Value = value;
        Emphasize = emphasize;
    }

    public string Label { get; set; } = string.Empty;
    public string? Value { get; set; }
    public bool Emphasize { get; set; }
}

/// <summary>A customer / supplier / partner card.</summary>
public sealed class PartyInfo
{
    /// <summary>Localised role caption, e.g. "Bill To", "Supplier".</summary>
    public string? Role { get; set; }
    public string Name { get; set; } = string.Empty;

    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? TaxNumber { get; set; }
    public string? AccountCode { get; set; }

    /// <summary>Extra free lines shown under the standard fields.</summary>
    public List<string> ExtraLines { get; set; } = new();

    /// <summary>Visual variant: customer (default), supplier, partner.</summary>
    public PartyKind Kind { get; set; } = PartyKind.Customer;
}

public enum PartyKind
{
    Customer,
    Supplier,
    Partner
}

/// <summary>A KPI / summary card.</summary>
public sealed class SummaryCard
{
    public SummaryCard() { }
    public SummaryCard(string label, string value, Accent accent = Accent.Primary)
    {
        Label = label;
        Value = value;
        Accent = accent;
    }

    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Delta { get; set; }
    public Accent Accent { get; set; } = Accent.Primary;
}

/// <summary>A data table (line items, ledger rows, statement rows, ...).</summary>
public sealed class DocumentTable
{
    public string? Title { get; set; }
    public List<TableColumn> Columns { get; set; } = new();
    public List<TableRow> Rows { get; set; } = new();

    /// <summary>Optional footer cells (e.g. column totals).</summary>
    public List<TableCell>? Footer { get; set; }

    /// <summary>Collapse each row into a card on mobile (default true).</summary>
    public bool Responsive { get; set; } = true;
    public bool Bordered { get; set; }
    public bool Compact { get; set; }
}

public sealed class TableColumn
{
    public TableColumn() { }
    public TableColumn(string header, TextAlign align = TextAlign.Start, string? width = null, bool numeric = false)
    {
        Header = header;
        Align = align;
        Width = width;
        Numeric = numeric;
    }

    public string Header { get; set; } = string.Empty;
    public TextAlign Align { get; set; } = TextAlign.Start;
    public string? Width { get; set; }
    public bool Numeric { get; set; }
}

public sealed class TableRow
{
    public TableRow() { }
    public TableRow(params TableCell[] cells) => Cells.AddRange(cells);

    public List<TableCell> Cells { get; set; } = new();
    public bool Highlight { get; set; }
}

public sealed class TableCell
{
    public TableCell() { }
    public TableCell(string? text, TextAlign? align = null)
    {
        Text = text;
        Align = align;
    }

    public string? Text { get; set; }
    public TextAlign? Align { get; set; }

    /// <summary>When set the cell renders a badge instead of plain text.</summary>
    public Accent? BadgeAccent { get; set; }

    public static TableCell Badge(string text, Accent accent) =>
        new(text) { BadgeAccent = accent };
}

/// <summary>The totals panel (subtotal / discount / tax / grand total).</summary>
public sealed class TotalsModel
{
    public List<TotalLine> Lines { get; set; } = new();
}

public sealed class TotalLine
{
    public TotalLine() { }
    public TotalLine(string label, string value, bool isGrand = false)
    {
        Label = label;
        Value = value;
        IsGrand = isGrand;
    }

    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool IsGrand { get; set; }
}

public sealed class TaxLine
{
    public string Label { get; set; } = string.Empty;
    public string? Base { get; set; }
    public string? Rate { get; set; }
    public string Amount { get; set; } = string.Empty;
}

public sealed class TimelineEntry
{
    public string? Time { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Accent Accent { get; set; } = Accent.Primary;
}

public sealed class SignatureSlot
{
    public SignatureSlot() { }
    public SignatureSlot(string title, string? name = null)
    {
        Title = title;
        Name = name;
    }

    public string Title { get; set; } = string.Empty;
    public string? Name { get; set; }
}

public sealed class AttachmentItem
{
    public string Name { get; set; } = string.Empty;
    public string? Meta { get; set; }
}

public sealed class ApprovalInfo
{
    public ApprovalState State { get; set; } = ApprovalState.Approved;
    public string? Label { get; set; }
    public string? By { get; set; }
    public string? Date { get; set; }
}
