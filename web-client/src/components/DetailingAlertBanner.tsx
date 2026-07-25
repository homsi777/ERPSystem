import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { getDashboardSummary } from '../api/dashboard.ts';
import { getDetailingQueue } from '../api/detailing.ts';
import { useAuth } from '../auth/AuthContext.tsx';
import { formatNumber } from '../lib/format.ts';

export function DetailingAlertBanner() {
  const { can } = useAuth();
  const canOpenDetailing = can('warehouse.detailing');
  const enabled = canOpenDetailing || can('sales.approve') || can('sales.send-to-warehouse');

  const summaryQuery = useQuery({
    queryKey: ['dashboard', 'summary'],
    queryFn: () => getDashboardSummary(),
    enabled,
    refetchInterval: 60_000,
    staleTime: 30_000
  });

  const queueQuery = useQuery({
    queryKey: ['detailing', 'queue', 'branch'],
    queryFn: () => getDetailingQueue(),
    enabled: canOpenDetailing,
    refetchInterval: 60_000,
    staleTime: 30_000
  });

  const firstInvoice = queueQuery.data?.[0];
  const firstInvoiceContainers = firstInvoice
    ? Array.from(
        new Set(
          firstInvoice.rolls
            .map((roll) => roll.containerDisplay?.trim())
            .filter((value): value is string => Boolean(value && value !== '—'))
        )
      ).join('، ')
    : '';
  const count = canOpenDetailing
    ? queueQuery.data?.length ?? summaryQuery.data?.awaitingDetailingCount ?? 0
    : summaryQuery.data?.awaitingDetailingCount ?? 0;
  if (!enabled || count <= 0) {
    return null;
  }

  return (
    <div className="banner banner--warn detailing-alert" role="status">
      <span>
        {firstInvoice
          ? `الفاتورة ${firstInvoice.invoiceNumber} للعميل ${firstInvoice.customerName || 'غير محدد'} من الحاوية ${firstInvoiceContainers || 'غير محددة'} بحاجة إلى تفنيد ${formatNumber(firstInvoice.rolls.length)} ثوب${count > 1 ? `، ويوجد ${formatNumber(count - 1)} فاتورة أخرى` : ''}.`
          : `يوجد ${formatNumber(count)} فاتورة بحاجة إلى تفنيد في المستودع.`}
      </span>
      {canOpenDetailing ? (
        <Link
          className="detailing-alert__link"
          to={firstInvoice ? `/delivery/${firstInvoice.invoiceId}` : '/delivery'}
        >
          فتح التسليم
        </Link>
      ) : null}
    </div>
  );
}
