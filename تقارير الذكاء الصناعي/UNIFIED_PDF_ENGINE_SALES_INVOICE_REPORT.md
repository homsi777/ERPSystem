# Unified PDF Engine - Sales Invoice Implementation Report

**Date:** 2026-07-11  
**Scope:** Sales Invoice only, A4, Arabic-first  
**Implementation status:** API engine and React integration complete locally; WPF migration and production deployment remain explicitly scoped follow-ups.

## 1. Architecture implemented

### Single server-side renderer

The new source of truth is:

- `ERPSystem.Api/Services/SalesInvoicePdfService.cs`
- Engine: QuestPDF `2024.12.3`
- Input: existing `SalesInvoiceOperationsCenterDto` and nested `SalesInvoiceDto`
- Output: PDF bytes generated entirely inside `ERPSystem.Api`
- Paper: A4
- Margin: 30 pt (the existing desktop convention)
- Endpoint: authenticated `GET /api/v1/sales/invoices/{invoiceId}/pdf`
- Response: `application/pdf` with filename `sales-invoice-{invoiceNumber}.pdf`

The endpoint is registered in `ERPSystem.Api/Endpoints/SalesEndpoints.cs`. It loads the same real operations-center DTO already used by invoice details and passes the DTO's existing values directly to the renderer. It does not recalculate amounts, discounts, taxes, rounding, or payment state.

### React web client

The Sales Invoice page now downloads and shares the PDF returned by the API:

- `web-client/src/api/client.ts`: adds authenticated binary/blob requests with the existing access-token refresh behavior.
- `web-client/src/api/sales.ts`: adds `getSalesInvoicePdf(invoiceId)`.
- `web-client/src/pages/Sales.tsx`: supplies that API PDF source to the existing `DocumentActions` UI.
- `web-client/src/components/DocumentActions.tsx`: accepts an optional server PDF source without changing the button layout/design.
- `web-client/src/lib/documentExport.ts`: can download/share an already-generated PDF blob.

For Sales Invoice, both **Export PDF** and **WhatsApp/native file share** now use the identical bytes returned by the API. The old jsPDF path remains for other document types because they are outside this task; Sales Invoice no longer uses jsPDF to generate its PDF.

### WPF desktop status and follow-up

WPF was deliberately not half-migrated. The current desktop application has no HTTP API client, no stored JWT session, and no refresh-token flow; it accesses the application/database layers directly. Replacing only its PDF call would require introducing and securing those concerns first.

The WPF follow-up is therefore explicitly scoped as:

1. Add an authenticated API session/client to WPF, including access-token refresh and configured API base URL.
2. Add a binary `GET /api/v1/sales/invoices/{id}/pdf` client method.
3. Replace the Sales Invoice calls to local `SalesDocumentService.BuildInvoiceDocument` with the returned server bytes while preserving the existing preview/save/Windows-print shell.
4. After verification, remove only the Sales Invoice QuestPDF composition from the WPF service; keep Delivery Note untouched until its own migration task.

Until that follow-up is done, WPF continues using its existing local QuestPDF Sales Invoice generator. This limitation is reported rather than represented as a completed cross-platform migration.

## 2. Layout implemented

The A4 Sales Invoice contains:

- Attached AB logo centered at the top.
- Navy/gold/cream palette derived from the logo rather than the former web/DocumentEngine tokens.
- Arabic document title, invoice number, and Western-digit date.
- Company block with temporary name and explicit missing address/phone labels.
- Customer name, available phone, payment type, warehouse, and invoice status.
- RTL table with the required eight columns, from right to left:
  1. `الصنف`
  2. `اللون`
  3. `عدد الأثواب`
  4. `الطول`
  5. `سعر الوحدة`
  6. `الخصم`
  7. `الضريبة`
  8. `الإجمالي`
- Subtotal, invoice discount total, tax total, optional rounding difference, and grand total.
- Page numbering and invoice number in the footer.

## 3. Arabic font, shaping, Bidi, and encoding

### Font solution

- Embedded font: `ERPSystem.Api/Assets/Fonts/NotoSansArabic.ttf`.
- License: `ERPSystem.Api/Assets/Fonts/OFL.txt` (SIL Open Font License 1.1).
- The font is packaged with API publish output and registered explicitly with `QuestPDF.Drawing.FontManager` under the name `Noto Sans Arabic`.
- Rendering therefore does not depend on Windows fonts or Linux system-font availability.
- The logo is packaged as `Assets/Brand/company-logo.png` from the attached `66.png`; its SHA-256 matches the supplied image: `1255E2FF4BD73937547EE32E29924A312D2A0187908EC747F52FC3ED8327B1F7`.

### Bidi handling

- The page and table use QuestPDF right-to-left content flow.
- Pure numeric/code fragments such as `INV-MAIN-000004`, dates, and amounts are placed in explicit left-to-right containers.
- Arabic labels and Latin/numeric values are placed in separate layout items where possible instead of relying on a mixed unstructured string.
- Visual QA caught one mixed-name defect (`الأمل.AB` ordering); the final version separates `شركة الأمل` from the LTR identifier `ALAMAL.AB`.
- All formatted dates and amounts use `InvariantCulture`, producing Western digits `0-9`.

### Visual proof

The final PDF was rendered at 3x resolution with MuPDF/PyMuPDF and visually inspected. Arabic letters are joined, table direction is RTL, Western digits are not reversed, and no boxes/mojibake/clipping/overlap were visible.

- Full rendered page: `output/pdf/proof/sales-invoice-page-1.png`
- Arabic header and mixed invoice ID: `output/pdf/proof/arabic-header-and-mixed-id.png`
- Customer block and RTL table: `output/pdf/proof/rtl-table-and-customer.png`
- Totals and Western numerals: `output/pdf/proof/totals-and-numerals.png`
- Sample PDF: `output/pdf/sales-invoice-INV-MAIN-000004.pdf`

## 4. Real production data used and deployment limitation

The public production API at `https://alamal-ab.org` was accessed read-only through its authenticated API. Real invoice `531ec17d-d7cf-4420-8335-afff1187766b` / `INV-MAIN-000004` was used as the sample data source:

- Customer: `نبيل تجربة موبايل`
- Warehouse: `المستودع الرئيسي`
- Date: `2026-07-11`
- Payment type: cash
- Status: delivered
- Subtotal/grand total: `31.10`
- One roll; unit price `3.11`

The sample PDF in this repository was generated locally by the new API renderer from those exact production DTO values.

**The required production deployment proof could not be completed in this environment.** The live health endpoint returns `200 OK`, but the new production PDF URL currently returns `404`, proving the new code has not been deployed. SSH attempts to the known VPS address/ports timed out. No claim is made that the sample came from the deployed endpoint.

To finish the exact deployment acceptance criterion, a maintainer with working VPS access must publish this checkout, then repeat:

1. Authenticate to production.
2. Request `GET /api/v1/sales/invoices/531ec17d-d7cf-4420-8335-afff1187766b/pdf`.
3. Confirm HTTP 200, `Content-Type: application/pdf`, and non-empty bytes.
4. Render the returned bytes on the Linux-deployed version and compare them with the proof images.
5. Exercise Export PDF from the deployed React invoice page.

## 5. Missing or incomplete data requiring Nabil

### Company identity

The repository/API does not expose a confirmed company address or phone for this document. The current PDF intentionally displays `غير محدد` rather than inventing values. Nabil needs to provide:

- Final legal/display company name in Arabic and English.
- Full postal/street address.
- Primary phone number.
- Optional email, website, commercial registration, tax number, and default currency label if they should appear.

### Production invoice enrichment gap

The selected real production invoice returned empty values for:

- `FabricDisplayName`
- `FabricCode`
- `ColorDisplayName`
- `TotalLengthMeters` (returned `0` although the line total is non-zero)
- `CustomerPhone` was unavailable on the selected operations data used for the sample.

The new PDF correctly renders the DTO as supplied and does not guess or query around these values. The sales DTO/query mapper must be enriched or corrected separately before a production sample can prove real Arabic fabric/color names and actual lengths. That is a data-projection issue, not a PDF calculation or layout change.

## 6. Verification results

- `dotnet build ERPSystem.Api/ERPSystem.Api.csproj --no-restore`: passed, 0 warnings, 0 errors.
- `dotnet build ERPSystem.csproj --no-restore`: passed, 0 warnings, 0 errors.
- `npm run build` in `web-client`: passed (`tsc --noEmit` and Vite production build).
- Generated PDF: one valid A4 page, `378249` bytes in the final local run.
- Visual render: `1785 x 2526` PNG at 3x, inspected with no visible Arabic shaping, direction, clipping, or overlap defects.
- Live production health: HTTP 200.
- Live production new PDF route before deployment: HTTP 404 (deployment outstanding).

## 7. Financial-logic confirmation

No accounting, tax, discount, payment, posting, journal, rounding, or invoice-total calculation logic was changed. The renderer is presentation-only and displays existing DTO values as-is.

