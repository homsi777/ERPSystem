import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { LoadingState } from '../components/LoadingState.tsx';
import { useAuth } from './AuthContext.tsx';

export function ProtectedRoute() {
  const { isAuthenticated, isBootstrapping } = useAuth();
  const location = useLocation();

  if (isBootstrapping) {
    return <LoadingState label="جاري تجهيز الجلسة..." fullScreen />;
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace state={{ from: location.pathname }} />;
  }

  return <Outlet />;
}
