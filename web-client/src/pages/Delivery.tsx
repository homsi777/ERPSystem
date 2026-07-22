import { useEffect, useMemo, useRef, useState, type FormEvent } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useLocation, useNavigate, useParams } from 'react-router-dom';
import { completeDetailing, getDetailing, getDetailingQueue, saveDetailingDraft } from '../api/detailing.ts';
import { getDetailingCandidateRolls, getInventoryWarehouses } from '../api/inventory.ts';
import { ApiError } from '../api/client.ts';
import type { WarehouseDetailingDto, WarehouseDetailingRollDto, WarehouseDetailingStatus } from '../api/types.ts';
import { useAuth } from '../auth/AuthContext.tsx';
import { AppShell } from '../components/AppShell.tsx';
import { DataCard } from '../components/DataCard.tsx';
import { EmptyState } from '../components/EmptyState.tsx';
import { ErrorState } from '../components/ErrorState.tsx';
import { Icon } from '../components/Icon.tsx';
import { LoadingState } from '../components/LoadingState.tsx';
import { SummaryCard } from '../components/SummaryCard.tsx';
import { getApiErrorMessage } from '../lib/apiError.ts';
import {
  displayLengthFromMeters,
  formatContainerLength,
  formatDate,
  formatInteger,
  formatLineIndex,
  formatNumber,
  lengthAbbrev,
  lengthUnitArabic,
  storedLengthFromDisplay,
  totalLengthLabel,
  unitStorageToDpl
} from '../lib/format.ts';
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
        <EmptyState
          title="لا تملك صلاحية الوصول"
          description="شاشة التسليم (تفصيل المستودع) تتطلب تفعيل صلاحية «تفصيل المستودع» في دور المستخدم من الإعدادات."
        />
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
  const [quickSerial, setQuickSerial] = useState('');
  const [quickLength, setQuickLength] = useState('');
  const [quickError, setQuickError] = useState<string | null>(null);
  const [editingRollDetailId, setEditingRollDetailId] = useState<string | null>(null);
  const [lengthTouched, setLengthTouched] = useState(false);
  const [dismissedWarningKey, setDismissedWarningKey] = useState<string | null>(null);
  const serialInputRef = useRef<HTMLInputElement>(null);

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
        const unit = unitStorageToDpl(roll.unit);
        initial[roll.rollDetailId] = roll.hasValidLength
          ? displayLengthFromMeters(roll.lengthMeters, unit).toFixed(2)
          : roll.draftLengthMeters != null && roll.draftLengthMeters > 0
            ? displayLengthFromMeters(roll.draftLengthMeters, unit).toFixed(2)
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

  const representativeUnit = detailingQuery.data?.rolls[0]?.unit ?? null;
  const representativeDpl = unitStorageToDpl(representativeUnit);
  const totalLabel = totalLengthLabel(representativeDpl);

  const totalEnteredMetersTrue = useMemo(() => {
    const rolls = detailingQuery.data?.rolls ?? [];
    return rolls.reduce((sum, roll) => {
      const displayValue = toNumber(lengths[roll.rollDetailId] ?? '');
      if (displayValue <= 0) {
        return sum;
      }
      return sum + storedLengthFromDisplay(displayValue, unitStorageToDpl(roll.unit));
    }, 0);
  }, [detailingQuery.data, lengths]);

  const totalEnteredDisplay = formatContainerLength(totalEnteredMetersTrue, representativeDpl);

  const isCompleted = detailingQuery.data?.status === 2;

  const allRollsValid = useMemo(() => {
    const rolls = detailingQuery.data?.rolls ?? [];
    if (rolls.length === 0) {
      return false;
    }
    return rolls.every((roll) => {
      const serial = toRollNumber(serials[roll.rollDetailId] ?? '');
      const length = toNumber(lengths[roll.rollDetailId] ?? '');
      return serial != null && length > 0;
    });
  }, [detailingQuery.data, lengths, serials]);

  const sortedRolls = useMemo(() => {
    const rolls = detailingQuery.data?.rolls ?? [];
    return [...rolls].sort((a, b) => a.rollSequence - b.rollSequence);
  }, [detailingQuery.data]);

  function isRollFilled(roll: WarehouseDetailingRollDto) {
    const serial = toRollNumber(serials[roll.rollDetailId] ?? '');
    const length = toNumber(lengths[roll.rollDetailId] ?? '');
    return serial != null && length > 0;
  }

  const pendingRolls = useMemo(() => sortedRolls.filter((roll) => !isRollFilled(roll)), [sortedRolls, lengths, serials]);
  const filledRolls = useMemo(() => sortedRolls.filter((roll) => isRollFilled(roll)), [sortedRolls, lengths, serials]);

  const activeRoll = useMemo(() => {
    if (editingRollDetailId) {
      return sortedRolls.find((roll) => roll.rollDetailId === editingRollDetailId) ?? null;
    }
    return pendingRolls[0] ?? null;
  }, [editingRollDetailId, sortedRolls, pendingRolls]);

  const activeRollUnit = unitStorageToDpl(activeRoll?.unit);

  const activeRollCandidatesQuery = useQuery({
    queryKey: [
      'detailing',
      'candidate-rolls',
      detailingQuery.data?.warehouseId,
      activeRoll?.chinaContainerId,
      activeRoll?.fabricItemId,
      activeRoll?.fabricColorId,
      invoiceId
    ],
    queryFn: () =>
      getDetailingCandidateRolls({
        warehouseId: detailingQuery.data!.warehouseId,
        containerId: activeRoll!.chinaContainerId,
        fabricItemId: activeRoll!.fabricItemId,
        fabricColorId: activeRoll!.fabricColorId,
        excludeSalesInvoiceId: invoiceId
      }),
    enabled: Boolean(
      activeRoll?.chinaContainerId &&
        activeRoll.fabricItemId &&
        activeRoll.fabricColorId &&
        detailingQuery.data?.warehouseId
    )
  });

  const matchedCandidate = useMemo(() => {
    const serialNum = toRollNumber(quickSerial);
    if (serialNum == null) {
      return null;
    }
    const candidates = activeRollCandidatesQuery.data ?? [];
    return candidates.find((candidate) => candidate.rollNumber === serialNum) ?? null;
  }, [quickSerial, activeRollCandidatesQuery.data]);

  useEffect(() => {
    setLengthTouched(false);
  }, [activeRoll?.rollDetailId]);

  useEffect(() => {
    if (matchedCandidate && !lengthTouched) {
      setQuickLength(displayLengthFromMeters(matchedCandidate.remainingLengthMeters, activeRollUnit).toFixed(2));
    }
  }, [matchedCandidate, activeRollUnit, lengthTouched]);

  const reservationWarningKey =
    activeRoll && matchedCandidate ? `${activeRoll.rollDetailId}:${matchedCandidate.rollNumber}` : null;
  const showReservationWarning =
    Boolean(matchedCandidate?.reservedInSalesInvoiceId) && reservationWarningKey !== dismissedWarningKey;

  function focusSerialInput() {
    requestAnimationFrame(() => serialInputRef.current?.focus());
  }

  function handleQuickAdd() {
    if (!activeRoll) {
      return;
    }
    const serialNum = toRollNumber(quickSerial);
    const lengthNum = toNumber(quickLength);
    if (serialNum == null || lengthNum <= 0) {
      setQuickError('أدخلي رقم السيريال والطول معًا لهذا الثوب.');
      return;
    }

    const duplicate = sortedRolls.find((roll) => {
      if (roll.rollDetailId === activeRoll.rollDetailId) {
        return false;
      }
      return toRollNumber(serials[roll.rollDetailId] ?? '') === serialNum;
    });
    if (duplicate) {
      setQuickError(
        `رقم السيريال ${serialNum} مستخدم مسبقاً في ثوب آخر بهذه الفاتورة. كل توب يجب أن يحمل سيريالاً فريداً.`
      );
      return;
    }

    setSerials((current) => ({ ...current, [activeRoll.rollDetailId]: quickSerial.trim() }));
    setLengths((current) => ({ ...current, [activeRoll.rollDetailId]: quickLength.trim() }));

    setQuickSerial('');
    setQuickLength('');
    setQuickError(null);
    setLengthTouched(false);
    setEditingRollDetailId(null);
    focusSerialInput();
  }

  function startEditRoll(roll: WarehouseDetailingRollDto) {
    setEditingRollDetailId(roll.rollDetailId);
    setQuickSerial(serials[roll.rollDetailId] ?? '');
    setQuickLength(lengths[roll.rollDetailId] ?? '');
    setQuickError(null);
    setLengthTouched(true);
    focusSerialInput();
  }

  function clearRoll(roll: WarehouseDetailingRollDto) {
    setSerials((current) => ({ ...current, [roll.rollDetailId]: '' }));
    setLengths((current) => ({ ...current, [roll.rollDetailId]: '' }));
    if (editingRollDetailId === roll.rollDetailId) {
      setEditingRollDetailId(null);
      setQuickSerial('');
      setQuickLength('');
      setLengthTouched(false);
    }
  }

  const completeMutation = useMutation({
    mutationFn: () => {
      const duplicate = findDuplicateSerial(detailingQuery.data?.rolls ?? [], serials);
      if (duplicate != null) {
        throw new ApiError(400, {
          code: 'ValidationFailed',
          message: `رقم السيريال ${duplicate} مكرر في نفس الفاتورة. كل توب يجب أن يحمل سيريالاً فريداً.`,
          validationErrors: []
        });
      }
      return completeDetailing(invoiceId, {
        rollEntries: (detailingQuery.data?.rolls ?? []).map((roll) => ({
          rollDetailId: roll.rollDetailId,
          rollNumber: toRollNumber(serials[roll.rollDetailId] ?? ''),
          lengthMeters: storedLengthFromDisplay(toNumber(lengths[roll.rollDetailId] ?? ''), unitStorageToDpl(roll.unit))
        }))
      });
    },
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
    mutationFn: () => {
      const duplicate = findDuplicateSerial(detailingQuery.data?.rolls ?? [], serials);
      if (duplicate != null) {
        throw new ApiError(400, {
          code: 'ValidationFailed',
          message: `رقم السيريال ${duplicate} مكرر في نفس الفاتورة. كل توب يجب أن يحمل سيريالاً فريداً.`,
          validationErrors: []
        });
      }
      return saveDetailingDraft(invoiceId, {
        rollEntries: (detailingQuery.data?.rolls ?? []).map((roll) => {
          const displayValue = toOptionalNumber(lengths[roll.rollDetailId] ?? '');
          return {
            rollDetailId: roll.rollDetailId,
            rollNumber: toRollNumber(serials[roll.rollDetailId] ?? ''),
            lengthMeters: displayValue == null ? null : storedLengthFromDisplay(displayValue, unitStorageToDpl(roll.unit))
          };
        })
      });
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['detailing', invoiceId] });
      await queryClient.invalidateQueries({ queryKey: ['detailing'] });
      setToast({
        tone: 'success',
        message: 'تم حفظ التقدم على الفاتورة. سيظهر في سطح المكتب والمتصفح عند فتح نفس الفاتورة.'
      });
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
    <SummaryCard label={totalLabel} value={totalEnteredDisplay} tone="amber" />
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
              <DetailItem label={totalLabel} value={totalEnteredDisplay} />
            </dl>
          </section>

          <section className="form-panel form-compact" aria-label="تفصيل الأثواب">
            <div className="form-section-head">
              <h2>تفصيل الأثواب</h2>
              {!isCompleted ? (
                <span className="form-hint">
                  {formatNumber(filledRolls.length)} / {formatNumber(detailing.rolls.length)} تم تفنيدها
                </span>
              ) : null}
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
                        <strong>{formatContainerLength(roll.lengthMeters, unitStorageToDpl(roll.unit))}</strong>
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
                  أدخلي رقم السيريال — سيُملأ الطول تلقائيًا من المخزون ويمكنك تعديله عند الحاجة، ثم اضغطي "+ تفنيد".
                </p>

                {activeRoll ? (
                  <p className="form-hint" style={{ marginBottom: 4 }}>
                    القادم: ثوب رقم {formatLineIndex(activeRoll.rollSequence)} — {activeRoll.fabricDisplayName} /{' '}
                    {activeRoll.colorDisplayName} — الوحدة: {lengthUnitArabic(activeRollUnit)}
                  </p>
                ) : (
                  <div className="banner banner--success" role="status">
                    تم تفنيد كل الأثواب ({formatNumber(filledRolls.length)}/{formatNumber(detailing.rolls.length)}).
                  </div>
                )}

                <div className="quick-entry">
                  <div className="form-row form-row--2">
                    <label className="form-field">
                      <span className="form-field__label">رقم التوب (سيريال)</span>
                      <input
                        ref={serialInputRef}
                        inputMode="numeric"
                        placeholder="مثلاً 126"
                        value={quickSerial}
                        onChange={(event) => setQuickSerial(event.target.value)}
                        onKeyDown={(event) => {
                          if (event.key === 'Enter') {
                            event.preventDefault();
                            handleQuickAdd();
                          }
                        }}
                        disabled={!activeRoll}
                        aria-label="رقم سيريال الثوب"
                      />
                    </label>
                    <label className="form-field">
                      <span className="form-field__label">الطول ({lengthUnitArabic(activeRollUnit)})</span>
                      <input
                        inputMode="decimal"
                        placeholder="0"
                        value={quickLength}
                        onChange={(event) => {
                          setQuickLength(event.target.value);
                          setLengthTouched(true);
                        }}
                        onKeyDown={(event) => {
                          if (event.key === 'Enter') {
                            event.preventDefault();
                            handleQuickAdd();
                          }
                        }}
                        disabled={!activeRoll}
                        aria-label="طول الثوب"
                      />
                    </label>
                  </div>

                  {quickError ? <p className="form-hint form-hint--warn">{quickError}</p> : null}

                  <button className="primary-button" type="button" onClick={handleQuickAdd} disabled={!activeRoll}>
                    {editingRollDetailId ? 'تحديث' : '+ تفنيد'}
                  </button>
                </div>

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
                      onClick={() => setDismissedWarningKey(reservationWarningKey)}
                      aria-label="إغلاق التنبيه"
                    >
                      ×
                    </button>
                  </div>
                ) : null}

                {filledRolls.length > 0 ? (
                  <div className="line-items">
                    {filledRolls.map((roll) => (
                      <article className="line-item" key={roll.rollDetailId}>
                        <div className="line-item__head">
                          <span className="line-item__index">{formatLineIndex(roll.rollSequence)}</span>
                          <strong>
                            سيريال {serials[roll.rollDetailId]} — {lengths[roll.rollDetailId]}{' '}
                            {lengthAbbrev(unitStorageToDpl(roll.unit))}
                          </strong>
                          <div style={{ display: 'flex', gap: 6 }}>
                            <button type="button" className="chip-button" onClick={() => startEditRoll(roll)}>
                              تعديل
                            </button>
                            <button
                              type="button"
                              className="line-item__remove"
                              onClick={() => clearRoll(roll)}
                              aria-label={`حذف تفنيد الثوب رقم ${formatInteger(roll.rollSequence)}`}
                            >
                              ×
                            </button>
                          </div>
                        </div>
                        <p className="line-item__meta">
                          {roll.fabricDisplayName} / {roll.colorDisplayName}
                        </p>
                      </article>
                    ))}
                  </div>
                ) : null}
              </>
            )}
          </section>

          {!isCompleted ? (
            <div className="sticky-form-footer">
              <div className="sticky-form-footer__total">
                <span>{totalLabel}</span>
                <strong>{totalEnteredDisplay}</strong>
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
                  title={!allRollsValid ? 'أدخلي رقم السيريال والطول لكل الأثواب قبل الإكمال.' : undefined}
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
  return getApiErrorMessage(error, 'حدث خطأ غير متوقع.');
}

function findDuplicateSerial(
  rolls: WarehouseDetailingRollDto[],
  serials: Record<string, string>
): number | null {
  const seen = new Set<number>();
  for (const roll of rolls) {
    const serial = toRollNumber(serials[roll.rollDetailId] ?? '');
    if (serial == null) {
      continue;
    }
    if (seen.has(serial)) {
      return serial;
    }
    seen.add(serial);
  }
  return null;
}
