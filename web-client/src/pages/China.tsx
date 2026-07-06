import { useMemo, useState, type ChangeEvent, type FormEvent } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useNavigate, useParams } from 'react-router-dom';
import {
  approveContainer,
  archiveContainer,
  calculateLandingCost,
  createContainer,
  getContainerOperations,
  getContainers,
  moveContainerToWarehouse,
  parseChinaInvoice,
  parseChinaPackingSummary,
  parseContainerDpl,
  setContainerSalePrices
} from '../api/containers.ts';
import { getSupplierLookups, getWarehouseLookups } from '../api/lookups.ts';
import { ApiError } from '../api/client.ts';
import type {
  CalculateLandingCostRequest,
  ChinaContainerStatus,
  ChinaInvoiceParseResultDto,
  ChinaPackingSummaryParseResultDto,
  ContainerDetailsDto,
  ContainerExcelParseResultDto,
  ContainerFabricTypeLineCommand,
  ContainerInventoryMetricsDto,
  ContainerListDto,
  CreateChinaContainerRequest,
  ImportContainerLineCommand,
  LandingCostDto,
  PackingListGroupDto,
  SetContainerSalePricesRequest
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
  chinaContainerStatusLabels,
  chinaContainerStatusOptions,
  getChinaContainerStatusTone,
  landingCostStatusLabels
} from '../lib/enums.ts';

const PAGE_SIZE = 10;

type ToastState = {
  tone: 'success' | 'error';
  message: string;
};

type ImportFormState = {
  supplierId: string;
  containerNumber: string;
  shipmentDate: string;
  expectedArrival: string;
  notes: string;
  exchangeRateToLocalCurrency: string;
  chinaInvoiceAmountUsd: string;
};

type UploadStepState = {
  fileName: string | null;
  error: string | null;
};

type LandingCostFormState = {
  containerWeightKg: string;
  customsAmount: string;
  clearance: string;
  shipping: string;
  insurance: string;
  otherExpense1: string;
  otherExpense2: string;
  otherExpense3: string;
  otherExpense4: string;
  usesWeightedAllocation: boolean;
};

type SalePriceFormState = Record<string, string>;

export function ChinaPage() {
  const { containerId } = useParams();

  if (containerId) {
    return <ChinaContainerDetailsPage containerId={containerId} />;
  }

  return <ChinaContainerListPage />;
}

function ChinaContainerListPage() {
  const { can } = useAuth();
  const [status, setStatus] = useState<ChinaContainerStatus | undefined>();
  const [page, setPage] = useState(1);
  const [showImportWizard, setShowImportWizard] = useState(false);
  const [toast, setToast] = useState<ToastState | null>(null);

  const containersQuery = useQuery({
    queryKey: ['china-containers', status, page],
    queryFn: () => getContainers({ status, page, pageSize: PAGE_SIZE })
  });

  const pageSummary = useMemo(() => {
    const rows = containersQuery.data?.items ?? [];
    // Display-only page summary. Business totals must stay sourced from API endpoints.
    return {
      count: rows.length,
      rolls: rows.reduce((sum, row) => sum + row.totalRolls, 0),
      meters: rows.reduce((sum, row) => sum + row.totalMeters, 0)
    };
  }, [containersQuery.data?.items]);

  const headerSummary = (
    <>
      <SummaryCard label="عدد الحاويات" value={formatNumber(pageSummary.count)} />
      <SummaryCard label="إجمالي الأثواب" value={formatNumber(pageSummary.rolls)} tone="green" />
      <SummaryCard label="إجمالي الأمتار" value={formatMeters(pageSummary.meters)} tone="amber" />
    </>
  );

  function selectStatus(nextStatus: ChinaContainerStatus | undefined) {
    setStatus(nextStatus);
    setPage(1);
  }

  return (
    <AppShell title="طلبات الصين" summary={headerSummary}>
      <Toast toast={toast} onClose={() => setToast(null)} />

      <section className="toolbar-row">
        {can('containers.create') ? (
          <button className="primary-button" type="button" onClick={() => setShowImportWizard((current) => !current)}>
            استيراد حاوية جديدة
          </button>
        ) : null}
      </section>

      {showImportWizard ? <ImportWizard onDone={() => setShowImportWizard(false)} onToast={setToast} /> : null}

      <section className="filter-strip" aria-label="تصفية حالة الحاوية">
        <button
          className={`filter-chip ${status === undefined ? 'filter-chip--active' : ''}`}
          type="button"
          onClick={() => selectStatus(undefined)}
        >
          الكل
        </button>
        {chinaContainerStatusOptions.map((option) => (
          <button
            className={`filter-chip ${status === option.value ? 'filter-chip--active' : ''}`}
            key={option.value}
            type="button"
            onClick={() => selectStatus(option.value)}
          >
            {option.label}
          </button>
        ))}
      </section>

      {containersQuery.isLoading ? <LoadingState /> : null}

      {containersQuery.isError ? (
        <ErrorState
          message={getErrorMessage(containersQuery.error)}
          onRetry={() => void containersQuery.refetch()}
        />
      ) : null}

      {containersQuery.isSuccess && containersQuery.data.items.length === 0 ? (
        <EmptyState title="لا توجد حاويات" description="ستظهر طلبات الصين هنا بعد إنشاء الحاويات في النظام." />
      ) : null}

      {containersQuery.isSuccess && containersQuery.data.items.length > 0 ? (
        <>
          <section className="card-list" aria-label="قائمة حاويات الصين">
            {containersQuery.data.items.map((container) => (
              <Link className="card-link" key={container.id} to={`/china/${container.id}`}>
                <ContainerListCard container={container} />
              </Link>
            ))}
          </section>
          <Pagination
            page={containersQuery.data.page}
            totalPages={containersQuery.data.totalPages}
            totalCount={containersQuery.data.totalCount}
            onPrevious={() => setPage((current) => Math.max(1, current - 1))}
            onNext={() => setPage((current) => current + 1)}
          />
        </>
      ) : null}
    </AppShell>
  );
}

function ImportWizard({ onDone, onToast }: { onDone: () => void; onToast: (toast: ToastState) => void }) {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [dplResult, setDplResult] = useState<ContainerExcelParseResultDto | null>(null);
  const [invoiceResult, setInvoiceResult] = useState<ChinaInvoiceParseResultDto | null>(null);
  const [packingResult, setPackingResult] = useState<ChinaPackingSummaryParseResultDto | null>(null);
  const [dplStep, setDplStep] = useState<UploadStepState>({ fileName: null, error: null });
  const [invoiceStep, setInvoiceStep] = useState<UploadStepState>({ fileName: null, error: null });
  const [packingStep, setPackingStep] = useState<UploadStepState>({ fileName: null, error: null });
  const [form, setForm] = useState<ImportFormState>({
    supplierId: '',
    containerNumber: '',
    shipmentDate: toDateInputValue(new Date()),
    expectedArrival: '',
    notes: '',
    exchangeRateToLocalCurrency: '1',
    chinaInvoiceAmountUsd: '0'
  });

  const dplMutation = useMutation({
    mutationFn: parseContainerDpl,
    onSuccess: (result, file) => {
      setDplResult(result);
      setDplStep({ fileName: result.fileName || file.name, error: null });
    },
    onError: (error) => {
      const message = getErrorMessage(error);
      setDplStep({ fileName: null, error: message });
      onToast({ tone: 'error', message });
    }
  });
  const invoiceMutation = useMutation({
    mutationFn: parseChinaInvoice,
    onSuccess: (result, file) => {
      setInvoiceResult(result);
      setInvoiceStep({ fileName: result.fileName || file.name, error: null });
      updateForm('chinaInvoiceAmountUsd', String(result.grandTotalUsd));
    },
    onError: (error) => {
      const message = getErrorMessage(error);
      setInvoiceStep({ fileName: null, error: message });
      onToast({ tone: 'error', message });
    }
  });
  const packingMutation = useMutation({
    mutationFn: parseChinaPackingSummary,
    onSuccess: (result, file) => {
      setPackingResult(result);
      setPackingStep({ fileName: result.fileName || file.name, error: null });
    },
    onError: (error) => {
      const message = getErrorMessage(error);
      setPackingStep({ fileName: null, error: message });
      onToast({ tone: 'error', message });
    }
  });

  const createMutation = useMutation({
    mutationFn: createContainer,
    onSuccess: async (containerId) => {
      await queryClient.invalidateQueries({ queryKey: ['china-containers'] });
      onToast({ tone: 'success', message: 'تم إنشاء الحاوية بنجاح.' });
      onDone();
      navigate(`/china/${containerId}`);
    },
    onError: (error) => onToast(errorToast(error))
  });

  const suppliersQuery = useQuery({
    queryKey: ['lookups', 'suppliers'],
    queryFn: getSupplierLookups
  });

  const importLines = useMemo(() => (dplResult ? buildImportLines(dplResult.groups) : []), [dplResult]);

  const createBlockReason = useMemo(() => {
    if (!dplResult) {
      return 'ارفع ملف DPL / Packing أولاً.';
    }
    if (dplResult.hasUnresolvedGroups) {
      return 'يجب حل كل مجموعات القماش غير المربوطة في ملف DPL.';
    }
    if (importLines.length === 0) {
      return 'لا توجد أسطر صالحة للاستيراد — تأكد من ربط الأقمشة والألوان في الملف.';
    }
    if (suppliersQuery.isLoading) {
      return 'جاري تحميل قائمة الموردين...';
    }
    if (suppliersQuery.isError) {
      return 'تعذر تحميل قائمة الموردين.';
    }
    if ((suppliersQuery.data?.length ?? 0) === 0) {
      return 'لا يوجد موردون نشطون في النظام.';
    }
    if (!form.supplierId.trim()) {
      return 'اختر المورد من القائمة.';
    }
    return null;
  }, [dplResult, importLines.length, form.supplierId, suppliersQuery.data, suppliersQuery.isError, suppliersQuery.isLoading]);

  const canCreate = createBlockReason === null;

  function updateForm(field: keyof ImportFormState, value: string) {
    setForm((current) => ({ ...current, [field]: value }));
  }

  function handleFileChange(parser: (file: File) => void) {
    return (event: ChangeEvent<HTMLInputElement>) => {
      const file = event.target.files?.[0];
      if (file) {
        parser(file);
      }
      event.target.value = '';
    };
  }

  function submitCreate(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!dplResult || !canCreate) {
      onToast({ tone: 'error', message: createBlockReason ?? 'أكمل البيانات المطلوبة قبل إنشاء الحاوية.' });
      return;
    }

    const request: CreateChinaContainerRequest = {
      supplierId: form.supplierId.trim(),
      containerNumber: form.containerNumber.trim(),
      shipmentDate: toIsoDate(form.shipmentDate),
      expectedArrival: form.expectedArrival ? toIsoDate(form.expectedArrival) : null,
      notes: nullableText(form.notes),
      exchangeRateToLocalCurrency: toNumber(form.exchangeRateToLocalCurrency),
      chinaInvoiceAmountUsd: invoiceResult ? invoiceResult.grandTotalUsd : toNumber(form.chinaInvoiceAmountUsd),
      importFileName: dplResult.fileName,
      lines: importLines
    };

    createMutation.mutate(request);
  }

  return (
    <section className="detail-card import-wizard">
      <div className="section-title-row">
        <h2>استيراد حاوية جديدة</h2>
        <button className="ghost-button" type="button" onClick={onDone}>إغلاق</button>
      </div>

      <div className="wizard-grid">
        <FileStep
          title="1. ملف DPL / Packing"
          description="هذا الملف مطلوب لإنشاء الحاوية."
          pending={dplMutation.isPending}
          fileName={dplStep.fileName}
          error={dplStep.error}
          onChange={handleFileChange((file) => dplMutation.mutate(file))}
        />
        <FileStep
          title="2. فاتورة الصين"
          description="اختياري، للعرض والمراجعة."
          pending={invoiceMutation.isPending}
          fileName={invoiceStep.fileName}
          error={invoiceStep.error}
          onChange={handleFileChange((file) => invoiceMutation.mutate(file))}
        />
        <FileStep
          title="3. ملخص Packing"
          description="اختياري، للوزن وCBM."
          pending={packingMutation.isPending}
          fileName={packingStep.fileName}
          error={packingStep.error}
          onChange={handleFileChange((file) => packingMutation.mutate(file))}
        />
      </div>

      {dplResult ? <DplParseResult result={dplResult} /> : null}
      {invoiceResult ? <InvoiceParseResult result={invoiceResult} /> : null}
      {packingResult ? <PackingParseResult result={packingResult} /> : null}

      <form className="form-grid" onSubmit={submitCreate}>
        <label>
          المورد
          <select
            value={form.supplierId}
            onChange={(event) => updateForm('supplierId', event.target.value)}
            disabled={suppliersQuery.isLoading || suppliersQuery.isError}
            required
          >
            <option value="">اختر المورد...</option>
            {(suppliersQuery.data ?? []).map((supplier) => (
              <option key={supplier.id} value={supplier.id}>
                {supplier.name}
              </option>
            ))}
          </select>
          {suppliersQuery.isError ? (
            <span className="field-note">{getErrorMessage(suppliersQuery.error)}</span>
          ) : null}
        </label>
        <label>
          رقم الحاوية
          <input value={form.containerNumber} onChange={(event) => updateForm('containerNumber', event.target.value)} placeholder="يولد تلقائياً إذا ترك فارغاً" />
        </label>
        <label>
          تاريخ الشحن
          <input type="date" value={form.shipmentDate} onChange={(event) => updateForm('shipmentDate', event.target.value)} />
        </label>
        <label>
          الوصول المتوقع
          <input type="date" value={form.expectedArrival} onChange={(event) => updateForm('expectedArrival', event.target.value)} />
        </label>
        <label>
          سعر الصرف
          <input inputMode="decimal" value={form.exchangeRateToLocalCurrency} onChange={(event) => updateForm('exchangeRateToLocalCurrency', event.target.value)} />
        </label>
        {invoiceResult ? (
          <label>
            قيمة فاتورة الصين (من الملف)
            <input readOnly value={formatCurrency(invoiceResult.grandTotalUsd)} />
          </label>
        ) : (
          <label>
            قيمة فاتورة الصين
            <input inputMode="decimal" value={form.chinaInvoiceAmountUsd} onChange={(event) => updateForm('chinaInvoiceAmountUsd', event.target.value)} />
            <span className="field-note">لم يُرفع ملف الفاتورة — أدخل القيمة يدوياً</span>
          </label>
        )}
        <label className="form-grid__wide">
          ملاحظات
          <input value={form.notes} onChange={(event) => updateForm('notes', event.target.value)} />
        </label>
        <button
          className="primary-button primary-button--wide form-grid__wide"
          type="submit"
          disabled={!canCreate || createMutation.isPending}
          title={createBlockReason ?? undefined}
        >
          {createMutation.isPending ? 'جار إنشاء الحاوية...' : 'إنشاء الحاوية'}
        </button>
        {createBlockReason && !createMutation.isPending ? (
          <p className="field-note form-grid__wide">{createBlockReason}</p>
        ) : null}
      </form>
    </section>
  );
}

function ChinaContainerDetailsPage({ containerId }: { containerId: string }) {
  const { can } = useAuth();
  const [activePanel, setActivePanel] = useState<'landing' | 'prices' | 'move' | null>(null);
  const [toast, setToast] = useState<ToastState | null>(null);
  const queryClient = useQueryClient();

  const operationsQuery = useQuery({
    queryKey: ['china-container-operations', containerId],
    queryFn: () => getContainerOperations(containerId)
  });

  const data = operationsQuery.data;
  const container = data?.container;

  const headerSummary = container ? (
    <>
      <SummaryCard label="الأثواب" value={formatNumber(container.totalRolls)} />
      <SummaryCard label="الأمتار" value={formatMeters(container.totalMeters)} tone="green" />
      <SummaryCard label="فاتورة الصين" value={formatCurrency(container.chinaInvoiceAmountUsd)} tone="amber" />
    </>
  ) : undefined;

  async function refreshAfterAction(message: string) {
    await queryClient.invalidateQueries({ queryKey: ['china-container-operations', containerId] });
    await queryClient.invalidateQueries({ queryKey: ['china-containers'] });
    setToast({ tone: 'success', message });
    setActivePanel(null);
  }

  const approveMutation = useMutation({
    mutationFn: () => approveContainer(containerId),
    onSuccess: () => void refreshAfterAction('تم اعتماد الحاوية.'),
    onError: (error) => setToast(errorToast(error))
  });
  const archiveMutation = useMutation({
    mutationFn: () => archiveContainer(containerId),
    onSuccess: () => void refreshAfterAction('تمت أرشفة الحاوية.'),
    onError: (error) => setToast(errorToast(error))
  });

  return (
    <AppShell title={container ? container.containerNumber : 'تفاصيل حاوية الصين'} summary={headerSummary}>
      <Toast toast={toast} onClose={() => setToast(null)} />

      {operationsQuery.isLoading ? <LoadingState /> : null}

      {operationsQuery.isError ? (
        <ErrorState message={getErrorMessage(operationsQuery.error)} onRetry={() => void operationsQuery.refetch()} />
      ) : null}

      {data && container ? (
        <div className="details-stack">
          <section className="detail-card detail-card--hero">
            <div>
              <StatusPill status={container.status} />
              <h2>{container.containerNumber}</h2>
              <p>{container.supplierName || 'مورد غير محدد'}</p>
            </div>
            {data.isReadyForSale ? <span className="ready-badge">جاهزة للبيع</span> : null}
          </section>

          <ActionPanel
            canCalculateLandingCost={data.canCalculateLandingCost && can('containers.landing-cost')}
            canSetSalePrices={data.canSetSalePrices && can('containers.landing-cost')}
            canApprove={data.canApprove && can('containers.approve')}
            canMoveToWarehouse={data.canMoveToWarehouse && can('containers.move-to-warehouse')}
            canArchive={can('containers.approve') && container.status !== 8}
            onLanding={() => setActivePanel((current) => (current === 'landing' ? null : 'landing'))}
            onPrices={() => setActivePanel((current) => (current === 'prices' ? null : 'prices'))}
            onMove={() => setActivePanel((current) => (current === 'move' ? null : 'move'))}
            onApprove={() => {
              if (window.confirm('هل تريد اعتماد هذه الحاوية؟')) approveMutation.mutate();
            }}
            onArchive={() => {
              if (window.confirm('هل تريد أرشفة هذه الحاوية؟')) archiveMutation.mutate();
            }}
            pending={approveMutation.isPending || archiveMutation.isPending}
          />

          {activePanel === 'landing' ? <LandingCostForm container={container} onToast={setToast} onDone={(message) => void refreshAfterAction(message)} /> : null}
          {activePanel === 'prices' ? <SalePricesForm container={container} onToast={setToast} onDone={(message) => void refreshAfterAction(message)} /> : null}
          {activePanel === 'move' ? <MoveToWarehouseForm containerId={containerId} onToast={setToast} onDone={(message) => void refreshAfterAction(message)} /> : null}

          <ContainerInfoSection container={container} />
          <LandingCostSection landingCost={container.landingCost} />
          <FabricTypeLinesSection container={container} />
          <InventorySection inventory={data.inventory} />
        </div>
      ) : null}
    </AppShell>
  );
}

function LandingCostForm({ container, onDone, onToast }: { container: ContainerDetailsDto; onDone: (message: string) => void; onToast: (toast: ToastState) => void }) {
  const [form, setForm] = useState<LandingCostFormState>({
    containerWeightKg: String(container.totalWeightKg ?? container.landingCost?.containerWeightKg ?? 0),
    customsAmount: String(container.landingCost?.customsAmount ?? 0),
    clearance: String(container.landingCost?.clearance ?? 0),
    shipping: String(container.landingCost?.shipping ?? 0),
    insurance: String(container.landingCost?.insurance ?? 0),
    otherExpense1: String(container.landingCost?.otherExpense1 ?? 0),
    otherExpense2: String(container.landingCost?.otherExpense2 ?? 0),
    otherExpense3: String(container.landingCost?.otherExpense3 ?? 0),
    otherExpense4: String(container.landingCost?.otherExpense4 ?? 0),
    usesWeightedAllocation: container.landingCost?.usesWeightedAllocation ?? false
  });

  const mutation = useMutation({
    mutationFn: (request: CalculateLandingCostRequest) => calculateLandingCost(container.id, request),
    onSuccess: () => onDone('تم احتساب تكلفة الوصول.'),
    onError: (error) => onToast(errorToast(error))
  });

  function update(field: keyof LandingCostFormState, value: string | boolean) {
    setForm((current) => ({ ...current, [field]: value }));
  }

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const customsAmount = toNumber(form.customsAmount);
    const clearance = toNumber(form.clearance);
    const request: CalculateLandingCostRequest = {
      totalLengthMeters: container.totalMeters,
      containerWeightKg: toNumber(form.containerWeightKg),
      customsClearanceAmount: customsAmount + clearance,
      shipping: toNumber(form.shipping),
      insurance: toNumber(form.insurance),
      otherExpense1: toNumber(form.otherExpense1),
      otherExpense2: toNumber(form.otherExpense2),
      otherExpense3: toNumber(form.otherExpense3),
      otherExpense4: toNumber(form.otherExpense4),
      usesWeightedAllocation: form.usesWeightedAllocation,
      typeLines: container.fabricTypeLines.map(mapDetailsLineToCommand),
      customsAmount,
      clearance,
      otherExpenses: toNumber(form.otherExpense1) + toNumber(form.otherExpense2) + toNumber(form.otherExpense3) + toNumber(form.otherExpense4)
    };
    mutation.mutate(request);
  }

  return (
    <section className="detail-card">
      <h2>احتساب تكلفة الوصول</h2>
      <form className="form-grid" onSubmit={submit}>
        <NumberInput label="وزن الحاوية كغ" value={form.containerWeightKg} onChange={(value) => update('containerWeightKg', value)} />
        <NumberInput label="الجمارك" value={form.customsAmount} onChange={(value) => update('customsAmount', value)} />
        <NumberInput label="التخليص" value={form.clearance} onChange={(value) => update('clearance', value)} />
        <NumberInput label="الشحن" value={form.shipping} onChange={(value) => update('shipping', value)} />
        <NumberInput label="التأمين" value={form.insurance} onChange={(value) => update('insurance', value)} />
        <NumberInput label="مصروف إضافي 1" value={form.otherExpense1} onChange={(value) => update('otherExpense1', value)} />
        <NumberInput label="مصروف إضافي 2" value={form.otherExpense2} onChange={(value) => update('otherExpense2', value)} />
        <NumberInput label="مصروف إضافي 3" value={form.otherExpense3} onChange={(value) => update('otherExpense3', value)} />
        <NumberInput label="مصروف إضافي 4" value={form.otherExpense4} onChange={(value) => update('otherExpense4', value)} />
        <label className="toggle-row form-grid__wide">
          <input type="checkbox" checked={form.usesWeightedAllocation} onChange={(event) => update('usesWeightedAllocation', event.target.checked)} />
          توزيع المصاريف حسب الوزن عند توفر بيانات الأنواع
        </label>
        <button className="primary-button primary-button--wide form-grid__wide" type="submit" disabled={mutation.isPending}>
          {mutation.isPending ? 'جار الاحتساب...' : 'حفظ تكلفة الوصول'}
        </button>
      </form>
    </section>
  );
}

function SalePricesForm({ container, onDone, onToast }: { container: ContainerDetailsDto; onDone: (message: string) => void; onToast: (toast: ToastState) => void }) {
  const [prices, setPrices] = useState<SalePriceFormState>(() =>
    Object.fromEntries(container.fabricTypeLines.map((line) => [line.id, String(line.marginPerMeterUsd ?? 0)]))
  );

  const mutation = useMutation({
    mutationFn: (request: SetContainerSalePricesRequest) => setContainerSalePrices(container.id, request),
    onSuccess: () => onDone('تم حفظ أسعار البيع.'),
    onError: (error) => onToast(errorToast(error))
  });

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    mutation.mutate({
      lines: container.fabricTypeLines.map((line) => ({
        typeLineId: line.id,
        marginPerMeterUsd: toNumber(prices[line.id] ?? '0')
      }))
    });
  }

  return (
    <section className="detail-card">
      <h2>تحديد أسعار البيع</h2>
      <form className="line-list" onSubmit={submit}>
        {container.fabricTypeLines.map((line) => (
          <label className="price-row" key={line.id}>
            <span>{line.typeDisplayName}</span>
            <input
              inputMode="decimal"
              value={prices[line.id] ?? ''}
              onChange={(event) => setPrices((current) => ({ ...current, [line.id]: event.target.value }))}
              aria-label={`هامش البيع ${line.typeDisplayName}`}
            />
          </label>
        ))}
        <button className="primary-button primary-button--wide" type="submit" disabled={mutation.isPending || container.fabricTypeLines.length === 0}>
          {mutation.isPending ? 'جار الحفظ...' : 'حفظ هوامش البيع'}
        </button>
      </form>
    </section>
  );
}

function MoveToWarehouseForm({ containerId, onDone, onToast }: { containerId: string; onDone: (message: string) => void; onToast: (toast: ToastState) => void }) {
  const [warehouseId, setWarehouseId] = useState('');
  const warehousesQuery = useQuery({
    queryKey: ['lookups', 'warehouses'],
    queryFn: getWarehouseLookups
  });
  const mutation = useMutation({
    mutationFn: () => moveContainerToWarehouse(containerId, { warehouseId: warehouseId.trim() }),
    onSuccess: () => onDone('تم نقل الحاوية إلى المستودع.'),
    onError: (error) => onToast(errorToast(error))
  });

  const moveBlockReason = useMemo(() => {
    if (warehousesQuery.isLoading) {
      return 'جاري تحميل قائمة المستودعات...';
    }
    if (warehousesQuery.isError) {
      return 'تعذر تحميل قائمة المستودعات.';
    }
    if ((warehousesQuery.data?.length ?? 0) === 0) {
      return 'لا توجد مستودعات نشطة في النظام.';
    }
    if (!warehouseId.trim()) {
      return 'اختر المستودع من القائمة.';
    }
    return null;
  }, [warehouseId, warehousesQuery.data, warehousesQuery.isError, warehousesQuery.isLoading]);

  const canMove = moveBlockReason === null;

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!canMove) {
      onToast({ tone: 'error', message: moveBlockReason ?? 'اختر المستودع قبل النقل.' });
      return;
    }
    if (window.confirm('هل تريد نقل الحاوية إلى المستودع المحدد؟')) {
      mutation.mutate();
    }
  }

  return (
    <section className="detail-card">
      <h2>نقل إلى المستودع</h2>
      <form className="form-grid" onSubmit={submit}>
        <label className="form-grid__wide">
          المستودع
          <select
            value={warehouseId}
            onChange={(event) => setWarehouseId(event.target.value)}
            disabled={warehousesQuery.isLoading || warehousesQuery.isError}
            required
          >
            <option value="">اختر المستودع...</option>
            {(warehousesQuery.data ?? []).map((warehouse) => (
              <option key={warehouse.id} value={warehouse.id}>
                {warehouse.name}
              </option>
            ))}
          </select>
          {warehousesQuery.isError ? (
            <span className="field-note">{getErrorMessage(warehousesQuery.error)}</span>
          ) : null}
        </label>
        <button
          className="primary-button primary-button--wide form-grid__wide"
          type="submit"
          disabled={!canMove || mutation.isPending}
          title={moveBlockReason ?? undefined}
        >
          {mutation.isPending ? 'جار النقل...' : 'نقل إلى المستودع'}
        </button>
        {moveBlockReason && !mutation.isPending ? (
          <p className="field-note form-grid__wide">{moveBlockReason}</p>
        ) : null}
      </form>
    </section>
  );
}

function ContainerListCard({ container }: { container: ContainerListDto }) {
  const meta = `${formatDate(container.shipmentDate)} • ${formatNumber(container.totalRolls)} ثوب • ${formatMeters(container.totalMeters)}`;

  return (
    <DataCard
      icon={<Icon name="china" />}
      title={container.containerNumber}
      subtitle={container.supplierName || 'مورد غير محدد'}
      meta={meta}
      value={<StatusPill status={container.status} />}
      tone={container.status === 6 ? 'available' : 'neutral'}
    />
  );
}

function StatusPill({ status }: { status: ChinaContainerStatus }) {
  return <span className={`status-pill status-pill--${getChinaContainerStatusTone(status)}`}>{chinaContainerStatusLabels[status]}</span>;
}

function ActionPanel({
  canCalculateLandingCost,
  canSetSalePrices,
  canApprove,
  canMoveToWarehouse,
  canArchive,
  onLanding,
  onPrices,
  onApprove,
  onMove,
  onArchive,
  pending
}: {
  canCalculateLandingCost: boolean;
  canSetSalePrices: boolean;
  canApprove: boolean;
  canMoveToWarehouse: boolean;
  canArchive: boolean;
  onLanding: () => void;
  onPrices: () => void;
  onApprove: () => void;
  onMove: () => void;
  onArchive: () => void;
  pending: boolean;
}) {
  const actions = [
    { label: 'احتساب تكلفة الوصول', visible: canCalculateLandingCost, onClick: onLanding },
    { label: 'تحديد أسعار البيع', visible: canSetSalePrices, onClick: onPrices },
    { label: 'اعتماد الحاوية', visible: canApprove, onClick: onApprove },
    { label: 'نقل إلى المستودع', visible: canMoveToWarehouse, onClick: onMove },
    { label: 'أرشفة', visible: canArchive, onClick: onArchive }
  ];
  const visibleActions = actions.filter((action) => action.visible);

  if (visibleActions.length === 0) {
    return null;
  }

  return (
    <section className="action-grid" aria-label="إجراءات الحاوية">
      {visibleActions.map((action) => (
        <button className="primary-button primary-button--wide" type="button" key={action.label} onClick={action.onClick} disabled={pending}>
          {pending ? 'جار التنفيذ...' : action.label}
        </button>
      ))}
    </section>
  );
}

function ContainerInfoSection({ container }: { container: ContainerDetailsDto }) {
  return (
    <section className="detail-card">
      <h2>بيانات الحاوية</h2>
      <dl className="detail-grid">
        <DetailItem label="المورد" value={container.supplierName || 'غير محدد'} />
        <DetailItem label="تاريخ الشحن" value={formatDate(container.shipmentDate)} />
        <DetailItem label="تاريخ الوصول" value={formatDate(container.arrivalDate)} />
        <DetailItem label="سعر الصرف" value={formatNumber(container.exchangeRateToLocalCurrency)} />
        <DetailItem label="فاتورة الصين" value={formatCurrency(container.chinaInvoiceAmountUsd)} />
        <DetailItem label="احتياطي الضريبة" value={formatCurrency(container.financialTaxReserveUsd)} />
        <DetailItem label="الضريبة المرحلة" value={container.financialTaxReservePostedLocal === null ? 'غير مرحلة' : formatNumber(container.financialTaxReservePostedLocal)} />
        <DetailItem label="الوزن" value={container.totalWeightKg === null ? 'غير محدد' : `${formatNumber(container.totalWeightKg)} كغ`} />
      </dl>
    </section>
  );
}

function LandingCostSection({ landingCost }: { landingCost: LandingCostDto | null }) {
  if (!landingCost) {
    return (
      <section className="detail-card">
        <h2>تكلفة الوصول</h2>
        <p className="muted-line">لم تُحتسب بعد</p>
      </section>
    );
  }

  return (
    <section className="detail-card">
      <div className="section-title-row">
        <h2>تكلفة الوصول</h2>
        <span className="status-pill status-pill--blue">{landingCostStatusLabels[landingCost.status]}</span>
      </div>
      <dl className="detail-grid">
        <DetailItem label="إجمالي الأمتار" value={formatMeters(landingCost.totalLengthMeters)} />
        <DetailItem label="وزن الحاوية" value={`${formatNumber(landingCost.containerWeightKg)} كغ`} />
        <DetailItem label="الجمارك" value={formatCurrency(landingCost.customsAmount)} />
        <DetailItem label="الشحن" value={formatCurrency(landingCost.shipping)} />
        <DetailItem label="التأمين" value={formatCurrency(landingCost.insurance)} />
        <DetailItem label="التخليص" value={formatCurrency(landingCost.clearance)} />
        <DetailItem label="مصاريف أخرى" value={formatCurrency(landingCost.otherExpenses)} />
        <DetailItem label="إجمالي المصاريف" value={formatCurrency(landingCost.totalImportExpenses)} />
        <DetailItem label="جمرك / متر" value={formatCurrency(landingCost.customsCostPerMeter)} />
        <DetailItem label="مصروف / متر" value={formatCurrency(landingCost.expenseCostPerMeter)} />
        <DetailItem label="متوسط غرام / متر" value={formatNumber(landingCost.avgGramPerMeter)} />
        <DetailItem label="توزيع وزني" value={landingCost.usesWeightedAllocation ? 'نعم' : 'لا'} />
      </dl>
    </section>
  );
}

function FabricTypeLinesSection({ container }: { container: ContainerDetailsDto }) {
  return (
    <section className="detail-card">
      <h2>أنواع الأقمشة</h2>
      {container.fabricTypeLines.length === 0 ? (
        <p className="muted-line">لا توجد بنود أقمشة.</p>
      ) : (
        <div className="line-list">
          {container.fabricTypeLines.map((line) => (
            <article className="line-card" key={line.id}>
              <div className="line-card__head">
                <h3>{line.typeDisplayName}</h3>
                <span className={`status-pill ${line.hasSalePrice ? 'status-pill--green' : 'status-pill--gray'}`}>
                  {line.hasSalePrice ? 'لها سعر بيع' : 'بدون سعر بيع'}
                </span>
              </div>
              <dl className="mini-grid">
                <DetailItem label="الأمتار" value={formatMeters(line.lengthMeters)} />
                <DetailItem label="الأثواب" value={formatNumber(line.rollCount)} />
                <DetailItem label="سعر الصين" value={formatCurrency(line.chinaUnitPriceUsd)} />
                <DetailItem label="تكلفة / متر" value={formatCurrency(line.landedCostPerMeterUsd)} />
                <DetailItem label="هامش / متر" value={formatCurrency(line.marginPerMeterUsd)} />
                <DetailItem label="سعر البيع / متر" value={formatCurrency(line.salePricePerMeterUsd)} />
              </dl>
            </article>
          ))}
        </div>
      )}
    </section>
  );
}

function InventorySection({ inventory }: { inventory: ContainerInventoryMetricsDto | null }) {
  return (
    <section className="detail-card">
      <h2>مؤشرات المخزون</h2>
      {!inventory ? (
        <p className="muted-line">لم تُرحل للمخزون بعد.</p>
      ) : (
        <dl className="detail-grid">
          <DetailItem label="إجمالي الأثواب" value={formatNumber(inventory.totalRolls)} />
          <DetailItem label="إجمالي الأمتار" value={formatMeters(inventory.totalMeters)} />
          <DetailItem label="محجوز" value={formatMeters(inventory.reservedMeters)} />
          <DetailItem label="مباع" value={formatMeters(inventory.soldMeters)} />
          <DetailItem label="متاح" value={formatMeters(inventory.availableMeters)} />
          <DetailItem label="تكلفة / متر" value={formatCurrency(inventory.costPerMeter)} />
          <DetailItem label="قيمة المخزون" value={formatCurrency(inventory.inventoryValuation)} />
          <DetailItem label="مرحل للمخزون" value={inventory.isStockPosted ? 'نعم' : 'لا'} />
        </dl>
      )}
    </section>
  );
}

function DplParseResult({ result }: { result: ContainerExcelParseResultDto }) {
  return (
    <section className="parse-result">
      <h3>نتيجة DPL: {result.fileName}</h3>
      <dl className="mini-grid">
        <DetailItem label="المورد من الملف" value={result.supplierNameFromFile ?? 'غير محدد'} />
        <DetailItem label="الأمتار المعلنة" value={result.grandTotal.declaredTotalMeters === null ? 'غير محدد' : formatMeters(result.grandTotal.declaredTotalMeters)} />
        <DetailItem label="الأمتار المقروءة" value={formatMeters(result.grandTotal.parsedTotalMeters)} />
        <DetailItem label="الأثواب المعلنة" value={result.grandTotal.declaredTotalRolls === null ? 'غير محدد' : formatNumber(result.grandTotal.declaredTotalRolls)} />
        <DetailItem label="الأثواب المقروءة" value={formatNumber(result.grandTotal.parsedTotalRolls)} />
        <DetailItem label="الحالة" value={result.hasUnresolvedGroups ? 'توجد مجموعات غير محلولة' : 'جاهز للمراجعة'} />
      </dl>
      <div className="line-list">
        {result.groups.map((group) => (
          <article className={`line-card ${group.fabricResolved && group.colorResolved ? '' : 'line-card--warning'}`} key={group.groupIndex}>
            <div className="line-card__head">
              <h3>{group.fabricCode} / {group.color}</h3>
              <span className={`status-pill ${group.fabricResolved && group.colorResolved ? 'status-pill--green' : 'status-pill--amber'}`}>
                {group.fabricResolved && group.colorResolved ? 'محلول' : 'يحتاج ربط'}
              </span>
            </div>
            <dl className="mini-grid">
              <DetailItem label="الأمتار" value={`${formatMeters(group.parsedTotalMeters)} / ${formatMeters(group.declaredTotalMeters)}`} />
              <DetailItem label="الأثواب" value={`${formatNumber(group.parsedTotalRolls)} / ${formatNumber(group.declaredTotalRolls)}`} />
              <DetailItem label="مطابقة الأمتار" value={group.metersMatch ? 'مطابق' : 'غير مطابق'} />
              <DetailItem label="مطابقة الأثواب" value={group.rollsMatch ? 'مطابق' : 'غير مطابق'} />
              <DetailItem label="الملاحظات" value={group.resolutionError ?? (group.resolutionIssues.map((issue) => issue.reason).join('، ') || 'لا توجد')} />
            </dl>
          </article>
        ))}
      </div>
    </section>
  );
}

function InvoiceParseResult({ result }: { result: ChinaInvoiceParseResultDto }) {
  return (
    <section className="parse-result">
      <h3>نتيجة الفاتورة: {result.fileName}</h3>
      <dl className="mini-grid">
        <DetailItem label="الشحن البحري" value={formatCurrency(result.seaFreightUsd)} />
        <DetailItem label="التأمين" value={formatCurrency(result.insuranceUsd)} />
        <DetailItem label="الإجمالي" value={formatCurrency(result.grandTotalUsd)} />
        <DetailItem label="الأمتار" value={formatMeters(result.declaredTotalMeters)} />
        <DetailItem label="الأثواب" value={formatNumber(result.declaredTotalRolls)} />
        <DetailItem label="التحقق" value={result.totalValidationWarning ?? (result.lineAmountsMatchTotal ? 'مطابق' : 'غير مطابق')} />
      </dl>
    </section>
  );
}

function PackingParseResult({ result }: { result: ChinaPackingSummaryParseResultDto }) {
  return (
    <section className="parse-result">
      <h3>نتيجة ملخص Packing: {result.fileName}</h3>
      <dl className="mini-grid">
        <DetailItem label="الأمتار" value={formatMeters(result.declaredTotalMeters)} />
        <DetailItem label="الأثواب" value={formatNumber(result.declaredTotalRolls)} />
        <DetailItem label="CBM" value={formatNumber(result.totalCbm)} />
        <DetailItem label="الوزن الصافي" value={`${formatNumber(result.totalNetWeightKg)} كغ`} />
        <DetailItem label="الوزن الإجمالي" value={`${formatNumber(result.totalGrossWeightKg)} كغ`} />
        <DetailItem label="عدد البنود" value={formatNumber(result.lines.length)} />
      </dl>
    </section>
  );
}

function FileStep({
  title,
  description,
  pending,
  fileName,
  error,
  onChange
}: {
  title: string;
  description: string;
  pending: boolean;
  fileName: string | null;
  error: string | null;
  onChange: (event: ChangeEvent<HTMLInputElement>) => void;
}) {
  return (
    <label className={`file-step ${fileName ? 'file-step--parsed' : ''} ${error ? 'file-step--error' : ''}`}>
      <strong>{title}</strong>
      <span>{description}</span>
      <span className={`upload-state ${pending ? 'upload-state--pending' : fileName ? 'upload-state--parsed' : error ? 'upload-state--error' : ''}`}>
        {pending ? 'جار الرفع والتحليل...' : fileName ? `✓ اسم الملف: ${fileName}` : error ? `تعذر التحليل: ${error}` : 'لم يتم اختيار ملف بعد'}
      </span>
      <input type="file" accept=".xls,.xlsx" onChange={onChange} disabled={pending} />
    </label>
  );
}

function NumberInput({ label, value, onChange }: { label: string; value: string; onChange: (value: string) => void }) {
  return (
    <label>
      {label}
      <input inputMode="decimal" value={value} onChange={(event) => onChange(event.target.value)} />
    </label>
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

function Pagination({ page, totalPages, totalCount, onPrevious, onNext }: { page: number; totalPages: number; totalCount: number; onPrevious: () => void; onNext: () => void }) {
  const hasPreviousPage = page > 1;
  const hasNextPage = page < totalPages;

  return (
    <nav className="pagination" aria-label="تنقل الصفحات">
      <button className="primary-button" type="button" disabled={!hasPreviousPage} onClick={onPrevious}>السابق</button>
      <span>صفحة {formatNumber(page)} من {formatNumber(Math.max(totalPages, 1))} • {formatNumber(totalCount)} حاوية</span>
      <button className="primary-button" type="button" disabled={!hasNextPage} onClick={onNext}>التالي</button>
    </nav>
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

function buildImportLines(groups: PackingListGroupDto[]): ImportContainerLineCommand[] {
  return groups.flatMap((group) => {
    if (!group.fabricItemId || !group.fabricColorId) {
      return [];
    }
    return group.rolls.map((roll) => ({
      lineNumber: roll.sequenceNumber,
      fabricItemId: group.fabricItemId ?? '',
      fabricColorId: group.fabricColorId ?? '',
      rollCount: 1,
      lengthMeters: roll.quantityMeters,
      weightKg: null,
      lotCode: nullableText(roll.lotCode),
      buyerCustomerId: null
    }));
  });
}

function mapDetailsLineToCommand(line: ContainerDetailsDto['fabricTypeLines'][number]): ContainerFabricTypeLineCommand {
  return {
    lineNumber: line.lineNumber,
    typeDisplayName: line.typeDisplayName,
    matchKey: line.typeDisplayName,
    fabricItemId: line.fabricItemId,
    fabricColorId: line.fabricColorId,
    lengthMeters: line.lengthMeters,
    rollCount: line.rollCount,
    netWeightKg: line.netWeightKg,
    cbm: 0,
    chinaUnitPriceUsd: line.chinaUnitPriceUsd,
    invoiceLineAmountUsd: line.lengthMeters * line.chinaUnitPriceUsd,
    hasInvoiceMatch: line.chinaUnitPriceUsd > 0,
    hasPlMatch: line.netWeightKg > 0,
    hasDplMatch: true,
    matchWarnings: null
  };
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
    return error.message;
  }
  return 'حدث خطأ غير متوقع.';
}

function errorToast(error: unknown): ToastState {
  return { tone: 'error', message: getErrorMessage(error) };
}
