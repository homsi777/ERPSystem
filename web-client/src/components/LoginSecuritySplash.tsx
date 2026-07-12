import { useEffect, useState } from 'react';
import {
  LOGIN_SECURITY_STEPS,
  runSecuritySplashSequence,
  type SecuritySplashStep,
} from '../auth/loginSecuritySteps.ts';

type LoginSecuritySplashProps = {
  onComplete: () => void;
};

export function LoginSecuritySplash({ onComplete }: LoginSecuritySplashProps) {
  const [activeIndex, setActiveIndex] = useState(0);
  const [completedThrough, setCompletedThrough] = useState(-1);

  useEffect(() => {
    let cancelled = false;

    void runSecuritySplashSequence((index) => {
      if (cancelled) return;
      setActiveIndex(index);
      setCompletedThrough(index - 1);
    }).then(() => {
      if (!cancelled) onComplete();
    });

    return () => {
      cancelled = true;
    };
  }, [onComplete]);

  const progress = ((completedThrough + 1) / LOGIN_SECURITY_STEPS.length) * 100;

  return (
    <div className="login-security-splash" role="status" aria-live="polite" aria-label="تهيئة الجلسة الآمنة">
      <div className="login-security-splash__backdrop" aria-hidden="true">
        <span className="login-orb login-orb--one" />
        <span className="login-orb login-orb--two" />
        <span className="login-orb login-orb--three" />
      </div>

      <div className="login-security-splash__panel">
        <div className="login-security-splash__brand">
          <img src="/company-logo.png" alt="" className="login-security-splash__logo" width={64} height={64} />
          <div>
            <p className="login-security-splash__brand-name">الأمل.AB</p>
            <p className="login-security-splash__brand-tag">نظام ERP آمن — تجارة أقمشة الجينز</p>
          </div>
        </div>

        <h2 className="login-security-splash__title">جاري تجهيز بيئتك الآمنة</h2>
        <p className="login-security-splash__subtitle">يرجى الانتظار لحظات قبل الدخول إلى لوحة التحكم</p>

        <ul className="login-security-splash__steps">
          {LOGIN_SECURITY_STEPS.map((step, index) => (
            <SplashStepRow
              key={step.id}
              step={step}
              state={getStepState(index, activeIndex, completedThrough)}
            />
          ))}
        </ul>

        <div className="login-security-splash__progress" aria-hidden="true">
          <span className="login-security-splash__progress-bar" style={{ width: `${Math.max(progress, 8)}%` }} />
        </div>
      </div>
    </div>
  );
}

type StepState = 'pending' | 'active' | 'done';

function getStepState(index: number, activeIndex: number, completedThrough: number): StepState {
  if (index <= completedThrough) return 'done';
  if (index === activeIndex) return 'active';
  return 'pending';
}

function SplashStepRow({ step, state }: { step: SecuritySplashStep; state: StepState }) {
  return (
    <li className={`login-security-splash__step login-security-splash__step--${state}`}>
      <span className="login-security-splash__step-icon" aria-hidden="true">
        {state === 'done' ? '✓' : step.icon}
      </span>
      <span className="login-security-splash__step-text">{step.text}</span>
      {state === 'active' ? <span className="login-security-splash__pulse" aria-hidden="true" /> : null}
    </li>
  );
}
