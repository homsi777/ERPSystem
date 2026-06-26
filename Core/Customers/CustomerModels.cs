using System.Collections.ObjectModel;
using System.ComponentModel;

namespace ERPSystem.Core.Customers
{
    // ══════════════════════════════════════════════════════════
    //  ENUMS
    // ══════════════════════════════════════════════════════════

    public enum CustomerType { Cash, Credit }
    public enum CustomerStatus { Active, Suspended, Blocked }
    public enum BalanceDirection { Neutral, Debit, Credit }   // Debit = customer owes us
    public enum TransactionType { Invoice, Receipt, Return, Settlement, CreditNote }

    // ══════════════════════════════════════════════════════════
    //  CUSTOMER MODEL
    // ══════════════════════════════════════════════════════════

    public class CustomerModel : INotifyPropertyChanged
    {
        private string _code = "";
        private string _nameAr = "";
        private string _nameEn = "";
        private string _phone = "";
        private string _phone2 = "";
        private string _address = "";
        private string _region = "";
        private string _salesRep = "";
        private string _notes = "";
        private CustomerType _type = CustomerType.Cash;
        private CustomerStatus _status = CustomerStatus.Active;
        private decimal _balance;
        private decimal _creditLimit;
        private DateTime? _lastInvoiceDate;
        private int _totalInvoices;
        private string _taxNumber = "";
        private string _priceLevel = "";
        private string _group = "";

        // ── Identity ────────────────────────────────────────────

        public string Code
        {
            get => _code;
            set { _code = value; Notify(nameof(Code)); }
        }

        public string NameAr
        {
            get => _nameAr;
            set { _nameAr = value; Notify(nameof(NameAr)); Notify(nameof(DisplayName)); }
        }

        public string NameEn
        {
            get => _nameEn;
            set { _nameEn = value; Notify(nameof(NameEn)); Notify(nameof(DisplayName)); }
        }

        public string Phone
        {
            get => _phone;
            set { _phone = value; Notify(nameof(Phone)); }
        }

        public string Phone2
        {
            get => _phone2;
            set { _phone2 = value; Notify(nameof(Phone2)); }
        }

        public string Address
        {
            get => _address;
            set { _address = value; Notify(nameof(Address)); }
        }

        public string Region
        {
            get => _region;
            set { _region = value; Notify(nameof(Region)); }
        }

        public string SalesRep
        {
            get => _salesRep;
            set { _salesRep = value; Notify(nameof(SalesRep)); }
        }

        public string Notes
        {
            get => _notes;
            set { _notes = value; Notify(nameof(Notes)); }
        }

        public string TaxNumber
        {
            get => _taxNumber;
            set { _taxNumber = value; Notify(nameof(TaxNumber)); }
        }

        // TODO: attach pricing level
        public string PriceLevel
        {
            get => _priceLevel;
            set { _priceLevel = value; Notify(nameof(PriceLevel)); }
        }

        // TODO: attach customer group
        public string Group
        {
            get => _group;
            set { _group = value; Notify(nameof(Group)); }
        }

        // ── Account type & status ────────────────────────────────

        public CustomerType Type
        {
            get => _type;
            set { _type = value; Notify(nameof(Type)); Notify(nameof(TypeDisplay)); Notify(nameof(IsCredit)); }
        }

        public CustomerStatus Status
        {
            get => _status;
            set { _status = value; Notify(nameof(Status)); Notify(nameof(StatusDisplay)); Notify(nameof(IsActive)); }
        }

        // ── Financial ────────────────────────────────────────────

        /// <summary>Positive = customer owes us (debit). Negative = we owe customer (credit).</summary>
        public decimal Balance
        {
            get => _balance;
            set
            {
                _balance = value;
                Notify(nameof(Balance));
                Notify(nameof(BalanceDirection));
                Notify(nameof(AvailableCredit));
                Notify(nameof(IsOverLimit));
                Notify(nameof(BalanceDisplay));
            }
        }

        public decimal CreditLimit
        {
            get => _creditLimit;
            set
            {
                _creditLimit = value;
                Notify(nameof(CreditLimit));
                Notify(nameof(AvailableCredit));
                Notify(nameof(IsOverLimit));
            }
        }

        public DateTime? LastInvoiceDate
        {
            get => _lastInvoiceDate;
            set { _lastInvoiceDate = value; Notify(nameof(LastInvoiceDate)); Notify(nameof(LastInvoiceDisplay)); }
        }

        public int TotalInvoices
        {
            get => _totalInvoices;
            set { _totalInvoices = value; Notify(nameof(TotalInvoices)); }
        }

        // ── Computed ─────────────────────────────────────────────

        public string DisplayName(bool ar) => ar ? _nameAr : (_nameEn.Length > 0 ? _nameEn : _nameAr);

        public BalanceDirection BalanceDirection => _balance switch
        {
            > 0 => BalanceDirection.Debit,
            < 0 => BalanceDirection.Credit,
            _ => BalanceDirection.Neutral
        };

        public decimal AvailableCredit => Math.Max(0, _creditLimit - _balance);

        public bool IsOverLimit => _type == CustomerType.Credit && _creditLimit > 0 && _balance > _creditLimit;

        public bool IsCredit => _type == CustomerType.Credit;

        public bool IsActive => _status == CustomerStatus.Active;

        public bool HasNoActivity => _lastInvoiceDate == null || (DateTime.Now - _lastInvoiceDate.Value).TotalDays > 90;

        public string TypeDisplay(bool ar) => _type switch
        {
            CustomerType.Credit => ar ? "آجل" : "Credit",
            _ => ar ? "نقدي" : "Cash"
        };

        public string StatusDisplay(bool ar) => _status switch
        {
            CustomerStatus.Suspended => ar ? "موقوف" : "Suspended",
            CustomerStatus.Blocked => ar ? "محظور" : "Blocked",
            _ => ar ? "نشط" : "Active"
        };

        public string BalanceDisplay(bool ar)
        {
            string cur = ar ? "ر.س" : "SAR";
            if (_balance > 0) return ar ? $"عليه {_balance:N2} {cur}" : $"Owes {cur} {_balance:N2}";
            if (_balance < 0) return ar ? $"له {Math.Abs(_balance):N2} {cur}" : $"Credit {cur} {Math.Abs(_balance):N2}";
            return ar ? "صفر" : "Zero";
        }

        public string LastInvoiceDisplay(bool ar)
        {
            if (_lastInvoiceDate == null) return ar ? "لا يوجد" : "None";
            var days = (DateTime.Now - _lastInvoiceDate.Value).TotalDays;
            if (days < 1) return ar ? "اليوم" : "Today";
            if (days < 2) return ar ? "أمس" : "Yesterday";
            if (days < 7) return ar ? $"منذ {(int)days} أيام" : $"{(int)days} days ago";
            if (days < 30) return ar ? $"منذ {(int)(days / 7)} أسابيع" : $"{(int)(days / 7)} weeks ago";
            return _lastInvoiceDate.Value.ToString("dd/MM/yyyy");
        }

        // TODO: attach loyalty profile
        // TODO: attach territory/route
        // TODO: attach tax profile
        // TODO: attach sales representative assignment
        // TODO: attach opening balance workflow

        public ObservableCollection<CustomerTransaction> RecentTransactions { get; set; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;
        private void Notify(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    // ══════════════════════════════════════════════════════════
    //  TRANSACTION (for mini statement)
    // ══════════════════════════════════════════════════════════

    public class CustomerTransaction
    {
        public string Reference { get; init; } = "";
        public TransactionType Type { get; init; }
        public DateTime Date { get; init; }
        public decimal Amount { get; init; }
        public decimal RunningBalance { get; init; }
        public string Notes { get; init; } = "";

        public string TypeDisplayAr => Type switch
        {
            TransactionType.Invoice => "فاتورة مبيعات",
            TransactionType.Receipt => "سند قبض",
            TransactionType.Return => "مرتجع مبيعات",
            TransactionType.Settlement => "سند تسوية",
            TransactionType.CreditNote => "إشعار دائن",
            _ => "معاملة"
        };

        public string TypeDisplayEn => Type switch
        {
            TransactionType.Invoice => "Sales Invoice",
            TransactionType.Receipt => "Receipt Voucher",
            TransactionType.Return => "Sales Return",
            TransactionType.Settlement => "Settlement",
            TransactionType.CreditNote => "Credit Note",
            _ => "Transaction"
        };

        public string TypeIcon => Type switch
        {
            TransactionType.Invoice => "\uE8B7",
            TransactionType.Receipt => "\uE8C1",
            TransactionType.Return => "\uE72C",
            TransactionType.Settlement => "\uE8AB",
            TransactionType.CreditNote => "\uE8D5",
            _ => "\uE8B7"
        };

        // Debit = customer owes more (invoice), Credit = reduces balance (receipt/return)
        public bool IsDebit => Type == TransactionType.Invoice;
        public bool IsCredit => !IsDebit;
    }

    // ══════════════════════════════════════════════════════════
    //  SUMMARY STATS
    // ══════════════════════════════════════════════════════════

    public class CustomerSummaryStats
    {
        public int TotalCustomers { get; set; }
        public int ActiveCustomers { get; set; }
        public int CreditCustomers { get; set; }
        public decimal TotalReceivables { get; set; }
        public int OverLimitCount { get; set; }
        public int NoActivityCount { get; set; }

        public static CustomerSummaryStats Compute(IEnumerable<CustomerModel> customers)
        {
            var list = customers.ToList();
            return new CustomerSummaryStats
            {
                TotalCustomers = list.Count,
                ActiveCustomers = list.Count(c => c.IsActive),
                CreditCustomers = list.Count(c => c.IsCredit),
                TotalReceivables = list.Where(c => c.Balance > 0).Sum(c => c.Balance),
                OverLimitCount = list.Count(c => c.IsOverLimit),
                NoActivityCount = list.Count(c => c.HasNoActivity)
            };
        }
    }

    // ══════════════════════════════════════════════════════════
    //  SAMPLE DATA
    // ══════════════════════════════════════════════════════════

    public static class CustomerSampleData
    {
        private static readonly Random _rng = new(42);

        private static readonly string[] ArabicNames = {
            "محمد أحمد العتيبي", "شركة الأمل للتجارة", "مؤسسة النور التجارية",
            "فهد سعد القحطاني", "شركة الرياض للتقنية", "عبدالله محمد الزهراني",
            "مؤسسة الخليج الدولية", "سلمى إبراهيم الحربي", "شركة الفجر للمقاولات",
            "خالد عمر الشهري", "مجموعة النخبة التجارية", "يوسف علي المطيري",
            "شركة الساهر للإلكترونيات", "نورة حمد العنزي", "مؤسسة الربيع للاستيراد",
            "عمر سعيد الغامدي", "شركة ذروة الاستشارات", "هند محمد القرني",
            "مجموعة الريادة التجارية", "أحمد علي الدوسري", "مؤسسة البناء الحديث",
            "فاطمة سالم البلوي", "شركة نجد للمعدات", "تركي عبدالرحمن العسيري",
            "مؤسسة الشرق الأوسط", "منى خالد الجهني", "شركة هلا للتوزيع"
        };

        private static readonly string[] Regions = {
            "الرياض", "جدة", "مكة المكرمة", "المدينة المنورة", "الدمام",
            "الخبر", "تبوك", "أبها", "القصيم", "حائل"
        };

        private static readonly string[] Reps = {
            "خالد المنصور", "سارة العتيبي", "نواف الشمري", "ريم الزهراني", "بندر القحطاني"
        };

        public static List<CustomerModel> Generate(int count = 50)
        {
            var list = new List<CustomerModel>();

            for (int i = 0; i < Math.Min(count, ArabicNames.Length * 2); i++)
            {
                string nameAr = ArabicNames[i % ArabicNames.Length];
                bool isCredit = i % 3 != 0;
                decimal balance = _rng.Next(-2000, 60000);
                decimal limit = isCredit ? _rng.Next(10000, 100000) : 0;
                int daysAgo = _rng.Next(0, 180);
                var status = i % 15 == 0 ? CustomerStatus.Suspended : CustomerStatus.Active;

                var customer = new CustomerModel
                {
                    Code = $"CUST-{1000 + i:D4}",
                    NameAr = nameAr,
                    NameEn = nameAr,
                    Phone = $"05{_rng.Next(10000000, 99999999)}",
                    Phone2 = i % 4 == 0 ? $"01{_rng.Next(10000000, 99999999)}" : "",
                    Address = $"شارع {_rng.Next(1, 200)}، حي {(char)(65 + _rng.Next(0, 20))}",
                    Region = Regions[_rng.Next(Regions.Length)],
                    SalesRep = Reps[_rng.Next(Reps.Length)],
                    Type = isCredit ? CustomerType.Credit : CustomerType.Cash,
                    Status = status,
                    Balance = Math.Round(balance, 2),
                    CreditLimit = limit,
                    TotalInvoices = _rng.Next(0, 200),
                    LastInvoiceDate = daysAgo == 0 ? null : DateTime.Now.AddDays(-daysAgo),
                    Notes = i % 5 == 0 ? "عميل مميز - خصم 5%" : ""
                };

                customer.RecentTransactions = GenerateTransactions(customer);
                list.Add(customer);
            }

            return list;
        }

        private static ObservableCollection<CustomerTransaction> GenerateTransactions(CustomerModel c)
        {
            var types = new[] {
                TransactionType.Invoice, TransactionType.Receipt,
                TransactionType.Invoice, TransactionType.Return, TransactionType.Settlement
            };
            var refs = new[] { "INV-1023", "RCV-0445", "INV-0998", "RTN-0221", "SET-0112" };
            var amounts = new[] { 12000m, -5000m, 8000m, -1200m, -500m };

            var txns = new ObservableCollection<CustomerTransaction>();
            decimal running = c.Balance;
            for (int i = 0; i < 5; i++)
            {
                txns.Add(new CustomerTransaction
                {
                    Reference = refs[i],
                    Type = types[i],
                    Date = DateTime.Now.AddDays(-(i * 7 + _rng.Next(0, 5))),
                    Amount = Math.Abs(amounts[i]),
                    RunningBalance = running,
                    Notes = ""
                });
                running -= amounts[i];
            }
            return txns;
        }
    }
}
