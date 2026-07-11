using ERPSystem.Application.DTOs.Sales;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Core.Sales;

public sealed class SalesInvoiceListRow
{
    public Guid Id { get; init; }
    public string InvoiceNumber { get; init; } = "";
    public string CustomerName { get; init; } = "";
    public string Warehouse { get; init; } = "";
    public string Container { get; init; } = "";
    public int RollCount { get; init; }
    public decimal Amount { get; init; }
    public SalesInvoiceStatus Status { get; init; }
    public PaymentType PaymentType { get; init; }
    public DateTime Date { get; init; }

    public string StatusDisplay => Status switch
    {
        SalesInvoiceStatus.Draft => "مسودة",
        SalesInvoiceStatus.AwaitingDetailing => "بانتظار التفصيل",
        SalesInvoiceStatus.Detailed => "مفصلة",
        SalesInvoiceStatus.ReadyForApproval => "جاهزة للاعتماد",
        SalesInvoiceStatus.Approved => "معتمدة",
        SalesInvoiceStatus.Printed => "مطبوعة",
        SalesInvoiceStatus.Delivered => "مسلمة",
        SalesInvoiceStatus.Cancelled => "ملغاة",
        SalesInvoiceStatus.PartiallyReturned => "مرتجع جزئي",
        SalesInvoiceStatus.Returned => "مرتجعة كاملاً",
        _ => Status.ToString()
    };

    public static SalesInvoiceListRow FromDto(
        SalesInvoiceDto dto,
        string warehouseName,
        string containerName) => new()
    {
        Id = dto.Id,
        InvoiceNumber = dto.InvoiceNumber,
        CustomerName = dto.CustomerName,
        Warehouse = warehouseName,
        Container = containerName,
        RollCount = dto.Lines.Sum(l => l.RollCount),
        Amount = dto.GrandTotal,
        Status = dto.Status,
        PaymentType = dto.PaymentType,
        Date = dto.InvoiceDate
    };
}
