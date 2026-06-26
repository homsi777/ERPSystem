using System.Collections.ObjectModel;
using System.ComponentModel;

namespace ERPSystem.Core.POS
{
    // ══════════════════════════════════════════════════════════
    //  ENUMS
    // ══════════════════════════════════════════════════════════

    public enum PaymentMethod { Cash, Card, Transfer, Split }
    public enum DiscountType { Fixed, Percentage }
    public enum SessionStatus { Open, Suspended, Closed }
    public enum StockStatus { Available, Low, OutOfStock }

    // ══════════════════════════════════════════════════════════
    //  CATEGORY
    // ══════════════════════════════════════════════════════════

    public class POSCategory
    {
        public string Id { get; init; } = "";
        public string NameAr { get; init; } = "";
        public string NameEn { get; init; } = "";
        public string Icon { get; init; } = "\uECA5";
        public bool IsAll { get; init; }

        public string Name(bool ar) => ar ? NameAr : NameEn;
    }

    // ══════════════════════════════════════════════════════════
    //  PRODUCT
    // ══════════════════════════════════════════════════════════

    public class POSProduct
    {
        public string Id { get; init; } = "";
        public string NameAr { get; init; } = "";
        public string NameEn { get; init; } = "";
        public string SKU { get; init; } = "";
        public string CategoryId { get; init; } = "";
        public decimal Price { get; init; }
        public int Stock { get; set; }
        public string Unit { get; init; } = "قطعة";
        public string Icon { get; init; } = "\uE821";
        public string IconColor { get; init; } = "#2563EB";
        public string IconBgColor { get; init; } = "#EFF6FF";

        public string Name(bool ar) => ar ? NameAr : NameEn;

        public StockStatus StockStatus => Stock switch
        {
            0 => StockStatus.OutOfStock,
            <= 5 => StockStatus.Low,
            _ => StockStatus.Available
        };

        public bool IsAvailable => Stock > 0;

        public string PriceFormatted(bool ar) =>
            ar ? $"{Price:N2} ر.س" : $"SAR {Price:N2}";
    }

    // ══════════════════════════════════════════════════════════
    //  CART ITEM
    // ══════════════════════════════════════════════════════════

    public class POSCartItem : INotifyPropertyChanged
    {
        private int _quantity = 1;
        private decimal _lineDiscount;
        private DiscountType _discountType = DiscountType.Fixed;

        public POSProduct Product { get; init; } = null!;

        public int Quantity
        {
            get => _quantity;
            set
            {
                if (value >= 1)
                {
                    _quantity = value;
                    Notify(nameof(Quantity));
                    Notify(nameof(DisplayQtyPrice));
                    Notify(nameof(LineDiscountAmount));
                    Notify(nameof(LineTotal));
                }
            }
        }

        public decimal LineDiscount
        {
            get => _lineDiscount;
            set
            {
                _lineDiscount = value >= 0 ? value : 0;
                Notify(nameof(LineDiscount));
                Notify(nameof(LineDiscountAmount));
                Notify(nameof(LineTotal));
            }
        }

        public DiscountType DiscountType
        {
            get => _discountType;
            set
            {
                _discountType = value;
                Notify(nameof(DiscountType));
                Notify(nameof(LineDiscountAmount));
                Notify(nameof(LineTotal));
            }
        }

        public decimal UnitPrice => Product.Price;
        public decimal GrossLine => UnitPrice * Quantity;

        public decimal LineDiscountAmount => _discountType == DiscountType.Percentage
            ? Math.Round(GrossLine * _lineDiscount / 100, 2)
            : Math.Min(_lineDiscount, GrossLine);

        public decimal LineTotal => GrossLine - LineDiscountAmount;

        public bool HasDiscount => LineDiscountAmount > 0;

        public string DisplayQtyPrice(bool ar) =>
            ar ? $"{UnitPrice:N2} ر.س × {Quantity}" : $"SAR {UnitPrice:N2} × {Quantity}";

        public string LineTotalFormatted(bool ar) =>
            ar ? $"{LineTotal:N2} ر.س" : $"SAR {LineTotal:N2}";

        public event PropertyChangedEventHandler? PropertyChanged;
        private void Notify(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    // ══════════════════════════════════════════════════════════
    //  ORDER
    // ══════════════════════════════════════════════════════════

    public class POSOrder : INotifyPropertyChanged
    {
        private string _customerName = "";
        private int? _customerId;
        private decimal _orderDiscount;
        private DiscountType _orderDiscountType = DiscountType.Fixed;
        private PaymentMethod _paymentMethod = PaymentMethod.Cash;
        private string _notes = "";
        private string _couponCode = "";

        public ObservableCollection<POSCartItem> Items { get; } = new();

        public string CustomerName
        {
            get => _customerName;
            set { _customerName = value; Notify(nameof(CustomerName)); }
        }

        public int? CustomerId
        {
            get => _customerId;
            set { _customerId = value; Notify(nameof(CustomerId)); Notify(nameof(HasCustomer)); }
        }

        public bool HasCustomer => _customerId.HasValue;

        public string Notes
        {
            get => _notes;
            set { _notes = value; Notify(nameof(Notes)); }
        }

        public string CouponCode
        {
            get => _couponCode;
            set { _couponCode = value; Notify(nameof(CouponCode)); }
        }

        public PaymentMethod PaymentMethod
        {
            get => _paymentMethod;
            set { _paymentMethod = value; Notify(nameof(PaymentMethod)); }
        }

        public decimal OrderDiscount
        {
            get => _orderDiscount;
            set { _orderDiscount = value >= 0 ? value : 0; NotifyTotals(); }
        }

        public DiscountType OrderDiscountType
        {
            get => _orderDiscountType;
            set { _orderDiscountType = value; NotifyTotals(); }
        }

        // ── Computed totals ─────────────────────────────────────────────

        public decimal Subtotal => Items.Sum(i => i.GrossLine);
        public decimal LineDiscountsTotal => Items.Sum(i => i.LineDiscountAmount);

        public decimal OrderDiscountAmount => _orderDiscountType == DiscountType.Percentage
            ? Math.Round((Subtotal - LineDiscountsTotal) * _orderDiscount / 100, 2)
            : Math.Min(_orderDiscount, Subtotal - LineDiscountsTotal);

        public decimal TotalDiscount => LineDiscountsTotal + OrderDiscountAmount;
        public decimal TaxableAmount => Math.Max(0, Subtotal - TotalDiscount);
        public const decimal TaxRate = 0.15m;
        public decimal TaxAmount => Math.Round(TaxableAmount * TaxRate, 2);
        public decimal GrandTotal => TaxableAmount + TaxAmount;

        public bool IsEmpty => !Items.Any();
        public int TotalItemCount => Items.Sum(i => i.Quantity);

        public void NotifyTotals()
        {
            foreach (var p in new[] {
                nameof(Subtotal), nameof(LineDiscountsTotal), nameof(OrderDiscountAmount),
                nameof(TotalDiscount), nameof(TaxableAmount), nameof(TaxAmount),
                nameof(GrandTotal), nameof(IsEmpty), nameof(TotalItemCount)
            }) Notify(p);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void Notify(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    // ══════════════════════════════════════════════════════════
    //  SESSION
    // ══════════════════════════════════════════════════════════

    public class POSSession : INotifyPropertyChanged
    {
        private int _orderCount;
        private decimal _totalCash;
        private decimal _totalCard;
        private decimal _totalTransfer;

        public string SessionNumber { get; init; } = "POS-0042";
        public string CashierNameAr { get; init; } = "أحمد محمد";
        public string CashierNameEn { get; init; } = "Ahmed Mohammed";
        public string TerminalId { get; init; } = "T-01";
        public DateTime OpenedAt { get; init; } = DateTime.Now;
        public SessionStatus Status { get; set; } = SessionStatus.Open;

        public List<POSOrder> HeldOrders { get; } = new();
        public int HeldCount => HeldOrders.Count;

        public int OrderCount
        {
            get => _orderCount;
            private set { _orderCount = value; Notify(nameof(OrderCount)); }
        }

        public decimal TotalSales => _totalCash + _totalCard + _totalTransfer;

        public string ElapsedDisplay
        {
            get
            {
                var elapsed = DateTime.Now - OpenedAt;
                return elapsed.TotalHours >= 1
                    ? $"{(int)elapsed.TotalHours}h {elapsed.Minutes}m"
                    : $"{elapsed.Minutes}m";
            }
        }

        public string CashierName(bool ar) => ar ? CashierNameAr : CashierNameEn;

        public void RecordSale(POSOrder order)
        {
            OrderCount++;
            switch (order.PaymentMethod)
            {
                case PaymentMethod.Cash: _totalCash += order.GrandTotal; break;
                case PaymentMethod.Card: _totalCard += order.GrandTotal; break;
                case PaymentMethod.Transfer: _totalTransfer += order.GrandTotal; break;
            }
            Notify(nameof(TotalSales));
        }

        public void HoldOrder(POSOrder order)
        {
            HeldOrders.Add(order);
            Notify(nameof(HeldCount));
        }

        public POSOrder? RecallOrder(int index)
        {
            if (index < 0 || index >= HeldOrders.Count) return null;
            var order = HeldOrders[index];
            HeldOrders.RemoveAt(index);
            Notify(nameof(HeldCount));
            return order;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void Notify(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    // ══════════════════════════════════════════════════════════
    //  SAMPLE DATA PROVIDER
    // ══════════════════════════════════════════════════════════

    public static class POSSampleData
    {
        public static List<POSCategory> GetCategories() => new()
        {
            new() { Id = "ALL", NameAr = "الكل", NameEn = "All", Icon = "\uECA5", IsAll = true },
            new() { Id = "ELEC", NameAr = "إلكترونيات", NameEn = "Electronics", Icon = "\uE950" },
            new() { Id = "MOBILE", NameAr = "هواتف", NameEn = "Phones", Icon = "\uE8EA" },
            new() { Id = "COMP", NameAr = "حاسبات", NameEn = "Computers", Icon = "\uE7C3" },
            new() { Id = "ACC", NameAr = "ملحقات", NameEn = "Accessories", Icon = "\uE767" },
            new() { Id = "SCREEN", NameAr = "شاشات", NameEn = "Screens", Icon = "\uE7F4" },
            new() { Id = "SVC", NameAr = "خدمات", NameEn = "Services", Icon = "\uE713" },
        };

        public static List<POSProduct> GetProducts() => new()
        {
            new() { Id = "P001", NameAr = "لاب توب Dell XPS 15", NameEn = "Dell XPS 15 Laptop",
                    SKU = "LT-001", CategoryId = "COMP", Price = 4500, Stock = 12,
                    Icon = "\uE950", IconColor = "#2563EB", IconBgColor = "#EFF6FF" },
            new() { Id = "P002", NameAr = "iPhone 15 Pro 256GB", NameEn = "iPhone 15 Pro 256GB",
                    SKU = "PH-001", CategoryId = "MOBILE", Price = 3999, Stock = 8,
                    Icon = "\uE8EA", IconColor = "#7C3AED", IconBgColor = "#EDE9FE" },
            new() { Id = "P003", NameAr = "سماعة Sony WH-1000XM5", NameEn = "Sony WH-1000XM5",
                    SKU = "AC-001", CategoryId = "ACC", Price = 750, Stock = 25,
                    Icon = "\uE767", IconColor = "#059669", IconBgColor = "#ECFDF5" },
            new() { Id = "P004", NameAr = "كاميرا Canon EOS R8", NameEn = "Canon EOS R8",
                    SKU = "EL-001", CategoryId = "ELEC", Price = 2200, Stock = 5,
                    Icon = "\uE722", IconColor = "#D97706", IconBgColor = "#FFFBEB" },
            new() { Id = "P005", NameAr = "ماوس Logitech MX Master 3", NameEn = "Logitech MX Master 3",
                    SKU = "AC-002", CategoryId = "ACC", Price = 180, Stock = 40,
                    Icon = "\uE7C3", IconColor = "#3B82F6", IconBgColor = "#EFF6FF" },
            new() { Id = "P006", NameAr = "شاشة LG 27 4K UHD", NameEn = "LG 27\" 4K UHD Monitor",
                    SKU = "SC-001", CategoryId = "SCREEN", Price = 1350, Stock = 7,
                    Icon = "\uE7F4", IconColor = "#0891B2", IconBgColor = "#ECFEFF" },
            new() { Id = "P007", NameAr = "لوحة مفاتيح Keychron K8", NameEn = "Keychron K8 Keyboard",
                    SKU = "AC-003", CategoryId = "ACC", Price = 320, Stock = 18,
                    Icon = "\uE92E", IconColor = "#7C3AED", IconBgColor = "#EDE9FE" },
            new() { Id = "P008", NameAr = "Samsung Galaxy S24 Ultra", NameEn = "Samsung S24 Ultra",
                    SKU = "PH-002", CategoryId = "MOBILE", Price = 4200, Stock = 3,
                    Icon = "\uE8EA", IconColor = "#0891B2", IconBgColor = "#ECFEFF" },
            new() { Id = "P009", NameAr = "iPad Pro 12.9 M4", NameEn = "iPad Pro 12.9 M4",
                    SKU = "COMP-002", CategoryId = "COMP", Price = 3600, Stock = 6,
                    Icon = "\uE7C3", IconColor = "#059669", IconBgColor = "#ECFDF5" },
            new() { Id = "P010", NameAr = "طابعة HP LaserJet Pro", NameEn = "HP LaserJet Pro",
                    SKU = "EL-002", CategoryId = "ELEC", Price = 850, Stock = 9,
                    Icon = "\uE749", IconColor = "#DC2626", IconBgColor = "#FEF2F2" },
            new() { Id = "P011", NameAr = "هارد SSD 1TB Samsung", NameEn = "Samsung 1TB SSD",
                    SKU = "COMP-003", CategoryId = "COMP", Price = 280, Stock = 30,
                    Icon = "\uEDA2", IconColor = "#2563EB", IconBgColor = "#EFF6FF" },
            new() { Id = "P012", NameAr = "خدمة صيانة وتنظيف", NameEn = "Maintenance Service",
                    SKU = "SVC-001", CategoryId = "SVC", Price = 150, Stock = 999, Unit = "خدمة",
                    Icon = "\uE90F", IconColor = "#D97706", IconBgColor = "#FFFBEB" },
        };
    }
}
