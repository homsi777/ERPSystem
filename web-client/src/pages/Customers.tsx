import { useMemo, useState, type FormEvent } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useLocation, useNavigate, useParams } from 'react-router-dom';
import {
  createCustomer,
  deactivateCustomer,
  getCustomerAccountLedger,
  getCustomerAccountLedgerPdf,
  getCustomerDetails,
  getCustomerSalesDetails,
  getCustomers,
  postCustomerOpeningBalance,
  reconcileCustomerAccount,
  updateCustomer
} from '../api/customers.ts';
import { getCashboxLookups } from '../api/lookups.ts';
import {
  approveReceiptVoucher,
  createReceiptVoucher,
  getBankAccounts,
  getPaymentMethods,
  getReceiptVoucherPdf,
  postReceiptVoucher
} from '../api/receipts.ts';
import { getApiErrorMessage } from '../lib/apiError.ts';
import { downloadPdfBlob } from '../lib/documentExport.ts';
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
import { DocumentActions } from '../components/DocumentActions.tsx';
import { EmptyState } from '../components/EmptyState.tsx';
import { ErrorState } from '../components/ErrorState.tsx';
import { Icon } from '../components/Icon.tsx';
import { LoadingState } from '../components/LoadingState.tsx';
import { RecordField } from '../components/RecordField.tsx';
import { SummaryCard } from '../components/SummaryCard.tsx';
import type { DocumentExportPayload } from '../lib/documentExport.ts';
import { formatCurrency, formatDateOnly, formatNumber, formatSalesLineLength, EMPTY_CELL } from '../lib/format.ts';
import {
  customerAccountMovementTypeLabels,
  customerStatusLabels,
  customerTypeLabels,
  getCustomerAccountMovementTypeTone,
  getCustomerStatusTone
} from '../lib/enums.ts';

const LIST_PAGE_SIZE = 500;

function formatOpeningBalanceCell(
  customer: Pick<CustomerListDto, 'openingBalanceAmount' | 'pendingOpeningBalanceAmount' | 'openingBalancePosted'>
) {
  const opening = customer.openingBalanceAmount ?? 0;
  if (opening > 0) {
    return `${formatCurrency(opening)}${(customer.pendingOpeningBalanceAmount ?? 0) > 0 ? ' *' : ''}`;
  }
  return customer.openingBalancePosted ? formatCurrency(0) : EMPTY_CELL;
}

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
      outstanding: rows.reduce((sum, row) => sum + (row.computedBalance ?? row.balance), 0),
      receipts: rows.reduce((sum, row) => sum + (row.totalReceipts ?? 0), 0)
    };
  }, [customersQuery.data?.items, customersQuery.data?.totalCount]);

  const headerSummary = (
    <>
      <SummaryCard label="عدد العملاء" value={formatNumber(listSummary.count)} />
      <SummaryCard label="إجمالي الذمة" value={formatCurrency(listSummary.outstanding)} tone="amber" />
      <SummaryCard label="قبض مرحّل" value={formatCurrency(listSummary.receipts)} tone="green" />
    </>
  );

  function submitSearch(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSearch(searchInput.trim());
  }

  return (
    <AppShell title="العملاء" summary={headerSummary}>
      <div className="page-stack">
        <section className="form-panel form-compact form-panel--filter">
          <div className="form-section-head">
            <h2>البحث</h2>
            {can('customers.create') ? (
              <button className="chip-button" type="button" onClick={() => navigate('/customers/new')}>
                + عميل جديد
              </button>
            ) : null}
          </div>
          <form className="compact-search-row" onSubmit={submitSearch}>
            <label className="form-field">
              <span className="form-field__label">ابحث بالاسم أو الكود</span>
              <input
                value={searchInput}
                onChange={(event) => setSearchInput(event.target.value)}
                placeholder="..."
                aria-label="بحث عن عميل"
              />
            </label>
            <button className="chip-button" type="submit">بحث</button>
          </form>
        </section>

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
        <CustomerListContent customers={customersQuery.data.items} />
      ) : null}
      </div>
    </AppShell>
  );
}

function CustomerListContent({ customers }: { customers: CustomerListDto[] }) {
  return (
    <>
      <section className="card-list customer-list-cards" aria-label="قائمة العملاء">
        {customers.map((customer) => (
          <Link className="card-link" key={customer.id} to={`/customers/${customer.id}`}>
            <CustomerListCard customer={customer} />
          </Link>
        ))}
      </section>
      <div className="table-scroll customer-list-table" aria-label="قائمة العملاء — جدول">
        <table className="data-table">
          <thead>
            <tr>
              <th>الكود</th>
              <th>الاسم</th>
              <th>النوع</th>
              <th>افتتاحي</th>
              <th>مبيعات</th>
              <th>قبض</th>
              <th>المتبقي</th>
              <th>سندات قبض</th>
              <th>حد الائتمان</th>
              <th>الحالة</th>
            </tr>
          </thead>
          <tbody>
            {customers.map((customer) => (
              <tr key={customer.id}>
                <td>
                  <Link to={`/customers/${customer.id}`}>{customer.code}</Link>
                </td>
                <td>{customer.nameAr}</td>
                <td>{customerTypeLabels[customer.type as CustomerType]}</td>
                <td>{formatOpeningBalanceCell(customer)}</td>
                <td>{formatCurrency(customer.totalInvoiced ?? 0)}</td>
                <td>{formatCurrency(customer.totalReceipts ?? 0)}</td>
                <td>{formatCurrency(customer.computedBalance ?? customer.balance ?? 0)}</td>
                <td>{customer.postedReceiptCount ?? 0}</td>
                <td>
                  {customer.type === 0
                    ? '—'
                    : customer.creditLimitEnabled
                      ? formatCurrency(customer.creditLimit)
                      : 'بدون حد'}
                </td>
                <td>
                  <CustomerStatusPill status={customer.status} />
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </>
  );
}

function CustomerListCard({ customer }: { customer: CustomerListDto }) {
  const subtitle = `${customer.code} • ${customerTypeLabels[customer.type as CustomerType]}`;
  const financialMeta = [
    `افتتاحي ${formatOpeningBalanceCell(customer)}`,
    `مبيعات ${formatCurrency(customer.totalInvoiced ?? 0)}`,
    `قبض ${formatCurrency(customer.totalReceipts ?? 0)}`,
    `المتبقي ${formatCurrency(customer.computedBalance ?? customer.balance ?? 0)}`
  ].join(' • ');

  return (
    <DataCard
      icon={<Icon name="customers" />}
      title={customer.nameAr}
      subtitle={subtitle}
      meta={financialMeta}
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
      <form className="page-stack page-stack--footer" onSubmit={submit}>
        <section className="form-panel form-compact" aria-label="بيانات العميل">
          <div className="form-field-row form-field-row--2">
            <label className="form-field">
              <span className="form-field__label">الكود</span>
              <input value={form.code} onChange={(event) => update('code', event.target.value)} required />
            </label>
            <label className="form-field">
              <span className="form-field__label">النوع</span>
              <select value={form.type} onChange={(event) => update('type', event.target.value as '0' | '1')}>
                <option value="0">{customerTypeLabels[0]}</option>
                <option value="1">{customerTypeLabels[1]}</option>
              </select>
            </label>
          </div>
          <label className="form-field form-field--wide">
            <span className="form-field__label">الاسم بالعربي</span>
            <input value={form.nameAr} onChange={(event) => update('nameAr', event.target.value)} required />
          </label>
          <label className="form-field form-field--wide">
            <span className="form-field__label">الاسم بالإنجليزي</span>
            <input value={form.nameEn} onChange={(event) => update('nameEn', event.target.value)} />
          </label>
          <label className="form-field form-field--wide">
            <span className="form-field__label">حد الائتمان</span>
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
        </section>
        <div className="sticky-form-footer">
          <div className="sticky-form-footer__total">
            <span>حد الائتمان</span>
            <strong>{formatCurrency(toNumber(form.creditLimit))}</strong>
          </div>
          <button className="primary-button sticky-form-footer__submit" type="submit" disabled={mutation.isPending}>
            {mutation.isPending ? 'جار الإنشاء...' : 'إنشاء العميل'}
          </button>
        </div>
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
      <SummaryCard label="افتتاحي" value={formatOpeningBalanceCell(customer)} />
      <SummaryCard label="مبيعات" value={formatCurrency(customer.totalInvoiced ?? 0)} />
      <SummaryCard label="قبض" value={formatCurrency(customer.totalReceipts ?? 0)} tone="green" />
      <SummaryCard label="المتبقي" value={formatCurrency(customer.computedBalance ?? customer.balance ?? 0)} tone="amber" />
    </>
  ) : undefined;

  const exportPayload: DocumentExportPayload | null = customer
    ? {
        title: `بطاقة عميل ${customer.code}`,
        subtitle: customer.nameAr,
        fileName: `customer-${customer.code}.pdf`,
        shareText: `عميل: ${customer.nameAr}\nالكود: ${customer.code}\nالهاتف: ${customer.phone ?? '—'}\nالرصيد: ${formatCurrency(customer.balance)}`,
        sections: [
          {
            heading: 'بيانات العميل',
            rows: [
              { label: 'الاسم', value: customer.nameAr },
              { label: 'الكود', value: customer.code },
              { label: 'النوع', value: customerTypeLabels[customer.type as CustomerType] },
              { label: 'الهاتف', value: customer.phone ?? '—' },
              { label: 'البريد', value: customer.email ?? '—' },
              { label: 'الرصيد', value: formatCurrency(customer.balance) },
              { label: 'حد الائتمان', value: formatCurrency(customer.creditLimit) },
              { label: 'شروط الدفع', value: `${formatNumber(customer.paymentTermsDays)} يوم` }
            ]
          }
        ]
      }
    : null;

  return (
    <AppShell title={customer ? customer.nameAr : 'تفاصيل العميل'} summary={headerSummary}>
      <Toast toast={toast} onClose={() => setToast(null)} />

      {detailsQuery.isLoading ? <LoadingState /> : null}

      {detailsQuery.isError ? (
        <ErrorState message={getErrorMessage(detailsQuery.error)} onRetry={() => void detailsQuery.refetch()} />
      ) : null}

      {customer ? (
        <div className="page-stack">
          <section className="form-panel form-compact">
            <div className="compact-hero">
              <div>
                <p className="compact-hero__eyebrow">{customer.code}</p>
                <h2>{customer.nameAr}</h2>
              </div>
              <CustomerStatusPill status={customer.status} />
            </div>
          </section>

          <section className="form-panel form-compact">
            <h2>بيانات العميل</h2>
            <dl className="detail-grid">
              <DetailItem label="النوع" value={customerTypeLabels[customer.type as CustomerType]} />
              <DetailItem label="شروط الدفع" value={`${formatNumber(customer.paymentTermsDays)} يوم`} />
              <DetailItem label="الهاتف" value={customer.phone ?? 'غير محدد'} />
              <DetailItem label="البريد" value={customer.email ?? 'غير محدد'} />
            </dl>
          </section>

          <section className="form-panel form-compact">
            <h2>الملخص المالي</h2>
            <dl className="detail-grid">
              <DetailItem label="افتتاحي" value={formatOpeningBalanceCell(customer)} />
              <DetailItem label="مبيعات" value={formatCurrency(customer.totalInvoiced ?? 0)} />
              <DetailItem label="قبض" value={formatCurrency(customer.totalReceipts ?? 0)} />
              <DetailItem label="المتبقي" value={formatCurrency(customer.computedBalance ?? customer.balance ?? 0)} />
              <DetailItem label="سندات قبض" value={formatNumber(customer.postedReceiptCount ?? 0)} />
              <DetailItem label="فواتير مفتوحة" value={formatNumber(customer.openInvoicesCount ?? 0)} />
              <DetailItem label="حد الائتمان" value={formatCurrency(customer.creditLimit)} />
            </dl>
          </section>

          <DocumentActions
            payload={exportPayload}
            onToast={(message, tone = 'success') => setToast({ tone, message })}
          />

          <section className="compact-action-row" aria-label="إجراءات العميل">
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

          <section className="form-panel form-compact">
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

            <div className="form-field-row form-field-row--2">
              <label className="form-field">
                <span className="form-field__label">من تاريخ</span>
                <input type="date" value={from} onChange={(event) => setFrom(event.target.value)} />
              </label>
              <label className="form-field">
                <span className="form-field__label">إلى تاريخ</span>
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
    <form className="form-panel form-compact" onSubmit={submit}>
      <div className="form-section-head">
        <h2>تعديل بيانات العميل</h2>
      </div>
      <label className="form-field form-field--wide">
        <span className="form-field__label">الاسم بالعربي</span>
        <input value={form.nameAr} onChange={(event) => update('nameAr', event.target.value)} required />
      </label>
      <label className="form-field form-field--wide">
        <span className="form-field__label">الاسم بالإنجليزي</span>
        <input value={form.nameEn} onChange={(event) => update('nameEn', event.target.value)} />
      </label>
      <div className="form-field-row form-field-row--2">
        <label className="form-field">
          <span className="form-field__label">حد الائتمان</span>
          <input inputMode="decimal" value={form.creditLimit} onChange={(event) => update('creditLimit', event.target.value)} />
        </label>
        <label className="form-field">
          <span className="form-field__label">شروط الدفع (أيام)</span>
          <input inputMode="numeric" value={form.paymentTermsDays} onChange={(event) => update('paymentTermsDays', event.target.value)} />
        </label>
      </div>
      <label className="toggle-row">
        <input
          type="checkbox"
          checked={form.creditLimitEnabled}
          onChange={(event) => update('creditLimitEnabled', event.target.checked)}
        />
        تفعيل حد الائتمان
      </label>
      <button className="primary-button" type="submit" disabled={mutation.isPending}>
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
    <form className="form-panel form-compact" onSubmit={submit}>
      <div className="form-section-head">
        <h2>ترحيل رصيد افتتاحي</h2>
      </div>
      <div className="form-field-row form-field-row--2">
        <label className="form-field">
          <span className="form-field__label">المبلغ</span>
          <input inputMode="decimal" value={amount} onChange={(event) => setAmount(event.target.value)} />
        </label>
        <label className="form-field">
          <span className="form-field__label">تاريخ الترحيل</span>
          <input type="date" value={postingDate} onChange={(event) => setPostingDate(event.target.value)} />
        </label>
      </div>
      <label className="form-field form-field--wide">
        <span className="form-field__label">ملاحظة مرجعية</span>
        <input value={note} onChange={(event) => setNote(event.target.value)} />
      </label>
      <button className="primary-button" type="submit" disabled={mutation.isPending}>
        {mutation.isPending ? 'جار الترحيل...' : 'ترحيل الرصيد'}
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
  const [paymentMethodId, setPaymentMethodId] = useState('');
  const [cashboxId, setCashboxId] = useState('');
  const [bankAccountId, setBankAccountId] = useState('');
  const [reference, setReference] = useState('');
  const [amount, setAmount] = useState('0');

  const paymentMethodsQuery = useQuery({
    queryKey: ['finance', 'payment-methods'],
    queryFn: getPaymentMethods
  });
  const cashboxesQuery = useQuery({
    queryKey: ['lookups', 'cashboxes'],
    queryFn: getCashboxLookups
  });
  const bankAccountsQuery = useQuery({
    queryKey: ['finance', 'bank-accounts'],
    queryFn: getBankAccounts
  });

  const selectedMethod = (paymentMethodsQuery.data ?? []).find((m) => m.id === paymentMethodId);
  const requiresCashbox = selectedMethod?.requiresCashbox ?? true;
  const requiresBank = selectedMethod?.requiresBankAccount ?? false;
  const requiresReference = selectedMethod?.requiresReference ?? false;

  const mutation = useMutation({
    mutationFn: async () => {
      const voucherId = await createReceiptVoucher({
        customerId,
        amount: toNumber(amount),
        paymentMethodId: paymentMethodId || undefined,
        cashboxId: requiresCashbox ? cashboxId : undefined,
        bankAccountId: requiresBank ? bankAccountId : undefined,
        reference: requiresReference ? reference : undefined,
        currency: 'USD',
        allocations: []
      });
      await approveReceiptVoucher(voucherId);
      await postReceiptVoucher(voucherId);
      try {
        const pdfBlob = await getReceiptVoucherPdf(voucherId);
        downloadPdfBlob(pdfBlob, `سند قبض - ${voucherId}.pdf`);
      } catch {
        /* voucher is posted regardless — PDF download is a bonus, not a blocker */
      }
    },
    onSuccess: () => onDone('تم إنشاء وترحيل سند القبض بنجاح.'),
    onError: (error) => onToast(errorToast(error))
  });

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!paymentMethodId.trim()) {
      onToast({ tone: 'error', message: 'اختر وسيلة الدفع.' });
      return;
    }
    if (requiresCashbox && !cashboxId.trim()) {
      onToast({ tone: 'error', message: 'اختر الصندوق أولاً.' });
      return;
    }
    if (requiresBank && !bankAccountId.trim()) {
      onToast({ tone: 'error', message: 'اختر الحساب البنكي.' });
      return;
    }
    if (requiresReference && !reference.trim()) {
      onToast({ tone: 'error', message: 'أدخل مرجع التحويل.' });
      return;
    }
    if (toNumber(amount) <= 0) {
      onToast({ tone: 'error', message: 'المبلغ يجب أن يكون أكبر من صفر.' });
      return;
    }
    mutation.mutate();
  }

  return (
    <form className="form-panel form-compact" onSubmit={submit}>
      <div className="form-section-head">
        <h2>سند قبض</h2>
      </div>
      <label className="form-field form-field--wide">
        <span className="form-field__label">وسيلة الدفع</span>
        <select
          value={paymentMethodId}
          onChange={(event) => setPaymentMethodId(event.target.value)}
          disabled={paymentMethodsQuery.isLoading || paymentMethodsQuery.isError}
          required
        >
          <option value="">اختر وسيلة الدفع...</option>
          {(paymentMethodsQuery.data ?? []).map((method) => (
            <option key={method.id} value={method.id}>
              {method.name}
            </option>
          ))}
        </select>
      </label>
      {requiresCashbox ? (
        <label className="form-field form-field--wide">
          <span className="form-field__label">الصندوق</span>
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
        </label>
      ) : null}
      {requiresBank ? (
        <label className="form-field form-field--wide">
          <span className="form-field__label">الحساب البنكي</span>
          <select
            value={bankAccountId}
            onChange={(event) => setBankAccountId(event.target.value)}
            disabled={bankAccountsQuery.isLoading || bankAccountsQuery.isError}
            required
          >
            <option value="">اختر البنك...</option>
            {(bankAccountsQuery.data ?? []).map((bank) => (
              <option key={bank.id} value={bank.id}>
                {bank.name} — {bank.bankName}
              </option>
            ))}
          </select>
        </label>
      ) : null}
      {requiresReference ? (
        <label className="form-field form-field--wide">
          <span className="form-field__label">مرجع التحويل</span>
          <input value={reference} onChange={(event) => setReference(event.target.value)} required />
        </label>
      ) : null}
      <label className="form-field form-field--wide">
        <span className="form-field__label">المبلغ (USD)</span>
        <input inputMode="decimal" value={amount} onChange={(event) => setAmount(event.target.value)} />
      </label>
      <p className="form-hint">مسودة → اعتماد → ترحيل. لا تخصيص على فواتير من الويب حالياً.</p>
      <button className="primary-button" type="submit" disabled={mutation.isPending}>
        {mutation.isPending ? 'جار التنفيذ...' : 'حفظ وترحيل'}
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

      <DocumentActions
        payload={{
          title: `كشف حساب ${ledger.customerName}`,
          subtitle: `${from || '—'} → ${to || '—'}`,
          fileName: `customer-ledger-${ledger.customerName}.pdf`,
          shareText: `كشف حساب: ${ledger.customerName}\nافتتاحي: ${ledger.openingBalance}\nختامي: ${ledger.closingBalance}`,
          sections: [
            {
              heading: 'ملخص الكشف',
              rows: [
                { label: 'الرصيد الافتتاحي', value: String(ledger.openingBalance) },
                { label: 'الرصيد الختامي', value: String(ledger.closingBalance) },
                { label: 'عدد الحركات', value: String(ledger.lines.length) }
              ]
            }
          ]
        }}
        pdfSource={{
          fileName: `كشف حساب - ${ledger.customerName} - ${new Date().toISOString().slice(0, 10)}.pdf`,
          load: () => getCustomerAccountLedgerPdf(customerId, { from: from || undefined, to: to || undefined })
        }}
        onToast={(message, tone = 'success') => onToast({ tone, message })}
      />

      <div className="form-section-head">
        <button
          className="chip-button"
          type="button"
          onClick={openReconcilePanel}
          disabled={ledger.lines.length === 0}
        >
          مطابقة الكشف
        </button>
      </div>

      {showReconcile && selectedLineIndex !== null ? (
        <form className="form-panel form-compact" onSubmit={submitReconcile}>
          <p className="form-hint">المطابقة الجديدة تحل محل السابقة.</p>
          <label className="form-field form-field--wide">
            <span className="form-field__label">السطر المرجعي</span>
            <select value={selectedLineIndex} onChange={(event) => setSelectedLineIndex(Number(event.target.value))}>
              {ledger.lines.map((line, index) => (
                <option key={index} value={index}>
                  {formatDateOnly(line.transactionDate)} — {line.documentNumber} ({formatCurrency(line.runningBalance)})
                </option>
              ))}
            </select>
          </label>
          <button className="primary-button" type="submit" disabled={reconcileMutation.isPending}>
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
                    <td>{line.totalLengthDisplay ?? (line.totalMeters != null ? formatSalesLineLength(line.totalMeters, { unit: line.lengthUnit }) : '—')}</td>
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
        <RecordField label="مجموع الأطوال" value={line.totalLengthDisplay ?? (line.totalMeters != null ? formatSalesLineLength(line.totalMeters, { unit: line.lengthUnit }) : '—')} />
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
  return getApiErrorMessage(error);
}
