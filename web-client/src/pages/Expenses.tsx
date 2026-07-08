import { useMemo, useState, type FormEvent } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useLocation, useNavigate, useParams } from 'react-router-dom';
import {
  approveExpense,
  createExpense,
  deleteExpense,
  getExpense,
  getExpenseCategories,
  getExpenseCostCenters,
  getExpenseDashboard,
  getExpenses,
  payExpense,
  rejectExpense
} from '../api/expenses.ts';
import { getCashboxLookups } from '../api/lookups.ts';
import { ApiError } from '../api/client.ts';
import type { ExpenseListDto, ExpensePaymentMethod, ExpenseStatus } from '../api/types.ts';
import { useAuth } from '../auth/AuthContext.tsx';
import { AppShell } from '../components/AppShell.tsx';
import { DataCard } from '../components/DataCard.tsx';
import { EmptyState } from '../components/EmptyState.tsx';
import { ErrorState } from '../components/ErrorState.tsx';
import { Icon } from '../components/Icon.tsx';
import { LoadingState } from '../components/LoadingState.tsx';
import { SummaryCard } from '../components/SummaryCard.tsx';
import { formatCurrency, formatDateOnly, formatNumber } from '../lib/format.ts';
import {
  expenseCategoryKindLabel,
  expensePaymentMethodOptions,
  expenseStatusLabel,
  expenseStatusOptions,
  getExpenseStatusTone
} from '../lib/enums.ts';

const LIST_PAGE_SIZE = 100;

type ToastState = { tone: 'success' | 'error'; message: string };

export function ExpensesPage() {
  const { expenseId } = useParams();
  const location = useLocation();

  if (location.pathname === '/expenses/new') {
    return <ExpenseCreatePage />;
  }
  if (expenseId) {
    return <ExpenseDetailPage expenseId={expenseId} />;
  }
  return <ExpenseListPage />;
}

function ExpenseListPage() {
  const { can } = useAuth();
  const navigate = useNavigate();
  const [status, setStatus] = useState('');

  const dashboardQuery = useQuery({
    queryKey: ['expenses', 'dashboard'],
    queryFn: () => getExpenseDashboard()
  });

  const expensesQuery = useQuery({
    queryKey: ['expenses', 'list', status],
    queryFn: () =>
      getExpenses({
        status: status === '' ? undefined : (Number(status) as ExpenseStatus),
        page: 1,
        pageSize: LIST_PAGE_SIZE
      })
  });

  const rows = expensesQuery.data?.items ?? [];
  const dashboard = dashboardQuery.data;

  const headerSummary = (
    <>
      <SummaryCard label="مصاريف الشهر" value={formatCurrency(dashboard?.monthlyExpensesBase ?? 0)} tone="amber" />
      <SummaryCard label="بانتظار الاعتماد" value={formatNumber(dashboard?.pendingApprovalCount ?? 0)} />
      <SummaryCard label="إجمالي المصاريف" value={formatCurrency(dashboard?.totalExpensesBase ?? 0)} />
    </>
  );

  return (
    <AppShell title="المصاريف" summary={headerSummary}>
      <div className="page-stack">
        <section className="form-panel form-compact form-panel--filter">
          <div className="form-section-head">
            <h2>التصفية</h2>
            {can('expenses.create') ? (
              <button className="chip-button" type="button" onClick={() => navigate('/expenses/new')}>
                + مصروف جديد
              </button>
            ) : null}
          </div>
          <label className="form-field form-field--wide">
            <span className="form-field__label">الحالة</span>
            <select value={status} onChange={(event) => setStatus(event.target.value)}>
              <option value="">كل الحالات</option>
              {expenseStatusOptions.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </label>
        </section>

      {expensesQuery.isLoading ? <LoadingState /> : null}
      {expensesQuery.isError ? (
        <ErrorState message={getErrorMessage(expensesQuery.error)} onRetry={() => void expensesQuery.refetch()} />
      ) : null}
      {expensesQuery.isSuccess && rows.length === 0 ? (
        <EmptyState title="لا توجد مصاريف" description="لم يتم العثور على مصاريف مطابقة." />
      ) : null}

      {rows.length > 0 ? (
        <section className="card-list" aria-label="قائمة المصاريف">
          {rows.map((expense) => (
            <Link className="card-link" key={expense.id} to={`/expenses/${expense.id}`}>
              <ExpenseListCard expense={expense} />
            </Link>
          ))}
        </section>
      ) : null}
      </div>
    </AppShell>
  );
}

function ExpenseListCard({ expense }: { expense: ExpenseListDto }) {
  return (
    <DataCard
      icon={<Icon name="expenses" />}
      title={expense.name}
      subtitle={`${expense.code} • ${expense.categoryName}`}
      meta={`${formatDateOnly(expense.startDate)} • ${expense.categoryKindDisplay}`}
      value={
        <span className={`status-pill status-pill--${getExpenseStatusTone(expense.status)}`}>
          {expenseStatusLabel(expense.status)}
        </span>
      }
      tone={expense.status === 5 ? 'available' : expense.status === 7 ? 'danger' : 'neutral'}
    />
  );
}

function ExpenseDetailPage({ expenseId }: { expenseId: string }) {
  const navigate = useNavigate();
  const { can } = useAuth();
  const queryClient = useQueryClient();
  const [toast, setToast] = useState<ToastState | null>(null);
  const [showPay, setShowPay] = useState(false);

  const detailsQuery = useQuery({
    queryKey: ['expense', expenseId],
    queryFn: () => getExpense(expenseId)
  });

  async function refresh(message: string) {
    await queryClient.invalidateQueries({ queryKey: ['expense', expenseId] });
    await queryClient.invalidateQueries({ queryKey: ['expenses'] });
    setToast({ tone: 'success', message });
  }

  const approveMutation = useMutation({
    mutationFn: () => approveExpense(expenseId),
    onSuccess: () => void refresh('تم اعتماد المصروف.'),
    onError: (error) => setToast({ tone: 'error', message: getErrorMessage(error) })
  });

  const rejectMutation = useMutation({
    mutationFn: (reason: string) => rejectExpense(expenseId, reason),
    onSuccess: () => void refresh('تم رفض المصروف.'),
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

  function handleReject() {
    const reason = window.prompt('سبب الرفض؟', '');
    if (reason !== null) {
      rejectMutation.mutate(reason.trim());
    }
  }

  function handleDelete() {
    if (window.confirm('هل تريد حذف هذا المصروف؟')) {
      deleteMutation.mutate();
    }
  }

  const expense = detailsQuery.data;
  const allowed = new Set(expense?.allowedTransitions ?? []);
  const canApprove = can('expenses.approve') && allowed.has(2);
  const canReject = can('expenses.approve') && allowed.has(7);
  const canPay = can('expenses.create') && expense != null && expense.remainingBalanceBase > 0 && expense.status >= 2 && expense.status <= 4;
  const canDelete = can('expenses.delete') && expense != null && (expense.status === 0 || expense.status === 1);

  const headerSummary = expense ? (
    <>
      <SummaryCard label="المبلغ" value={formatCurrency(expense.baseAmount)} />
      <SummaryCard label="المدفوع" value={formatCurrency(expense.paidAmountBase)} tone="green" />
      <SummaryCard label="المتبقي" value={formatCurrency(expense.remainingBalanceBase)} tone="amber" />
    </>
  ) : undefined;

  return (
    <AppShell title={expense ? expense.name : 'تفاصيل المصروف'} summary={headerSummary}>
      <Toast toast={toast} onClose={() => setToast(null)} />

      {detailsQuery.isLoading ? <LoadingState /> : null}
      {detailsQuery.isError ? (
        <ErrorState message={getErrorMessage(detailsQuery.error)} onRetry={() => void detailsQuery.refetch()} />
      ) : null}

      {expense ? (
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

          <section className="form-panel form-compact">
            <h2>بيانات المصروف</h2>
            <dl className="detail-grid">
              <DetailItem label="التصنيف" value={expense.categoryName} />
              <DetailItem label="النوع" value={expense.categoryKindDisplay} />
              <DetailItem label="طريقة الدفع" value={expense.paymentMethodDisplay} />
              <DetailItem label="المستفيد" value={expense.payeeName ?? 'غير محدد'} />
              <DetailItem label="مركز التكلفة" value={expense.costCenterName ?? 'غير محدد'} />
              <DetailItem label="التاريخ" value={formatDateOnly(expense.startDate)} />
              <DetailItem label="العملة" value={expense.originalCurrency} />
              <DetailItem label="المبلغ الأصلي" value={formatNumber(expense.originalAmount)} />
            </dl>
            {expense.description ? <p className="form-hint">{expense.description}</p> : null}
          </section>

          <section className="compact-action-row" aria-label="إجراءات المصروف">
            {canApprove ? (
              <button className="primary-button primary-button--wide" type="button" onClick={() => approveMutation.mutate()} disabled={approveMutation.isPending}>
                {approveMutation.isPending ? 'جار الاعتماد...' : 'اعتماد'}
              </button>
            ) : null}
            {canReject ? (
              <button className="primary-button primary-button--wide" type="button" onClick={handleReject} disabled={rejectMutation.isPending}>
                {rejectMutation.isPending ? 'جار الرفض...' : 'رفض'}
              </button>
            ) : null}
            {canPay ? (
              <button className="primary-button primary-button--wide" type="button" onClick={() => setShowPay((current) => !current)}>
                تسجيل دفعة
              </button>
            ) : null}
            {canDelete ? (
              <button className="primary-button primary-button--wide" type="button" onClick={handleDelete} disabled={deleteMutation.isPending}>
                {deleteMutation.isPending ? 'جار الحذف...' : 'حذف'}
              </button>
            ) : null}
          </section>

          {showPay && canPay ? (
            <PayExpenseForm
              expenseId={expenseId}
              remaining={expense.remainingBalanceBase}
              defaultMethod={expense.paymentMethod}
              onToast={setToast}
              onDone={async () => {
                setShowPay(false);
                await refresh('تم تسجيل الدفعة.');
              }}
            />
          ) : null}

          {expense.payments.length > 0 ? (
            <section className="form-panel form-compact">
              <h2>الدفعات</h2>
              <div className="table-scroll">
                <table className="data-table">
                  <thead>
                    <tr>
                      <th>التاريخ</th>
                      <th>المبلغ</th>
                      <th>الطريقة</th>
                      <th>المصدر</th>
                      <th>الحالة</th>
                    </tr>
                  </thead>
                  <tbody>
                    {expense.payments.map((payment) => (
                      <tr key={payment.id}>
                        <td>{formatDateOnly(payment.paymentDate)}</td>
                        <td>{formatCurrency(payment.amountBase)}</td>
                        <td>{payment.paymentMethodDisplay}</td>
                        <td>{payment.fundingSourceDisplay}</td>
                        <td>{payment.statusDisplay}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </section>
          ) : null}

          <button className="ghost-button" type="button" onClick={() => navigate('/expenses')}>
            العودة إلى القائمة
          </button>
        </div>
      ) : null}
    </AppShell>
  );
}

function PayExpenseForm({
  expenseId,
  remaining,
  defaultMethod,
  onDone,
  onToast
}: {
  expenseId: string;
  remaining: number;
  defaultMethod: ExpensePaymentMethod;
  onDone: () => void | Promise<void>;
  onToast: (toast: ToastState) => void;
}) {
  const [amount, setAmount] = useState(String(remaining));
  const [method, setMethod] = useState(String(defaultMethod));
  const [cashboxId, setCashboxId] = useState('');

  const cashboxesQuery = useQuery({
    queryKey: ['lookups', 'cashboxes'],
    queryFn: getCashboxLookups
  });

  const mutation = useMutation({
    mutationFn: () =>
      payExpense(expenseId, {
        paymentDate: new Date().toISOString(),
        amount: toNumber(amount),
        currency: 'USD',
        paymentMethod: Number(method) as ExpensePaymentMethod,
        fundingSource: 0,
        referenceNumber: null,
        notes: null,
        cashboxId: cashboxId || null
      }),
    onSuccess: () => void onDone(),
    onError: (error) => onToast({ tone: 'error', message: getErrorMessage(error) })
  });

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (toNumber(amount) <= 0) {
      onToast({ tone: 'error', message: 'المبلغ يجب أن يكون أكبر من صفر.' });
      return;
    }
    mutation.mutate();
  }

  return (
    <form className="form-panel form-compact" onSubmit={submit}>
      <div className="form-section-head">
        <h2>تسجيل دفعة</h2>
      </div>
      <div className="form-field-row form-field-row--2">
        <label className="form-field">
          <span className="form-field__label">المبلغ</span>
          <input inputMode="decimal" value={amount} onChange={(event) => setAmount(event.target.value)} />
        </label>
        <label className="form-field">
          <span className="form-field__label">طريقة الدفع</span>
          <select value={method} onChange={(event) => setMethod(event.target.value)}>
            {expensePaymentMethodOptions.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </label>
      </div>
      <label className="form-field form-field--wide">
        <span className="form-field__label">الصندوق (اختياري)</span>
        <select value={cashboxId} onChange={(event) => setCashboxId(event.target.value)}>
          <option value="">بدون صندوق</option>
          {(cashboxesQuery.data ?? []).map((cashbox) => (
            <option key={cashbox.id} value={cashbox.id}>
              {cashbox.name}
            </option>
          ))}
        </select>
      </label>
      <button className="primary-button" type="submit" disabled={mutation.isPending}>
        {mutation.isPending ? 'جار الحفظ...' : 'حفظ الدفعة'}
      </button>
    </form>
  );
}

function ExpenseCreatePage() {
  const navigate = useNavigate();
  const [toast, setToast] = useState<ToastState | null>(null);
  const [form, setForm] = useState({
    name: '',
    categoryId: '',
    amount: '0',
    currency: 'USD',
    paymentMethod: '1',
    costCenterId: '',
    payeeName: '',
    startDate: toDateInputValue(new Date()),
    description: '',
    submitForApproval: true
  });

  const categoriesQuery = useQuery({
    queryKey: ['expenses', 'categories'],
    queryFn: () => getExpenseCategories()
  });

  const costCentersQuery = useQuery({
    queryKey: ['expenses', 'cost-centers'],
    queryFn: () => getExpenseCostCenters()
  });

  const categoriesByKind = useMemo(() => categoriesQuery.data ?? [], [categoriesQuery.data]);

  const mutation = useMutation({
    mutationFn: () =>
      createExpense({
        name: form.name.trim(),
        categoryId: form.categoryId,
        description: nullableText(form.description),
        startDate: toIsoDate(form.startDate),
        endDate: null,
        originalCurrency: form.currency,
        originalAmount: toNumber(form.amount),
        exchangeRate: 1,
        baseCurrency: 'USD',
        paymentMethod: Number(form.paymentMethod) as ExpensePaymentMethod,
        payeeName: nullableText(form.payeeName),
        supplierId: null,
        costCenterId: form.costCenterId || null,
        department: null,
        notes: null,
        submitForApproval: form.submitForApproval
      }),
    onSuccess: (id) => navigate(`/expenses/${id}`),
    onError: (error) => setToast({ tone: 'error', message: getErrorMessage(error) })
  });

  function update<K extends keyof typeof form>(field: K, value: (typeof form)[K]) {
    setForm((current) => ({ ...current, [field]: value }));
  }

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!form.name.trim()) {
      setToast({ tone: 'error', message: 'اسم المصروف مطلوب.' });
      return;
    }
    if (!form.categoryId) {
      setToast({ tone: 'error', message: 'اختر التصنيف.' });
      return;
    }
    if (toNumber(form.amount) <= 0) {
      setToast({ tone: 'error', message: 'المبلغ يجب أن يكون أكبر من صفر.' });
      return;
    }
    mutation.mutate();
  }

  return (
    <AppShell title="مصروف جديد">
      <Toast toast={toast} onClose={() => setToast(null)} />
      <form className="page-stack page-stack--footer" onSubmit={submit}>
        <section className="form-panel form-compact" aria-label="بيانات المصروف">
          <label className="form-field form-field--wide">
            <span className="form-field__label">اسم المصروف</span>
            <input value={form.name} onChange={(event) => update('name', event.target.value)} required />
          </label>
          <label className="form-field form-field--wide">
            <span className="form-field__label">التصنيف</span>
            <select value={form.categoryId} onChange={(event) => update('categoryId', event.target.value)} required>
              <option value="">اختر التصنيف...</option>
              {categoriesByKind.map((category) => (
                <option key={category.id} value={category.id}>
                  {category.nameAr} ({expenseCategoryKindLabel(category.kind)})
                </option>
              ))}
            </select>
          </label>
          <div className="form-field-row form-field-row--2">
            <label className="form-field">
              <span className="form-field__label">المبلغ</span>
              <input inputMode="decimal" value={form.amount} onChange={(event) => update('amount', event.target.value)} />
            </label>
            <label className="form-field">
              <span className="form-field__label">العملة</span>
              <input value={form.currency} onChange={(event) => update('currency', event.target.value)} />
            </label>
          </div>
          <div className="form-field-row form-field-row--2">
            <label className="form-field">
              <span className="form-field__label">طريقة الدفع</span>
              <select value={form.paymentMethod} onChange={(event) => update('paymentMethod', event.target.value)}>
                {expensePaymentMethodOptions.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </label>
            <label className="form-field">
              <span className="form-field__label">التاريخ</span>
              <input type="date" value={form.startDate} onChange={(event) => update('startDate', event.target.value)} />
            </label>
          </div>
          <label className="form-field form-field--wide">
            <span className="form-field__label">مركز التكلفة (اختياري)</span>
            <select value={form.costCenterId} onChange={(event) => update('costCenterId', event.target.value)}>
              <option value="">بدون</option>
              {(costCentersQuery.data ?? []).map((cc) => (
                <option key={cc.id} value={cc.id}>
                  {cc.name}
                </option>
              ))}
            </select>
          </label>
          <label className="form-field form-field--wide">
            <span className="form-field__label">المستفيد (اختياري)</span>
            <input value={form.payeeName} onChange={(event) => update('payeeName', event.target.value)} />
          </label>
          <label className="form-field form-field--wide">
            <span className="form-field__label">الوصف (اختياري)</span>
            <input value={form.description} onChange={(event) => update('description', event.target.value)} />
          </label>
          <label className="toggle-row">
            <input type="checkbox" checked={form.submitForApproval} onChange={(event) => update('submitForApproval', event.target.checked)} />
            إرسال للاعتماد مباشرة
          </label>
        </section>

        <div className="sticky-form-footer">
          <div className="sticky-form-footer__total">
            <span>المبلغ</span>
            <strong>{formatCurrency(toNumber(form.amount))}</strong>
          </div>
          <button className="primary-button sticky-form-footer__submit" type="submit" disabled={mutation.isPending}>
            {mutation.isPending ? 'جار الحفظ...' : 'حفظ المصروف'}
          </button>
        </div>
      </form>
    </AppShell>
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
