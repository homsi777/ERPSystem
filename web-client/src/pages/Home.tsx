import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { getDashboardSummary } from '../api/dashboard.ts';
import { ApiError } from '../api/client.ts';
import { useAuth } from '../auth/AuthContext.tsx';
import { AppShell } from '../components/AppShell.tsx';
import { ErrorState } from '../components/ErrorState.tsx';
import { Icon } from '../components/Icon.tsx';
import { LoadingState } from '../components/LoadingState.tsx';
import { SummaryCard } from '../components/SummaryCard.tsx';
import { formatDate, formatNumber } from '../lib/format.ts';

export function HomePage() {
  const { user } = useAuth();

  const summaryQuery = useQuery({
    queryKey: ['dashboard', 'summary'],
    queryFn: () => getDashboardSummary()
  });

  const summary = summaryQuery.data;

  const headerSummary = summary ? (
    <>
      <SummaryCard label="فواتير بانتظار تفصيل" value={formatNumber(summary.awaitingDetailingCount)} tone="amber" />
      <SummaryCard label="حاويات صين قيد المراجعة" value={formatNumber(summary.pendingContainersCount)} />
      <SummaryCard label="عملاء نشطون" value={formatNumber(summary.activeCustomersCount)} tone="green" />
      <SummaryCard label="تنبيهات مخزون منخفض" value={formatNumber(summary.lowStockItemsCount)} tone="amber" />
    </>
  ) : undefined;

  return (
    <AppShell title="الرئيسية" summary={headerSummary}>
      <p className="muted-line home-greeting">
        {user ? `مرحبًا ${user.fullNameAr}، إليك نظرة سريعة على عملك اليوم.` : 'مرحبًا بك.'}
      </p>

      {summaryQuery.isLoading ? <LoadingState /> : null}

      {summaryQuery.isError ? (
        <ErrorState
          message={summaryQuery.error instanceof ApiError ? summaryQuery.error.message : 'حدث خطأ غير متوقع.'}
          onRetry={() => void summaryQuery.refetch()}
        />
      ) : null}

      <section className="shortcut-grid" aria-label="اختصارات سريعة">
        <Link className="shortcut-card" to="/inventory">
          <Icon name="inventory" />
          <span>المخزون</span>
        </Link>
        <Link className="shortcut-card" to="/customers">
          <Icon name="customers" />
          <span>العملاء</span>
        </Link>
        <Link className="shortcut-card" to="/china">
          <Icon name="china" />
          <span>طلبات الصين</span>
        </Link>
        <Link className="shortcut-card" to="/delivery">
          <Icon name="delivery" />
          <span>التسليم</span>
        </Link>
      </section>

      {summary && summary.recentActivity.length > 0 ? (
        <section className="detail-card">
          <h2>آخر الأنشطة</h2>
          <div className="line-list">
            {summary.recentActivity.map((activity, index) => (
              <article className="line-card" key={index}>
                <div className="line-card__head">
                  <h3>{activity.description}</h3>
                </div>
                <p className="muted-line">{formatDate(activity.occurredAt)}</p>
              </article>
            ))}
          </div>
        </section>
      ) : null}
    </AppShell>
  );
}
