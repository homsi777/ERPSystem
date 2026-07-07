export type ApiErrorResponse = {
  code: string;
  message: string;
  validationErrors: ValidationError[];
};

export type ValidationError = {
  field: string;
  message: string;
};

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
  cashboxId: string;
  amount: number;
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
  fabricDisplayName: string;
  fabricCode: string;
  colorDisplayName: string;
  lengthMeters: number;
  hasValidLength: boolean;
};

export type WarehouseDetailingDto = {
  invoiceId: string;
  invoiceNumber: string;
  customerName: string;
  chinaContainerId: string;
  sentToWarehouseAt: string | null;
  representativeUnitPrice: number | null;
  status: WarehouseDetailingStatus;
  rolls: WarehouseDetailingRollDto[];
};

export type RollLengthEntryRequest = {
  rollDetailId: string;
  lengthMeters: number;
};

export type CompleteWarehouseDetailingRequest = {
  rollEntries: RollLengthEntryRequest[];
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
