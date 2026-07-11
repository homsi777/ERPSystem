# Print/Export Template Discovery Report

**Scope:** read-only discovery of the current repository state for print/export work, with Sales Invoice as the first target.  
**Repository:** `ERPSystem`  
**Discovery date:** 2026-07-11  
**No product code, UI, templates, or styles were changed as part of this task.**

## Executive summary

The repository currently has **three separate document-generation paths**:

1. The live WPF application generates Sales Invoice, Delivery Note, Purchase Invoice, and customer/supplier account-statement PDFs with **QuestPDF 2024.12.3**. These are real, wired features and use A4 with a QuestPDF margin of `30` points.
2. `ERPSystem.DocumentEngine` is a standalone, host-agnostic **HTML/CSS renderer** with 22 registered document types and a shared embedded design system. It is **not referenced or used by the WPF, API, or web projects**, has no concrete HTML-to-PDF converter, and currently **does not compile** because `ReceiptVoucherTemplate.cs` cannot resolve `RenderContext`.
3. The React/Vite client has a separate, wired but explicitly preliminary **jsPDF 4.2.1** export helper. Sales Invoice detail uses it, but the output is a simple text/section PDF rather than a branded invoice table.

There is therefore no single unified template in active use. The current live Sales Invoice PDF is `Services/Sales/SalesDocumentService.cs`, not `ERPSystem.DocumentEngine/Templates/SalesInvoice/SalesInvoiceTemplate.cs`.

## 1. Existing print/export/PDF templates and assets

### 1.1 `ERPSystem.DocumentEngine` templates

All template classes below are registered by `ERPSystem.DocumentEngine/Services/TemplateRegistry.cs`. Except where noted, each class only declares its `DocumentType` and inherits the same generic composition from `Templates/Shared/BaseDocumentTemplate.cs`; it does not contain document-specific field mapping or layout.

| File | Declared purpose | Current status |
|---|---|---|
| `ERPSystem.DocumentEngine/Templates/SalesInvoice/SalesInvoiceTemplate.cs` | Sales invoice | Registered, generic/partial, not integrated |
| `ERPSystem.DocumentEngine/Templates/PurchaseInvoice/PurchaseInvoiceTemplate.cs` | Purchase invoice and purchase order (two classes) | Registered, generic/partial, not integrated |
| `ERPSystem.DocumentEngine/Templates/Quotation/QuotationTemplate.cs` | Quotation | Registered, generic/partial, not integrated |
| `ERPSystem.DocumentEngine/Templates/CustomerStatement/CustomerStatementTemplate.cs` | Customer statement | Registered, generic/partial, not integrated |
| `ERPSystem.DocumentEngine/Templates/SupplierStatement/SupplierStatementTemplate.cs` | Supplier statement | Registered, generic/partial, not integrated |
| `ERPSystem.DocumentEngine/Templates/ReceiptVoucher/ReceiptVoucherTemplate.cs` | Receipt voucher; adds a `REVERSED` alert when `StatusLabel == "REVERSED"` | Registered but **broken**: compile error CS0246, `RenderContext` not found at line 11 |
| `ERPSystem.DocumentEngine/Templates/PaymentVoucher/PaymentVoucherTemplate.cs` | Payment voucher | Registered, generic/partial, not integrated |
| `ERPSystem.DocumentEngine/Templates/ExpenseVoucher/ExpenseVoucherTemplate.cs` | Expense voucher | Registered, generic/partial, not integrated |
| `ERPSystem.DocumentEngine/Templates/InventoryTransfer/InventoryTransferTemplate.cs` | Inventory transfer | Registered, generic/partial, not integrated |
| `ERPSystem.DocumentEngine/Templates/Stocktake/StocktakeTemplate.cs` | Stocktake and opening stock (two classes) | Registered, generic/partial, not integrated |
| `ERPSystem.DocumentEngine/Templates/InventoryReport/InventoryReportTemplate.cs` | Inventory report | Registered, generic/partial, not integrated |
| `ERPSystem.DocumentEngine/Templates/ContainerReport/ContainerReportTemplate.cs` | Container report | Registered, generic/partial, not integrated |
| `ERPSystem.DocumentEngine/Templates/PartnerStatement/PartnerStatementTemplate.cs` | Partner statement | Registered, generic/partial, not integrated |
| `ERPSystem.DocumentEngine/Templates/ExecutiveReports/ExecutiveReportTemplates.cs` | Executive dashboard, trial balance, balance sheet, income statement, cash flow, general ledger, journal voucher (seven classes) | Registered, generic/partial, not integrated |
| `ERPSystem.DocumentEngine/Templates/Shared/BaseDocumentTemplate.cs` | Shared document composition: parties/header fields, summaries, tables, tax/totals, approval, timeline, notes/terms, attachments, signatures/QR | Core shared template; source for nearly all template output |
| `ERPSystem.DocumentEngine/Templates/Shared/IDocumentTemplate.cs` | Template contract | Core infrastructure |

The registry contains **22 types**: `SalesInvoice`, `PurchaseInvoice`, `PurchaseOrder`, `Quotation`, `CustomerStatement`, `SupplierStatement`, `ReceiptVoucher`, `PaymentVoucher`, `ExpenseVoucher`, `InventoryTransfer`, `OpeningStock`, `Stocktake`, `InventoryReport`, `ContainerReport`, `PartnerStatement`, `ExecutiveDashboardReport`, `TrialBalance`, `BalanceSheet`, `IncomeStatement`, `CashFlow`, `GeneralLedger`, and `JournalVoucher`.

### 1.2 `ERPSystem.DocumentEngine` CSS/template assets

These files are embedded resources and are combined, in the following cascade, by `Services/AssetProvider.cs`:

| File | Purpose/status |
|---|---|
| `ERPSystem.DocumentEngine/Assets/css/variables.css` | Document-engine tokens, themes, and page geometry; active in engine output |
| `ERPSystem.DocumentEngine/Assets/fonts/fonts.css` | Cairo/Inter `@font-face` declarations; active CSS, but actual `.woff/.woff2` font files are absent |
| `ERPSystem.DocumentEngine/Assets/css/base.css` | Base/reset and typography |
| `ERPSystem.DocumentEngine/Assets/css/layout.css` | A4 sheet shell, header/body/footer, grid/flex layout |
| `ERPSystem.DocumentEngine/Assets/css/components.css` | Shared document components |
| `ERPSystem.DocumentEngine/Assets/css/tables.css` | Document tables |
| `ERPSystem.DocumentEngine/Assets/css/forms.css` | Form-like document elements |
| `ERPSystem.DocumentEngine/Assets/css/cards.css` | Summary/info cards |
| `ERPSystem.DocumentEngine/Assets/css/badges.css` | Status/accent badges |
| `ERPSystem.DocumentEngine/Assets/css/timeline.css` | Timeline component |
| `ERPSystem.DocumentEngine/Assets/css/charts.css` | Chart styles |
| `ERPSystem.DocumentEngine/Assets/icons/icons.css` | CSS icons |
| `ERPSystem.DocumentEngine/Assets/css/utilities.css` | Utility classes |
| `ERPSystem.DocumentEngine/Assets/css/desktop.css` | Desktop/browser A4 preview behavior |
| `ERPSystem.DocumentEngine/Assets/css/mobile.css` | Responsive/mobile overrides |
| `ERPSystem.DocumentEngine/Assets/css/print.css` | `@page` and print/PDF rules; hard-coded to A4/12 mm |
| `ERPSystem.DocumentEngine/Assets/logo/logo-placeholder.svg` | Placeholder logo, not real company branding |
| `ERPSystem.DocumentEngine/Assets/images/watermark-placeholder.svg` | Placeholder watermark |

No hand-authored `.html` template exists under `ERPSystem.DocumentEngine`; full HTML is assembled in C# by `Services/DocumentShell.cs`, templates, and components. The only repository HTML file is the Vite host file `web-client/index.html`, which is not a print template.

### 1.3 Live WPF generators and exporters outside DocumentEngine

| File | Purpose | Current status |
|---|---|---|
| `Services/Sales/SalesDocumentService.cs` | QuestPDF Sales Invoice and Delivery Note; preview, export, and Windows print verb | **In use and builds** |
| `Services/Purchases/PurchaseDocumentService.cs` | QuestPDF Purchase Invoice | **In use and builds** |
| `Services/Documents/StatementDocumentService.cs` | QuestPDF customer/supplier account statement | **In use and builds** |
| `Services/Documents/ListExportService.cs` | Generic WPF `DataGrid`/record export to `.xlsx` with ClosedXML | **In use and builds**; export, not a print template |
| `Services/Inventory/InventoryExportService.cs` | Warehouse stock export to UTF-8 CSV | **In use and builds**; export, not a print template |
| `Dialogs/DocumentPreviewWindow.xaml` + `.xaml.cs` | Old/mock preview shell | Present, but Sales Invoice comments state it has been replaced by `SalesDocumentService`; mock service still opens it |
| `ERPSystem.Application/Abstractions/Services/IDocumentPreviewService.cs` | Abstract preview interface | Registered to a null implementation |
| `ERPSystem.Infrastructure/Notifications/InfrastructureServices.cs` | `NullDocumentPreviewService`, always returns `null` | Placeholder/unused for real rendering |

### 1.4 React/Vite export files

| File | Purpose | Current status |
|---|---|---|
| `web-client/src/lib/documentExport.ts` | A4 PDF blob generation with jsPDF; download and Web Share/WhatsApp fallback | **In use**, but text-only/preliminary; footer literally labels the export as preliminary and says final design comes later |
| `web-client/src/components/DocumentActions.tsx` | Reusable “Export PDF” and WhatsApp buttons | **In use** on Sales, China, Expenses, Customers, and Accounting pages |
| `web-client/src/pages/Sales.tsx` | Constructs the Sales Invoice export payload | **In use**, but exports summary rows and each item as one text row, not a full invoice table |
| `web-client/src/theme/tokens.css` | Web design tokens used by the export-hosting UI | Active web design system; not consumed by generated jsPDF styling |
| `web-client/src/theme/global.css` | Web component/layout styling | Active UI CSS; not a print stylesheet and contains no `@media print`/`@page` template |

## 2. Current state of `ERPSystem.DocumentEngine`

### Architecture and output

- Target: `.NET 9` class library (`net9.0`).
- Input: neutral `DocumentModel` objects.
- Primary output: self-contained HTML strings with embedded CSS.
- `RenderHtml`: responsive web/preview HTML.
- `RenderPrint`: print-ready HTML using the same templates with `mode-print`.
- `RenderPdf`: despite its name, returns PDF-ready **HTML**, not PDF bytes.
- `RenderPdfBytes`: can return bytes only when a host supplies an `IPdfConverter` implementation.
- No `IPdfConverter` implementation exists in the repository, and no host injects one. Therefore DocumentEngine cannot currently generate actual PDF bytes.
- `PrintRenderer` also returns HTML; browser printing is expected through `window.print()` in the generated preview toolbar.

### Libraries and dependencies

`ERPSystem.DocumentEngine/ERPSystem.DocumentEngine.csproj` contains **no NuGet `PackageReference` at all**. It uses only .NET/BCL code and custom HTML/CSS composition. In particular, it does not depend on QuestPDF, wkhtmltopdf, PuppeteerSharp, Playwright, DinkToPdf, iText, PDFsharp, or another converter.

The real WPF PDF path is separate: root `ERPSystem.csproj` references **`QuestPDF` version `2024.12.3`**. Excel export uses **`ClosedXML` version `0.104.2`** through `ERPSystem.Application`. The React client references **`jspdf` version `^4.2.1`**.

### Integration and health

- `ERPSystem.csproj`, `ERPSystem.Api.csproj`, and the web client do **not** reference `ERPSystem.DocumentEngine`.
- Repository-wide usage of `DocumentEngineService`, renderers, registry, or concrete engine templates is confined to the DocumentEngine project itself.
- `IDocumentPreviewService` is wired to `NullDocumentPreviewService`, not to DocumentEngine.
- `dotnet build ERPSystem.DocumentEngine/ERPSystem.DocumentEngine.csproj --no-restore` on 2026-07-11: **failed**, 0 warnings / 1 error. Exact error: `ReceiptVoucherTemplate.cs(11,60): error CS0246: The type or namespace name 'RenderContext' could not be found`.
- `dotnet build ERPSystem.csproj --no-restore` on the same checkout: **succeeded**, 0 warnings / 0 errors. The live app builds because it does not reference DocumentEngine.

Conclusion: DocumentEngine is a substantial prepared foundation, but it is currently **unintegrated, generic for most document types, unable to produce PDF bytes by itself, and blocked by one compile error**.

## 3. Existing design system and shared style tokens

### 3.1 React/Vite “iOS-inspired” web design system

Source of truth: `web-client/src/theme/tokens.css`, imported by the web client and consumed throughout `global.css`.

**Colors**

| Token | Exact value |
|---|---|
| `--color-primary` | `#185fa5` |
| `--color-primary-dark` | `#124c86` |
| `--color-primary-soft` | `rgba(255, 255, 255, 0.16)` |
| `--color-background` | `#f4f7fb` |
| `--color-surface` | `#ffffff` |
| `--color-border` | `#dce5ef` |
| `--color-text` | `#152238` |
| `--color-muted` | `#6a778a` |
| `--color-good` | `#1d9e75` |
| `--color-warning` | `#ba7517` |
| `--color-danger` | `#c94343` |

**Typography**

- Global font stack: `"Segoe UI", Tahoma, Arial, sans-serif`.
- No web font files or `@font-face` rules are defined in the web client.

**Spacing/sizing tokens**

There is no numbered spacing scale (`space-1`, `space-2`, etc.) in the web client. The established shared size tokens are:

| Token | Exact value |
|---|---|
| `--spacing-page` | `clamp(10px, 3.2vw, 22px)` |
| `--content-max-width` | `1080px` |
| `--touch-target` | `44px` |
| `--control-height` | `44px` |
| `--safe-bottom` | `env(safe-area-inset-bottom, 0px)` |
| `--safe-top` | `env(safe-area-inset-top, 0px)` |
| `--radius-card` | `14px` |
| `--radius-control` | `12px` |
| `--shadow-soft` | `0 12px 30px rgba(24, 95, 165, 0.09)` |

`global.css` uses many hard-coded paddings/gaps in addition to these tokens, so it is not a fully tokenized spacing system.

### 3.2 DocumentEngine’s separate document design system

DocumentEngine does **not** currently match the web palette. Its source of truth is `ERPSystem.DocumentEngine/Assets/css/variables.css`.

**Brand/semantic colors:** primary `#1f4e79`, primary dark `#163a5a`, primary light `#2f6ba3`, primary soft `#eaf1f8`; secondary `#b8860b`, secondary soft `#f7efd8`; success `#1e7d46` / `#e4f4ea`; danger `#b02a37` / `#f8e6e8`; warning `#c17e0a` / `#fbf0d9`; info `#1c6ea4` / `#e5f0f7`.

**Surfaces/neutrals:** background `#f2f4f7`, surface `#ffffff`, alternate surface `#f8fafc`, text `#1a1f26`, muted text `#5b6572`, inverse text `#ffffff`, heading `#12222f`, border `#dfe3e8`, strong border `#c3c9d1`.

**Font stacks:**

- Arabic: `"Cairo", "Tajawal", "Segoe UI", "Dubai", "Tahoma", sans-serif`.
- Latin: `"Inter", "Segoe UI", "Helvetica Neue", Arial, sans-serif`.
- Mono: `"JetBrains Mono", "Consolas", "Courier New", monospace`.
- Base: Arabic stack.
- Cairo and Inter faces are declared, but no bundled font binaries exist; output falls back to locally installed/system fonts.

**4 px spacing scale:** `0`, `4px`, `8px`, `12px`, `16px`, `20px`, `24px`, `32px`, `40px`, `48px`, `64px` (`--space-0`, `1`, `2`, `3`, `4`, `5`, `6`, `8`, `10`, `12`, `16`).

**Radius scale:** `3px`, `5px`, `8px`, `12px`, `18px`, `999px`. **Font-size scale:** `10`, `11`, `12`, `13`, `14`, `16`, `20`, `26`, `34px`.

Important design decision for the next task: the web client and DocumentEngine have **different primary colors, fonts, radii, and spacing conventions**. A unified template cannot truthfully “reuse the existing brand identity” without first choosing which system is authoritative or mapping one to the other.

## 4. Sales Invoice discovery

### 4.1 Exact fields available from the application DTO

Source: `ERPSystem.Application/DTOs/Sales/SalesDtos.cs`.

**`SalesInvoiceDto` header/status/customer/totals fields (exact C# names and types):**

| Field | Type |
|---|---|
| `Id` | `Guid` |
| `InvoiceNumber` | `string` |
| `Status` | `SalesInvoiceStatus` |
| `CustomerId` | `Guid` |
| `CustomerName` | `string` |
| `WarehouseId` | `Guid` |
| `WarehouseName` | `string` |
| `ChinaContainerId` | `Guid` |
| `ContainerNumber` | `string` |
| `InvoiceDate` | `DateTime` |
| `PaymentType` | `PaymentType` |
| `PartialPaymentAmount` | `decimal` |
| `CashboxId` | `Guid?` |
| `SubTotal` | `decimal` |
| `DiscountTotal` | `decimal` |
| `TaxTotal` | `decimal` |
| `GrandTotal` | `decimal` |
| `RoundingDifference` | `decimal` |
| `IsLegacyUntaxed` | `bool` |
| `SentToWarehouseAt` | `DateTime?` |
| `DetailedAt` | `DateTime?` |
| `ApprovedAt` | `DateTime?` |
| `PrintedAt` | `DateTime?` |
| `DeliveredAt` | `DateTime?` |
| `CancelledAt` | `DateTime?` |
| `DeliveredToName` | `string?` |
| `DeliveryDriverName` | `string?` |
| `DeliveryNotes` | `string?` |
| `CancelReason` | `string?` |
| `Lines` | `IReadOnlyList<SalesInvoiceLineDto>` |

**`SalesInvoiceLineDto` fields (exact names and types):**

| Field | Type |
|---|---|
| `Id` | `Guid` |
| `LineNumber` | `int` |
| `ChinaContainerId` | `Guid` |
| `FabricItemId` | `Guid` |
| `FabricColorId` | `Guid` |
| `FabricDisplayName` | `string` |
| `FabricCode` | `string` |
| `ColorDisplayName` | `string` |
| `RollCount` | `int` |
| `UnitPrice` | `decimal` |
| `OriginalUnitPrice` | `decimal` |
| `TotalLengthMeters` | `decimal` |
| `LineTotal` | `decimal` |
| `DiscountAmount` | `decimal` |
| `DiscountReason` | `string?` |
| `TaxCodeId` | `Guid?` |
| `TaxCode` | `string?` |
| `TaxName` | `string?` |
| `TaxRate` | `decimal` |
| `TaxCategory` | `TaxCategory?` |
| `IsTaxInclusive` | `bool` |
| `TaxableAmount` | `decimal` |
| `TaxAmount` | `decimal` |
| `Notes` | `string?` |

### 4.2 Entity-only fields not present in the print DTO

The aggregate `ERPSystem.Domain/Aggregates/SalesInvoiceAggregate.cs` additionally has exact fields `CompanyId`, `BranchId`, `CreatedByUserId`, `ApprovedByUserId`, `ReversedByJournalEntryId`, and `IsArchived`, plus collections `Items`, `RollDetails`, `ItemTaxSnapshots`, and `DetailingSession`. These are not exposed by `SalesInvoiceDto`.

`ERPSystem.Domain/Entities/Sales/SalesEntities.cs` defines entity-only line fields `Unit`, `PriceModifiedByUserId`, and `PriceModifiedAt`. It also stores roll detail fields separately: `SalesInvoiceItemId`, `RollSequence`, `FabricRollId`, `LengthMeters`, `EnteredByUserId`, `EnteredAt`, `DraftRollNumber`, `DraftLengthMeters`, and computed `HasValidLength`. None of these per-roll details is included in `SalesInvoiceLineDto`; the DTO only exposes aggregate `TotalLengthMeters`.

Customer address, tax number, email, and full phone are **not fields on `SalesInvoiceDto`**. `SalesInvoiceOperationsCenterDto` adds `CustomerPhone`, `CollectedAmount`, `RemainingBalance`, payments, returns, and journal entries, but the current WPF `SalesDocumentService` receives only the invoice plus customer name. A future print mapper would need an explicit source if those additional customer/accounting fields are required.

### 4.3 Current New Sales Invoice UI line-table columns

Source: `Controls/Sales/NewSalesInvoiceControl.xaml`, explicit `DataGrid.Columns`, in display order. There are **16 displayed columns** (15 data/input/display columns plus 1 action column):

1. `الحاوية` — bound to line container selection/display.
2. `اختر التوب` — fabric/stock selector.
3. `نوع البضاعة` — `GoodsType`.
4. `كود الثوب` — `BoltCode`.
5. `اللون` — `Color`.
6. `عدد الأثواب` — `RollCountText`.
7. `الأطوال` — `LengthsDisplay`.
8. `الوحدة` — `Unit`.
9. `السعر الأصلي` — `OriginalUnitPriceText`.
10. `سعر الوحدة` — `UnitPriceText`.
11. `كود الضريبة` — tax-code selector/display.
12. `نسبة الضريبة` — `TaxRateDisplay`.
13. `مبلغ الضريبة` — `PreviewTaxAmountText`.
14. `الخصم` — `DiscountHint`.
15. `ملاحظة` — `Notes`.
16. `إجراءات` — remove action.

The current QuestPDF printed Sales Invoice table is narrower and has **8 columns**, in this order: `#`, item (`FabricDisplayName` + `FabricCode`), color, roll count, unit price, line total, tax amount, tax code. It omits container, lengths, unit, original price, tax rate/name/category, discount reason/amount, and notes as dedicated columns.

### 4.4 Existing Sales Invoice print/export wiring

**WPF — wired and functional:**

- `NewSalesInvoiceControl.xaml` exposes Print, PDF, and Preview buttons.
- Handlers in `NewSalesInvoiceControl.xaml.cs` require a saved invoice ID, load the real operations-center DTO, and call `SalesDocumentService.ShowInvoicePreview`.
- Sales list/context actions `InvoicePrint` and `InvoiceExportPdf` route through `SalesActionRouter` → `SalesPopupService.PrintAsync` → `SalesDocumentService`.
- `SalesDocumentService` generates a temporary PDF for preview, opens it externally, exports via `SaveFileDialog`, and prints by invoking the Windows shell `print` verb on the PDF.
- The generated document includes invoice number/date, payment type, status, customer name, 8-column line table, subtotal, line discount, invoice discount, taxable amount, grouped tax totals, rounding difference, and grand total.
- It does **not** use DocumentEngine or the React design tokens.

**WPF operations-center quick actions — visible but incomplete:** `Controls/Sales/SalesInvoiceOperationsCenterControl.cs` adds `sales:print` and `sales:pdf` quick actions with null callbacks in that control. The independently wired list/form paths above are real.

**React/Vite — wired but preliminary:** `web-client/src/pages/Sales.tsx` builds a `DocumentExportPayload` and renders `DocumentActions`. The PDF contains invoice number, customer, date, payment type, total, collected, remaining, and item summary strings. It does not render the complete DTO fields or a true invoice table and uses Helvetica/default jsPDF text behavior.

## 5. Existing paper-size and margin conventions

| Path | Paper | Margin/convention |
|---|---|---|
| Live WPF Sales Invoice | A4 | QuestPDF `page.Margin(30)` points (about 10.58 mm) |
| Live WPF Delivery Note | A4 | QuestPDF `page.Margin(30)` points |
| Live WPF Purchase Invoice | A4 | QuestPDF `page.Margin(30)` points |
| Live WPF account statements | A4 | QuestPDF `page.Margin(30)` points |
| React/jsPDF helper | A4 | `unit: 'pt'`, `margin = 40` points (about 14.11 mm); content line ends around x=555 |
| DocumentEngine browser sheet | A4 | `--page-width: 210mm`, `--page-height: 297mm`, inner `--page-margin: 14mm` |
| DocumentEngine print/PDF CSS | A4 | `@page { size: A4; margin: 12mm; }`; print mode removes the 14 mm inner padding, so effective printed margin is 12 mm |
| DocumentEngine model enum | A4/A5/Letter | `PageSize` supports all three values, default A4, but CSS is hard-coded to A4 and does not consume `RenderOptions.PageSize` |

No active A5 voucher implementation was found. Receipt/Payment Voucher templates exist in DocumentEngine, but the engine is unintegrated and its CSS remains A4. The WPF receipt/payment voucher controls expose action labels, but no QuestPDF A5 generator or A5 page-size call exists. Therefore there is **no established A5 margin convention** in current executable code.

## Design handoff facts for the next Sales Invoice template task

- The next implementation must choose whether the authoritative visual identity is the React/Vite token set (`#185fa5`, Segoe UI stack) or the separate DocumentEngine set (`#1f4e79`, Cairo/Tajawal-first stack), or explicitly define a mapping.
- The actual saved invoice DTO has 30 top-level properties (including `Lines`) plus 24 line properties; customer address/tax identity is not present in that DTO.
- The editing UI displays 16 line-table columns (15 data columns + actions), while the live PDF displays 8.
- The current executable convention for invoices is A4 with 30 pt margins; the unintegrated HTML engine uses A4/12 mm for print.
- DocumentEngine should not be treated as production-ready until its compile error, host integration, DTO-to-`DocumentModel` mapping, real branding/fonts, page-size handling, and PDF converter decision are resolved.
