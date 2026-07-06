import type { ReactNode } from 'react';

type DataCardProps = {
  icon: ReactNode;
  title: string;
  subtitle: string;
  meta: string;
  value: string;
  tone?: 'available' | 'low' | 'neutral';
};

export function DataCard({ icon, title, subtitle, meta, value, tone = 'neutral' }: DataCardProps) {
  return (
    <article className={`data-card data-card--${tone}`}>
      <div className="data-card__icon">{icon}</div>
      <div className="data-card__content">
        <h2>{title}</h2>
        <p>{subtitle}</p>
        <span>{meta}</span>
      </div>
      <strong className="data-card__value">{value}</strong>
    </article>
  );
}
