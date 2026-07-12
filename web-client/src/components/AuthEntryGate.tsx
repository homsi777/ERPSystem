import type { ReactNode } from 'react';
import { useAuth } from '../auth/AuthContext.tsx';
import { LoadingState } from './LoadingState.tsx';
import { LoginSecuritySplash } from './LoginSecuritySplash.tsx';

type AuthEntryGateProps = {
  children: ReactNode;
};

export function AuthEntryGate({ children }: AuthEntryGateProps) {
  const { isBootstrapping, entrySplashPending, completeEntrySplash } = useAuth();

  if (isBootstrapping) {
    return <LoadingState label="جاري تجهيز الجلسة..." fullScreen />;
  }

  if (entrySplashPending) {
    return <LoginSecuritySplash onComplete={completeEntrySplash} />;
  }

  return children;
}
