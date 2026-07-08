import { apiRequest } from './client.ts';
import type {
  AccountLedgerLineDto,
  AccountListDto,
  GlAccountType,
  JournalEntryDetailsDto,
  JournalEntryListDto,
  JournalEntryStatus,
  PagedResult,
  TrialBalanceLineDto
} from './types.ts';

export type JournalEntryListParams = {
  search?: string;
  status?: JournalEntryStatus;
  from?: string;
  to?: string;
  page: number;
  pageSize: number;
};

export function getJournalEntries(params: JournalEntryListParams) {
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
  if (params.from) {
    searchParams.set('from', params.from);
  }
  if (params.to) {
    searchParams.set('to', params.to);
  }
  return apiRequest<PagedResult<JournalEntryListDto>>(`/api/v1/accounting/journal-entries?${searchParams.toString()}`);
}

export function getJournalEntry(entryId: string) {
  return apiRequest<JournalEntryDetailsDto>(`/api/v1/accounting/journal-entries/${entryId}`);
}

export type AccountListParams = {
  search?: string;
  accountType?: GlAccountType;
};

export function getAccounts(params: AccountListParams = {}) {
  const searchParams = new URLSearchParams();
  if (params.search) {
    searchParams.set('search', params.search);
  }
  if (params.accountType !== undefined) {
    searchParams.set('accountType', String(params.accountType));
  }
  const suffix = searchParams.size > 0 ? `?${searchParams.toString()}` : '';
  return apiRequest<AccountListDto[]>(`/api/v1/accounting/accounts${suffix}`);
}

export function getAccountLedger(accountId: string, from?: string, to?: string) {
  const searchParams = new URLSearchParams();
  if (from) {
    searchParams.set('from', from);
  }
  if (to) {
    searchParams.set('to', to);
  }
  const suffix = searchParams.size > 0 ? `?${searchParams.toString()}` : '';
  return apiRequest<AccountLedgerLineDto[]>(`/api/v1/accounting/accounts/${accountId}/ledger${suffix}`);
}

export function getTrialBalance(asOfDate?: string) {
  const searchParams = new URLSearchParams();
  if (asOfDate) {
    searchParams.set('asOfDate', asOfDate);
  }
  const suffix = searchParams.size > 0 ? `?${searchParams.toString()}` : '';
  return apiRequest<TrialBalanceLineDto[]>(`/api/v1/accounting/reports/trial-balance${suffix}`);
}
