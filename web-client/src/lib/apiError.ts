import { ApiError } from '../api/client.ts';

/**
 * Single place for turning API/client failures into user-facing Arabic (or server) text.
 * Always prefers concrete validation/conflict messages over generic English stubs.
 */
export function getApiErrorMessage(error: unknown, fallback = 'حدث خطأ غير متوقع.'): string {
  if (!(error instanceof ApiError)) {
    if (error instanceof Error && error.message.trim()) {
      return error.message.trim();
    }
    return fallback;
  }

  const validationMessages = (error.validationErrors ?? [])
    .map((item) => (item.message ?? '').trim())
    .filter((message) => message.length > 0);

  if (validationMessages.length === 1) {
    return validationMessages[0]!;
  }
  if (validationMessages.length > 1) {
    return validationMessages.map((message) => `• ${message}`).join('\n');
  }

  const message = (error.message ?? '').trim();
  if (
    message &&
    message !== 'Validation failed.' &&
    message !== 'Request failed.' &&
    message !== 'تعذر تنفيذ الطلب.'
  ) {
    return message;
  }

  if (error.status === 403) {
    return 'لا تملك صلاحية لتنفيذ هذا الإجراء.';
  }
  if (error.status === 404) {
    return 'العنصر غير موجود أو لم يعد متاحاً.';
  }
  if (error.status === 0) {
    return 'تعذر الاتصال بالخادم. تأكد من الشبكة ثم أعد المحاولة.';
  }

  return message || fallback;
}
