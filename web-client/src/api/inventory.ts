import { apiRequest } from './client.ts';
import type {
  FabricStockBalanceDto,
  InventoryAlertDto,
  InventoryDashboardDto,
  StockMovementListDto,
  WarehouseListExtendedDto
} from './types.ts';

export function getFabricStock(warehouseId?: string) {
  const search = new URLSearchParams();
  if (warehouseId) {
    search.set('warehouseId', warehouseId);
  }

  const suffix = search.size > 0 ? `?${search.toString()}` : '';
  return apiRequest<FabricStockBalanceDto[]>(`/api/v1/inventory/stock${suffix}`);
}

export function getInventoryWarehouses() {
  return apiRequest<WarehouseListExtendedDto[]>('/api/v1/inventory/warehouses');
}

export function getInventoryDashboard() {
  return apiRequest<InventoryDashboardDto>('/api/v1/inventory/dashboard');
}

export function getInventoryMovements(warehouseId?: string) {
  const search = new URLSearchParams();
  if (warehouseId) {
    search.set('warehouseId', warehouseId);
  }

  const suffix = search.size > 0 ? `?${search.toString()}` : '';
  return apiRequest<StockMovementListDto[]>(`/api/v1/inventory/movements${suffix}`);
}

export function getInventoryAlerts(unacknowledgedOnly = true) {
  const search = new URLSearchParams({ unacknowledgedOnly: String(unacknowledgedOnly) });
  return apiRequest<InventoryAlertDto[]>(`/api/v1/inventory/alerts?${search.toString()}`);
}
