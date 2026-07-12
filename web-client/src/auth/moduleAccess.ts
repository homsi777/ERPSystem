/**
 * Predefined modules for web navigation — must stay aligned with PermissionModuleCatalog (backend).
 */
import { isGeneralManager } from './generalManagerAccess.ts';

export type WebModuleDef = {
  route: string;
  label: string;
  icon: 'home' | 'inventory' | 'sales' | 'customers' | 'expenses' | 'accounting' | 'china' | 'delivery';
  modules: string[];
  alwaysVisible?: boolean;
  generalManagerOnly?: boolean;
};

export const WEB_MODULES: WebModuleDef[] = [
  { route: '/home', label: 'رئيسية', icon: 'home', modules: [], alwaysVisible: true },
  { route: '/inventory', label: 'المخزون', icon: 'inventory', modules: ['warehouse'] },
  { route: '/sales', label: 'المبيعات', icon: 'sales', modules: ['sales'] },
  { route: '/customers', label: 'العملاء', icon: 'customers', modules: ['customers'] },
  { route: '/expenses', label: 'المصاريف', icon: 'expenses', modules: ['expenses'] },
  {
    route: '/accounting',
    label: 'المحاسبة',
    icon: 'accounting',
    modules: ['accounting', 'finance', 'openingbalances']
  },
  { route: '/china', label: 'الصين', icon: 'china', modules: [], generalManagerOnly: true },
  { route: '/delivery', label: 'التسليم', icon: 'delivery', modules: ['warehouse', 'sales'] }
];

export function hasModuleAccess(permissions: readonly string[], moduleKey: string): boolean {
  const prefix = `${moduleKey}.`;
  return permissions.some((code) => code.startsWith(prefix));
}

export function canAccessWebModule(permissions: readonly string[], module: WebModuleDef): boolean {
  if (module.generalManagerOnly && !isGeneralManager(permissions)) {
    return false;
  }

  if (module.alwaysVisible) {
    return true;
  }

  if (permissions.length === 0) {
    return false;
  }

  if (module.modules.length === 0) {
    return Boolean(module.generalManagerOnly && isGeneralManager(permissions));
  }

  return module.modules.some((moduleKey) => hasModuleAccess(permissions, moduleKey));
}

export function visibleWebModules(permissions: readonly string[]): WebModuleDef[] {
  return WEB_MODULES.filter((module) => canAccessWebModule(permissions, module));
}

export function isChinaRoute(pathname: string): boolean {
  return pathname === '/china' || pathname.startsWith('/china/');
}
