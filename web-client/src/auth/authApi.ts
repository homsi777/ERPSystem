import { apiRequest, rawRequest } from '../api/client.ts';
import type {
  AuthTokenResponse,
  LoginRequest,
  MeResponse,
  RefreshTokenResponse
} from '../api/types.ts';

export function loginRequest(body: LoginRequest) {
  return rawRequest<AuthTokenResponse>('/api/v1/auth/login', {
    method: 'POST',
    body,
    skipAuth: true
  });
}

export function refreshRequest(refreshToken: string) {
  return rawRequest<RefreshTokenResponse>('/api/v1/auth/refresh', {
    method: 'POST',
    body: { refreshToken },
    skipAuth: true
  });
}

export function logoutRequest(refreshToken: string) {
  return rawRequest<{ message: string }>('/api/v1/auth/logout', {
    method: 'POST',
    body: { refreshToken },
    skipAuth: true
  });
}

export function getMeRequest() {
  return apiRequest<MeResponse>('/api/v1/auth/me');
}
