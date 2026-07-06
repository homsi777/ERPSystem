import { Icon } from './Icon.tsx';

type EmptyStateProps = {
  title: string;
  description?: string;
};

export function EmptyState({ title, description }: EmptyStateProps) {
  return (
    <section className="state-card">
      <Icon name="box" />
      <h2>{title}</h2>
      {description ? <p>{description}</p> : null}
    </section>
  );
}
