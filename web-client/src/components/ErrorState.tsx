import { Icon } from './Icon.tsx';

type ErrorStateProps = {
  message: string;
  onRetry: () => void;
};

export function ErrorState({ message, onRetry }: ErrorStateProps) {
  return (
    <section className="state-card state-card--error">
      <Icon name="alert" />
      <h2>تعذر تحميل البيانات</h2>
      <p>{message}</p>
      <button className="primary-button" type="button" onClick={onRetry}>
        إعادة المحاولة
      </button>
    </section>
  );
}
