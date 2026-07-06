const numberFormatter = new Intl.NumberFormat('ar-SY', {
  maximumFractionDigits: 2
});

const currencyFormatter = new Intl.NumberFormat('ar-SY', {
  style: 'currency',
  currency: 'USD',
  maximumFractionDigits: 2
});

const dateFormatter = new Intl.DateTimeFormat('ar-SY', {
  dateStyle: 'medium',
  timeStyle: 'short'
});

const dateOnlyFormatter = new Intl.DateTimeFormat('ar-SY', {
  dateStyle: 'medium'
});

export function formatNumber(value: number) {
  return numberFormatter.format(value);
}

export function formatMeters(value: number) {
  return `${numberFormatter.format(value)} م`;
}

export function formatCurrency(value: number) {
  return currencyFormatter.format(value);
}

export function formatDate(value: string | null) {
  if (!value) {
    return 'غير محدد';
  }
  return dateFormatter.format(new Date(value));
}

export function formatDateOnly(value: string | null) {
  if (!value) {
    return 'غير محدد';
  }
  return dateOnlyFormatter.format(new Date(value));
}
