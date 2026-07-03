using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.Common;

public static class SupplierPaymentTermsDisplay
{
    public static string Format(int paymentTermsDays) => paymentTermsDays switch
    {
        0 => "فوري",
        15 => "Net 15",
        30 => "Net 30",
        60 => "Net 60",
        90 => "Net 90",
        _ => $"{paymentTermsDays} يوم"
    };

    public static int[] StandardOptions { get; } = [0, 15, 30, 60, 90];
}
