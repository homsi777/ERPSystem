type RecordFieldProps = {
  label: string;
  value: string;
  emphasis?: boolean;
};

export function RecordField({ label, value, emphasis = false }: RecordFieldProps) {
  return (
    <div className={emphasis ? 'record-card__field record-card__field--emphasis' : 'record-card__field'}>
      <dt>{label}</dt>
      <dd>{value}</dd>
    </div>
  );
}
