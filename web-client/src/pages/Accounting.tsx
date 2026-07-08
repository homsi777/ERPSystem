import { useMemo, useState } from 'react';
import { useQuery, type UseQueryResult } from '@tanstack/react-query';
import { useNavigate, useParams } from 'react-router-dom';
import {
  getAccounts,
  getJournalEntries,
  getJournalEntry,
  getTrialBalance
} from '../api/accounting.ts';
import { ApiError } from '../api/client.ts';
import type { JournalEntryStatus, TrialBalanceLineDto } from '../api/types.ts';
import { AppShell } from '../components/AppShell.tsx';
import { EmptyState } from '../components/EmptyState.tsx';
import { ErrorState } from '../components/ErrorState.tsx';
import { LoadingState } from '../components/LoadingState.tsx';
import { SummaryCard } from '../components/SummaryCard.tsx';
import { formatCurrency, formatDate, formatDateOnly } from '../lib/format.ts';
import {
  getJournalEntryStatusTone,
  glAccountTypeLabel,
  journalEntryStatusLabel,
  journalEntryStatusOptions
} from '../lib/enums.ts';

const LIST_PAGE_SIZE = 100;

type AccountingTab = 'summary' | 'trial-balance' | 'journal' | 'accounts';

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
            <TabButton active={tab === 'summary'} onClick={() => setTab('summary')} label="الملخص المالي" />
            <TabButton active={tab === 'trial-balance'} onClick={() => setTab('trial-balance')} label="ميزان المراجعة" />
            <TabButton active={tab === 'journal'} onClick={() => setTab('journal')} label="القيود اليومية" />
            <TabButton active={tab === 'accounts'} onClick={() => setTab('accounts')} label="دليل الحسابات" />
          </div>
        </section>

        <section className="form-panel form-compact">
          {tab === 'summary' ? <SummaryTab query={trialBalanceQuery} metrics={metrics} /> : null}
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

function JournalTab() {
  const navigate = useNavigate();
  const [status, setStatus] = useState('');

  const journalQuery = useQuery({
    queryKey: ['accounting', 'journal', status],
    queryFn: () =>
      getJournalEntries({
        status: status === '' ? undefined : (Number(status) as JournalEntryStatus),
        page: 1,
        pageSize: LIST_PAGE_SIZE
      })
  });

  const rows = journalQuery.data?.items ?? [];

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

      {journalQuery.isLoading ? <LoadingState /> : null}
      {journalQuery.isError ? (
        <ErrorState message={getErrorMessage(journalQuery.error)} onRetry={() => void journalQuery.refetch()} />
      ) : null}
      {journalQuery.isSuccess && rows.length === 0 ? (
        <EmptyState title="لا توجد قيود" description="لا توجد قيود يومية مطابقة." />
      ) : null}

      {rows.length > 0 ? (
        <div className="table-scroll">
          <table className="data-table">
            <thead>
              <tr>
                <th>رقم القيد</th>
                <th>التاريخ</th>
                <th>الوصف</th>
                <th>مدين</th>
                <th>دائن</th>
                <th>الحالة</th>
              </tr>
            </thead>
            <tbody>
              {rows.map((entry) => (
                <tr key={entry.id} className="clickable-row" onClick={() => navigate(`/accounting/journal/${entry.id}`)}>
                  <td>{entry.entryNumber}</td>
                  <td>{formatDateOnly(entry.entryDate)}</td>
                  <td>{entry.description}</td>
                  <td>{formatCurrency(entry.debitTotal)}</td>
                  <td>{formatCurrency(entry.creditTotal)}</td>
                  <td>
                    <span className={`status-pill status-pill--${getJournalEntryStatusTone(entry.status)}`}>
                      {journalEntryStatusLabel(entry.status)}
                    </span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : null}
    </>
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
  if (error instanceof ApiError) {
    if (error.status === 403) {
      return 'لا تملك صلاحية للوصول إلى البيانات المحاسبية.';
    }
    return error.message;
  }
  return 'حدث خطأ غير متوقع.';
}
