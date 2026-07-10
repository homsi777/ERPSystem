import { apiRequest } from './client.ts';
import type {
  DetailingCandidateRollDto,
  FabricRollListDto,
  FabricRollSalesReservationDto,
  FabricStockBalanceDto,
  InventoryAlertDto,
  InventoryDashboardDto,
  PaginatedFabricRollDto,
  StockMovementListDto,
  WarehouseListExtendedDto
} from './types.ts';

export function getFabricStock(warehouseId?: string, searchTerm?: string) {
  const search = new URLSearchParams();
  if (warehouseId) {
    search.set('warehouseId', warehouseId);
  }
  if (searchTerm?.trim()) {
    search.set('search', searchTerm.trim());
  }

  const suffix = search.size > 0 ? `?${search.toString()}` : '';
  return apiRequest<FabricStockBalanceDto[]>(`/api/v1/inventory/stock${suffix}`);
}

export function getInventoryWarehouses() {
  return apiRequest<WarehouseListExtendedDto[]>('/api/v1/inventory/warehouses');
}

export function getWarehouseFabricRolls(
  warehouseId: string,
  pageNumber = 1,
  pageSize = 50,
  status?: number,
  search?: string
) {
  const params = new URLSearchParams({
    pageNumber: String(pageNumber),
    pageSize: String(pageSize)
  });
  if (status !== undefined) {
    params.set('status', String(status));
  }
  if (search?.trim()) {
    params.set('search', search.trim());
  }

  return apiRequest<PaginatedFabricRollDto>(
    `/api/v1/inventory/warehouses/${warehouseId}/rolls?${params.toString()}`
  );
}

export function getFabricRollsByStock(params: {
  warehouseId: string;
  containerId: string;
  fabricItemId: string;
  fabricColorId: string;
}) {
  const search = new URLSearchParams({
    warehouseId: params.warehouseId,
    containerId: params.containerId,
    fabricItemId: params.fabricItemId,
    fabricColorId: params.fabricColorId
  });
  return apiRequest<FabricRollListDto[]>(`/api/v1/inventory/rolls-by-stock?${search.toString()}`);
}

export function getFabricRollSalesReservations(rollIds: string[], excludeSalesInvoiceId?: string) {
  const search = new URLSearchParams();
  for (const rollId of rollIds) {
    search.append('rollIds', rollId);
  }
  if (excludeSalesInvoiceId) {
    search.set('excludeSalesInvoiceId', excludeSalesInvoiceId);
  }
  return apiRequest<FabricRollSalesReservationDto[]>(
    `/api/v1/inventory/roll-sales-reservations?${search.toString()}`
  );
}

export function getDetailingCandidateRolls(params: {
  warehouseId: string;
  containerId: string;
  fabricItemId: string;
  fabricColorId: string;
  excludeSalesInvoiceId?: string;
}) {
  const search = new URLSearchParams({
    warehouseId: params.warehouseId,
    containerId: params.containerId,
    fabricItemId: params.fabricItemId,
    fabricColorId: params.fabricColorId
  });
  if (params.excludeSalesInvoiceId) {
    search.set('excludeSalesInvoiceId', params.excludeSalesInvoiceId);
  }
  return apiRequest<DetailingCandidateRollDto[]>(
    `/api/v1/inventory/detailing-candidate-rolls?${search.toString()}`
  );
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
