import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { getInventoryMovements, getInventoryWarehouses } from '../api/inventory.ts';
import { ApiError } from '../api/client.ts';
import type { StockMovementListDto } from '../api/types.ts';
import { AppShell } from '../components/AppShell.tsx';
import { EmptyState } from '../components/EmptyState.tsx';
import { ErrorState } from '../components/ErrorState.tsx';
import { LoadingState } from '../components/LoadingState.tsx';
import { SummaryCard } from '../components/SummaryCard.tsx';
import { formatCurrency, formatDate, formatMeters, formatNumber } from '../lib/format.ts';
import { documentTypeName, movementTypeLabel, stockMovementStatusLabel } from '../lib/enums.ts';

export function InventoryMovementsPage() {
  const [warehouseId, setWarehouseId] = useState('');

  const warehousesQuery = useQuery({
    queryKey: ['inventory', 'warehouses'],
    queryFn: () => getInventoryWarehouses()
  });

  const movementsQuery = useQuery({
    queryKey: ['inventory', 'movements', warehouseId],
    queryFn: () => getInventoryMovements(warehouseId || undefined)
  });

  const headerSummary = (
    <SummaryCard label="عدد الحركات" value={formatNumber(movementsQuery.data?.length ?? 0)} tone="amber" />
  );

  return (
    <AppShell title="حركة المخزون" summary={headerSummary}>
      <div className="page-stack">
        <section className="form-panel form-compact form-panel--filter">
          <label className="form-field form-field--wide">
            <span className="form-field__label">المستودع</span>
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
        </section>

        {movementsQuery.isLoading ? <LoadingState /> : null}

        {movementsQuery.isError ? (
          <ErrorState
            message={movementsQuery.error instanceof ApiError ? movementsQuery.error.message : 'حدث خطأ غير متوقع.'}
            onRetry={() => void movementsQuery.refetch()}
          />
        ) : null}

        {movementsQuery.isSuccess && movementsQuery.data.length === 0 ? (
          <EmptyState title="لا توجد حركات" description="لم تُسجَّل أي حركة مخزون بعد." />
        ) : null}

        {movementsQuery.isSuccess && movementsQuery.data.length > 0 ? (
          <section className="card-list" aria-label="حركات المخزون">
            {movementsQuery.data.map((movement) => (
              <MovementCard key={movement.id} movement={movement} />
            ))}
          </section>
        ) : null}
      </div>
    </AppShell>
  );
}

function MovementCard({ movement }: { movement: StockMovementListDto }) {
  return (
    <article className="form-panel form-compact">
      <div className="compact-hero">
        <div>
          <p className="compact-hero__eyebrow">{formatDate(movement.movementDate)}</p>
          <h2>{movement.movementNumber}</h2>
        </div>
        <span className="status-pill status-pill--blue">{stockMovementStatusLabel(movement.status)}</span>
      </div>
      <dl className="detail-grid">
        <div>
          <dt>النوع</dt>
          <dd>{movementTypeLabel(movement.type)}</dd>
        </div>
        <div>
          <dt>المستودع</dt>
          <dd>{movement.warehouseName}</dd>
        </div>
        <div>
          <dt>المرجع</dt>
          <dd>{documentTypeName(movement.reference)}</dd>
        </div>
        <div>
          <dt>الأمتار</dt>
          <dd>{formatMeters(movement.totalMeters)}</dd>
        </div>
        <div>
          <dt>القيمة</dt>
          <dd>{formatCurrency(movement.totalValue)}</dd>
        </div>
      </dl>
    </article>
  );
}
