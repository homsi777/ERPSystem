import { NavLink } from 'react-router-dom';
import { Icon } from './Icon.tsx';

const tabs = [
  { to: '/home', label: 'رئيسية', icon: 'home' },
  { to: '/inventory', label: 'المخزون', icon: 'inventory' },
  { to: '/customers', label: 'العملاء', icon: 'customers' },
  { to: '/china', label: 'طلبات الصين', icon: 'china' },
  { to: '/delivery', label: 'التسليم', icon: 'delivery' }
] as const;

export function BottomNav() {
  return (
    <nav className="bottom-nav" aria-label="التنقل الرئيسي">
      {tabs.map((tab) => (
        <NavLink key={tab.to} to={tab.to} className="bottom-nav__item">
          <Icon name={tab.icon} />
          <span>{tab.label}</span>
        </NavLink>
      ))}
    </nav>
  );
}
