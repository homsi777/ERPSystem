import { useQuery } from '@tanstack/react-query';
import { getUserSessions } from '../api/settings.ts';
import { ApiError } from '../api/client.ts';
import { AppShell } from '../components/AppShell.tsx';
import { ErrorState } from '../components/ErrorState.tsx';
import { LoadingState } from '../components/LoadingState.tsx';
import { formatDate } from '../lib/format.ts';

export function SettingsUserSessionsPage() {
  const sessionsQuery = useQuery({
    queryKey: ['settings', 'user-sessions'],
    queryFn: () => getUserSessions(),
    refetchInterval: 30_000
  });

  return (
    <AppShell title="حالة المستخدمين">
      <p className="muted-line">
        سجل تسجيل الدخول والخروج لكل حساب — متصفح ويب أو تطبيق سطح المكتب. يُسمح بجلسة واحدة فقط لكل مستخدم.
      </p>

      {sessionsQuery.isLoading ? <LoadingState /> : null}

      {sessionsQuery.isError ? (
        <ErrorState
          message={sessionsQuery.error instanceof ApiError ? sessionsQuery.error.message : 'حدث خطأ غير متوقع.'}
          onRetry={() => void sessionsQuery.refetch()}
        />
      ) : null}

      {sessionsQuery.data && sessionsQuery.data.length > 0 ? (
        <div className="table-scroll desktop-only">
          <table className="data-table">
            <thead>
              <tr>
                <th>المستخدم</th>
                <th>الاسم</th>
                <th>نوع الدخول</th>
                <th>الحالة</th>
                <th>تسجيل الدخول</th>
                <th>تسجيل الخروج</th>
                <th>الجهاز</th>
                <th>IP</th>
              </tr>
            </thead>
            <tbody>
              {sessionsQuery.data.map((row) => (
                <tr key={row.id}>
                  <td>{row.username}</td>
                  <td>{row.fullNameAr}</td>
                  <td>{row.clientTypeDisplay}</td>
                  <td>
                    <span className={row.isActive ? 'status-pill status-pill--green' : 'status-pill'}>
                      {row.statusDisplay}
                    </span>
                  </td>
                  <td>{formatDate(row.loginAt)}</td>
                  <td>{row.logoutAt ? formatDate(row.logoutAt) : '—'}</td>
                  <td>{row.deviceInfo ?? '—'}</td>
                  <td>{row.ipAddress ?? '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : null}

      {sessionsQuery.data && sessionsQuery.data.length > 0 ? (
        <div className="card-list mobile-only">
          {sessionsQuery.data.map((row) => (
            <article className="line-card" key={row.id}>
              <div className="line-card__head">
                <h3>{row.fullNameAr}</h3>
                <span className={row.isActive ? 'status-pill status-pill--green' : 'status-pill'}>
                  {row.statusDisplay}
                </span>
              </div>
              <p className="muted-line">{row.username}</p>
              <dl className="detail-grid">
                <div>
                  <dt>نوع الدخول</dt>
                  <dd>{row.clientTypeDisplay}</dd>
                </div>
                <div>
                  <dt>تسجيل الدخول</dt>
                  <dd>{formatDate(row.loginAt)}</dd>
                </div>
                <div>
                  <dt>تسجيل الخروج</dt>
                  <dd>{row.logoutAt ? formatDate(row.logoutAt) : '—'}</dd>
                </div>
                {row.deviceInfo ? (
                  <div>
                    <dt>الجهاز</dt>
                    <dd>{row.deviceInfo}</dd>
                  </div>
                ) : null}
              </dl>
            </article>
          ))}
        </div>
      ) : null}

      {sessionsQuery.data && sessionsQuery.data.length === 0 ? (
        <p className="muted-line">لا توجد جلسات مسجّلة بعد.</p>
      ) : null}
    </AppShell>
  );
}
