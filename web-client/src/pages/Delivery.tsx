import { useEffect, useMemo, useState, type FormEvent } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useLocation, useNavigate, useParams } from 'react-router-dom';
import { completeDetailing, getDetailing, getDetailingQueue, saveDetailingDraft } from '../api/detailing.ts';
import { getDetailingCandidateRolls, getInventoryWarehouses } from '../api/inventory.ts';
import { ApiError } from '../api/client.ts';
import type {
  DetailingCandidateRollDto,
  WarehouseDetailingDto,
  WarehouseDetailingRollDto,
  WarehouseDetailingStatus
} from '../api/types.ts';
import { useAuth } from '../auth/AuthContext.tsx';
import { AppShell } from '../components/AppShell.tsx';
import { DataCard } from '../components/DataCard.tsx';
import { EmptyState } from '../components/EmptyState.tsx';
import { ErrorState } from '../components/ErrorState.tsx';
import { Icon } from '../components/Icon.tsx';
import { LoadingState } from '../components/LoadingState.tsx';
import { SummaryCard } from '../components/SummaryCard.tsx';
import { formatDate, formatInteger, formatLineIndex, formatMeters, formatNumber } from '../lib/format.ts';
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
  const location = useLocation();
  const navigate = useNavigate();
  const [warehouseId, setWarehouseId] = useState('');
  const [toast, setToast] = useState<ToastState | null>(null);

  useEffect(() => {
    const state = location.state as { toast?: ToastState } | null;
    if (state?.toast) {
      setToast(state.toast);
      navigate(location.pathname, { replace: true, state: null });
    }
  }, [location.pathname, location.state, navigate]);

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
      <Toast toast={toast} onClose={() => setToast(null)} />
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
  const [serials, setSerials] = useState<Record<string, string>>({});

  const detailingQuery = useQuery({
    queryKey: ['detailing', invoiceId],
    queryFn: () => getDetailing(invoiceId)
  });

  useEffect(() => {
    if (!detailingQuery.data) {
      return;
    }
    // Previously-saved partial progress (Part 4) pre-populates the inputs so the employee
    // doesn't lose work if interrupted; a finally-resolved length always takes priority.
    setLengths((current) => {
      if (Object.keys(current).length > 0) {
        return current;
      }
      const initial: Record<string, string> = {};
      for (const roll of detailingQuery.data.rolls) {
        initial[roll.rollDetailId] = roll.hasValidLength
          ? String(roll.lengthMeters)
          : roll.draftLengthMeters != null && roll.draftLengthMeters > 0
            ? String(roll.draftLengthMeters)
            : '';
      }
      return initial;
    });
    setSerials((current) => {
      if (Object.keys(current).length > 0) {
        return current;
      }
      const initial: Record<string, string> = {};
      for (const roll of detailingQuery.data.rolls) {
        initial[roll.rollDetailId] =
          roll.draftRollNumber != null && roll.draftRollNumber > 0 ? String(roll.draftRollNumber) : '';
      }
      return initial;
    });
  }, [detailingQuery.data]);

  const totalEnteredMeters = useMemo(() => {
    // مجموع تقريبي: يُحسب من الأطوال اليدوية فقط؛ عند السيريال يُحل الطول في الـ API من المخزون.
    return Object.values(lengths).reduce((sum, value) => sum + (toNumber(value) || 0), 0);
  }, [lengths]);

  const isCompleted = detailingQuery.data?.status === 2;

  const allRollsValid = useMemo(() => {
    const rolls = detailingQuery.data?.rolls ?? [];
    if (rolls.length === 0) {
      return false;
    }
    return rolls.every((roll) => {
      const serial = toRollNumber(serials[roll.rollDetailId] ?? '');
      const length = toNumber(lengths[roll.rollDetailId] ?? '');
      return serial != null || length > 0;
    });
  }, [detailingQuery.data, lengths, serials]);

  const completeMutation = useMutation({
    mutationFn: () =>
      completeDetailing(invoiceId, {
        rollEntries: (detailingQuery.data?.rolls ?? []).map((roll) => ({
          rollDetailId: roll.rollDetailId,
          rollNumber: toRollNumber(serials[roll.rollDetailId] ?? ''),
          lengthMeters: toNumber(lengths[roll.rollDetailId] ?? '')
        }))
      }),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['detailing'] });
      await queryClient.invalidateQueries({ queryKey: ['dashboard'] });
      navigate('/delivery', {
        replace: true,
        state: { toast: { tone: 'success', message: 'تم إكمال التفصيل بنجاح. يمكنك اعتماد الفاتورة من شاشة المبيعات.' } }
      });
    },
    onError: (error) => setToast({ tone: 'error', message: getErrorMessage(error) })
  });

  const saveDraftMutation = useMutation({
    mutationFn: () =>
      saveDetailingDraft(invoiceId, {
        rollEntries: (detailingQuery.data?.rolls ?? []).map((roll) => ({
          rollDetailId: roll.rollDetailId,
          rollNumber: toRollNumber(serials[roll.rollDetailId] ?? ''),
          lengthMeters: toOptionalNumber(lengths[roll.rollDetailId] ?? '')
        }))
      }),
    onSuccess: () => {
      setToast({ tone: 'success', message: 'تم حفظ التقدم الحالي. يمكنك إكمال الأثواب المتبقية لاحقاً.' });
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
            <div className="compact-hero">
              <div>
                <p className="compact-hero__eyebrow">{detailing.customerName || 'عميل غير محدد'}</p>
                <h2>{detailing.invoiceNumber}</h2>
              </div>
              <DeliveryStatusPill status={detailing.status} />
            </div>
            <dl className="detail-grid">
              <DetailItem label="تاريخ الإرسال" value={formatDate(detailing.sentToWarehouseAt)} />
              <DetailItem
                label="سعر الوحدة"
                value={detailing.representativeUnitPrice != null ? formatNumber(detailing.representativeUnitPrice) : '—'}
              />
              <DetailItem label="عدد الأثواب" value={formatNumber(detailing.rolls.length)} />
              <DetailItem label="الأمتار المُدخلة" value={formatMeters(totalEnteredMeters)} />
            </dl>
          </section>

          <section className="form-panel form-compact" aria-label="تفصيل الأثواب">
            <div className="form-section-head">
              <h2>تفصيل الأثواب</h2>
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
                        <span className="line-item__index">{formatLineIndex(roll.rollSequence)}</span>
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
              <>
                <p className="form-hint" style={{ marginBottom: 8 }}>
                  أدخل رقم التوب (سيريال DPL) أو الطول بالمتر — يكفي أحدهما، أو اختر من قائمة اللفافات المتاحة أدناه.
                  السيريال أدق ويخصم من نفس التوب في المخزون.
                </p>
                <div className="line-items">
                  {detailing.rolls.map((roll) => (
                    <RollCard
                      key={roll.rollDetailId}
                      roll={roll}
                      warehouseId={detailing.warehouseId}
                      invoiceId={invoiceId}
                      serialValue={serials[roll.rollDetailId] ?? ''}
                      lengthValue={lengths[roll.rollDetailId] ?? ''}
                      onSerialChange={(value) =>
                        setSerials((current) => ({ ...current, [roll.rollDetailId]: value }))
                      }
                      onLengthChange={(value) =>
                        setLengths((current) => ({ ...current, [roll.rollDetailId]: value }))
                      }
                    />
                  ))}
                </div>
              </>
            )}
          </section>

          {!isCompleted ? (
            <div className="sticky-form-footer">
              <div className="sticky-form-footer__total">
                <span>إجمالي الأمتار (يدوي)</span>
                <strong>{formatMeters(totalEnteredMeters)}</strong>
              </div>
              <div style={{ display: 'flex', gap: 8 }}>
                <button
                  type="button"
                  className="ghost-button"
                  onClick={() => saveDraftMutation.mutate()}
                  disabled={saveDraftMutation.isPending || completeMutation.isPending}
                  title="حفظ التقدم الحالي دون إكمال التفصيل"
                >
                  {saveDraftMutation.isPending ? 'جار الحفظ...' : 'حفظ التقدم'}
                </button>
                <button
                  className="primary-button sticky-form-footer__submit"
                  type="submit"
                  disabled={!allRollsValid || completeMutation.isPending}
                  title={!allRollsValid ? 'أدخل رقم التوب أو الطول لكل الأثواب قبل الإكمال.' : undefined}
                >
                  {completeMutation.isPending ? 'جار الإكمال...' : 'إكمال التفصيل'}
                </button>
              </div>
            </div>
          ) : null}
        </form>
      ) : null}
    </AppShell>
  );
}

type RollCardProps = {
  roll: WarehouseDetailingRollDto;
  warehouseId: string;
  invoiceId: string;
  serialValue: string;
  lengthValue: string;
  onSerialChange: (value: string) => void;
  onLengthChange: (value: string) => void;
};

function RollCard({
  roll,
  warehouseId,
  invoiceId,
  serialValue,
  lengthValue,
  onSerialChange,
  onLengthChange
}: RollCardProps) {
  const [dismissedKey, setDismissedKey] = useState<string | null>(null);

  const canLookup = Boolean(warehouseId) && Boolean(roll.chinaContainerId) && Boolean(roll.fabricItemId) && Boolean(roll.fabricColorId);

  const candidatesQuery = useQuery({
    queryKey: [
      'detailing',
      'candidate-rolls',
      warehouseId,
      roll.chinaContainerId,
      roll.fabricItemId,
      roll.fabricColorId,
      invoiceId
    ],
    queryFn: () =>
      getDetailingCandidateRolls({
        warehouseId,
        containerId: roll.chinaContainerId,
        fabricItemId: roll.fabricItemId,
        fabricColorId: roll.fabricColorId,
        excludeSalesInvoiceId: invoiceId
      }),
    enabled: canLookup
  });

  const candidates = candidatesQuery.data ?? [];
  const selectedSerial = toRollNumber(serialValue);
  const matchedCandidate = selectedSerial != null
    ? candidates.find((candidate) => candidate.rollNumber === selectedSerial)
    : undefined;
  const warningKey = matchedCandidate ? `${roll.rollDetailId}:${matchedCandidate.fabricRollId}` : null;
  const showReservationWarning = Boolean(matchedCandidate?.reservedInSalesInvoiceId) && warningKey !== dismissedKey;

  return (
    <article className="line-item">
      <div className="line-item__head">
        <span className="line-item__index">{formatLineIndex(roll.rollSequence)}</span>
      </div>
      <p className="line-item__meta">
        {roll.fabricDisplayName} / {roll.colorDisplayName}
        {roll.containerDisplay && roll.containerDisplay !== '—' ? ` — حاوية ${roll.containerDisplay}` : ''}
      </p>

      <div className="form-row form-row--2">
        <label className="form-field">
          <span className="form-field__label">رقم التوب (سيريال)</span>
          <input
            inputMode="numeric"
            placeholder="مثلاً 126"
            value={serialValue}
            onChange={(event) => onSerialChange(event.target.value)}
            aria-label={`رقم سيريال الثوب ${formatInteger(roll.rollSequence)}`}
          />
        </label>
        <label className="form-field">
          <span className="form-field__label">أو الطول (م)</span>
          <input
            inputMode="decimal"
            placeholder="0"
            value={lengthValue}
            onChange={(event) => onLengthChange(event.target.value)}
            aria-label={`طول الثوب رقم ${formatInteger(roll.rollSequence)}`}
          />
        </label>
      </div>

      {canLookup ? (
        <div className="candidate-rolls">
          <p className="candidate-rolls__label">لفافات متاحة في هذه الحاوية</p>

          {candidatesQuery.isLoading ? <p className="form-hint">جار تحميل اللفافات المتاحة...</p> : null}

          {candidatesQuery.isError ? (
            <p className="form-hint form-hint--warn">تعذّر تحميل اللفافات المتاحة لهذا الصنف.</p>
          ) : null}

          {candidatesQuery.isSuccess && candidates.length === 0 ? (
            <p className="form-hint">لا توجد لفافات متاحة في هذه الحاوية.</p>
          ) : null}

          {candidates.length > 0 ? (
            <div className="candidate-rolls__list">
              {candidates.map((candidate) => (
                <CandidateChip
                  key={candidate.fabricRollId}
                  candidate={candidate}
                  isSelected={selectedSerial === candidate.rollNumber}
                  onSelect={() => onSerialChange(String(candidate.rollNumber))}
                />
              ))}
            </div>
          ) : null}
        </div>
      ) : null}

      {showReservationWarning && matchedCandidate ? (
        <div className="reservation-warning" role="status">
          <span>
            هذا التوب موجود بالفعل في فاتورة رقم{' '}
            <Link to={`/sales/${matchedCandidate.reservedInSalesInvoiceId}`}>
              {matchedCandidate.reservedInSalesInvoiceNumber}
            </Link>
            . يمكنك المتابعة إذا كنت متأكداً.
          </span>
          <button
            type="button"
            className="reservation-warning__dismiss"
            onClick={() => setDismissedKey(warningKey)}
            aria-label="إغلاق التنبيه"
          >
            ×
          </button>
        </div>
      ) : null}
    </article>
  );
}

function CandidateChip({
  candidate,
  isSelected,
  onSelect
}: {
  candidate: DetailingCandidateRollDto;
  isSelected: boolean;
  onSelect: () => void;
}) {
  const isReserved = Boolean(candidate.reservedInSalesInvoiceId);
  return (
    <button
      type="button"
      className={`candidate-chip${isSelected ? ' candidate-chip--selected' : ''}${isReserved ? ' candidate-chip--reserved' : ''}`}
      onClick={onSelect}
      title={isReserved ? `محجوز أيضاً في فاتورة ${candidate.reservedInSalesInvoiceNumber}` : undefined}
    >
      <span className="candidate-chip__dot" aria-hidden="true" />
      <span>
        {formatLineIndex(candidate.rollNumber)} • {formatMeters(candidate.remainingLengthMeters)}
      </span>
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

function toNumber(value: string) {
  const normalized = Number(value.replace(',', '.'));
  return Number.isFinite(normalized) ? normalized : 0;
}

function toRollNumber(value: string): number | null {
  const trimmed = value.trim();
  if (!trimmed) {
    return null;
  }
  const parsed = Number.parseInt(trimmed, 10);
  return Number.isFinite(parsed) && parsed > 0 ? parsed : null;
}

function toOptionalNumber(value: string): number | null {
  const trimmed = value.trim();
  if (!trimmed) {
    return null;
  }
  const parsed = Number(trimmed.replace(',', '.'));
  return Number.isFinite(parsed) && parsed > 0 ? parsed : null;
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
