export const inventoryStatusLabels = {
  available: 'متاح',
  low: 'منخفض'
} as const;

export const movementTypeLabels: Record<string, string> = {
  Import: 'استيراد',
  Purchase: 'شراء',
  Sale: 'بيع',
  SaleReturn: 'مرتجع بيع',
  PurchaseReturn: 'مرتجع شراء',
  Transfer: 'تحويل',
  OpeningBalance: 'رصيد افتتاحي',
  Adjustment: 'تسوية',
  Stocktake: 'جرد',
  Manufacturing: 'تصنيع',
  Consumption: 'استهلاك',
  Production: 'إنتاج',
  Damage: 'تلف',
  Loss: 'فقد',
  Correction: 'تصحيح',
  Waste: 'هدر'
};

export const stockMovementStatusLabels: Record<string, string> = {
  Draft: 'مسودة',
  Posted: 'مرحّلة',
  Cancelled: 'ملغاة',
  Reversed: 'معكوسة'
};

export const documentTypeNameLabels: Record<string, string> = {
  SalesInvoice: 'فاتورة بيع',
  SalesReturn: 'مرتجع بيع',
  PurchaseInvoice: 'فاتورة شراء',
  PurchaseReturn: 'مرتجع شراء',
  ReceiptVoucher: 'سند قبض',
  PaymentVoucher: 'سند صرف',
  JournalEntry: 'قيد يومية',
  StockMovement: 'حركة مخزون',
  DeliveryNote: 'إشعار تسليم',
  ChinaContainer: 'حاوية صين',
  ExpensePayment: 'دفعة مصروف',
  SupplierOpeningBalance: 'رصيد افتتاحي مورد',
  OpeningBalance: 'رصيد افتتاحي',
  StockTransfer: 'تحويل مخزون',
  Stocktake: 'جرد',
  PurchaseInvoiceReversal: 'عكس فاتورة شراء',
  CustomerOpeningBalance: 'رصيد افتتاحي عميل',
  CashboxTransfer: 'تحويل صندوق',
  FinanceOpeningBalance: 'رصيد افتتاحي مالي'
};

export function movementTypeLabel(value: string) {
  return movementTypeLabels[value] ?? value;
}

export function stockMovementStatusLabel(value: string) {
  return stockMovementStatusLabels[value] ?? value;
}

export function documentTypeName(value: string | null | undefined) {
  if (!value) {
    return '—';
  }
  return documentTypeNameLabels[value] ?? value;
}

export const chinaContainerStatusLabels = {
  0: 'مسودة',
  1: 'قيد الشحن',
  2: 'وصلت',
  3: 'قيد المراجعة',
  4: 'روجعت التكلفة',
  5: 'معتمدة',
  6: 'في المستودع',
  7: 'مغلقة',
  8: 'مؤرشفة',
  9: 'ملغاة'
} as const;

export const landingCostStatusLabels = {
  0: 'مسودة',
  1: 'مراجعة',
  2: 'معتمدة'
} as const;

export const chinaContainerStatusOptions = [
  { value: 0, label: chinaContainerStatusLabels[0] },
  { value: 1, label: chinaContainerStatusLabels[1] },
  { value: 2, label: chinaContainerStatusLabels[2] },
  { value: 3, label: chinaContainerStatusLabels[3] },
  { value: 4, label: chinaContainerStatusLabels[4] },
  { value: 5, label: chinaContainerStatusLabels[5] },
  { value: 6, label: chinaContainerStatusLabels[6] },
  { value: 7, label: chinaContainerStatusLabels[7] },
  { value: 8, label: chinaContainerStatusLabels[8] },
  { value: 9, label: chinaContainerStatusLabels[9] }
] as const;

type ContainerStatusValue = keyof typeof chinaContainerStatusLabels;

export function getChinaContainerStatusTone(status: ContainerStatusValue) {
  if (status === 6) {
    return 'green';
  }
  if (status === 4 || status === 5) {
    return 'blue';
  }
  if (status === 1 || status === 2 || status === 3) {
    return 'amber';
  }
  if (status === 9) {
    return 'red';
  }
  return 'gray';
}

export const customerTypeLabels = {
  0: 'نقدي',
  1: 'آجل'
} as const;

export const customerStatusLabels = {
  0: 'نشط',
  1: 'موقوف',
  2: 'محظور'
} as const;

type CustomerStatusValue = keyof typeof customerStatusLabels;

export function getCustomerStatusTone(status: CustomerStatusValue) {
  if (status === 0) {
    return 'green';
  }
  if (status === 1) {
    return 'amber';
  }
  return 'red';
}

export const warehouseDetailingStatusLabels = {
  0: 'بانتظار التفصيل',
  1: 'قيد التفصيل',
  2: 'تم التفصيل',
  3: 'مرفوض'
} as const;

type WarehouseDetailingStatusValue = keyof typeof warehouseDetailingStatusLabels;

export function getWarehouseDetailingStatusTone(status: WarehouseDetailingStatusValue) {
  if (status === 2) {
    return 'green';
  }
  if (status === 1) {
    return 'blue';
  }
  if (status === 3) {
    return 'red';
  }
  return 'amber';
}

export const customerAccountMovementTypeLabels = {
  0: 'فاتورة بيع',
  1: 'مرتجع بيع',
  2: 'سند قبض'
} as const;

type CustomerAccountMovementTypeValue = keyof typeof customerAccountMovementTypeLabels;

export function getCustomerAccountMovementTypeTone(type: CustomerAccountMovementTypeValue) {
  if (type === 0) {
    return 'blue';
  }
  if (type === 1) {
    return 'amber';
  }
  return 'green';
}

// ── Sales invoice status ─────────────────────────────────────────
export const salesInvoiceStatusLabels = {
  0: 'مسودة',
  1: 'بانتظار التفصيل',
  2: 'تم التفصيل',
  3: 'جاهزة للاعتماد',
  4: 'معتمدة',
  5: 'مطبوعة',
  6: 'مُسلّمة',
  7: 'ملغاة',
  8: 'مرتجعة جزئيًا',
  9: 'مرتجعة'
} as const;

type SalesInvoiceStatusValue = keyof typeof salesInvoiceStatusLabels;

export function salesInvoiceStatusLabel(status: number) {
  return salesInvoiceStatusLabels[status as SalesInvoiceStatusValue] ?? String(status);
}

export function getSalesInvoiceStatusTone(status: number) {
  if (status === 4 || status === 5 || status === 6) {
    return 'green';
  }
  if (status === 2 || status === 3) {
    return 'blue';
  }
  if (status === 0 || status === 1) {
    return 'amber';
  }
  if (status === 7) {
    return 'red';
  }
  return 'gray';
}

export const salesInvoiceStatusOptions = [
  { value: 0, label: salesInvoiceStatusLabels[0] },
  { value: 1, label: salesInvoiceStatusLabels[1] },
  { value: 2, label: salesInvoiceStatusLabels[2] },
  { value: 3, label: salesInvoiceStatusLabels[3] },
  { value: 4, label: salesInvoiceStatusLabels[4] },
  { value: 6, label: salesInvoiceStatusLabels[6] },
  { value: 7, label: salesInvoiceStatusLabels[7] }
] as const;

export const paymentTypeLabels = {
  0: 'نقدي',
  1: 'آجل'
} as const;

export function paymentTypeLabel(value: number) {
  return paymentTypeLabels[value as keyof typeof paymentTypeLabels] ?? String(value);
}

// ── Expenses ─────────────────────────────────────────────────────
export const expenseStatusLabels = {
  0: 'مسودة',
  1: 'بانتظار الاعتماد',
  2: 'معتمد',
  3: 'مجدول',
  4: 'مدفوع جزئيًا',
  5: 'مدفوع',
  6: 'مغلق',
  7: 'ملغى',
  8: 'مؤرشف'
} as const;

type ExpenseStatusValue = keyof typeof expenseStatusLabels;

export function expenseStatusLabel(status: number) {
  return expenseStatusLabels[status as ExpenseStatusValue] ?? String(status);
}

export function getExpenseStatusTone(status: number) {
  if (status === 5) {
    return 'green';
  }
  if (status === 2 || status === 3 || status === 4) {
    return 'blue';
  }
  if (status === 0 || status === 1) {
    return 'amber';
  }
  if (status === 7) {
    return 'red';
  }
  return 'gray';
}

export const expenseStatusOptions = [
  { value: 0, label: expenseStatusLabels[0] },
  { value: 1, label: expenseStatusLabels[1] },
  { value: 2, label: expenseStatusLabels[2] },
  { value: 4, label: expenseStatusLabels[4] },
  { value: 5, label: expenseStatusLabels[5] },
  { value: 7, label: expenseStatusLabels[7] }
] as const;

export const expenseCategoryKindLabels = {
  1: 'رأسمالي',
  2: 'شخصي',
  3: 'تشغيلي'
} as const;

export function expenseCategoryKindLabel(value: number) {
  return expenseCategoryKindLabels[value as keyof typeof expenseCategoryKindLabels] ?? String(value);
}

export const expensePaymentMethodLabels = {
  1: 'نقدًا',
  2: 'حوالة بنكية',
  3: 'بطاقة',
  4: 'شيك',
  5: 'أخرى'
} as const;

export function expensePaymentMethodLabel(value: number) {
  return expensePaymentMethodLabels[value as keyof typeof expensePaymentMethodLabels] ?? String(value);
}

export const expensePaymentMethodOptions = [
  { value: 1, label: expensePaymentMethodLabels[1] },
  { value: 2, label: expensePaymentMethodLabels[2] },
  { value: 3, label: expensePaymentMethodLabels[3] },
  { value: 4, label: expensePaymentMethodLabels[4] },
  { value: 5, label: expensePaymentMethodLabels[5] }
] as const;

export const expenseFundingSourceLabels = {
  0: 'نقدية',
  1: 'بنك',
  2: 'خزينة',
  3: 'شريك',
  4: 'قرض',
  5: 'تسهيلات ائتمانية',
  6: 'تحويل داخلي'
} as const;

export const expenseFundingSourceOptions = [
  { value: 0, label: expenseFundingSourceLabels[0] },
  { value: 1, label: expenseFundingSourceLabels[1] },
  { value: 2, label: expenseFundingSourceLabels[2] }
] as const;

// ── Accounting ───────────────────────────────────────────────────
export const journalEntryStatusLabels = {
  0: 'مسودة',
  1: 'معتمد',
  2: 'مُرحّل',
  3: 'معكوس',
  4: 'ملغى'
} as const;

export function journalEntryStatusLabel(status: number) {
  return journalEntryStatusLabels[status as keyof typeof journalEntryStatusLabels] ?? String(status);
}

export function getJournalEntryStatusTone(status: number) {
  if (status === 2) {
    return 'green';
  }
  if (status === 1) {
    return 'blue';
  }
  if (status === 0) {
    return 'amber';
  }
  return 'red';
}

export const journalEntryStatusOptions = [
  { value: 0, label: journalEntryStatusLabels[0] },
  { value: 1, label: journalEntryStatusLabels[1] },
  { value: 2, label: journalEntryStatusLabels[2] },
  { value: 3, label: journalEntryStatusLabels[3] }
] as const;

export const glAccountTypeLabels = {
  1: 'أصول',
  2: 'خصوم',
  3: 'حقوق ملكية',
  4: 'إيرادات',
  5: 'مصروفات'
} as const;

export function glAccountTypeLabel(value: number) {
  return glAccountTypeLabels[value as keyof typeof glAccountTypeLabels] ?? String(value);
}

export const glAccountTypeOptions = [
  { value: 1, label: glAccountTypeLabels[1] },
  { value: 2, label: glAccountTypeLabels[2] },
  { value: 3, label: glAccountTypeLabels[3] },
  { value: 4, label: glAccountTypeLabels[4] },
  { value: 5, label: glAccountTypeLabels[5] }
] as const;

export const documentTypeLabels = {
  0: 'فاتورة بيع',
  1: 'مرتجع بيع',
  2: 'فاتورة شراء',
  3: 'مرتجع شراء',
  4: 'سند قبض',
  5: 'سند صرف',
  6: 'قيد يومية',
  7: 'حركة مخزون',
  8: 'إشعار تسليم',
  9: 'حاوية صين',
  10: 'دفعة مصروف',
  11: 'رصيد افتتاحي مورد',
  12: 'رصيد افتتاحي',
  13: 'تحويل مخزون',
  14: 'جرد',
  15: 'عكس فاتورة شراء',
  16: 'رصيد افتتاحي عميل',
  17: 'تحويل صندوق',
  18: 'رصيد افتتاحي مالي'
} as const;
