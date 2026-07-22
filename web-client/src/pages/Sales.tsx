import { useEffect, useMemo, useState, type FormEvent } from 'react';
import { useMutation, useQueries, useQuery, useQueryClient, keepPreviousData } from '@tanstack/react-query';
import { Link, useLocation, useNavigate, useParams } from 'react-router-dom';
import {
  approveSalesInvoice,
  calculateSalesInvoiceTax,
  cancelSalesInvoice,
  createSalesInvoice,
  getSalesInvoice,
  getSalesInvoicePdf,
  getSalesInvoices,
  getSalesWarehouseStock,
  getTaxCodes,
  sendSalesInvoiceToWarehouse
} from '../api/sales.ts';
import { getCustomers } from '../api/customers.ts';
import { getContainers } from '../api/containers.ts';
import { getInventoryWarehouses } from '../api/inventory.ts';
import { getCashboxLookups } from '../api/lookups.ts';
import { getApiErrorMessage } from '../lib/apiError.ts';
import type {
  CalculateSalesInvoiceTaxRequest,
  CreateSalesInvoiceLineRequest,
  PaymentType,
  SalesInvoiceDto,
  SalesInvoiceStatus,
  SalesWarehouseStockOptionDto,
  TaxCodeDto
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
import { formatCurrency, formatDate, formatLineIndex, formatNumber, formatSalesLineLength, formatContainerLength, perUnitLabel } from '../lib/format.ts';
import {
  getJournalEntryStatusTone,
  getSalesInvoiceStatusTone,
  journalEntryStatusLabel,
  paymentTypeLabel,
  salesInvoiceStatusLabel,
  salesInvoiceStatusOptions
} from '../lib/enums.ts';

const LIST_PAGE_SIZE = 100;
/** Seeded default VAT 15% exclusive — matches ERPSystem.Application.Common.SalesTaxCodeIds */
const DEFAULT_VAT_TAX_CODE_ID = 'c1000002-0002-0002-0002-000000000002';

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
      <div className="page-stack">
        <section className="form-panel form-compact form-panel--filter">
          <div className="form-section-head">
            <h2>الفواتير</h2>
            {can('sales.create') ? (
              <button className="chip-button" type="button" onClick={() => navigate('/sales/new')}>
                + فاتورة جديدة
              </button>
            ) : null}
          </div>
          <label className="form-field form-field--wide">
            <span className="form-field__label">الحالة</span>
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
      </div>
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
    onSuccess: () => void refresh('تم اعتماد الفاتورة وخصم المخزون وإنشاء القيود المحاسبية.'),
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

  const headerSummary = (
    <>
      <SummaryCard label="الإجمالي" value={formatCurrency(invoice?.grandTotal ?? 0)} tone="green" />
      <SummaryCard label="المحصّل" value={formatCurrency(ops?.collectedAmount ?? 0)} />
      <SummaryCard label="المتبقي" value={formatCurrency(ops?.remainingBalance ?? 0)} tone="amber" />
    </>
  );

  const exportPayload: DocumentExportPayload | null = invoice
    ? {
        title: `فاتورة بيع ${invoice.invoiceNumber}`,
        subtitle: invoice.customerName || 'عميل غير محدد',
        fileName: `sales-invoice-${invoice.invoiceNumber}.pdf`,
        shareText: `فاتورة بيع ${invoice.invoiceNumber}\nالعميل: ${invoice.customerName || '—'}\nالإجمالي: ${formatCurrency(invoice.grandTotal)}\nالتاريخ: ${formatDate(invoice.invoiceDate)}`,
        sections: [
          {
            heading: 'بيانات الفاتورة',
            rows: [
              { label: 'رقم الفاتورة', value: invoice.invoiceNumber },
              { label: 'العميل', value: invoice.customerName || '—' },
              { label: 'التاريخ', value: formatDate(invoice.invoiceDate) },
              { label: 'نوع الدفع', value: paymentTypeLabel(invoice.paymentType) },
              { label: 'الإجمالي', value: formatCurrency(invoice.grandTotal) },
              { label: 'المحصّل', value: formatCurrency(ops?.collectedAmount ?? 0) },
              { label: 'المتبقي', value: formatCurrency(ops?.remainingBalance ?? 0) }
            ]
          },
          {
            heading: 'الأصناف',
            rows: invoice.lines.map((line) => ({
              label: `${formatLineIndex(line.lineNumber)} ${line.fabricDisplayName}/${line.colorDisplayName}`,
              value: `${formatNumber(line.rollCount)} ثوب · ${formatSalesLineLength(line.totalLengthMeters, { totalLengthDisplay: line.totalLengthDisplay, unit: line.unit })} · ${formatCurrency(line.lineTotal)}`
            }))
          }
        ]
      }
    : null;

  return (
    <AppShell title={invoice ? invoice.invoiceNumber : 'تفاصيل الفاتورة'} summary={headerSummary}>
      <Toast toast={toast} onClose={() => setToast(null)} />

      {opsQuery.isLoading ? <LoadingState /> : null}
      {opsQuery.isError ? (
        <ErrorState message={getErrorMessage(opsQuery.error)} onRetry={() => void opsQuery.refetch()} />
      ) : null}

      {ops && invoice ? (
        <div className="page-stack">
          <section className="form-panel form-compact">
            <div className="compact-hero">
              <div>
                <p className="compact-hero__eyebrow">{invoice.customerName}</p>
                <h2>{invoice.invoiceNumber}</h2>
              </div>
              <StatusPill status={invoice.status} />
            </div>
          </section>

          <section className="form-panel form-compact">
            <h2>بيانات الفاتورة</h2>
            <dl className="detail-grid">
              <DetailItem label="التاريخ" value={formatDate(invoice.invoiceDate)} />
              <DetailItem label="نوع الدفع" value={paymentTypeLabel(invoice.paymentType)} />
              {invoice.paymentType === 1 && (invoice.partialPaymentAmount ?? 0) > 0 ? (
                <DetailItem label="دفعة مقدّمة" value={formatCurrency(invoice.partialPaymentAmount ?? 0)} />
              ) : null}
              <DetailItem label="المستودع" value={ops.warehouseName ?? 'غير محدد'} />
              <DetailItem label="هاتف العميل" value={ops.customerPhone ?? 'غير محدد'} />
              <DetailItem label="الإجمالي الفرعي" value={formatCurrency(invoice.subTotal)} />
              <DetailItem label="الخصم" value={formatCurrency(invoice.discountTotal)} />
              <DetailItem label="الإجمالي" value={formatCurrency(invoice.grandTotal)} />
            </dl>
          </section>

          <DocumentActions
            payload={exportPayload}
            pdfSource={{
              fileName: `sales-invoice-${invoice.invoiceNumber}.pdf`,
              load: () => getSalesInvoicePdf(invoice.id)
            }}
            onToast={(message, tone = 'success') => setToast({ tone, message })}
          />

          <section className="compact-action-row" aria-label="إجراءات الفاتورة">
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

          <section className="form-panel form-compact">
            <h2>أصناف الفاتورة</h2>
            {invoice.lines.length === 0 ? (
              <EmptyState title="لا توجد أصناف" description="لم تُضف أصناف لهذه الفاتورة." />
            ) : (
              <div className="line-items">
                {invoice.lines.map((line) => (
                  <article className="line-item" key={line.id}>
                    <div className="line-item__head">
                      <span className="line-item__index">{formatLineIndex(line.lineNumber)}</span>
                      <strong>{formatCurrency(line.lineTotal)}</strong>
                    </div>
                    <p className="line-item__meta">
                      {line.fabricDisplayName} / {line.colorDisplayName}
                    </p>
                    <div className="form-field-row form-field-row--2">
                      <span className="form-hint">{formatNumber(line.rollCount)} ثوب</span>
                      <span className="form-hint">{formatSalesLineLength(line.totalLengthMeters, { totalLengthDisplay: line.totalLengthDisplay, unit: line.unit })}</span>
                    </div>
                    <p className="form-hint">
                      سعر {line.lengthUnitDisplay ?? 'متر'}: {formatCurrency(line.unitPrice)}
                    </p>
                  </article>
                ))}
              </div>
            )}
          </section>

          {ops.journalEntries.length > 0 ? (
            <section className="form-panel form-compact">
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
            <section className="form-panel form-compact">
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
  containerId: string;
  stockKey: string;
  rollCount: string;
  unitPrice: string;
  taxCodeId: string;
};

function SalesCreatePage() {
  const navigate = useNavigate();
  const [toast, setToast] = useState<ToastState | null>(null);
  const [customerId, setCustomerId] = useState('');
  const [warehouseId, setWarehouseId] = useState('');
  const [containerId, setContainerId] = useState('');
  const [paymentType, setPaymentType] = useState<'0' | '1'>('0');
  const [cashboxId, setCashboxId] = useState('');
  const [discount, setDiscount] = useState('0');
  const [partialPayment, setPartialPayment] = useState('0');
  const [lines, setLines] = useState<DraftLine[]>([]);
  const [debouncedTaxRequest, setDebouncedTaxRequest] = useState<CalculateSalesInvoiceTaxRequest | null>(null);

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

  const taxCodesQuery = useQuery({
    queryKey: ['sales', 'tax-codes'],
    queryFn: () => getTaxCodes()
  });

  const cashboxesQuery = useQuery({
    queryKey: ['lookups', 'cashboxes'],
    queryFn: () => getCashboxLookups()
  });

  const taxCodes = taxCodesQuery.data ?? [];

  function defaultTaxCodeId(codes: TaxCodeDto[]) {
    if (codes.some((code) => code.id === DEFAULT_VAT_TAX_CODE_ID)) {
      return DEFAULT_VAT_TAX_CODE_ID;
    }
    return codes[0]?.id ?? '';
  }

  function taxCodeLabel(code: TaxCodeDto) {
    const rateLabel = `${formatNumber(code.rate * 100)}%`;
    return `${code.code} — ${code.name} (${rateLabel}${code.isInclusive ? '، شامل' : ''})`;
  }

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

  const selectedLineContainerIds = useMemo(
    () => Array.from(new Set(lines.map((line) => line.containerId).filter(Boolean))),
    [lines]
  );

  const stockQueries = useQueries({
    queries: selectedLineContainerIds.map((lineContainerId) => ({
      queryKey: ['sales-warehouse-stock', lineContainerId, warehouseId],
      queryFn: () => getSalesWarehouseStock(lineContainerId, warehouseId),
      enabled: lineContainerId.length > 0 && warehouseId.length > 0
    }))
  });

  const stockByContainer = useMemo(() => {
    const map = new Map<string, SalesWarehouseStockOptionDto[]>();
    selectedLineContainerIds.forEach((lineContainerId, index) => {
      map.set(lineContainerId, stockQueries[index]?.data ?? []);
    });
    return map;
  }, [selectedLineContainerIds, stockQueries]);
  const stockOptions: SalesWarehouseStockOptionDto[] = [];
  const stockQuery = {
    isLoading: false,
    isError: false,
    isSuccess: false,
    error: null as unknown,
    refetch: async () => undefined
  };

  function stockKeyOf(option: SalesWarehouseStockOptionDto) {
    return `${option.fabricItemId}::${option.fabricColorId}`;
  }

  function findStock(line: DraftLine) {
    return (stockByContainer.get(line.containerId) ?? []).find((option) => stockKeyOf(option) === line.stockKey);
  }

  function addLine() {
    setLines((current) => [
      ...current,
      {
        key: `${Date.now()}-${Math.random()}`,
        containerId: '',
        stockKey: '',
        rollCount: '1',
        unitPrice: '0',
        taxCodeId: defaultTaxCodeId(taxCodes)
      }
    ]);
  }

  function updateLine(key: string, patch: Partial<DraftLine>) {
    setLines((current) => current.map((line) => (line.key === key ? { ...line, ...patch } : line)));
  }

  function removeLine(key: string) {
    setLines((current) => current.filter((line) => line.key !== key));
  }

  function netLineAmount(line: DraftLine) {
    const stock = findStock(line);
    if (!stock || stock.availableRollCount === 0) {
      return 0;
    }
    const rolls = toNumber(line.rollCount);
    const avgPerRoll = stock.availableMeters / stock.availableRollCount;
    return rolls * avgPerRoll * toNumber(line.unitPrice);
  }

  const taxPreviewRequest = useMemo((): CalculateSalesInvoiceTaxRequest | null => {
    const previewLines = lines
      .map((line, index) => {
        const amount = netLineAmount(line);
        if (amount <= 0 || !line.stockKey) {
          return null;
        }
        return {
          lineNumber: index + 1,
          netLineAmount: amount,
          lineDiscountTotal: 0,
          taxCodeId: line.taxCodeId || null
        };
      })
      .filter((line): line is NonNullable<typeof line> => line !== null);

    if (previewLines.length === 0) {
      return null;
    }

    return {
      invoiceDate: new Date().toISOString(),
      invoiceDiscountTotal: toNumber(discount),
      lines: previewLines
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [lines, discount, stockByContainer]);

  useEffect(() => {
    const handle = window.setTimeout(() => setDebouncedTaxRequest(taxPreviewRequest), 300);
    return () => window.clearTimeout(handle);
  }, [taxPreviewRequest]);

  const taxPreviewQuery = useQuery({
    queryKey: ['sales-invoice-tax-preview', debouncedTaxRequest],
    queryFn: () => calculateSalesInvoiceTax(debouncedTaxRequest!),
    enabled: debouncedTaxRequest !== null && debouncedTaxRequest.lines.length > 0,
    placeholderData: keepPreviousData
  });

  const taxPreview = taxPreviewQuery.data;
  const previewGrandTotal = taxPreview?.grandTotal ?? 0;
  const previewValidationErrors = taxPreview?.validationErrors ?? [];

  const mutation = useMutation({
    mutationFn: () => {
      const payloadLines: CreateSalesInvoiceLineRequest[] = lines
        .map((line, index): CreateSalesInvoiceLineRequest | null => {
          const stock = findStock(line);
          if (!stock) {
            return null;
          }
          const unitPrice = toNumber(line.unitPrice);
          const unit = stock.dplQuantityUnit === 1 ? 'yard' : 'meter';
          return {
            lineNumber: index + 1,
            chinaContainerId: line.containerId,
            fabricItemId: stock.fabricItemId,
            fabricColorId: stock.fabricColorId,
            rollCount: Math.round(toNumber(line.rollCount)),
            unitPrice,
            originalUnitPrice: stock.salePricePerMeter ?? unitPrice,
            unit,
            discountReason: null,
            notes: null,
            taxCodeId: line.taxCodeId || null
          };
        })
        .filter((line): line is CreateSalesInvoiceLineRequest => line !== null);

      return createSalesInvoice({
        customerId,
        warehouseId,
        chinaContainerId: payloadLines[0]?.chinaContainerId ?? containerId,
        paymentType: Number(paymentType) as PaymentType,
        discountAmount: toNumber(discount),
        partialPaymentAmount:
          paymentType === '1' && toNumber(partialPayment) > 0 ? toNumber(partialPayment) : null,
        cashboxId: paymentType === '0' || toNumber(partialPayment) > 0 ? cashboxId : null,
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
    if (false && !containerId) {
      setToast({ tone: 'error', message: 'اختر الحاوية المصدر أولاً.' });
      return;
    }
    const validLines = lines.filter((line) => line.stockKey && toNumber(line.rollCount) > 0);
    if (validLines.length === 0) {
      setToast({ tone: 'error', message: 'أضف صنفًا واحدًا على الأقل مع عدد أثواب صحيح.' });
      return;
    }
    if (validLines.some((line) => !line.containerId)) {
      setToast({ tone: 'error', message: 'Choose a container for every invoice line.' });
      return;
    }
    if (previewValidationErrors.length > 0) {
      setToast({ tone: 'error', message: previewValidationErrors[0] ?? 'تعذّر التحقق من الضريبة.' });
      return;
    }
    if ((paymentType === '0' || toNumber(partialPayment) > 0) && !cashboxId) {
      setToast({ tone: 'error', message: 'اختر الصندوق أولاً.' });
      return;
    }
    if (paymentType === '1') {
      const partial = toNumber(partialPayment);
      if (partial < 0) {
        setToast({ tone: 'error', message: 'مبلغ الدفعة لا يمكن أن يكون سالباً.' });
        return;
      }
      if (previewGrandTotal > 0 && partial > previewGrandTotal) {
        setToast({ tone: 'error', message: 'مبلغ الدفعة لا يمكن أن يتجاوز إجمالي الفاتورة.' });
        return;
      }
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
              <select
                value={paymentType}
                onChange={(event) => {
                  const next = event.target.value as '0' | '1';
                  setPaymentType(next);
                  if (next === '0') {
                    setPartialPayment('0');
                  }
                }}
              >
                <option value="0">{paymentTypeLabel(0)}</option>
                <option value="1">{paymentTypeLabel(1)}</option>
              </select>
            </label>
            <label className="form-field">
              <span className="form-field__label">الخصم</span>
              <input inputMode="decimal" value={discount} onChange={(event) => setDiscount(event.target.value)} />
            </label>
          </div>

          {paymentType === '1' ? (
            <label className="form-field form-field--wide form-credit-payment">
              <span className="form-field__label">دفعة من الفاتورة (آجل)</span>
              <input
                inputMode="decimal"
                value={partialPayment}
                onChange={(event) => setPartialPayment(event.target.value)}
                placeholder="0"
              />
            </label>
          ) : null}
          {(paymentType === '0' || toNumber(partialPayment) > 0) ? (
            <label className="form-field form-field--wide">
              <span className="form-field__label">الصندوق</span>
              <select value={cashboxId} onChange={(event) => setCashboxId(event.target.value)} required>
                <option value="">اختر الصندوق...</option>
                {(cashboxesQuery.data ?? []).map((cashbox) => (
                  <option key={cashbox.id} value={cashbox.id}>{cashbox.name}</option>
                ))}
              </select>
            </label>
          ) : null}
        </section>

        <section className="form-panel form-compact" aria-label="أصناف الفاتورة">
          <div className="form-section-head">
            <h2>الأصناف</h2>
            <button
              className="chip-button"
              type="button"
              onClick={addLine}
              disabled={!warehouseId}
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
              const lineStockOptions = stockByContainer.get(line.containerId) ?? [];
              const lineStockQueryIndex = selectedLineContainerIds.indexOf(line.containerId);
              const lineStockQuery = lineStockQueryIndex >= 0 ? stockQueries[lineStockQueryIndex] : undefined;
              const stock = findStock(line);
              return (
                <article className="line-item" key={line.key}>
                  <div className="line-item__head">
                    <span className="line-item__index">{formatLineIndex(index + 1)}</span>
                    <button className="line-item__remove" type="button" onClick={() => removeLine(line.key)} aria-label="حذف الصنف">
                      حذف
                    </button>
                  </div>
                  <label className="form-field form-field--wide">
                    <span className="form-field__label">الحاوية</span>
                    <select
                      value={line.containerId}
                      onChange={(event) => {
                        if (
                          line.stockKey &&
                          !window.confirm('Changing the line container will clear the selected item and price. Continue?')
                        ) {
                          return;
                        }
                        updateLine(line.key, {
                          containerId: event.target.value,
                          stockKey: '',
                          unitPrice: '0'
                        });
                      }}
                    >
                      <option value="">اختر الحاوية...</option>
                      {(containersQuery.data?.items ?? []).map((container) => (
                        <option key={container.id} value={container.id}>
                          {container.containerNumber}
                        </option>
                      ))}
                    </select>
                  </label>
                  <label className="form-field form-field--wide">
                    <span className="form-field__label">الصنف</span>
                    <select
                      value={line.stockKey}
                      disabled={!line.containerId || !warehouseId || lineStockQuery?.isLoading}
                      onChange={(event) => {
                        const option = lineStockOptions.find((item) => stockKeyOf(item) === event.target.value);
                        updateLine(line.key, {
                          stockKey: event.target.value,
                          unitPrice: option?.salePricePerMeter != null ? String(option.salePricePerMeter) : line.unitPrice
                        });
                      }}
                    >
                      <option value="">اختر الصنف...</option>
                      {lineStockOptions.map((option) => (
                        <option key={stockKeyOf(option)} value={stockKeyOf(option)}>
                          {option.display}
                        </option>
                      ))}
                    </select>
                  </label>
                  {line.containerId && lineStockQuery?.isSuccess && lineStockOptions.length === 0 ? (
                    <p className="form-hint form-hint--warn">No available stock for this container.</p>
                  ) : null}
                  <div className="form-field-row form-field-row--2">
                    <label className="form-field">
                      <span className="form-field__label">الأثواب</span>
                      <input inputMode="numeric" value={line.rollCount} onChange={(event) => updateLine(line.key, { rollCount: event.target.value })} />
                    </label>
                    <label className="form-field">
                      <span className="form-field__label">{perUnitLabel(stock?.dplQuantityUnit ?? 0, '$')}</span>
                      <input inputMode="decimal" value={line.unitPrice} onChange={(event) => updateLine(line.key, { unitPrice: event.target.value })} />
                    </label>
                  </div>
                  <label className="form-field form-field--wide">
                    <span className="form-field__label">رمز الضريبة</span>
                    <select
                      value={line.taxCodeId}
                      disabled={taxCodesQuery.isLoading}
                      onChange={(event) => updateLine(line.key, { taxCodeId: event.target.value })}
                    >
                      <option value="">بدون ضريبة</option>
                      {taxCodes.map((code) => (
                        <option key={code.id} value={code.id}>
                          {taxCodeLabel(code)}
                        </option>
                      ))}
                    </select>
                  </label>
                  {stock ? (
                    <p className="line-item__meta">
                      متاح {formatNumber(stock.availableRollCount)} ثوب · {formatContainerLength(stock.availableMeters, stock.dplQuantityUnit ?? 0)}
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
            {taxPreviewQuery.isFetching && debouncedTaxRequest ? (
              <p className="form-hint">جار حساب الضريبة...</p>
            ) : null}
            {taxPreviewQuery.isError ? (
              <p className="form-hint form-hint--warn">{getErrorMessage(taxPreviewQuery.error)}</p>
            ) : null}
            {previewValidationErrors.length > 0 ? (
              <ul className="form-hint form-hint--warn" aria-label="أخطاء التحقق من الضريبة">
                {previewValidationErrors.map((error) => (
                  <li key={error}>{error}</li>
                ))}
              </ul>
            ) : null}
            {taxPreview ? (
              <div className="line-list">
                <div className="price-row">
                  <span>الإجمالي الفرعي</span>
                  <strong>{formatCurrency(taxPreview.subtotalBeforeDiscount)}</strong>
                </div>
                {taxPreview.lineDiscountTotal > 0 ? (
                  <div className="price-row">
                    <span>خصومات الأصناف</span>
                    <strong>{formatCurrency(taxPreview.lineDiscountTotal)}</strong>
                  </div>
                ) : null}
                {taxPreview.invoiceDiscountTotal > 0 ? (
                  <div className="price-row">
                    <span>خصم الفاتورة</span>
                    <strong>{formatCurrency(taxPreview.invoiceDiscountTotal)}</strong>
                  </div>
                ) : null}
                <div className="price-row">
                  <span>المبلغ الخاضع للضريبة</span>
                  <strong>{formatCurrency(taxPreview.taxableAmount)}</strong>
                </div>
                <div className="price-row">
                  <span>إجمالي الضريبة</span>
                  <strong>{formatCurrency(taxPreview.taxTotal)}</strong>
                </div>
                {taxPreview.roundingDifference !== 0 ? (
                  <div className="price-row">
                    <span>فرق التقريب</span>
                    <strong>{formatCurrency(taxPreview.roundingDifference)}</strong>
                  </div>
                ) : null}
                <div className="price-row">
                  <span>الإجمالي</span>
                  <strong>{formatCurrency(taxPreview.grandTotal)}</strong>
                </div>
              </div>
            ) : debouncedTaxRequest ? (
              <p className="form-hint">جار حساب الإجمالي...</p>
            ) : (
              <>
                <span>الإجمالي</span>
                <strong>{formatCurrency(0)}</strong>
              </>
            )}
          </div>
          {/*
            Web create saves a draft only. Invoice approval (inventory + tax GL posting)
            is not available on this screen — use the invoice detail page after warehouse detailing.
          */}
          <button
            className="primary-button sales-create__submit"
            type="submit"
            disabled={mutation.isPending || previewValidationErrors.length > 0 || taxPreviewQuery.isFetching}
          >
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
  return getApiErrorMessage(error);
}
