import type { ApiErrorResponse } from './types.ts';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5218';

type RequestOptions = Omit<RequestInit, 'body'> & {
  body?: unknown;
  skipAuth?: boolean;
};

type AuthController = {
  getAccessToken: () => string | null;
  refreshAccessToken: () => Promise<string | null>;
  clearAuth: () => void;
};

export class ApiError extends Error {
  public readonly status: number;
  public readonly code: string;
  public readonly validationErrors: ApiErrorResponse['validationErrors'];

  constructor(status: number, response: ApiErrorResponse) {
    super(response.message);
    this.name = 'ApiError';
    this.status = status;
    this.code = response.code;
    this.validationErrors = response.validationErrors;
  }
}

let authController: AuthController | null = null;

export function setAuthController(controller: AuthController) {
  authController = controller;
}

export async function rawRequest<T>(path: string, options: RequestOptions = {}): Promise<T> {
  return sendRequest<T>(path, options, false);
}

export async function apiRequest<T>(path: string, options: RequestOptions = {}): Promise<T> {
  return sendRequest<T>(path, options, true);
}

async function sendRequest<T>(path: string, options: RequestOptions, allowRefresh: boolean): Promise<T> {
  const token = options.skipAuth ? null : authController?.getAccessToken() ?? null;
  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...options,
    headers: buildHeaders(options, token),
    body: options.body === undefined ? undefined : JSON.stringify(options.body)
  });

  if (response.status === 401 && allowRefresh && authController && !options.skipAuth) {
    const refreshedToken = await authController.refreshAccessToken();
    if (refreshedToken) {
      const retryResponse = await fetch(`${API_BASE_URL}${path}`, {
        ...options,
        headers: buildHeaders(options, refreshedToken),
        body: options.body === undefined ? undefined : JSON.stringify(options.body)
      });
      return parseResponse<T>(retryResponse);
    }

    authController.clearAuth();
    window.dispatchEvent(new Event('erp-auth-expired'));
  }

  return parseResponse<T>(response);
}

function buildHeaders(options: RequestOptions, token: string | null): HeadersInit {
  const headers = new Headers(options.headers);
  if (!headers.has('Accept')) {
    headers.set('Accept', 'application/json');
  }
  if (options.body !== undefined && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json');
  }
  if (token) {
    headers.set('Authorization', `Bearer ${token}`);
  }
  return headers;
}

async function parseResponse<T>(response: Response): Promise<T> {
  if (response.ok) {
    if (response.status === 204) {
      return undefined as T;
    }
    return (await response.json()) as T;
  }

  const fallback: ApiErrorResponse = {
    code: `HTTP_${response.status}`,
    message: 'تعذر تنفيذ الطلب.',
    validationErrors: []
  };

  let errorBody = fallback;
  try {
    errorBody = (await response.json()) as ApiErrorResponse;
  } catch {
    // Keep the fallback body for non-JSON failures.
  }

  throw new ApiError(response.status, errorBody);
}
