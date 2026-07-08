import { useEffect, useMemo, useState, type FormEvent } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useLocation, useNavigate, useParams } from 'react-router-dom';
import {
  approveSalesInvoice,
  cancelSalesInvoice,
  createSalesInvoice,
  getSalesInvoice,
  getSalesInvoices,
  getSalesWarehouseStock,
  sendSalesInvoiceToWarehouse
} from '../api/sales.ts';
import { getCustomers } from '../api/customers.ts';
import { getContainers } from '../api/containers.ts';
import { getInventoryWarehouses } from '../api/inventory.ts';
import { ApiError } from '../api/client.ts';
import type {
  CreateSalesInvoiceLineRequest,
  PaymentType,
  SalesInvoiceDto,
  SalesInvoiceStatus,
  SalesWarehouseStockOptionDto
} from '../api/types.ts';
import { useAuth } from '../auth/AuthContext.tsx';
import { AppShell } from '../components/AppShell.tsx';
import { DataCard } from '../components/DataCard.tsx';
import { EmptyState } from '../components/EmptyState.tsx';
import { ErrorState } from '../components/ErrorState.tsx';
import { Icon } from '../components/Icon.tsx';
import { LoadingState } from '../components/LoadingState.tsx';
import { SummaryCard } from '../components/SummaryCard.tsx';
import { formatCurrency, formatDate, formatMeters, formatNumber } from '../lib/format.ts';
import {
  getJournalEntryStatusTone,
  getSalesInvoiceStatusTone,
  journalEntryStatusLabel,
  paymentTypeLabel,
  salesInvoiceStatusLabel,
  salesInvoiceStatusOptions
} from '../lib/enums.ts';

const LIST_PAGE_SIZE = 100;

type ToastState = { tone: 'success' | 'error'; message: string };

export function SalesPage() {
  const { invoiceId } = useParams();
  const location = useLocation();

  if (location.pathname === '/sales/new') {
    return <SalesCreatePage />;
  }
  if (invoiceId) {
    return <SalesDetailPage invoiceId={invoiceId} />;
  }
  return <SalesListPage />;
}

function SalesListPage() {
  const { can } = useAuth();
  const navigate = useNavigate();
  const [status, setStatus] = useState<'' | string>('');

  const invoicesQuery = useQuery({
    queryKey: ['sales-invoices', status],
    queryFn: () =>
      getSalesInvoices({
        status: status === '' ? undefined : (Number(status) as SalesInvoiceStatus),
        page: 1,
        pageSize: LIST_PAGE_SIZE
      })
  });

  const rows = invoicesQuery.data?.items ?? [];
  const summary = useMemo(() => {
    const total = rows.reduce((sum, row) => sum + row.grandTotal, 0);
    const pending = rows.filter((row) => row.status <= 3).length;
    return { count: invoicesQuery.data?.totalCount ?? rows.length, total, pending };
  }, [rows, invoicesQuery.data?.totalCount]);

  const headerSummary = (
    <>
      <SummaryCard label="عدد الفواتير" value={formatNumber(summary.count)} />
      <SummaryCard label="إجمالي القيمة" value={formatCurrency(summary.total)} tone="green" />
      <SummaryCard label="قيد المعالجة" value={formatNumber(summary.pending)} tone="amber" />
    </>
  );

  return (
    <AppShell title="فواتير البيع" summary={headerSummary}>
      <section className="toolbar-row">
        {can('sales.create') ? (
          <button className="primary-button" type="button" onClick={() => navigate('/sales/new')}>
            فاتورة جديدة
          </button>
        ) : null}
      </section>

      <section className="toolbar-row toolbar-row--start">
        <label className="inline-field">
          الحالة
          <select value={status} onChange={(event) => setStatus(event.target.value)}>
            <option value="">كل الحالات</option>
            {salesInvoiceStatusOptions.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </label>
      </section>

      {invoicesQuery.isLoading ? <LoadingState /> : null}
      {invoicesQuery.isError ? (
        <ErrorState message={getErrorMessage(invoicesQuery.error)} onRetry={() => void invoicesQuery.refetch()} />
      ) : null}
      {invoicesQuery.isSuccess && rows.length === 0 ? (
        <EmptyState title="لا توجد فواتير" description="لم يتم العثور على فواتير بيع مطابقة." />
      ) : null}

      {rows.length > 0 ? (
        <section className="card-list" aria-label="قائمة فواتير البيع">
          {rows.map((invoice) => (
            <Link className="card-link" key={invoice.id} to={`/sales/${invoice.id}`}>
              <SalesListCard invoice={invoice} />
            </Link>
          ))}
        </section>
      ) : null}
    </AppShell>
  );
}

function SalesListCard({ invoice }: { invoice: SalesInvoiceDto }) {
  return (
    <DataCard
      icon={<Icon name="sales" />}
      title={invoice.invoiceNumber}
      subtitle={invoice.customerName || 'عميل غير محدد'}
      meta={`${formatDate(invoice.invoiceDate)} • ${paymentTypeLabel(invoice.paymentType)}`}
      value={<StatusPill status={invoice.status} />}
      tone={invoice.status === 4 || invoice.status === 6 ? 'available' : invoice.status === 7 ? 'danger' : 'neutral'}
    />
  );
}

function StatusPill({ status }: { status: SalesInvoiceStatus }) {
  return (
    <span className={`status-pill status-pill--${getSalesInvoiceStatusTone(status)}`}>
      {salesInvoiceStatusLabel(status)}
    </span>
  );
}

function SalesDetailPage({ invoiceId }: { invoiceId: string }) {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [toast, setToast] = useState<ToastState | null>(null);

  const opsQuery = useQuery({
    queryKey: ['sales-invoice', invoiceId],
    queryFn: () => getSalesInvoice(invoiceId)
  });

  async function refresh(message: string) {
    await queryClient.invalidateQueries({ queryKey: ['sales-invoice', invoiceId] });
    await queryClient.invalidateQueries({ queryKey: ['sales-invoices'] });
    setToast({ tone: 'success', message });
  }

  const sendMutation = useMutation({
    mutationFn: () => sendSalesInvoiceToWarehouse(invoiceId),
    onSuccess: () => void refresh('تم إرسال الفاتورة إلى المستودع.'),
    onError: (error) => setToast({ tone: 'error', message: getErrorMessage(error) })
  });

  const approveMutation = useMutation({
    mutationFn: () => approveSalesInvoice(invoiceId),
    onSuccess: () => void refresh('تم اعتماد الفاتورة وإنشاء القيود المحاسبية.'),
    onError: (error) => setToast({ tone: 'error', message: getErrorMessage(error) })
  });

  const cancelMutation = useMutation({
    mutationFn: (reason: string) => cancelSalesInvoice(invoiceId, reason),
    onSuccess: () => void refresh('تم إلغاء الفاتورة.'),
    onError: (error) => setToast({ tone: 'error', message: getErrorMessage(error) })
  });

  function handleCancel() {
    const reason = window.prompt('سبب الإلغاء؟', 'إلغاء بواسطة المستخدم');
    if (reason && reason.trim()) {
      cancelMutation.mutate(reason.trim());
    }
  }

  const ops = opsQuery.data;
  const invoice = ops?.invoice;

  const headerSummary = invoice ? (
    <>
      <SummaryCard label="الإجمالي" value={formatCurrency(invoice.grandTotal)} tone="green" />
      <SummaryCard label="المحصّل" value={formatCurrency(ops?.collectedAmount ?? 0)} />
      <SummaryCard label="المتبقي" value={formatCurrency(ops?.remainingBalance ?? 0)} tone="amber" />
    </>
  ) : undefined;

  return (
    <AppShell title={invoice ? invoice.invoiceNumber : 'تفاصيل الفاتورة'} summary={headerSummary}>
      <Toast toast={toast} onClose={() => setToast(null)} />

      {opsQuery.isLoading ? <LoadingState /> : null}
      {opsQuery.isError ? (
        <ErrorState message={getErrorMessage(opsQuery.error)} onRetry={() => void opsQuery.refetch()} />
      ) : null}

      {ops && invoice ? (
        <div className="details-stack">
          <section className="detail-card detail-card--hero">
            <div className="detail-card__lead">
              <p className="detail-card__eyebrow">{invoice.customerName}</p>
              <h2>{invoice.invoiceNumber}</h2>
              <StatusPill status={invoice.status} />
            </div>
          </section>

          <section className="detail-card">
            <h2>بيانات الفاتورة</h2>
            <dl className="detail-grid">
              <DetailItem label="التاريخ" value={formatDate(invoice.invoiceDate)} />
              <DetailItem label="نوع الدفع" value={paymentTypeLabel(invoice.paymentType)} />
              <DetailItem label="المستودع" value={ops.warehouseName ?? 'غير محدد'} />
              <DetailItem label="هاتف العميل" value={ops.customerPhone ?? 'غير محدد'} />
              <DetailItem label="الإجمالي الفرعي" value={formatCurrency(invoice.subTotal)} />
              <DetailItem label="الخصم" value={formatCurrency(invoice.discountTotal)} />
              <DetailItem label="الإجمالي" value={formatCurrency(invoice.grandTotal)} />
            </dl>
          </section>

          <section className="action-grid" aria-label="إجراءات الفاتورة">
            {ops.canSendToWarehouse ? (
              <button className="primary-button primary-button--wide" type="button" onClick={() => sendMutation.mutate()} disabled={sendMutation.isPending}>
                {sendMutation.isPending ? 'جار الإرسال...' : 'إرسال إلى المستودع'}
              </button>
            ) : null}
            {ops.canApprove ? (
              <button className="primary-button primary-button--wide" type="button" onClick={() => approveMutation.mutate()} disabled={approveMutation.isPending}>
                {approveMutation.isPending ? 'جار الاعتماد...' : 'اعتماد الفاتورة'}
              </button>
            ) : null}
            {ops.canCancel ? (
              <button className="primary-button primary-button--wide" type="button" onClick={handleCancel} disabled={cancelMutation.isPending}>
                {cancelMutation.isPending ? 'جار الإلغاء...' : 'إلغاء الفاتورة'}
              </button>
            ) : null}
          </section>

          <section className="detail-card">
            <h2>أصناف الفاتورة</h2>
            {invoice.lines.length === 0 ? (
              <EmptyState title="لا توجد أصناف" description="لم تُضف أصناف لهذه الفاتورة." />
            ) : (
              <div className="table-scroll">
                <table className="data-table">
                  <thead>
                    <tr>
                      <th>#</th>
                      <th>الصنف</th>
                      <th>اللون</th>
                      <th>الأثواب</th>
                      <th>الأمتار</th>
                      <th>السعر</th>
                      <th>الإجمالي</th>
                    </tr>
                  </thead>
                  <tbody>
                    {invoice.lines.map((line) => (
                      <tr key={line.id}>
                        <td>{line.lineNumber}</td>
                        <td>{line.fabricDisplayName}</td>
                        <td>{line.colorDisplayName}</td>
                        <td>{formatNumber(line.rollCount)}</td>
                        <td>{formatMeters(line.totalLengthMeters)}</td>
                        <td>{formatCurrency(line.unitPrice)}</td>
                        <td>{formatCurrency(line.lineTotal)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </section>

          {ops.journalEntries.length > 0 ? (
            <section className="detail-card">
              <h2>القيود المحاسبية المرتبطة</h2>
              <div className="line-list">
                {ops.journalEntries.map((entry) => (
                  <div className="price-row" key={entry.id}>
                    <span>
                      {entry.entryNumber} — {entry.description}
                      <span className={`status-pill status-pill--${getJournalEntryStatusTone(entry.status)} status-pill--inline`}>
                        {journalEntryStatusLabel(entry.status)}
                      </span>
                    </span>
                    <strong>{formatCurrency(entry.debitTotal)}</strong>
                  </div>
                ))}
              </div>
            </section>
          ) : null}

          {ops.payments.length > 0 ? (
            <section className="detail-card">
              <h2>الدفعات المحصّلة</h2>
              <div className="line-list">
                {ops.payments.map((payment) => (
                  <div className="price-row" key={payment.receiptVoucherId}>
                    <span>
                      {payment.receiptNumber} — {formatDate(payment.appliedAt)}
                    </span>
                    <strong>{formatCurrency(payment.amount)}</strong>
                  </div>
                ))}
              </div>
            </section>
          ) : null}

          <button className="ghost-button" type="button" onClick={() => navigate('/sales')}>
            العودة إلى القائمة
          </button>
        </div>
      ) : null}
    </AppShell>
  );
}

type DraftLine = {
  key: string;
  stockKey: string;
  rollCount: string;
  unitPrice: string;
};

function SalesCreatePage() {
  const navigate = useNavigate();
  const [toast, setToast] = useState<ToastState | null>(null);
  const [customerId, setCustomerId] = useState('');
  const [warehouseId, setWarehouseId] = useState('');
  const [containerId, setContainerId] = useState('');
  const [paymentType, setPaymentType] = useState<'0' | '1'>('0');
  const [discount, setDiscount] = useState('0');
  const [lines, setLines] = useState<DraftLine[]>([]);

  const customersQuery = useQuery({
    queryKey: ['customers', 'lookup'],
    queryFn: () => getCustomers({ page: 1, pageSize: 500 })
  });

  const warehousesQuery = useQuery({
    queryKey: ['inventory', 'warehouses'],
    queryFn: () => getInventoryWarehouses()
  });

  const containersQuery = useQuery({
    queryKey: ['containers', 'in-warehouse'],
    queryFn: () => getContainers({ status: 6, page: 1, pageSize: 200 })
  });

  useEffect(() => {
    if (warehouseId || !warehousesQuery.data || warehousesQuery.data.length === 0) {
      return;
    }
    const preferred =
      warehousesQuery.data.find((warehouse) => warehouse.isDefault) ??
      warehousesQuery.data.find((warehouse) => warehouse.nameAr?.includes('رئيسي')) ??
      warehousesQuery.data[0];
    if (preferred) {
      setWarehouseId(preferred.id);
    }
  }, [warehouseId, warehousesQuery.data]);

  const stockQuery = useQuery({
    queryKey: ['sales-warehouse-stock', containerId, warehouseId],
    queryFn: () => getSalesWarehouseStock(containerId, warehouseId),
    enabled: containerId.length > 0 && warehouseId.length > 0
  });

  const stockOptions = stockQuery.data ?? [];

  function stockKeyOf(option: SalesWarehouseStockOptionDto) {
    return `${option.fabricItemId}::${option.fabricColorId}`;
  }

  function findStock(stockKey: string) {
    return stockOptions.find((option) => stockKeyOf(option) === stockKey);
  }

  function addLine() {
    setLines((current) => [
      ...current,
      { key: `${Date.now()}-${Math.random()}`, stockKey: '', rollCount: '1', unitPrice: '0' }
    ]);
  }

  function updateLine(key: string, patch: Partial<DraftLine>) {
    setLines((current) => current.map((line) => (line.key === key ? { ...line, ...patch } : line)));
  }

  function removeLine(key: string) {
    setLines((current) => current.filter((line) => line.key !== key));
  }

  const estimatedTotal = useMemo(() => {
    return lines.reduce((sum, line) => {
      const stock = findStock(line.stockKey);
      if (!stock || stock.availableRollCount === 0) {
        return sum;
      }
      const rolls = toNumber(line.rollCount);
      const avgPerRoll = stock.availableMeters / stock.availableRollCount;
      return sum + rolls * avgPerRoll * toNumber(line.unitPrice);
    }, 0);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [lines, stockOptions]);

  const mutation = useMutation({
    mutationFn: () => {
      const payloadLines: CreateSalesInvoiceLineRequest[] = lines
        .map((line, index): CreateSalesInvoiceLineRequest | null => {
          const stock = findStock(line.stockKey);
          if (!stock) {
            return null;
          }
          const unitPrice = toNumber(line.unitPrice);
          return {
            lineNumber: index + 1,
            fabricItemId: stock.fabricItemId,
            fabricColorId: stock.fabricColorId,
            rollCount: Math.round(toNumber(line.rollCount)),
            unitPrice,
            originalUnitPrice: stock.salePricePerMeter ?? unitPrice,
            discountReason: null,
            notes: null
          };
        })
        .filter((line): line is CreateSalesInvoiceLineRequest => line !== null);

      return createSalesInvoice({
        customerId,
        warehouseId,
        chinaContainerId: containerId,
        paymentType: Number(paymentType) as PaymentType,
        discountAmount: toNumber(discount),
        invoiceNumber: null,
        lines: payloadLines
      });
    },
    onSuccess: (id) => navigate(`/sales/${id}`),
    onError: (error) => setToast({ tone: 'error', message: getErrorMessage(error) })
  });

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!customerId) {
      setToast({ tone: 'error', message: 'اختر العميل أولاً.' });
      return;
    }
    if (!containerId) {
      setToast({ tone: 'error', message: 'اختر الحاوية المصدر أولاً.' });
      return;
    }
    const validLines = lines.filter((line) => line.stockKey && toNumber(line.rollCount) > 0);
    if (validLines.length === 0) {
      setToast({ tone: 'error', message: 'أضف صنفًا واحدًا على الأقل مع عدد أثواب صحيح.' });
      return;
    }
    mutation.mutate();
  }

  return (
    <AppShell title="فاتورة بيع جديدة">
      <Toast toast={toast} onClose={() => setToast(null)} />
      <form className="sales-create" onSubmit={submit}>
        <section className="form-panel form-compact" aria-label="بيانات الفاتورة">
          <label className="form-field form-field--wide">
            <span className="form-field__label">العميل</span>
            <select value={customerId} onChange={(event) => setCustomerId(event.target.value)} required>
              <option value="">اختر العميل...</option>
              {(customersQuery.data?.items ?? []).map((customer) => (
                <option key={customer.id} value={customer.id}>
                  {customer.nameAr}
                </option>
              ))}
            </select>
          </label>

          <div className="form-field-row form-field-row--2">
            <label className="form-field">
              <span className="form-field__label">المستودع</span>
              <select value={warehouseId} onChange={(event) => setWarehouseId(event.target.value)} required>
                <option value="">اختر...</option>
                {(warehousesQuery.data ?? []).map((warehouse) => (
                  <option key={warehouse.id} value={warehouse.id}>
                    {warehouse.nameAr}
                  </option>
                ))}
              </select>
            </label>
            <label className="form-field">
              <span className="form-field__label">الحاوية</span>
              <select value={containerId} onChange={(event) => setContainerId(event.target.value)} required>
                <option value="">اختر...</option>
                {(containersQuery.data?.items ?? []).map((container) => (
                  <option key={container.id} value={container.id}>
                    {container.containerNumber}
                  </option>
                ))}
              </select>
            </label>
          </div>

          <div className="form-field-row form-field-row--2">
            <label className="form-field">
              <span className="form-field__label">نوع الدفع</span>
              <select value={paymentType} onChange={(event) => setPaymentType(event.target.value as '0' | '1')}>
                <option value="0">{paymentTypeLabel(0)}</option>
                <option value="1">{paymentTypeLabel(1)}</option>
              </select>
            </label>
            <label className="form-field">
              <span className="form-field__label">الخصم</span>
              <input inputMode="decimal" value={discount} onChange={(event) => setDiscount(event.target.value)} />
            </label>
          </div>
        </section>

        <section className="form-panel form-compact" aria-label="أصناف الفاتورة">
          <div className="form-section-head">
            <h2>الأصناف</h2>
            <button
              className="chip-button"
              type="button"
              onClick={addLine}
              disabled={!containerId || !warehouseId || stockQuery.isLoading}
            >
              + إضافة
            </button>
          </div>

          {!containerId || !warehouseId ? (
            <p className="form-hint">اختر الحاوية والمستودع لعرض الأصناف.</p>
          ) : null}
          {stockQuery.isLoading ? <LoadingState /> : null}
          {stockQuery.isError ? (
            <ErrorState message={getErrorMessage(stockQuery.error)} onRetry={() => void stockQuery.refetch()} />
          ) : null}
          {containerId && warehouseId && stockQuery.isSuccess && stockOptions.length === 0 ? (
            <p className="form-hint form-hint--warn">لا يوجد مخزون متاح.</p>
          ) : null}

          <div className="line-items">
            {lines.map((line, index) => {
              const stock = findStock(line.stockKey);
              return (
                <article className="line-item" key={line.key}>
                  <div className="line-item__head">
                    <span className="line-item__index">#{index + 1}</span>
                    <button className="line-item__remove" type="button" onClick={() => removeLine(line.key)} aria-label="حذف الصنف">
                      حذف
                    </button>
                  </div>
                  <label className="form-field form-field--wide">
                    <span className="form-field__label">الصنف</span>
                    <select
                      value={line.stockKey}
                      onChange={(event) => {
                        const option = findStock(event.target.value);
                        updateLine(line.key, {
                          stockKey: event.target.value,
                          unitPrice: option?.salePricePerMeter != null ? String(option.salePricePerMeter) : line.unitPrice
                        });
                      }}
                    >
                      <option value="">اختر الصنف...</option>
                      {stockOptions.map((option) => (
                        <option key={stockKeyOf(option)} value={stockKeyOf(option)}>
                          {option.display}
                        </option>
                      ))}
                    </select>
                  </label>
                  <div className="form-field-row form-field-row--2">
                    <label className="form-field">
                      <span className="form-field__label">الأثواب</span>
                      <input inputMode="numeric" value={line.rollCount} onChange={(event) => updateLine(line.key, { rollCount: event.target.value })} />
                    </label>
                    <label className="form-field">
                      <span className="form-field__label">سعر/م</span>
                      <input inputMode="decimal" value={line.unitPrice} onChange={(event) => updateLine(line.key, { unitPrice: event.target.value })} />
                    </label>
                  </div>
                  {stock ? (
                    <p className="line-item__meta">
                      متاح {formatNumber(stock.availableRollCount)} ثوب · {formatMeters(stock.availableMeters)}
                      {stock.salePricePerMeter != null ? ` · بطاقة ${formatCurrency(stock.salePricePerMeter)}` : ''}
                    </p>
                  ) : null}
                </article>
              );
            })}
          </div>
        </section>

        <div className="sales-create__footer">
          <div className="sales-create__total">
            <span>الإجمالي التقديري</span>
            <strong>{formatCurrency(estimatedTotal)}</strong>
          </div>
          <button className="primary-button sales-create__submit" type="submit" disabled={mutation.isPending}>
            {mutation.isPending ? 'جار الإنشاء...' : 'إنشاء مسودة'}
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

function getErrorMessage(error: unknown) {
  if (error instanceof ApiError) {
    if (error.status === 403) {
      return 'لا تملك صلاحية لهذا الإجراء.';
    }
    if (error.status === 404) {
      return 'الفاتورة غير موجودة.';
    }
    if (error.status === 409) {
      return `تعذّر تنفيذ الإجراء: ${error.message}`;
    }
    return error.message;
  }
  return 'حدث خطأ غير متوقع.';
}
