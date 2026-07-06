import type { ReactElement } from 'react';

type IconName = 'home' | 'inventory' | 'customers' | 'china' | 'delivery' | 'logout' | 'box' | 'chart' | 'alert' | 'back';

type IconProps = {
  name: IconName;
  className?: string;
};

export function Icon({ name, className }: IconProps) {
  return (
    <svg className={className} viewBox="0 0 24 24" aria-hidden="true" focusable="false">
      {paths[name]}
    </svg>
  );
}

const paths: Record<IconName, ReactElement> = {
  home: (
    <>
      <path d="M4 10.8 12 4l8 6.8v8.4a1.8 1.8 0 0 1-1.8 1.8h-3.7v-6.2h-5V21H5.8A1.8 1.8 0 0 1 4 19.2z" />
    </>
  ),
  inventory: (
    <>
      <path d="M4 7.2 12 3l8 4.2-8 4.2z" />
      <path d="M4 9.6 12 14l8-4.4v7.2L12 21l-8-4.2z" />
    </>
  ),
  customers: (
    <>
      <path d="M9 11.5a3.5 3.5 0 1 0 0-7 3.5 3.5 0 0 0 0 7Z" />
      <path d="M3.5 20.5c.6-3.7 2.5-5.7 5.5-5.7s4.9 2 5.5 5.7z" />
      <path d="M16.5 11a2.7 2.7 0 1 0 0-5.4 2.7 2.7 0 0 0 0 5.4Z" />
      <path d="M15.2 14.4c2.8.2 4.5 2 5.1 5.3h-3.8" />
    </>
  ),
  china: (
    <>
      <path d="M4 6h16v8H4z" />
      <path d="M6 14v4h12v-4" />
      <path d="M7 9h2m3 0h2m3 0h1" />
    </>
  ),
  delivery: (
    <>
      <path d="M3 6h11v10H3z" />
      <path d="M14 10h3.8l3.2 3.4V16h-7z" />
      <path d="M7 19a2 2 0 1 0 0-4 2 2 0 0 0 0 4Zm10 0a2 2 0 1 0 0-4 2 2 0 0 0 0 4Z" />
    </>
  ),
  logout: (
    <>
      <path d="M11 4H5v16h6" />
      <path d="M14 8l4 4-4 4" />
      <path d="M8 12h10" />
    </>
  ),
  box: <path d="M4 7.5 12 3l8 4.5v9L12 21l-8-4.5zm8 4.5 8-4.5M12 12 4 7.5m8 4.5V21" />,
  chart: <path d="M5 19V5m0 14h14M9 16v-5m4 5V8m4 8v-7" />,
  alert: <path d="M12 4 3.5 19h17zM12 9v4m0 3h.01" />,
  back: <path d="M10 6 8.59 7.41 13.17 12l-4.58 4.59L10 18l6-6z" />
};
