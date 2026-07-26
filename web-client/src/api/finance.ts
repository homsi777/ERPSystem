import { apiRequest } from './client.ts';
import type {
  CashboxListDto,
  CashboxTransferListDto,
  CreateCashboxRequest,
  CreateCashboxTransferRequest
} from './types.ts';

export function getCashboxes() {
  return apiRequest<CashboxListDto[]>('/api/v1/finance/cashboxes');
}

export function createCashbox(request: CreateCashboxRequest) {
  return apiRequest<string>('/api/v1/finance/cashboxes', {
    method: 'POST',
    body: request
  });
}

export function getCashboxTransfers() {
  return apiRequest<CashboxTransferListDto[]>('/api/v1/finance/cashbox-transfers');
}

export function createCashboxTransfer(request: CreateCashboxTransferRequest) {
  return apiRequest<string>('/api/v1/finance/cashbox-transfers', {
    method: 'POST',
    body: request
  });
}
