import { apiRequest } from './client.ts';
import type { UserSessionStatusDto } from './types.ts';

export function getUserSessions(limit = 200) {
  return apiRequest<UserSessionStatusDto[]>(`/api/v1/settings/user-sessions?limit=${limit}`);
}
