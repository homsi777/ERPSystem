import { useMemo, useState, type FormEvent } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useLocation, useNavigate, useParams } from 'react-router-dom';
import {
  createCustomer,
  deactivateCustomer,
  getCustomerAccountLedger,
  getCustomerDetails,
  getCustomerSalesDetails,
  getCustomers,
  postCustomerOpeningBalance,
  reconcileCustomerAccount,
  updateCustomer
} from '../api/customers.ts';
import { getCashboxLookups } from '../api/lookups.ts';
import { createReceiptVoucher, postReceiptVoucher } from '../api/receipts.ts';
import { ApiError } from '../api/client.ts';
import type {
  CustomerAccountLedgerDto,
  CustomerAccountLedgerLineDto,
  CustomerDetailsDto,
  CustomerListDto,
  CustomerStatus,
  CustomerType
} from '../api/types.ts';
import { useAuth } from '../auth/AuthContext.tsx';
import { AppShell } from '../components/AppShell.tsx';
import { DataCard } from '../components/DataCard.tsx';
import { EmptyState } from '../components/EmptyState.tsx';
import { ErrorState } from '../components/ErrorState.tsx';
import { Icon } from '../components/Icon.tsx';
import { LoadingState } from '../components/LoadingState.tsx';
import { RecordField } from '../components/RecordField.tsx';
import { SummaryCard } from '../components/SummaryCard.tsx';
import { formatCurrency, formatDateOnly, formatNumber } from '../lib/format.ts';
import {
  customerAccountMovementTypeLabels,
  customerStatusLabels,
  customerTypeLabels,
  getCustomerAccountMovementTypeTone,
  getCustomerStatusTone
} from '../lib/enums.ts';

const LIST_PAGE_SIZE = 500;

type ToastState = {
  tone: 'success' | 'error';
  message: string;
};

export function CustomersPage() {
  const { customerId } = useParams();
  const location = useLocation();

  if (location.pathname === '/customers/new') {
    return <CustomerCreatePage />;
  }

  if (customerId) {
    return <CustomerDetailsPage customerId={customerId} />;
  }

  return <CustomerListPage />;
}

function CustomerListPage() {
  const { can } = useAuth();
  const navigate = useNavigate();
  const [searchInput, setSearchInput] = useState('');
  const [search, setSearch] = useState('');

  const customersQuery = useQuery({
    queryKey: ['customers', search],
    queryFn: () => getCustomers({ search: search || undefined, page: 1, pageSize: LIST_PAGE_SIZE })
  });

  const listSummary = useMemo(() => {
    const rows = customersQuery.data?.items ?? [];
    return {
      count: customersQuery.data?.totalCount ?? rows.length,
      outstanding: rows.reduce((sum, row) => sum + row.balance, 0)
    };
  }, [customersQuery.data?.items, customersQuery.data?.totalCount]);

  const headerSummary = (
    <>
      <SummaryCard label="عدد العملاء" value={formatNumber(listSummary.count)} />
      <SummaryCard label="إجمالي الأرصدة" value={formatCurrency(listSummary.outstanding)} tone="amber" />
    </>
  );

  function submitSearch(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSearch(searchInput.trim());
  }

  return (
    <AppShell title="العملاء" summary={headerSummary}>
      <section className="toolbar-row">
        {can('customers.create') ? (
          <button className="primary-button" type="button" onClick={() => navigate('/customers/new')}>
            عميل جديد
          </button>
        ) : null}
      </section>

      <form className="search-row" onSubmit={submitSearch}>
        <input
          className="search-input"
          value={searchInput}
          onChange={(event) => setSearchInput(event.target.value)}
          placeholder="ابحث بالاسم أو الكود..."
          aria-label="بحث عن عميل"
        />
        <button className="primary-button" type="submit">بحث</button>
      </form>

      {customersQuery.isLoading ? <LoadingState /> : null}

      {customersQuery.isError ? (
        <ErrorState
          message={getErrorMessage(customersQuery.error)}
          onRetry={() => void customersQuery.refetch()}
        />
      ) : null}

      {customersQuery.isSuccess && customersQuery.data.items.length === 0 ? (
        <EmptyState title="لا يوجد عملاء" description="لم يتم العثور على عملاء مطابقين لبحثك." />
      ) : null}

      {customersQuery.isSuccess && customersQuery.data.items.length > 0 ? (
        <section className="card-list" aria-label="قائمة العملاء">
          {customersQuery.data.items.map((customer) => (
            <Link className="card-link" key={customer.id} to={`/customers/${customer.id}`}>
              <CustomerListCard customer={customer} />
            </Link>
          ))}
        </section>
      ) : null}
    </AppShell>
  );
}

function CustomerListCard({ customer }: { customer: CustomerListDto }) {
  const subtitle = `${customer.code} • ${customerTypeLabels[customer.type as CustomerType]}`;

  return (
    <DataCard
      icon={<Icon name="customers" />}
      title={customer.nameAr}
      subtitle={subtitle}
      meta={formatCurrency(customer.balance)}
      value={<CustomerStatusPill status={customer.status} />}
      tone={customer.status === 0 ? 'available' : 'low'}
    />
  );
}

function CustomerStatusPill({ status }: { status: CustomerStatus }) {
  return (
    <span className={`status-pill status-pill--${getCustomerStatusTone(status)}`}>
      {customerStatusLabels[status]}
    </span>
  );
}

function CustomerCreatePage() {
  const navigate = useNavigate();
  const [toast, setToast] = useState<ToastState | null>(null);
  const [form, setForm] = useState({
    code: '',
    nameAr: '',
    nameEn: '',
    type: '0' as '0' | '1',
    creditLimit: '0',
    creditLimitEnabled: false
  });

  const mutation = useMutation({
    mutationFn: () =>
      createCustomer({
        code: form.code.trim(),
        nameAr: form.nameAr.trim(),
        nameEn: form.nameEn.trim(),
        type: Number(form.type) as CustomerType,
        creditLimit: toNumber(form.creditLimit),
        creditLimitEnabled: form.creditLimitEnabled
      }),
    onSuccess: (customerId) => navigate(`/customers/${customerId}`),
    onError: (error) => setToast(errorToast(error))
  });

  function update<K extends keyof typeof form>(field: K, value: (typeof form)[K]) {
    setForm((current) => ({ ...current, [field]: value }));
  }

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!form.code.trim() || !form.nameAr.trim()) {
      setToast({ tone: 'error', message: 'الكود والاسم العربي مطلوبان.' });
      return;
    }
    mutation.mutate();
  }

  return (
    <AppShell title="عميل جديد">
      <Toast toast={toast} onClose={() => setToast(null)} />
      <form className="detail-card form-grid" onSubmit={submit}>
        <label>
          الكود
          <input value={form.code} onChange={(event) => update('code', event.target.value)} required />
        </label>
        <label>
          النوع
          <select value={form.type} onChange={(event) => update('type', event.target.value as '0' | '1')}>
            <option value="0">{customerTypeLabels[0]}</option>
            <option value="1">{customerTypeLabels[1]}</option>
          </select>
        </label>
        <label>
          الاسم بالعربي
          <input value={form.nameAr} onChange={(event) => update('nameAr', event.target.value)} required />
        </label>
        <label>
          الاسم بالإنجليزي
          <input value={form.nameEn} onChange={(event) => update('nameEn', event.target.value)} />
        </label>
        <label>
          حد الائتمان
          <input inputMode="decimal" value={form.creditLimit} onChange={(event) => update('creditLimit', event.target.value)} />
        </label>
        <label className="toggle-row">
          <input
            type="checkbox"
            checked={form.creditLimitEnabled}
            onChange={(event) => update('creditLimitEnabled', event.target.checked)}
          />
          تفعيل حد الائتمان
        </label>
        <button className="primary-button primary-button--wide form-grid__wide" type="submit" disabled={mutation.isPending}>
          {mutation.isPending ? 'جار الإنشاء...' : 'إنشاء العميل'}
        </button>
      </form>
    </AppShell>
  );
}

type DetailsTab = 'sales' | 'statement';
type ActivePanel = 'edit' | 'opening-balance' | 'receipt' | null;

function CustomerDetailsPage({ customerId }: { customerId: string }) {
  const { can } = useAuth();
  const queryClient = useQueryClient();
  const [activeTab, setActiveTab] = useState<DetailsTab>('statement');
  const [from, setFrom] = useState('');
  const [to, setTo] = useState('');
  const [activePanel, setActivePanel] = useState<ActivePanel>(null);
  const [toast, setToast] = useState<ToastState | null>(null);

  const detailsQuery = useQuery({
    queryKey: ['customer-details', customerId],
    queryFn: () => getCustomerDetails(customerId)
  });

  const customer = detailsQuery.data;

  async function refreshAfterAction(message: string) {
    await queryClient.invalidateQueries({ queryKey: ['customer-details', customerId] });
    await queryClient.invalidateQueries({ queryKey: ['customer-ledger', customerId] });
    await queryClient.invalidateQueries({ queryKey: ['customers'] });
    setToast({ tone: 'success', message });
    setActivePanel(null);
  }

  const deactivateMutation = useMutation({
    mutationFn: () => deactivateCustomer(customerId),
    onSuccess: () => void refreshAfterAction('تم تعطيل العميل.'),
    onError: (error) => setToast(errorToast(error))
  });

  function handleDeactivate() {
    if (!customer) {
      return;
    }
    const message =
      customer.balance !== 0
        ? `هذا العميل لديه رصيد قائم قدره ${formatCurrency(customer.balance)} — المتابعة ستعطّله دون تسوية الرصيد. هل تريد المتابعة؟`
        : 'هل تريد تعطيل هذا العميل؟';
    if (window.confirm(message)) {
      deactivateMutation.mutate();
    }
  }

  const headerSummary = customer ? (
    <>
      <SummaryCard label="الرصيد" value={formatCurrency(customer.balance)} tone={customer.balance > 0 ? 'amber' : 'green'} />
      <SummaryCard label="حد الائتمان" value={formatCurrency(customer.creditLimit)} />
    </>
  ) : undefined;

  return (
    <AppShell title={customer ? customer.nameAr : 'تفاصيل العميل'} summary={headerSummary}>
      <Toast toast={toast} onClose={() => setToast(null)} />

      {detailsQuery.isLoading ? <LoadingState /> : null}

      {detailsQuery.isError ? (
        <ErrorState message={getErrorMessage(detailsQuery.error)} onRetry={() => void detailsQuery.refetch()} />
      ) : null}

      {customer ? (
        <div className="details-stack">
          <section className="detail-card detail-card--hero">
            <div className="detail-card__lead">
              <p className="detail-card__eyebrow">{customer.code}</p>
              <h2>{customer.nameAr}</h2>
              <CustomerStatusPill status={customer.status} />
            </div>
          </section>

          <section className="detail-card">
            <h2>بيانات العميل</h2>
            <dl className="detail-grid">
              <DetailItem label="النوع" value={customerTypeLabels[customer.type as CustomerType]} />
              <DetailItem label="شروط الدفع" value={`${formatNumber(customer.paymentTermsDays)} يوم`} />
              <DetailItem label="الهاتف" value={customer.phone ?? 'غير محدد'} />
              <DetailItem label="البريد" value={customer.email ?? 'غير محدد'} />
            </dl>
          </section>

          <section className="action-grid" aria-label="إجراءات العميل">
            {can('customers.create') ? (
              <button
                className="primary-button primary-button--wide"
                type="button"
                onClick={() => setActivePanel((current) => (current === 'edit' ? null : 'edit'))}
              >
                تعديل
              </button>
            ) : null}
            {can('customers.deactivate') && customer.isActive ? (
              <button
                className="primary-button primary-button--wide"
                type="button"
                onClick={handleDeactivate}
                disabled={deactivateMutation.isPending}
              >
                {deactivateMutation.isPending ? 'جار التعطيل...' : 'تعطيل'}
              </button>
            ) : null}
            {can('customers.opening-balance') && !customer.openingBalancePosted ? (
              <button
                className="primary-button primary-button--wide"
                type="button"
                onClick={() => setActivePanel((current) => (current === 'opening-balance' ? null : 'opening-balance'))}
              >
                ترحيل رصيد افتتاحي
              </button>
            ) : null}
            {can('finance.receipt.create') && can('finance.receipt.post') ? (
              <button
                className="primary-button primary-button--wide"
                type="button"
                onClick={() => setActivePanel((current) => (current === 'receipt' ? null : 'receipt'))}
              >
                سند قبض
              </button>
            ) : null}
          </section>

          {activePanel === 'edit' ? (
            <EditCustomerForm customer={customer} onToast={setToast} onDone={refreshAfterAction} />
          ) : null}
          {activePanel === 'opening-balance' ? (
            <OpeningBalanceForm customerId={customerId} onToast={setToast} onDone={refreshAfterAction} />
          ) : null}
          {activePanel === 'receipt' ? (
            <ReceiptVoucherForm customerId={customerId} onToast={setToast} onDone={refreshAfterAction} />
          ) : null}

          <section className="detail-card detail-card--tabs">
            <div className="tab-strip" role="tablist" aria-label="تبويبات العميل">
              <button
                className={`filter-chip ${activeTab === 'statement' ? 'filter-chip--active' : ''}`}
                type="button"
                role="tab"
                aria-selected={activeTab === 'statement'}
                onClick={() => setActiveTab('statement')}
              >
                كشف الحساب
              </button>
              <button
                className={`filter-chip ${activeTab === 'sales' ? 'filter-chip--active' : ''}`}
                type="button"
                role="tab"
                aria-selected={activeTab === 'sales'}
                onClick={() => setActiveTab('sales')}
              >
                تفاصيل المبيعات
              </button>
            </div>

            <div className="form-grid form-grid--dates">
              <label>
                من تاريخ
                <input type="date" value={from} onChange={(event) => setFrom(event.target.value)} />
              </label>
              <label>
                إلى تاريخ
                <input type="date" value={to} onChange={(event) => setTo(event.target.value)} />
              </label>
            </div>

            {activeTab === 'statement' ? (
              <CustomerLedgerPanel customerId={customerId} from={from} to={to} onToast={setToast} />
            ) : (
              <CustomerSalesDetailsPanel customerId={customerId} from={from} to={to} />
            )}
          </section>
        </div>
      ) : null}
    </AppShell>
  );
}

function EditCustomerForm({
  customer,
  onDone,
  onToast
}: {
  customer: CustomerDetailsDto;
  onDone: (message: string) => void;
  onToast: (toast: ToastState) => void;
}) {
  const [form, setForm] = useState({
    nameAr: customer.nameAr,
    nameEn: customer.nameEn,
    creditLimit: String(customer.creditLimit),
    creditLimitEnabled: customer.creditLimitEnabled,
    paymentTermsDays: String(customer.paymentTermsDays)
  });

  const mutation = useMutation({
    mutationFn: () =>
      updateCustomer(customer.id, {
        nameAr: form.nameAr.trim(),
        nameEn: form.nameEn.trim(),
        creditLimit: toNumber(form.creditLimit),
        creditLimitEnabled: form.creditLimitEnabled,
        paymentTermsDays: Math.round(toNumber(form.paymentTermsDays))
      }),
    onSuccess: () => onDone('تم تحديث بيانات العميل.'),
    onError: (error) => onToast(errorToast(error))
  });

  function update<K extends keyof typeof form>(field: K, value: (typeof form)[K]) {
    setForm((current) => ({ ...current, [field]: value }));
  }

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    mutation.mutate();
  }

  return (
    <form className="detail-card form-grid" onSubmit={submit}>
      <h2 className="form-grid__wide">تعديل بيانات العميل</h2>
      <label>
        الاسم بالعربي
        <input value={form.nameAr} onChange={(event) => update('nameAr', event.target.value)} required />
      </label>
      <label>
        الاسم بالإنجليزي
        <input value={form.nameEn} onChange={(event) => update('nameEn', event.target.value)} />
      </label>
      <label>
        حد الائتمان
        <input inputMode="decimal" value={form.creditLimit} onChange={(event) => update('creditLimit', event.target.value)} />
      </label>
      <label>
        شروط الدفع (أيام)
        <input inputMode="numeric" value={form.paymentTermsDays} onChange={(event) => update('paymentTermsDays', event.target.value)} />
      </label>
      <label className="toggle-row form-grid__wide">
        <input
          type="checkbox"
          checked={form.creditLimitEnabled}
          onChange={(event) => update('creditLimitEnabled', event.target.checked)}
        />
        تفعيل حد الائتمان
      </label>
      <button className="primary-button primary-button--wide form-grid__wide" type="submit" disabled={mutation.isPending}>
        {mutation.isPending ? 'جار الحفظ...' : 'حفظ التعديلات'}
      </button>
    </form>
  );
}

function OpeningBalanceForm({
  customerId,
  onDone,
  onToast
}: {
  customerId: string;
  onDone: (message: string) => void;
  onToast: (toast: ToastState) => void;
}) {
  const [amount, setAmount] = useState('0');
  const [postingDate, setPostingDate] = useState(toDateInputValue(new Date()));
  const [note, setNote] = useState('');

  const mutation = useMutation({
    mutationFn: () =>
      postCustomerOpeningBalance(customerId, {
        amount: toNumber(amount),
        postingDate: toIsoDate(postingDate),
        referenceNote: nullableText(note)
      }),
    onSuccess: (result) => onDone(`تم ترحيل الرصيد الافتتاحي (قيد ${result.journalEntryNumber}).`),
    onError: (error) => onToast(errorToast(error))
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
    <form className="detail-card form-grid" onSubmit={submit}>
      <h2 className="form-grid__wide">ترحيل رصيد افتتاحي</h2>
      <label>
        المبلغ
        <input inputMode="decimal" value={amount} onChange={(event) => setAmount(event.target.value)} />
      </label>
      <label>
        تاريخ الترحيل
        <input type="date" value={postingDate} onChange={(event) => setPostingDate(event.target.value)} />
      </label>
      <label className="form-grid__wide">
        ملاحظة مرجعية
        <input value={note} onChange={(event) => setNote(event.target.value)} />
      </label>
      <button className="primary-button primary-button--wide form-grid__wide" type="submit" disabled={mutation.isPending}>
        {mutation.isPending ? 'جار الترحيل...' : 'ترحيل الرصيد الافتتاحي'}
      </button>
    </form>
  );
}

function ReceiptVoucherForm({
  customerId,
  onDone,
  onToast
}: {
  customerId: string;
  onDone: (message: string) => void;
  onToast: (toast: ToastState) => void;
}) {
  const [cashboxId, setCashboxId] = useState('');
  const [amount, setAmount] = useState('0');

  const cashboxesQuery = useQuery({
    queryKey: ['lookups', 'cashboxes'],
    queryFn: getCashboxLookups
  });

  const mutation = useMutation({
    mutationFn: async () => {
      const voucherId = await createReceiptVoucher({
        customerId,
        cashboxId,
        amount: toNumber(amount),
        allocations: []
      });
      await postReceiptVoucher(voucherId);
    },
    onSuccess: () => onDone('تم إنشاء وترحيل سند القبض بنجاح.'),
    onError: (error) => onToast(errorToast(error))
  });

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!cashboxId.trim()) {
      onToast({ tone: 'error', message: 'اختر الصندوق أولاً.' });
      return;
    }
    if (toNumber(amount) <= 0) {
      onToast({ tone: 'error', message: 'المبلغ يجب أن يكون أكبر من صفر.' });
      return;
    }
    mutation.mutate();
  }

  return (
    <form className="detail-card form-grid" onSubmit={submit}>
      <h2 className="form-grid__wide">سند قبض</h2>
      <label>
        الصندوق
        <select
          value={cashboxId}
          onChange={(event) => setCashboxId(event.target.value)}
          disabled={cashboxesQuery.isLoading || cashboxesQuery.isError}
          required
        >
          <option value="">اختر الصندوق...</option>
          {(cashboxesQuery.data ?? []).map((cashbox) => (
            <option key={cashbox.id} value={cashbox.id}>
              {cashbox.name}
            </option>
          ))}
        </select>
        {cashboxesQuery.isError ? <span className="field-note">تعذر تحميل قائمة الصناديق.</span> : null}
      </label>
      <label>
        المبلغ
        <input inputMode="decimal" value={amount} onChange={(event) => setAmount(event.target.value)} />
      </label>
      <p className="field-note form-grid__wide">
        سيُنشأ السند ويُرحَّل فورًا (بلا تخصيص تفصيلي على فواتير محددة).
      </p>
      <button className="primary-button primary-button--wide form-grid__wide" type="submit" disabled={mutation.isPending}>
        {mutation.isPending ? 'جار الحفظ...' : 'حفظ وترحيل السند'}
      </button>
    </form>
  );
}

function CustomerLedgerPanel({
  customerId,
  from,
  to,
  onToast
}: {
  customerId: string;
  from: string;
  to: string;
  onToast: (toast: ToastState) => void;
}) {
  const queryClient = useQueryClient();
  const [showReconcile, setShowReconcile] = useState(false);
  const [selectedLineIndex, setSelectedLineIndex] = useState<number | null>(null);

  const ledgerQuery = useQuery({
    queryKey: ['customer-ledger', customerId, from, to],
    queryFn: () => getCustomerAccountLedger(customerId, { from: from || undefined, to: to || undefined })
  });

  const reconcileMutation = useMutation({
    mutationFn: (line: CustomerAccountLedgerLineDto) =>
      reconcileCustomerAccount(customerId, {
        reconciliationDate: line.transactionDate,
        documentId: line.documentId,
        balanceAtReconciliation: line.runningBalance
      }),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['customer-ledger', customerId] });
      onToast({ tone: 'success', message: 'تمت مطابقة الكشف.' });
      setShowReconcile(false);
    },
    onError: (error) => onToast(errorToast(error))
  });

  if (ledgerQuery.isLoading) {
    return <LoadingState />;
  }

  if (ledgerQuery.isError) {
    return <ErrorState message={getErrorMessage(ledgerQuery.error)} onRetry={() => void ledgerQuery.refetch()} />;
  }

  const ledger = ledgerQuery.data;
  if (!ledger) {
    return null;
  }

  function openReconcilePanel() {
    setSelectedLineIndex(ledger!.lines.length > 0 ? ledger!.lines.length - 1 : null);
    setShowReconcile(true);
  }

  function submitReconcile(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (selectedLineIndex === null) {
      return;
    }
    const line = ledger!.lines[selectedLineIndex];
    if (window.confirm('المطابقة الجديدة تحل محل السابقة. هل تريد المتابعة؟')) {
      reconcileMutation.mutate(line);
    }
  }

  return (
    <>
      <dl className="mini-grid">
        <DetailItem label="الرصيد الافتتاحي" value={formatCurrency(ledger.openingBalance)} />
        <DetailItem label="الرصيد الختامي" value={formatCurrency(ledger.closingBalance)} />
        <DetailItem
          label="رصيد المطابقة"
          value={ledger.lastReconciliationBalance != null ? formatCurrency(ledger.lastReconciliationBalance) : 'لم تتم المطابقة بعد'}
        />
        <DetailItem
          label="تاريخ آخر مطابقة"
          value={ledger.lastReconciliationDate ? formatDateOnly(ledger.lastReconciliationDate) : 'لا يوجد'}
        />
      </dl>

      <div className="section-title-row">
        <button
          className="ghost-button"
          type="button"
          onClick={openReconcilePanel}
          disabled={ledger.lines.length === 0}
        >
          مطابقة الكشف
        </button>
      </div>

      {showReconcile && selectedLineIndex !== null ? (
        <form className="detail-card form-grid" onSubmit={submitReconcile}>
          <p className="field-note form-grid__wide">المطابقة الجديدة تحل محل السابقة.</p>
          <label className="form-grid__wide">
            السطر المرجعي
            <select value={selectedLineIndex} onChange={(event) => setSelectedLineIndex(Number(event.target.value))}>
              {ledger.lines.map((line, index) => (
                <option key={index} value={index}>
                  {formatDateOnly(line.transactionDate)} — {line.documentNumber} ({formatCurrency(line.runningBalance)})
                </option>
              ))}
            </select>
          </label>
          <button className="primary-button primary-button--wide form-grid__wide" type="submit" disabled={reconcileMutation.isPending}>
            {reconcileMutation.isPending ? 'جار المطابقة...' : 'تأكيد المطابقة'}
          </button>
        </form>
      ) : null}

      {ledger.lines.length === 0 ? (
        <EmptyState title="لا توجد حركات" description="لا توجد حركات ضمن الفترة المحددة." />
      ) : (
        <>
          <div className="record-list mobile-only" aria-label="حركات كشف الحساب">
            {ledger.lines.map((line, index) => (
              <LedgerMobileCard key={index} ledger={ledger} line={line} />
            ))}
          </div>
          <div className="table-scroll desktop-only">
            <table className="data-table">
              <thead>
                <tr>
                  <th>نوع الحركة</th>
                  <th>نوع البضاعة</th>
                  <th>عدد الأثواب</th>
                  <th>مجموع الأطوال</th>
                  <th>السعر</th>
                  <th>مجموع المبلغ</th>
                  <th>رقم المستند</th>
                  <th>التاريخ</th>
                  <th>ملاحظة</th>
                </tr>
              </thead>
              <tbody>
                {ledger.lines.map((line, index) => (
                  <tr key={index} className={isReconciledLine(ledger, line) ? 'is-reconciled' : ''}>
                    <td>
                      <span className={`status-pill status-pill--${getCustomerAccountMovementTypeTone(line.movementType)}`}>
                        {customerAccountMovementTypeLabels[line.movementType]}
                      </span>
                    </td>
                    <td>{line.fabricDescription || '—'}</td>
                    <td>{line.rollCount != null ? formatNumber(line.rollCount) : '—'}</td>
                    <td>{line.totalMeters != null ? formatNumber(line.totalMeters) : '—'}</td>
                    <td>{line.unitPrice != null ? formatCurrency(line.unitPrice) : '—'}</td>
                    <td>{formatCurrency(line.lineAmount)}</td>
                    <td>{line.documentNumber}</td>
                    <td>{formatDateOnly(line.transactionDate)}</td>
                    <td>{line.notes ?? '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </>
      )}
    </>
  );
}

function LedgerMobileCard({ ledger, line }: { ledger: CustomerAccountLedgerDto; line: CustomerAccountLedgerLineDto }) {
  return (
    <article className={`record-card ${isReconciledLine(ledger, line) ? 'record-card--reconciled' : ''}`}>
      <div className="record-card__head">
        <strong className="record-card__title">{line.fabricDescription || line.documentNumber}</strong>
        <span className={`status-pill status-pill--${getCustomerAccountMovementTypeTone(line.movementType)}`}>
          {customerAccountMovementTypeLabels[line.movementType]}
        </span>
      </div>
      <p className="record-card__meta">
        {formatDateOnly(line.transactionDate)} • {line.documentNumber}
      </p>
      <dl className="record-card__grid record-card__grid--quad">
        <RecordField label="عدد الأثواب" value={line.rollCount != null ? formatNumber(line.rollCount) : '—'} />
        <RecordField label="مجموع الأطوال" value={line.totalMeters != null ? formatNumber(line.totalMeters) : '—'} />
        <RecordField label="السعر" value={line.unitPrice != null ? formatCurrency(line.unitPrice) : '—'} />
        <RecordField label="مجموع المبلغ" value={formatCurrency(line.lineAmount)} emphasis />
      </dl>
      {line.notes ? <p className="record-card__meta">{line.notes}</p> : null}
    </article>
  );
}

function isReconciledLine(ledger: CustomerAccountLedgerDto, line: CustomerAccountLedgerLineDto) {
  if (!ledger.lastReconciliationDate) {
    return false;
  }
  return new Date(line.transactionDate).getTime() <= new Date(ledger.lastReconciliationDate).getTime();
}

function CustomerSalesDetailsPanel({ customerId, from, to }: { customerId: string; from: string; to: string }) {
  const salesQuery = useQuery({
    queryKey: ['customer-sales-details', customerId, from, to],
    queryFn: () => getCustomerSalesDetails(customerId, { from: from || undefined, to: to || undefined })
  });

  if (salesQuery.isLoading) {
    return <LoadingState />;
  }

  if (salesQuery.isError) {
    return <ErrorState message={getErrorMessage(salesQuery.error)} onRetry={() => void salesQuery.refetch()} />;
  }

  if (!salesQuery.data) {
    return null;
  }

  if (salesQuery.data.length === 0) {
    return <EmptyState title="لا توجد مبيعات" description="لا توجد مبيعات ضمن الفترة المحددة." />;
  }

  return (
    <>
      <div className="record-list mobile-only" aria-label="تفاصيل المبيعات">
        {salesQuery.data.map((line, index) => (
          <article className="record-card" key={index}>
            <div className="record-card__head">
              <strong className="record-card__title">{line.fabricName}</strong>
              <span className="record-card__badge">{formatDateOnly(line.saleDate)}</span>
            </div>
            <p className="record-card__meta">
              {line.fabricCode} • {line.colorName}
            </p>
            <dl className="record-card__grid record-card__grid--single">
              <RecordField label="سعر الوحدة" value={formatCurrency(line.unitPrice)} emphasis />
            </dl>
          </article>
        ))}
      </div>
      <div className="table-scroll desktop-only">
        <table className="data-table">
          <thead>
            <tr>
              <th>التاريخ</th>
              <th>الصنف</th>
              <th>الكود</th>
              <th>اللون</th>
              <th>سعر الوحدة</th>
            </tr>
          </thead>
          <tbody>
            {salesQuery.data.map((line, index) => (
              <tr key={index}>
                <td>{formatDateOnly(line.saleDate)}</td>
                <td>{line.fabricName}</td>
                <td>{line.fabricCode}</td>
                <td>{line.colorName}</td>
                <td>{formatCurrency(line.unitPrice)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </>
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

function errorToast(error: unknown): ToastState {
  return { tone: 'error', message: getErrorMessage(error) };
}

function getErrorMessage(error: unknown) {
  if (error instanceof ApiError) {
    if (error.status === 403) {
      return 'لا تملك صلاحية لهذا الإجراء.';
    }
    if (error.status === 404) {
      return 'العميل غير موجود.';
    }
    return error.message;
  }
  return 'حدث خطأ غير متوقع.';
}
