import { useEffect, useMemo, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import {
  getFabricRollSalesReservations,
  getFabricRollsByStock,
  getFabricSearchProfiles,
  getFabricStock,
  getInventoryAlerts,
  getInventoryDashboard,
  getInventoryWarehouses
} from '../api/inventory.ts';
import { ApiError } from '../api/client.ts';
import type {
  FabricRollListDto,
  FabricRollSalesReservationDto,
  FabricSearchProfileDto,
  FabricStockBalanceDto,
  InventoryAlertDto
} from '../api/types.ts';
import { AppShell } from '../components/AppShell.tsx';
import { DataCard } from '../components/DataCard.tsx';
import { EmptyState } from '../components/EmptyState.tsx';
import { ErrorState } from '../components/ErrorState.tsx';
import { Icon } from '../components/Icon.tsx';
import { LoadingState } from '../components/LoadingState.tsx';
import { Modal } from '../components/Modal.tsx';
import { RecordField } from '../components/RecordField.tsx';
import { SummaryCard } from '../components/SummaryCard.tsx';
import { formatCurrency, formatDate, formatDateOnly, formatMeters, formatNumber, EMPTY_CELL } from '../lib/format.ts';
import { inventoryStatusLabels } from '../lib/enums.ts';
import { useAuth } from '../auth/AuthContext.tsx';
import { canViewSensitivePricing } from '../auth/generalManagerAccess.ts';

const rollStatusLabels: Record<string, string> = {
  Available: 'متاح',
  Reserved: 'محجوز',
  PartiallySold: 'مباع جزئياً',
  PartiallyReserved: 'محجوز جزئياً',
  Sold: 'مباع',
  Damaged: 'تالف',
  InTransit: 'قيد النقل',
  Returned: 'مُرجع'
};

function rollStatusLabel(status: string) {
  return rollStatusLabels[status] ?? status;
}

function formatPricePerMeter(value: number | null | undefined) {
  return value != null && value > 0 ? `${formatCurrency(value)}/م` : EMPTY_CELL;
}

function filterProfileByContainer(profile: FabricSearchProfileDto, selectedContainerId: string): FabricSearchProfileDto {
  if (!selectedContainerId) {
    return profile;
  }

  const locations = profile.locations.filter((loc) => loc.containerId === selectedContainerId);
  const containerJourney = profile.containerJourney.filter((leg) => leg.containerId === selectedContainerId);
  const containerIds = new Set(locations.map((loc) => loc.containerId));

  return {
    ...profile,
    totalRolls: locations.reduce((sum, loc) => sum + loc.rollCount, 0),
    totalMeters: locations.reduce((sum, loc) => sum + loc.totalMeters, 0),
    availableMeters: locations.reduce((sum, loc) => sum + loc.availableMeters, 0),
    reservedMeters: locations.reduce((sum, loc) => sum + loc.reservedMeters, 0),
    inventoryValue: locations.reduce((sum, loc) => sum + loc.inventoryValue, 0),
    warehouseCount: new Set(locations.map((loc) => loc.warehouseId)).size,
    containerCount: containerIds.size,
    locations,
    containerJourney
  };
}

export function InventoryPage() {
  const { user } = useAuth();
  const showPricing = canViewSensitivePricing(user?.permissions ?? []);
  const [warehouseId, setWarehouseId] = useState('');
  const [containerId, setContainerId] = useState('');
  const [searchInput, setSearchInput] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [selectedRow, setSelectedRow] = useState<FabricStockBalanceDto | null>(null);

  useEffect(() => {
    const handle = window.setTimeout(() => setDebouncedSearch(searchInput.trim()), 280);
    return () => window.clearTimeout(handle);
  }, [searchInput]);

  const warehousesQuery = useQuery({
    queryKey: ['inventory', 'warehouses'],
    queryFn: () => getInventoryWarehouses()
  });

  const dashboardQuery = useQuery({
    queryKey: ['inventory', 'dashboard'],
    queryFn: () => getInventoryDashboard()
  });

  const alertsQuery = useQuery({
    queryKey: ['inventory', 'alerts'],
    queryFn: () => getInventoryAlerts()
  });

  const serverSearch = debouncedSearch.length >= 2 ? debouncedSearch : undefined;

  // Server search when term ≥ 2 chars; otherwise load by warehouse and filter client-side.
  const stockQuery = useQuery({
    queryKey: ['inventory', 'stock', warehouseId, serverSearch ?? ''],
    queryFn: () => getFabricStock(warehouseId || undefined, serverSearch)
  });

  const showRichSearch = debouncedSearch.length >= 2;

  const profileQuery = useQuery({
    queryKey: ['inventory', 'fabric-search-profiles', debouncedSearch, warehouseId],
    queryFn: () => getFabricSearchProfiles(debouncedSearch, warehouseId || undefined),
    enabled: showRichSearch
  });

  const searchProfiles = useMemo(() => {
    if (!profileQuery.data) {
      return [];
    }

    return profileQuery.data
      .map((profile) => filterProfileByContainer(profile, containerId))
      .filter((profile) => profile.locations.length > 0);
  }, [profileQuery.data, containerId]);

  const dashboard = dashboardQuery.data;
  const showWarehouseName = warehouseId.length === 0;
  const isFiltered = warehouseId.length > 0 || containerId.length > 0 || debouncedSearch.length > 0;

  const containerOptions = useMemo(() => {
    const rows = stockQuery.data ?? [];
    const scoped = warehouseId ? rows.filter((row) => row.warehouseId === warehouseId) : rows;
    const map = new Map<string, string>();
    for (const row of scoped) {
      if (row.containerId && !map.has(row.containerId)) {
        map.set(row.containerId, row.containerNumber || row.containerId);
      }
    }
    return [...map.entries()]
      .map(([id, number]) => ({ id, number }))
      .sort((a, b) => a.number.localeCompare(b.number, 'ar'));
  }, [stockQuery.data, warehouseId]);

  const filteredStock = useMemo(() => {
    let rows = stockQuery.data ?? [];
    if (containerId) {
      rows = rows.filter((row) => row.containerId === containerId);
    }
    // Client-side refine for short terms (< 2) or extra local match when server already filtered.
    if (debouncedSearch.length > 0 && debouncedSearch.length < 2) {
      const term = debouncedSearch.toLowerCase();
      rows = rows.filter(
        (row) =>
          row.fabricName.toLowerCase().includes(term) ||
          row.fabricCode.toLowerCase().includes(term) ||
          row.colorName.toLowerCase().includes(term) ||
          (row.containerNumber ?? '').toLowerCase().includes(term)
      );
    }
    return rows;
  }, [stockQuery.data, containerId, debouncedSearch]);

  const headerSummaryValues = useMemo(() => {
    if (showRichSearch && profileQuery.isSuccess && searchProfiles.length > 0) {
      return {
        totalValue: searchProfiles.reduce((sum, profile) => sum + profile.inventoryValue, 0),
        totalRolls: searchProfiles.reduce((sum, profile) => sum + profile.totalRolls, 0),
        itemCount: searchProfiles.length
      };
    }

    if (stockQuery.isSuccess) {
      return {
        totalValue: filteredStock.reduce((sum, row) => sum + row.inventoryValue, 0),
        totalRolls: filteredStock.reduce((sum, row) => sum + row.rollCount, 0),
        itemCount: filteredStock.length
      };
    }

    if (!isFiltered && dashboard) {
      return {
        totalValue: dashboard.totalInventoryValue,
        totalRolls: dashboard.totalRolls,
        itemCount: 0
      };
    }

    return { totalValue: 0, totalRolls: 0, itemCount: 0 };
  }, [showRichSearch, profileQuery.isSuccess, searchProfiles, stockQuery.isSuccess, filteredStock, isFiltered, dashboard]);

  function handleWarehouseChange(nextWarehouseId: string) {
    setWarehouseId(nextWarehouseId);
    setContainerId('');
  }

  const headerSummary = (
    <>
      {showPricing ? (
        <SummaryCard
          label={isFiltered ? 'قيمة التصفية' : 'قيمة المخزون'}
          value={formatCurrency(headerSummaryValues.totalValue)}
        />
      ) : null}
      <SummaryCard
        label={isFiltered ? 'أثواب التصفية' : 'عدد الأثواب'}
        value={formatNumber(headerSummaryValues.totalRolls)}
        tone="green"
      />
      <SummaryCard
        label={isFiltered ? 'أصناف التصفية' : 'الأصناف'}
        value={formatNumber(headerSummaryValues.itemCount)}
        tone="amber"
      />
    </>
  );

  return (
    <AppShell title="المخزون" summary={headerSummary}>
      <div className="page-stack">
      <section className="form-panel form-compact form-panel--filter form-panel--search" aria-label="بحث المخزون">
        <div className="form-section-head">
          <h2>بحث ذكي عن التوب</h2>
          <Link className="chip-button" to="/inventory/movements">
            حركة المخزون
          </Link>
        </div>
        <p className="form-hint inventory-search-hint">
          ابحث باسم التوب أو الكود لعرض ملف كامل: سعر البيع، التكلفة، المستودعات، الحاويات، الألوان، ورحلة التوب.
        </p>
        <div className="form-field-row form-field-row--2">
          <label className="form-field form-field--wide">
            <span className="form-field__label">بحث ذكي</span>
            <input
              type="search"
              value={searchInput}
              onChange={(event) => setSearchInput(event.target.value)}
              placeholder="اسم التوب، الكود، اللون، أو رقم الحاوية…"
              autoComplete="off"
              aria-label="بحث ذكي عن التوب في المخزون"
            />
          </label>
          <label className="form-field">
            <span className="form-field__label">المستودع</span>
            <select
              value={warehouseId}
              onChange={(event) => handleWarehouseChange(event.target.value)}
              disabled={warehousesQuery.isLoading || warehousesQuery.isError}
            >
              <option value="">كل المستودعات</option>
              {(warehousesQuery.data ?? []).map((warehouse) => (
                <option key={warehouse.id} value={warehouse.id}>
                  {warehouse.nameAr}
                </option>
              ))}
            </select>
          </label>
          <label className="form-field">
            <span className="form-field__label">الحاوية</span>
            <select
              value={containerId}
              onChange={(event) => setContainerId(event.target.value)}
              disabled={stockQuery.isLoading || containerOptions.length === 0}
            >
              <option value="">كل الحاويات</option>
              {containerOptions.map((container) => (
                <option key={container.id} value={container.id}>
                  {container.number}
                </option>
              ))}
            </select>
          </label>
        </div>
        {containerOptions.length === 0 && stockQuery.isSuccess ? (
          <p className="form-hint">لا توجد حاويات ضمن التصفية الحالية.</p>
        ) : null}
      </section>

      {showRichSearch ? (
        <section className="form-panel form-compact form-panel--search-results" aria-label="ملفات التوب">
          <div className="form-section-head">
            <h2>ملف التوب — «{debouncedSearch}»</h2>
          </div>

          {profileQuery.isLoading ? <LoadingState /> : null}

          {profileQuery.isError ? (
            <ErrorState
              message={profileQuery.error instanceof ApiError ? profileQuery.error.message : 'تعذّر تحميل ملف التوب.'}
              onRetry={() => void profileQuery.refetch()}
            />
          ) : null}

          {profileQuery.isSuccess && searchProfiles.length === 0 ? (
            <EmptyState
              title="لا توجد نتائج"
              description={`لا يوجد توب مطابق لـ «${debouncedSearch}» ضمن التصفية الحالية.`}
            />
          ) : null}

          {profileQuery.isSuccess && searchProfiles.length > 0 ? (
            <div className="fabric-profile-list">
              {searchProfiles.map((profile) => (
                <FabricSearchProfileCard key={profile.fabricItemId} profile={profile} showPricing={showPricing} />
              ))}
            </div>
          ) : null}
        </section>
      ) : null}

      {alertsQuery.isSuccess && alertsQuery.data.length > 0 ? (
        <>
          <section className="record-list mobile-only" aria-label="تنبيهات المخزون">
            {alertsQuery.data.map((alert) => (
              <AlertMobileCard key={alert.id} alert={alert} />
            ))}
          </section>
          <section className="card-list desktop-only" aria-label="تنبيهات المخزون">
            {alertsQuery.data.map((alert) => (
              <AlertDesktopCard key={alert.id} alert={alert} />
            ))}
          </section>
        </>
      ) : null}

      {!showRichSearch && stockQuery.isLoading ? <LoadingState /> : null}

      {!showRichSearch && stockQuery.isError ? (
        <ErrorState
          message={stockQuery.error instanceof ApiError ? stockQuery.error.message : 'حدث خطأ غير متوقع.'}
          onRetry={() => void stockQuery.refetch()}
        />
      ) : null}

      {!showRichSearch && stockQuery.isSuccess && filteredStock.length === 0 ? (
        <EmptyState
          title="لا توجد أرصدة مخزون"
          description={
            debouncedSearch.length > 0
              ? `لا توجد نتائج مطابقة لـ «${debouncedSearch}».`
              : isFiltered
                ? 'لا توجد أثواب مطابقة للمستودع/الحاوية المحددين.'
                : 'ستظهر الأقمشة هنا بعد ترحيلها إلى المستودع.'
          }
        />
      ) : null}

      {!showRichSearch && stockQuery.isSuccess && filteredStock.length > 0 ? (
        <>
          <section className="record-list mobile-only" aria-label="أرصدة الأقمشة">
            {filteredStock.map((row) => (
              <button
                key={stockRowKey(row)}
                type="button"
                className="card-link--button"
                onClick={() => setSelectedRow(row)}
              >
                <StockBalanceCard row={row} showWarehouseName={showWarehouseName} showPricing={showPricing} />
              </button>
            ))}
          </section>
          <section className="card-list desktop-only" aria-label="أرصدة الأقمشة">
            {filteredStock.map((row) => (
              <button
                key={stockRowKey(row)}
                type="button"
                className="card-link--button"
                onClick={() => setSelectedRow(row)}
              >
                <StockBalanceDataCard row={row} showPricing={showPricing} />
              </button>
            ))}
          </section>
        </>
      ) : null}

      {selectedRow ? (
        <RollDetailsModal row={selectedRow} onClose={() => setSelectedRow(null)} showPricing={showPricing} />
      ) : null}
      </div>
    </AppShell>
  );
}

function FabricSearchProfileCard({
  profile,
  showPricing
}: {
  profile: FabricSearchProfileDto;
  showPricing: boolean;
}) {
  const saleRange =
    profile.minSalePricePerMeter != null &&
    profile.maxSalePricePerMeter != null &&
    profile.minSalePricePerMeter !== profile.maxSalePricePerMeter
      ? `${formatPricePerMeter(profile.minSalePricePerMeter)} – ${formatPricePerMeter(profile.maxSalePricePerMeter)}`
      : formatPricePerMeter(profile.avgSalePricePerMeter);

  return (
    <article className="fabric-profile-card">
      <header className="fabric-profile-card__head">
        <div>
          <h3 className="fabric-profile-card__title">
            {profile.fabricName} ({profile.fabricCode})
          </h3>
          <p className="fabric-profile-card__meta">الفئة: {profile.categoryName}</p>
        </div>
        <span className="status-pill status-pill--green">{formatNumber(profile.totalRolls)} توب</span>
      </header>

      <dl className="fabric-profile-card__stats">
        <RecordField label="إجمالي الأمتار" value={formatMeters(profile.totalMeters)} />
        <RecordField label="متاح" value={formatMeters(profile.availableMeters)} />
        <RecordField label="محجوز" value={formatMeters(profile.reservedMeters)} />
        {showPricing ? (
          <RecordField label="قيمة المخزون" value={formatCurrency(profile.inventoryValue)} emphasis />
        ) : null}
        <RecordField label="سعر البيع" value={saleRange} />
        {showPricing ? (
          <RecordField label="متوسط التكلفة" value={formatPricePerMeter(profile.avgCostPerMeter)} />
        ) : null}
        <RecordField label="المستودعات" value={formatNumber(profile.warehouseCount)} />
        <RecordField label="الحاويات" value={formatNumber(profile.containerCount)} />
        <RecordField label="الألوان" value={formatNumber(profile.colorCount)} />
      </dl>

      <details className="fabric-profile-section" open>
        <summary>توزيع الألوان ({formatNumber(profile.colors.length)})</summary>
        <ul className="fabric-profile-section__list">
          {profile.colors.map((color) => (
            <li key={color.fabricColorId}>
              <strong>{color.colorName}</strong>
              <span>
                {formatNumber(color.rollCount)} توب • {formatMeters(color.totalMeters)} • متاح{' '}
                {formatMeters(color.availableMeters)} • {formatNumber(color.containerCount)} حاوية
              </span>
              <span>
                بيع {formatPricePerMeter(color.avgSalePricePerMeter)}
                {showPricing
                  ? ` • تكلفة ${formatPricePerMeter(color.avgCostPerMeter)} • ${formatCurrency(color.inventoryValue)}`
                  : ''}
              </span>
            </li>
          ))}
        </ul>
      </details>

      <details className="fabric-profile-section" open>
        <summary>المواقع التفصيلية ({formatNumber(profile.locations.length)})</summary>
        <ul className="fabric-profile-section__list">
          {profile.locations.map((loc) => (
            <li key={`${loc.warehouseId}-${loc.containerId}-${loc.fabricColorId}`}>
              <strong>
                {loc.warehouseName} — حاوية {loc.containerNumber} — {loc.colorName}
              </strong>
              <span>
                {formatNumber(loc.rollCount)} توب • {formatMeters(loc.totalMeters)} • متاح{' '}
                {formatMeters(loc.availableMeters)}
                {loc.reservedMeters > 0 ? ` • محجوز ${formatMeters(loc.reservedMeters)}` : ''}
              </span>
              <span>
                بيع {formatPricePerMeter(loc.avgSalePricePerMeter)}
                {showPricing
                  ? ` • تكلفة ${formatPricePerMeter(loc.avgCostPerMeter)} • ${formatCurrency(loc.inventoryValue)}`
                  : ''}
              </span>
            </li>
          ))}
        </ul>
      </details>

      {profile.containerJourney.length > 0 ? (
        <details className="fabric-profile-section">
          <summary>رحلة الحاويات ({formatNumber(profile.containerJourney.length)})</summary>
          <ul className="fabric-profile-section__list">
            {profile.containerJourney.map((leg) => (
              <li key={leg.containerId}>
                <strong>
                  حاوية {leg.containerNumber} — {leg.statusLabel}
                </strong>
                <span>
                  {formatNumber(leg.rollCount)} توب • {formatMeters(leg.totalMeters)} • المستودعات:{' '}
                  {leg.warehouses.length > 0 ? leg.warehouses.join('، ') : EMPTY_CELL}
                </span>
                <span>
                  المورد: {leg.supplierName ?? EMPTY_CELL} • شحن {formatDateOnly(leg.shipmentDate)} • وصول{' '}
                  {formatDateOnly(leg.arrivalDate)} • اعتماد {formatDateOnly(leg.approvedAt)}
                </span>
                <span>
                  {showPricing
                    ? `تكلفة ${formatPricePerMeter(leg.landedCostPerMeter)} • `
                    : ''}
                  بيع {formatPricePerMeter(leg.salePricePerMeter)}
                </span>
              </li>
            ))}
          </ul>
        </details>
      ) : null}

      {profile.journeyTimeline.length > 0 ? (
        <details className="fabric-profile-section">
          <summary>الخط الزمني ({formatNumber(profile.journeyTimeline.length)})</summary>
          <ol className="fabric-profile-timeline">
            {profile.journeyTimeline.map((event, index) => (
              <li key={`${event.occurredAt}-${event.title}-${index}`}>
                <time dateTime={event.occurredAt}>{formatDate(event.occurredAt)}</time>
                <div>
                  <strong>{event.title}</strong>
                  <p>{event.description}</p>
                  <span className="fabric-profile-timeline__tag">{event.category === 'China' ? 'الصين' : 'المخزون'}</span>
                </div>
              </li>
            ))}
          </ol>
        </details>
      ) : null}
    </article>
  );
}

function RollDetailsModal({
  row,
  onClose,
  showPricing
}: {
  row: FabricStockBalanceDto;
  onClose: () => void;
  showPricing: boolean;
}) {
  const rollsQuery = useQuery({
    queryKey: [
      'inventory',
      'rolls-by-stock',
      row.warehouseId,
      row.containerId,
      row.fabricItemId,
      row.fabricColorId
    ],
    queryFn: () =>
      getFabricRollsByStock({
        warehouseId: row.warehouseId,
        containerId: row.containerId,
        fabricItemId: row.fabricItemId,
        fabricColorId: row.fabricColorId
      })
  });

  const rolls = rollsQuery.data ?? [];
  const reservationsQuery = useQuery({
    queryKey: ['inventory', 'roll-sales-reservations', rolls.map((roll) => roll.id).join(',')],
    queryFn: () => getFabricRollSalesReservations(rolls.map((roll) => roll.id)),
    enabled: rolls.length > 0
  });
  const reservations = useMemo(() => {
    const map = new Map<string, FabricRollSalesReservationDto>();
    for (const reservation of reservationsQuery.data ?? []) {
      map.set(reservation.fabricRollId, reservation);
    }
    return map;
  }, [reservationsQuery.data]);
  const totalRemaining = rolls.reduce((sum, roll) => sum + roll.remainingLengthMeters, 0);
  const totalValue = rolls.reduce((sum, roll) => sum + roll.currentValue, 0);

  return (
    <Modal
      title={`${row.fabricName} — ${row.colorName}`}
      subtitle={`الحاوية ${row.containerNumber || EMPTY_CELL} • المستودع ${row.warehouseName}`}
      onClose={onClose}
    >
      {rollsQuery.isLoading ? <LoadingState /> : null}

      {rollsQuery.isError ? (
        <ErrorState
          message={rollsQuery.error instanceof ApiError ? rollsQuery.error.message : 'تعذّر تحميل تفاصيل الأتواب.'}
          onRetry={() => void rollsQuery.refetch()}
        />
      ) : null}

      {rollsQuery.isSuccess && rolls.length === 0 ? (
        <EmptyState title="لا توجد أتواب" description="لا توجد أتواب متبقية لهذا الصنف حالياً." />
      ) : null}

      {rollsQuery.isSuccess && rolls.length > 0 ? (
        <>
          <p className="roll-summary">
            عدد الأتواب: {formatNumber(rolls.length)} • إجمالي المتبقي: {formatMeters(totalRemaining)}
            {showPricing ? ` • القيمة: ${formatCurrency(totalValue)}` : ''}
          </p>
          <div className="roll-table-wrap">
            <table className="roll-table">
              <thead>
                <tr>
                  <th>رقم التوب</th>
                  <th>اللوت</th>
                  <th>الباركود</th>
                  <th>الطول الأصلي</th>
                  <th>المتبقي</th>
                  {showPricing ? (
                    <>
                      <th>التكلفة/م</th>
                      <th>القيمة $</th>
                    </>
                  ) : null}
                  <th>الموقع</th>
                  <th>الحالة</th>
                  <th>حجز بيع</th>
                </tr>
              </thead>
              <tbody>
                {rolls.map((roll) => (
                  <RollRow
                    key={roll.id}
                    roll={roll}
                    reservation={reservations.get(roll.id)}
                    showPricing={showPricing}
                  />
                ))}
              </tbody>
            </table>
          </div>
        </>
      ) : null}
    </Modal>
  );
}

function RollRow({
  roll,
  reservation,
  showPricing
}: {
  roll: FabricRollListDto;
  reservation?: FabricRollSalesReservationDto;
  showPricing: boolean;
}) {
  return (
    <tr>
      <td>{roll.rollNumber}</td>
      <td>{roll.lotCode ?? EMPTY_CELL}</td>
      <td dir="ltr">{roll.barcode ?? EMPTY_CELL}</td>
      <td>{formatMeters(roll.lengthMeters)}</td>
      <td>{formatMeters(roll.remainingLengthMeters)}</td>
      {showPricing ? (
        <>
          <td>{formatCurrency(roll.costPerMeter)}</td>
          <td>{formatCurrency(roll.currentValue)}</td>
        </>
      ) : null}
      <td>{roll.locationCode ?? EMPTY_CELL}</td>
      <td>{rollStatusLabel(roll.status)}</td>
      <td>
        {reservation ? (
          <Link className="status-pill status-pill--amber" to={`/sales/${reservation.salesInvoiceId}`}>
            مسودة بيع {reservation.salesInvoiceNumber}
          </Link>
        ) : (
          EMPTY_CELL
        )}
      </td>
    </tr>
  );
}

function stockRowKey(row: FabricStockBalanceDto) {
  return `${row.warehouseId}-${row.fabricItemId}-${row.fabricColorId}-${row.containerId}`;
}

function StockBalanceCard({
  row,
  showWarehouseName,
  showPricing
}: {
  row: FabricStockBalanceDto;
  showWarehouseName: boolean;
  showPricing: boolean;
}) {
  const isLow = row.availableMeters <= 0;

  return (
    <article className="record-card">
      <div className="record-card__head">
        <strong className="record-card__title">
          {row.fabricCode} — {row.colorName}
        </strong>
        <span className={`status-pill status-pill--${isLow ? 'amber' : 'green'}`}>
          {isLow ? inventoryStatusLabels.low : inventoryStatusLabels.available}
        </span>
      </div>
      <p className="record-card__meta">
        {row.fabricName}
        {showWarehouseName ? ` • ${row.warehouseName}` : ''}
      </p>
      <dl className="record-card__grid">
        <RecordField label="الأثواب" value={`${formatNumber(row.rollCount)} توب`} />
        <RecordField label="الأمتار" value={formatMeters(row.totalMeters)} />
        {showPricing ? (
          <RecordField label="القيمة" value={formatCurrency(row.inventoryValue)} emphasis />
        ) : null}
      </dl>
    </article>
  );
}

function StockBalanceDataCard({ row, showPricing }: { row: FabricStockBalanceDto; showPricing: boolean }) {
  const isLow = row.availableMeters <= 0;

  return (
    <DataCard
      icon={<Icon name="inventory" />}
      title={`${row.fabricCode} - ${row.colorName}`}
      subtitle={row.fabricName}
      meta={`${formatNumber(row.rollCount)} توب • ${formatMeters(row.totalMeters)} • ${
        isLow ? inventoryStatusLabels.low : inventoryStatusLabels.available
      }`}
      value={showPricing ? formatCurrency(row.inventoryValue) : formatMeters(row.totalMeters)}
      tone={isLow ? 'low' : 'available'}
    />
  );
}

function AlertMobileCard({ alert }: { alert: InventoryAlertDto }) {
  return (
    <article className="record-card record-card--alert">
      <div className="record-card__head">
        <strong className="record-card__title">{alert.title}</strong>
        <span className={`status-pill status-pill--${alert.severity === 'Critical' ? 'red' : 'amber'}`}>
          {alert.severity === 'Critical' ? 'حرج' : 'تنبيه'}
        </span>
      </div>
      <p className="record-card__meta">{alert.message}</p>
      <p className="record-card__meta">{alert.warehouseName ?? 'كل المستودعات'}</p>
    </article>
  );
}

function AlertDesktopCard({ alert }: { alert: InventoryAlertDto }) {
  return (
    <DataCard
      icon={<Icon name="alert" />}
      title={alert.title}
      subtitle={alert.message}
      meta={alert.warehouseName ?? 'كل المستودعات'}
      value=""
      tone={alert.severity === 'Critical' ? 'danger' : 'low'}
    />
  );
}
