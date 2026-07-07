import { apiRequest } from './client.ts';
import type { CreateReceiptVoucherRequest } from './types.ts';

export function createReceiptVoucher(request: CreateReceiptVoucherRequest) {
  return apiRequest<string>('/api/v1/receipts', {
    method: 'POST',
    body: request
  });
}

export function postReceiptVoucher(id: string) {
  return apiRequest<void>(`/api/v1/receipts/${id}/post`, {
    method: 'POST'
  });
}
