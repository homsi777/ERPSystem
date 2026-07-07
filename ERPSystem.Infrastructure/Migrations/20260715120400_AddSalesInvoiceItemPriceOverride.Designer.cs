using ERPSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Infrastructure.Migrations;

[DbContext(typeof(ErpDbContext))]
[Migration("20260715120400_AddSalesInvoiceItemPriceOverride")]
partial class AddSalesInvoiceItemPriceOverride
{
}
