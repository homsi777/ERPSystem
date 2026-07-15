# Western Digits Global Fix Report

## Scope

This change is presentation-only. It changes neither stored data, calculations, rounding rules, API contracts, nor database schema.

Arabic labels and right-to-left layout remain unchanged. The only behavior changed is how user-visible numeric and date text is rendered: Western digits `0-9` are enforced instead of Arabic-Indic or Persian digits.

## Root cause

### WPF desktop

The application already initialized a custom Arabic culture in `App.xaml.cs` through `AppCulture.ConfigureWpfPresentation()` and `AppCulture.Apply()`.

The gap was in `Core/AppCulture.cs`: it replaced `NumberFormat` with the `en-US` equivalent but retained the Arabic date/calendar configuration. User-facing date formatting that relied on the current culture could therefore still inherit Arabic digit shaping. Some code-created `DataGrid` bindings also used `StringFormat` without explicitly setting `ConverterCulture`.

### Web client

`web-client/src/lib/format.ts` already used `ar-SY-u-nu-latn`, and there were no `toLocaleString()` calls or separate `Intl` formatters outside that file.

The remaining gap was raw numeric JSX for navigation badges, row counts, line/roll indices, and percentages. Those bypassed the central formatter and could be shaped differently by an Arabic browser locale.

## Fix applied

### WPF desktop

- `Core/AppCulture.cs`
  - Uses an Arabic culture with a supported Gregorian calendar and cloned `en-US` number format.
  - Keeps Arabic date names/UI culture while enforcing Latin digits for both numeric and date formatting.
  - Retains the global WPF `FrameworkElement.Language = en-US` metadata override.
  - Adds a startup guard that rejects a configured culture if a representative number or date contains Arabic-Indic/Persian digits.
- `Controls/Sales/SalesInvoiceOperationsCenterControl.cs`
  - Explicitly applies `AppCulture.FormatCulture` to numeric bindings.
  - Uses `AppFormats.DateTime` for accounting-entry dates.
- `Controls/Sales/SalesTaxReportPageControl.cs`
  - Explicitly applies the central converter culture to report bindings.
  - Uses `AppFormats.Date` for invoice dates.
- `Core/Customers/CustomerModels.cs`
  - Routes the last-invoice date through `AppFormats.Date`.
- `ERPSystem.Infrastructure/Repositories/ModuleReportRepository.cs`
  - Normalizes KPI display strings to Latin digits, preventing server/current-culture leakage into WPF and report views.

### Web client

- `web-client/src/lib/format.ts`
  - Keeps the Arabic locale and adds explicit `numberingSystem: 'latn'` to all `Intl` formatters.
  - Adds shared `formatInteger`, `formatPercent`, and `formatLineIndex` helpers.
- `web-client/src/components/BottomNav.tsx`
  - Formats the delivery badge and its accessibility label through `formatInteger`.
- `web-client/src/pages/Sales.tsx`, `Delivery.tsx`, and `Inventory.tsx`
  - Format invoice/roll indices and roll numbers through the shared helpers.
- `web-client/src/pages/Expenses.tsx`
  - Formats counts, payment labels, and percentages through the shared helpers.
- `web-client/src/theme/global.css`
  - Adds tabular, lining number glyph preferences as a visual safety net.

## PDFs and reports

Sales invoice, delivery, voucher, statement, and other established QuestPDF generators already use `CultureInfo.InvariantCulture` for their numeric/date bodies and remain correct.

The audit found a real exception in `ERPSystem.Application/Documents/ModuleReportPdfGenerator.cs`: report-generation timestamps, row counts, ranges, and preformatted KPI strings could bypass that convention. Those paths now explicitly use invariant formatting and normalize incoming KPI digit strings. No invoice/voucher PDF behavior was changed.

`Services/Reports/SalesTaxReportDocumentService.cs` now also creates KPI strings with `InvariantCulture`.

## Validation

- `dotnet build ERPSystem.csproj --no-restore` succeeded with 0 warnings and 0 errors.
- `npm run test:digits` succeeded. The new `web-client/scripts/verify-western-digits.mjs` checks Arabic-locale number, integer, percent, and date formatting and fails if Arabic-Indic or Persian digits are emitted.
- `npm run build` succeeded.
- IDE diagnostics reported no errors in the changed files.

## Screenshots

Live WPF and web screenshots were intentionally not captured. Test access/screenshots were declined before implementation, so this report does not claim a manual authenticated-screen verification.

## Data integrity confirmation

No entity, DTO, command, calculation, migration, database query, or persistence mapping was changed. All changes are confined to string formatting, WPF binding culture, web display helpers, report presentation strings, and automated presentation regression checks.
