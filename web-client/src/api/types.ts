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
