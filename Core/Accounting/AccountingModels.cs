namespace ERPSystem.Core.Accounting
{
    public enum JournalStatus { Draft, Posted, Cancelled }

    public class JournalEntryModel
    {
        public string EntryNumber { get; set; } = "";
        public DateTime EntryDate { get; set; }
        public string Description { get; set; } = "";
        public decimal DebitTotal { get; set; }
        public decimal CreditTotal { get; set; }
        public JournalStatus Status { get; set; }
        public string CreatedBy { get; set; } = "";

        public string StatusDisplay => Status switch
        {
            JournalStatus.Draft => "مسودة",
            JournalStatus.Posted => "مرحّل",
            JournalStatus.Cancelled => "ملغي",
            _ => ""
        };
    }

    public static class AccountingSampleData
    {
        public static List<JournalEntryModel> Generate(int count = 20)
        {
            var rnd = new Random(31);
            var descs = new[]
            {
                "قيد مبيعات أقمشة", "سند قبض عميل", "سند دفع مورد",
                "قيد استيراد حاوية", "تسوية صندوق", "قيد رواتب"
            };

            return Enumerable.Range(1, count).Select(i =>
            {
                var amount = rnd.Next(1000, 80000);
                return new JournalEntryModel
                {
                    EntryNumber = $"JV-2026-{i:D4}",
                    EntryDate = DateTime.Today.AddDays(-rnd.Next(1, 60)),
                    Description = descs[rnd.Next(descs.Length)],
                    DebitTotal = amount,
                    CreditTotal = amount,
                    Status = (JournalStatus)(rnd.Next(3)),
                    CreatedBy = "أحمد الحمصي"
                };
            }).ToList();
        }
    }
}
