import { apiRequest } from './client.ts';
import type {
  CostCenterDto,
  CreateExpenseRequest,
  ExpenseCategoryDto,
  ExpenseCategoryKind,
  ExpenseDashboardDto,
  ExpenseDetailsDto,
  ExpenseListDto,
  ExpenseStatus,
  PagedResult,
  PayExpenseRequest
} from './types.ts';

export type ExpenseListParams = {
  search?: string;
  status?: ExpenseStatus;
  categoryKind?: ExpenseCategoryKind;
  page: number;
  pageSize: number;
};

export function getExpenses(params: ExpenseListParams) {
  const searchParams = new URLSearchParams({
    page: String(params.page),
    pageSize: String(params.pageSize)
  });
  if (params.search) {
    searchParams.set('search', params.search);
  }
  if (params.status !== undefined) {
    searchParams.set('status', String(params.status));
  }
  if (params.categoryKind !== undefined) {
    searchParams.set('categoryKind', String(params.categoryKind));
  }
  return apiRequest<PagedResult<ExpenseListDto>>(`/api/v1/expenses?${searchParams.toString()}`);
}

export function getExpense(expenseId: string) {
  return apiRequest<ExpenseDetailsDto>(`/api/v1/expenses/${expenseId}`);
}

export function getExpenseDashboard() {
  return apiRequest<ExpenseDashboardDto>('/api/v1/expenses/dashboard/summary');
}

export function getExpenseCategories() {
  return apiRequest<ExpenseCategoryDto[]>('/api/v1/expenses/categories');
}

export function getExpenseCostCenters() {
  return apiRequest<CostCenterDto[]>('/api/v1/expenses/cost-centers');
}

export function createExpense(request: CreateExpenseRequest) {
  return apiRequest<string>('/api/v1/expenses', {
    method: 'POST',
    body: request
  });
}

export function updateExpense(expenseId: string, request: CreateExpenseRequest) {
  return apiRequest<void>(`/api/v1/expenses/${expenseId}`, {
    method: 'PUT',
    body: request
  });
}

export function approveExpense(expenseId: string, reason?: string) {
  return apiRequest<void>(`/api/v1/expenses/${expenseId}/approve`, {
    method: 'POST',
    body: { reason: reason ?? null }
  });
}

export function rejectExpense(expenseId: string, reason?: string) {
  return apiRequest<void>(`/api/v1/expenses/${expenseId}/reject`, {
    method: 'POST',
    body: { reason: reason ?? null }
  });
}

export function payExpense(expenseId: string, request: PayExpenseRequest) {
  return apiRequest<void>(`/api/v1/expenses/${expenseId}/pay`, {
    method: 'POST',
    body: request
  });
}

export function deleteExpense(expenseId: string) {
  return apiRequest<void>(`/api/v1/expenses/${expenseId}`, {
    method: 'DELETE'
  });
}
