import { apiRequest } from './client.ts';
import type {
  CalculateLandingCostRequest,
  ChinaContainerStatus,
  ChinaInvoiceParseResultDto,
  ChinaPackingSummaryParseResultDto,
  ContainerExcelParseResultDto,
  ContainerListDto,
  ContainerOperationsCenterDto,
  CreateChinaContainerRequest,
  MoveContainerToWarehouseRequest,
  PagedResult,
  SetContainerSalePricesRequest
} from './types.ts';

export type ContainerListParams = {
  status?: ChinaContainerStatus;
  page: number;
  pageSize: number;
};

export function getContainers(params: ContainerListParams) {
  const searchParams = new URLSearchParams({
    page: String(params.page),
    pageSize: String(params.pageSize)
  });

  if (params.status !== undefined) {
    searchParams.set('status', String(params.status));
  }

  return apiRequest<PagedResult<ContainerListDto>>(`/api/v1/containers?${searchParams.toString()}`);
}

export function getContainerOperations(containerId: string) {
  return apiRequest<ContainerOperationsCenterDto>(`/api/v1/containers/${containerId}/operations`);
}

export function createContainer(request: CreateChinaContainerRequest) {
  return apiRequest<string>('/api/v1/containers', {
    method: 'POST',
    body: request
  });
}

export function calculateLandingCost(containerId: string, request: CalculateLandingCostRequest) {
  return apiRequest<void>(`/api/v1/containers/${containerId}/landing-cost`, {
    method: 'POST',
    body: request
  });
}

export function setContainerSalePrices(containerId: string, request: SetContainerSalePricesRequest) {
  return apiRequest<void>(`/api/v1/containers/${containerId}/sale-prices`, {
    method: 'POST',
    body: request
  });
}

export function approveContainer(containerId: string) {
  return apiRequest<void>(`/api/v1/containers/${containerId}/approve`, {
    method: 'POST'
  });
}

export function moveContainerToWarehouse(containerId: string, request: MoveContainerToWarehouseRequest) {
  return apiRequest<void>(`/api/v1/containers/${containerId}/move-to-warehouse`, {
    method: 'POST',
    body: request
  });
}

export function archiveContainer(containerId: string) {
  return apiRequest<void>(`/api/v1/containers/${containerId}/archive`, {
    method: 'POST'
  });
}

export function parseContainerDpl(file: File) {
  return parseContainerFile<ContainerExcelParseResultDto>('/api/v1/containers/parse/dpl', file);
}

export function parseChinaInvoice(file: File) {
  return parseContainerFile<ChinaInvoiceParseResultDto>('/api/v1/containers/parse/invoice', file);
}

export function parseChinaPackingSummary(file: File) {
  return parseContainerFile<ChinaPackingSummaryParseResultDto>('/api/v1/containers/parse/packing-summary', file);
}

function parseContainerFile<T>(path: string, file: File) {
  const formData = new FormData();
  formData.set('file', file);
  return apiRequest<T>(path, {
    method: 'POST',
    body: formData
  });
}
