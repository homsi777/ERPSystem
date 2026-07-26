import { apiRequest } from './client.ts';
import type {
  IdentityRoleListDto,
  IdentityUserListDto,
  PermissionModuleGroupDto,
  RolePermissionsDto,
  UserSessionStatusDto
} from './types.ts';

export function getUserSessions(limit = 200) {
  return apiRequest<UserSessionStatusDto[]>(`/api/v1/settings/user-sessions?limit=${limit}`);
}

export function getIdentityUsers() {
  return apiRequest<IdentityUserListDto[]>('/api/v1/settings/users');
}

export function createIdentityUser(request: {
  username: string;
  password: string;
  fullNameAr: string;
  fullNameEn: string;
  roleIds: string[];
}) {
  return apiRequest<{ id: string }>('/api/v1/settings/users', {
    method: 'POST',
    body: request
  });
}

export function updateIdentityUserRoles(userId: string, roleIds: string[]) {
  return apiRequest<void>(`/api/v1/settings/users/${userId}/roles`, {
    method: 'PUT',
    body: { roleIds }
  });
}

export function getIdentityRoles() {
  return apiRequest<IdentityRoleListDto[]>('/api/v1/settings/roles');
}

export function createIdentityRole(request: { name: string; description: string }) {
  return apiRequest<{ id: string }>('/api/v1/settings/roles', {
    method: 'POST',
    body: request
  });
}

export function getPermissionTree() {
  return apiRequest<PermissionModuleGroupDto[]>('/api/v1/settings/permissions');
}

export function getRolePermissions(roleId: string) {
  return apiRequest<RolePermissionsDto>(`/api/v1/settings/roles/${roleId}/permissions`);
}

export function updateRolePermissions(roleId: string, permissionCodes: string[]) {
  return apiRequest<void>(`/api/v1/settings/roles/${roleId}/permissions`, {
    method: 'PUT',
    body: { permissionCodes }
  });
}
