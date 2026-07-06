import { apiRequest } from './client.ts';
import type { LookupItemDto } from './types.ts';

export function getSupplierLookups() {
  return apiRequest<LookupItemDto[]>('/api/v1/lookups/suppliers');
}

export function getWarehouseLookups() {
  return apiRequest<LookupItemDto[]>('/api/v1/lookups/warehouses');
}
