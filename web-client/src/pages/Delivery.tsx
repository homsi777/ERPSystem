import { useEffect, useMemo, useState, type FormEvent } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { completeDetailing, getDetailing, getDetailingQueue } from '../api/detailing.ts';
import { getInventoryWarehouses } from '../api/inventory.ts';
import { ApiError } from '../api/client.ts';
import type { WarehouseDetailingDto, WarehouseDetailingStatus } from '../api/types.ts';
import { useAuth } from '../auth/AuthContext.tsx';
import { AppShell } from '../components/AppShell.tsx';
import { DataCard } from '../components/DataCard.tsx';
import { EmptyState } from '../components/EmptyState.tsx';
import { ErrorState } from '../components/ErrorState.tsx';
import { Icon } from '../components/Icon.tsx';
import { LoadingState } from '../components/LoadingState.tsx';
import { SummaryCard } from '../components/SummaryCard.tsx';
import { formatDate, formatMeters, formatNumber } from '../lib/format.ts';
import { getWarehouseDetailingStatusTone, warehouseDetailingStatusLabels } from '../lib/enums.ts';

type ToastState = {
  tone: 'success' | 'error';
  message: string;
};

export function DeliveryPage() {
  const { invoiceId } = useParams();
  const { can } = useAuth();

  if (!can('warehouse.detailing')) {
    return (
      <AppShell title="التسليم">
        <EmptyState title="لا تملك صلاحية الوصول" description="شاشة تفصيل المستودع تتطلب صلاحية warehouse.detailing." />
      </AppShell>
    );
  }

  if (invoiceId) {
    return <DeliveryDetailPage invoiceId={invoiceId} />;
  }

  return <DeliveryQueuePage />;
}

function DeliveryQueuePage() {
  const [warehouseId, setWarehouseId] = useState('');

  const warehousesQuery = useQuery({
    queryKey: ['inventory', 'warehouses'],
    queryFn: () => getInventoryWarehouses()
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

  const queueQuery = useQuery({
    queryKey: ['detailing', 'queue', warehouseId],
    queryFn: () => getDetailingQueue(warehouseId),
    enabled: warehouseId.length > 0
  });

  const headerSummary = (
    <SummaryCard label="بانتظار التفصيل" value={formatNumber(queueQuery.data?.length ?? 0)} tone="amber" />
  );

  return (
    <AppShell title="التسليم" summary={headerSummary}>
      <div className="page-stack">
        <section className="form-panel form-compact form-panel--filter" aria-label="اختيار المستودع">
          <label className="form-field form-field--wide">
            <span className="form-field__label">المستودع</span>
            <select
              value={warehouseId}
              onChange={(event) => setWarehouseId(event.target.value)}
              disabled={warehousesQuery.isLoading || warehousesQuery.isError}
            >
              <option value="">اختر المستودع...</option>
              {(warehousesQuery.data ?? []).map((warehouse) => (
                <option key={warehouse.id} value={warehouse.id}>
                  {warehouse.nameAr}
                </option>
              ))}
            </select>
          </label>
        </section>

      {!warehouseId ? (
        <EmptyState title="اختر مستودعًا" description="اختر المستودع لعرض فواتير بانتظار التفصيل." />
      ) : null}

      {warehouseId && queueQuery.isLoading ? <LoadingState /> : null}

      {warehouseId && queueQuery.isError ? (
        <ErrorState message={getErrorMessage(queueQuery.error)} onRetry={() => void queueQuery.refetch()} />
      ) : null}

      {warehouseId && queueQuery.isSuccess && queueQuery.data.length === 0 ? (
        <EmptyState title="لا توجد فواتير" description="لا توجد فواتير بانتظار التفصيل في هذا المستودع حاليًا." />
      ) : null}

      {warehouseId && queueQuery.isSuccess && queueQuery.data.length > 0 ? (
        <section className="card-list" aria-label="قائمة انتظار التفصيل">
          {queueQuery.data.map((item) => (
            <Link className="card-link" key={item.invoiceId} to={`/delivery/${item.invoiceId}`}>
              <DeliveryQueueCard item={item} />
            </Link>
          ))}
        </section>
      ) : null}
      </div>
    </AppShell>
  );
}

function DeliveryQueueCard({ item }: { item: WarehouseDetailingDto }) {
  const meta = `${formatDate(item.sentToWarehouseAt)} • ${formatNumber(item.rolls.length)} ثوب`;

  return (
    <DataCard
      icon={<Icon name="delivery" />}
      title={item.invoiceNumber}
      subtitle={item.customerName || 'عميل غير محدد'}
      meta={meta}
      value={<DeliveryStatusPill status={item.status} />}
      tone={item.status === 2 ? 'available' : 'neutral'}
    />
  );
}

function DeliveryStatusPill({ status }: { status: WarehouseDetailingStatus }) {
  return (
    <span className={`status-pill status-pill--${getWarehouseDetailingStatusTone(status)}`}>
      {warehouseDetailingStatusLabels[status]}
    </span>
  );
}

function DeliveryDetailPage({ invoiceId }: { invoiceId: string }) {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [toast, setToast] = useState<ToastState | null>(null);
  const [lengths, setLengths] = useState<Record<string, string>>({});

  const detailingQuery = useQuery({
    queryKey: ['detailing', invoiceId],
    queryFn: () => getDetailing(invoiceId)
  });

  useEffect(() => {
    if (!detailingQuery.data) {
      return;
    }
    setLengths((current) => {
      if (Object.keys(current).length > 0) {
        return current;
      }
      const initial: Record<string, string> = {};
      for (const roll of detailingQuery.data.rolls) {
        initial[roll.rollDetailId] = roll.hasValidLength ? String(roll.lengthMeters) : '';
      }
      return initial;
    });
  }, [detailingQuery.data]);

  const totalEnteredMeters = useMemo(() => {
    // عرض فقط: مجموع فوري للأمتار المُدخلة في الواجهة، والقيمة الفعلية تُعاد التحقق منها في الـ API عند الإكمال.
    return Object.values(lengths).reduce((sum, value) => sum + (toNumber(value) || 0), 0);
  }, [lengths]);

  const isCompleted = detailingQuery.data?.status === 2;

  const allRollsValid = useMemo(() => {
    const rolls = detailingQuery.data?.rolls ?? [];
    if (rolls.length === 0) {
      return false;
    }
    return rolls.every((roll) => toNumber(lengths[roll.rollDetailId] ?? '') > 0);
  }, [detailingQuery.data, lengths]);

  const completeMutation = useMutation({
    mutationFn: () =>
      completeDetailing(invoiceId, {
        rollEntries: (detailingQuery.data?.rolls ?? []).map((roll) => ({
          rollDetailId: roll.rollDetailId,
          lengthMeters: toNumber(lengths[roll.rollDetailId] ?? '')
        }))
      }),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['detailing'] });
      setToast({ tone: 'success', message: 'تم إكمال التفصيل بنجاح.' });
      navigate('/delivery');
    },
    onError: (error) => setToast({ tone: 'error', message: getErrorMessage(error) })
  });

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (isCompleted || !allRollsValid || completeMutation.isPending) {
      return;
    }
    completeMutation.mutate();
  }

  const detailing = detailingQuery.data;

  const headerSummary = detailing ? (
    <SummaryCard label="الأمتار المُدخلة" value={formatMeters(totalEnteredMeters)} tone="amber" />
  ) : undefined;

  return (
    <AppShell title={detailing ? detailing.invoiceNumber : 'تفصيل الفاتورة'} summary={headerSummary}>
      <Toast toast={toast} onClose={() => setToast(null)} />

      {detailingQuery.isLoading ? <LoadingState /> : null}

      {detailingQuery.isError ? (
        <ErrorState message={getErrorMessage(detailingQuery.error)} onRetry={() => void detailingQuery.refetch()} />
      ) : null}

      {detailing ? (
        <form className="page-stack page-stack--footer" onSubmit={submit}>
          <section className="form-panel form-compact" aria-label="بيانات الفاتورة">
            <div className="form-section-head">
              <h2>بيانات الفاتورة</h2>
              <DeliveryStatusPill status={detailing.status} />
            </div>
            <dl className="detail-grid">
              <DetailItem label="العميل" value={detailing.customerName || 'غير محدد'} />
              <DetailItem label="تاريخ الإرسال" value={formatDate(detailing.sentToWarehouseAt)} />
              <DetailItem
                label="سعر الوحدة"
                value={detailing.representativeUnitPrice != null ? formatNumber(detailing.representativeUnitPrice) : '—'}
              />
              <DetailItem label="عدد الأثواب" value={formatNumber(detailing.rolls.length)} />
            </dl>
          </section>

          <section className="form-panel form-compact" aria-label="أطوال الأثواب">
            <div className="form-section-head">
              <h2>أطوال الأثواب</h2>
              {!isCompleted ? <span className="form-hint">{formatNumber(detailing.rolls.length)} ثوب</span> : null}
            </div>

            {isCompleted ? (
              <>
                <div className="banner banner--success" role="status">
                  تم تفصيل هذه الفاتورة — بانتظار الاعتماد.
                </div>
                <div className="line-items">
                  {detailing.rolls.map((roll) => (
                    <article className="line-item" key={roll.rollDetailId}>
                      <div className="line-item__head">
                        <span className="line-item__index">#{roll.rollSequence}</span>
                        <strong>{formatMeters(roll.lengthMeters)}</strong>
                      </div>
                      <p className="line-item__meta">
                        {roll.fabricDisplayName} / {roll.colorDisplayName}
                      </p>
                    </article>
                  ))}
                </div>
                <button className="primary-button" type="button" onClick={() => navigate('/delivery')}>
                  العودة للقائمة
                </button>
              </>
            ) : (
              <div className="line-items">
                {detailing.rolls.map((roll) => (
                  <article className="line-item" key={roll.rollDetailId}>
                    <div className="line-item__head">
                      <span className="line-item__index">#{roll.rollSequence}</span>
                    </div>
                    <p className="line-item__meta">
                      {roll.fabricDisplayName} / {roll.colorDisplayName}
                    </p>
                    <label className="form-field form-field--wide">
                      <span className="form-field__label">الطول (م)</span>
                      <input
                        inputMode="decimal"
                        placeholder="0"
                        value={lengths[roll.rollDetailId] ?? ''}
                        onChange={(event) =>
                          setLengths((current) => ({ ...current, [roll.rollDetailId]: event.target.value }))
                        }
                        aria-label={`طول الثوب رقم ${roll.rollSequence}`}
                      />
                    </label>
                  </article>
                ))}
              </div>
            )}
          </section>

          {!isCompleted ? (
            <div className="sticky-form-footer">
              <div className="sticky-form-footer__total">
                <span>إجمالي الأمتار</span>
                <strong>{formatMeters(totalEnteredMeters)}</strong>
              </div>
              <button
                className="primary-button sticky-form-footer__submit"
                type="submit"
                disabled={!allRollsValid || completeMutation.isPending}
                title={!allRollsValid ? 'أدخل طول كل الأثواب قبل الإكمال.' : undefined}
              >
                {completeMutation.isPending ? 'جار الإكمال...' : 'إكمال التفصيل'}
              </button>
            </div>
          ) : null}
        </form>
      ) : null}
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
      return 'لا تملك صلاحية لإكمال التفصيل.';
    }
    if (error.status === 404) {
      return 'الفاتورة غير موجودة أو ليست بانتظار التفصيل.';
    }
    if (error.status === 409) {
      return 'تعذّر إكمال التفصيل — يبدو أن الفاتورة لم تعد بانتظار التفصيل (ربما تم تفصيلها مسبقًا). حدّث الصفحة للتحقق من الحالة.';
    }
    return error.message;
  }
  return 'حدث خطأ غير متوقع.';
}
