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

/** DPL quantity unit from API: 0 = meters, 1 = yards */
export type DplQuantityUnit = 0 | 1;

export const YARDS_TO_METERS = 0.9144;

export function isYardsUnit(unit?: number | null) {
  return unit === 1;
}

export function displayLengthFromMeters(meters: number, unit?: number | null) {
  return isYardsUnit(unit) ? meters / YARDS_TO_METERS : meters;
}

export function displayRateFromPerMeter(perMeter: number, unit?: number | null) {
  return isYardsUnit(unit) ? perMeter * YARDS_TO_METERS : perMeter;
}

export function storedRateFromDisplay(displayRate: number, unit?: number | null) {
  return isYardsUnit(unit) ? displayRate / YARDS_TO_METERS : displayRate;
}

export function lengthAbbrev(unit?: number | null) {
  return isYardsUnit(unit) ? 'ي' : 'م';
}

export function lengthUnitArabic(unit?: number | null) {
  return isYardsUnit(unit) ? 'يارد' : 'متر';
}

export function totalLengthLabel(unit?: number | null) {
  return isYardsUnit(unit) ? 'إجمالي الياردات' : 'إجمالي الأمتار';
}

export function lengthColumnLabel(unit?: number | null) {
  return isYardsUnit(unit) ? 'يارد' : 'أمتار';
}

export function perUnitLabel(unit?: number | null, prefix = '') {
  return isYardsUnit(unit) ? `${prefix}/ي` : `${prefix}/م`;
}

export function formatContainerLength(meters: number, unit?: number | null) {
  return `${numberFormatter.format(displayLengthFromMeters(meters, unit))} ${lengthAbbrev(unit)}`;
}

export function formatRatePerUnit(perMeter: number, unit?: number | null) {
  return `${numberFormatter.format(displayRateFromPerMeter(perMeter, unit))} ${lengthAbbrev(unit)}`;
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
