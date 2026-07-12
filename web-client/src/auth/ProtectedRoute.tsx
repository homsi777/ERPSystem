import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from './AuthContext.tsx';
import { canAccessWebModule, WEB_MODULES } from './moduleAccess.ts';

export function ProtectedRoute() {
  const { isAuthenticated, user } = useAuth();
  const location = useLocation();
  const permissions = user?.permissions ?? [];

  if (!isAuthenticated) {
    return <Navigate to="/login" replace state={{ from: location.pathname }} />;
  }

  const module = WEB_MODULES.find((entry) =>
    location.pathname === entry.route || location.pathname.startsWith(`${entry.route}/`));

  if (module && !canAccessWebModule(permissions, module)) {
    return <Navigate to="/home" replace />;
  }

  return <Outlet />;
}
