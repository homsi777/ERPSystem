import { useMemo } from 'react';
import { useQuery } from '@tanstack/react-query';
import { getFabricStock } from '../api/inventory.ts';
import { ApiError } from '../api/client.ts';
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
  const stockQuery = useQuery({
    queryKey: ['inventory', 'stock'],
    queryFn: () => getFabricStock()
  });

  const summary = useMemo(() => {
    const rows = stockQuery.data ?? [];
    // Display-only sums for the field PWA. Business summary totals should later come from a dedicated API endpoint.
    return {
      totalValue: rows.reduce((sum, row) => sum + row.inventoryValue, 0),
      totalRolls: rows.reduce((sum, row) => sum + row.rollCount, 0),
      itemCount: rows.length
    };
  }, [stockQuery.data]);

  const headerSummary = (
    <>
      <SummaryCard label="قيمة المخزون" value={formatCurrency(summary.totalValue)} />
      <SummaryCard label="عدد الأتواب" value={formatNumber(summary.totalRolls)} tone="green" />
      <SummaryCard label="الأصناف" value={formatNumber(summary.itemCount)} tone="amber" />
    </>
  );

  return (
    <AppShell title="المخزون" summary={headerSummary}>
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
