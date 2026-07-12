import { useState, type FormEvent } from 'react';
import { Navigate, useLocation, useNavigate } from 'react-router-dom';
import { ApiError } from '../api/client.ts';
import { useAuth } from '../auth/AuthContext.tsx';

type LocationState = {
  from?: string;
};

const FEATURES = [
  { icon: '⚡', tone: 'violet', title: 'مبيعات ومشتريات', desc: 'فواتير، اعتماد، وتتبع كامل' },
  { icon: '📊', tone: 'emerald', title: 'محاسبة وتقارير', desc: 'قيود، صناديق، وأرصدة فورية' },
  { icon: '📦', tone: 'rose', title: 'استيراد الصين والمخزون', desc: 'حاويات، تفصيل، ومستودعات' },
  { icon: '☁️', tone: 'cyan', title: 'سحابي متكامل', desc: 'متصفح وسطح مكتب على قاعدة واحدة' },
] as const;

export function LoginPage() {
  const { isAuthenticated, login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  if (isAuthenticated) {
    return <Navigate to="/home" replace />;
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setIsSubmitting(true);
    setError(null);

    try {
      await login({ username, password });
      const state = location.state as LocationState | null;
      navigate(state?.from ?? '/home', { replace: true });
    } catch (caught) {
      const message = caught instanceof ApiError ? caught.message : 'تعذر تسجيل الدخول.';
      setError(message);
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <main className="login-shell">
      <div className="login-shell__backdrop" aria-hidden="true">
        <span className="login-orb login-orb--one" />
        <span className="login-orb login-orb--two" />
        <span className="login-orb login-orb--three" />
      </div>

      <div className="login-shell__grid">
        <section className="login-hero" aria-label="عن النظام">
          <div className="login-hero__content">
            <h1 className="login-hero__title">
              أدر عملك بذكاء
              <span aria-hidden="true"> ⚡</span>
            </h1>
            <p className="login-hero__subtitle">نظام ERP متكامل — تجارة أقمشة الجينز</p>

            <ul className="login-feature-list">
              {FEATURES.map((feature) => (
                <li key={feature.title} className="login-feature-list__item">
                  <span className={`login-feature-list__icon login-feature-list__icon--${feature.tone}`} aria-hidden="true">
                    {feature.icon}
                  </span>
                  <div>
                    <strong>{feature.title}</strong>
                    <span>{feature.desc}</span>
                  </div>
                </li>
              ))}
            </ul>
          </div>
        </section>

        <section className="login-card" aria-labelledby="login-title">
          <div className="login-card__brand">
            <img src="/company-logo.png" alt="" className="login-card__logo" width={56} height={56} />
            <div>
              <p className="login-card__brand-label">الأمل.AB</p>
              <p className="login-card__brand-tag">تجارة أقمشة الجينز — جملة</p>
            </div>
          </div>

          <h2 id="login-title" className="login-card__title">
            مرحباً بعودتك
            <span aria-hidden="true"> 👋</span>
          </h2>
          <p className="login-card__hint">سجّل الدخول للمتابعة إلى لوحة التحكم</p>

          <form className="login-form login-form--dark" onSubmit={(event) => void handleSubmit(event)}>
            <label className="login-field">
              <span>اسم المستخدم</span>
              <input
                value={username}
                onChange={(event) => setUsername(event.target.value)}
                autoComplete="username"
                placeholder="admin"
                required
              />
            </label>

            <label className="login-field">
              <span>كلمة المرور</span>
              <div className="login-field__password">
                <input
                  type={showPassword ? 'text' : 'password'}
                  value={password}
                  onChange={(event) => setPassword(event.target.value)}
                  autoComplete="current-password"
                  placeholder="••••••••"
                  required
                />
                <button
                  type="button"
                  className="login-field__toggle"
                  onClick={() => setShowPassword((value) => !value)}
                  aria-label={showPassword ? 'إخفاء كلمة المرور' : 'إظهار كلمة المرور'}
                >
                  {showPassword ? '🙈' : '👁'}
                </button>
              </div>
            </label>

            {error ? <p className="login-form__error">{error}</p> : null}

            <button className="login-submit" type="submit" disabled={isSubmitting}>
              <span>{isSubmitting ? 'جاري الدخول...' : 'تسجيل الدخول'}</span>
              <span className="login-submit__icon" aria-hidden="true">→</span>
            </button>
          </form>
        </section>
      </div>
    </main>
  );
}
