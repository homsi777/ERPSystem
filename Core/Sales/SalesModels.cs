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

    // ══════════════════════════════════════════════════════════
    //  SAMPLE DATA
    // ══════════════════════════════════════════════════════════

    public static class SalesSampleData
    {
        private static readonly Random _rng = new(99);

        private static readonly string[] CustomerNames = {
            "محمد أحمد العتيبي", "محل الأناقة للأقمشة", "مؤسسة النور للنسيج",
            "فهد سعد القحطاني", "شركة الرياض للأقمشة", "عبدالله الزهراني",
            "مجموعة النخبة للتجارة", "يوسف علي المطيري", "نورة حمد العنزي",
            "مؤسسة الربيع للاستيراد", "شركة هلا للتوزيع", "أحمد علي الدوسري"
        };

        private static readonly string[] CustomerPhones = {
            "0501234567", "0509876543", "0557891234", "0554561230",
            "0501112223", "0509998887", "0551234999", "0505556667"
        };

        private static readonly string[] Users = { "أحمد محمد", "سارة العتيبي", "خالد المنصور", "ريم الزهراني" };
        private static readonly string[] Branches = { "الرئيسي", "فرع جدة", "فرع الدمام" };

        private static readonly (string Ar, string En, string Code, decimal Price)[] Products = {
            ("كتان F12 — أبيض",     "Cotton F12 White",   "FAB-101",  42.5m),
            ("شيفون S8 — بيج",      "Chiffon S8 Beige",   "FAB-102",  38.0m),
            ("ساتان M3 — أسود",     "Satin M3 Black",     "FAB-103",  45.0m),
            ("كولومبيا — رمادي",    "Colombia Grey",      "FAB-104",  52.0m),
            ("فيسكوز V2 — كريمي",   "Viscose V2 Cream",   "FAB-105",  36.5m),
            ("جاكار J5 — ذهبي",     "Jacquard J5 Gold",   "FAB-106",  58.0m),
            ("لين L1 — طبيعي",      "Linen L1 Natural",   "FAB-107",  48.0m),
            ("تول T4 — أبيض",       "Tulle T4 White",     "FAB-108",  28.0m),
            ("دانتيل D2 — أبيض",    "Lace D2 White",      "FAB-109",  65.0m),
            ("مخمل V8 — كحلي",      "Velvet V8 Navy",     "FAB-110",  72.0m),
        };

        public static List<SalesInvoice> Generate(int count = 60)
        {
            var list = new List<SalesInvoice>();
            var statuses = new[] { InvoiceStatus.Posted, InvoiceStatus.Posted, InvoiceStatus.Posted,
                                   InvoiceStatus.Draft, InvoiceStatus.Cancelled, InvoiceStatus.Returned };

            for (int i = 0; i < count; i++)
            {
                var custIdx  = _rng.Next(CustomerNames.Length);
                var status   = statuses[_rng.Next(statuses.Length)];
                var type     = i % 3 == 0 ? InvoiceType.Cash : InvoiceType.Credit;
                var method   = (SalesPayMethod)_rng.Next(3);
                int daysBack = _rng.Next(0, 90);

                var inv = new SalesInvoice
                {
                    InvoiceNumber  = $"INV-2026-{1000 + i:D4}",
                    Date           = DateTime.Now.AddDays(-daysBack).AddHours(-_rng.Next(0, 8)),
                    Branch         = Branches[_rng.Next(Branches.Length)],
                    UserName       = Users[_rng.Next(Users.Length)],
                    CustomerId     = $"CUST-{custIdx:D4}",
                    CustomerNameAr = CustomerNames[custIdx],
                    CustomerNameEn = CustomerNames[custIdx],
                    CustomerPhone  = CustomerPhones[_rng.Next(CustomerPhones.Length)],
                    CustomerBalance = Math.Round((decimal)_rng.NextDouble() * 50000, 2),
                    CustomerLimit  = type == InvoiceType.Credit ? _rng.Next(20000, 100000) : 0,
                    CustomerIsCredit = type == InvoiceType.Credit,
                    Type           = type,
                    PayMethod      = type == InvoiceType.Cash ? method : SalesPayMethod.Transfer,
                    Notes          = i % 8 == 0 ? "تسليم عاجل" : "",
                };

                // Add status
                inv.Status = status;

                // Build 2-4 random lines
                var usedItems = new HashSet<int>();
                int lineCount = _rng.Next(2, 5);
                for (int l = 0; l < lineCount; l++)
                {
                    int pIdx;
                    do { pIdx = _rng.Next(Products.Length); } while (!usedItems.Add(pIdx));
                    var (ar, en, code, price) = Products[pIdx];
                    decimal qty = _rng.Next(50, 500);
                    inv.Lines.Add(new InvoiceLine
                    {
                        ItemCode    = code,
                        ItemNameAr  = ar,
                        ItemNameEn  = en,
                        Unit        = "متر",
                        Qty         = qty,
                        UnitPrice   = price,
                        Discount    = _rng.Next(0, 2) == 0 ? Math.Round(price * qty * 0.03m, 2) : 0
                    });
                }

                // Set paid amount
                inv.PaidAmount = status switch
                {
                    InvoiceStatus.Posted when _rng.Next(3) == 0 => 0,
                    InvoiceStatus.Posted when _rng.Next(3) == 1 => Math.Round(inv.GrandTotal / 2, 2),
                    InvoiceStatus.Posted => inv.GrandTotal,
                    InvoiceStatus.Cancelled => 0,
                    InvoiceStatus.Returned  => inv.GrandTotal,
                    _ => 0
                };

                list.Add(inv);
            }

            return list.OrderByDescending(i => i.Date).ToList();
        }
    }
}
