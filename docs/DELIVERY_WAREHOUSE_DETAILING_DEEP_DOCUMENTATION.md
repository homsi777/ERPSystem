# Delivery / Warehouse Detailing Module - Deep Documentation

Date: 2026-07-10  
Scope: current code state after per-line container selection and draft reservation indicator work.  
Primary platform: Web React client. WPF is documented separately because it remains present and functional.

This document is code-verified. Every behavioral claim below is tied to file and line references.

## 1. Module Boundaries And Entry Points

### 1.1 Web Routes

The browser delivery/detailing module is exposed through the React router in `web-client/src/App.tsx`. The `/delivery` route opens the queue page, and `/delivery/:invoiceId` opens a specific invoice detailing page (`web-client/src/App.tsx:9`, `web-client/src/App.tsx:29-30`).

The page component itself first checks the `warehouse.detailing` permission. If the permission is missing, the user sees an error state instead of the module (`web-client/src/pages/Delivery.tsx:24-34`). If an `invoiceId` route parameter exists, the same component renders `DeliveryDetailPage`; otherwise it renders `DeliveryQueuePage` (`web-client/src/pages/Delivery.tsx:36-40`).

### 1.2 WPF Entry Point

The WPF equivalent is `WarehouseDetailingPageControl`. It builds a queue screen with a warehouse selector, banner area, empty state, and grid columns for invoice number, customer, container, roll count, and status (`Controls/Sales/WarehouseDetailingPageControl.cs:19-29`, `Controls/Sales/WarehouseDetailingPageControl.cs:34-90`).

When a queue row is selected, WPF loads the detailing workspace by passing invoice identity, invoice number, customer name, header container display text, roll DTOs, and unit price into `WarehouseDetailingWorkspaceControl.LoadFromDatabase` (`Controls/Sales/WarehouseDetailingPageControl.cs:256-268`).

## 2. Web UI Screens And Current Roll Lookup Behavior

### 2.1 Queue Screen

The queue page keeps local state for the selected warehouse and toast message (`web-client/src/pages/Delivery.tsx:43-47`). It loads warehouses through `getWarehouses`, prefers the default warehouse when present, and otherwise selects the first warehouse (`web-client/src/pages/Delivery.tsx:57-73`).

The queue data is loaded with React Query using the key `['detailing', 'queue', warehouseId]` and `getDetailingQueue(warehouseId)`. The query is only enabled when a warehouse is selected (`web-client/src/pages/Delivery.tsx:75-79`).

The visible queue UI includes:

- A warehouse dropdown (`web-client/src/pages/Delivery.tsx:89-104`).
- Loading, error, and empty states (`web-client/src/pages/Delivery.tsx:107-119`).
- One card per invoice, linking to `/delivery/{invoiceId}` (`web-client/src/pages/Delivery.tsx:121-129`).
- Card data for invoice number, customer name, sent-to-warehouse date, roll count, and status (`web-client/src/pages/Delivery.tsx:135-147`).
- Arabic status labels for pending, in-progress, completed, and rejected (`web-client/src/pages/Delivery.tsx:150-155`).

The queue API is `GET /api/v1/detailing/queue?warehouseId=...`, implemented in the web API client as `getDetailingQueue` (`web-client/src/api/detailing.ts:4-7`) and in the API endpoint group as `/api/v1/detailing/queue` (`ERPSystem.Api/Endpoints/DetailingEndpoints.cs:14-20`, `ERPSystem.Api/Endpoints/DetailingEndpoints.cs:32-48`).

### 2.2 Detail Screen

The detail page loads invoice detailing data through `getDetailing(invoiceId)`, which calls `GET /api/v1/detailing/{invoiceId}` (`web-client/src/pages/Delivery.tsx:165-168`, `web-client/src/api/detailing.ts:9-11`). The endpoint returns the operations-center detailing projection when the invoice is awaiting detailing or detailed; otherwise it returns 404 with "Invoice is not awaiting detailing." (`ERPSystem.Api/Endpoints/DetailingEndpoints.cs:51-64`).

The web detail page maintains two local maps:

- `lengths`: manual meter input by `rollDetailId` (`web-client/src/pages/Delivery.tsx:158-160`).
- `serials`: DPL / roll serial input by `rollDetailId` (`web-client/src/pages/Delivery.tsx:161-164`).

Existing valid lengths from the API are copied into `lengths` once, only when the map is still empty (`web-client/src/pages/Delivery.tsx:170-184`). The manually entered total meters shown in the UI sums only local manual length values. A source comment explicitly states that serial-based length is resolved by the API from inventory (`web-client/src/pages/Delivery.tsx:186-189`).

The detail page allows completion only when every roll has either a serial number or a manual length (`web-client/src/pages/Delivery.tsx:193-203`). The UI renders each roll as a card with:

- Sequence number (`web-client/src/pages/Delivery.tsx:305-308`).
- Fabric and color display fields (`web-client/src/pages/Delivery.tsx:309-311`).
- DPL serial input (`web-client/src/pages/Delivery.tsx:313-324`).
- Manual length input (`web-client/src/pages/Delivery.tsx:325-337`).

The active hint says the warehouse employee may enter either a DPL serial or a length, and that a serial is more accurate because it deducts from the same inventory roll (`web-client/src/pages/Delivery.tsx:300-302`).

### 2.3 Core Operational Problem: Finding The Physical Roll

The main practical gap for warehouse employees is still roll discovery. The web detailing page asks the employee to enter a DPL serial or length, but it does not provide an in-page picker, scanner result list, candidate roll table, fabric/container filtered lookup, or reservation warning before submit.

Current browser behavior is:

1. The employee opens `/delivery`.
2. The employee chooses a warehouse from the dropdown (`web-client/src/pages/Delivery.tsx:89-104`).
3. The employee opens an invoice card (`web-client/src/pages/Delivery.tsx:121-129`).
4. The employee sees roll cards with fabric and color, but no per-roll candidate inventory list (`web-client/src/pages/Delivery.tsx:303-340`).
5. The employee manually types a DPL serial into a numeric input, or manually types a length (`web-client/src/pages/Delivery.tsx:313-337`).
6. The employee submits; the frontend sends only `rollDetailId`, `rollNumber`, and `lengthMeters` (`web-client/src/pages/Delivery.tsx:205-213`).
7. The backend resolves the actual `FabricRollId` during completion, not during browsing (`ERPSystem.Infrastructure/Services/InventoryEngine.cs:508-633`).

There is no dedicated browser-side "find physical roll" API call from the delivery page. The delivery page imports only detailing APIs, warehouse lookup, auth, and UI helpers; it does not import inventory roll lookup or reservation lookup functions (`web-client/src/pages/Delivery.tsx:1-17`). The inventory lookup functions exist, but they are used by the inventory modal, not by the delivery detail page (`web-client/src/api/inventory.ts:50-75`, `web-client/src/pages/Inventory.tsx:260-282`).

#### Worked Example

Assume a sales invoice has two lines after the per-line container change:

- Line 1: container A, fabric X, red, 2 rolls.
- Line 2: container B, fabric X, red, 1 roll.

The sales command model supports `ChinaContainerId` per invoice line (`ERPSystem.Application/Commands/Sales/SalesInvoiceCommands.cs:19-23`). During create/update, the application validates stock using each line's own `ChinaContainerId`, while keeping the header container only as the primary container for backward-compatible display/reporting (`ERPSystem.Application/UseCases/Sales/SalesInvoiceHandlers.cs:57-75`, `ERPSystem.Application/UseCases/Sales/SalesInvoiceHandlers.cs:155-164`).

When the warehouse opens detailing, the web DTO exposes a header-level `ChinaContainerId` on `WarehouseDetailingDto`, but `WarehouseDetailingRollDto` does not expose the per-line container (`ERPSystem.Application/DTOs/Sales/SalesDtos.cs:67-89`). The roll cards show fabric and color, not the source container for each roll (`web-client/src/pages/Delivery.tsx:303-340`).

If the employee types serial `123` for a roll belonging to line 2, the backend resolves it against the line item's `ChinaContainerId`, `FabricItemId`, and `FabricColorId` (`ERPSystem.Infrastructure/Services/InventoryEngine.cs:532-538`). If the serial exists in another line's container, the lookup does not accept it for line 2. If a roll number exists but fabric/color does not match, the backend throws a mismatch error (`ERPSystem.Infrastructure/Services/InventoryEngine.cs:540-552`). If no matching roll exists in the invoice warehouse/container, the backend throws a not-found error (`ERPSystem.Infrastructure/Services/InventoryEngine.cs:555-556`).

That backend safety is correct, but the web UI does not currently help the employee find which physical roll in container B is valid before they submit. The first strong feedback occurs only after completion fails or succeeds.

### 2.4 Completed Detail Screen

When `status === 2`, the web page treats the detailing session as completed (`web-client/src/pages/Delivery.tsx:191`). In completed mode, it shows a success banner, a read-only list of roll lengths, fabric, and color, and a back button (`web-client/src/pages/Delivery.tsx:278-296`). It does not provide edit or re-open controls.

## 3. Application Handlers And Data Flow

### 3.1 Sending A Sales Invoice To Warehouse

Sending to warehouse is handled by `SendSalesInvoiceToWarehouseHandler`. It checks `sales.send-to-warehouse`, loads the invoice aggregate, calls `aggregate.SendToWarehouse()`, reserves inventory for the invoice, persists, dispatches domain events, and publishes an inventory-changed notification (`ERPSystem.Application/UseCases/Sales/SalesInvoiceHandlers.cs:255-296`).

The aggregate transition requires draft status, at least one item, sets status to `AwaitingDetailing`, sets `SentToWarehouseAt`, creates a pending detailing session, and raises a warehouse-sent event (`ERPSystem.Domain/Aggregates/SalesInvoiceAggregate.cs:151-159`).

### 3.2 Queue Query

The web and WPF queue both use `GetWarehouseDetailingQueueHandler`. It calls `invoiceRepository.GetDetailingQueueAsync(query.WarehouseId)`, maps each aggregate to a detailing DTO, enriches roll display data, and returns the list (`ERPSystem.Application/UseCases/Queries/OperationsQueryHandlers.cs:298-325`).

The repository filters sales invoices by selected warehouse and `AwaitingDetailing`, and orders by `SentToWarehouseAt` (`ERPSystem.Infrastructure/Repositories/AggregateRepositories.cs:248-260`).

### 3.3 Detail Query

The detail endpoint uses `GetSalesInvoiceOperationsCenterHandler` and returns only the `Detailing` slice (`ERPSystem.Api/Endpoints/DetailingEndpoints.cs:51-64`). The operations-center handler loads the aggregate, customer, maps the base DTO, enriches invoice lines, enriches detailing rolls, and returns an operations-center DTO (`ERPSystem.Application/UseCases/Queries/OperationsQueryHandlers.cs:160-197`, `ERPSystem.Application/UseCases/Queries/OperationsQueryHandlers.cs:279-294`).

`DomainMappers.ToOperationsCenterDto` only includes detailing when the invoice status is `AwaitingDetailing` or `Detailed`, and only allows completion when the status is `AwaitingDetailing` (`ERPSystem.Application/Mapping/DomainMappers.cs:270-275`).

### 3.4 Completing Detailing

The web page posts completion through `completeDetailing(invoiceId, request)`, which calls `POST /api/v1/detailing/{invoiceId}/complete` (`web-client/src/api/detailing.ts:13-18`). The endpoint maps request roll entries into `CompleteWarehouseDetailingCommand` (`ERPSystem.Api/Endpoints/DetailingEndpoints.cs:67-93`).

`CompleteWarehouseDetailingHandler` performs the following sequence:

1. Validate the command (`ERPSystem.Application/UseCases/Sales/SalesInvoiceHandlers.cs:312-314`).
2. Check `warehouse.detailing` permission (`ERPSystem.Application/UseCases/Sales/SalesInvoiceHandlers.cs:316-317`).
3. Load the invoice aggregate (`ERPSystem.Application/UseCases/Sales/SalesInvoiceHandlers.cs:319-321`).
4. Require the warehouse detailing specification (`ERPSystem.Application/UseCases/Sales/SalesInvoiceHandlers.cs:323-325`).
5. Resolve serials and lengths through inventory operations (`ERPSystem.Application/UseCases/Sales/SalesInvoiceHandlers.cs:331-338`).
6. Enter every resolved length into the aggregate (`ERPSystem.Application/UseCases/Sales/SalesInvoiceHandlers.cs:340-346`).
7. Require every roll to be valid before completion (`ERPSystem.Application/UseCases/Sales/SalesInvoiceHandlers.cs:348-349`).
8. Complete the aggregate, persist, dispatch events, publish a notification, and return success (`ERPSystem.Application/UseCases/Sales/SalesInvoiceHandlers.cs:351-376`).

The aggregate enforces the same core state rules: `EnterRollLength` only works while awaiting detailing, and `CompleteDetailing` requires all roll details to have valid lengths before setting status to `Detailed` and raising a completed event (`ERPSystem.Domain/Aggregates/SalesInvoiceAggregate.cs:169-191`).

## 4. Roll Resolution, Reservation, And Save Mechanics

### 4.1 Serial-Based Resolution

`InventoryEngine.ResolveDetailingEntriesAsync` is the authoritative roll resolver. It prefetches candidate rolls from all containers used by the invoice lines, in the invoice warehouse, with status `Reserved` or `Available` (`ERPSystem.Infrastructure/Services/InventoryEngine.cs:515-522`).

When a serial is provided, the resolver first matches the line item's container, roll number, fabric item, and color (`ERPSystem.Infrastructure/Services/InventoryEngine.cs:532-538`). If no exact match is found, it checks the same line container and roll number, then throws a fabric/color mismatch when the serial belongs to a different fabric or color (`ERPSystem.Infrastructure/Services/InventoryEngine.cs:540-552`). If still not found, it throws that the roll does not exist in the invoice warehouse/container (`ERPSystem.Infrastructure/Services/InventoryEngine.cs:555-556`).

The resolver also blocks using the same physical roll twice within the same completion payload (`ERPSystem.Infrastructure/Services/InventoryEngine.cs:558-559`). If the roll is still `Available`, it marks it reserved before assigning it to the roll detail (`ERPSystem.Infrastructure/Services/InventoryEngine.cs:564-571`).

When a serial is used and manual length is not positive, the resolved length becomes the roll's remaining length (`ERPSystem.Infrastructure/Services/InventoryEngine.cs:573-580`). This matches the web comment that serial length is resolved by the API rather than counted in the browser's manual total (`web-client/src/pages/Delivery.tsx:186-189`).

### 4.2 Length-Only Resolution

If no serial is provided, the resolver requires a positive length (`ERPSystem.Infrastructure/Services/InventoryEngine.cs:586-587`). It groups length-only entries by invoice line, finds unassigned details for that line, and searches the reserved candidate pool by the line's container, fabric, color, reserved status, and remaining length (`ERPSystem.Infrastructure/Services/InventoryEngine.cs:596-624`). It then assigns the selected `FabricRollId` to the detail (`ERPSystem.Infrastructure/Services/InventoryEngine.cs:628`).

This means manual length entry is not just a length save; it also attempts to bind the detail row to an inventory roll behind the scenes.

### 4.3 Persistence Model

The final completion save is aggregate-based. The handler updates the aggregate through `aggregate.EnterRollLength` and `aggregate.CompleteDetailing`, then calls `invoiceRepository.UpdateAsync` and `SaveAndDispatchAsync` (`ERPSystem.Application/UseCases/Sales/SalesInvoiceHandlers.cs:340-358`).

The repository update persists invoice header fields, including status, header container, sent/detailed dates, and totals (`ERPSystem.Infrastructure/Repositories/AggregateRepositories.cs:269-295`). It also synchronizes child invoice items and persists each item-level `ChinaContainerId` (`ERPSystem.Infrastructure/Repositories/AggregateRepositories.cs:321-339`).

### 4.4 Notifications

After successful completion, the handler tries to publish `SalesInvoiceDetailedNotification`. Notification failure is caught and does not fail the detailing completion (`ERPSystem.Application/UseCases/Sales/SalesInvoiceHandlers.cs:360-374`).

On the web, success invalidates the detailing and dashboard query caches, then navigates back to `/delivery` with a success toast (`web-client/src/pages/Delivery.tsx:214-220`). The queue page reads that navigation state once and displays the toast (`web-client/src/pages/Delivery.tsx:49-55`).

### 4.5 What "Save" Means Today

The browser has no draft save button for detailing. It only posts the final complete action (`web-client/src/pages/Delivery.tsx:205-231`, `web-client/src/pages/Delivery.tsx:345-360`).

WPF has a button labeled "التحقق من الأطوال" that calls `TryCompleteAsync(saveOnly: true)`, but that branch only validates locally and displays a success message; it does not call an API or persist partial detailing data (`Controls/Workspace/WarehouseDetailingWorkspaceControl.cs:145-151`, `Controls/Workspace/WarehouseDetailingWorkspaceControl.cs:199-216`). The WPF final completion button calls the application completion flow (`Controls/Workspace/WarehouseDetailingWorkspaceControl.cs:152-157`, `Controls/Workspace/WarehouseDetailingWorkspaceControl.cs:237-247`).

There is a domain method `StartDetailing`, but the collected application flow does not call it from the web delivery page or WPF completion path. The domain method itself only starts the detailing session when the invoice is awaiting detailing (`ERPSystem.Domain/Aggregates/SalesInvoiceAggregate.cs:162-167`).

## 5. Schema, DTOs, And API Contracts

### 5.1 Status Enums

Sales invoice statuses include `Draft`, `AwaitingDetailing`, `Detailed`, `ReadyForApproval`, `Approved`, `Printed`, `Delivered`, cancellation, and return states (`ERPSystem.Domain/Enums/SalesInvoiceStatus.cs:3-15`). Warehouse detailing session statuses are `Pending`, `InProgress`, `Completed`, and `Rejected` (`ERPSystem.Domain/Enums/WarehouseDetailingStatus.cs:3-9`).

The web client maps warehouse detailing status labels and tones in `web-client/src/lib/enums.ts` (`web-client/src/lib/enums.ts:141-160`).

### 5.2 Sales Invoice Line Container Schema

The current domain model stores `ChinaContainerId` on each sales invoice item (`ERPSystem.Domain/Entities/Sales/SalesEntities.cs:11`). `SalesInvoiceItem.Create` requires that value and rejects an empty container id (`ERPSystem.Domain/Entities/Sales/SalesEntities.cs:35-58`).

The persistence model also stores `ChinaContainerId` on `SalesInvoiceItemEntity` (`ERPSystem.Infrastructure/Persistence/Models/Sales/SalesEntities.cs:72-89`). The EF configuration indexes `ChinaContainerId` and `(SalesInvoiceId, ChinaContainerId)` for sales invoice items (`ERPSystem.Infrastructure/Configurations/SalesConfigurations.cs:24-39`). The migration `AddSalesInvoiceItemLineContainer` added the column, backfilled existing rows from the parent invoice header, set it not-null, and added indexes (`ERPSystem.Infrastructure/Migrations/20260717120000_AddSalesInvoiceItemLineContainer.cs:12-31`).

The header `SalesInvoiceEntity` still has `ChinaContainerId` (`ERPSystem.Infrastructure/Persistence/Models/Sales/SalesEntities.cs:10`). Application code treats that header value as the primary container for backward-compatible display/reporting, while stock operations use the per-line container (`ERPSystem.Application/UseCases/Sales/SalesInvoiceHandlers.cs:70-75`, `ERPSystem.Application/UseCases/Sales/SalesInvoiceHandlers.cs:159-164`).

### 5.3 Detailing DTOs

`WarehouseDetailingDto` exposes invoice id, invoice number, customer name, header `ChinaContainerId`, sent date, representative unit price, status, and roll list (`ERPSystem.Application/DTOs/Sales/SalesDtos.cs:67-77`).

`WarehouseDetailingRollDto` exposes roll detail id, sales invoice item id, roll sequence, fabric display/code, color display, length, and validity (`ERPSystem.Application/DTOs/Sales/SalesDtos.cs:79-89`). It does not expose the line's `ChinaContainerId`. This is the current DTO gap that affects browser-side physical roll discovery for multi-container invoices.

`DomainMappers.ToDetailingDto` fills the header container and roll data from aggregate state (`ERPSystem.Application/Mapping/DomainMappers.cs:247-264`). `SalesInvoiceCatalogEnricher.EnrichRollsAsync` enriches roll display data with fabric and color, but it does not add a per-roll or per-line container field to the detailing DTO (`ERPSystem.Application/Common/SalesInvoiceCatalogEnricher.cs:55-88`).

### 5.4 Inventory Roll DTOs And Reservation DTO

The inventory roll list DTO includes roll number, barcode, fabric, color, original length, remaining length, cost, current value, status, batch, location, and lot code (`ERPSystem.Application/DTOs/Inventory/InventoryManagementDtos.cs:76-91`).

The sales reservation DTO includes `FabricRollId`, sales invoice id, sales invoice number, and sales invoice status (`ERPSystem.Application/DTOs/Inventory/InventoryManagementDtos.cs:93-99`).

### 5.5 API Surface

Detailing endpoints:

- `GET /api/v1/detailing/queue` (`ERPSystem.Api/Endpoints/DetailingEndpoints.cs:20`, `ERPSystem.Api/Endpoints/DetailingEndpoints.cs:32-48`).
- `GET /api/v1/detailing/{invoiceId}` (`ERPSystem.Api/Endpoints/DetailingEndpoints.cs:23`, `ERPSystem.Api/Endpoints/DetailingEndpoints.cs:51-64`).
- `POST /api/v1/detailing/{invoiceId}/complete` (`ERPSystem.Api/Endpoints/DetailingEndpoints.cs:26`, `ERPSystem.Api/Endpoints/DetailingEndpoints.cs:67-93`).

Inventory roll lookup endpoints:

- `GET /api/v1/inventory/rolls-by-stock` with warehouse, container, fabric, and color filters (`ERPSystem.Api/Endpoints/InventoryEndpoints.cs:25-26`, `ERPSystem.Api/Endpoints/InventoryEndpoints.cs:100-113`).
- `GET /api/v1/inventory/roll-sales-reservations` with repeated roll ids and optional excluded invoice id (`ERPSystem.Api/Endpoints/InventoryEndpoints.cs:28-29`, `ERPSystem.Api/Endpoints/InventoryEndpoints.cs:115-126`).
- A generic warehouse rolls endpoint supports page number, page size, status, and search (`ERPSystem.Api/Endpoints/InventoryEndpoints.cs:79-98`).

The web delivery page does not call these inventory endpoints today; the inventory page/modal does (`web-client/src/pages/Delivery.tsx:1-17`, `web-client/src/pages/Inventory.tsx:260-282`).

## 6. WPF Detailing Flow

### 6.1 Queue

WPF loads warehouses through `SalesUiService.GetWarehousesAsync` and falls back to a default warehouse when the call fails or returns none (`Controls/Sales/WarehouseDetailingPageControl.cs:121-147`). It then loads container names for display, calls `SalesUiService.Instance.GetDetailingQueueAsync(GetSelectedWarehouseId())`, maps DTOs to queue rows, and shows or hides the empty/workspace states (`Controls/Sales/WarehouseDetailingPageControl.cs:155-203`).

`SalesUiService.GetDetailingQueueAsync` resolves `GetWarehouseDetailingQueueHandler` from DI and calls it with the selected warehouse id (`Services/Sales/SalesUiService.cs:352-359`).

### 6.2 Workspace

The WPF workspace has fields for roll detail id, roll index, fabric code, color, serial text, length text, and current-row state (`Controls/Workspace/WarehouseDetailingWorkspaceControl.cs:21-29`). Its grid columns are sequence number, fabric code, color, serial, and length (`Controls/Workspace/WarehouseDetailingWorkspaceControl.cs:120-136`).

`LoadFromDatabase` populates rows from the detailing rolls ordered by roll sequence, including fabric code, color display, and existing valid length. The header text shows invoice, customer, and the container string passed from the queue (`Controls/Workspace/WarehouseDetailingWorkspaceControl.cs:165-197`). Like the web detail DTO, this workspace does not display a per-roll/per-line container because the roll DTO does not carry one.

### 6.3 WPF Completion And Soft Reservation Warning

WPF validates that each row has either a positive serial or a positive length (`Controls/Workspace/WarehouseDetailingWorkspaceControl.cs:339-367`). It builds `RollLengthEntryCommand` values from serial and length text (`Controls/Workspace/WarehouseDetailingWorkspaceControl.cs:255-273`).

Before final completion, WPF calls `ConfirmExternalRollReservationsAsync(entries)` (`Controls/Workspace/WarehouseDetailingWorkspaceControl.cs:233-239`). That helper collects positive serials, looks up matching `FabricRolls` by roll number, calls `GetFabricRollSalesReservationsHandler` while excluding the current invoice, and shows a non-blocking confirmation if the roll appears in another active sales invoice (`Controls/Workspace/WarehouseDetailingWorkspaceControl.cs:275-319`).

This WPF warning is useful but approximate: it looks up by roll number first, not by the final resolver's line-container/fabric/color match, because the UI does not have the fully resolved fabric roll id before submit (`Controls/Workspace/WarehouseDetailingWorkspaceControl.cs:290-301`).

Final WPF completion calls `SalesUiService.Instance.CompleteDetailingAsync`, which resolves the same application command handler used by the API (`Controls/Workspace/WarehouseDetailingWorkspaceControl.cs:237-247`, `Services/Sales/SalesUiService.cs:361-373`). Permission is checked through `CanCompleteDetailingAsync`, which maps to `warehouse.detailing` (`Controls/Workspace/WarehouseDetailingWorkspaceControl.cs:227-231`, `Services/Sales/SalesUiService.cs:375-376`).

## 7. Cross-Reference To Recent Per-Line Container And Reservation Work

### 7.1 Per-Line Container Is Implemented In Sales Drafts And Backend Resolution

Per-line container selection is represented in command models by `SalesInvoiceLineCommand.ChinaContainerId` (`ERPSystem.Application/Commands/Sales/SalesInvoiceCommands.cs:19-23`). Create and update validate stock by line container and store the first positive-roll line as the header's primary container for old display/reporting (`ERPSystem.Application/UseCases/Sales/SalesInvoiceHandlers.cs:57-75`, `ERPSystem.Application/UseCases/Sales/SalesInvoiceHandlers.cs:155-164`).

The backend resolver uses `item.ChinaContainerId` when matching DPL serials and when selecting length-only reserved rolls (`ERPSystem.Infrastructure/Services/InventoryEngine.cs:532-538`, `ERPSystem.Infrastructure/Services/InventoryEngine.cs:604-624`). Therefore, the backend currently respects per-line containers during detailing completion.

### 7.2 Reservation Indicator Exists In Web Inventory Modal

The web inventory roll details modal loads rolls by warehouse, container, fabric, and color (`web-client/src/pages/Inventory.tsx:260-274`). It then calls `getFabricRollSalesReservations` for those roll ids (`web-client/src/pages/Inventory.tsx:278-282`) and builds a map by `fabricRollId` (`web-client/src/pages/Inventory.tsx:283-289`).

The modal table includes a "حجز بيع" column (`web-client/src/pages/Inventory.tsx:318-332`). When a reservation exists, the row renders a link to `/sales/{salesInvoiceId}` with the text "مسودة بيع {salesInvoiceNumber}" (`web-client/src/pages/Inventory.tsx:365-368`).

The backend reservation query considers active statuses `Detailed`, `ReadyForApproval`, `Approved`, and `Printed` (`ERPSystem.Infrastructure/Repositories/InventoryManagementRepository.cs:500-515`). It joins roll details to sales invoices, filters by requested roll ids and active invoice statuses, optionally excludes one invoice, groups by fabric roll, and returns one reservation per roll (`ERPSystem.Infrastructure/Repositories/InventoryManagementRepository.cs:517-540`).

### 7.3 What Did Not Reach Browser Detailing Yet

The browser delivery/detailing page does not show the reservation indicator. The reservation indicator is in `Inventory.tsx`, not `Delivery.tsx` (`web-client/src/pages/Inventory.tsx:278-289`, `web-client/src/pages/Delivery.tsx:1-17`).

The browser delivery/detailing page does not pre-check external reservations before completion. It calls `completeDetailing` directly with roll entries and handles backend success/error afterward (`web-client/src/pages/Delivery.tsx:205-231`). WPF has a soft warning before completion, but web does not (`Controls/Workspace/WarehouseDetailingWorkspaceControl.cs:233-239`, `Controls/Workspace/WarehouseDetailingWorkspaceControl.cs:275-319`).

The browser delivery/detailing DTO does not expose per-roll container data, even though the invoice item domain and schema now store it (`ERPSystem.Application/DTOs/Sales/SalesDtos.cs:67-89`, `ERPSystem.Domain/Entities/Sales/SalesEntities.cs:11`, `ERPSystem.Infrastructure/Persistence/Models/Sales/SalesEntities.cs:72-89`).

## 8. Known Gaps And Risks

1. No efficient physical-roll finder in browser detailing.
   The daily browser workflow requires manual serial or length entry on each roll card (`web-client/src/pages/Delivery.tsx:303-340`). Candidate inventory APIs exist, but the delivery page does not call them (`web-client/src/api/inventory.ts:50-75`, `web-client/src/pages/Delivery.tsx:1-17`).

2. No per-roll container display in detailing DTO.
   `WarehouseDetailingDto` has only a header `ChinaContainerId`, and `WarehouseDetailingRollDto` does not include per-line container (`ERPSystem.Application/DTOs/Sales/SalesDtos.cs:67-89`). This is especially important after per-line container selection because two visually identical fabric/color lines can come from different containers.

3. Browser has no soft reservation warning before final submit.
   WPF warns on possible external reservations before completion (`Controls/Workspace/WarehouseDetailingWorkspaceControl.cs:275-319`). Browser completion posts directly (`web-client/src/pages/Delivery.tsx:205-231`).

4. Browser has no persisted partial save for warehouse entries.
   The web flow has only final completion (`web-client/src/pages/Delivery.tsx:205-231`). WPF's "save/check" branch validates locally but does not persist (`Controls/Workspace/WarehouseDetailingWorkspaceControl.cs:199-216`).

5. Detail session `InProgress` is not activated by the observed UI flow.
   The domain has `StartDetailing` (`ERPSystem.Domain/Aggregates/SalesInvoiceAggregate.cs:162-167`), and the enum supports `InProgress` (`ERPSystem.Domain/Enums/WarehouseDetailingStatus.cs:3-9`), but the documented web and WPF flows go from awaiting detailing to final completion without a separate start API call.

6. Length-only entry can bind a roll invisibly.
   The resolver assigns a matching reserved roll based on line container, fabric, color, status, and remaining meters (`ERPSystem.Infrastructure/Services/InventoryEngine.cs:596-628`). The browser does not show which roll will be assigned before submit.

7. WPF reservation warning is weaker than backend resolution.
   WPF looks up entered serials by roll number and then asks for reservations (`Controls/Workspace/WarehouseDetailingWorkspaceControl.cs:290-301`). The backend resolver uses line container, roll number, fabric, and color (`ERPSystem.Infrastructure/Services/InventoryEngine.cs:532-538`).

## 9. Recommended Next Design Direction

The most direct improvement for the daily browser platform is not a visual redesign. It is to wire existing inventory lookup and reservation data into the existing roll-card workflow:

1. Extend the detailing roll DTO with the line's `ChinaContainerId` and container display text.
2. In the web detail page, for each invoice line or roll group, load candidate rolls using warehouse id, line container id, fabric item id, and color id.
3. Show a compact selectable roll list or search dropdown inside the existing roll card area.
4. Reuse `roll-sales-reservations` to show reservation warnings next to candidate rolls.
5. Keep final backend validation unchanged because `InventoryEngine.ResolveDetailingEntriesAsync` is the authoritative guard.

This direction preserves the existing frontend layout while solving the operational problem: the warehouse employee can identify and select the correct physical roll before final submission.
