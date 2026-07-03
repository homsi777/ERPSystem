using System.Collections.ObjectModel;
using System.ComponentModel;

namespace ERPSystem.Core.Sales
{
    // ══════════════════════════════════════════════════════════
    //  ENUMS
    // ══════════════════════════════════════════════════════════

    public enum InvoiceStatus   { Draft, Posted, Cancelled, Returned }
    public enum PaymentStatus   { Unpaid, Partial, Paid }
    public enum InvoiceType     { Cash, Credit }
    public enum SalesPayMethod  { Cash, Card, Transfer, Split }

    // ══════════════════════════════════════════════════════════
    //  INVOICE LINE
    // ══════════════════════════════════════════════════════════

    public class InvoiceLine
    {
        public string ItemCode   { get; init; } = "";
        public string ItemNameAr { get; init; } = "";
        public string ItemNameEn { get; init; } = "";
        public string Unit       { get; init; } = "متر";
        public decimal Qty       { get; init; }
        public decimal UnitPrice { get; init; }
        public decimal Discount  { get; init; }          // fixed amount per line
        public decimal LineTotal => Qty * UnitPrice - Discount;

        public string ItemName(bool ar) => ar ? ItemNameAr : ItemNameEn;

        // TODO: attach stock movement link
        // TODO: attach price list / discount policy
    }

    // ══════════════════════════════════════════════════════════
    //  SALES INVOICE
    // ══════════════════════════════════════════════════════════

    public class SalesInvoice : INotifyPropertyChanged
    {
        private InvoiceStatus _status = InvoiceStatus.Draft;
        private decimal _paidAmount;

        // ── Identity ──────────────────────────────────────────

        public string   InvoiceNumber  { get; init; } = "";
        public DateTime Date           { get; init; }
        public string   Branch         { get; init; } = "";
        public string   UserName       { get; init; } = "";

        // ── Customer ──────────────────────────────────────────

        public string   CustomerId       { get; init; } = "";
        public string   CustomerNameAr   { get; init; } = "";
        public string   CustomerNameEn   { get; init; } = "";
        public string   CustomerPhone    { get; init; } = "";
        public decimal  CustomerBalance  { get; init; }
        public decimal  CustomerLimit    { get; init; }
        public bool     CustomerIsCredit { get; init; }
        public bool     CustomerOverLimit => CustomerIsCredit && CustomerLimit > 0 && CustomerBalance > CustomerLimit;

        // ── Type & Status ─────────────────────────────────────

        public InvoiceType    Type       { get; init; } = InvoiceType.Cash;
        public SalesPayMethod PayMethod  { get; init; } = SalesPayMethod.Cash;
        public string         Notes      { get; init; } = "";

        public InvoiceStatus Status
        {
            get => _status;
            set { _status = value; Notify(nameof(Status)); Notify(nameof(PaymentStatus)); Notify(nameof(CanEdit)); Notify(nameof(CanReturn)); Notify(nameof(CanCancel)); }
        }

        public decimal PaidAmount
        {
            get => _paidAmount;
            set { _paidAmount = value; Notify(nameof(PaidAmount)); Notify(nameof(RemainingAmount)); Notify(nameof(PaymentStatus)); }
        }

        // ── Lines ─────────────────────────────────────────────

        public ObservableCollection<InvoiceLine> Lines { get; } = new();

        // ── Computed financials ───────────────────────────────

        public decimal Subtotal         => Lines.Sum(l => l.Qty * l.UnitPrice);
        public decimal TotalDiscount    => Lines.Sum(l => l.Discount);
        public decimal TaxableAmount    => Subtotal - TotalDiscount;
        public const decimal TaxRate    = 0.15m;
        public decimal TaxAmount        => Math.Round(TaxableAmount * TaxRate, 2);
        public decimal GrandTotal       => TaxableAmount + TaxAmount;
        public decimal RemainingAmount  => Math.Max(0, GrandTotal - _paidAmount);

        public PaymentStatus PaymentStatus => _paidAmount <= 0
            ? PaymentStatus.Unpaid
            : _paidAmount >= GrandTotal
                ? PaymentStatus.Paid
                : PaymentStatus.Partial;

        // ── Business rules ────────────────────────────────────

        public bool CanEdit    => _status == InvoiceStatus.Draft;
        public bool CanReturn  => _status == InvoiceStatus.Posted;
        public bool CanCancel  => _status is InvoiceStatus.Draft or InvoiceStatus.Posted;
        public bool IsPosted   => _status == InvoiceStatus.Posted;
        public bool IsCancelled => _status == InvoiceStatus.Cancelled;

        // ── Display helpers ───────────────────────────────────

        public string CustomerName(bool ar) => ar ? CustomerNameAr : CustomerNameEn;

        public string StatusDisplayAr => _status switch
        {
            InvoiceStatus.Posted    => "مرحل",
            InvoiceStatus.Cancelled => "ملغي",
            InvoiceStatus.Returned  => "مرتجع",
            _                       => "مسودة"
        };

        public string StatusDisplayEn => _status switch
        {
            InvoiceStatus.Posted    => "Posted",
            InvoiceStatus.Cancelled => "Cancelled",
            InvoiceStatus.Returned  => "Returned",
            _                       => "Draft"
        };

        public string StatusDisplay(bool ar) => ar ? StatusDisplayAr : StatusDisplayEn;

        public string PayStatusDisplayAr => PaymentStatus switch
        {
            PaymentStatus.Paid    => "مسدد",
            PaymentStatus.Partial => "جزئي",
            _                     => "غير مسدد"
        };

        public string PayStatusDisplayEn => PaymentStatus switch
        {
            PaymentStatus.Paid    => "Paid",
            PaymentStatus.Partial => "Partial",
            _                     => "Unpaid"
        };

        public string PayStatusDisplay(bool ar) => ar ? PayStatusDisplayAr : PayStatusDisplayEn;

        public string TypeDisplay(bool ar) => Type switch
        {
            InvoiceType.Credit => ar ? "آجل" : "Credit",
            _                  => ar ? "نقدي" : "Cash"
        };

        public string PayMethodDisplay(bool ar) => PayMethod switch
        {
            SalesPayMethod.Card     => ar ? "بطاقة" : "Card",
            SalesPayMethod.Transfer => ar ? "تحويل" : "Transfer",
            SalesPayMethod.Split    => ar ? "مقسم"  : "Split",
            _                       => ar ? "نقداً"  : "Cash"
        };

        // TODO: attach receipt voucher link
        // TODO: attach journal entry link
        // TODO: attach delivery status
        // TODO: attach approval workflow

        public event PropertyChangedEventHandler? PropertyChanged;
        private void Notify(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    // ══════════════════════════════════════════════════════════
    //  SUMMARY STATS
    // ══════════════════════════════════════════════════════════

    public class SalesSummaryStats
    {
        public int     TotalInvoices    { get; set; }
        public int     TodayInvoices    { get; set; }
        public decimal TotalSales       { get; set; }
        public decimal CreditSales      { get; set; }
        public decimal Unpaid           { get; set; }
        public decimal TotalReturns     { get; set; }

        public static SalesSummaryStats Compute(IEnumerable<SalesInvoice> invoices)
        {
            var list = invoices.ToList();
            var today = DateTime.Today;
            return new SalesSummaryStats
            {
                TotalInvoices  = list.Count,
                TodayInvoices  = list.Count(i => i.Date.Date == today),
                TotalSales     = list.Where(i => i.Status != InvoiceStatus.Cancelled).Sum(i => i.GrandTotal),
                CreditSales    = list.Where(i => i.Type == InvoiceType.Credit && i.Status == InvoiceStatus.Posted).Sum(i => i.GrandTotal),
                Unpaid         = list.Where(i => i.PaymentStatus != PaymentStatus.Paid && i.Status == InvoiceStatus.Posted).Sum(i => i.RemainingAmount),
                TotalReturns   = list.Where(i => i.Status == InvoiceStatus.Returned).Sum(i => i.GrandTotal)
            };
        }
    }
}
