import { useMemo, useState, type FormEvent } from 'react';
import { useMutation, useQuery, useQueryClient, type UseQueryResult } from '@tanstack/react-query';
import { useNavigate, useParams } from 'react-router-dom';
import {
  getAccounts,
  getJournalEntries,
  getJournalEntry,
  getJournalEntryPdf,
  getTrialBalance
} from '../api/accounting.ts';
import { getCustomers } from '../api/customers.ts';
import {
  createCashbox,
  createCashboxTransfer,
  getCashboxes,
  getCashboxTransfers
} from '../api/finance.ts';
import {
  getBankAccounts,
  getPaymentMethods,
  getReceiptVoucher,
  getReceiptVouchers,
  postReceiptVoucher,
  updateReceiptVoucherDraft
} from '../api/receipts.ts';
import { getApiErrorMessage } from '../lib/apiError.ts';
import type {
  JournalEntryListDto,
  JournalEntryStatus,
  ReceiptVoucherDetailsDto,
  TrialBalanceLineDto
} from '../api/types.ts';
import { AppShell } from '../components/AppShell.tsx';
import { DocumentActions } from '../components/DocumentActions.tsx';
import { EmptyState } from '../components/EmptyState.tsx';
import { ErrorState } from '../components/ErrorState.tsx';
import { LoadingState } from '../components/LoadingState.tsx';
import { Modal } from '../components/Modal.tsx';
import { SummaryCard } from '../components/SummaryCard.tsx';
import type { DocumentExportPayload } from '../lib/documentExport.ts';
import { formatCurrency, formatDate, formatDateOnly } from '../lib/format.ts';
import {
  getJournalEntryStatusTone,
  glAccountTypeLabel,
  journalEntryStatusLabel,
  journalEntryStatusOptions
} from '../lib/enums.ts';

const LIST_PAGE_SIZE = 100;

type AccountingTab = 'summary' | 'cashboxes' | 'trial-balance' | 'journal' | 'accounts';

export function AccountingPage() {
  const { entryId } = useParams();
  if (entryId) {
    return <JournalEntryDetailPage entryId={entryId} />;
  }
  return <AccountingHomePage />;
}

function AccountingHomePage() {
  const [tab, setTab] = useState<AccountingTab>('summary');

  const trialBalanceQuery = useQuery({
    queryKey: ['accounting', 'trial-balance'],
    queryFn: () => getTrialBalance()
  });

  const metrics = useMemo(() => computeMetrics(trialBalanceQuery.data ?? []), [trialBalanceQuery.data]);

  const headerSummary = (
    <>
      <SummaryCard label="إجمالي الأصول" value={formatCurrency(metrics.assets)} />
      <SummaryCard label="إجمالي الخصوم" value={formatCurrency(metrics.liabilities)} tone="amber" />
      <SummaryCard label="صافي الدخل" value={formatCurrency(metrics.netIncome)} tone={metrics.netIncome >= 0 ? 'green' : 'amber'} />
    </>
  );

  return (
    <AppShell title="المحاسبة والتقارير" summary={headerSummary}>
      <div className="page-stack">
        <section className="form-panel form-compact">
          <div className="tab-strip" role="tablist" aria-label="تبويبات المحاسبة">
            <TabButton active={tab === 'journal'} onClick={() => setTab('journal')} label="القيود اليومية" />
            <TabButton active={tab === 'cashboxes'} onClick={() => setTab('cashboxes')} label="الصناديق" />
            <TabButton active={tab === 'summary'} onClick={() => setTab('summary')} label="الملخص المالي" />
            <TabButton active={tab === 'trial-balance'} onClick={() => setTab('trial-balance')} label="ميزان المراجعة" />
            <TabButton active={tab === 'accounts'} onClick={() => setTab('accounts')} label="دليل الحسابات" />
          </div>
        </section>

        <section className="form-panel form-compact">
          {tab === 'summary' ? <SummaryTab query={trialBalanceQuery} metrics={metrics} /> : null}
          {tab === 'cashboxes' ? <CashboxesTab /> : null}
          {tab === 'trial-balance' ? <TrialBalanceTab query={trialBalanceQuery} metrics={metrics} /> : null}
          {tab === 'journal' ? <JournalTab /> : null}
          {tab === 'accounts' ? <AccountsTab /> : null}
        </section>
      </div>
    </AppShell>
  );
}

function TabButton({ active, onClick, label }: { active: boolean; onClick: () => void; label: string }) {
  return (
    <button
      className={`filter-chip ${active ? 'filter-chip--active' : ''}`}
      type="button"
      role="tab"
      aria-selected={active}
      onClick={onClick}
    >
      {label}
    </button>
  );
}

type TrialBalanceQuery = UseQueryResult<TrialBalanceLineDto[], Error>;
type Metrics = ReturnType<typeof computeMetrics>;

function SummaryTab({ query, metrics }: { query: TrialBalanceQuery; metrics: Metrics }) {
  if (query.isLoading) {
    return <LoadingState />;
  }
  if (query.isError) {
    return <ErrorState message={getErrorMessage(query.error)} onRetry={() => void query.refetch()} />;
  }
  return (
    <>
      <dl className="detail-grid">
        <DetailItem label="إجمالي الأصول" value={formatCurrency(metrics.assets)} />
        <DetailItem label="إجمالي الخصوم" value={formatCurrency(metrics.liabilities)} />
        <DetailItem label="حقوق الملكية" value={formatCurrency(metrics.equity)} />
        <DetailItem label="إجمالي الإيرادات" value={formatCurrency(metrics.revenue)} />
        <DetailItem label="إجمالي المصروفات" value={formatCurrency(metrics.expenses)} />
        <DetailItem label="صافي الدخل" value={formatCurrency(metrics.netIncome)} />
      </dl>
      <div className="banner banner--success" role="status">
        {metrics.isBalanced
          ? 'ميزان المراجعة متوازن (إجمالي المدين = إجمالي الدائن).'
          : `تنبيه: الميزان غير متوازن — المدين ${formatCurrency(metrics.totalDebits)} مقابل الدائن ${formatCurrency(metrics.totalCredits)}.`}
      </div>
      <h3>قائمة الدخل المبسطة</h3>
      <div className="line-list">
        <div className="price-row"><span>الإيرادات</span><strong>{formatCurrency(metrics.revenue)}</strong></div>
        <div className="price-row"><span>(ناقص) المصروفات</span><strong>{formatCurrency(metrics.expenses)}</strong></div>
        <div className="price-row"><span>= صافي الدخل</span><strong>{formatCurrency(metrics.netIncome)}</strong></div>
      </div>
      <h3>الميزانية المبسطة</h3>
      <div className="line-list">
        <div className="price-row"><span>الأصول</span><strong>{formatCurrency(metrics.assets)}</strong></div>
        <div className="price-row"><span>الخصوم</span><strong>{formatCurrency(metrics.liabilities)}</strong></div>
        <div className="price-row"><span>حقوق الملكية</span><strong>{formatCurrency(metrics.equity)}</strong></div>
      </div>
    </>
  );
}

function TrialBalanceTab({ query, metrics }: { query: TrialBalanceQuery; metrics: Metrics }) {
  if (query.isLoading) {
    return <LoadingState />;
  }
  if (query.isError) {
    return <ErrorState message={getErrorMessage(query.error)} onRetry={() => void query.refetch()} />;
  }
  const rows = query.data ?? [];
  if (rows.length === 0) {
    return <EmptyState title="لا توجد بيانات" description="لا توجد أرصدة في ميزان المراجعة." />;
  }
  return (
    <div className="table-scroll">
      <table className="data-table">
        <thead>
          <tr>
            <th>الحساب</th>
            <th>الاسم</th>
            <th>النوع</th>
            <th>مدين</th>
            <th>دائن</th>
            <th>الرصيد</th>
          </tr>
        </thead>
        <tbody>
          {rows.map((row) => (
            <tr key={row.accountId}>
              <td>{row.accountCode}</td>
              <td>{row.accountName}</td>
              <td>{row.accountTypeDisplay}</td>
              <td>{formatCurrency(row.debitTotal)}</td>
              <td>{formatCurrency(row.creditTotal)}</td>
              <td>{formatCurrency(row.balance)}</td>
            </tr>
          ))}
        </tbody>
        <tfoot>
          <tr>
            <td colSpan={3}>الإجمالي</td>
            <td>{formatCurrency(metrics.totalDebits)}</td>
            <td>{formatCurrency(metrics.totalCredits)}</td>
            <td>{metrics.isBalanced ? 'متوازن' : 'غير متوازن'}</td>
          </tr>
        </tfoot>
      </table>
    </div>
  );
}

function CashboxesTab() {
  const queryClient = useQueryClient();
  const [dialog, setDialog] = useState<'create' | 'transfer' | null>(null);
  const [notice, setNotice] = useState('');

  const cashboxesQuery = useQuery({
    queryKey: ['finance', 'cashboxes'],
    queryFn: getCashboxes
  });
  const transfersQuery = useQuery({
    queryKey: ['finance', 'cashbox-transfers'],
    queryFn: getCashboxTransfers
  });

  const cashboxes = cashboxesQuery.data ?? [];
  const activeCashboxes = cashboxes.filter((cashbox) => cashbox.isActive);
  const totalBalance = activeCashboxes.reduce((sum, cashbox) => sum + cashbox.balance, 0);

  function complete(message: string) {
    setNotice(message);
    setDialog(null);
    void queryClient.invalidateQueries({ queryKey: ['finance', 'cashboxes'] });
    void queryClient.invalidateQueries({ queryKey: ['finance', 'cashbox-transfers'] });
  }

  if (cashboxesQuery.isLoading) {
    return <LoadingState />;
  }
  if (cashboxesQuery.isError) {
    return <ErrorState message={getErrorMessage(cashboxesQuery.error)} onRetry={() => void cashboxesQuery.refetch()} />;
  }

  return (
    <>
      <div className="toolbar-row toolbar-row--start">
        <div>
          <h2 className="section-heading">إدارة الصناديق</h2>
          <p className="form-hint">الأرصدة الحية والتحويلات المرحلة بين صناديق الفرع الحالي.</p>
        </div>
        <div className="compact-action-row">
          <button
            className="ghost-button"
            type="button"
            disabled={activeCashboxes.length < 2}
            onClick={() => setDialog('transfer')}
          >
            مناقلة بين صندوقين
          </button>
          <button className="primary-button" type="button" onClick={() => setDialog('create')}>
            إضافة صندوق
          </button>
        </div>
      </div>

      {notice ? <div className="banner banner--success" role="status">{notice}</div> : null}

      <div className="line-list">
        <div className="price-row">
          <span>إجمالي أرصدة الصناديق النشطة</span>
          <strong>{formatCurrency(totalBalance)}</strong>
        </div>
        <div className="price-row">
          <span>عدد الصناديق النشطة</span>
          <strong>{activeCashboxes.length}</strong>
        </div>
      </div>

      {cashboxes.length === 0 ? (
        <EmptyState title="لا توجد صناديق" description="أضف أول صندوق لبدء عمليات القبض والصرف والمناقلة." />
      ) : (
        <div className="table-scroll">
          <table className="data-table">
            <thead>
              <tr>
                <th>الكود</th>
                <th>اسم الصندوق</th>
                <th>العملة</th>
                <th>الرصيد الحالي</th>
                <th>حساب الأستاذ</th>
                <th>الحالة</th>
              </tr>
            </thead>
            <tbody>
              {cashboxes.map((cashbox) => (
                <tr key={cashbox.id}>
                  <td>{cashbox.code}</td>
                  <td>{cashbox.name}</td>
                  <td>{cashbox.currency}</td>
                  <td>{formatCurrency(cashbox.balance)}</td>
                  <td>{cashbox.accountId ? 'مرتبط' : 'غير مرتبط'}</td>
                  <td>
                    <span className={`status-pill status-pill--${cashbox.isActive ? 'green' : 'gray'}`}>
                      {cashbox.isActive ? 'نشط' : 'معطل'}
                    </span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <div className="section-title-row">
        <div>
          <h3>آخر المناقلات</h3>
          <p>تظهر المناقلة بعد ترحيلها وتحديث رصيدي الصندوقين.</p>
        </div>
      </div>

      {transfersQuery.isLoading ? <LoadingState /> : null}
      {transfersQuery.isError ? (
        <ErrorState message={getErrorMessage(transfersQuery.error)} onRetry={() => void transfersQuery.refetch()} />
      ) : null}
      {transfersQuery.isSuccess && (transfersQuery.data?.length ?? 0) === 0 ? (
        <EmptyState title="لا توجد مناقلات" description="لم تسجل أي مناقلة بين الصناديق حتى الآن." />
      ) : null}
      {(transfersQuery.data?.length ?? 0) > 0 ? (
        <div className="table-scroll">
          <table className="data-table">
            <thead>
              <tr>
                <th>رقم المناقلة</th>
                <th>من صندوق</th>
                <th>إلى صندوق</th>
                <th>التاريخ</th>
                <th>المبلغ</th>
                <th>الحالة</th>
              </tr>
            </thead>
            <tbody>
              {transfersQuery.data!.slice(0, 20).map((transfer) => (
                <tr key={transfer.id}>
                  <td>{transfer.transferNumber}</td>
                  <td>{transfer.fromCashboxName}</td>
                  <td>{transfer.toCashboxName}</td>
                  <td>{formatDate(transfer.transferDate)}</td>
                  <td>{formatCurrency(transfer.amount)}</td>
                  <td><span className="status-pill status-pill--green">{transfer.statusDisplay}</span></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : null}

      {dialog === 'create' ? (
        <Modal
          title="إضافة صندوق مالي"
          subtitle="أنشئ صندوقاً جديداً مرتبطاً بحساب النقدية."
          onClose={() => setDialog(null)}
        >
          <CreateCashboxForm onDone={() => complete('تم إنشاء الصندوق بنجاح.')} onCancel={() => setDialog(null)} />
        </Modal>
      ) : null}

      {dialog === 'transfer' ? (
        <Modal
          title="مناقلة بين الصناديق"
          subtitle="تُرحّل المناقلة فوراً ويُحدّث رصيد الصندوقين."
          onClose={() => setDialog(null)}
        >
          <CashboxTransferForm
            cashboxes={activeCashboxes}
            onDone={() => complete('تم ترحيل المناقلة وتحديث الأرصدة بنجاح.')}
            onCancel={() => setDialog(null)}
          />
        </Modal>
      ) : null}
    </>
  );
}

function CreateCashboxForm({ onDone, onCancel }: { onDone: () => void; onCancel: () => void }) {
  const [code, setCode] = useState('');
  const [name, setName] = useState('');
  const [error, setError] = useState('');

  const mutation = useMutation({
    mutationFn: () => createCashbox({
      code: code.trim() || null,
      name: name.trim(),
      currency: 'USD'
    }),
    onSuccess: onDone,
    onError: (reason) => setError(getErrorMessage(reason))
  });

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError('');
    if (!name.trim()) {
      setError('اسم الصندوق مطلوب.');
      return;
    }
    mutation.mutate();
  }

  return (
    <form className="form-grid" onSubmit={submit}>
      {error ? <div className="banner banner--warn form-grid__wide" role="alert">{error}</div> : null}
      <label className="form-field">
        <span className="form-field__label">كود الصندوق</span>
        <input value={code} onChange={(event) => setCode(event.target.value)} placeholder="يُولّد تلقائياً عند تركه فارغاً" />
      </label>
      <label className="form-field">
        <span className="form-field__label">اسم الصندوق *</span>
        <input value={name} onChange={(event) => setName(event.target.value)} required autoFocus />
      </label>
      <label className="form-field">
        <span className="form-field__label">العملة</span>
        <input value="USD" readOnly />
      </label>
      <div className="compact-action-row form-grid__wide">
        <button className="primary-button" type="submit" disabled={mutation.isPending}>
          {mutation.isPending ? 'جار الإنشاء...' : 'إنشاء الصندوق'}
        </button>
        <button className="ghost-button" type="button" onClick={onCancel} disabled={mutation.isPending}>
          إلغاء
        </button>
      </div>
    </form>
  );
}

function CashboxTransferForm({
  cashboxes,
  onDone,
  onCancel
}: {
  cashboxes: Awaited<ReturnType<typeof getCashboxes>>;
  onDone: () => void;
  onCancel: () => void;
}) {
  const [fromCashboxId, setFromCashboxId] = useState(cashboxes[0]?.id ?? '');
  const [toCashboxId, setToCashboxId] = useState(cashboxes[1]?.id ?? '');
  const [amount, setAmount] = useState('');
  const [notes, setNotes] = useState('');
  const [error, setError] = useState('');
  const source = cashboxes.find((cashbox) => cashbox.id === fromCashboxId);

  const mutation = useMutation({
    mutationFn: () => createCashboxTransfer({
      fromCashboxId,
      toCashboxId,
      amount: Number(amount),
      notes: notes.trim() || null
    }),
    onSuccess: onDone,
    onError: (reason) => setError(getErrorMessage(reason))
  });

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError('');
    const parsedAmount = Number(amount);
    if (!fromCashboxId || !toCashboxId) {
      setError('اختر صندوق المصدر وصندوق الوجهة.');
      return;
    }
    if (fromCashboxId === toCashboxId) {
      setError('يجب أن يكون صندوق الوجهة مختلفاً عن صندوق المصدر.');
      return;
    }
    if (!Number.isFinite(parsedAmount) || parsedAmount <= 0) {
      setError('أدخل مبلغاً صحيحاً أكبر من صفر.');
      return;
    }
    if (source && parsedAmount > source.balance) {
      setError('المبلغ يتجاوز الرصيد المتاح في صندوق المصدر.');
      return;
    }
    mutation.mutate();
  }

  return (
    <form className="form-grid" onSubmit={submit}>
      {error ? <div className="banner banner--warn form-grid__wide" role="alert">{error}</div> : null}
      <label className="form-field">
        <span className="form-field__label">من صندوق *</span>
        <select value={fromCashboxId} onChange={(event) => setFromCashboxId(event.target.value)} required>
          {cashboxes.map((cashbox) => (
            <option key={cashbox.id} value={cashbox.id}>
              {cashbox.name} - {formatCurrency(cashbox.balance)}
            </option>
          ))}
        </select>
      </label>
      <label className="form-field">
        <span className="form-field__label">إلى صندوق *</span>
        <select value={toCashboxId} onChange={(event) => setToCashboxId(event.target.value)} required>
          {cashboxes.map((cashbox) => (
            <option key={cashbox.id} value={cashbox.id} disabled={cashbox.id === fromCashboxId}>
              {cashbox.name}
            </option>
          ))}
        </select>
      </label>
      <label className="form-field">
        <span className="form-field__label">المبلغ *</span>
        <input
          inputMode="decimal"
          min="0.01"
          step="0.01"
          value={amount}
          onChange={(event) => setAmount(event.target.value)}
          required
          autoFocus
        />
        <span className="form-hint">المتاح: {formatCurrency(source?.balance ?? 0)}</span>
      </label>
      <label className="form-field">
        <span className="form-field__label">ملاحظة</span>
        <input value={notes} onChange={(event) => setNotes(event.target.value)} placeholder="سبب المناقلة أو مرجعها" />
      </label>
      <div className="compact-action-row form-grid__wide">
        <button className="primary-button" type="submit" disabled={mutation.isPending}>
          {mutation.isPending ? 'جار الترحيل...' : 'ترحيل المناقلة'}
        </button>
        <button className="ghost-button" type="button" onClick={onCancel} disabled={mutation.isPending}>
          إلغاء
        </button>
      </div>
    </form>
  );
}

function JournalTab() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [status, setStatus] = useState('');
  const [selectedReceiptId, setSelectedReceiptId] = useState<string | null>(null);
  const [notice, setNotice] = useState('');

  const journalQuery = useQuery({
    queryKey: ['accounting', 'journal', status],
    queryFn: () =>
      getJournalEntries({
        status: status === '' ? undefined : (Number(status) as JournalEntryStatus),
        page: 1,
        pageSize: LIST_PAGE_SIZE
      })
  });
  const pendingReceiptsQuery = useQuery({
    queryKey: ['finance', 'receipts', 'pending'],
    queryFn: () => getReceiptVouchers({ pendingOnly: true })
  });

  const rows = journalQuery.data?.items ?? [];
  const showPendingReceipts = status === '' || status === '0';
  const workspaceRows = useMemo<JournalWorkspaceRow[]>(() => {
    const journalRows: JournalWorkspaceRow[] = rows.map((entry) => ({
      key: `journal-${entry.id}`,
      kind: 'journal',
      date: entry.entryDate,
      entry
    }));
    const receiptRows: JournalWorkspaceRow[] = showPendingReceipts
      ? (pendingReceiptsQuery.data ?? []).map((receipt) => ({
          key: `receipt-${receipt.id}`,
          kind: 'receipt',
          date: receipt.voucherDate,
          receipt
        }))
      : [];

    return [...journalRows, ...receiptRows]
      .sort((left, right) => new Date(right.date).getTime() - new Date(left.date).getTime());
  }, [pendingReceiptsQuery.data, rows, showPendingReceipts]);

  async function handleReceiptPosted() {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: ['accounting', 'journal'] }),
      queryClient.invalidateQueries({ queryKey: ['accounting', 'trial-balance'] }),
      queryClient.invalidateQueries({ queryKey: ['finance', 'receipts'] }),
      queryClient.invalidateQueries({ queryKey: ['finance', 'cashboxes'] }),
      queryClient.invalidateQueries({ queryKey: ['customers'] }),
      queryClient.invalidateQueries({ queryKey: ['customer-details'] }),
      queryClient.invalidateQueries({ queryKey: ['customer-ledger'] })
    ]);
    setSelectedReceiptId(null);
    setNotice('تم ترحيل سند القبض وإنشاء القيد المحاسبي وتحديث الأرصدة بنجاح.');
  }

  return (
    <>
      <label className="inline-field">
        الحالة
        <select value={status} onChange={(event) => setStatus(event.target.value)}>
          <option value="">كل الحالات</option>
          {journalEntryStatusOptions.map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </select>
      </label>

      {notice ? <div className="banner banner--success" role="status">{notice}</div> : null}

      {journalQuery.isLoading || pendingReceiptsQuery.isLoading ? <LoadingState /> : null}
      {journalQuery.isError ? (
        <ErrorState message={getErrorMessage(journalQuery.error)} onRetry={() => void journalQuery.refetch()} />
      ) : null}
      {pendingReceiptsQuery.isError ? (
        <ErrorState
          message={getErrorMessage(pendingReceiptsQuery.error)}
          onRetry={() => void pendingReceiptsQuery.refetch()}
        />
      ) : null}
      {journalQuery.isSuccess && pendingReceiptsQuery.isSuccess && workspaceRows.length === 0 ? (
        <EmptyState title="لا توجد قيود" description="لا توجد قيود يومية مطابقة." />
      ) : null}

      {workspaceRows.length > 0 ? (
        <div className="table-scroll">
          <table className="data-table">
            <thead>
              <tr>
                <th>الطرف / الجهة</th>
                <th>نوع الحركة</th>
                <th>الوصف</th>
                <th>مدين</th>
                <th>دائن</th>
                <th>التاريخ</th>
                <th>الحالة</th>
              </tr>
            </thead>
            <tbody>
              {workspaceRows.map((row) => {
                if (row.kind === 'receipt') {
                  return (
                    <tr
                      key={row.key}
                      className="clickable-row accounting-row accounting-row--unposted"
                      onClick={() => setSelectedReceiptId(row.receipt.id)}
                    >
                      <td>{row.receipt.customerName || '—'}</td>
                      <td>سند قبض</td>
                      <td>
                        سند قبض {row.receipt.voucherNumber}
                        {' — '}
                        {row.receipt.paymentMethodName || 'طريقة قبض غير محددة'}
                        {row.receipt.cashboxName ? ` — ${row.receipt.cashboxName}` : ''}
                      </td>
                      <td>{formatCurrency(row.receipt.amount)}</td>
                      <td>{formatCurrency(row.receipt.amount)}</td>
                      <td>{formatDateOnly(row.receipt.voucherDate)}</td>
                      <td><span className="status-pill status-pill--amber">غير مرحّل</span></td>
                    </tr>
                  );
                }

                const entry = row.entry;
                return (
                  <tr
                    key={row.key}
                    className={`clickable-row accounting-row ${
                      entry.status === 2
                        ? 'accounting-row--posted'
                        : entry.status === 0 || entry.status === 1
                          ? 'accounting-row--unposted'
                          : 'accounting-row--closed'
                    }`}
                    onClick={() => navigate(`/accounting/journal/${entry.id}`)}
                  >
                    <td>{entry.partyName || '—'}</td>
                    <td>{entry.sourceTypeDisplay || 'قيد يدوي'}</td>
                    <td>{entry.description}</td>
                    <td>{formatCurrency(entry.debitTotal)}</td>
                    <td>{formatCurrency(entry.creditTotal)}</td>
                    <td>{formatDateOnly(entry.entryDate)}</td>
                    <td>
                      <span className={`status-pill status-pill--${getJournalEntryStatusTone(entry.status)}`}>
                        {journalEntryStatusLabel(entry.status)}
                      </span>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      ) : null}

      {selectedReceiptId ? (
        <ReceiptPostingModal
          receiptId={selectedReceiptId}
          onClose={() => setSelectedReceiptId(null)}
          onPosted={handleReceiptPosted}
        />
      ) : null}
    </>
  );
}

type JournalWorkspaceRow =
  | {
      key: string;
      kind: 'journal';
      date: string;
      entry: JournalEntryListDto;
    }
  | {
      key: string;
      kind: 'receipt';
      date: string;
      receipt: ReceiptVoucherDetailsDto;
    };

function ReceiptPostingModal({
  receiptId,
  onClose,
  onPosted
}: {
  receiptId: string;
  onClose: () => void;
  onPosted: () => Promise<void>;
}) {
  const [busy, setBusy] = useState(false);
  const receiptQuery = useQuery({
    queryKey: ['finance', 'receipt', receiptId],
    queryFn: () => getReceiptVoucher(receiptId)
  });
  const receipt = receiptQuery.data;

  return (
    <Modal
      title={receipt ? `سند قبض ${receipt.voucherNumber}` : 'بيانات سند القبض'}
      subtitle={receipt?.status === 0
        ? 'يمكن تعديل بيانات المسودة ثم حفظها وترحيلها.'
        : 'راجع البيانات قبل الترحيل؛ لا يمكن تعديل القيد بعد ترحيله.'}
      onClose={busy ? () => undefined : onClose}
    >
      {receiptQuery.isLoading ? <LoadingState /> : null}
      {receiptQuery.isError ? (
        <ErrorState message={getErrorMessage(receiptQuery.error)} onRetry={() => void receiptQuery.refetch()} />
      ) : null}

      {receipt?.status === 0 ? (
        <ReceiptDraftPostingForm
          receipt={receipt}
          onClose={onClose}
          onPosted={onPosted}
          onBusyChange={setBusy}
        />
      ) : null}
      {receipt && receipt.status !== 0 ? (
        <ReceiptPostOnlyPanel
          receipt={receipt}
          onClose={onClose}
          onPosted={onPosted}
          onBusyChange={setBusy}
        />
      ) : null}
    </Modal>
  );
}

function ReceiptDraftPostingForm({
  receipt,
  onClose,
  onPosted,
  onBusyChange
}: {
  receipt: ReceiptVoucherDetailsDto;
  onClose: () => void;
  onPosted: () => Promise<void>;
  onBusyChange: (busy: boolean) => void;
}) {
  const [paymentMethodId, setPaymentMethodId] = useState(receipt.paymentMethodId);
  const [customerId, setCustomerId] = useState(receipt.customerId);
  const [cashboxId, setCashboxId] = useState(receipt.cashboxId ?? '');
  const [bankAccountId, setBankAccountId] = useState(receipt.bankAccountId ?? '');
  const [amount, setAmount] = useState(String(receipt.amount));
  const [reference, setReference] = useState(receipt.reference ?? '');
  const [validationError, setValidationError] = useState('');

  const paymentMethodsQuery = useQuery({
    queryKey: ['finance', 'payment-methods'],
    queryFn: getPaymentMethods
  });
  const customersQuery = useQuery({
    queryKey: ['customers', 'receipt-draft-editor'],
    queryFn: () => getCustomers({ page: 1, pageSize: 500 })
  });
  const cashboxesQuery = useQuery({
    queryKey: ['finance', 'cashboxes'],
    queryFn: getCashboxes
  });
  const bankAccountsQuery = useQuery({
    queryKey: ['finance', 'bank-accounts'],
    queryFn: getBankAccounts
  });

  const selectedMethod = (paymentMethodsQuery.data ?? [])
    .find((method) => method.id === paymentMethodId);
  const requiresBank = selectedMethod?.requiresBankAccount ?? Boolean(receipt.bankAccountId);
  const requiresCashbox = selectedMethod?.requiresCashbox ?? !requiresBank;
  const requiresReference = selectedMethod?.requiresReference ?? false;

  const mutation = useMutation({
    mutationFn: async () => {
      onBusyChange(true);
      try {
        await updateReceiptVoucherDraft(receipt.id, {
          customerId,
          paymentMethodId,
          cashboxId: requiresCashbox ? cashboxId : null,
          bankAccountId: requiresBank ? bankAccountId : null,
          amount: Number(amount),
          currency: receipt.currency || 'USD',
          exchangeRate: 1,
          reference: reference.trim() || null
        });
        await postReceiptVoucher(receipt.id, `web-journal-receipt:${receipt.id}`);
      } finally {
        onBusyChange(false);
      }
    },
    onSuccess: onPosted
  });

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setValidationError('');
    const parsedAmount = Number(amount);
    if (!customerId) {
      setValidationError('اختر العميل.');
      return;
    }
    if (!paymentMethodId) {
      setValidationError('اختر طريقة القبض.');
      return;
    }
    if (requiresCashbox && !cashboxId) {
      setValidationError('اختر الصندوق.');
      return;
    }
    if (requiresBank && !bankAccountId) {
      setValidationError('اختر الحساب البنكي.');
      return;
    }
    if (requiresReference && !reference.trim()) {
      setValidationError('أدخل مرجع عملية القبض.');
      return;
    }
    if (!Number.isFinite(parsedAmount) || parsedAmount <= 0) {
      setValidationError('أدخل مبلغاً صحيحاً أكبر من صفر.');
      return;
    }
    mutation.mutate();
  }

  const lookupError = customersQuery.error
    ?? paymentMethodsQuery.error
    ?? cashboxesQuery.error
    ?? bankAccountsQuery.error;

  return (
    <form className="form-grid" onSubmit={submit}>
      <div className="form-grid__wide">
        <dl className="detail-grid">
          <DetailItem label="رقم السند" value={receipt.voucherNumber} />
          <DetailItem label="العميل" value={receipt.customerName} />
          <DetailItem label="التاريخ" value={formatDate(receipt.voucherDate)} />
          <DetailItem label="الحالة" value="مسودة قابلة للتعديل" />
        </dl>
      </div>

      {lookupError ? (
        <div className="banner banner--warn form-grid__wide" role="alert">{getErrorMessage(lookupError)}</div>
      ) : null}
      {validationError ? (
        <div className="banner banner--warn form-grid__wide" role="alert">{validationError}</div>
      ) : null}
      {mutation.isError ? (
        <div className="banner banner--warn form-grid__wide" role="alert">{getErrorMessage(mutation.error)}</div>
      ) : null}

      <label className="form-field">
        <span className="form-field__label">العميل *</span>
        <select
          value={customerId}
          onChange={(event) => setCustomerId(event.target.value)}
          disabled={customersQuery.isLoading || mutation.isPending}
          required
        >
          <option value="">اختر العميل...</option>
          {(customersQuery.data?.items ?? []).filter((customer) => customer.isActive).map((customer) => (
            <option key={customer.id} value={customer.id}>
              {customer.nameAr} — {customer.code}
            </option>
          ))}
        </select>
      </label>

      <label className="form-field">
        <span className="form-field__label">طريقة القبض *</span>
        <select
          value={paymentMethodId}
          onChange={(event) => setPaymentMethodId(event.target.value)}
          disabled={paymentMethodsQuery.isLoading || mutation.isPending}
          required
        >
          <option value="">اختر طريقة القبض...</option>
          {(paymentMethodsQuery.data ?? []).map((method) => (
            <option key={method.id} value={method.id}>{method.name}</option>
          ))}
        </select>
      </label>

      {requiresCashbox ? (
        <label className="form-field">
          <span className="form-field__label">الصندوق *</span>
          <select
            value={cashboxId}
            onChange={(event) => setCashboxId(event.target.value)}
            disabled={cashboxesQuery.isLoading || mutation.isPending}
            required
          >
            <option value="">اختر الصندوق...</option>
            {(cashboxesQuery.data ?? []).filter((cashbox) => cashbox.isActive).map((cashbox) => (
              <option key={cashbox.id} value={cashbox.id}>
                {cashbox.name} — {formatCurrency(cashbox.balance)}
              </option>
            ))}
          </select>
        </label>
      ) : null}

      {requiresBank ? (
        <label className="form-field">
          <span className="form-field__label">الحساب البنكي *</span>
          <select
            value={bankAccountId}
            onChange={(event) => setBankAccountId(event.target.value)}
            disabled={bankAccountsQuery.isLoading || mutation.isPending}
            required
          >
            <option value="">اختر الحساب البنكي...</option>
            {(bankAccountsQuery.data ?? []).filter((bank) => bank.isActive).map((bank) => (
              <option key={bank.id} value={bank.id}>{bank.name} — {bank.bankName}</option>
            ))}
          </select>
        </label>
      ) : null}

      <label className="form-field">
        <span className="form-field__label">المبلغ ({receipt.currency || 'USD'}) *</span>
        <input
          inputMode="decimal"
          min="0.01"
          step="0.01"
          value={amount}
          onChange={(event) => setAmount(event.target.value)}
          disabled={mutation.isPending}
          required
          autoFocus
        />
      </label>

      {requiresReference ? (
        <label className="form-field">
          <span className="form-field__label">المرجع *</span>
          <input
            value={reference}
            onChange={(event) => setReference(event.target.value)}
            disabled={mutation.isPending}
            required
          />
        </label>
      ) : null}

      <div className="compact-action-row form-grid__wide">
        <button
          className="primary-button"
          type="submit"
          disabled={mutation.isPending || Boolean(lookupError)}
        >
          {mutation.isPending ? 'جار الحفظ والترحيل...' : 'حفظ وترحيل'}
        </button>
        <button className="ghost-button" type="button" onClick={onClose} disabled={mutation.isPending}>
          إغلاق
        </button>
      </div>
    </form>
  );
}

function ReceiptPostOnlyPanel({
  receipt,
  onClose,
  onPosted,
  onBusyChange
}: {
  receipt: ReceiptVoucherDetailsDto;
  onClose: () => void;
  onPosted: () => Promise<void>;
  onBusyChange: (busy: boolean) => void;
}) {
  const mutation = useMutation({
    mutationFn: async () => {
      onBusyChange(true);
      try {
        await postReceiptVoucher(receipt.id, `web-journal-receipt:${receipt.id}`);
      } finally {
        onBusyChange(false);
      }
    },
    onSuccess: onPosted
  });

  return (
    <div className="page-stack">
      <dl className="detail-grid">
        <DetailItem label="رقم السند" value={receipt.voucherNumber} />
        <DetailItem label="العميل" value={receipt.customerName} />
        <DetailItem label="التاريخ" value={formatDate(receipt.voucherDate)} />
        <DetailItem label="طريقة القبض" value={receipt.paymentMethodName || 'غير محددة'} />
        <DetailItem
          label="مصدر القبض"
          value={receipt.cashboxName || receipt.bankAccountName || 'غير محدد'}
        />
        <DetailItem label="المبلغ" value={formatCurrency(receipt.amount)} />
        <DetailItem label="الحالة" value="غير مرحّل" />
      </dl>

      {mutation.isError ? (
        <div className="banner banner--warn" role="alert">{getErrorMessage(mutation.error)}</div>
      ) : null}

      <div className="compact-action-row">
        <button className="primary-button" type="button" onClick={() => mutation.mutate()} disabled={mutation.isPending}>
          {mutation.isPending ? 'جار الترحيل...' : 'حفظ وترحيل'}
        </button>
        <button className="ghost-button" type="button" onClick={onClose} disabled={mutation.isPending}>
          إغلاق
        </button>
      </div>
    </div>
  );
}

function AccountsTab() {
  const accountsQuery = useQuery({
    queryKey: ['accounting', 'accounts'],
    queryFn: () => getAccounts()
  });

  const rows = accountsQuery.data ?? [];

  if (accountsQuery.isLoading) {
    return <LoadingState />;
  }
  if (accountsQuery.isError) {
    return <ErrorState message={getErrorMessage(accountsQuery.error)} onRetry={() => void accountsQuery.refetch()} />;
  }
  if (rows.length === 0) {
    return <EmptyState title="لا توجد حسابات" description="دليل الحسابات فارغ." />;
  }

  return (
    <div className="table-scroll">
      <table className="data-table">
        <thead>
          <tr>
            <th>الكود</th>
            <th>الاسم</th>
            <th>النوع</th>
            <th>قابل للترحيل</th>
          </tr>
        </thead>
        <tbody>
          {rows.map((account) => (
            <tr key={account.id}>
              <td style={{ paddingInlineStart: `${account.level * 16 + 8}px` }}>{account.code}</td>
              <td>{account.nameAr}</td>
              <td>{glAccountTypeLabel(account.accountType)}</td>
              <td>{account.isPostable ? 'نعم' : 'لا'}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function JournalEntryDetailPage({ entryId }: { entryId: string }) {
  const navigate = useNavigate();

  const entryQuery = useQuery({
    queryKey: ['journal-entry', entryId],
    queryFn: () => getJournalEntry(entryId)
  });

  const entry = entryQuery.data;

  const headerSummary = entry ? (
    <>
      <SummaryCard label="إجمالي المدين" value={formatCurrency(entry.debitTotal)} />
      <SummaryCard label="إجمالي الدائن" value={formatCurrency(entry.creditTotal)} />
    </>
  ) : undefined;

  const exportPayload: DocumentExportPayload | null = entry
    ? {
        title: `قيد محاسبي ${entry.entryNumber}`,
        subtitle: entry.description,
        fileName: `journal-${entry.entryNumber}.pdf`,
        shareText: `قيد محاسبي ${entry.entryNumber}\n${entry.description}\nمدين: ${formatCurrency(entry.debitTotal)}\nدائن: ${formatCurrency(entry.creditTotal)}`,
        sections: [
          {
            heading: 'بيانات القيد',
            rows: [
              { label: 'رقم القيد', value: entry.entryNumber },
              { label: 'البيان', value: entry.description },
              { label: 'التاريخ', value: formatDate(entry.entryDate) },
              { label: 'الحالة', value: journalEntryStatusLabel(entry.status) },
              { label: 'المدين', value: formatCurrency(entry.debitTotal) },
              { label: 'الدائن', value: formatCurrency(entry.creditTotal) }
            ]
          },
          {
            heading: 'السطور',
            rows: entry.lines.map((line) => ({
              label: `${line.accountCode} ${line.accountName}`,
              value: line.debit > 0 ? `مدين ${formatCurrency(line.debit)}` : `دائن ${formatCurrency(line.credit)}`
            }))
          }
        ]
      }
    : null;

  return (
    <AppShell title={entry ? entry.entryNumber : 'تفاصيل القيد'} summary={headerSummary}>
      {entryQuery.isLoading ? <LoadingState /> : null}
      {entryQuery.isError ? (
        <ErrorState message={getErrorMessage(entryQuery.error)} onRetry={() => void entryQuery.refetch()} />
      ) : null}

      {entry ? (
        <div className="page-stack">
          <section className="form-panel form-compact">
            <div className="compact-hero">
              <div>
                <p className="compact-hero__eyebrow">{entry.entryNumber}</p>
                <h2>{entry.description}</h2>
              </div>
              <span className={`status-pill status-pill--${getJournalEntryStatusTone(entry.status)}`}>
                {journalEntryStatusLabel(entry.status)}
              </span>
            </div>
            <dl className="detail-grid">
              <DetailItem label="التاريخ" value={formatDate(entry.entryDate)} />
              <DetailItem label="المصدر" value={entry.sourceTypeDisplay ?? 'يدوي'} />
              <DetailItem label="تاريخ الترحيل" value={entry.postedAt ? formatDate(entry.postedAt) : 'غير مُرحّل'} />
              <DetailItem label="المدين" value={formatCurrency(entry.debitTotal)} />
              <DetailItem label="الدائن" value={formatCurrency(entry.creditTotal)} />
            </dl>
          </section>

          <DocumentActions
            payload={exportPayload}
            pdfSource={{
              fileName: `قيد يومية - ${entry.entryNumber}.pdf`,
              load: () => getJournalEntryPdf(entryId)
            }}
          />

          <section className="form-panel form-compact">
            <h2>سطور القيد</h2>
            <div className="line-items">
              {entry.lines.map((line) => (
                <article className="line-item" key={line.id}>
                  <div className="line-item__head">
                    <strong>{line.accountCode}</strong>
                    <span className="form-hint">
                      {line.debit > 0 ? `مدين ${formatCurrency(line.debit)}` : `دائن ${formatCurrency(line.credit)}`}
                    </span>
                  </div>
                  <p className="line-item__meta">{line.accountName}</p>
                  {line.narrative ? <p className="form-hint">{line.narrative}</p> : null}
                </article>
              ))}
            </div>
          </section>

          <button className="ghost-button" type="button" onClick={() => navigate('/accounting')}>
            العودة إلى المحاسبة
          </button>
        </div>
      ) : null}
    </AppShell>
  );
}

function computeMetrics(rows: TrialBalanceLineDto[]) {
  let assets = 0;
  let liabilities = 0;
  let equity = 0;
  let revenue = 0;
  let expenses = 0;
  let totalDebits = 0;
  let totalCredits = 0;

  for (const row of rows) {
    totalDebits += row.debitTotal;
    totalCredits += row.creditTotal;
    switch (row.accountTypeDisplay) {
      case 'أصول':
        assets += row.balance;
        break;
      case 'خصوم':
        liabilities += row.balance;
        break;
      case 'حقوق ملكية':
        equity += row.balance;
        break;
      case 'إيرادات':
        revenue += row.balance;
        break;
      case 'مصروفات':
        expenses += row.balance;
        break;
      default:
        break;
    }
  }

  return {
    assets,
    liabilities,
    equity,
    revenue,
    expenses,
    netIncome: revenue - expenses,
    totalDebits,
    totalCredits,
    isBalanced: Math.abs(totalDebits - totalCredits) < 0.01
  };
}

function DetailItem({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <dt>{label}</dt>
      <dd>{value}</dd>
    </div>
  );
}

function getErrorMessage(error: unknown) {
  return getApiErrorMessage(error);
}
