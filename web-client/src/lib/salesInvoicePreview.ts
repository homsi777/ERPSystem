import type {
  CalculateSalesInvoiceTaxRequest,
  SalesInvoiceTaxPreviewDto
} from '../api/types.ts';

export function requiresRemoteSalesTaxPreview(
  request: CalculateSalesInvoiceTaxRequest | null
): boolean {
  return request?.lines.some((line) => Boolean(line.taxCodeId)) ?? false;
}

export function buildUntaxedSalesInvoicePreview(
  request: CalculateSalesInvoiceTaxRequest | null
): SalesInvoiceTaxPreviewDto | undefined {
  if (!request || requiresRemoteSalesTaxPreview(request)) {
    return undefined;
  }

  const subtotal = request.lines.reduce((sum, line) => sum + line.netLineAmount, 0);
  const invoiceDiscount = request.invoiceDiscountTotal;
  const validationErrors: string[] = [];
  if (invoiceDiscount < 0) {
    validationErrors.push('مبلغ الخصم لا يمكن أن يكون سالباً.');
  }
  if (invoiceDiscount > subtotal) {
    validationErrors.push('مبلغ الخصم لا يمكن أن يتجاوز إجمالي الفاتورة.');
  }

  return {
    subtotalBeforeDiscount: subtotal,
    lineDiscountTotal: 0,
    invoiceDiscountTotal: invoiceDiscount,
    taxableAmount: 0,
    taxTotal: 0,
    grandTotal: Math.max(0, subtotal - Math.max(0, invoiceDiscount)),
    roundingDifference: 0,
    lines: request.lines.map((line) => ({
      lineId: `draft-line-${line.lineNumber}`,
      lineNumber: line.lineNumber,
      taxCodeId: null,
      taxCode: null,
      taxName: null,
      taxRate: 0,
      taxCategory: null,
      isInclusive: false,
      lineDiscountTotal: line.lineDiscountTotal,
      taxableAmount: 0,
      taxAmount: 0,
      lineGrandTotal: line.netLineAmount
    })),
    taxSummary: [],
    validationErrors
  };
}
