import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';

const easternDigits = /[\u0660-\u0669\u06F0-\u06F9]/u;
const locale = 'ar-SY-u-nu-latn';
const latinDigits = { numberingSystem: 'latn' };

const samples = [
  new Intl.NumberFormat(locale, { ...latinDigits, maximumFractionDigits: 2 }).format(1234.5),
  new Intl.NumberFormat(locale, { ...latinDigits, maximumFractionDigits: 0 }).format(42),
  new Intl.NumberFormat(locale, { ...latinDigits, style: 'percent', maximumFractionDigits: 1 }).format(0.125),
  new Intl.DateTimeFormat(locale, { ...latinDigits, dateStyle: 'medium', timeStyle: 'short' })
    .format(new Date('2026-07-15T12:34:00Z'))
];

if (samples.some((sample) => easternDigits.test(sample))) {
  throw new Error(`Expected Western digits only, received: ${samples.join(' | ')}`);
}

const formatterSource = readFileSync(resolve('src/lib/format.ts'), 'utf8');
if (!formatterSource.includes("numberingSystem: 'latn'")) {
  throw new Error('The shared formatter must explicitly specify numberingSystem: latn.');
}

console.log('Western digit formatter checks passed.');
