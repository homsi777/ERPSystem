using ERPSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Infrastructure.Migrations;

[DbContext(typeof(ErpDbContext))]
[Migration("20260715120200_AddSalesInvoiceItemNotes")]
partial class AddSalesInvoiceItemNotes
{
}
