import { Icon } from './Icon.tsx';

type ComingSoonProps = {
  title: string;
};

export function ComingSoon({ title }: ComingSoonProps) {
  return (
    <section className="coming-soon">
      <div className="coming-soon__icon">
        <Icon name="chart" />
      </div>
      <h2>{title}</h2>
      <p>قيد الإنشاء</p>
    </section>
  );
}
