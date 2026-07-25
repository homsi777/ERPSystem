import assert from 'node:assert/strict';
import test from 'node:test';
import {
  buildUntaxedSalesInvoicePreview,
  requiresRemoteSalesTaxPreview
} from '../src/lib/salesInvoicePreview.ts';

function request(overrides = {}) {
  return {
    invoiceDate: '2026-07-25T00:00:00.000Z',
    invoiceDiscountTotal: 100,
    lines: [
      {
        lineNumber: 1,
        netLineAmount: 1511.81,
        lineDiscountTotal: 0,
        taxCodeId: null
      }
    ],
    ...overrides
  };
}

test('an invoice without a tax code is calculated locally and remains submittable', () => {
  const input = request();

  assert.equal(requiresRemoteSalesTaxPreview(input), false);
  const preview = buildUntaxedSalesInvoicePreview(input);
  assert.ok(preview);
  assert.equal(preview.subtotalBeforeDiscount, 1511.81);
  assert.equal(preview.taxableAmount, 0);
  assert.equal(preview.taxTotal, 0);
  assert.equal(preview.grandTotal, 1411.81);
  assert.deepEqual(preview.validationErrors, []);
});

test('a real tax code still requires the authoritative server preview', () => {
  const input = request({
    lines: [
      {
        lineNumber: 1,
        netLineAmount: 1511.81,
        lineDiscountTotal: 0,
        taxCodeId: 'c1000002-0002-0002-0002-000000000002'
      }
    ]
  });

  assert.equal(requiresRemoteSalesTaxPreview(input), true);
  assert.equal(buildUntaxedSalesInvoicePreview(input), undefined);
});

test('an excessive discount is rejected before draft creation', () => {
  const preview = buildUntaxedSalesInvoicePreview(
    request({ invoiceDiscountTotal: 2000 })
  );

  assert.ok(preview);
  assert.equal(preview.grandTotal, 0);
  assert.equal(preview.validationErrors.length, 1);
});
