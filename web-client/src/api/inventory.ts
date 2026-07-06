import { apiRequest } from './client.ts';
import type { FabricStockBalanceDto } from './types.ts';

export function getFabricStock(warehouseId?: string) {
  const search = new URLSearchParams();
  if (warehouseId) {
    search.set('warehouseId', warehouseId);
  }

  const suffix = search.size > 0 ? `?${search.toString()}` : '';
  return apiRequest<FabricStockBalanceDto[]>(`/api/v1/inventory/stock${suffix}`);
}
