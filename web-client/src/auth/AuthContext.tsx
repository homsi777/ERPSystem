import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode
} from 'react';
import { useNavigate } from 'react-router-dom';
import { setAuthController } from '../api/client.ts';
import type { AuthenticatedUserDto, LoginRequest } from '../api/types.ts';
import { getMeRequest, loginRequest, logoutRequest, refreshRequest } from './authApi.ts';
import {
  clearStoredAuth,
  getAccessToken,
  getRefreshToken,
  getStoredUser,
  setAccessToken,
  setRefreshToken,
  setStoredUser
} from './tokenStorage.ts';

type AuthContextValue = {
  user: AuthenticatedUserDto | null;
  isAuthenticated: boolean;
  isBootstrapping: boolean;
  entrySplashPending: boolean;
  login: (request: LoginRequest, redirectTo?: string) => Promise<void>;
  logout: () => Promise<void>;
  completeEntrySplash: () => void;
  can: (permission: string) => boolean;
};

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const navigate = useNavigate();
  const [user, setUser] = useState<AuthenticatedUserDto | null>(() => getStoredUser());
  const [isBootstrapping, setIsBootstrapping] = useState(true);
  const [entrySplashPending, setEntrySplashPending] = useState(false);
  const [postSplashRedirect, setPostSplashRedirect] = useState<string | null>(null);

  const clearAuth = useCallback(() => {
    clearStoredAuth();
    setUser(null);
  }, []);

  const refreshAccessToken = useCallback(async () => {
    const refreshToken = getRefreshToken();
    if (!refreshToken) {
      return null;
    }

    try {
      const response = await refreshRequest(refreshToken);
      setAccessToken(response.accessToken);
      return response.accessToken;
    } catch {
      clearAuth();
      return null;
    }
  }, [clearAuth]);

  useEffect(() => {
    setAuthController({
      getAccessToken,
      refreshAccessToken,
      clearAuth
    });
  }, [clearAuth, refreshAccessToken]);

  useEffect(() => {
    let active = true;

    async function bootstrapSession() {
      const refreshToken = getRefreshToken();
      if (!refreshToken) {
        if (active) {
          setIsBootstrapping(false);
        }
        return;
      }

      const token = await refreshAccessToken();
      if (!token) {
        if (active) {
          setIsBootstrapping(false);
        }
        return;
      }

      try {
        const currentUser = await getMeRequest();
        if (active) {
          setStoredUser(currentUser);
          setUser(currentUser);
        }
      } catch {
        clearAuth();
      } finally {
        if (active) {
          setIsBootstrapping(false);
        }
      }
    }

    void bootstrapSession();
    return () => {
      active = false;
    };
  }, [clearAuth, refreshAccessToken]);

  useEffect(() => {
    function handleExpired() {
      clearAuth();
      navigate('/login', { replace: true });
    }

    window.addEventListener('erp-auth-expired', handleExpired);
    return () => window.removeEventListener('erp-auth-expired', handleExpired);
  }, [clearAuth, navigate]);

  const login = useCallback(async (request: LoginRequest, redirectTo = '/home') => {
    const response = await loginRequest(request);
    setAccessToken(response.accessToken);
    setRefreshToken(response.refreshToken);
    setStoredUser(response.user);
    setUser(response.user);
    setPostSplashRedirect(redirectTo);
    setEntrySplashPending(true);
  }, []);

  const completeEntrySplash = useCallback(() => {
    setEntrySplashPending(false);
    if (postSplashRedirect) {
      navigate(postSplashRedirect, { replace: true });
      setPostSplashRedirect(null);
    }
  }, [navigate, postSplashRedirect]);

  const logout = useCallback(async () => {
    const refreshToken = getRefreshToken();
    clearAuth();
    setEntrySplashPending(false);
    setPostSplashRedirect(null);
    if (refreshToken) {
      try {
        await logoutRequest(refreshToken);
      } catch {
        // Local logout must still complete if the API is offline.
      }
    }
    navigate('/login', { replace: true });
  }, [clearAuth, navigate]);

  const can = useCallback(
    (permission: string) => user?.permissions.includes(permission) ?? false,
    [user]
  );

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      isAuthenticated: Boolean(user),
      isBootstrapping,
      entrySplashPending,
      login,
      logout,
      completeEntrySplash,
      can
    }),
    [can, completeEntrySplash, entrySplashPending, isBootstrapping, login, logout, user]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used inside AuthProvider.');
  }
  return context;
}
