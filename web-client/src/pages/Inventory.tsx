import { useMemo, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import {
  getFabricStock,
  getInventoryAlerts,
  getInventoryDashboard,
  getInventoryWarehouses
} from '../api/inventory.ts';
import { ApiError } from '../api/client.ts';
import type { FabricStockBalanceDto, InventoryAlertDto } from '../api/types.ts';
import { AppShell } from '../components/AppShell.tsx';
import { DataCard } from '../components/DataCard.tsx';
import { EmptyState } from '../components/EmptyState.tsx';
import { ErrorState } from '../components/ErrorState.tsx';
import { Icon } from '../components/Icon.tsx';
import { LoadingState } from '../components/LoadingState.tsx';
import { RecordField } from '../components/RecordField.tsx';
import { SummaryCard } from '../components/SummaryCard.tsx';
import { formatCurrency, formatMeters, formatNumber } from '../lib/format.ts';
import { inventoryStatusLabels } from '../lib/enums.ts';

export function InventoryPage() {
  const [warehouseId, setWarehouseId] = useState('');

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

  const stockQuery = useQuery({
    queryKey: ['inventory', 'stock', warehouseId],
    queryFn: () => getFabricStock(warehouseId || undefined)
  });

  const dashboard = dashboardQuery.data;
  const showWarehouseName = warehouseId.length === 0;
  const isWarehouseFiltered = warehouseId.length > 0;

  const headerSummaryValues = useMemo(() => {
    if (stockQuery.isSuccess) {
      const rows = stockQuery.data;
      return {
        totalValue: rows.reduce((sum, row) => sum + row.inventoryValue, 0),
        totalRolls: rows.reduce((sum, row) => sum + row.rollCount, 0),
        itemCount: rows.length
      };
    }

    if (!isWarehouseFiltered && dashboard) {
      return {
        totalValue: dashboard.totalInventoryValue,
        totalRolls: dashboard.totalRolls,
        itemCount: 0
      };
    }

    return { totalValue: 0, totalRolls: 0, itemCount: 0 };
  }, [stockQuery.isSuccess, stockQuery.data, isWarehouseFiltered, dashboard]);

  const headerSummary = (
    <>
      <SummaryCard
        label={isWarehouseFiltered ? 'قيمة المستودع' : 'قيمة المخزون'}
        value={formatCurrency(headerSummaryValues.totalValue)}
      />
      <SummaryCard
        label={isWarehouseFiltered ? 'أثواب المستودع' : 'عدد الأثواب'}
        value={formatNumber(headerSummaryValues.totalRolls)}
        tone="green"
      />
      <SummaryCard
        label={isWarehouseFiltered ? 'أصناف المستودع' : 'الأصناف'}
        value={formatNumber(headerSummaryValues.itemCount)}
        tone="amber"
      />
    </>
  );

  return (
    <AppShell title="المخزون" summary={headerSummary}>
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

      <section className="toolbar-row toolbar-row--start toolbar-panel">
        <label className="inline-field inline-field--grow">
          المستودع
          <select
            value={warehouseId}
            onChange={(event) => setWarehouseId(event.target.value)}
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
        <Link className="ghost-button toolbar-panel__action" to="/inventory/movements">
          حركة المخزون
        </Link>
      </section>

      {stockQuery.isLoading ? <LoadingState /> : null}

      {stockQuery.isError ? (
        <ErrorState
          message={stockQuery.error instanceof ApiError ? stockQuery.error.message : 'حدث خطأ غير متوقع.'}
          onRetry={() => void stockQuery.refetch()}
        />
      ) : null}

      {stockQuery.isSuccess && stockQuery.data.length === 0 ? (
        <EmptyState
          title="لا توجد أرصدة مخزون"
          description={
            isWarehouseFiltered
              ? 'لا توجد أثواب في هذا المستودع حاليًا.'
              : 'ستظهر الأقمشة هنا بعد ترحيلها إلى المستودع.'
          }
        />
      ) : null}

      {stockQuery.isSuccess && stockQuery.data.length > 0 ? (
        <>
          <section className="record-list mobile-only" aria-label="أرصدة الأقمشة">
            {stockQuery.data.map((row) => (
              <StockBalanceCard key={stockRowKey(row)} row={row} showWarehouseName={showWarehouseName} />
            ))}
          </section>
          <section className="card-list desktop-only" aria-label="أرصدة الأقمشة">
            {stockQuery.data.map((row) => (
              <StockBalanceDataCard key={stockRowKey(row)} row={row} />
            ))}
          </section>
        </>
      ) : null}
    </AppShell>
  );
}

function stockRowKey(row: FabricStockBalanceDto) {
  return `${row.warehouseId}-${row.fabricItemId}-${row.fabricColorId}-${row.containerId}`;
}

function StockBalanceCard({
  row,
  showWarehouseName
}: {
  row: FabricStockBalanceDto;
  showWarehouseName: boolean;
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
        <RecordField label="القيمة" value={formatCurrency(row.inventoryValue)} emphasis />
      </dl>
    </article>
  );
}

function StockBalanceDataCard({ row }: { row: FabricStockBalanceDto }) {
  const isLow = row.availableMeters <= 0;

  return (
    <DataCard
      icon={<Icon name="inventory" />}
      title={`${row.fabricCode} - ${row.colorName}`}
      subtitle={row.fabricName}
      meta={`${formatNumber(row.rollCount)} توب • ${formatMeters(row.totalMeters)} • ${
        isLow ? inventoryStatusLabels.low : inventoryStatusLabels.available
      }`}
      value={formatCurrency(row.inventoryValue)}
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
