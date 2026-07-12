import { apiBlobRequest, apiRequest } from './client.ts';
import type {
  CreateCustomerRequest,
  CustomerAccountLedgerDto,
  CustomerDetailsDto,
  CustomerListDto,
  CustomerOpeningBalanceResultDto,
  CustomerSalesDetailDto,
  CustomerStatementDto,
  PagedResult,
  PostCustomerOpeningBalanceRequest,
  ReconcileCustomerAccountRequest,
  UpdateCustomerRequest
} from './types.ts';

export type CustomerListParams = {
  search?: string;
  page: number;
  pageSize: number;
};

export function getCustomers(params: CustomerListParams) {
  const searchParams = new URLSearchParams({
    page: String(params.page),
    pageSize: String(params.pageSize)
  });

  if (params.search) {
    searchParams.set('search', params.search);
  }

  return apiRequest<PagedResult<CustomerListDto>>(`/api/v1/customers?${searchParams.toString()}`);
}

export function getCustomerDetails(id: string) {
  return apiRequest<CustomerDetailsDto>(`/api/v1/customers/${id}`);
}

export type CustomerDateRangeParams = {
  from?: string;
  to?: string;
};

export function getCustomerSalesDetails(id: string, params: CustomerDateRangeParams = {}) {
  const searchParams = new URLSearchParams();
  if (params.from) {
    searchParams.set('from', params.from);
  }
  if (params.to) {
    searchParams.set('to', params.to);
  }
  const suffix = searchParams.size > 0 ? `?${searchParams.toString()}` : '';
  return apiRequest<CustomerSalesDetailDto[]>(`/api/v1/customers/${id}/sales-details${suffix}`);
}

export function getCustomerStatement(id: string, params: CustomerDateRangeParams = {}) {
  const searchParams = new URLSearchParams();
  if (params.from) {
    searchParams.set('from', params.from);
  }
  if (params.to) {
    searchParams.set('to', params.to);
  }
  const suffix = searchParams.size > 0 ? `?${searchParams.toString()}` : '';
  return apiRequest<CustomerStatementDto>(`/api/v1/customers/${id}/statement${suffix}`);
}

export function getCustomerAccountLedger(id: string, params: CustomerDateRangeParams = {}) {
  const searchParams = new URLSearchParams();
  if (params.from) {
    searchParams.set('from', params.from);
  }
  if (params.to) {
    searchParams.set('to', params.to);
  }
  const suffix = searchParams.size > 0 ? `?${searchParams.toString()}` : '';
  return apiRequest<CustomerAccountLedgerDto>(`/api/v1/customers/${id}/ledger${suffix}`);
}

export function getCustomerAccountLedgerPdf(id: string, params: CustomerDateRangeParams = {}) {
  const searchParams = new URLSearchParams();
  if (params.from) {
    searchParams.set('from', params.from);
  }
  if (params.to) {
    searchParams.set('to', params.to);
  }
  const suffix = searchParams.size > 0 ? `?${searchParams.toString()}` : '';
  return apiBlobRequest(`/api/v1/customers/${id}/ledger/pdf${suffix}`, {
    headers: { Accept: 'application/pdf' }
  });
}

export function createCustomer(request: CreateCustomerRequest) {
  return apiRequest<string>('/api/v1/customers', {
    method: 'POST',
    body: request
  });
}

export function updateCustomer(id: string, request: UpdateCustomerRequest) {
  return apiRequest<void>(`/api/v1/customers/${id}`, {
    method: 'PUT',
    body: request
  });
}

export function deactivateCustomer(id: string) {
  return apiRequest<void>(`/api/v1/customers/${id}/deactivate`, {
    method: 'POST'
  });
}

export function postCustomerOpeningBalance(id: string, request: PostCustomerOpeningBalanceRequest) {
  return apiRequest<CustomerOpeningBalanceResultDto>(`/api/v1/customers/${id}/opening-balance`, {
    method: 'POST',
    body: request
  });
}

export function reconcileCustomerAccount(id: string, request: ReconcileCustomerAccountRequest) {
  return apiRequest<void>(`/api/v1/customers/${id}/reconcile`, {
    method: 'POST',
    body: request
  });
}
