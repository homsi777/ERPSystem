# ERP PRO — Domain Model Diagrams

Companion to [`ERP_PRO_DOMAIN_FOUNDATION.md`](ERP_PRO_DOMAIN_FOUNDATION.md).  
Visual reference only — not executable code.

---

## 1. Core Business Pipeline

```mermaid
flowchart LR
    CI[China Import] --> CN[Container]
    CN --> LC[Landing Cost]
    LC --> WH[Warehouse Stock]
    WH --> SI[Sales Invoice]
    SI --> WD[Warehouse Detailing]
    WD --> AP[Invoice Approval]
    AP --> CB[Customer Balance]
    AP --> AC[Accounting Posting]
```

---

## 2. Aggregate Map (Bounded Contexts)

```mermaid
flowchart TB
    subgraph identity [Identity]
        User
        Role
        Permission
    end

    subgraph company [Company]
        Company
        Branch
    end

    subgraph parties [Parties]
        Customer
        Supplier
        ChinaSupplier
    end

    subgraph china [China Import]
        ChinaContainer
        ChinaContainerItem
        LandingCost
        ContainerCustomerDistribution
    end

    subgraph inventory [Inventory]
        Warehouse
        FabricRoll
        WarehouseStockBalance
        StockMovement
    end

    subgraph sales [Sales]
        SalesInvoice
        SalesInvoiceItem
        SalesInvoiceRollDetail
    end

    subgraph finance [Finance]
        ReceiptVoucher
        PaymentVoucher
        Cashbox
    end

    subgraph accounting [Accounting]
        Account
        JournalEntry
        JournalEntryLine
    end

    ChinaContainer --> Warehouse
    Warehouse --> SalesInvoice
    SalesInvoice --> JournalEntry
    Customer --> SalesInvoice
    Customer --> ReceiptVoucher
    Supplier --> PaymentVoucher
    SalesInvoice --> StockMovement
    ChinaContainer --> LandingCost
```

---

## 3. Sales Invoice State Machine

```mermaid
stateDiagram-v2
    [*] --> Draft
    Draft --> AwaitingDetailing: SendToWarehouse
    Draft --> Cancelled: Cancel
    AwaitingDetailing --> Detailed: DetailingCompleted
    AwaitingDetailing --> Cancelled: Cancel
    Detailed --> ReadyForApproval: ReviewTotals
    ReadyForApproval --> Approved: Approve
    ReadyForApproval --> AwaitingDetailing: RejectDetailing
    Approved --> Printed: Print
    Printed --> Delivered: Deliver
    Approved --> Cancelled: CancelWithReversal
    Delivered --> [*]
    Cancelled --> [*]
```

---

## 4. China Container State Machine

```mermaid
stateDiagram-v2
    [*] --> Draft
    Draft --> InTransit: Ship
    InTransit --> Arrived: Arrive
    Arrived --> UnderReview: ImportExcel
    UnderReview --> LandingCostReviewed: CalculateLandingCost
    LandingCostReviewed --> Approved: ApproveContainer
    Approved --> InWarehouse: TransferToStock
    InWarehouse --> Closed: Close
    Closed --> Archived: Archive
    Draft --> Cancelled: Cancel
    UnderReview --> Draft: RejectImport
    Cancelled --> [*]
    Archived --> [*]
```

---

## 5. Warehouse Detailing State Machine

```mermaid
stateDiagram-v2
    [*] --> Pending
    Pending --> InProgress: StartDetailing
    InProgress --> Completed: AllLengthsValid
    InProgress --> Rejected: Reject
    Rejected --> InProgress: Reopen
    Completed --> [*]
```

---

## 6. Accounting Posting Flow

```mermaid
sequenceDiagram
    participant SI as SalesInvoice
    participant SVC as SalesService
    participant INV as InventoryService
    participant ACC as AccountingService
    participant AUD as AuditLog

    SI->>SVC: ApproveInvoice
    SVC->>INV: DeductStock(onApproval)
    SVC->>ACC: CreateDraftJournalEntry
    ACC->>ACC: ValidateBalanced
    ACC->>ACC: Post
    ACC->>AUD: JournalEntryPosted
    SVC->>AUD: SalesInvoiceApproved
```

---

## 7. Future PostgreSQL Schema Groups

```mermaid
flowchart TB
    subgraph schemas [PostgreSQL Schemas]
        identity
        company
        parties
        china_import
        inventory
        sales
        purchasing
        finance
        accounting
        documents
        settings
        audit
        hr
    end

    china_import --> inventory
    inventory --> sales
    sales --> finance
    sales --> accounting
    purchasing --> accounting
    finance --> accounting
    parties --> sales
    parties --> finance
    identity --> audit
```

---

## 8. Entity Relationship — Sales Detailing (Critical Path)

```mermaid
erDiagram
    SalesInvoice ||--o{ SalesInvoiceItem : contains
    SalesInvoiceItem ||--o{ SalesInvoiceRollDetail : has
    SalesInvoice }o--|| Customer : billedTo
    SalesInvoice }o--|| Warehouse : shipsFrom
    SalesInvoice }o--|| ChinaContainer : sourcedFrom
    SalesInvoiceRollDetail }o--o| FabricRoll : references
    ChinaContainer ||--o{ ChinaContainerItem : contains
    FabricRoll }o--|| ChinaContainer : origin
    FabricRoll }o--|| Warehouse : storedIn
    SalesInvoice ||--o| JournalEntry : postsTo
    SalesInvoice ||--o| DeliveryNote : deliversVia
```

---

## 9. Landing Cost Calculation Data Flow

```mermaid
flowchart TD
    A[China Invoice Total Length] --> B[Customs Amount Paid]
    A --> C[Container Weight kg]
    B --> D["CustomsCostPerMeter = Customs / TotalLength"]
    C --> E["AvgGramPerMeter = WeightGrams / TotalLength"]
    F[Shipping + Clearance + Other] --> G["ExpenseCostPerMeter = TotalExpenses / TotalLength"]
    D --> H[LandingCost per Container]
    E --> H
    G --> H
    H --> I[Inventory Unit Cost Basis]
```

---

## 10. Soft Delete / Immutability Pattern

```mermaid
flowchart LR
    Draft[Draft Document] --> Posted[Posted Document]
    Posted --> Reversed[Reversal Entry]
    Posted --> Archived[IsArchived=true]
    Draft --> Cancelled[CancelledAt + CancelReason]
    Posted -.->|never hard delete| X[Hard Delete Forbidden]
```
