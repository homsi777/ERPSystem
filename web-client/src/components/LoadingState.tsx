type LoadingStateProps = {
  label?: string;
  fullScreen?: boolean;
};

export function LoadingState({ label = 'جاري التحميل...', fullScreen = false }: LoadingStateProps) {
  return (
    <div className={fullScreen ? 'state state--screen' : 'state'}>
      <span className="spinner" />
      <p>{label}</p>
    </div>
  );
}
