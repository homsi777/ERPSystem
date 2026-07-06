import { apiRequest } from './client.ts';
import type { ChinaContainerStatus, ContainerListDto, ContainerOperationsCenterDto, PagedResult } from './types.ts';

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
