export type SecuritySplashStep = {
  id: string;
  text: string;
  icon: string;
};

export const LOGIN_SECURITY_STEPS: SecuritySplashStep[] = [
  { id: 'welcome', text: 'مرحباً بك في الأمل.AB — جاري تهيئة جلسة آمنة', icon: '👋' },
  { id: 'secure', text: 'يتم تأمين اتصال', icon: '🔒' },
  { id: 'encrypt', text: 'يتم تشفير البيانات', icon: '🔐' },
  { id: 'odocore', text: 'يتم أتصال OdoCore', icon: '◉' },
  { id: 'dlp', text: 'يتم أتصال Google Workspace DLP', icon: '🛡️' },
  { id: 'ready', text: 'تم تجهيز أتصالات', icon: '✓' },
  { id: 'enter', text: 'تفضل', icon: '→' },
];

export const LOGIN_SECURITY_STEP_MS = 550;
export const LOGIN_SECURITY_FINAL_STEP_EXTRA_MS = 500;
export const LOGIN_SECURITY_ENTER_PAUSE_MS = 500;

export async function runSecuritySplashSequence(
  onStep: (index: number, step: SecuritySplashStep) => void,
  stepMs: number = LOGIN_SECURITY_STEP_MS,
): Promise<void> {
  for (let index = 0; index < LOGIN_SECURITY_STEPS.length; index += 1) {
    onStep(index, LOGIN_SECURITY_STEPS[index]!);
    const delay = index === LOGIN_SECURITY_STEPS.length - 1
      ? stepMs + LOGIN_SECURITY_FINAL_STEP_EXTRA_MS
      : stepMs;
    await new Promise((resolve) => window.setTimeout(resolve, delay));
  }

  await new Promise((resolve) => window.setTimeout(resolve, LOGIN_SECURITY_ENTER_PAUSE_MS));
}
