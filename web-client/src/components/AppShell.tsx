import type { ReactNode } from 'react';
import { BottomNav } from './BottomNav.tsx';
import { Header } from './Header.tsx';

type AppShellProps = {
  title: string;
  summary?: ReactNode;
  children: ReactNode;
};

export function AppShell({ title, summary, children }: AppShellProps) {
  return (
    <div className="app-shell">
      <Header title={title}>{summary}</Header>
      <main className="app-main">{children}</main>
      <BottomNav />
    </div>
  );
}
