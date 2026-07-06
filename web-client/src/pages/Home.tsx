import { AppShell } from '../components/AppShell.tsx';
import { ComingSoon } from '../components/ComingSoon.tsx';

export function HomePage() {
  return (
    <AppShell title="الرئيسية">
      <ComingSoon title="الرئيسية" />
    </AppShell>
  );
}
