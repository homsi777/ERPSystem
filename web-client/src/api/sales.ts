import { apiRequest } from './client.ts';
import type {
  CreateSalesInvoiceRequest,
  PagedResult,
  SalesInvoiceDto,
  SalesInvoiceOperationsCenterDto,
  SalesInvoiceStatus,
  SalesWarehouseStockOptionDto
} from './types.ts';

export type SalesInvoiceListParams = {
  status?: SalesInvoiceStatus;
  customerId?: string;
  page: number;
  pageSize: number;
};

export function getSalesInvoices(params: SalesInvoiceListParams) {
  const searchParams = new URLSearchParams({
    page: String(params.page),
    pageSize: String(params.pageSize)
  });
  if (params.status !== undefined) {
    searchParams.set('status', String(params.status));
  }
  if (params.customerId) {
    searchParams.set('customerId', params.customerId);
  }
  return apiRequest<PagedResult<SalesInvoiceDto>>(`/api/v1/sales/invoices?${searchParams.toString()}`);
}

export function getSalesInvoice(invoiceId: string) {
  return apiRequest<SalesInvoiceOperationsCenterDto>(`/api/v1/sales/invoices/${invoiceId}`);
}

export function createSalesInvoice(request: CreateSalesInvoiceRequest) {
  return apiRequest<string>('/api/v1/sales/invoices', {
    method: 'POST',
    body: request
  });
}

export function sendSalesInvoiceToWarehouse(invoiceId: string) {
  return apiRequest<void>(`/api/v1/sales/invoices/${invoiceId}/send-to-warehouse`, {
    method: 'POST'
  });
}

export function approveSalesInvoice(invoiceId: string) {
  return apiRequest<void>(`/api/v1/sales/invoices/${invoiceId}/approve`, {
    method: 'POST'
  });
}

export function cancelSalesInvoice(invoiceId: string, reason: string) {
  return apiRequest<void>(`/api/v1/sales/invoices/${invoiceId}/cancel`, {
    method: 'POST',
    body: { reason }
  });
}

export function getSalesWarehouseStock(containerId: string, warehouseId: string) {
  const searchParams = new URLSearchParams({ containerId, warehouseId });
  return apiRequest<SalesWarehouseStockOptionDto[]>(`/api/v1/sales/warehouse-stock?${searchParams.toString()}`);
}
