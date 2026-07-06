/**
 * Arabic UI formatting with Latin digits (0-9) via Unicode numbering system extension.
 * All screens must use these helpers instead of ad-hoc Intl / toLocaleString calls.
 */
const DISPLAY_LOCALE = 'ar-SY-u-nu-latn';

const numberFormatter = new Intl.NumberFormat(DISPLAY_LOCALE, {
  maximumFractionDigits: 2
});

const currencyFormatter = new Intl.NumberFormat(DISPLAY_LOCALE, {
  style: 'currency',
  currency: 'USD',
  maximumFractionDigits: 2
});

const dateFormatter = new Intl.DateTimeFormat(DISPLAY_LOCALE, {
  dateStyle: 'medium',
  timeStyle: 'short'
});

const dateOnlyFormatter = new Intl.DateTimeFormat(DISPLAY_LOCALE, {
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
