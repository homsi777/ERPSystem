import { useMemo, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link, useParams } from 'react-router-dom';
import { getContainerOperations, getContainers } from '../api/containers.ts';
import { ApiError } from '../api/client.ts';
import type {
  ChinaContainerStatus,
  ContainerDetailsDto,
  ContainerInventoryMetricsDto,
  ContainerListDto,
  LandingCostDto
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

export function ChinaPage() {
  const { containerId } = useParams();

  if (containerId) {
    return <ChinaContainerDetailsPage containerId={containerId} />;
  }

  return <ChinaContainerListPage />;
}

function ChinaContainerListPage() {
  const [status, setStatus] = useState<ChinaContainerStatus | undefined>();
  const [page, setPage] = useState(1);

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
          message={containersQuery.error instanceof ApiError ? containersQuery.error.message : 'حدث خطأ غير متوقع.'}
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

      <div className="floating-actions">
        <button className="primary-button" type="button" onClick={() => window.alert('قريباً')}>
          حاوية جديدة قريباً
        </button>
      </div>
    </AppShell>
  );
}

function ContainerListCard({ container }: { container: ContainerListDto }) {
  const meta = `${formatDate(container.shipmentDate)} • ${formatNumber(container.totalRolls)} ثوب • ${formatMeters(
    container.totalMeters
  )}`;

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

function ChinaContainerDetailsPage({ containerId }: { containerId: string }) {
  const { can } = useAuth();
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

  return (
    <AppShell title={container ? container.containerNumber : 'تفاصيل حاوية الصين'} summary={headerSummary}>
      {operationsQuery.isLoading ? <LoadingState /> : null}

      {operationsQuery.isError ? (
        <ErrorState
          message={operationsQuery.error instanceof ApiError ? operationsQuery.error.message : 'حدث خطأ غير متوقع.'}
          onRetry={() => void operationsQuery.refetch()}
        />
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
          />

          <ContainerInfoSection container={container} />
          <LandingCostSection landingCost={container.landingCost} />
          <FabricTypeLinesSection container={container} />
          <InventorySection inventory={data.inventory} />
        </div>
      ) : null}
    </AppShell>
  );
}

function StatusPill({ status }: { status: ChinaContainerStatus }) {
  return (
    <span className={`status-pill status-pill--${getChinaContainerStatusTone(status)}`}>
      {chinaContainerStatusLabels[status]}
    </span>
  );
}

function ActionPanel({
  canCalculateLandingCost,
  canSetSalePrices,
  canApprove,
  canMoveToWarehouse
}: {
  canCalculateLandingCost: boolean;
  canSetSalePrices: boolean;
  canApprove: boolean;
  canMoveToWarehouse: boolean;
}) {
  const actions = [
    { label: 'احتساب التكلفة', visible: canCalculateLandingCost },
    { label: 'أسعار البيع', visible: canSetSalePrices },
    { label: 'اعتماد الحاوية', visible: canApprove },
    { label: 'نقل للمستودع', visible: canMoveToWarehouse }
  ];
  const visibleActions = actions.filter((action) => action.visible);

  if (visibleActions.length === 0) {
    return null;
  }

  return (
    <section className="action-grid" aria-label="إجراءات الحاوية">
      {visibleActions.map((action) => (
        <button className="primary-button primary-button--wide" type="button" key={action.label} onClick={() => window.alert('قريباً')}>
          {action.label}
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
        <DetailItem
          label="الضريبة المرحلة"
          value={container.financialTaxReservePostedLocal === null ? 'غير مرحلة' : formatNumber(container.financialTaxReservePostedLocal)}
        />
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

function DetailItem({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <dt>{label}</dt>
      <dd>{value}</dd>
    </div>
  );
}

function Pagination({
  page,
  totalPages,
  totalCount,
  onPrevious,
  onNext
}: {
  page: number;
  totalPages: number;
  totalCount: number;
  onPrevious: () => void;
  onNext: () => void;
}) {
  const hasPreviousPage = page > 1;
  const hasNextPage = page < totalPages;

  return (
    <nav className="pagination" aria-label="تنقل الصفحات">
      <button className="primary-button" type="button" disabled={!hasPreviousPage} onClick={onPrevious}>
        السابق
      </button>
      <span>
        صفحة {formatNumber(page)} من {formatNumber(Math.max(totalPages, 1))} • {formatNumber(totalCount)} حاوية
      </span>
      <button className="primary-button" type="button" disabled={!hasNextPage} onClick={onNext}>
        التالي
      </button>
    </nav>
  );
}
