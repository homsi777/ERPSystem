import type { ReactNode } from 'react';
import { useAuth } from '../auth/AuthContext.tsx';
import { BackButton } from './BackButton.tsx';
import { Icon } from './Icon.tsx';

type HeaderProps = {
  title: string;
  children?: ReactNode;
};

export function Header({ title, children }: HeaderProps) {
  const { user, logout } = useAuth();

  return (
    <header className="app-header">
      <div className="app-header__top">
        <div className="app-header__lead">
          <BackButton />
          <div>
            <p className="app-header__company">الأمل.AB</p>
            <h1>{title}</h1>
            {user ? <span className="app-header__user">{user.fullNameAr}</span> : null}
          </div>
        </div>
        <button className="icon-button" type="button" onClick={() => void logout()} aria-label="تسجيل الخروج">
          <Icon name="logout" />
        </button>
      </div>
      {children ? <div className="app-header__summary">{children}</div> : null}
    </header>
  );
}
