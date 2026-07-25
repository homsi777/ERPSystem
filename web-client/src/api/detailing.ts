import { apiRequest } from './client.ts';
import type {
  CompleteWarehouseDetailingRequest,
  SaveWarehouseDetailingDraftRequest,
  WarehouseDetailingDto
} from './types.ts';

export function getDetailingQueue(warehouseId?: string) {
  const search = new URLSearchParams();
  if (warehouseId) {
    search.set('warehouseId', warehouseId);
  }
  const suffix = search.size > 0 ? `?${search.toString()}` : '';
  return apiRequest<WarehouseDetailingDto[]>(`/api/v1/detailing/queue${suffix}`);
}

export function getDetailing(invoiceId: string) {
  return apiRequest<WarehouseDetailingDto>(`/api/v1/detailing/${invoiceId}`);
}

export function completeDetailing(invoiceId: string, request: CompleteWarehouseDetailingRequest) {
  return apiRequest<void>(`/api/v1/detailing/${invoiceId}/complete`, {
    method: 'POST',
    body: request
  });
}

export function saveDetailingDraft(invoiceId: string, request: SaveWarehouseDetailingDraftRequest) {
  return apiRequest<void>(`/api/v1/detailing/${invoiceId}/save-draft`, {
    method: 'POST',
    body: request
  });
}
