import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { getDashboardSummary } from '../api/dashboard.ts';
import { useAuth } from '../auth/AuthContext.tsx';
import { formatNumber } from '../lib/format.ts';

export function DetailingAlertBanner() {
  const { can } = useAuth();
  const enabled = can('warehouse.detailing') || can('sales.view') || can('sales.approve');

  const summaryQuery = useQuery({
    queryKey: ['dashboard', 'summary'],
    queryFn: () => getDashboardSummary(),
    enabled,
    refetchInterval: 60_000,
    staleTime: 30_000
  });

  const count = summaryQuery.data?.awaitingDetailingCount ?? 0;
  if (!enabled || count <= 0) {
    return null;
  }

  return (
    <div className="banner banner--warn detailing-alert" role="status">
      <span>
        يوجد {formatNumber(count)} فاتورة بحاجة إلى تفنيد في المستودع.
      </span>
      {can('warehouse.detailing') ? (
        <Link className="detailing-alert__link" to="/delivery">
          فتح التسليم
        </Link>
      ) : null}
    </div>
  );
}
