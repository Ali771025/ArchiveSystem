export type AccessSubjectType = 1 | 2;

export interface RoleDto {
  id: number;
  name: string;
  isSystem: boolean;
  description?: string | null;
}

export interface PermissionDto {
  id: number;
  key: string;
  description: string;
}

export interface AdminUserDto {
  id: number;
  userName: string;
  email: string;
  firstName?: string | null;
  lastName?: string | null;
}

export interface ClassificationNodeDto {
  id: number;
  title: string;
  parentId?: number | null;
  path: string;
  level: number;
  isActive: boolean;
}

export interface ClassificationAclEntryDto {
  id: number;
  subjectType: AccessSubjectType;
  subjectId: number;
  classificationId: number;
  permissionMask: number;
  isExplicit: boolean;
  inheritMode: number;
  cascade?: boolean;
}

export interface TagDto {
  id: number;
  name: string;
  description?: string | null;
}

export interface TagAclEntryDto {
  id: number;
  subjectType: AccessSubjectType;
  subjectId: number;
  tagId: number;
  permissionMask: number;
}

