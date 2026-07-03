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
}
