import { useEffect, useMemo, useState, type FormEvent } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useParams } from 'react-router-dom';
import { getCashboxLookups } from '../api/lookups.ts';
import { getBankAccounts, getPaymentMethods } from '../api/receipts.ts';
import { approvePaymentVoucher, createPaymentVoucher, getPurchaseOperations, postPaymentVoucher } from '../api/payments.ts';

export function PurchaseInvoicePage() {
  const { invoiceId = '' } = useParams();
  const queryClient = useQueryClient();
  const invoiceQuery = useQuery({ queryKey: ['purchase-invoice', invoiceId], queryFn: () => getPurchaseOperations(invoiceId), enabled: !!invoiceId });
  const methodsQuery = useQuery({ queryKey: ['finance', 'payment-methods'], queryFn: getPaymentMethods });
  const cashboxesQuery = useQuery({ queryKey: ['lookups', 'cashboxes'], queryFn: getCashboxLookups });
  const banksQuery = useQuery({ queryKey: ['finance', 'bank-accounts'], queryFn: getBankAccounts });
  const invoice = invoiceQuery.data?.invoice;
  const [methodId, setMethodId] = useState('');
  const [sourceId, setSourceId] = useState('');
  const [amount, setAmount] = useState('');
  const [reference, setReference] = useState('');
  const method = useMemo(() => methodsQuery.data?.find(x => x.id === methodId), [methodsQuery.data, methodId]);

  useEffect(() => { if (invoice) setAmount(String(invoice.remainingAmount)); }, [invoice?.remainingAmount]);
  useEffect(() => {
    const first = methodsQuery.data?.find(x => x.requiresCashbox) ?? methodsQuery.data?.[0];
    if (!methodId && first) setMethodId(first.id);
  }, [methodsQuery.data, methodId]);
  useEffect(() => {
    setSourceId(method?.requiresBankAccount ? banksQuery.data?.[0]?.id ?? '' : cashboxesQuery.data?.[0]?.id ?? '');
  }, [method?.id, method?.requiresBankAccount, banksQuery.data, cashboxesQuery.data]);

  const payment = useMutation({
    mutationFn: async () => {
      if (!invoice || !method || !sourceId) throw new Error('يرجى اختيار طريقة ومصدر الدفع.');
      const value = Number(amount);
      if (value <= 0 || value > invoice.remainingAmount) throw new Error('مبلغ الدفع غير صحيح.');
      const id = await createPaymentVoucher({
        supplierId: invoice.supplierId, cashboxId: method.requiresCashbox ? sourceId : null,
        bankAccountId: method.requiresBankAccount ? sourceId : null, paymentMethodId: method.id,
        purchaseInvoiceId: invoice.id, amount: value, currency: invoice.currencyCode, reference: reference || null
      });
      await approvePaymentVoucher(id);
      await postPaymentVoucher(id, invoice.id);
    },
    onSuccess: async () => { await queryClient.invalidateQueries({ queryKey: ['purchase-invoice', invoiceId] }); setReference(''); },
  });

  if (invoiceQuery.isLoading) return <main className="page-shell"><p>جار التحميل...</p></main>;
  if (!invoice) return <main className="page-shell"><p>تعذر تحميل فاتورة الشراء.</p></main>;
  const sources = method?.requiresBankAccount ? banksQuery.data ?? [] : cashboxesQuery.data ?? [];

  function submit(event: FormEvent) { event.preventDefault(); payment.mutate(); }

  return <main className="page-shell" dir="rtl">
    <div className="page-stack">
      <section className="form-panel form-compact">
        <div className="compact-hero"><div><p className="compact-hero__eyebrow">{invoice.supplierName}</p><h2>فاتورة شراء {invoice.invoiceNumber}</h2></div><span className="ready-badge">{invoice.statusDisplay}</span></div>
      </section>
      <section className="form-panel form-compact">
        <dl className="mini-grid"><div><dt>الإجمالي</dt><dd>{invoice.totalAmount.toFixed(2)} {invoice.currencyCode}</dd></div><div><dt>المدفوع</dt><dd>{invoice.paidAmount.toFixed(2)}</dd></div><div><dt>المتبقي</dt><dd>{invoice.remainingAmount.toFixed(2)}</dd></div></dl>
        {invoice.sourceContainerId ? <Link className="chip-button" to={`/china/${invoice.sourceContainerId}`}>الحاوية {invoice.sourceContainerNumber}</Link> : null}
      </section>
      {invoice.remainingAmount > 0 ? <form className="form-panel form-grid" onSubmit={submit}>
        <label className="form-field"><span className="form-field__label">طريقة الدفع</span><select value={methodId} onChange={e => setMethodId(e.target.value)}>{methodsQuery.data?.map(x => <option key={x.id} value={x.id}>{x.name}</option>)}</select></label>
        <label className="form-field"><span className="form-field__label">{method?.requiresBankAccount ? 'الحساب البنكي' : 'الصندوق'}</span><select value={sourceId} onChange={e => setSourceId(e.target.value)}>{sources.map(x => <option key={x.id} value={x.id}>{x.name}</option>)}</select></label>
        <label className="form-field"><span className="form-field__label">المبلغ</span><input inputMode="decimal" value={amount} onChange={e => setAmount(e.target.value)} /></label>
        {method?.requiresReference ? <label className="form-field"><span className="form-field__label">المرجع البنكي</span><input value={reference} onChange={e => setReference(e.target.value)} required /></label> : null}
        {payment.error ? <p className="field-note form-grid__wide">{payment.error instanceof Error ? payment.error.message : 'تعذر تسجيل الدفعة.'}</p> : null}
        <button className="primary-button primary-button--wide form-grid__wide" disabled={payment.isPending}>{payment.isPending ? 'جار التسجيل...' : 'تسجيل دفعة'}</button>
      </form> : null}
      <section className="form-panel form-compact"><h3>الدفعات المسجلة</h3>{invoiceQuery.data?.payments.length ? <div className="table-wrap"><table><thead><tr><th>السند</th><th>التاريخ</th><th>المبلغ</th><th>الحالة</th></tr></thead><tbody>{invoiceQuery.data.payments.map(p => <tr key={p.voucherId}><td>{p.voucherNumber}</td><td>{new Date(p.voucherDate).toLocaleDateString('ar')}</td><td>{p.amount.toFixed(2)}</td><td>{p.statusDisplay}</td></tr>)}</tbody></table></div> : <p className="field-note">لا توجد دفعات بعد.</p>}</section>
    </div>
  </main>;
}
