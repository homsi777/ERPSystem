import { useEffect, useMemo, useState, type FormEvent } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useLocation, useNavigate, useParams, useSearchParams } from 'react-router-dom';
import {
  archiveExpense,
  cancelExpense,
  closeExpense,
  createExpense,
  deleteExpense,
  duplicateExpense,
  getExpenseCategories,
  getExpenseDashboard,
  getExpenseEntries,
  getExpenseOperationsCenter,
  getExpensePdf,
  getExpenseReport,
  getExpenseReportPdf,
  getExpenses,
  payExpense
} from '../api/expenses.ts';
import { getCashboxLookups } from '../api/lookups.ts';
import { ApiError } from '../api/client.ts';
import type {
  ExpenseEntryListDto,
  ExpenseLifecycleStepDto,
  ExpenseListDto,
  ExpenseOperationsCenterDto
} from '../api/types.ts';
import { useAuth } from '../auth/AuthContext.tsx';
import { AppShell } from '../components/AppShell.tsx';
import { DataCard } from '../components/DataCard.tsx';
import { DocumentActions } from '../components/DocumentActions.tsx';
import { EmptyState } from '../components/EmptyState.tsx';
import { ErrorState } from '../components/ErrorState.tsx';
import { Icon } from '../components/Icon.tsx';
import { LoadingState } from '../components/LoadingState.tsx';
import { SummaryCard } from '../components/SummaryCard.tsx';
import type { DocumentExportPayload } from '../lib/documentExport.ts';
import { formatCurrency, formatDateOnly, formatInteger, formatNumber, formatPercent } from '../lib/format.ts';
import { expenseCategoryKindLabel, expenseStatusLabel, getExpenseStatusTone } from '../lib/enums.ts';

const LIST_PAGE_SIZE = 100;
const BASE_CURRENCY = 'USD';

const EXPENSE_CURRENCIES = [
  { code: 'USD', label: 'دولار أمريكي (USD)' },
  { code: 'SYP', label: 'ليرة سورية (SYP)' },
  { code: 'SAR', label: 'ريال سعودي (SAR)' },
  { code: 'EUR', label: 'يورو (EUR)' },
  { code: 'CNY', label: 'يوان صيني (CNY)' }
] as const;

const REPORT_TYPES = [
  { value: 'Detailed', label: 'تفصيلي' },
  { value: 'Outstanding', label: 'المتبقي' },
  { value: 'UpcomingPayments', label: 'دفعات قادمة' },
  { value: 'OverduePayments', label: 'متأخرة' },
  { value: 'Recurring', label: 'متكررة' },
  { value: 'FundingSource', label: 'مصادر التمويل' }
] as const;

type ToastState = { tone: 'success' | 'error'; message: string };
type OpsTab = 'overview' | 'financial' | 'lifecycle' | 'payments' | 'installments' | 'audit' | 'timeline' | 'notes';

export function ExpensesPage() {
  const location = useLocation();
  const { expenseId } = useParams();

  if (expenseId) {
    return <OperationsCenterPage expenseId={expenseId} />;
  }
  if (location.pathname === '/expenses/dashboard') {
    return <DashboardPage />;
  }
  if (location.pathname === '/expenses/entries') {
    return <EntriesListPage />;
  }
  if (location.pathname === '/expenses/entry') {
    return <EntryFormPage />;
  }
  if (location.pathname === '/expenses/new') {
    return <DefinitionFormPage />;
  }
  if (location.pathname === '/expenses/categories') {
    return <CategoriesPage />;
  }
  if (location.pathname === '/expenses/reports') {
    return <ReportsPage />;
  }
  return <DefinitionsListPage />;
}

function ExpenseSubmoduleNav() {
  const location = useLocation();
  const items = [
    { path: '/expenses', label: 'التعريفات' },
    { path: '/expenses/entries', label: 'سجل القيود' },
    { path: '/expenses/entry', label: 'قيد جديد' },
    { path: '/expenses/new', label: 'تعريف جديد' },
    { path: '/expenses/dashboard', label: 'لوحة المصاريف' },
    { path: '/expenses/reports', label: 'التقارير' },
    { path: '/expenses/categories', label: 'الفئات' }
  ];

  return (
    <nav className="tab-strip" aria-label="أقسام المصاريف">
      {items.map((item) => (
        <Link
          key={item.path}
          className={`filter-chip ${location.pathname === item.path ? 'filter-chip--active' : ''}`}
          to={item.path}
        >
          {item.label}
        </Link>
      ))}
    </nav>
  );
}

function DefinitionsListPage() {
  const { can } = useAuth();
  const navigate = useNavigate();
  const [search, setSearch] = useState('');

  const expensesQuery = useQuery({
    queryKey: ['expenses', 'definitions', search],
    queryFn: () =>
      getExpenses({
        search: search.trim() || undefined,
        page: 1,
        pageSize: LIST_PAGE_SIZE
      })
  });

  const rows = expensesQuery.data?.items ?? [];

  return (
    <AppShell title="المصاريف — التعريفات">
      <div className="page-stack">
        <ExpenseSubmoduleNav />

        <section className="form-panel form-compact form-panel--filter">
          <div className="form-section-head">
            <h2>تعريفات المصاريف</h2>
            {can('expenses.create') ? (
              <button className="chip-button" type="button" onClick={() => navigate('/expenses/new')}>
                + تعريف جديد
              </button>
            ) : null}
          </div>
          <label className="form-field form-field--wide">
            <span className="form-field__label">بحث بالاسم أو الكود</span>
            <input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="بحث..." />
          </label>
        </section>

        {expensesQuery.isLoading ? <LoadingState /> : null}
        {expensesQuery.isError ? (
          <ErrorState message={getErrorMessage(expensesQuery.error)} onRetry={() => void expensesQuery.refetch()} />
        ) : null}
        {expensesQuery.isSuccess && rows.length === 0 ? (
          <>
            <EmptyState
              title="لا توجد تعريفات مصاريف"
              description="أنشئ تعريفاً أولاً ثم سجّل قيوداً يومية عليه."
            />
            {can('expenses.create') ? (
              <button className="primary-button" type="button" onClick={() => navigate('/expenses/new')}>
                تعريف جديد
              </button>
            ) : null}
          </>
        ) : null}

        {rows.length > 0 ? (
          <section className="card-list" aria-label="تعريفات المصاريف">
            {rows.map((expense) => (
              <Link className="card-link" key={expense.id} to={`/expenses/${expense.id}`}>
                <DefinitionCard expense={expense} />
              </Link>
            ))}
          </section>
        ) : null}

        {rows.length > 0 ? (
          <p className="form-hint">عرض {formatInteger(rows.length)} تعريف</p>
        ) : null}
      </div>
    </AppShell>
  );
}

function DefinitionCard({ expense }: { expense: ExpenseListDto }) {
  return (
    <DataCard
      icon={<Icon name="expenses" />}
      title={expense.name}
      subtitle={expense.code}
      meta={`${expense.categoryKindDisplay} • منذ ${formatDateOnly(expense.startDate)}`}
      value={
        <span>
          إجمالي المصروف: <strong>{formatCurrency(expense.paidAmountBase)}</strong>
        </span>
      }
      tone={expense.isArchived ? 'neutral' : 'available'}
    />
  );
}

function DefinitionFormPage() {
  const navigate = useNavigate();
  const { can } = useAuth();
  const [toast, setToast] = useState<ToastState | null>(null);
  const [name, setName] = useState('');
  const [notes, setNotes] = useState('');

  const categoriesQuery = useQuery({
    queryKey: ['expenses', 'categories'],
    queryFn: () => getExpenseCategories()
  });

  const defaultCategoryId = categoriesQuery.data?.[0]?.id ?? '';

  const mutation = useMutation({
    mutationFn: () =>
      createExpense({
        name: name.trim(),
        categoryId: defaultCategoryId,
        description: null,
        startDate: toIsoDate(toDateInputValue(new Date())),
        endDate: null,
        originalCurrency: BASE_CURRENCY,
        originalAmount: 0,
        exchangeRate: 1,
        baseCurrency: BASE_CURRENCY,
        paymentMethod: 1,
        payeeName: null,
        supplierId: null,
        costCenterId: null,
        department: null,
        notes: nullableText(notes),
        submitForApproval: false
      }),
    onSuccess: (id) => {
      navigate(`/expenses/entry?expenseId=${id}`);
    },
    onError: (error) => setToast({ tone: 'error', message: getErrorMessage(error) })
  });

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!name.trim()) {
      setToast({ tone: 'error', message: 'اسم المصروف مطلوب.' });
      return;
    }
    if (!defaultCategoryId) {
      setToast({ tone: 'error', message: 'لا توجد فئات مصاريف في النظام.' });
      return;
    }
    mutation.mutate();
  }

  if (!can('expenses.create')) {
    return (
      <AppShell title="تعريف مصروف جديد">
        <ErrorState message="لا تملك صلاحية إنشاء تعريفات المصاريف." onRetry={() => navigate('/expenses')} />
      </AppShell>
    );
  }

  return (
    <AppShell title="تعريف مصروف جديد">
      <Toast toast={toast} onClose={() => setToast(null)} />
      <form className="page-stack page-stack--footer" onSubmit={submit}>
        <ExpenseSubmoduleNav />
        <section className="form-panel form-compact">
          <p className="form-hint">تعريف المصروف (اسم + ملاحظات فقط). بعد الحفظ يمكنك تسجيل قيود يومية عليه.</p>
          <label className="form-field form-field--wide">
            <span className="form-field__label">اسم المصروف *</span>
            <input value={name} onChange={(event) => setName(event.target.value)} required />
          </label>
          <label className="form-field form-field--wide">
            <span className="form-field__label">ملاحظات (اختياري)</span>
            <input value={notes} onChange={(event) => setNotes(event.target.value)} />
          </label>
        </section>
        <div className="sticky-form-footer">
          <button className="primary-button sticky-form-footer__submit" type="submit" disabled={mutation.isPending || categoriesQuery.isLoading}>
            {mutation.isPending ? 'جار الحفظ...' : 'حفظ التعريف'}
          </button>
        </div>
      </form>
    </AppShell>
  );
}

function EntryFormPage() {
  const navigate = useNavigate();
  const { can } = useAuth();
  const [searchParams] = useSearchParams();
  const [toast, setToast] = useState<ToastState | null>(null);
  const [expenseId, setExpenseId] = useState(searchParams.get('expenseId') ?? '');
  const [cashboxId, setCashboxId] = useState('');
  const [description, setDescription] = useState('');
  const [currency, setCurrency] = useState(BASE_CURRENCY);
  const [amount, setAmount] = useState('0');
  const [exchangeRate, setExchangeRate] = useState('1');
  const [paymentDate, setPaymentDate] = useState(toDateInputValue(new Date()));

  const definitionsQuery = useQuery({
    queryKey: ['expenses', 'definitions'],
    queryFn: () => getExpenses({ page: 1, pageSize: LIST_PAGE_SIZE })
  });

  const cashboxesQuery = useQuery({
    queryKey: ['lookups', 'cashboxes'],
    queryFn: getCashboxLookups
  });

  useEffect(() => {
    const preselected = searchParams.get('expenseId');
    if (preselected) {
      setExpenseId(preselected);
    }
  }, [searchParams]);

  const isForeign = currency !== BASE_CURRENCY;
  const convertedUsd = useMemo(() => {
    const amountValue = toNumber(amount);
    const rateValue = toNumber(exchangeRate);
    if (!isForeign || amountValue <= 0 || rateValue <= 0) {
      return null;
    }
    return Math.round((amountValue / rateValue) * 100) / 100;
  }, [amount, exchangeRate, isForeign]);

  const mutation = useMutation({
    mutationFn: () => {
      const amountOriginal = toNumber(amount);
      const rate = isForeign ? toNumber(exchangeRate) : 1;
      const amountBase = isForeign ? Math.round((amountOriginal / rate) * 10000) / 10000 : amountOriginal;
      return payExpense(expenseId, {
        paymentDate: toIsoDate(paymentDate),
        amount: amountBase,
        amountOriginal,
        amountBase,
        exchangeRateSnapshot: rate,
        currency: isForeign ? currency : BASE_CURRENCY,
        paymentMethod: 1,
        fundingSource: 0,
        referenceNumber: null,
        notes: description.trim(),
        cashboxId
      });
    },
    onSuccess: () => {
      setDescription('');
      setAmount('0');
      setToast({ tone: 'success', message: isForeign && convertedUsd != null ? `تم تسجيل القيد: ${toNumber(amount)} ${currency} = ${convertedUsd} ${BASE_CURRENCY}` : 'تم تسجيل القيد.' });
      navigate('/expenses/entries');
    },
    onError: (error) => setToast({ tone: 'error', message: getErrorMessage(error) })
  });

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!expenseId) {
      setToast({ tone: 'error', message: 'اختر المصروف.' });
      return;
    }
    if (!cashboxId) {
      setToast({ tone: 'error', message: 'اختر الصندوق.' });
      return;
    }
    if (!description.trim()) {
      setToast({ tone: 'error', message: 'البيان مطلوب.' });
      return;
    }
    if (toNumber(amount) <= 0) {
      setToast({ tone: 'error', message: 'المبلغ يجب أن يكون أكبر من صفر.' });
      return;
    }
    if (isForeign && toNumber(exchangeRate) <= 0) {
      setToast({ tone: 'error', message: 'سعر الصرف يجب أن يكون أكبر من صفر.' });
      return;
    }
    mutation.mutate();
  }

  const definitions = definitionsQuery.data?.items ?? [];
  const canRecord = can('expenses.edit');

  return (
    <AppShell title="قيد مصروف جديد">
      <Toast toast={toast} onClose={() => setToast(null)} />
      <form className="page-stack page-stack--footer" onSubmit={submit}>
        <ExpenseSubmoduleNav />
        <section className="form-panel form-compact">
          <p className="form-hint">تسجيل حركة يومية على مصروف معرّف — نقداً من صندوق محدد.</p>

          {definitions.length === 0 && !definitionsQuery.isLoading ? (
            <p className="form-hint form-hint--warn">
              لا يوجد مصروف معرّف. <Link to="/expenses/new">أنشئ تعريفاً أولاً</Link>.
            </p>
          ) : null}

          <label className="form-field form-field--wide">
            <span className="form-field__label">المصروف *</span>
            <select value={expenseId} onChange={(event) => setExpenseId(event.target.value)} required>
              <option value="">اختر المصروف...</option>
              {definitions.map((item) => (
                <option key={item.id} value={item.id}>
                  {item.name} ({item.code})
                </option>
              ))}
            </select>
          </label>

          <label className="form-field form-field--wide">
            <span className="form-field__label">الصندوق *</span>
            <select value={cashboxId} onChange={(event) => setCashboxId(event.target.value)} required>
              <option value="">اختر الصندوق...</option>
              {(cashboxesQuery.data ?? []).map((box) => (
                <option key={box.id} value={box.id}>
                  {box.name}
                </option>
              ))}
            </select>
          </label>

          <label className="form-field form-field--wide">
            <span className="form-field__label">البيان *</span>
            <input value={description} onChange={(event) => setDescription(event.target.value)} placeholder="وصف الحركة" required />
          </label>

          <div className="form-field-row form-field-row--2">
            <label className="form-field">
              <span className="form-field__label">العملة</span>
              <select value={currency} onChange={(event) => setCurrency(event.target.value)}>
                {EXPENSE_CURRENCIES.map((item) => (
                  <option key={item.code} value={item.code}>
                    {item.label}
                  </option>
                ))}
              </select>
            </label>
            <label className="form-field">
              <span className="form-field__label">التاريخ</span>
              <input type="date" value={paymentDate} onChange={(event) => setPaymentDate(event.target.value)} />
            </label>
          </div>

          {isForeign ? (
            <label className="form-field form-field--wide">
              <span className="form-field__label">سعر الصرف ({currency} لكل 1 دولار) *</span>
              <input inputMode="decimal" value={exchangeRate} onChange={(event) => setExchangeRate(event.target.value)} />
            </label>
          ) : null}

          <label className="form-field form-field--wide">
            <span className="form-field__label">
              {isForeign ? `المبلغ بـ${currencyLabel(currency)} *` : 'المبلغ بالدولار (USD) *'}
            </span>
            <input inputMode="decimal" value={amount} onChange={(event) => setAmount(event.target.value)} />
          </label>

          {convertedUsd != null ? (
            <p className="form-hint">المعادل بالدولار: {formatCurrency(convertedUsd)}</p>
          ) : null}

          {!canRecord ? (
            <p className="form-hint form-hint--warn">لا تملك صلاحية تسجيل قيود.</p>
          ) : null}
        </section>

        <div className="sticky-form-footer">
          <button className="ghost-button" type="button" onClick={() => navigate('/expenses/new')}>
            تعريف جديد
          </button>
          <button className="primary-button sticky-form-footer__submit" type="submit" disabled={mutation.isPending || !canRecord || definitions.length === 0}>
            {mutation.isPending ? 'جار الحفظ...' : 'تسجيل القيد'}
          </button>
        </div>
      </form>
    </AppShell>
  );
}

function EntriesListPage() {
  const { can } = useAuth();
  const navigate = useNavigate();
  const [search, setSearch] = useState('');
  const [expenseId, setExpenseId] = useState('');
  const [from, setFrom] = useState(toDateInputValue(new Date(Date.now() - 30 * 24 * 60 * 60 * 1000)));
  const [to, setTo] = useState(toDateInputValue(new Date()));

  const definitionsQuery = useQuery({
    queryKey: ['expenses', 'definitions'],
    queryFn: () => getExpenses({ page: 1, pageSize: LIST_PAGE_SIZE })
  });

  const entriesQuery = useQuery({
    queryKey: ['expenses', 'entries', search, expenseId, from, to],
    queryFn: () =>
      getExpenseEntries({
        search: search.trim() || undefined,
        expenseId: expenseId || undefined,
        from: from ? toIsoDate(from) : undefined,
        to: to ? toIsoDate(to) : undefined,
        page: 1,
        pageSize: LIST_PAGE_SIZE
      })
  });

  const rows = entriesQuery.data?.items ?? [];

  return (
    <AppShell title="سجل قيود المصاريف">
      <div className="page-stack">
        <ExpenseSubmoduleNav />

        <section className="form-panel form-compact form-panel--filter">
          <div className="form-section-head">
            <h2>سجل القيود</h2>
            {can('expenses.edit') ? (
              <button className="chip-button" type="button" onClick={() => navigate('/expenses/entry')}>
                + قيد جديد
              </button>
            ) : null}
          </div>
          <div className="form-field-row form-field-row--2">
            <label className="form-field">
              <span className="form-field__label">من</span>
              <input type="date" value={from} onChange={(event) => setFrom(event.target.value)} />
            </label>
            <label className="form-field">
              <span className="form-field__label">إلى</span>
              <input type="date" value={to} onChange={(event) => setTo(event.target.value)} />
            </label>
          </div>
          <label className="form-field form-field--wide">
            <span className="form-field__label">المصروف</span>
            <select value={expenseId} onChange={(event) => setExpenseId(event.target.value)}>
              <option value="">كل المصاريف</option>
              {(definitionsQuery.data?.items ?? []).map((item) => (
                <option key={item.id} value={item.id}>
                  {item.name}
                </option>
              ))}
            </select>
          </label>
          <label className="form-field form-field--wide">
            <span className="form-field__label">بحث</span>
            <input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="بحث في البيان..." />
          </label>
        </section>

        {entriesQuery.isLoading ? <LoadingState /> : null}
        {entriesQuery.isError ? (
          <ErrorState message={getErrorMessage(entriesQuery.error)} onRetry={() => void entriesQuery.refetch()} />
        ) : null}
        {entriesQuery.isSuccess && rows.length === 0 ? (
          <EmptyState title="لا توجد قيود" description="لم يتم العثور على قيود مطابقة للفلتر." />
        ) : null}

        {rows.length > 0 ? (
          <section className="card-list" aria-label="قيود المصاريف">
            {rows.map((entry) => (
              <EntryCard key={entry.id} entry={entry} />
            ))}
          </section>
        ) : null}

        {rows.length > 0 ? <p className="form-hint">عرض {formatInteger(rows.length)} قيد</p> : null}
      </div>
    </AppShell>
  );
}

function EntryCard({ entry }: { entry: ExpenseEntryListDto }) {
  return (
    <DataCard
      icon={<Icon name="expenses" />}
      title={entry.expenseName}
      subtitle={entry.expenseCode}
      meta={`${formatDateOnly(entry.paymentDate)} • ${entry.cashboxName ?? '—'}`}
      value={
        <span>
          {formatNumber(entry.amountOriginal)} {entry.currency}
          {entry.currency !== BASE_CURRENCY ? ` = ${formatCurrency(entry.amountBase)}` : null}
        </span>
      }
      tone="neutral"
    />
  );
}

function DashboardPage() {
  const dashboardQuery = useQuery({
    queryKey: ['expenses', 'dashboard'],
    queryFn: () => getExpenseDashboard()
  });

  const d = dashboardQuery.data;
  const headerSummary = d ? (
    <>
      <SummaryCard label="مصاريف الشهر" value={formatCurrency(d.monthlyExpensesBase)} tone="amber" />
      <SummaryCard label="إجمالي المصاريف" value={formatCurrency(d.totalExpensesBase)} />
      <SummaryCard label="معدل الحرق" value={formatCurrency(d.burnRateMonthly)} tone="green" />
    </>
  ) : undefined;

  const kpis = d
    ? [
        ['إجمالي المصاريف', formatCurrency(d.totalExpensesBase)],
        ['مصاريف الشهر', formatCurrency(d.monthlyExpensesBase)],
        ['رأسمالية', formatCurrency(d.capitalExpensesBase)],
        ['شخصية', formatCurrency(d.personalExpensesBase)],
        ['تشغيلية', formatCurrency(d.operatingExpensesBase)],
        ['نشطة', formatNumber(d.activeCount)],
        ['بانتظار الاعتماد', formatNumber(d.pendingApprovalCount)],
        ['معدل الحرق الشهري', formatCurrency(d.burnRateMonthly)],
        ['سنوي', formatCurrency(d.yearlyExpensesBase)],
        ['دفعات قادمة', formatNumber(d.upcomingPaymentsCount)],
        ['متأخرة', formatNumber(d.overdueCount)],
        ['أكبر مصروف', d.largestExpenseBase > 0 ? formatCurrency(d.largestExpenseBase) : '—']
      ]
    : [];

  return (
    <AppShell title="لوحة المصاريف" summary={headerSummary}>
      <div className="page-stack">
        <ExpenseSubmoduleNav />
        {dashboardQuery.isLoading ? <LoadingState /> : null}
        {dashboardQuery.isError ? (
          <ErrorState message={getErrorMessage(dashboardQuery.error)} onRetry={() => void dashboardQuery.refetch()} />
        ) : null}
        {d ? (
          <>
            <section className="form-panel form-compact">
              <h2>مؤشرات رئيسية</h2>
              <dl className="detail-grid">
                {kpis.map(([label, value]) => (
                  <DetailItem key={label} label={label} value={value} />
                ))}
              </dl>
            </section>
            {d.categoryBreakdown.length > 0 ? (
              <section className="form-panel form-compact">
                <h2>حسب الفئة</h2>
                <div className="line-list">
                  {d.categoryBreakdown.map((item) => (
                    <div className="price-row" key={item.label}>
                      <span>{item.label}</span>
                      <strong>{formatCurrency(item.amountBase)} ({formatPercent(item.percentage)})</strong>
                    </div>
                  ))}
                </div>
              </section>
            ) : null}
            {d.largestExpenseName ? (
              <p className="form-hint">أكبر مصروف: {d.largestExpenseName}</p>
            ) : null}
          </>
        ) : null}
      </div>
    </AppShell>
  );
}

function CategoriesPage() {
  const categoriesQuery = useQuery({
    queryKey: ['expenses', 'categories'],
    queryFn: () => getExpenseCategories()
  });

  const rows = categoriesQuery.data ?? [];

  return (
    <AppShell title="فئات المصاريف">
      <div className="page-stack">
        <ExpenseSubmoduleNav />
        <p className="form-hint">عرض فقط — الفئات تُدار من النظام.</p>
        {categoriesQuery.isLoading ? <LoadingState /> : null}
        {categoriesQuery.isError ? (
          <ErrorState message={getErrorMessage(categoriesQuery.error)} onRetry={() => void categoriesQuery.refetch()} />
        ) : null}
        {rows.length > 0 ? (
          <section className="card-list">
            {rows.map((category) => (
              <DataCard
                key={category.id}
                icon={<Icon name="expenses" />}
                title={category.nameAr}
                subtitle={category.code}
                meta={expenseCategoryKindLabel(category.kind)}
                value={category.kindDisplay}
                tone="neutral"
              />
            ))}
          </section>
        ) : (
          <EmptyState title="لا توجد فئات" />
        )}
      </div>
    </AppShell>
  );
}

function ReportsPage() {
  const [reportType, setReportType] = useState('Detailed');
  const [from, setFrom] = useState('');
  const [to, setTo] = useState('');
  const [toast, setToast] = useState<ToastState | null>(null);

  const reportQuery = useQuery({
    queryKey: ['expenses', 'report', reportType, from, to],
    queryFn: () =>
      getExpenseReport({
        reportType,
        from: from ? toIsoDate(from) : undefined,
        to: to ? toIsoDate(to) : undefined
      })
  });

  const report = reportQuery.data;

  const exportPayload: DocumentExportPayload | null = report
    ? {
        title: report.title || 'تقرير مصاريف',
        subtitle: `${formatNumber(report.expenseCount)} مصروف — الإجمالي ${formatCurrency(report.totalBase)}`,
        fileName: `تقرير مصاريف - ${new Date().toISOString().slice(0, 10)}.pdf`,
        shareText: `${report.title}\nعدد المصاريف: ${report.expenseCount}\nالإجمالي: ${formatCurrency(report.totalBase)}\nالمدفوع: ${formatCurrency(report.totalPaidBase)}\nالمتبقي: ${formatCurrency(report.totalRemainingBase)}`,
        sections: [
          {
            heading: 'الملخص',
            rows: [
              { label: 'عدد المصاريف', value: formatNumber(report.expenseCount) },
              { label: 'الإجمالي', value: formatCurrency(report.totalBase) },
              { label: 'المدفوع', value: formatCurrency(report.totalPaidBase) },
              { label: 'المتبقي', value: formatCurrency(report.totalRemainingBase) }
            ]
          }
        ]
      }
    : null;

  return (
    <AppShell title="تقارير المصاريف">
      <div className="page-stack">
        <ExpenseSubmoduleNav />
        <section className="form-panel form-compact form-panel--filter">
          <label className="form-field form-field--wide">
            <span className="form-field__label">نوع التقرير</span>
            <select value={reportType} onChange={(event) => setReportType(event.target.value)}>
              {REPORT_TYPES.map((item) => (
                <option key={item.value} value={item.value}>
                  {item.label}
                </option>
              ))}
            </select>
          </label>
          <div className="form-field-row form-field-row--2">
            <label className="form-field">
              <span className="form-field__label">من</span>
              <input type="date" value={from} onChange={(event) => setFrom(event.target.value)} />
            </label>
            <label className="form-field">
              <span className="form-field__label">إلى</span>
              <input type="date" value={to} onChange={(event) => setTo(event.target.value)} />
            </label>
          </div>
        </section>

        {reportQuery.isLoading ? <LoadingState /> : null}
        {reportQuery.isError ? (
          <ErrorState message={getErrorMessage(reportQuery.error)} onRetry={() => void reportQuery.refetch()} />
        ) : null}

        {report ? (
          <>
            <section className="form-panel form-compact">
              <h2>{report.title}</h2>
              <dl className="detail-grid">
                <DetailItem label="عدد المصاريف" value={formatNumber(report.expenseCount)} />
                <DetailItem label="الإجمالي" value={formatCurrency(report.totalBase)} />
                <DetailItem label="المدفوع" value={formatCurrency(report.totalPaidBase)} />
                <DetailItem label="المتبقي" value={formatCurrency(report.totalRemainingBase)} />
              </dl>
            </section>
            {report.rows.length > 0 ? (
              <section className="form-panel form-compact">
                <div className="table-scroll">
                  <table className="data-table">
                    <thead>
                      <tr>
                        <th>الكود</th>
                        <th>الاسم</th>
                        <th>الفئة</th>
                        <th>المدفوع</th>
                        <th>المتبقي</th>
                      </tr>
                    </thead>
                    <tbody>
                      {report.rows.map((row) => (
                        <tr key={row.expenseId}>
                          <td>{row.code}</td>
                          <td>{row.name}</td>
                          <td>{row.category}</td>
                          <td>{formatCurrency(row.paidAmountBase)}</td>
                          <td>{formatCurrency(row.remainingBalanceBase)}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </section>
            ) : (
              <EmptyState title="لا توجد بيانات" />
            )}
            <DocumentActions
              payload={exportPayload}
              pdfSource={{
                fileName: `تقرير مصاريف - ${new Date().toISOString().slice(0, 10)}.pdf`,
                load: () =>
                  getExpenseReportPdf({
                    reportType,
                    from: from ? toIsoDate(from) : undefined,
                    to: to ? toIsoDate(to) : undefined
                  })
              }}
              onToast={(message, tone = 'success') => setToast({ tone, message })}
            />
          </>
        ) : null}
        <Toast toast={toast} onClose={() => setToast(null)} />
      </div>
    </AppShell>
  );
}

function OperationsCenterPage({ expenseId }: { expenseId: string }) {
  const navigate = useNavigate();
  const { can } = useAuth();
  const queryClient = useQueryClient();
  const [toast, setToast] = useState<ToastState | null>(null);
  const [tab, setTab] = useState<OpsTab>('overview');

  const opsQuery = useQuery({
    queryKey: ['expense', 'ops', expenseId],
    queryFn: () => getExpenseOperationsCenter(expenseId)
  });

  async function refresh(message: string) {
    await queryClient.invalidateQueries({ queryKey: ['expense', 'ops', expenseId] });
    await queryClient.invalidateQueries({ queryKey: ['expenses'] });
    setToast({ tone: 'success', message });
  }

  const duplicateMutation = useMutation({
    mutationFn: () => duplicateExpense(expenseId),
    onSuccess: (newId) => navigate(`/expenses/${newId}`),
    onError: (error) => setToast({ tone: 'error', message: getErrorMessage(error) })
  });

  const closeMutation = useMutation({
    mutationFn: (reason: string) => closeExpense(expenseId, reason),
    onSuccess: () => void refresh('تم إغلاق المصروف.'),
    onError: (error) => setToast({ tone: 'error', message: getErrorMessage(error) })
  });

  const cancelMutation = useMutation({
    mutationFn: (reason: string) => cancelExpense(expenseId, reason),
    onSuccess: () => void refresh('تم إلغاء المصروف.'),
    onError: (error) => setToast({ tone: 'error', message: getErrorMessage(error) })
  });

  const archiveMutation = useMutation({
    mutationFn: (reason: string) => archiveExpense(expenseId, reason),
    onSuccess: () => void refresh('تم أرشفة المصروف.'),
    onError: (error) => setToast({ tone: 'error', message: getErrorMessage(error) })
  });

  const deleteMutation = useMutation({
    mutationFn: () => deleteExpense(expenseId),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['expenses'] });
      navigate('/expenses');
    },
    onError: (error) => setToast({ tone: 'error', message: getErrorMessage(error) })
  });

  function runWithReason(action: (reason: string) => void) {
    const reason = window.prompt('السبب (اختياري):', '') ?? '';
    action(reason.trim());
  }

  const ops = opsQuery.data;
  const expense = ops?.details;
  const lifecycleSteps = normalizeLifecycleSteps(ops?.lifecycleSteps);

  const headerSummary = expense ? (
    <>
      <SummaryCard label="المدفوع" value={formatCurrency(expense.paidAmountBase)} tone="green" />
      <SummaryCard label="المتبقي" value={formatCurrency(expense.remainingBalanceBase)} tone="amber" />
      <SummaryCard label="القيود" value={formatNumber(ops?.statistics.totalPayments ?? 0)} />
    </>
  ) : undefined;

  const exportPayload: DocumentExportPayload | null = expense
    ? {
        title: `مصروف ${expense.code}`,
        subtitle: expense.name,
        fileName: `expense-${expense.code}.pdf`,
        shareText: `مصروف: ${expense.name}\nالكود: ${expense.code}\nالمدفوع: ${formatCurrency(expense.paidAmountBase)}\nالمتبقي: ${formatCurrency(expense.remainingBalanceBase)}`,
        sections: [
          {
            heading: 'بيانات المصروف',
            rows: [
              { label: 'الاسم', value: expense.name },
              { label: 'الكود', value: expense.code },
              { label: 'التصنيف', value: expense.categoryName },
              { label: 'الحالة', value: expense.statusDisplay },
              { label: 'المدفوع', value: formatCurrency(expense.paidAmountBase) },
              { label: 'المتبقي', value: formatCurrency(expense.remainingBalanceBase) },
              { label: 'ملاحظات', value: expense.notes?.trim() || '—' }
            ]
          },
          {
            heading: 'الدفعات',
            rows: (expense.payments ?? []).map((payment, index) => ({
              label: `دفعة ${formatInteger(index + 1)}`,
              value: `${formatDateOnly(payment.paymentDate)} · ${formatCurrency(payment.amountBase)} · ${payment.notes ?? '—'}`
            }))
          }
        ]
      }
    : null;

  return (
    <AppShell title={expense ? expense.name : 'مركز عمل المصروف'} summary={headerSummary}>
      <Toast toast={toast} onClose={() => setToast(null)} />
      <ExpenseSubmoduleNav />

      {opsQuery.isLoading ? <LoadingState /> : null}
      {opsQuery.isError ? (
        <ErrorState message={getErrorMessage(opsQuery.error)} onRetry={() => void opsQuery.refetch()} />
      ) : null}

      {expense && ops ? (
        <div className="page-stack">
          <section className="form-panel form-compact">
            <div className="compact-hero">
              <div>
                <p className="compact-hero__eyebrow">{expense.code}</p>
                <h2>{expense.name}</h2>
              </div>
              <span className={`status-pill status-pill--${getExpenseStatusTone(expense.status)}`}>
                {expenseStatusLabel(expense.status)}
              </span>
            </div>
          </section>

          <DocumentActions
            payload={exportPayload}
            pdfSource={{
              fileName: `مصروف - ${expense.code} - ${new Date().toISOString().slice(0, 10)}.pdf`,
              load: () => getExpensePdf(expenseId)
            }}
            onToast={(message, tone = 'success') => setToast({ tone, message })}
          />

          <section className="compact-action-row">
            {can('expenses.edit') ? (
              <Link className="chip-button" to={`/expenses/entry?expenseId=${expenseId}`}>
                قيد جديد
              </Link>
            ) : null}
            {can('expenses.create') ? (
              <button className="chip-button" type="button" onClick={() => duplicateMutation.mutate()} disabled={duplicateMutation.isPending}>
                نسخ
              </button>
            ) : null}
            {can('expenses.archive') ? (
              <button className="chip-button" type="button" onClick={() => runWithReason((r) => archiveMutation.mutate(r))}>
                أرشفة
              </button>
            ) : null}
            <button className="chip-button" type="button" onClick={() => runWithReason((r) => closeMutation.mutate(r))}>
              إغلاق
            </button>
            <button className="chip-button" type="button" onClick={() => runWithReason((r) => cancelMutation.mutate(r))}>
              إلغاء
            </button>
            {can('expenses.delete') ? (
              <button
                className="chip-button"
                type="button"
                onClick={() => {
                  if (window.confirm('هل تريد حذف هذا المصروف؟')) {
                    deleteMutation.mutate();
                  }
                }}
              >
                حذف
              </button>
            ) : null}
          </section>

          <section className="form-panel form-compact">
            <div className="tab-strip" role="tablist">
              <OpsTabButton active={tab === 'overview'} onClick={() => setTab('overview')} label="نظرة عامة" />
              <OpsTabButton active={tab === 'financial'} onClick={() => setTab('financial')} label="الملخص المالي" />
              <OpsTabButton active={tab === 'lifecycle'} onClick={() => setTab('lifecycle')} label="دورة الحياة" />
              <OpsTabButton active={tab === 'payments'} onClick={() => setTab('payments')} label="الدفعات" />
              <OpsTabButton active={tab === 'installments'} onClick={() => setTab('installments')} label="الأقساط" />
              <OpsTabButton active={tab === 'audit'} onClick={() => setTab('audit')} label="التدقيق" />
              <OpsTabButton active={tab === 'timeline'} onClick={() => setTab('timeline')} label="الخط الزمني" />
              <OpsTabButton active={tab === 'notes'} onClick={() => setTab('notes')} label="ملاحظات" />
            </div>

            {tab === 'overview' ? <OverviewTab expense={expense} ops={ops} /> : null}
            {tab === 'financial' ? <FinancialTab ops={ops} /> : null}
            {tab === 'lifecycle' ? <LifecycleTab steps={lifecycleSteps} /> : null}
            {tab === 'payments' ? <PaymentsTab expense={expense} /> : null}
            {tab === 'installments' ? <InstallmentsTab expense={expense} /> : null}
            {tab === 'audit' ? <AuditTab entries={ops.recentAudit} /> : null}
            {tab === 'timeline' ? <TimelineTab events={ops.timeline} /> : null}
            {tab === 'notes' ? <NotesTab expense={expense} /> : null}
          </section>

          <button className="ghost-button" type="button" onClick={() => navigate('/expenses')}>
            العودة إلى التعريفات
          </button>
        </div>
      ) : null}
    </AppShell>
  );
}

function OverviewTab({ expense, ops }: { expense: ExpenseOperationsCenterDto['details']; ops: ExpenseOperationsCenterDto }) {
  return (
    <dl className="detail-grid">
      <DetailItem label="التصنيف" value={expense.categoryName} />
      <DetailItem label="النوع" value={expense.categoryKindDisplay} />
      <DetailItem label="الحالة" value={expense.statusDisplay} />
      <DetailItem label="تاريخ الإنشاء" value={formatDateOnly(expense.createdAt)} />
      <DetailItem label="أنشئ بواسطة" value={expense.createdByName ?? '—'} />
      <DetailItem label="إجمالي الدفعات" value={formatNumber(ops.statistics.totalPayments)} />
      <DetailItem label="أيام منذ الإنشاء" value={formatNumber(ops.statistics.daysSinceCreated)} />
      <DetailItem label="أحداث التدقيق" value={formatNumber(ops.statistics.auditEventCount)} />
    </dl>
  );
}

function FinancialTab({ ops }: { ops: ExpenseOperationsCenterDto }) {
  const f = ops.financial;
  return (
    <dl className="detail-grid">
      <DetailItem label="المبلغ الأصلي" value={`${formatNumber(f.originalAmount)} ${f.originalCurrency}`} />
      <DetailItem label="المبلغ بالأساس" value={formatCurrency(f.baseAmount)} />
      <DetailItem label="المدفوع" value={formatCurrency(f.paidAmountBase)} />
      <DetailItem label="المتبقي" value={formatCurrency(f.remainingBalanceBase)} />
      <DetailItem label="سعر الصرف" value={formatNumber(f.exchangeRate)} />
      <DetailItem label="دفعات مكتملة" value={formatNumber(f.completedPayments)} />
      <DetailItem label="دفعات مجدولة" value={formatNumber(f.scheduledPayments)} />
      <DetailItem label="أقساط معلقة" value={formatNumber(f.pendingInstallments)} />
    </dl>
  );
}

function LifecycleTab({ steps }: { steps: ExpenseLifecycleStepDto[] }) {
  if (steps.length === 0) {
    return <p className="form-hint">لا توجد خطوات دورة حياة.</p>;
  }
  return (
    <ol className="line-list">
      {steps.map((step) => (
        <li key={step.label} className="price-row">
          <span>
            {step.completed ? '✓' : step.current ? '●' : '○'} {step.label}
          </span>
        </li>
      ))}
    </ol>
  );
}

function PaymentsTab({ expense }: { expense: ExpenseOperationsCenterDto['details'] }) {
  if (expense.payments.length === 0) {
    return <EmptyState title="لا توجد دفعات" />;
  }
  return (
    <div className="table-scroll">
      <table className="data-table">
        <thead>
          <tr>
            <th>التاريخ</th>
            <th>المبلغ</th>
            <th>العملة</th>
            <th>البيان</th>
            <th>الحالة</th>
          </tr>
        </thead>
        <tbody>
          {expense.payments.map((payment) => (
            <tr key={payment.id}>
              <td>{formatDateOnly(payment.paymentDate)}</td>
              <td>{formatCurrency(payment.amountBase)}</td>
              <td>{payment.currency}</td>
              <td>{payment.notes ?? '—'}</td>
              <td>{payment.statusDisplay}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function InstallmentsTab({ expense }: { expense: ExpenseOperationsCenterDto['details'] }) {
  const installments = expense.installments ?? [];
  if (installments.length === 0) {
    return <EmptyState title="لا توجد أقساط" />;
  }
  return (
    <div className="table-scroll">
      <table className="data-table">
        <thead>
          <tr>
            <th>#</th>
            <th>الاستحقاق</th>
            <th>المبلغ</th>
            <th>الحالة</th>
          </tr>
        </thead>
        <tbody>
          {installments.map((item) => (
            <tr key={item.id}>
              <td>{item.installmentNumber}</td>
              <td>{formatDateOnly(item.dueDate)}</td>
              <td>{formatCurrency(item.amountBase)}</td>
              <td>{item.statusDisplay}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function AuditTab({ entries }: { entries: ExpenseOperationsCenterDto['recentAudit'] }) {
  if (entries.length === 0) {
    return <EmptyState title="لا يوجد سجل تدقيق" />;
  }
  return (
    <div className="line-list">
      {entries.map((entry, index) => (
        <div className="record-card" key={`${entry.timestamp}-${index}`}>
          <strong>{entry.action}</strong>
          <span>{entry.userName} • {formatDateOnly(entry.timestamp)}</span>
          {entry.fieldName ? <span>{entry.fieldName}: {entry.previousValue ?? '—'} → {entry.newValue ?? '—'}</span> : null}
          {entry.reason ? <span className="form-hint">{entry.reason}</span> : null}
        </div>
      ))}
    </div>
  );
}

function TimelineTab({ events }: { events: ExpenseOperationsCenterDto['timeline'] }) {
  if (events.length === 0) {
    return <EmptyState title="لا توجد أحداث" />;
  }
  return (
    <div className="line-list">
      {events.map((event, index) => (
        <div className="record-card" key={`${event.timestamp}-${index}`}>
          <strong>{event.title}</strong>
          <span>{event.userName} • {formatDateOnly(event.timestamp)}</span>
          {event.description ? <span>{event.description}</span> : null}
        </div>
      ))}
    </div>
  );
}

function NotesTab({ expense }: { expense: ExpenseOperationsCenterDto['details'] }) {
  return (
    <div className="form-panel form-compact">
      <p>{expense.notes?.trim() || 'لا توجد ملاحظات.'}</p>
      {expense.description ? <p className="form-hint">{expense.description}</p> : null}
    </div>
  );
}

function OpsTabButton({ active, onClick, label }: { active: boolean; onClick: () => void; label: string }) {
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

function DetailItem({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <dt>{label}</dt>
      <dd>{value}</dd>
    </div>
  );
}

function Toast({ toast, onClose }: { toast: ToastState | null; onClose: () => void }) {
  if (!toast) {
    return null;
  }
  return (
    <div className={`toast toast--${toast.tone}`} role="status">
      <span>{toast.message}</span>
      <button type="button" onClick={onClose} aria-label="إغلاق">×</button>
    </div>
  );
}

function normalizeLifecycleSteps(raw: unknown): ExpenseLifecycleStepDto[] {
  if (!Array.isArray(raw)) {
    return [];
  }
  return raw.map((item) => {
    if (Array.isArray(item)) {
      return { label: String(item[0] ?? ''), completed: Boolean(item[1]), current: Boolean(item[2]) };
    }
    const obj = item as Record<string, unknown>;
    return {
      label: String(obj.label ?? obj.item1 ?? ''),
      completed: Boolean(obj.completed ?? obj.item2),
      current: Boolean(obj.current ?? obj.item3)
    };
  });
}

function currencyLabel(code: string) {
  return EXPENSE_CURRENCIES.find((item) => item.code === code)?.label ?? code;
}

function toNumber(value: string) {
  const normalized = Number(value.replace(',', '.'));
  return Number.isFinite(normalized) ? normalized : 0;
}

function nullableText(value: string) {
  const trimmed = value.trim();
  return trimmed.length > 0 ? trimmed : null;
}

function toDateInputValue(date: Date) {
  return date.toISOString().slice(0, 10);
}

function toIsoDate(value: string) {
  return new Date(`${value}T00:00:00`).toISOString();
}

function getErrorMessage(error: unknown) {
  if (error instanceof ApiError) {
    if (error.status === 403) {
      return 'لا تملك صلاحية لهذا الإجراء.';
    }
    if (error.status === 404) {
      return 'المصروف غير موجود.';
    }
    if (error.status === 409) {
      return `تعذّر تنفيذ الإجراء: ${error.message}`;
    }
    return error.message;
  }
  return 'حدث خطأ غير متوقع.';
}
