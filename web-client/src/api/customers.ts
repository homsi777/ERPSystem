import { apiRequest } from './client.ts';
import type {
  CustomerDetailsDto,
  CustomerListDto,
  CustomerSalesDetailDto,
  CustomerStatementDto,
  PagedResult
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
