/**
 * Arabic UI formatting with Latin digits (0-9) via Unicode numbering system extension.
 * All screens must use these helpers instead of ad-hoc Intl / toLocaleString calls.
 */
export const DISPLAY_LOCALE = 'ar-SY-u-nu-latn';
const LATIN_DIGITS = { numberingSystem: 'latn' } as const;

/** Empty table/detail placeholder — Unicode escape avoids source-file encoding corruption (mojibake). */
export const EMPTY_CELL = '\u2014';

const numberFormatter = new Intl.NumberFormat(DISPLAY_LOCALE, {
  ...LATIN_DIGITS,
  maximumFractionDigits: 2
});

const integerFormatter = new Intl.NumberFormat(DISPLAY_LOCALE, {
  ...LATIN_DIGITS,
  maximumFractionDigits: 0
});

const currencyFormatter = new Intl.NumberFormat(DISPLAY_LOCALE, {
  ...LATIN_DIGITS,
  style: 'currency',
  currency: 'USD',
  maximumFractionDigits: 2
});

const dateFormatter = new Intl.DateTimeFormat(DISPLAY_LOCALE, {
  ...LATIN_DIGITS,
  dateStyle: 'medium',
  timeStyle: 'short'
});

const dateOnlyFormatter = new Intl.DateTimeFormat(DISPLAY_LOCALE, {
  ...LATIN_DIGITS,
  dateStyle: 'medium'
});

export function formatNumber(value: number) {
  return numberFormatter.format(value);
}

export function formatInteger(value: number) {
  return integerFormatter.format(value);
}

export function formatPercent(value: number, maximumFractionDigits = 1) {
  return new Intl.NumberFormat(DISPLAY_LOCALE, {
    ...LATIN_DIGITS,
    style: 'percent',
    maximumFractionDigits
  }).format(value / 100);
}

export function formatLineIndex(value: number) {
  return `#${formatInteger(value)}`;
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
