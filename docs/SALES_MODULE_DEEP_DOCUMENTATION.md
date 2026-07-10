# Sales Module Deep Documentation

This document describes the current Sales module as implemented in this repository. It is a documentation artifact only; no application code was changed. Every behavioral claim below is tied to a source file and line number so future planning can extend the module without guessing.

## Part 1: UI Layer

Plain-language summary: Desktop WPF contains the main sales workflow: create draft, send to warehouse, warehouse detailing, approve, delivery, returns, cancellation, print/PDF/preview, and an operations-center view. The React web client is not zero: it has sales list/detail/create screens and a delivery/detailing screen, but it is thinner than WPF and has no sales return screen.

### Desktop Navigation Shell

- The Sales module hosts `ModuleShellControl` and sets `Shell.Module = AppModule.Sales`; subpage navigation is delegated to `Shell.NavigateSubpage(subPage)` (`Modules/SalesModule.xaml:1-6`, `Modules/SalesModule.xaml.cs:6-14`).
- `SalesViews.Create(key)` maps subpage keys to WPF screens: `NewInvoice`, `InvoiceView`, `NewReturn`, `Returns`, `Delivery`, `Detailing`, `Reports`, and default invoice list (`Views/Sales/SalesViews.cs:14-23`).
- `InvoiceView` is currently a placeholder-style detail entry point that tells the user to choose an invoice from the invoice list; actual detail operations are opened from list/operation services, not this placeholder (`Views/Sales/SalesViews.cs:30-39`).

### New Sales Invoice Screen

Files:

- XAML: `Controls/Sales/NewSalesInvoiceControl.xaml`
- Code-behind: `Controls/Sales/NewSalesInvoiceControl.xaml.cs`

Purpose:

- Creates and edits the invoice draft header and line skeleton before lengths are known. The XAML title and subtitle explicitly describe creation with customer, container, item, and roll count "بدون أطوال" (`Controls/Sales/NewSalesInvoiceControl.xaml:185-188`).
- The visible lifecycle strip says: accountant draft, warehouse execution, accountant approval, customer delivery (`Controls/Sales/NewSalesInvoiceControl.xaml:221-283`).

Entry point:

- `SalesViews.Create("NewInvoice")` returns `new NewSalesInvoiceControl()` (`Views/Sales/SalesViews.cs:16`, `Views/Sales/SalesViews.cs:28`).
- In the React web client, `/sales/new` is separate; for WPF, subpage key `NewInvoice` is the route (`Views/Sales/SalesViews.cs:14-23`).

Displayed fields and controls:

- Top actions: Print, PDF, Preview, Cancel, Save Draft, Send to Warehouse, Approve and Deliver (`Controls/Sales/NewSalesInvoiceControl.xaml:194-214`).
- Header fields: date, invoice number, customer, warehouse, container, cashbox, payment type, currency (`Controls/Sales/NewSalesInvoiceControl.xaml:354-424`).
- Credit-only partial-payment field is displayed through `BtnCredit` visibility binding (`Controls/Sales/NewSalesInvoiceControl.xaml:433-444`).
- Line metadata fields: line notes and discount reason, with built-in reason values "عميل وفي", "أمر كبير", "تصفية مخزون", "أخرى" (`Controls/Sales/NewSalesInvoiceControl.xaml:451-467`).
- Add-line button is wired to `BtnAddLine_Click` (`Controls/Sales/NewSalesInvoiceControl.xaml:495-502`).
- Item grid columns: stock picker, goods type, bolt code, color, roll count, lengths status, unit, original unit price, unit price, line discount, note, remove action (`Controls/Sales/NewSalesInvoiceControl.xaml:534-608`).
- Invoice-level discount field and apply button are `TxtDiscount` and `BtnApplyDiscount_Click` (`Controls/Sales/NewSalesInvoiceControl.xaml:636-649`).
- Summary cards show total meters, subtotal, discount, and grand total (`Controls/Sales/NewSalesInvoiceControl.xaml:671-741`).

Data source and handlers:

- Creation calls `CreateSalesInvoiceDraftHandler`, which validates the command, checks `sales.create`, validates container/stock, generates or accepts an invoice number, creates the aggregate, adds lines, applies invoice discount and partial payment, validates the draft, then saves (`ERPSystem.Application/UseCases/Sales/SalesInvoiceHandlers.cs:35-116`).
- Send to warehouse is a separate handler, `SendSalesInvoiceToWarehouseHandler`, which checks `sales.create`, loads the invoice, calls `aggregate.SendToWarehouse()`, reserves stock, updates, and saves/dispatches (`ERPSystem.Application/UseCases/Sales/SalesInvoiceHandlers.cs:245-286`).
- Approval is `ApproveSalesInvoiceHandler`, not a simple UI save. It checks `sales.approve`, verifies invoice/customer, opens a transaction, checks credit limit, approves the aggregate, posts customer balance, deducts inventory, posts GL, updates invoice/customer, dispatches events, and commits (`ERPSystem.Application/UseCases/Sales/SalesInvoiceHandlers.cs:375-460`).

Client-side validation:

- The UI defines required selection controls but most hard validation is in application/domain validators. Application validation runs at the start of create/update (`ERPSystem.Application/UseCases/Sales/SalesInvoiceHandlers.cs:48-50`, `ERPSystem.Application/UseCases/Sales/SalesInvoiceHandlers.cs:137-139`).
- Domain rules reject empty warehouse/container, invalid partial payment, empty line set, and invalid status transitions (`ERPSystem.Domain/Aggregates/SalesInvoiceAggregate.cs:69-72`, `ERPSystem.Domain/Aggregates/SalesInvoiceAggregate.cs:121-136`, `ERPSystem.Domain/Aggregates/SalesInvoiceAggregate.cs:139-148`, `ERPSystem.Domain/Aggregates/SalesInvoiceAggregate.cs:292-303`).

Actions:

- Save Draft: creates/updates the draft through the sales invoice command handlers (`ERPSystem.Application/Commands/Sales/SalesInvoiceCommands.cs:5-16`, `ERPSystem.Application/Commands/Sales/SalesInvoiceCommands.cs:34-43`).
- Send to Warehouse: changes status from `Draft` to `AwaitingDetailing` and creates a pending detailing session (`ERPSystem.Domain/Aggregates/SalesInvoiceAggregate.cs:151-160`).
- Approve: allowed only after detailing and no invalid lengths; sets approval user/date and raises `SalesInvoiceApproved` (`ERPSystem.Domain/Aggregates/SalesInvoiceAggregate.cs:200-212`).
- Cancel: requires non-empty reason and `sales.cancel`; releases inventory, calls `aggregate.Cancel(reason)`, updates, saves, and notifies inventory change (`ERPSystem.Application/UseCases/Sales/SalesInvoiceHandlers.cs:482-518`).
- Print/PDF/Preview: exposed on the screen (`Controls/Sales/NewSalesInvoiceControl.xaml:194-204`) and list popup service also provides print/export (`Services/Sales/SalesPopupService.cs:296-313`).

### Warehouse Detailing Screen

File: `Controls/Sales/WarehouseDetailingPageControl.cs`

Purpose:

- Lists invoices awaiting warehouse detailing by warehouse, then loads a `WarehouseDetailingWorkspaceControl` for entering roll serials or lengths (`Controls/Sales/WarehouseDetailingPageControl.cs:17-29`, `Controls/Sales/WarehouseDetailingPageControl.cs:75-100`).

Entry point:

- `SalesViews.Create("Detailing")` returns `new WarehouseDetailingPageControl()` (`Views/Sales/SalesViews.cs:21`, `Views/Sales/SalesViews.cs:42`).
- `SalesPopupService.NavigateToDetailing(row)` stores invoice context and navigates to Sales/Detailing (`Services/Sales/SalesPopupService.cs:230-233`).

Fields and controls:

- Warehouse combo box uses `WarehouseListDto.NameAr` and triggers queue reload on selection change (`Controls/Sales/WarehouseDetailingPageControl.cs:45-52`).
- Queue grid columns are invoice number, customer, container, roll count, and status (`Controls/Sales/WarehouseDetailingPageControl.cs:75-86`).
- Selecting/double-clicking a queue row loads the selected invoice into the workspace (`Controls/Sales/WarehouseDetailingPageControl.cs:78-79`, `Controls/Sales/WarehouseDetailingPageControl.cs:250-261`).

Data source:

- Warehouses are loaded from `SalesUiService.Instance.GetWarehousesAsync()` (`Controls/Sales/WarehouseDetailingPageControl.cs:121-147`).
- Queue data is loaded from `SalesUiService.Instance.GetDetailingQueueAsync(GetSelectedWarehouseId())` (`Controls/Sales/WarehouseDetailingPageControl.cs:155-177`).
- The API equivalent is `/api/v1/detailing/queue` and `/api/v1/detailing/{invoiceId}` (`ERPSystem.Api/Endpoints/DetailingEndpoints.cs:16-27`, `ERPSystem.Api/Endpoints/DetailingEndpoints.cs:32-64`).

Actions and validation:

- Completion calls `CompleteWarehouseDetailingHandler`. It checks `sales.detailing`, requires invoice, resolves roll entries by roll number or length, calls `EnterRollLength`, requires all lengths, completes detailing, saves/dispatches, and publishes a notification (`ERPSystem.Application/UseCases/Sales/SalesInvoiceHandlers.cs:288-364`).
- Domain completion requires status `AwaitingDetailing`, all roll details valid, recalculates line totals/subtotal/grand total, sets status `Detailed`, and raises `SalesInvoiceDetailed` (`ERPSystem.Domain/Aggregates/SalesInvoiceAggregate.cs:177-192`).

### Sales Invoice List/Search Screen

File: `Controls/Sales/SalesInvoiceListPageControl.cs`

Purpose:

- Displays the invoice list and opens invoice operations. The list screen is the default Sales view (`Views/Sales/SalesViews.cs:23`, `Views/Sales/SalesViews.cs:26`).

Entry point:

- Any Sales subpage key not mapped explicitly falls back to `BuildInvoiceList()` (`Views/Sales/SalesViews.cs:14-23`, `Views/Sales/SalesViews.cs:26`).

Data source:

- API list endpoint is `GET /api/v1/sales/invoices` and maps to `GetSalesInvoiceListHandler` (`ERPSystem.Api/Endpoints/SalesEndpoints.cs:18-24`, `ERPSystem.Api/Endpoints/SalesEndpoints.cs:34-61`).
- The query object supports search, status, customer, date range, paging, and sorting (`ERPSystem.Application/Queries/Sales/SalesQueries.cs`).

Actions:

- Context/menu actions route through `SalesPopupService`, including send to warehouse, cancel, navigate to detailing, delivery, returns, and print/export (`Services/Sales/SalesPopupService.cs:168-230`, `Services/Sales/SalesPopupService.cs:236-313`).

### Sales Invoice Detail / Operations Center

Files:

- Placeholder view: `Views/Sales/SalesViews.cs`
- Operations DTO: `ERPSystem.Application/DTOs/Sales/SalesDtos.cs`

Purpose:

- The explicit `InvoiceView` screen is only a placeholder instructing selection from the list (`Views/Sales/SalesViews.cs:30-39`).
- The real operations data shape is `SalesInvoiceOperationsCenterDto`, containing invoice, optional detailing, action flags, journal entries, payments, collected amount, remaining balance, and returns (`ERPSystem.Application/DTOs/Sales/SalesDtos.cs:105-117`).

Data source:

- `GET /api/v1/sales/invoices/{invoiceId}` maps to `GetSalesInvoiceOperationsCenterHandler` (`ERPSystem.Api/Endpoints/SalesEndpoints.cs:22-23`, `ERPSystem.Api/Endpoints/SalesEndpoints.cs:63-70`).
- React details page uses `getSalesInvoice(invoiceId)` (`web-client/src/api/sales.ts:32-33`, `web-client/src/pages/Sales.tsx:159-166`).

### Approval Screen

- There is no separate WPF "approval-only" screen found. Approval is an action exposed from the creation/operations/list flow: the XAML exposes `BtnApprove` (`Controls/Sales/NewSalesInvoiceControl.xaml:213-214`) and `ApproveSalesInvoiceHandler` enforces server-side approval rules (`ERPSystem.Application/UseCases/Sales/SalesInvoiceHandlers.cs:375-460`).

### Delivery Screen

File: `Controls/Sales/SalesDeliveryListPageControl.cs`

Purpose:

- Lists approved/printed invoices awaiting delivery and can show delivery confirmation popup (`Controls/Sales/SalesDeliveryListPageControl.cs:16-29`, `Controls/Sales/SalesDeliveryListPageControl.cs:70-71`).

Entry point:

- `SalesViews.Create("Delivery")` returns `new SalesDeliveryListPageControl()` (`Views/Sales/SalesViews.cs:20`, `Views/Sales/SalesViews.cs:44`).

Fields and actions:

- Status filter reloads the list; columns include approval date; double-click opens delivery for `Approved` or `Printed` invoices (`Controls/Sales/SalesDeliveryListPageControl.cs:19-29`, `Controls/Sales/SalesDeliveryListPageControl.cs:43-57`, `Controls/Sales/SalesDeliveryListPageControl.cs:70-71`).
- `SalesPopupService.ShowDelivery(row)` refuses rows not in `Approved` or `Printed`, opens `SalesDeliveryPopupControl`, and refreshes on success (`Services/Sales/SalesPopupService.cs:236-258`).

Handler:

- `ConfirmSalesInvoiceDeliveryHandler` handles `ConfirmSalesInvoiceDeliveryCommand`, marks delivery on the aggregate, updates, saves, and returns success (`ERPSystem.Application/UseCases/Sales/SalesDeliveryHandlers.cs:10-35`).
- Domain method allows delivery from `Approved` or `Printed`, sets status `Delivered`, and stores date/receiver/driver/notes (`ERPSystem.Domain/Aggregates/SalesInvoiceAggregate.cs:224-247`).

### Sales Return Screen

Files:

- List: `Controls/Sales/SalesReturnListPageControl.cs`
- Popup form: `Controls/Sales/Popups/SalesReturnFormPopupControl.cs`

Purpose:

- Creates draft returns and optionally posts them against an original invoice.

Entry point:

- `SalesViews.Create("NewReturn")` and `SalesViews.Create("Returns")` both return `SalesReturnListPageControl` (`Views/Sales/SalesViews.cs:18-19`, `Views/Sales/SalesViews.cs:48-49`).
- `SalesPopupService.ShowReturnsForInvoice(row)` navigates to Returns (`Services/Sales/SalesPopupService.cs:291-293`).

Fields:

- Popup fields include reason combo, reason notes, notes, return date, and a line grid (`Controls/Sales/Popups/SalesReturnFormPopupControl.cs:22-39`, `Controls/Sales/Popups/SalesReturnFormPopupControl.cs:80-99`).
- Buttons are cancel, save draft, and post (`Controls/Sales/Popups/SalesReturnFormPopupControl.cs:123-151`).

Actions and validation:

- Save/post requires at least one line with returned quantity and rejects invalid quantities before calling `SalesReturnUiService.Instance.CreateDraftAsync(...)` (`Controls/Sales/Popups/SalesReturnFormPopupControl.cs:196-235`).
- Application handlers are `CreateSalesReturnHandler`, `UpdateSalesReturnHandler`, `PostSalesReturnHandler`, and `CancelSalesReturnHandler` (`ERPSystem.Application/UseCases/Sales/SalesReturnHandlers.cs:14-66`, `ERPSystem.Application/UseCases/Sales/SalesReturnHandlers.cs:105-131`, `ERPSystem.Application/UseCases/Sales/SalesReturnHandlers.cs:141-193`, `ERPSystem.Application/UseCases/Sales/SalesReturnHandlers.cs:219-238`).
- Posting opens a transaction, receives inventory return, posts sales-return GL, saves, and commits (`ERPSystem.Application/UseCases/Sales/SalesReturnHandlers.cs:141-193`).

### Receipt / Payment Screen Tied to Sales

- There is no sales-specific receipt screen under `Controls/Sales`. Receipts are in Accounting/Finance (`Views/Accounting/AccountingViews.cs:18` for Receipts).
- API receipt creation is `/api/v1/receipts` with allocations to sales invoices (`ERPSystem.Api/Endpoints/ReceiptEndpoints.cs:14-24`, `ERPSystem.Api/Endpoints/ReceiptEndpoints.cs:68-74`).
- `CreateReceiptVoucherHandler` creates the voucher, allocates sales invoices, creates `ReceiptInvoicePayment` rows, and saves (`ERPSystem.Application/UseCases/Finance/FinanceHandlers.cs:14-65`).
- `PostReceiptVoucherHandler` posts cash/AR accounting through `PostReceiptVoucherAsync` (`ERPSystem.Infrastructure/Services/IntegratedAccountingService.cs:102-118`).

### React Web Screens

Routes:

- `/sales`, `/sales/new`, `/sales/:invoiceId`, `/delivery`, and `/delivery/:invoiceId` are registered (`web-client/src/App.tsx:29-33`).

Sales web page:

- `SalesPage` dispatches to list, create, or detail based on path (`web-client/src/pages/Sales.tsx:48-52`).
- List uses `getSalesInvoices` (`web-client/src/pages/Sales.tsx:61-69`) and links to `/sales/{id}` (`web-client/src/pages/Sales.tsx:127-127`).
- Detail uses `getSalesInvoice`, with mutations for send to warehouse, approve, and cancel (`web-client/src/pages/Sales.tsx:159-188`).
- Create page exists. It loads customers, warehouses, containers, warehouse stock, builds lines, and calls `createSalesInvoice` (`web-client/src/pages/Sales.tsx:378-425`, `web-client/src/pages/Sales.tsx:461-495`).

Delivery web page:

- Delivery queue uses warehouses and `getDetailingQueue`; detail uses `getDetailing` and `completeDetailing` (`web-client/src/pages/Delivery.tsx:43-75`, `web-client/src/pages/Delivery.tsx:158-213`).
- Client-side validation for detailing requires every roll to have either a roll serial or positive length (`web-client/src/pages/Delivery.tsx:193-202`).

Explicit answer:

- Web-client does have sales invoice creation capability today via `/sales/new` and `createSalesInvoice()` (`web-client/src/pages/Sales.tsx:378-495`, `web-client/src/api/sales.ts:36-41`).
- Web-client does not show a sales return route/screen in `App.tsx`; only Sales and Delivery routes are present (`web-client/src/App.tsx:29-33`).

## Part 2: Application Layer

Plain-language summary: Sales creation is draft-first. Drafts reserve by roll count, go to warehouse for exact roll/length detailing, become detailed, then approval performs the high-risk work: credit check, inventory deduction, customer AR update, GL posting, audit logging, event dispatch, and transaction commit.

### Commands and DTOs

Key command inputs:

- `CreateSalesInvoiceDraftCommand`: company, branch, optional invoice number, customer, warehouse, China container, payment type, discount, partial payment, and lines (`ERPSystem.Application/Commands/Sales/SalesInvoiceCommands.cs:5-16`).
- `SalesInvoiceLineCommand`: line number, fabric item/color, roll count, unit price, original unit price, discount reason, notes (`ERPSystem.Application/Commands/Sales/SalesInvoiceCommands.cs:19-31`).
- Update, discount, send-to-warehouse, complete-detailing, approve, cancel, delivery, and warehouse-update commands are defined in the same file (`ERPSystem.Application/Commands/Sales/SalesInvoiceCommands.cs:34-95`).

Key response DTOs:

- `SalesInvoiceDto` includes header, status, totals, lifecycle dates, delivery/cancel fields, and line list (`ERPSystem.Application/DTOs/Sales/SalesDtos.cs:6-32`).
- `SalesInvoiceLineDto` includes item/color, roll count, unit/original price, total length, line total, discount, reason, notes (`ERPSystem.Application/DTOs/Sales/SalesDtos.cs:35-52`).
- `WarehouseDetailingDto` and `WarehouseDetailingRollDto` carry invoice/roll detailing state (`ERPSystem.Application/DTOs/Sales/SalesDtos.cs:66-88`).
- `SalesInvoiceOperationsCenterDto` adds action flags, journal entries, payments, collected/remaining balances, and returns (`ERPSystem.Application/DTOs/Sales/SalesDtos.cs:105-117`).

### Handler Inventory

`CreateSalesInvoiceDraftHandler` (`ERPSystem.Application/UseCases/Sales/SalesInvoiceHandlers.cs:35-120`)

1. Validates the command (`:48-50`).
2. Requires `sales.create` (`:52-53`).
3. Validates container and stock lines through `IInventoryOperationsService` (`:57-62`).
4. Uses provided invoice number or generates one through `INumberingService` (`:64-66`).
5. Rejects duplicate invoice number (`:68-69`).
6. Creates `SalesInvoiceAggregate.CreateDraft(...)` (`:71-82`).
7. Resolves price overrides and creates `SalesInvoiceItem` lines (`:84-100`).
8. Applies invoice discount and partial payment (`:103-109`).
9. Validates draft, adds repository row, and saves (`:111-114`).
10. Returns invoice id (`:116`) or maps exceptions to failure (`:118-120`).

Transaction scope: no explicit transaction; just repository add plus `SaveChangesAsync` (`:113-114`).

`UpdateSalesInvoiceDraftHandler` (`ERPSystem.Application/UseCases/Sales/SalesInvoiceHandlers.cs:125-205`)

1. Validates command and `sales.create` (`:137-142`).
2. Loads invoice or returns not found (`:144-146`).
3. Validates container and stock (`:150-155`).
4. Updates draft header, replaces lines, resets discount/partial payment, validates draft, updates and saves (`:157-199`).

Transaction scope: no explicit transaction.

`UpdateSalesInvoiceDiscountHandler` (`ERPSystem.Application/UseCases/Sales/SalesInvoiceHandlers.cs:209-241`)

- Validates invoice id and non-negative discount, requires `sales.create`, loads invoice, sets discount total, updates and saves.

`SendSalesInvoiceToWarehouseHandler` (`ERPSystem.Application/UseCases/Sales/SalesInvoiceHandlers.cs:245-286`)

- Requires `sales.create`, loads invoice, calls `SendToWarehouse`, reserves inventory, updates, saves/dispatches. Domain status changes from `Draft` to `AwaitingDetailing` and creates pending session (`ERPSystem.Domain/Aggregates/SalesInvoiceAggregate.cs:151-160`).

`CompleteWarehouseDetailingHandler` (`ERPSystem.Application/UseCases/Sales/SalesInvoiceHandlers.cs:288-364`)

1. Requires `sales.detailing` (`ERPSystem.Application/UseCases/Sales/SalesInvoiceHandlers.cs:291-307`).
2. Loads invoice and resolves roll entries by serial or length through inventory operations (`ERPSystem.Application/UseCases/Sales/SalesInvoiceHandlers.cs:298-329`).
3. Enters each roll length, rejects mismatched details, validates all lengths, completes detailing (`ERPSystem.Application/UseCases/Sales/SalesInvoiceHandlers.cs:330-341`).
4. Updates invoice, saves/dispatches, publishes a notification (`ERPSystem.Application/UseCases/Sales/SalesInvoiceHandlers.cs:347-359`).

`ApproveSalesInvoiceHandler` (`ERPSystem.Application/UseCases/Sales/SalesInvoiceHandlers.cs:375-476`)

1. Validates invoice id and permission `sales.approve` (`:393-397`).
2. Loads invoice/customer and checks `InvoiceCanBeApprovedSpecification` (`:399-409`).
3. Begins transaction (`:413`).
4. Enforces credit limit (`:415`).
5. Approves aggregate and posts customer invoice balance (`:417-419`).
6. Deducts inventory and posts sales invoice GL (`:421-422`).
7. Writes audit log if line discounts exist (`:424-444`).
8. Updates invoice/customer, saves/dispatches, commits (`:447-450`).
9. Publishes approval notification (`:452-458`).
10. On failure, rolls back and may publish credit-limit notification (`:462-476`).

`CancelSalesInvoiceHandler` (`ERPSystem.Application/UseCases/Sales/SalesInvoiceHandlers.cs:482-518`)

- Validates invoice id and reason, requires `sales.cancel`, loads invoice, releases inventory, calls `aggregate.Cancel`, updates, saves, publishes inventory change.

No delete handler found:

- There is no `DeleteSalesInvoiceHandler` in the sales use-case files; cancellation is the implemented destructive/voiding action (`ERPSystem.Application/UseCases/Sales/SalesInvoiceHandlers.cs:482-518`).

Sales return handlers:

- `CreateSalesReturnHandler` creates draft return (`ERPSystem.Application/UseCases/Sales/SalesReturnHandlers.cs:14-66`).
- `UpdateSalesReturnHandler` updates draft (`ERPSystem.Application/UseCases/Sales/SalesReturnHandlers.cs:105-131`).
- `PostSalesReturnHandler` opens a transaction, restores inventory, posts sales-return accounting, saves, and commits (`ERPSystem.Application/UseCases/Sales/SalesReturnHandlers.cs:141-193`).
- `CancelSalesReturnHandler` cancels a return (`ERPSystem.Application/UseCases/Sales/SalesReturnHandlers.cs:219-238`).

Pricing/discount handlers:

- Line-level price override is resolved during create/update (`ERPSystem.Application/UseCases/Sales/SalesInvoiceHandlers.cs:87-99`, `ERPSystem.Application/UseCases/Sales/SalesInvoiceHandlers.cs:168-180`).
- Line discount is calculated from `OriginalUnitPrice - UnitPrice` times sold meters (`ERPSystem.Domain/Entities/Sales/SalesEntities.cs:65-75`).
- Invoice-level discount is `SetDiscountTotal`, and grand total is recalculated as subtotal plus tax minus discount (`ERPSystem.Domain/Aggregates/SalesInvoiceAggregate.cs:249-264`).

Accounting posting:

- `PostSalesInvoiceApprovalAsync` posts AR debit, sales revenue credit, optional sales discount debit, and optional COGS/inventory lines (`ERPSystem.Infrastructure/Services/IntegratedAccountingService.cs:68-100`).
- It calls `PostIfNotExistsAsync`, which checks existing journal entries by source type/id before creating a posted journal (`ERPSystem.Infrastructure/Services/IntegratedAccountingService.cs:437-461`).

### Lifecycle State Machine

Actual enum:

```csharp
public enum SalesInvoiceStatus
{
    Draft = 0,
    AwaitingDetailing = 1,
    Detailed = 2,
    ReadyForApproval = 3,
    Approved = 4,
    Printed = 5,
    Delivered = 6,
    Cancelled = 7,
    PartiallyReturned = 8,
    Returned = 9
}
```

Source: `ERPSystem.Domain/Enums/SalesInvoiceStatus.cs:3-15`.

Flow:

- `Draft`: created by `CreateDraft`; editable only in Draft (`ERPSystem.Domain/Aggregates/SalesInvoiceAggregate.cs:58-88`, `ERPSystem.Domain/Aggregates/SalesInvoiceAggregate.cs:292-296`).
- `Draft -> AwaitingDetailing`: `SendToWarehouse()` requires Draft, at least one line, sets `SentToWarehouseAt`, creates pending detailing session (`ERPSystem.Domain/Aggregates/SalesInvoiceAggregate.cs:151-160`).
- `AwaitingDetailing -> Detailed`: `CompleteDetailing()` requires all roll lengths and recalculates totals (`ERPSystem.Domain/Aggregates/SalesInvoiceAggregate.cs:177-192`).
- `Detailed -> ReadyForApproval`: `MarkReadyForApproval()` exists but no handler call was found in the sales handlers; approval also accepts `Detailed` directly (`ERPSystem.Domain/Aggregates/SalesInvoiceAggregate.cs:194-198`, `ERPSystem.Domain/Aggregates/SalesInvoiceAggregate.cs:200-203`).
- `Detailed/ReadyForApproval -> Approved`: `Approve()` sets user/time and raises approval event (`ERPSystem.Domain/Aggregates/SalesInvoiceAggregate.cs:200-212`).
- `Approved/Printed -> Delivered`: `ConfirmDelivery()` allows either Approved or Printed and sets status Delivered (`ERPSystem.Domain/Aggregates/SalesInvoiceAggregate.cs:224-247`).
- `Approved -> Printed`: print method exists and is allowed for Approved or Printed (`ERPSystem.Domain/Aggregates/SalesInvoiceAggregate.cs:215-222`).
- `Draft/AwaitingDetailing/Detailed/ReadyForApproval -> Cancelled`: `Cancel()` rejects Approved/Printed/Delivered; otherwise sets Cancelled and reason (`ERPSystem.Domain/Aggregates/SalesInvoiceAggregate.cs:273-289`).
- Backward transition from Approved to Draft is not implemented; approved/printed/delivered invoices cannot be cancelled and there is no reverse/unapprove sales-invoice handler (`ERPSystem.Domain/Aggregates/SalesInvoiceAggregate.cs:273-280`).

Warehouse detailing enum:

```csharp
public enum WarehouseDetailingStatus
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    Rejected = 3
}
```

Source: `ERPSystem.Domain/Enums/WarehouseDetailingStatus.cs:3-9`.

## Part 3: Domain Layer

Plain-language summary: The aggregate protects the sales workflow state, but tax is permanently zero unless infrastructure hydrates another value. Exact meters are unknown until detailing, so totals become meaningful after roll lengths are entered.

### SalesInvoiceAggregate Public Method Inventory

- `CreateDraft(...)`: requires warehouse and China container, sets initial state Draft, raises `SalesInvoiceCreated` (`ERPSystem.Domain/Aggregates/SalesInvoiceAggregate.cs:58-89`).
- `AddItem(SalesInvoiceItem item)`: editable only; adds item and creates one `SalesInvoiceRollDetail` per requested roll (`ERPSystem.Domain/Aggregates/SalesInvoiceAggregate.cs:91-97`).
- `UpdateDraftHeader(...)`: editable only; validates customer/warehouse/container and resets partial payment when cash (`ERPSystem.Domain/Aggregates/SalesInvoiceAggregate.cs:99-119`).
- `SetPartialPaymentAmount(Money? amount)`: editable only; cash clears it; rejects negative or greater-than-total payments (`ERPSystem.Domain/Aggregates/SalesInvoiceAggregate.cs:121-137`).
- `ReplaceDraftLines(...)`: editable only; requires at least one line, clears items/roll details, re-adds ordered lines (`ERPSystem.Domain/Aggregates/SalesInvoiceAggregate.cs:139-149`).
- `SendToWarehouse()`: Draft only; requires items; sets AwaitingDetailing and creates session (`ERPSystem.Domain/Aggregates/SalesInvoiceAggregate.cs:151-160`).
- `StartDetailing(Guid officerUserId)`: AwaitingDetailing only; starts detailing session (`ERPSystem.Domain/Aggregates/SalesInvoiceAggregate.cs:162-167`).
- `EnterRollLength(Guid rollDetailId, LengthInMeters length, Guid userId)`: AwaitingDetailing only; finds detail or throws `WarehouseDetailingException`, then stores length/user/time (`ERPSystem.Domain/Aggregates/SalesInvoiceAggregate.cs:169-175`, `ERPSystem.Domain/Entities/Sales/SalesEntities.cs:97-104`).
- `CompleteDetailing()`: AwaitingDetailing only; requires all lengths, recalculates line totals/subtotal/grand total, sets Detailed (`ERPSystem.Domain/Aggregates/SalesInvoiceAggregate.cs:177-192`).
- `MarkReadyForApproval()`: Detailed only; sets ReadyForApproval (`ERPSystem.Domain/Aggregates/SalesInvoiceAggregate.cs:194-198`).
- `Approve(Guid approvedByUserId)`: requires Detailed or ReadyForApproval and all valid lengths; sets Approved/user/time and raises event (`ERPSystem.Domain/Aggregates/SalesInvoiceAggregate.cs:200-212`).
- `MarkPrinted()`: Approved or Printed; sets Printed and `PrintedAt`, raises event (`ERPSystem.Domain/Aggregates/SalesInvoiceAggregate.cs:215-222`).
- `ConfirmDelivery(...)`: Printed or Approved; validates delivery date, sets Delivered and delivery metadata (`ERPSystem.Domain/Aggregates/SalesInvoiceAggregate.cs:224-247`).
- `SetDiscountTotal(Money discount)`: editable only; rejects negative or greater than subtotal when subtotal exists; recalculates total (`ERPSystem.Domain/Aggregates/SalesInvoiceAggregate.cs:249-254`).
- `Cancel(string reason)`: rejects already posted states, requires reason, sets Cancelled and reason (`ERPSystem.Domain/Aggregates/SalesInvoiceAggregate.cs:273-289`).
- Private `RecalculateGrandTotal()`: computes `GrandTotal = SubTotal + TaxTotal - DiscountTotal`, floors at zero through `Money.Zero()` when negative (`ERPSystem.Domain/Aggregates/SalesInvoiceAggregate.cs:256-264`).

Important exception sources:

- `EnsureEditable()` throws when status is not Draft (`ERPSystem.Domain/Aggregates/SalesInvoiceAggregate.cs:292-296`).
- `EnsureStatus(...)` throws when current status is not allowed (`ERPSystem.Domain/Aggregates/SalesInvoiceAggregate.cs:298-303`).

### Related Domain Objects

Sales invoice item:

- `SalesInvoiceItem.Create(...)` rejects non-positive unit price and stores original/applied price and discount metadata (`ERPSystem.Domain/Entities/Sales/SalesEntities.cs:32-63`).
- `RecalculateTotal(...)` sums matching roll-detail meters, calculates `LineTotal`, and computes line discount when original price exceeds applied price (`ERPSystem.Domain/Entities/Sales/SalesEntities.cs:65-75`).

Roll detail and detailing session:

- `SalesInvoiceRollDetail` stores sequence, optional fabric roll id, entered length/user/time, and `HasValidLength` (`ERPSystem.Domain/Entities/Sales/SalesEntities.cs:78-106`).
- `WarehouseDetailingSession` supports pending, start, complete, and reject (`ERPSystem.Domain/Entities/Sales/SalesEntities.cs:109-145`).

Inventory/fabric roll:

- Approval uses inventory engine `IssueForInvoiceAsync`; for each valid roll detail it finds/uses a fabric roll, rejects missing roll or over-deduction, subtracts meters, marks sold when depleted, updates warehouse stock, writes stock movement lines, records valuation snapshot, and returns COGS (`ERPSystem.Infrastructure/Services/InventoryEngine.cs:631-713`).
- A domain-level `DeductLength` exists in inventory domain, but the approval path above mutates `FabricRollEntity` directly in infrastructure.

Customer credit:

- Approval calls `CreditLimitChecker.EnsureWithinLimit(customerAggregate, aggregate.GrandTotal)` inside the transaction (`ERPSystem.Application/UseCases/Sales/SalesInvoiceHandlers.cs:413-419`).
- Customer balance is increased by `customerAggregate.RecordPostedInvoice(aggregate.GrandTotal.Amount)` (`ERPSystem.Application/UseCases/Sales/SalesInvoiceHandlers.cs:417-419`).

Delivery:

- Delivery is stored on the sales invoice aggregate itself; there is a `DeliveryNote` domain entity, but no delivery-note handler/table flow is used in the documented delivery action (`ERPSystem.Domain/Entities/Sales/SalesEntities.cs:191-208`, `ERPSystem.Application/UseCases/Sales/SalesDeliveryHandlers.cs:10-35`).

## Part 4: Data Layer

Plain-language summary: Sales persistence uses schema `sales` for invoice tables and `finance` for receipt vouchers. Most sales relationships are GUID columns and indexes, not EF navigation/FK relationships, matching the prior audit constraints.

### Tables and Columns

`sales.sales_invoices`

- Migration creates `sales_invoices` with columns: `Id`, audit fields, `CancelledAt`, `CancelReason`, `CompanyId`, `BranchId`, `InvoiceNumber`, `CustomerId`, `WarehouseId`, `ChinaContainerId`, `InvoiceDate`, `PaymentType`, `PartialPaymentAmount`, `Status`, `SubTotal`, `DiscountTotal`, `TaxTotal`, `GrandTotal`, `ApprovedByUserId`, `SentToWarehouseAt`, `DetailedAt`, `ApprovedAt`, `PrintedAt`, `DeliveredAt`, `ReversedByJournalEntryId` (`ERPSystem.Infrastructure/Migrations/20260626235435_InitialCreate.cs:829-867`).
- Later migration adds delivery fields `DeliveredToName`, `DeliveryDriverName`, and `DeliveryNotes` (`ERPSystem.Infrastructure/Migrations/20260711120000_AddSalesReturnsAndDelivery.cs:12-15`).
- EF maps precision for subtotal/discount/tax/grand total and unique index on `(CompanyId, InvoiceNumber)` (`ERPSystem.Infrastructure/Configurations/SalesConfigurations.cs:12-20`).

`sales.sales_invoice_items`

- Migration creates columns including invoice id, line number, fabric item/color, roll count, unit price, unit, and line total (`ERPSystem.Infrastructure/Migrations/20260626235435_InitialCreate.cs:778-800`).
- Later migrations add notes and price override columns: `Notes`, `OriginalUnitPrice`, `DiscountAmount`, `DiscountReason`, `PriceModifiedByUserId`, `PriceModifiedAt` (`ERPSystem.Infrastructure/Migrations/20260715120200_AddSalesInvoiceItemNotes.cs:12-19`, `ERPSystem.Infrastructure/Migrations/20260715120400_AddSalesInvoiceItemPriceOverride.cs:12-36`).
- EF maps precision and unique index `(SalesInvoiceId, LineNumber)` (`ERPSystem.Infrastructure/Configurations/SalesConfigurations.cs:28-37`).

`sales.sales_invoice_roll_details`

- Migration creates roll-detail rows with invoice item id, sequence, optional fabric roll id, length, entered-by, entered-at (`ERPSystem.Infrastructure/Migrations/20260626235435_InitialCreate.cs:804-825`).
- EF maps `LengthMeters numeric(18,4)` and unique index `(SalesInvoiceItemId, RollSequence)` (`ERPSystem.Infrastructure/Configurations/SalesConfigurations.cs:40-48`).

`sales.warehouse_detailing_sessions`

- Migration creates session table with sales invoice id, status, assigned officer, started/completed dates, rejection reason (`ERPSystem.Infrastructure/Migrations/20260626235435_InitialCreate.cs:980-1000`).
- EF maps unique index on `SalesInvoiceId` (`ERPSystem.Infrastructure/Configurations/SalesConfigurations.cs:51-58`).

`sales.sales_returns` and `sales.sales_return_lines`

- Added by SQL migration with return header fields, unique return number, original invoice/customer indexes, and line table (`ERPSystem.Infrastructure/Migrations/20260711120000_AddSalesReturnsAndDelivery.cs:18-72`).
- EF maps sales returns and line precision/indexes (`ERPSystem.Infrastructure/Configurations/SalesConfigurations.cs:61-91`).

`sales.receipt_invoice_payments`

- Added by SQL migration with `Id`, audit fields, `SalesInvoiceId`, `ReceiptVoucherId`, `Amount`, `AppliedAt`, plus indexes on invoice/voucher (`ERPSystem.Infrastructure/Migrations/20260711120000_AddSalesReturnsAndDelivery.cs:75-92`).
- EF maps amount precision and indexes (`ERPSystem.Infrastructure/Configurations/SalesConfigurations.cs:94-103`).

`documents.document_counters`

- Initial migration creates `document_counters`; unique index on `(BranchId, DocumentType)` (`ERPSystem.Infrastructure/Migrations/20260626235435_InitialCreate.cs:329-343`, `ERPSystem.Infrastructure/Migrations/20260626235435_InitialCreate.cs:1140-1145`).
- EF maps it in `RemainingConfigurations` (`ERPSystem.Infrastructure/Configurations/RemainingConfigurations.cs:317`).

`finance.receipt_vouchers`

- Initial migration creates receipt vouchers in finance schema and unique index `(CompanyId, VoucherNumber)` (`ERPSystem.Infrastructure/Migrations/20260626235435_InitialCreate.cs:714-740`, `ERPSystem.Infrastructure/Migrations/20260626235435_InitialCreate.cs:1203-1209`).
- Receipt API and finance handlers allocate payments to sales invoices through `receipt_invoice_payments` (`ERPSystem.Api/Endpoints/ReceiptEndpoints.cs:68-74`, `ERPSystem.Application/UseCases/Finance/FinanceHandlers.cs:42-63`).

`public."FabricRolls"`

- Initial migration creates `FabricRolls` (`ERPSystem.Infrastructure/Migrations/20260626235435_InitialCreate.cs:459-480`).
- Traceability migration adds `ContainerItemId`, `CostPerMeter`, `LotCode`, `RemainingLengthMeters` and initializes remaining length from original length (`ERPSystem.Infrastructure/Migrations/20260630142011_AddFabricRollInventoryTraceability.cs:14-41`).
- Inventory-engine migration adds batch/storage/barcode/quality/reservation columns and indexes (`ERPSystem.Infrastructure/Migrations/20260710120000_AddInventoryEngineModule.cs:53-58`, `ERPSystem.Infrastructure/Migrations/20260710120000_AddInventoryEngineModule.cs:414-415`).

### Text Relationship Diagram

Confirmed relationship shape from columns/configuration:

```text
sales.sales_invoices (1) -- GUID column only -- (many) sales.sales_invoice_items
sales.sales_invoice_items (1) -- GUID column only -- (many) sales.sales_invoice_roll_details
sales.sales_invoice_roll_details (many) -- optional FabricRollId GUID -- (1) public."FabricRolls"
sales.sales_invoices (many) -- CustomerId GUID -- (1) parties.customers
sales.sales_invoices (many) -- WarehouseId GUID -- (1) inventory.warehouses
sales.sales_invoices (many) -- ChinaContainerId GUID -- (1) china_import.containers
sales.sales_invoices (1) -- unique SalesInvoiceId -- (0/1) sales.warehouse_detailing_sessions
sales.sales_invoices (1) -- GUID column only -- (many) sales.receipt_invoice_payments
finance.receipt_vouchers (1) -- GUID column only -- (many) sales.receipt_invoice_payments
sales.sales_invoices (1) -- OriginalInvoiceId GUID -- (many) sales.sales_returns
sales.sales_returns (1) -- GUID column only -- (many) sales.sales_return_lines
```

No `HasOne().HasForeignKey()` relationship appears in `SalesConfigurations`; only table names, keys, precision, indexes, and query filters are configured (`ERPSystem.Infrastructure/Configurations/SalesConfigurations.cs:8-103`).

## Part 5: API Layer

Plain-language summary: Sales API exists as Minimal API endpoints under `/api/v1/sales`; detailing has its own `/api/v1/detailing`; receipt allocation has `/api/v1/receipts`. These groups require authentication, but sales endpoints do not attach endpoint-level permission codes; permission checks are in handlers.

### Sales Endpoints

Base group:

- `/api/v1/sales`, tag `sales`, `RequireAuthorization()` (`ERPSystem.Api/Endpoints/SalesEndpoints.cs:18-20`).

Endpoints:

- `GET /api/v1/sales/invoices`: request query `search`, `status`, `customerId`, `from`, `to`, `page`, `pageSize`, `sortBy`, `sortDescending`; calls `GetSalesInvoiceListHandler` and returns `PagedResult<SalesInvoiceDto>` (`ERPSystem.Api/Endpoints/SalesEndpoints.cs:22`, `ERPSystem.Api/Endpoints/SalesEndpoints.cs:34-61`).
- `GET /api/v1/sales/invoices/{invoiceId}`: calls `GetSalesInvoiceOperationsCenterHandler`; returns `SalesInvoiceOperationsCenterDto` (`ERPSystem.Api/Endpoints/SalesEndpoints.cs:23`, `ERPSystem.Api/Endpoints/SalesEndpoints.cs:63-70`).
- `POST /api/v1/sales/invoices`: body `CreateSalesInvoiceRequest(CustomerId, WarehouseId, ChinaContainerId, PaymentType, DiscountAmount, PartialPaymentAmount, InvoiceNumber, Lines)`; uses current branch/company and calls `CreateSalesInvoiceDraftHandler`; returns invoice id (`ERPSystem.Api/Endpoints/SalesEndpoints.cs:24`, `ERPSystem.Api/Endpoints/SalesEndpoints.cs:72-110`, `ERPSystem.Api/Endpoints/SalesEndpoints.cs:169-187`).
- `POST /api/v1/sales/invoices/{invoiceId}/send-to-warehouse`: calls `SendSalesInvoiceToWarehouseHandler` (`ERPSystem.Api/Endpoints/SalesEndpoints.cs:25`, `ERPSystem.Api/Endpoints/SalesEndpoints.cs:112-119`).
- `POST /api/v1/sales/invoices/{invoiceId}/approve`: calls `ApproveSalesInvoiceHandler` (`ERPSystem.Api/Endpoints/SalesEndpoints.cs:26`, `ERPSystem.Api/Endpoints/SalesEndpoints.cs:121-128`).
- `POST /api/v1/sales/invoices/{invoiceId}/cancel`: body `Reason`; calls `CancelSalesInvoiceHandler` (`ERPSystem.Api/Endpoints/SalesEndpoints.cs:27`, `ERPSystem.Api/Endpoints/SalesEndpoints.cs:130-142`, `ERPSystem.Api/Endpoints/SalesEndpoints.cs:189`).
- `GET /api/v1/sales/invoices/{invoiceId}/below-cost`: calls `CheckSalesInvoiceBelowCostHandler` (`ERPSystem.Api/Endpoints/SalesEndpoints.cs:28`, `ERPSystem.Api/Endpoints/SalesEndpoints.cs:145-151`).
- `GET /api/v1/sales/warehouse-stock?containerId=&warehouseId=`: calls `GetSalesWarehouseStockHandler` (`ERPSystem.Api/Endpoints/SalesEndpoints.cs:29`, `ERPSystem.Api/Endpoints/SalesEndpoints.cs:154-167`).

Example create request:

```json
{
  "customerId": "11111111-1111-1111-1111-111111111111",
  "warehouseId": "22222222-2222-2222-2222-222222222222",
  "chinaContainerId": "33333333-3333-3333-3333-333333333333",
  "paymentType": 1,
  "discountAmount": 0,
  "partialPaymentAmount": 575,
  "invoiceNumber": null,
  "lines": [
    {
      "lineNumber": 1,
      "fabricItemId": "44444444-4444-4444-4444-444444444444",
      "fabricColorId": "55555555-5555-5555-5555-555555555555",
      "rollCount": 3,
      "unitPrice": 4.5,
      "originalUnitPrice": 4.7,
      "discountReason": "عميل وفي",
      "notes": "طلب عميل ABC"
    }
  ]
}
```

Example response:

```json
"66666666-6666-6666-6666-666666666666"
```

### Detailing Endpoints

- Base group `/api/v1/detailing`, authenticated (`ERPSystem.Api/Endpoints/DetailingEndpoints.cs:16-18`).
- `GET /queue?warehouseId=` validates warehouse id then calls `GetWarehouseDetailingQueueHandler` (`ERPSystem.Api/Endpoints/DetailingEndpoints.cs:20-21`, `ERPSystem.Api/Endpoints/DetailingEndpoints.cs:32-48`).
- `GET /{invoiceId}` returns `WarehouseDetailingDto` only if operations center has a detailing payload (`ERPSystem.Api/Endpoints/DetailingEndpoints.cs:23-24`, `ERPSystem.Api/Endpoints/DetailingEndpoints.cs:51-64`).
- `POST /{invoiceId}/complete` maps body `RollEntries` to `CompleteWarehouseDetailingCommand` (`ERPSystem.Api/Endpoints/DetailingEndpoints.cs:26-27`, `ERPSystem.Api/Endpoints/DetailingEndpoints.cs:67-93`).

Example complete request:

```json
{
  "rollEntries": [
    { "rollDetailId": "aaaaaaa1-aaaa-aaaa-aaaa-aaaaaaaaaaa1", "rollNumber": 101, "lengthMeters": 180 },
    { "rollDetailId": "aaaaaaa2-aaaa-aaaa-aaaa-aaaaaaaaaaa2", "rollNumber": 102, "lengthMeters": 160 },
    { "rollDetailId": "aaaaaaa3-aaaa-aaaa-aaaa-aaaaaaaaaaa3", "rollNumber": 103, "lengthMeters": 160 }
  ]
}
```

### Receipt Endpoints

- Base group `/api/v1/receipts`, authenticated (`ERPSystem.Api/Endpoints/ReceiptEndpoints.cs:14-16`).
- `POST /api/v1/receipts`: creates voucher and allocations (`ERPSystem.Api/Endpoints/ReceiptEndpoints.cs:18-19`, `ERPSystem.Api/Endpoints/ReceiptEndpoints.cs:27-56`).
- `POST /api/v1/receipts/{id}/post`: posts receipt voucher (`ERPSystem.Api/Endpoints/ReceiptEndpoints.cs:21-22`, `ERPSystem.Api/Endpoints/ReceiptEndpoints.cs:59-65`).

Authorization:

- Endpoint groups require authentication (`ERPSystem.Api/Endpoints/SalesEndpoints.cs:18-20`, `ERPSystem.Api/Endpoints/DetailingEndpoints.cs:16-18`, `ERPSystem.Api/Endpoints/ReceiptEndpoints.cs:14-16`).
- Permission codes are checked inside handlers: `sales.create`, `sales.detailing`, `sales.approve`, `sales.cancel`, `finance.receipt.create`, and `finance.receipt.post` (`ERPSystem.Application/UseCases/Sales/SalesInvoiceHandlers.cs:52-53`, `ERPSystem.Application/UseCases/Sales/SalesInvoiceHandlers.cs:291-307`, `ERPSystem.Application/UseCases/Sales/SalesInvoiceHandlers.cs:396-397`, `ERPSystem.Application/UseCases/Sales/SalesInvoiceHandlers.cs:499-500`, `ERPSystem.Application/UseCases/Finance/FinanceHandlers.cs:30-31`).

## Part 6: Real Example Trace

Plain-language summary: The system creates a draft with roll counts first. The 500 meters are not financially final until warehouse enters exact roll lengths. Approval posts AR/revenue/discount/COGS/inventory, while the 50% receipt is a separate finance voucher.

Scenario:

- Customer: ABC Textiles.
- Three rolls: 180m, 160m, 160m = 500m.
- Original card price: $4.70/m.
- Applied selling price: $4.50/m.
- Line discount: $0.20/m * 500m = $100.
- No invoice-level discount.
- Tax: $0 because `TaxTotal` defaults to zero and no tax method exists (`ERPSystem.Domain/Aggregates/SalesInvoiceAggregate.cs:25-28`, `ERPSystem.Domain/Aggregates/SalesInvoiceAggregate.cs:256-264`).
- Grand total: `SubTotal 2250 + Tax 0 - InvoiceDiscount 0 = 2250`.
- Up-front receipt: 50% = $1125. Remaining AR = $1125.

Step trace:

1. UI draft: user opens New Invoice, selects date/customer/warehouse/container/cashbox/payment type, enters one line with 3 rolls, original unit price 4.70, applied unit price 4.50, and saves (`Controls/Sales/NewSalesInvoiceControl.xaml:354-424`, `Controls/Sales/NewSalesInvoiceControl.xaml:534-608`).
2. API/handler: `POST /api/v1/sales/invoices` maps request to `CreateSalesInvoiceDraftCommand`, then `CreateSalesInvoiceDraftHandler` validates permissions/stock, generates number, creates aggregate, adds one item and three roll details (`ERPSystem.Api/Endpoints/SalesEndpoints.cs:72-110`, `ERPSystem.Application/UseCases/Sales/SalesInvoiceHandlers.cs:48-114`, `ERPSystem.Domain/Aggregates/SalesInvoiceAggregate.cs:91-97`).
3. Warehouse send: user sends to warehouse; domain changes status to `AwaitingDetailing` and creates a pending detailing session (`ERPSystem.Domain/Aggregates/SalesInvoiceAggregate.cs:151-160`).
4. Warehouse detailing: warehouse enters roll serials/lengths 180/160/160. `CompleteDetailing()` recalculates item total: `500 * 4.50 = 2250`; line discount: `(4.70 - 4.50) * 500 = 100`; aggregate subtotal = 2250; grand total = 2250 (`ERPSystem.Domain/Entities/Sales/SalesEntities.cs:65-75`, `ERPSystem.Domain/Aggregates/SalesInvoiceAggregate.cs:177-192`, `ERPSystem.Domain/Aggregates/SalesInvoiceAggregate.cs:256-264`).
5. Approval: handler starts transaction, checks credit, approves invoice, records customer invoice, deducts inventory, posts GL, writes audit for the $100 discount, updates, saves, commits (`ERPSystem.Application/UseCases/Sales/SalesInvoiceHandlers.cs:413-450`).
6. Inventory: three roll details reduce `FabricRollEntity.RemainingLengthMeters`; if depleted, roll status becomes Sold and warehouse stock meters decrease; movement lines are recorded with negative quantities and COGS is returned (`ERPSystem.Infrastructure/Services/InventoryEngine.cs:640-713`).
7. Accounting on approval:

```text
Dr AccountsReceivable     2250
Cr SalesRevenue           2350
Dr SalesDiscounts          100
Dr CostOfGoodsSold         cogsAmount
Cr InventoryAsset          cogsAmount
```

This follows `netReceivable = GrandTotal`, `lineDiscount = TotalLineDiscount`, and `grossRevenue = netReceivable + lineDiscount` (`ERPSystem.Infrastructure/Services/IntegratedAccountingService.cs:73-90`).

8. Receipt: user creates receipt voucher for $1125 with allocation to this invoice. Handler creates voucher allocation and `ReceiptInvoicePayment`; posting later debits cash and credits AR (`ERPSystem.Application/UseCases/Finance/FinanceHandlers.cs:35-63`, `ERPSystem.Infrastructure/Services/IntegratedAccountingService.cs:102-118`).
9. Final persisted shape:

```text
sales_invoices:
  Status = Approved (4)
  SubTotal = 2250
  DiscountTotal = 0
  TaxTotal = 0
  GrandTotal = 2250
  ApprovedAt = set

sales_invoice_items:
  RollCount = 3
  UnitPrice = 4.50
  OriginalUnitPrice = 4.70
  LineTotal = 2250
  DiscountAmount = 100

sales_invoice_roll_details:
  three rows with LengthMeters 180, 160, 160 and FabricRollId assigned

sales.receipt_invoice_payments:
  Amount = 1125 linked to receipt voucher and invoice
```

Column support is defined in `sales_invoices`, `sales_invoice_items`, `sales_invoice_roll_details`, and `receipt_invoice_payments` migrations/configurations (`ERPSystem.Infrastructure/Migrations/20260626235435_InitialCreate.cs:778-867`, `ERPSystem.Infrastructure/Migrations/20260711120000_AddSalesReturnsAndDelivery.cs:75-92`, `ERPSystem.Infrastructure/Configurations/SalesConfigurations.cs:8-103`).

## Part 7: Known Gaps and Constraints

Plain-language summary: The sales flow is functional but has known accounting/control gaps. New work should either preserve these constraints or explicitly plan fixes before relying on behavior that does not exist.

- F-02 no tax: `TaxTotal` defaults to zero, there is no public tax setter in the aggregate, and grand total uses whatever `TaxTotal` already contains. Tax would plug into aggregate totals before approval and into `PostSalesInvoiceApprovalAsync` before journal posting (`ERPSystem.Domain/Aggregates/SalesInvoiceAggregate.cs:25-28`, `ERPSystem.Domain/Aggregates/SalesInvoiceAggregate.cs:256-264`, `ERPSystem.Infrastructure/Services/IntegratedAccountingService.cs:68-100`).
- F-04 concurrency: approval checks status/spec before the transaction, then updates invoice and inventory without a sales invoice row-version token. Sales EF config has keys/indexes/query filter but no `RowVersion` mapping (`ERPSystem.Application/UseCases/Sales/SalesInvoiceHandlers.cs:399-405`, `ERPSystem.Infrastructure/Configurations/SalesConfigurations.cs:8-20`, `ERPSystem.Infrastructure/Services/InventoryEngine.cs:631-713`).
- F-05 segregation of duties: `ApproveSalesInvoiceHandler` checks permission `sales.approve`, but does not compare approver user id against `CreatedByUserId` before `aggregate.Approve(userId)` (`ERPSystem.Application/UseCases/Sales/SalesInvoiceHandlers.cs:396-418`, `ERPSystem.Domain/Aggregates/SalesInvoiceAggregate.cs:29-30`).
- F-16 no reversal: `Cancel()` refuses Approved/Printed/Delivered invoices and there is no sales invoice reverse/unapprove handler. `ReversedByJournalEntryId` exists on aggregate/entity but is not assigned by the documented flow (`ERPSystem.Domain/Aggregates/SalesInvoiceAggregate.cs:41`, `ERPSystem.Domain/Aggregates/SalesInvoiceAggregate.cs:273-280`, `ERPSystem.Infrastructure/Persistence/Models/Sales/SalesEntities.cs:28-29`).
- Receipt allocation is separate from invoice approval: partial payment on the invoice is captured as a field, but actual cash/AR settlement is through receipt vouchers and `receipt_invoice_payments` (`ERPSystem.Application/Commands/Sales/SalesInvoiceCommands.cs:15`, `ERPSystem.Application/UseCases/Finance/FinanceHandlers.cs:42-63`).
- Delivery note aggregate exists but the implemented delivery handler marks the invoice delivered directly; no separate delivery note persistence flow was found (`ERPSystem.Domain/Entities/Sales/SalesEntities.cs:191-208`, `ERPSystem.Application/UseCases/Sales/SalesDeliveryHandlers.cs:10-35`).

