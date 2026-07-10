import { apiRequest } from './client.ts';
import type { CreateReceiptVoucherRequest } from './types.ts';

export type PaymentMethodDto = {
  id: string;
  code: string;
  name: string;
  kind: number;
  requiresCashbox: boolean;
  requiresBankAccount: boolean;
  requiresReference: boolean;
};

export type BankAccountListDto = {
  id: string;
  code: string;
  name: string;
  bankName: string;
  glAccountId: string;
  currency: string;
  isActive: boolean;
};

export function getPaymentMethods() {
  return apiRequest<PaymentMethodDto[]>('/api/v1/finance/payment-methods');
}

export function getBankAccounts() {
  return apiRequest<BankAccountListDto[]>('/api/v1/finance/bank-accounts');
}

export function createReceiptVoucher(request: CreateReceiptVoucherRequest) {
  return apiRequest<string>('/api/v1/finance/receipts', {
    method: 'POST',
    body: request
  });
}

export function approveReceiptVoucher(id: string) {
  return apiRequest<void>(`/api/v1/finance/receipts/${id}/approve`, {
    method: 'POST'
  });
}

export function postReceiptVoucher(id: string, idempotencyKey?: string) {
  return apiRequest<void>(`/api/v1/finance/receipts/${id}/post`, {
    method: 'POST',
    body: idempotencyKey ? { idempotencyKey } : undefined,
    headers: idempotencyKey ? { 'X-Idempotency-Key': idempotencyKey } : undefined
  });
}

export function reverseReceiptVoucher(id: string, reason: string) {
  return apiRequest<void>(`/api/v1/finance/receipts/${id}/reverse`, {
    method: 'POST',
    body: { reason }
  });
}
