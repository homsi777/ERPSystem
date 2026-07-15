import { NavLink } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { getDashboardSummary } from '../api/dashboard.ts';
import { useAuth } from '../auth/AuthContext.tsx';
import { visibleWebModules } from '../auth/moduleAccess.ts';
import { formatInteger } from '../lib/format.ts';
import { Icon } from './Icon.tsx';

export function BottomNav() {
  const { can, user } = useAuth();
  const permissions = user?.permissions ?? [];
  const tabs = visibleWebModules(permissions);

  const summaryQuery = useQuery({
    queryKey: ['dashboard', 'summary'],
    queryFn: () => getDashboardSummary(),
    enabled: can('warehouse.detailing') || can('sales.create'),
    refetchInterval: 60_000,
    staleTime: 30_000
  });

  const awaitingCount = summaryQuery.data?.awaitingDetailingCount ?? 0;

  return (
    <nav className="bottom-nav" aria-label="التنقل الرئيسي">
      {tabs.map((tab) => (
        <NavLink key={tab.route} to={tab.route} className="bottom-nav__item">
          <span className="bottom-nav__icon-wrap">
            <Icon name={tab.icon} />
            {tab.route === '/delivery' && awaitingCount > 0 ? (
              <span className="nav-badge" aria-label={`${formatInteger(awaitingCount)} بانتظار التفنيد`}>
                {awaitingCount > 99 ? `${formatInteger(99)}+` : formatInteger(awaitingCount)}
              </span>
            ) : null}
          </span>
          <span>{tab.label}</span>
        </NavLink>
      ))}
    </nav>
  );
}
