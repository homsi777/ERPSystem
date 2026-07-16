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
import { visibleWebModules } from '../auth/moduleAccess.ts';
import { formatCurrency, formatDate, formatNumber } from '../lib/format.ts';

export function HomePage() {
  const { user } = useAuth();
  const shortcuts = visibleWebModules(user?.permissions ?? []).filter((module) => module.route !== '/home');

  const summaryQuery = useQuery({
    queryKey: ['dashboard', 'summary'],
    queryFn: () => getDashboardSummary()
  });

  const summary = summaryQuery.data;

  const headerSummary = summary ? (
    <>
      <SummaryCard label="مبيعات اليوم" value={formatCurrency(summary.todaySalesTotal)} tone="green" />
      <SummaryCard label="إجمالي الذمة" value={formatCurrency(summary.totalCustomerOutstanding)} tone="amber" />
      <SummaryCard label="قبض الموردين" value={formatCurrency(summary.totalSupplierPayables)} />
      <SummaryCard label="عدد العملاء" value={formatNumber(summary.activeCustomersCount)} tone="green" />
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
        {shortcuts.map((module) => (
          <Link className="shortcut-card" key={module.route} to={module.route}>
            <Icon name={module.icon} />
            <span>{module.label}</span>
            {module.route === '/delivery' && summary && summary.awaitingDetailingCount > 0 ? (
              <em className="shortcut-card__badge">{formatNumber(summary.awaitingDetailingCount)} بانتظار التفنيد</em>
            ) : null}
          </Link>
        ))}
      </section>

      {summary && summary.recentActivity.length > 0 ? (
        <section className="form-panel form-compact">
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
