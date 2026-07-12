type BrandMarkProps = {
  compact?: boolean;
  showTagline?: boolean;
};

export function BrandMark({ compact = false, showTagline = false }: BrandMarkProps) {
  return (
    <div className={`brand-mark${compact ? ' brand-mark--compact' : ''}`}>
      <img src="/pwa-192x192.png" alt="شعار الأمل.AB" className="brand-mark__logo" width={compact ? 36 : 44} height={compact ? 36 : 44} />
      <div className="brand-mark__text">
        <p className="brand-mark__name">الأمل.AB</p>
        {showTagline ? <span className="brand-mark__tagline">تجارة أقمشة الجينز — جملة</span> : null}
      </div>
    </div>
  );
}
