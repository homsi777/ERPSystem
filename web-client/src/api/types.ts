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
