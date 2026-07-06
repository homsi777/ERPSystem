import { apiRequest } from './client.ts';
import type { DashboardSummaryDto } from './types.ts';

export function getDashboardSummary() {
  return apiRequest<DashboardSummaryDto>('/api/v1/dashboard/summary');
}
