import { apiBlobRequest, apiRequest } from './client.ts';
import type {
  CalculateSalesInvoiceTaxRequest,
  CreateSalesInvoiceRequest,
  PagedResult,
  SalesInvoiceDto,
  SalesInvoiceOperationsCenterDto,
  SalesInvoiceStatus,
  SalesInvoiceTaxPreviewDto,
  SalesTaxReportDto,
  SalesWarehouseStockOptionDto,
  TaxCodeDto
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

export function getSalesInvoicePdf(invoiceId: string) {
  return apiBlobRequest(`/api/v1/sales/invoices/${invoiceId}/pdf`, {
    headers: { Accept: 'application/pdf' }
  });
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

export function getTaxCodes(effectiveOn?: string) {
  const searchParams = new URLSearchParams();
  if (effectiveOn) {
    searchParams.set('effectiveOn', effectiveOn);
  }
  const query = searchParams.toString();
  return apiRequest<TaxCodeDto[]>(`/api/v1/sales/tax-codes${query ? `?${query}` : ''}`);
}

export function calculateSalesInvoiceTax(request: CalculateSalesInvoiceTaxRequest) {
  return apiRequest<SalesInvoiceTaxPreviewDto>('/api/v1/sales/invoices/calculate', {
    method: 'POST',
    body: request
  });
}

export function getSalesTaxReport(from: string, to: string, includeLegacy = false) {
  const searchParams = new URLSearchParams({
    from,
    to,
    includeLegacy: String(includeLegacy)
  });
  return apiRequest<SalesTaxReportDto>(`/api/v1/sales/tax-report?${searchParams.toString()}`);
}
