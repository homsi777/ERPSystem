type SummaryCardProps = {
  label: string;
  value: string;
  tone?: 'blue' | 'green' | 'amber';
};

export function SummaryCard({ label, value, tone = 'blue' }: SummaryCardProps) {
  return (
    <div className={`summary-card summary-card--${tone}`}>
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  );
}
