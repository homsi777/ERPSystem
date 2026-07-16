import { apiRequest } from './client.ts';

export type PurchaseOperations = {
  invoice: {
    id: string; invoiceNumber: string; supplierId: string; supplierName: string;
    currencyCode: string; totalAmount: number; paidAmount: number; remainingAmount: number;
    status: number; statusDisplay: string; sourceContainerId: string | null; sourceContainerNumber: string | null;
  };
  payments: Array<{ voucherId: string; voucherNumber: string; voucherDate: string; amount: number; statusDisplay: string }>;
};

export type CreatePaymentVoucher = {
  supplierId: string; cashboxId: string | null; bankAccountId: string | null;
  paymentMethodId: string; purchaseInvoiceId: string; amount: number; currency: string; reference: string | null;
};

export const getPurchaseOperations = (id: string) =>
  apiRequest<PurchaseOperations>(`/api/v1/purchase-invoices/${id}`);
export const createPaymentVoucher = (body: CreatePaymentVoucher) =>
  apiRequest<string>('/api/v1/payment-vouchers', { method: 'POST', body });
export const approvePaymentVoucher = (id: string) =>
  apiRequest<void>(`/api/v1/payment-vouchers/${id}/approve`, { method: 'POST' });
export const postPaymentVoucher = (id: string, purchaseInvoiceId: string) =>
  apiRequest<void>(`/api/v1/payment-vouchers/${id}/post`, { method: 'POST', body: { purchaseInvoiceId } });
