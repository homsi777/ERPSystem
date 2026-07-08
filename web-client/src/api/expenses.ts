import { apiRequest } from './client.ts';
import type {
  CostCenterDto,
  CreateExpenseRequest,
  ExpenseCategoryDto,
  ExpenseCategoryKind,
  ExpenseDashboardDto,
  ExpenseDetailsDto,
  ExpenseEntryListDto,
  ExpenseListDto,
  ExpenseOperationsCenterDto,
  ExpenseReportDto,
  ExpenseStatus,
  ExpenseAuditEntryDto,
  ExpenseTimelineEventDto,
  PagedResult,
  PayExpenseRequest
} from './types.ts';

export type ExpenseListParams = {
  search?: string;
  status?: ExpenseStatus;
  categoryKind?: ExpenseCategoryKind;
  from?: string;
  to?: string;
  includeArchived?: boolean;
  page: number;
  pageSize: number;
};

export type ExpenseEntryListParams = {
  search?: string;
  expenseId?: string;
  cashboxId?: string;
  from?: string;
  to?: string;
  page: number;
  pageSize: number;
};

export type ExpenseReportParams = {
  reportType: string;
  from?: string;
  to?: string;
  categoryKind?: ExpenseCategoryKind;
  costCenterId?: string;
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
  if (params.from) {
    searchParams.set('from', params.from);
  }
  if (params.to) {
    searchParams.set('to', params.to);
  }
  if (params.includeArchived) {
    searchParams.set('includeArchived', 'true');
  }
  return apiRequest<PagedResult<ExpenseListDto>>(`/api/v1/expenses?${searchParams.toString()}`);
}

export function getExpenseEntries(params: ExpenseEntryListParams) {
  const searchParams = new URLSearchParams({
    page: String(params.page),
    pageSize: String(params.pageSize)
  });
  if (params.search) {
    searchParams.set('search', params.search);
  }
  if (params.expenseId) {
    searchParams.set('expenseId', params.expenseId);
  }
  if (params.cashboxId) {
    searchParams.set('cashboxId', params.cashboxId);
  }
  if (params.from) {
    searchParams.set('from', params.from);
  }
  if (params.to) {
    searchParams.set('to', params.to);
  }
  return apiRequest<PagedResult<ExpenseEntryListDto>>(`/api/v1/expenses/entries?${searchParams.toString()}`);
}

export function getExpense(expenseId: string) {
  return apiRequest<ExpenseDetailsDto>(`/api/v1/expenses/${expenseId}`);
}

export function getExpenseOperationsCenter(expenseId: string) {
  return apiRequest<ExpenseOperationsCenterDto>(`/api/v1/expenses/${expenseId}/operations-center`);
}

export function getExpenseAuditTrail(expenseId: string) {
  return apiRequest<ExpenseAuditEntryDto[]>(`/api/v1/expenses/${expenseId}/audit`);
}

export function getExpenseTimeline(expenseId: string) {
  return apiRequest<ExpenseTimelineEventDto[]>(`/api/v1/expenses/${expenseId}/timeline`);
}

export function getExpenseDashboard() {
  return apiRequest<ExpenseDashboardDto>('/api/v1/expenses/dashboard/summary');
}

export function getExpenseReport(params: ExpenseReportParams) {
  const searchParams = new URLSearchParams({ reportType: params.reportType });
  if (params.from) {
    searchParams.set('from', params.from);
  }
  if (params.to) {
    searchParams.set('to', params.to);
  }
  if (params.categoryKind !== undefined) {
    searchParams.set('categoryKind', String(params.categoryKind));
  }
  if (params.costCenterId) {
    searchParams.set('costCenterId', params.costCenterId);
  }
  return apiRequest<ExpenseReportDto>(`/api/v1/expenses/reports?${searchParams.toString()}`);
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

export function duplicateExpense(expenseId: string) {
  return apiRequest<string>(`/api/v1/expenses/${expenseId}/duplicate`, {
    method: 'POST',
    body: {}
  });
}

export function closeExpense(expenseId: string, reason?: string) {
  return apiRequest<void>(`/api/v1/expenses/${expenseId}/close`, {
    method: 'POST',
    body: { reason: reason ?? null }
  });
}

export function cancelExpense(expenseId: string, reason?: string) {
  return apiRequest<void>(`/api/v1/expenses/${expenseId}/cancel`, {
    method: 'POST',
    body: { reason: reason ?? null }
  });
}

export function archiveExpense(expenseId: string, reason?: string) {
  return apiRequest<void>(`/api/v1/expenses/${expenseId}/archive`, {
    method: 'POST',
    body: { reason: reason ?? null }
  });
}

export function deleteExpense(expenseId: string) {
  return apiRequest<void>(`/api/v1/expenses/${expenseId}`, {
    method: 'DELETE'
  });
}
