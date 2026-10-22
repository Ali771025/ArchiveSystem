import apiClient from './apiClient';
import type {
  AdminUserDto,
  ClassificationAclEntryDto,
  ClassificationNodeDto,
  PermissionDto,
  RoleDto,
  TagAclEntryDto,
  TagDto
} from '../types/accessControl';

export const accessControlApi = {
  getRoles: async (): Promise<RoleDto[]> => {
    const response = await apiClient.get<RoleDto[]>('/v1/roles');
    return response.data;
  },

  createRole: async (name: string, isSystem = false): Promise<RoleDto> => {
    const response = await apiClient.post<RoleDto>('/v1/roles', { name, isSystem });
    return response.data;
  },

  updateRole: async (id: number, name: string): Promise<RoleDto> => {
    const response = await apiClient.put<RoleDto>(`/v1/roles/${id}`, { name, isSystem: false });
    return response.data;
  },

  deleteRole: async (id: number): Promise<void> => {
    await apiClient.delete(`/v1/roles/${id}`);
  },

  getPermissions: async (): Promise<PermissionDto[]> => {
    const response = await apiClient.get<PermissionDto[]>('/admin/permissions');
    return response.data;
  },

  getPermissionDefinitions: async (): Promise<string[]> => {
    const response = await apiClient.get<string[]>('/admin/permissions/definitions');
    return response.data;
  },

  getRolePermissions: async (roleId: number): Promise<number[]> => {
    const response = await apiClient.get<number[]>(`/admin/roles/${roleId}/permissions`);
    return response.data;
  },

  setRolePermissions: async (roleId: number, permissionIds: number[]): Promise<void> => {
    await apiClient.put(`/admin/roles/${roleId}/permissions`, { permissionIds });
  },

  getUsers: async (): Promise<AdminUserDto[]> => {
    const response = await apiClient.get<AdminUserDto[]>('/admin/users');
    return response.data;
  },

  setAccessRoleIds: async (userId: number, roleIds: number[]): Promise<void> => {
    await apiClient.put(`/admin/users/${userId}/roles`, { roleIds });
  },

  getIdentityRolePermissions: async (roleName: string): Promise<string[]> => {
    const encodedRoleName = encodeURIComponent(roleName);
    const response = await apiClient.get<string[]>(`/admin/identity/roles/${encodedRoleName}/permissions`);
    return response.data;
  },

  updateIdentityRolePermissions: async (roleName: string, permissions: string[]): Promise<void> => {
    const encodedRoleName = encodeURIComponent(roleName);
    await apiClient.put(`/admin/identity/roles/${encodedRoleName}/permissions`, { permissions });
  },

  getIdentityUserRoles: async (userId: number): Promise<string[]> => {
    const response = await apiClient.get<string[]>(`/admin/identity/users/${userId}/roles`);
    return response.data;
  },

  updateIdentityUserRoles: async (userId: number, roleNames: string[]): Promise<void> => {
    await apiClient.put(`/admin/identity/users/${userId}/roles`, { roleNames });
  },

  getClassificationTree: async (): Promise<ClassificationNodeDto[]> => {
    const response = await apiClient.get<Array<ClassificationNodeDto & { children?: ClassificationNodeDto[] }>>('/classifications/tree');

    const flatten = (
      items: Array<ClassificationNodeDto & { children?: ClassificationNodeDto[] }>,
      flat: ClassificationNodeDto[] = [],
    ): ClassificationNodeDto[] => {
      items.forEach((item) => {
        flat.push({
          id: item.id,
          title: item.title,
          parentId: item.parentId ?? null,
          path: item.path,
          level: item.level,
          isActive: item.isActive,
        });

        if (item.children && item.children.length > 0) {
          flatten(item.children, flat);
        }
      });

      return flat;
    };

    return flatten(response.data);
  },

  getClassificationAcl: async (classificationId: number): Promise<ClassificationAclEntryDto[]> => {
    const response = await apiClient.get<ClassificationAclEntryDto[]>(`/admin/classifications/${classificationId}/acl`);
    return response.data;
  },

  updateClassificationAcl: async (classificationId: number, entries: ClassificationAclEntryDto[]): Promise<void> => {
    await apiClient.post(`/admin/classifications/${classificationId}/acl`, { entries });
  },

  getTags: async (): Promise<TagDto[]> => {
    const response = await apiClient.get<TagDto[]>('/admin/tags');
    return response.data;
  },

  createTag: async (name: string, description?: string): Promise<TagDto> => {
    const response = await apiClient.post<TagDto>('/admin/tags', { name, description });
    return response.data;
  },

  getTagAcl: async (tagId: number): Promise<TagAclEntryDto[]> => {
    const response = await apiClient.get<TagAclEntryDto[]>('/admin/tags/acl', { params: { tagId } });
    return response.data;
  },

  updateTagAcl: async (tagId: number, entries: TagAclEntryDto[]): Promise<void> => {
    await apiClient.put('/admin/tags/acl', { tagId, entries });
  }
};
