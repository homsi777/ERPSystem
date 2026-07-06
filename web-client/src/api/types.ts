export type ApiErrorResponse = {
  code: string;
  message: string;
  validationErrors: ValidationError[];
};

export type ValidationError = {
  field: string;
  message: string;
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
