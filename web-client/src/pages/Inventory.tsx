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
import type { InventoryAlertDto } from '../api/types.ts';
import { AppShell } from '../components/AppShell.tsx';
import { DataCard } from '../components/DataCard.tsx';
import { EmptyState } from '../components/EmptyState.tsx';
import { ErrorState } from '../components/ErrorState.tsx';
import { Icon } from '../components/Icon.tsx';
import { LoadingState } from '../components/LoadingState.tsx';
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

  const itemCount = useMemo(() => {
    // عرض فقط: عدد بنود المخزون (صنف/لون/مستودع) ضمن الصفحة الحالية.
    // لوحة GET /inventory/dashboard لا تعيد رقمًا مكافئًا لهذا العدد (فقط قيمة/أثواب/أمتار إجمالية)،
    // لذلك بقي هذا الرقم محسوبًا في الواجهة من نتيجة GET /inventory/stock نفسها.
    return (stockQuery.data ?? []).length;
  }, [stockQuery.data]);

  const dashboard = dashboardQuery.data;

  const headerSummary = (
    <>
      <SummaryCard
        label="قيمة المخزون"
        value={formatCurrency(dashboard?.totalInventoryValue ?? 0)}
      />
      <SummaryCard label="عدد الأثواب" value={formatNumber(dashboard?.totalRolls ?? 0)} tone="green" />
      <SummaryCard label="الأصناف" value={formatNumber(itemCount)} tone="amber" />
    </>
  );

  return (
    <AppShell title="المخزون" summary={headerSummary}>
      {alertsQuery.isSuccess && alertsQuery.data.length > 0 ? (
        <section className="card-list" aria-label="تنبيهات المخزون">
          {alertsQuery.data.map((alert) => (
            <AlertCard key={alert.id} alert={alert} />
          ))}
        </section>
      ) : null}

      <section className="toolbar-row toolbar-row--start">
        <label className="inline-field">
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
        <Link className="ghost-button" to="/inventory/movements">حركة المخزون</Link>
      </section>

      {stockQuery.isLoading ? <LoadingState /> : null}

      {stockQuery.isError ? (
        <ErrorState
          message={stockQuery.error instanceof ApiError ? stockQuery.error.message : 'حدث خطأ غير متوقع.'}
          onRetry={() => void stockQuery.refetch()}
        />
      ) : null}

      {stockQuery.isSuccess && stockQuery.data.length === 0 ? (
        <EmptyState title="لا توجد أرصدة مخزون" description="ستظهر الأقمشة هنا بعد ترحيلها إلى المستودع." />
      ) : null}

      {stockQuery.isSuccess && stockQuery.data.length > 0 ? (
        <section className="card-list" aria-label="أرصدة الأقمشة">
          {stockQuery.data.map((row) => {
            const isLow = row.availableMeters <= 0;
            return (
              <DataCard
                key={`${row.warehouseId}-${row.fabricItemId}-${row.fabricColorId}-${row.containerId}`}
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
          })}
        </section>
      ) : null}
    </AppShell>
  );
}

function AlertCard({ alert }: { alert: InventoryAlertDto }) {
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
