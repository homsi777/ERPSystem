import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { getInventoryMovements, getInventoryWarehouses } from '../api/inventory.ts';
import { ApiError } from '../api/client.ts';
import { AppShell } from '../components/AppShell.tsx';
import { EmptyState } from '../components/EmptyState.tsx';
import { ErrorState } from '../components/ErrorState.tsx';
import { LoadingState } from '../components/LoadingState.tsx';
import { formatCurrency, formatDate, formatMeters } from '../lib/format.ts';

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

  return (
    <AppShell title="حركة المخزون">
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
        <div className="table-scroll">
          <table className="data-table">
            <thead>
              <tr>
                <th>رقم الحركة</th>
                <th>التاريخ</th>
                <th>النوع</th>
                <th>المستودع</th>
                <th>المرجع</th>
                <th>الأمتار</th>
                <th>القيمة</th>
                <th>الحالة</th>
              </tr>
            </thead>
            <tbody>
              {movementsQuery.data.map((movement) => (
                <tr key={movement.id}>
                  <td>{movement.movementNumber}</td>
                  <td>{formatDate(movement.movementDate)}</td>
                  <td>{movement.type}</td>
                  <td>{movement.warehouseName}</td>
                  <td>{movement.reference ?? '—'}</td>
                  <td>{formatMeters(movement.totalMeters)}</td>
                  <td>{formatCurrency(movement.totalValue)}</td>
                  <td>{movement.status}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : null}
    </AppShell>
  );
}
