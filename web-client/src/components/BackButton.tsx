import { useEffect, useRef, useState } from 'react';
import { useLocation, useNavigate, useNavigationType } from 'react-router-dom';
import { Icon } from './Icon.tsx';

const ROOT_PATHS = new Set(['/home', '/inventory', '/customers', '/china', '/delivery']);

function parentPath(pathname: string) {
  const segments = pathname.split('/').filter(Boolean);
  if (segments.length <= 1) {
    return '/home';
  }
  return `/${segments.slice(0, -1).join('/')}`;
}

function useCanGoBack() {
  const location = useLocation();
  const navigationType = useNavigationType();
  const depthRef = useRef(0);
  const [canGoBack, setCanGoBack] = useState(false);

  useEffect(() => {
    if (navigationType === 'PUSH') {
      depthRef.current += 1;
    } else if (navigationType === 'POP') {
      depthRef.current = Math.max(0, depthRef.current - 1);
    }
    setCanGoBack(depthRef.current > 0);
  }, [location.key, navigationType]);

  return canGoBack;
}

export function BackButton() {
  const location = useLocation();
  const navigate = useNavigate();
  const canGoBack = useCanGoBack();

  if (ROOT_PATHS.has(location.pathname)) {
    return null;
  }

  function handleBack() {
    if (canGoBack) {
      navigate(-1);
    } else {
      navigate(parentPath(location.pathname), { replace: true });
    }
  }

  return (
    <button className="icon-button back-button" type="button" onClick={handleBack} aria-label="رجوع">
      <Icon name="back" />
    </button>
  );
}
