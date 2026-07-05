using ERPSystem.DocumentEngine.Models;

namespace ERPSystem.DocumentEngine.Services;

/// <summary>
/// Ready-made demo document models used for design previews and smoke tests.
/// These contain placeholder data only — no business logic and no real data
/// source — so the visual system can be reviewed independently of the ERP.
/// </summary>
public static class SampleDocuments
{
    public static CompanyBranding SampleBranding() => new()
    {
        Name = "شركة الأنظمة المتكاملة",
        Tagline = "Integrated Systems Trading Co.",
        Address = "الرياض - طريق الملك فهد - مبنى 1200",
        Phone = "+966 11 000 0000",
        Email = "info@example.com",
        Website = "www.example.com",
        TaxNumber = "300000000000003",
        CommercialRegister = "1010000000",
        FooterNote = "هذا المستند صادر إلكترونياً من نظام ERP ولا يحتاج إلى توقيع.",
        QrContent = "https://example.com/invoice/INV-2026-000123",
        WatermarkText = "PAID"
    };

    public static DocumentModel SalesInvoice()
    {
        return new DocumentModel
        {
            Type = DocumentType.SalesInvoice,
            Title = "فاتورة مبيعات",
            Subtitle = "Tax Invoice",
            Number = "INV-2026-000123",
            Status = DocumentStatus.Paid,
            PrimaryParty = new PartyInfo
            {
                Role = "فاتورة إلى",
                Name = "مؤسسة العميل التجارية",
                Kind = PartyKind.Customer,
                Address = "جدة - حي الروضة",
                Phone = "+966 12 111 2222",
                TaxNumber = "310000000000009",
                AccountCode = "CUST-0042"
            },
            HeaderFields =
            {
                new InfoField("تاريخ الإصدار", "2026-07-04"),
                new InfoField("تاريخ الاستحقاق", "2026-07-19"),
                new InfoField("المرجع", "SO-2026-0091"),
                new InfoField("العملة", "SAR"),
                new InfoField("مندوب المبيعات", "أحمد سالم")
            },
            SummaryCards =
            {
                new SummaryCard("عدد الأصناف", "3", Accent.Info),
                new SummaryCard("الإجمالي قبل الضريبة", "3,450.00", Accent.Primary),
                new SummaryCard("الضريبة (15%)", "517.50", Accent.Warning),
                new SummaryCard("الإجمالي النهائي", "3,967.50", Accent.Success)
            },
            Tables =
            {
                new DocumentTable
                {
                    Columns =
                    {
                        new TableColumn("#", TextAlign.Center, "40px"),
                        new TableColumn("الصنف"),
                        new TableColumn("الكمية", TextAlign.End, "70px", numeric: true),
                        new TableColumn("السعر", TextAlign.End, "100px", numeric: true),
                        new TableColumn("الإجمالي", TextAlign.End, "110px", numeric: true)
                    },
                    Rows =
                    {
                        new TableRow(new TableCell("1"), new TableCell("جهاز حاسب محمول"), new TableCell("2"), new TableCell("1,200.00"), new TableCell("2,400.00")),
                        new TableRow(new TableCell("2"), new TableCell("طابعة ليزر"), new TableCell("1"), new TableCell("650.00"), new TableCell("650.00")),
                        new TableRow(new TableCell("3"), new TableCell("حبر أسود"), new TableCell("4"), new TableCell("100.00"), new TableCell("400.00"))
                    }
                }
            },
            TaxLines =
            {
                new TaxLine { Label = "ضريبة القيمة المضافة", Rate = "15%", Base = "3,450.00", Amount = "517.50" }
            },
            Totals = new TotalsModel
            {
                Lines =
                {
                    new TotalLine("الإجمالي الفرعي", "3,450.00"),
                    new TotalLine("الخصم", "0.00"),
                    new TotalLine("الضريبة", "517.50"),
                    new TotalLine("الإجمالي المستحق", "3,967.50", isGrand: true)
                }
            },
            Timeline =
            {
                new TimelineEntry { Time = "2026-07-04 09:12", Title = "تم إنشاء الفاتورة", Accent = Accent.Info },
                new TimelineEntry { Time = "2026-07-04 09:20", Title = "تم الاعتماد", Accent = Accent.Primary },
                new TimelineEntry { Time = "2026-07-04 10:05", Title = "تم السداد بالكامل", Accent = Accent.Success }
            },
            Approval = new ApprovalInfo { State = ApprovalState.Approved, By = "المدير المالي", Date = "2026-07-04" },
            Notes = "شكراً لتعاملكم معنا.",
            Terms = "تُستحق المدفوعات خلال 15 يوماً من تاريخ الفاتورة.",
            Signatures =
            {
                new SignatureSlot("توقيع المستلم"),
                new SignatureSlot("توقيع المحاسب")
            }
        };
    }
}
