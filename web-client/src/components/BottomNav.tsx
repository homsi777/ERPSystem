import { NavLink } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { getDashboardSummary } from '../api/dashboard.ts';
import { useAuth } from '../auth/AuthContext.tsx';
import { Icon } from './Icon.tsx';

const tabs = [
  { to: '/home', label: 'رئيسية', icon: 'home' },
  { to: '/inventory', label: 'المخزون', icon: 'inventory' },
  { to: '/sales', label: 'المبيعات', icon: 'sales' },
  { to: '/customers', label: 'العملاء', icon: 'customers' },
  { to: '/expenses', label: 'المصاريف', icon: 'expenses' },
  { to: '/accounting', label: 'المحاسبة', icon: 'accounting' },
  { to: '/china', label: 'الصين', icon: 'china' },
  { to: '/delivery', label: 'التسليم', icon: 'delivery' }
] as const;

export function BottomNav() {
  const { can } = useAuth();
  const summaryQuery = useQuery({
    queryKey: ['dashboard', 'summary'],
    queryFn: () => getDashboardSummary(),
    enabled: can('warehouse.detailing') || can('sales.view'),
    refetchInterval: 60_000,
    staleTime: 30_000
  });

  const awaitingCount = summaryQuery.data?.awaitingDetailingCount ?? 0;

  return (
    <nav className="bottom-nav" aria-label="التنقل الرئيسي">
      {tabs.map((tab) => (
        <NavLink key={tab.to} to={tab.to} className="bottom-nav__item">
          <span className="bottom-nav__icon-wrap">
            <Icon name={tab.icon} />
            {tab.to === '/delivery' && awaitingCount > 0 ? (
              <span className="nav-badge" aria-label={`${awaitingCount} بانتظار التفنيد`}>
                {awaitingCount > 99 ? '99+' : awaitingCount}
              </span>
            ) : null}
          </span>
          <span>{tab.label}</span>
        </NavLink>
      ))}
    </nav>
  );
}
