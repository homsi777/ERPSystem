export type ApiErrorResponse = {
  code: string;
  message: string;
  validationErrors: ValidationError[];
};

export type ValidationError = {
  field: string;
  message: string;
};

/** DPL roll length unit: 0 = meters, 1 = yards */
export type DplQuantityUnit = 0 | 1;

export type LookupItemDto = {
  id: string;
  name: string;
};

export type AuthenticatedUserDto = {
  userId: string;
  username: string;
  fullNameAr: string;
  roles: string[];
  permissions: string[];
};

export type LoginRequest = {
  username: string;
  password: string;
};

export type AuthTokenResponse = {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string;
  refreshTokenExpiresAt: string;
  user: AuthenticatedUserDto;
};

export type RefreshTokenRequest = {
  refreshToken: string;
};

export type RefreshTokenResponse = {
  accessToken: string;
  accessTokenExpiresAt: string;
};

export type MeResponse = AuthenticatedUserDto;

export type FabricStockBalanceDto = {
  warehouseId: string;
  warehouseName: string;
  fabricItemId: string;
  fabricCode: string;
  fabricName: string;
  fabricColorId: string;
  colorName: string;
  containerId: string;
  containerNumber: string;
  rollCount: number;
  totalMeters: number;
  reservedMeters: number;
  availableMeters: number;
  inventoryValue: number;
};

export type FabricSearchProfileDto = {
  fabricItemId: string;
  fabricCode: string;
  fabricName: string;
  categoryName: string;
  totalRolls: number;
  totalMeters: number;
  availableMeters: number;
  reservedMeters: number;
  inventoryValue: number;
  avgCostPerMeter: number | null;
  avgSalePricePerMeter: number | null;
  minSalePricePerMeter: number | null;
  maxSalePricePerMeter: number | null;
  warehouseCount: number;
  containerCount: number;
  colorCount: number;
  colors: FabricSearchColorBreakdownDto[];
  locations: FabricSearchLocationDetailDto[];
  containerJourney: FabricSearchContainerLegDto[];
  journeyTimeline: FabricSearchJourneyEventDto[];
};

export type FabricSearchColorBreakdownDto = {
  fabricColorId: string;
  colorName: string;
  rollCount: number;
  totalMeters: number;
  availableMeters: number;
  reservedMeters: number;
  inventoryValue: number;
  avgSalePricePerMeter: number | null;
  avgCostPerMeter: number | null;
  containerCount: number;
};

export type FabricSearchLocationDetailDto = {
  warehouseId: string;
  warehouseName: string;
  containerId: string;
  containerNumber: string;
  fabricColorId: string;
  colorName: string;
  rollCount: number;
  totalMeters: number;
  availableMeters: number;
  reservedMeters: number;
  inventoryValue: number;
  avgCostPerMeter: number | null;
  avgSalePricePerMeter: number | null;
};

export type FabricSearchContainerLegDto = {
  containerId: string;
  containerNumber: string;
  statusLabel: string;
  supplierName: string | null;
  shipmentDate: string | null;
  arrivalDate: string | null;
  approvedAt: string | null;
  rollCount: number;
  totalMeters: number;
  landedCostPerMeter: number | null;
  salePricePerMeter: number | null;
  warehouses: string[];
};

export type FabricSearchJourneyEventDto = {
  occurredAt: string;
  title: string;
  description: string;
  category: string;
};

export type PagedResult<T> = {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
};

export type ChinaContainerStatus = 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9;

export type LandingCostStatus = 0 | 1 | 2;

export type ContainerListDto = {
  id: string;
  containerNumber: string;
  status: ChinaContainerStatus;
  shipmentDate: string;
  expectedArrival: string | null;
  totalRolls: number;
  totalMeters: number;
  totalWeightKg: number | null;
  codeCount: number;
  colorCount: number;
  exchangeRateToLocalCurrency: number;
  supplierName: string;
};

export type ContainerOperationsCenterDto = {
  container: ContainerDetailsDto;
  inventory: ContainerInventoryMetricsDto | null;
  canApprove: boolean;
  canSetSalePrices: boolean;
  canMoveToWarehouse: boolean;
  moveToWarehouseBlockReason?: string | null;
  canCalculateLandingCost: boolean;
  isReadyForSale: boolean;
};

export type ContainerDetailsDto = {
  id: string;
  containerNumber: string;
  status: ChinaContainerStatus;
  supplierId: string;
  supplierName: string;
  shipmentDate: string;
  arrivalDate: string | null;
  totalRolls: number;
  totalMeters: number;
  dplQuantityUnit?: DplQuantityUnit | null;
  lengthUnitDisplay?: string;
  totalWeightKg: number | null;
  exchangeRateToLocalCurrency: number;
  chinaInvoiceAmountUsd: number;
  financialTaxReserveUsd: number;
  financialTaxReservePostedLocal: number | null;
  landingCost: LandingCostDto | null;
  fabricTypeLines: ContainerFabricTypeLineDto[];
  items: ContainerItemDto[];
};

export type LandingCostDto = {
  totalLengthMeters: number;
  containerWeightKg: number;
  customsAmount: number;
  shipping: number;
  insurance: number;
  clearance: number;
  otherExpenses: number;
  otherExpense1: number;
  otherExpense2: number;
  otherExpense3: number;
  otherExpense4: number;
  usesWeightedAllocation: boolean;
  totalImportExpenses: number;
  customsCostPerMeter: number;
  expenseCostPerMeter: number;
  avgGramPerMeter: number;
  status: LandingCostStatus;
};

export type ContainerFabricTypeLineDto = {
  id: string;
  lineNumber: number;
  typeDisplayName: string;
  fabricItemId: string | null;
  fabricColorId: string | null;
  lengthMeters: number;
  rollCount: number;
  netWeightKg: number;
  chinaUnitPriceUsd: number;
  expenseShareUsd: number;
  landedCostPerMeterUsd: number;
  marginPerMeterUsd: number;
  salePricePerMeterUsd: number;
  hasSalePrice: boolean;
};

export type ContainerItemDto = {
  lineNumber: number;
  fabricItemId: string;
  fabricColorId: string;
  rollCount: number;
  lengthMeters: number;
  isValid: boolean;
};

export type ContainerInventoryMetricsDto = {
  totalRolls: number;
  totalMeters: number;
  reservedMeters: number;
  soldMeters: number;
  availableMeters: number;
  costPerMeter: number;
  inventoryValuation: number;
  isStockPosted: boolean;
};

export type CreateChinaContainerRequest = {
  supplierId: string;
  containerNumber: string;
  shipmentDate: string;
  expectedArrival: string | null;
  notes: string | null;
  exchangeRateToLocalCurrency: number;
  chinaInvoiceAmountUsd: number;
  importFileName: string | null;
  lines: ImportContainerLineCommand[];
};

export type ImportContainerLineCommand = {
  lineNumber: number;
  fabricItemId: string;
  fabricColorId: string;
  rollCount: number;
  lengthMeters: number;
  weightKg: number | null;
  lotCode: string | null;
  buyerCustomerId: string | null;
};

export type CalculateLandingCostRequest = {
  totalLengthMeters: number;
  containerWeightKg: number;
  customsClearanceAmount: number;
  shipping: number;
  insurance: number;
  otherExpense1: number;
  otherExpense2: number;
  otherExpense3: number;
  otherExpense4: number;
  usesWeightedAllocation: boolean;
  typeLines: ContainerFabricTypeLineCommand[];
  customsAmount: number;
  clearance: number;
  otherExpenses: number;
};

export type ContainerFabricTypeLineCommand = {
  lineNumber: number;
  typeDisplayName: string;
  matchKey: string;
  fabricItemId: string | null;
  fabricColorId: string | null;
  lengthMeters: number;
  rollCount: number;
  netWeightKg: number;
  cbm: number;
  chinaUnitPriceUsd: number;
  invoiceLineAmountUsd: number;
  hasInvoiceMatch: boolean;
  hasPlMatch: boolean;
  hasDplMatch: boolean;
  matchWarnings: string | null;
};

export type SetContainerSalePricesRequest = {
  lines: ContainerTypeSalePriceCommand[];
};

export type ContainerTypeSalePriceCommand = {
  typeLineId: string;
  marginPerMeterUsd: number;
};

export type MoveContainerToWarehouseRequest = {
  warehouseId: string;
};

export type PackingListGrandTotalDto = {
  declaredTotalMeters: number | null;
  declaredTotalRolls: number | null;
  parsedTotalMeters: number;
  parsedTotalRolls: number;
  metersMatch: boolean;
  rollsMatch: boolean;
  matchIndicator: string;
  summaryText: string;
};

export type PackingListRollDto = {
  sequenceNumber: number;
  groupIndex: number;
  rollNumber: number;
  quantityMeters: number;
  lotCode: string;
  isValid: boolean;
  invalidReason: string | null;
};

export type PackingListResolutionIssueDto = {
  groupIndex: number;
  fabricCode: string;
  color: string;
  rollNumber: number | null;
  reason: string;
};

export type PackingListGroupDto = {
  groupIndex: number;
  fabricCode: string;
  color: string;
  declaredTotalMeters: number;
  declaredTotalRolls: number;
  parsedTotalMeters: number;
  parsedTotalRolls: number;
  metersMatch: boolean;
  rollsMatch: boolean;
  metersMatchIndicator: string;
  rollsMatchIndicator: string;
  fabricResolved: boolean;
  colorResolved: boolean;
  fabricItemId: string | null;
  fabricColorId: string | null;
  resolutionError: string | null;
  rolls: PackingListRollDto[];
  resolutionIssues: PackingListResolutionIssueDto[];
};

export type ContainerExcelParseResultDto = {
  fileName: string;
  supplierNameFromFile: string | null;
  grandTotal: PackingListGrandTotalDto;
  groups: PackingListGroupDto[];
  hasUnresolvedGroups: boolean;
};

export type ChinaInvoiceLineDto = {
  lineIndex: number;
  description: string;
  matchKey: string;
  lengthMeters: number;
  rollCount: number;
  unitPriceUsd: number;
  lineAmountUsd: number;
};

export type ChinaInvoiceParseResultDto = {
  fileName: string;
  lines: ChinaInvoiceLineDto[];
  seaFreightUsd: number;
  insuranceUsd: number;
  grandTotalUsd: number;
  declaredTotalMeters: number;
  declaredTotalRolls: number;
  lineAmountsMatchTotal: boolean;
  totalValidationWarning: string | null;
};

export type ChinaPackingSummaryLineDto = {
  lineIndex: number;
  description: string;
  matchKey: string;
  rollCount: number;
  lengthMeters: number;
  cbm: number;
  grossWeightKg: number;
  netWeightKg: number;
};

export type ChinaPackingSummaryParseResultDto = {
  fileName: string;
  lines: ChinaPackingSummaryLineDto[];
  declaredTotalMeters: number;
  declaredTotalRolls: number;
  totalCbm: number;
  totalGrossWeightKg: number;
  totalNetWeightKg: number;
};

export type CustomerType = 0 | 1;

export type CustomerStatus = 0 | 1 | 2;

export type DocumentType = 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | 12 | 13 | 14 | 15 | 16 | 17 | 18;

export type CustomerListDto = {
  id: string;
  code: string;
  nameAr: string;
  nameEn: string;
  type: CustomerType;
  status: CustomerStatus;
  balance: number;
  creditLimit: number;
  creditLimitEnabled: boolean;
  isActive: boolean;
  openingBalancePosted: boolean;
  openingBalanceAmount: number;
  pendingOpeningBalanceAmount: number;
  totalInvoiced: number;
  totalReceipts: number;
  postedReceiptCount: number;
  openInvoicesCount: number;
  computedBalance: number;
  lastReceiptDate?: string | null;
};

export type CustomerDetailsDto = {
  id: string;
  code: string;
  nameAr: string;
  nameEn: string;
  type: CustomerType;
  status: CustomerStatus;
  balance: number;
  creditLimit: number;
  creditLimitEnabled: boolean;
  paymentTermsDays: number;
  phone: string | null;
  email: string | null;
  isActive: boolean;
  openingBalancePosted: boolean;
  openingBalanceAmount: number;
  pendingOpeningBalanceAmount: number;
  totalInvoiced: number;
  totalReceipts: number;
  postedReceiptCount: number;
  openInvoicesCount: number;
  computedBalance: number;
  lastReceiptDate?: string | null;
};

export type CustomerStatementLineDto = {
  entryDate: string;
  documentType: DocumentType;
  documentNumber: string;
  debit: number;
  credit: number;
  runningBalance: number;
};

export type CustomerStatementDto = {
  customerId: string;
  customerName: string;
  openingBalance: number;
  closingBalance: number;
  lines: CustomerStatementLineDto[];
};

export type CustomerSalesDetailDto = {
  saleDate: string;
  fabricName: string;
  fabricCode: string;
  colorName: string;
  unitPrice: number;
};

export type CustomerAccountMovementType = 0 | 1 | 2;

export type CustomerAccountLedgerLineDto = {
  movementType: CustomerAccountMovementType;
  documentId: string;
  entryId: string;
  documentNumber: string;
  transactionDate: string;
  fabricDescription: string;
  rollCount: number | null;
  totalMeters: number | null;
  lengthUnit?: string | null;
  lengthUnitDisplay?: string | null;
  totalLengthDisplay?: string | null;
  unitPrice: number | null;
  lineAmount: number;
  notes: string | null;
  runningBalance: number;
};

export type CustomerAccountLedgerDto = {
  customerId: string;
  customerName: string;
  openingBalance: number;
  closingBalance: number;
  lastReconciliationDate: string | null;
  lastReconciliationBalance: number | null;
  lastReconciliationDocumentId: string | null;
  lines: CustomerAccountLedgerLineDto[];
};

export type CustomerOpeningBalanceResultDto = {
  journalEntryNumber: string;
  postedDate: string;
  amount: number;
};

export type CreateCustomerRequest = {
  code: string;
  nameAr: string;
  nameEn: string;
  type: CustomerType;
  creditLimit: number;
  creditLimitEnabled: boolean;
};

export type UpdateCustomerRequest = {
  nameAr: string;
  nameEn: string;
  creditLimit: number;
  creditLimitEnabled: boolean;
  paymentTermsDays: number;
};

export type PostCustomerOpeningBalanceRequest = {
  amount: number;
  postingDate: string;
  referenceNote: string | null;
};

export type ReconcileCustomerAccountRequest = {
  reconciliationDate: string;
  documentId: string;
  balanceAtReconciliation: number;
};

export type ReceiptAllocationRequest = {
  salesInvoiceId: string;
  amount: number;
};

export type CreateReceiptVoucherRequest = {
  customerId: string;
  amount: number;
  cashboxId?: string;
  paymentMethodId?: string;
  bankAccountId?: string;
  reference?: string;
  currency?: string;
  exchangeRate?: number;
  allocations: ReceiptAllocationRequest[];
};

export type WarehouseListExtendedDto = {
  id: string;
  code: string;
  nameAr: string;
  nameEn: string | null;
  city: string;
  manager: string | null;
  isDefault: boolean;
  isActive: boolean;
  rollCount: number;
  totalMeters: number;
  inventoryValue: number;
};

export type FabricRollListDto = {
  id: string;
  rollNumber: number;
  barcode: string | null;
  fabricName: string;
  colorName: string;
  lengthMeters: number;
  remainingLengthMeters: number;
  costPerMeter: number;
  currentValue: number;
  status: string;
  batchNumber: string | null;
  locationCode: string | null;
  lotCode: string | null;
};

export type FabricRollSalesReservationDto = {
  fabricRollId: string;
  salesInvoiceId: string;
  salesInvoiceNumber: string;
  salesInvoiceStatus: number;
};

export type PaginatedFabricRollDto = {
  items: FabricRollListDto[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
};

export type InventoryAlertDto = {
  id: string;
  alertType: string;
  severity: string;
  title: string;
  message: string;
  warehouseName: string | null;
  createdAt: string;
  isAcknowledged: boolean;
};

export type InventoryDashboardDto = {
  totalInventoryValue: number;
  warehouseCount: number;
  totalRolls: number;
  totalMeters: number;
  reservedMeters: number;
  lowStockCount: number;
  pendingTransfers: number;
  pendingStocktakes: number;
  activeAlerts: number;
  topFabrics: FabricStockBalanceDto[];
  recentAlerts: InventoryAlertDto[];
};

export type StockMovementListDto = {
  id: string;
  movementNumber: string;
  movementDate: string;
  type: string;
  warehouseName: string;
  reference: string | null;
  totalMeters: number;
  totalValue: number;
  status: string;
};

export type DashboardActivityDto = {
  occurredAt: string;
  entityType: string;
  entityId: string;
  description: string;
};

export type WarehouseDetailingStatus = 0 | 1 | 2 | 3;

export type WarehouseDetailingRollDto = {
  rollDetailId: string;
  salesInvoiceItemId: string;
  rollSequence: number;
  fabricItemId: string;
  fabricColorId: string;
  fabricDisplayName: string;
  fabricCode: string;
  colorDisplayName: string;
  lengthMeters: number;
  hasValidLength: boolean;
  /** The invoice LINE's own container — may differ from the header container on multi-container invoices. */
  chinaContainerId: string;
  containerDisplay: string;
  /** Previously-saved partial progress (not yet resolved/finalized). */
  draftRollNumber: number | null;
  draftLengthMeters: number | null;
};

export type WarehouseDetailingDto = {
  invoiceId: string;
  invoiceNumber: string;
  customerName: string;
  warehouseId: string;
  chinaContainerId: string;
  sentToWarehouseAt: string | null;
  representativeUnitPrice: number | null;
  status: WarehouseDetailingStatus;
  rolls: WarehouseDetailingRollDto[];
};

export type RollLengthEntryRequest = {
  rollDetailId: string;
  /** DPL / inventory roll serial (رقم التوب). Prefer this for reliable matching. */
  rollNumber?: number | null;
  /** Manual length in meters when serial is not used (or for partial sale with serial). */
  lengthMeters: number;
};

export type CompleteWarehouseDetailingRequest = {
  rollEntries: RollLengthEntryRequest[];
};

export type RollDraftEntryRequest = {
  rollDetailId: string;
  rollNumber?: number | null;
  lengthMeters?: number | null;
};

export type SaveWarehouseDetailingDraftRequest = {
  rollEntries: RollDraftEntryRequest[];
};

export type DetailingCandidateRollDto = {
  fabricRollId: string;
  rollNumber: number;
  remainingLengthMeters: number;
  status: string;
  reservedInSalesInvoiceId: string | null;
  reservedInSalesInvoiceNumber: string | null;
  reservedInSalesInvoiceStatus: number | null;
};

export type DashboardSummaryDto = {
  pendingContainersCount: number;
  awaitingDetailingCount: number;
  readyForApprovalInvoicesCount: number;
  openReceiptsCount: number;
  totalCustomerOutstanding: number;
  totalSupplierPayables: number;
  activeCustomersCount: number;
  todaySalesTotal: number;
  lowStockItemsCount: number;
  recentActivity: DashboardActivityDto[];
};

// ── Sales invoices ────────────────────────────────────────────────
export type SalesInvoiceStatus = 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9;
export type PaymentType = 0 | 1;
export type TaxPriceMode = 0 | 1;
export type TaxCategory = 0 | 1 | 2;

export type TaxCodeDto = {
  id: string;
  code: string;
  name: string;
  rate: number;
  priceMode: TaxPriceMode;
  category: TaxCategory;
  effectiveFrom: string;
  effectiveTo: string | null;
  isInclusive: boolean;
};

export type SalesInvoiceTaxPreviewLineDto = {
  lineId: string;
  lineNumber: number;
  taxCodeId: string | null;
  taxCode: string | null;
  taxName: string | null;
  taxRate: number;
  taxCategory: TaxCategory | null;
  isInclusive: boolean;
  lineDiscountTotal: number;
  taxableAmount: number;
  taxAmount: number;
  lineGrandTotal: number;
};

export type SalesTaxSummaryLineDto = {
  taxCodeId: string | null;
  taxCode: string | null;
  taxName: string | null;
  taxRate: number;
  taxableAmount: number;
  taxAmount: number;
};

export type SalesInvoiceTaxPreviewDto = {
  subtotalBeforeDiscount: number;
  lineDiscountTotal: number;
  invoiceDiscountTotal: number;
  taxableAmount: number;
  taxTotal: number;
  grandTotal: number;
  roundingDifference: number;
  lines: SalesInvoiceTaxPreviewLineDto[];
  taxSummary: SalesTaxSummaryLineDto[];
  validationErrors: string[];
};

export type CalculateSalesInvoiceTaxLineRequest = {
  lineNumber: number;
  lineId?: string | null;
  netLineAmount: number;
  lineDiscountTotal: number;
  taxCodeId?: string | null;
};

export type CalculateSalesInvoiceTaxRequest = {
  invoiceDate: string;
  invoiceDiscountTotal: number;
  lines: CalculateSalesInvoiceTaxLineRequest[];
};

export type SalesTaxReportRowDto = {
  invoiceNumber: string;
  invoiceDate: string;
  customerName: string;
  taxCode: string | null;
  taxRate: number;
  taxableAmount: number;
  taxAmount: number;
  isLegacyUntaxed: boolean;
  journalEntryNumber: string | null;
  postingStatus: string;
};

export type SalesTaxReportSummaryDto = {
  taxCode: string | null;
  taxableAmount: number;
  taxAmount: number;
};

export type SalesTaxReportDto = {
  rows: SalesTaxReportRowDto[];
  summaryByTaxCode: SalesTaxReportSummaryDto[];
};

export type SalesInvoiceLineDto = {
  id: string;
  lineNumber: number;
  chinaContainerId: string;
  fabricItemId: string;
  fabricColorId: string;
  fabricDisplayName: string;
  fabricCode: string;
  colorDisplayName: string;
  rollCount: number;
  unitPrice: number;
  originalUnitPrice: number;
  unit?: string | null;
  lengthUnitDisplay?: string | null;
  totalLengthMeters: number;
  totalLengthDisplay?: string | null;
  lineTotal: number;
  discountAmount: number;
  discountReason: string | null;
  taxCodeId: string | null;
  taxCode: string | null;
  taxName: string | null;
  taxRate: number;
  taxCategory: TaxCategory | null;
  isTaxInclusive: boolean;
  taxableAmount: number;
  taxAmount: number;
  notes: string | null;
};

export type SalesInvoiceDto = {
  id: string;
  invoiceNumber: string;
  status: SalesInvoiceStatus;
  customerId: string;
  customerName: string;
  warehouseId: string;
  chinaContainerId: string;
  invoiceDate: string;
  paymentType: PaymentType;
  partialPaymentAmount?: number | null;
  subTotal: number;
  discountTotal: number;
  taxTotal: number;
  grandTotal: number;
  sentToWarehouseAt: string | null;
  detailedAt: string | null;
  approvedAt: string | null;
  printedAt: string | null;
  deliveredAt: string | null;
  cancelledAt: string | null;
  deliveredToName: string | null;
  deliveryDriverName: string | null;
  deliveryNotes: string | null;
  cancelReason: string | null;
  lines: SalesInvoiceLineDto[];
};

export type SalesJournalEntryDto = {
  id: string;
  entryNumber: string;
  entryDate: string;
  description: string;
  status: JournalEntryStatus;
  debitTotal: number;
  creditTotal: number;
};

export type ReceiptInvoicePaymentDto = {
  salesInvoiceId: string;
  receiptVoucherId: string;
  receiptNumber: string;
  amount: number;
  appliedAt: string;
};

export type SalesInvoiceOperationsCenterDto = {
  invoice: SalesInvoiceDto;
  detailing: WarehouseDetailingDto | null;
  canSendToWarehouse: boolean;
  canCompleteDetailing: boolean;
  canApprove: boolean;
  canCancel: boolean;
  journalEntries: SalesJournalEntryDto[];
  payments: ReceiptInvoicePaymentDto[];
  collectedAmount: number;
  remainingBalance: number;
  customerBalance: number;
  warehouseName: string | null;
  customerPhone: string | null;
};

export type SalesWarehouseStockOptionDto = {
  fabricItemId: string;
  fabricColorId: string;
  fabricDisplayName: string;
  fabricCode: string;
  colorDisplayName: string;
  availableRollCount: number;
  availableMeters: number;
  salePricePerMeter: number | null;
  dplQuantityUnit?: number | null;
  lengthUnitDisplay?: string | null;
  display: string;
};

export type CreateSalesInvoiceLineRequest = {
  lineNumber: number;
  chinaContainerId: string;
  fabricItemId: string;
  fabricColorId: string;
  rollCount: number;
  unitPrice: number;
  originalUnitPrice: number;
  unit?: string | null;
  discountReason: string | null;
  notes: string | null;
  taxCodeId?: string | null;
};

export type CreateSalesInvoiceRequest = {
  customerId: string;
  warehouseId: string;
  chinaContainerId: string;
  paymentType: PaymentType;
  discountAmount: number;
  partialPaymentAmount?: number | null;
  invoiceNumber: string | null;
  lines: CreateSalesInvoiceLineRequest[];
};

// ── Expenses ──────────────────────────────────────────────────────
export type ExpenseStatus = 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8;
export type ExpenseCategoryKind = 1 | 2 | 3;
export type ExpensePaymentMethod = 1 | 2 | 3 | 4 | 5;
export type ExpenseFundingSource = 0 | 1 | 2 | 3 | 4 | 5 | 6;

export type ExpenseListDto = {
  id: string;
  code: string;
  name: string;
  categoryKind: ExpenseCategoryKind;
  categoryKindDisplay: string;
  categoryName: string;
  status: ExpenseStatus;
  statusDisplay: string;
  startDate: string;
  endDate: string | null;
  originalCurrency: string;
  originalAmount: number;
  baseAmount: number;
  paidAmountBase: number;
  remainingBalanceBase: number;
  baseCurrency: string;
  department: string | null;
  costCenterId: string | null;
  costCenterName: string | null;
  payeeName: string | null;
  isRecurring: boolean;
  nextDueDate: string | null;
  isArchived: boolean;
};

export type ExpensePaymentDto = {
  id: string;
  paymentDate: string;
  dueDate: string | null;
  amountOriginal: number;
  amountBase: number;
  currency: string;
  exchangeRateSnapshot: number;
  paymentMethodDisplay: string;
  fundingSourceDisplay: string;
  statusDisplay: string;
  approvalStatusDisplay: string;
  referenceNumber: string | null;
  notes: string | null;
  installmentNumber: number | null;
};

export type ExpenseDetailsDto = {
  id: string;
  code: string;
  name: string;
  categoryId: string;
  categoryKind: ExpenseCategoryKind;
  categoryKindDisplay: string;
  categoryName: string;
  description: string | null;
  status: ExpenseStatus;
  statusDisplay: string;
  allowedTransitions: ExpenseStatus[];
  startDate: string;
  endDate: string | null;
  originalCurrency: string;
  originalAmount: number;
  exchangeRate: number;
  baseCurrency: string;
  baseAmount: number;
  paidAmountBase: number;
  remainingBalanceBase: number;
  paymentMethod: ExpensePaymentMethod;
  paymentMethodDisplay: string;
  payeeName: string | null;
  supplierId: string | null;
  costCenterId: string | null;
  costCenterName: string | null;
  department: string | null;
  projectCode: string | null;
  notes: string | null;
  isRecurring: boolean;
  isArchived: boolean;
  createdAt: string;
  createdByName: string | null;
  updatedAt: string | null;
  payments: ExpensePaymentDto[];
  installments?: ExpenseInstallmentDto[];
};

export type ExpenseCategoryDto = {
  id: string;
  kind: ExpenseCategoryKind;
  code: string;
  nameAr: string;
  nameEn: string;
  kindDisplay: string;
};

export type CostCenterDto = {
  id: string;
  code: string;
  name: string;
  description: string | null;
  parentCostCenterId: string | null;
  status: number;
  statusDisplay: string;
};

export type ExpenseEntryListDto = {
  id: string;
  expenseId: string;
  expenseCode: string;
  expenseName: string;
  paymentDate: string;
  amountOriginal: number;
  amountBase: number;
  currency: string;
  description: string | null;
  cashboxId: string | null;
  cashboxName: string | null;
};

export type ExpenseInstallmentDto = {
  id: string;
  installmentNumber: number;
  dueDate: string;
  amountOriginal: number;
  amountBase: number;
  currency: string;
  statusDisplay: string;
  paymentId: string | null;
};

export type ExpenseAuditEntryDto = {
  action: string;
  fieldName: string | null;
  previousValue: string | null;
  newValue: string | null;
  userName: string;
  timestamp: string;
  reason: string | null;
};

export type ExpenseTimelineEventDto = {
  eventType: string;
  title: string;
  description: string | null;
  previousValue: string | null;
  newValue: string | null;
  userName: string;
  timestamp: string;
  reason: string | null;
};

export type ExpenseFinancialSummaryDto = {
  originalAmount: number;
  originalCurrency: string;
  baseAmount: number;
  baseCurrency: string;
  paidAmountBase: number;
  remainingBalanceBase: number;
  exchangeRate: number;
  completedPayments: number;
  scheduledPayments: number;
  pendingInstallments: number;
  nextPaymentDue: string | null;
};

export type ExpenseStatisticsDto = {
  totalPayments: number;
  totalAttachments: number;
  daysSinceCreated: number;
  auditEventCount: number;
};

export type ExpenseLifecycleStepDto = {
  label: string;
  completed: boolean;
  current: boolean;
};

export type ExpenseOperationsCenterDto = {
  details: ExpenseDetailsDto;
  financial: ExpenseFinancialSummaryDto;
  lifecycleSteps: ExpenseLifecycleStepDto[];
  timeline: ExpenseTimelineEventDto[];
  recentAudit: ExpenseAuditEntryDto[];
  statistics: ExpenseStatisticsDto;
};

export type ExpenseReportRowDto = {
  expenseId: string;
  code: string;
  name: string;
  category: string;
  categoryKindDisplay: string;
  status: string;
  startDate: string;
  endDate: string | null;
  originalAmount: number;
  currency: string;
  exchangeRate: number;
  baseAmount: number;
  paidAmountBase: number;
  remainingBalanceBase: number;
  department: string | null;
  costCenter: string | null;
  payeeName: string | null;
  fundingSource: string | null;
  paymentMethod: string;
  description: string | null;
  notes: string | null;
  isRecurring: boolean;
  nextDueDate: string | null;
  paymentCount: number;
};

export type ExpenseReportDto = {
  title: string;
  reportType: string;
  generatedAt: string;
  rows: ExpenseReportRowDto[];
  totalBase: number;
  totalPaidBase: number;
  totalRemainingBase: number;
  expenseCount: number;
  baseCurrency: string;
  fromDate: string | null;
  toDate: string | null;
  scopeLabel: string | null;
};

export type ExpenseMonthlyTrendDto = {
  label: string;
  amountBase: number;
};

export type ExpenseCurrencyBreakdownDto = {
  currency: string;
  amountOriginal: number;
  amountBase: number;
  exposurePercentage: number;
};

export type ExpenseCategoryBreakdownDto = {
  label: string;
  amountBase: number;
  percentage: number;
  growthPercentage: number;
};

export type ExpenseDashboardDto = {
  totalExpensesBase: number;
  monthlyExpensesBase: number;
  yearlyExpensesBase: number;
  capitalExpensesBase: number;
  personalExpensesBase: number;
  operatingExpensesBase: number;
  activeCount: number;
  pendingApprovalCount: number;
  upcomingPaymentsCount: number;
  overdueCount: number;
  largestExpenseBase: number;
  largestExpenseName: string;
  burnRateMonthly: number;
  baseCurrency: string;
  categoryBreakdown: ExpenseCategoryBreakdownDto[];
  monthlyTrend?: ExpenseMonthlyTrendDto[];
  currencyBreakdown?: ExpenseCurrencyBreakdownDto[];
};

export type CreateExpenseRequest = {
  name: string;
  categoryId: string;
  description: string | null;
  startDate: string;
  endDate: string | null;
  originalCurrency: string;
  originalAmount: number;
  exchangeRate: number;
  baseCurrency: string;
  paymentMethod: ExpensePaymentMethod;
  payeeName: string | null;
  supplierId: string | null;
  costCenterId: string | null;
  department: string | null;
  notes: string | null;
  submitForApproval: boolean;
};

export type PayExpenseRequest = {
  paymentDate: string;
  amount: number;
  amountOriginal: number;
  amountBase: number;
  exchangeRateSnapshot: number;
  currency: string;
  paymentMethod: ExpensePaymentMethod;
  fundingSource: ExpenseFundingSource;
  referenceNumber: string | null;
  notes: string | null;
  cashboxId: string | null;
};

// ── Accounting ────────────────────────────────────────────────────
export type JournalEntryStatus = 0 | 1 | 2 | 3 | 4;
export type GlAccountType = 1 | 2 | 3 | 4 | 5;

export type JournalEntryListDto = {
  id: string;
  entryNumber: string;
  entryDate: string;
  description: string;
  status: JournalEntryStatus;
  statusDisplay: string;
  debitTotal: number;
  creditTotal: number;
  lineCount: number;
  sourceType: DocumentType | null;
  sourceTypeDisplay: string | null;
};

export type JournalEntryLineDetailsDto = {
  id: string;
  accountId: string;
  accountCode: string;
  accountName: string;
  debit: number;
  credit: number;
  narrative: string;
};

export type JournalEntryDetailsDto = {
  id: string;
  entryNumber: string;
  entryDate: string;
  description: string;
  status: JournalEntryStatus;
  statusDisplay: string;
  debitTotal: number;
  creditTotal: number;
  sourceType: DocumentType | null;
  sourceTypeDisplay: string | null;
  sourceId: string | null;
  postedAt: string | null;
  lines: JournalEntryLineDetailsDto[];
};

export type AccountListDto = {
  id: string;
  code: string;
  nameAr: string;
  nameEn: string;
  accountType: GlAccountType;
  accountTypeDisplay: string;
  parentId: string | null;
  parentName: string | null;
  isPostable: boolean;
  isActive: boolean;
  childCount: number;
  level: number;
};

export type TrialBalanceLineDto = {
  accountId: string;
  accountCode: string;
  accountName: string;
  accountTypeDisplay: string;
  debitTotal: number;
  creditTotal: number;
  balance: number;
};

export type AccountLedgerLineDto = {
  journalEntryId: string;
  entryNumber: string;
  entryDate: string;
  description: string;
  lineNarrative: string;
  debit: number;
  credit: number;
  runningBalance: number;
};
