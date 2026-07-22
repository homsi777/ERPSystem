import type { ApiErrorResponse } from './types.ts';

const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL ??
  (import.meta.env.DEV
    ? `${window.location.protocol}//${window.location.hostname}:5218`
    : '');

type RequestBody = BodyInit | Record<string, unknown> | unknown[] | null;

type RequestOptions = Omit<RequestInit, 'body'> & {
  body?: RequestBody;
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

export async function apiBlobRequest(path: string, options: RequestOptions = {}): Promise<Blob> {
  const token = options.skipAuth ? null : authController?.getAccessToken() ?? null;
  let response: Response;
  try {
    response = await fetch(`${API_BASE_URL}${path}`, {
      ...options,
      headers: buildHeaders(options, token),
      body: serializeBody(options.body)
    });
  } catch {
    throw new ApiError(0, {
      code: 'NETWORK_ERROR',
      message: 'تعذّر الاتصال بالخادم لتنزيل المستند.',
      validationErrors: []
    });
  }

  if (response.status === 401 && authController && !options.skipAuth) {
    const refreshedToken = await authController.refreshAccessToken();
    if (refreshedToken) {
      response = await fetch(`${API_BASE_URL}${path}`, {
        ...options,
        headers: buildHeaders(options, refreshedToken),
        body: serializeBody(options.body)
      });
    } else {
      authController.clearAuth();
      window.dispatchEvent(new Event('erp-auth-expired'));
    }
  }

  if (response.ok) return response.blob();

  const fallback: ApiErrorResponse = {
    code: `HTTP_${response.status}`,
    message: 'تعذّر تنزيل المستند من الخادم.',
    validationErrors: []
  };
  try {
    throw new ApiError(response.status, (await response.json()) as ApiErrorResponse);
  } catch (error) {
    if (error instanceof ApiError) throw error;
    throw new ApiError(response.status, fallback);
  }
}

async function sendRequest<T>(path: string, options: RequestOptions, allowRefresh: boolean): Promise<T> {
  const token = options.skipAuth ? null : authController?.getAccessToken() ?? null;
  let response: Response;
  try {
    response = await fetch(`${API_BASE_URL}${path}`, {
      ...options,
      headers: buildHeaders(options, token),
      body: serializeBody(options.body)
    });
  } catch {
    throw new ApiError(0, {
      code: 'NETWORK_ERROR',
      message: 'تعذر الاتصال بالخادم. تأكد أن الخادم يعمل وأنك على نفس الشبكة.',
      validationErrors: []
    });
  }

  if (response.status === 401 && allowRefresh && authController && !options.skipAuth) {
    const refreshedToken = await authController.refreshAccessToken();
    if (refreshedToken) {
      let retryResponse: Response;
      try {
        retryResponse = await fetch(`${API_BASE_URL}${path}`, {
          ...options,
          headers: buildHeaders(options, refreshedToken),
          body: serializeBody(options.body)
        });
      } catch {
        throw new ApiError(0, {
          code: 'NETWORK_ERROR',
          message: 'تعذر الاتصال بالخادم. تأكد أن الخادم يعمل وأنك على نفس الشبكة.',
          validationErrors: []
        });
      }
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
  if (options.body !== undefined && !(options.body instanceof FormData) && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json');
  }
  if (token) {
    headers.set('Authorization', `Bearer ${token}`);
  }
  return headers;
}

function serializeBody(body: RequestBody | undefined): BodyInit | null | undefined {
  if (body === undefined) {
    return undefined;
  }
  if (body === null) {
    return null;
  }
  if (body instanceof FormData || body instanceof Blob || body instanceof URLSearchParams || typeof body === 'string') {
    return body;
  }
  return JSON.stringify(body);
}

async function parseResponse<T>(response: Response): Promise<T> {
  if (response.ok) {
    // Successful void commands often return 200 with an empty body (or 204).
    // Parsing empty JSON throws and falsely shows "save failed" after a real success.
    if (response.status === 204 || response.status === 205) {
      return undefined as T;
    }

    const raw = await response.text();
    if (!raw.trim()) {
      return undefined as T;
    }

    try {
      return JSON.parse(raw) as T;
    } catch {
      throw new ApiError(response.status, {
        code: 'INVALID_JSON',
        message: 'استجابة الخادم غير صالحة.',
        validationErrors: []
      });
    }
  }

  const fallback: ApiErrorResponse = {
    code: `HTTP_${response.status}`,
    message: 'تعذر تنفيذ الطلب.',
    validationErrors: []
  };

  let errorBody = fallback;
  try {
    const parsed = (await response.json()) as Record<string, unknown>;
    const code = typeof parsed.code === 'string'
      ? parsed.code
      : typeof parsed.Code === 'string'
        ? parsed.Code
        : fallback.code;
    const message = typeof parsed.message === 'string'
      ? parsed.message
      : typeof parsed.Message === 'string'
        ? parsed.Message
        : fallback.message;
    const rawValidationErrors = Array.isArray(parsed.validationErrors)
      ? parsed.validationErrors
      : Array.isArray(parsed.ValidationErrors)
        ? parsed.ValidationErrors
        : [];
    errorBody = {
      code,
      message,
      validationErrors: normalizeValidationErrors(rawValidationErrors)
    };
  } catch {
    // Keep the fallback body for non-JSON failures.
  }

  throw new ApiError(response.status, errorBody);
}

function normalizeValidationErrors(raw: unknown[]): ApiErrorResponse['validationErrors'] {
  return raw
    .map((item) => {
      if (typeof item === 'string') {
        const message = item.trim();
        return message ? { field: '', message } : null;
      }
      if (!item || typeof item !== 'object') {
        return null;
      }
      const record = item as Record<string, unknown>;
      const field = String(record.field ?? record.Field ?? '').trim();
      const message = String(record.message ?? record.Message ?? '').trim();
      return message ? { field, message } : null;
    })
    .filter((item): item is ApiErrorResponse['validationErrors'][number] => item !== null);
}
