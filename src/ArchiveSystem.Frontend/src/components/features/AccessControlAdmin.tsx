import { useEffect, useMemo, useRef, useState } from 'react';
import { accessControlApi } from '../../services/accessControlApi';
import apiClient from '../../services/apiClient';
import type {
  AdminUserDto,
  ClassificationAclEntryDto,
  ClassificationNodeDto,
  PermissionDto,
  RoleDto,
  TagAclEntryDto,
  TagDto
} from '../../types/accessControl';

const CLASSIFICATION_READ = 1;
const CLASSIFICATION_WRITE = 2;
const CLASSIFICATION_MANAGE = 4;
const TAG_VIEW = 1;
const TAG_ASSIGN = 2;

interface AccessControlAdminProps {
  canManage: boolean;
}

const subjectLabel = (subjectType: number): string => {
  return subjectType === 1 ? 'User' : 'Role';
};

const extractRoleIds = (payload: unknown): number[] => {
  if (!Array.isArray(payload)) {
    return [];
  }

  return payload
    .map((item) => {
      if (typeof item === 'number') {
        return item;
      }

      if (item && typeof item === 'object' && 'id' in item) {
        const rawId = Number((item as { id: unknown }).id);
        return Number.isFinite(rawId) ? rawId : 0;
      }

      return 0;
    })
    .filter((id) => id > 0);
};

export default function AccessControlAdmin({ canManage }: AccessControlAdminProps) {
  const [roles, setRoles] = useState<RoleDto[]>([]);
  const [permissions, setPermissions] = useState<PermissionDto[]>([]);
  const [users, setUsers] = useState<AdminUserDto[]>([]);
  const [classifications, setClassifications] = useState<ClassificationNodeDto[]>([]);
  const [tags, setTags] = useState<TagDto[]>([]);

  const [selectedRoleId, setSelectedRoleId] = useState<number | null>(null);
  const [selectedRolePermissionIds, setSelectedRolePermissionIds] = useState<number[]>([]);
  const [newRoleName, setNewRoleName] = useState('');

  const [selectedUserId, setSelectedUserId] = useState<number | null>(null);
  const [selectedUserRoleIds, setSelectedUserRoleIds] = useState<number[]>([]);
  const [userRolesByUserId, setUserRolesByUserId] = useState<Record<number, number[]>>({});
  const userRolesByUserIdRef = useRef<Record<number, number[]>>({});
  const [loadingUserRoles, setLoadingUserRoles] = useState(false);
  const [savingUserRoles, setSavingUserRoles] = useState(false);
  const [userRoleInfo, setUserRoleInfo] = useState('');

  const [selectedClassificationId, setSelectedClassificationId] = useState<number | null>(null);
  const [classificationAclEntries, setClassificationAclEntries] = useState<ClassificationAclEntryDto[]>([]);
  const [classificationSubjectType, setClassificationSubjectType] = useState<number>(2);
  const [classificationSubjectId, setClassificationSubjectId] = useState<number>(0);
  const [classificationMask, setClassificationMask] = useState<number>(0);

  const [selectedTagId, setSelectedTagId] = useState<number | null>(null);
  const [tagAclEntries, setTagAclEntries] = useState<TagAclEntryDto[]>([]);
  const [tagSubjectType, setTagSubjectType] = useState<number>(2);
  const [tagSubjectId, setTagSubjectId] = useState<number>(0);
  const [tagMask, setTagMask] = useState<number>(0);
  const [newTagName, setNewTagName] = useState('');

  useEffect(() => {
    const loadInitialData = async () => {
      const [roleData, permissionData, userData, classificationData, tagData] = await Promise.all([
        accessControlApi.getRoles(),
        accessControlApi.getPermissions(),
        accessControlApi.getUsers(),
        accessControlApi.getClassificationTree(),
        accessControlApi.getTags()
      ]);

      setRoles(roleData);
      setPermissions(permissionData);
      setUsers(userData);
      setClassifications(classificationData);
      setTags(tagData);
      if (roleData.length > 0) {
        setSelectedRoleId(roleData[0].id);
      }
      if (userData.length > 0) {
        setSelectedUserId(userData[0].id);
      }
      if (classificationData.length > 0) {
        setSelectedClassificationId(classificationData[0].id);
      }
      if (tagData.length > 0) {
        setSelectedTagId(tagData[0].id);
      }
    };

    loadInitialData().catch((error) => {
      console.error('Failed to load access control data', error);
    });
  }, []);

  useEffect(() => {
    if (!selectedRoleId) {
      setSelectedRolePermissionIds([]);
      return;
    }

    accessControlApi.getRolePermissions(selectedRoleId)
      .then(setSelectedRolePermissionIds)
      .catch((error) => {
        console.error('Failed to load role permissions', error);
      });
  }, [selectedRoleId]);

  useEffect(() => {
    if (!selectedClassificationId) {
      setClassificationAclEntries([]);
      return;
    }

    accessControlApi.getClassificationAcl(selectedClassificationId)
      .then(setClassificationAclEntries)
      .catch((error) => {
        console.error('Failed to load classification ACL', error);
      });
  }, [selectedClassificationId]);

  useEffect(() => {
    if (!selectedUserId) {
      setSelectedUserRoleIds([]);
      return;
    }

    let cancelled = false;

    const loadUserRoles = async () => {
      setLoadingUserRoles(true);
      setUserRoleInfo('');
      try {
        const response = await apiClient.get<unknown>(`/admin/users/${selectedUserId}/roles`);
        if (cancelled) {
          return;
        }

        const roleIds = extractRoleIds(response.data);
        userRolesByUserIdRef.current = { ...userRolesByUserIdRef.current, [selectedUserId]: roleIds };
        setUserRolesByUserId(userRolesByUserIdRef.current);
        setSelectedUserRoleIds(roleIds);
      } catch (error) {
        if (cancelled) {
          return;
        }

        const statusCode = Number((error as { response?: { status?: unknown } })?.response?.status ?? 0);
        if (statusCode === 404 || statusCode === 0) {
          setUserRoleInfo('مسیر خواندن نقش‌های کاربر در API یافت نشد. انتخاب‌ها به‌صورت محلی نگه‌داری می‌شوند.');
          setSelectedUserRoleIds(userRolesByUserIdRef.current[selectedUserId] ?? []);
        } else {
          console.error('Failed to load user roles', error);
          setUserRoleInfo('خطا در بارگذاری نقش‌های کاربر. می‌توانید نقش‌ها را انتخاب و ذخیره کنید.');
        }
      } finally {
        if (!cancelled) {
          setLoadingUserRoles(false);
        }
      }
    };

    loadUserRoles().catch((error) => {
      console.error('Failed to load user roles', error);
      setLoadingUserRoles(false);
    });

    return () => {
      cancelled = true;
    };
  }, [selectedUserId]);

  useEffect(() => {
    if (!selectedTagId) {
      setTagAclEntries([]);
      return;
    }

    accessControlApi.getTagAcl(selectedTagId)
      .then(setTagAclEntries)
      .catch((error) => {
        console.error('Failed to load tag ACL', error);
      });
  }, [selectedTagId]);

  const sortedClassifications = useMemo(
    () => [...classifications].sort((a, b) => a.path.localeCompare(b.path)),
    [classifications]
  );

  const roleNameById = useMemo(() => {
    return new Map(roles.map((role) => [role.id, role.name]));
  }, [roles]);

  const togglePermission = (permissionId: number) => {
    setSelectedRolePermissionIds((current) =>
      current.includes(permissionId)
        ? current.filter((id) => id !== permissionId)
        : [...current, permissionId]
    );
  };

  const toggleMask = (flag: number, currentMask: number, setter: (mask: number) => void) => {
    const enabled = (currentMask & flag) === flag;
    setter(enabled ? currentMask & ~flag : currentMask | flag);
  };

  const saveRolePermissions = async () => {
    if (!selectedRoleId) {
      return;
    }
    await accessControlApi.setRolePermissions(selectedRoleId, selectedRolePermissionIds);
  };

  const createRole = async () => {
    if (!newRoleName.trim()) {
      return;
    }
    const role = await accessControlApi.createRole(newRoleName.trim());
    const roleData = await accessControlApi.getRoles();
    setRoles(roleData);
    setNewRoleName('');
    setSelectedRoleId(role.id);
  };

  const saveUserRoles = async () => {
    if (!selectedUserId) {
      return;
    }

    setSavingUserRoles(true);
    try {
      await accessControlApi.setAccessRoleIds(selectedUserId, selectedUserRoleIds);
      userRolesByUserIdRef.current = {
        ...userRolesByUserIdRef.current,
        [selectedUserId]: selectedUserRoleIds
      };
      setUserRolesByUserId(userRolesByUserIdRef.current);
      setUserRoleInfo('نقش‌های کاربر با موفقیت ذخیره شد.');
    } catch (error) {
      console.error('Failed to save user roles', error);
      setUserRoleInfo('ذخیره نقش‌ها ناموفق بود.');
    } finally {
      setSavingUserRoles(false);
    }
  };

  const upsertClassificationEntry = () => {
    if (!selectedClassificationId || classificationSubjectId <= 0 || classificationMask === 0) {
      return;
    }

    setClassificationAclEntries((current) => {
      const filtered = current.filter(
        (x) => !(x.subjectType === classificationSubjectType && x.subjectId === classificationSubjectId)
      );

      return [
        ...filtered,
        {
          id: 0,
          subjectType: classificationSubjectType as 1 | 2,
          subjectId: classificationSubjectId,
          classificationId: selectedClassificationId,
          permissionMask: classificationMask,
          isExplicit: true,
          inheritMode: 1
        }
      ];
    });
  };

  const saveClassificationAcl = async () => {
    if (!selectedClassificationId) {
      return;
    }

    await accessControlApi.updateClassificationAcl(selectedClassificationId, classificationAclEntries);
  };

  const upsertTagEntry = () => {
    if (!selectedTagId || tagSubjectId <= 0 || tagMask === 0) {
      return;
    }

    setTagAclEntries((current) => {
      const filtered = current.filter((x) => !(x.subjectType === tagSubjectType && x.subjectId === tagSubjectId));
      return [
        ...filtered,
        {
          id: 0,
          subjectType: tagSubjectType as 1 | 2,
          subjectId: tagSubjectId,
          tagId: selectedTagId,
          permissionMask: tagMask
        }
      ];
    });
  };

  const saveTagAcl = async () => {
    if (!selectedTagId) {
      return;
    }
    await accessControlApi.updateTagAcl(selectedTagId, tagAclEntries);
  };

  const createTag = async () => {
    if (!newTagName.trim()) {
      return;
    }

    const created = await accessControlApi.createTag(newTagName.trim());
    const updated = await accessControlApi.getTags();
    setTags(updated);
    setSelectedTagId(created.id);
    setNewTagName('');
  };

  return (
    <section className="rounded border p-4 space-y-6">
      <header>
        <h2 className="text-xl font-semibold">Access Control Administration</h2>
        {!canManage && (
          <p className="text-sm text-red-700 mt-1">
            You do not have permission to modify ACL settings. Read-only mode is active.
          </p>
        )}
      </header>

      <div className="grid gap-6 lg:grid-cols-2">
        <article className="rounded border p-3 space-y-3">
          <h3 className="font-semibold">Roles and Permissions</h3>
          <div className="flex gap-2">
            <input
              value={newRoleName}
              onChange={(event) => setNewRoleName(event.target.value)}
              placeholder="New role name"
              className="border rounded px-2 py-1 flex-1"
            />
            <button
              type="button"
              className="border rounded px-3 py-1"
              disabled={!canManage}
              onClick={createRole}
            >
              Create
            </button>
          </div>
          <select
            value={selectedRoleId ?? ''}
            onChange={(event) => setSelectedRoleId(Number(event.target.value))}
            className="border rounded px-2 py-1 w-full"
          >
            {roles.map((role) => (
              <option key={role.id} value={role.id}>
                {role.name}
              </option>
            ))}
          </select>
          <div className="max-h-40 overflow-auto space-y-1 border rounded p-2">
            {permissions.map((permission) => (
              <label key={permission.id} className="flex items-center gap-2 text-sm">
                <input
                  type="checkbox"
                  checked={selectedRolePermissionIds.includes(permission.id)}
                  onChange={() => togglePermission(permission.id)}
                  disabled={!canManage}
                />
                <span>{permission.key}</span>
              </label>
            ))}
          </div>
          <button type="button" className="border rounded px-3 py-1" disabled={!canManage} onClick={saveRolePermissions}>
            Save Role Permissions
          </button>
        </article>

        <article className="rounded border p-3 space-y-3">
          <h3 className="font-semibold">User Role Assignment</h3>
          <select
            value={selectedUserId ?? ''}
            onChange={(event) => setSelectedUserId(Number(event.target.value))}
            className="border rounded px-2 py-1 w-full"
          >
            {users.map((user) => (
              <option key={user.id} value={user.id}>
                {user.userName} ({user.email})
              </option>
            ))}
          </select>
          {userRoleInfo && <p className="text-xs text-amber-700">{userRoleInfo}</p>}
          <div className="max-h-40 overflow-auto space-y-1 border rounded p-2">
            {roles.map((role) => (
              <label key={role.id} className="flex items-center gap-2 text-sm">
                <input
                  type="checkbox"
                  checked={selectedUserRoleIds.includes(role.id)}
                  onChange={() =>
                    setSelectedUserRoleIds((current) =>
                      current.includes(role.id) ? current.filter((id) => id !== role.id) : [...current, role.id]
                    )
                  }
                  disabled={!canManage || loadingUserRoles}
                />
                <span>{role.name}</span>
              </label>
            ))}
          </div>
          <div className="flex flex-wrap gap-2 text-xs">
            {selectedUserRoleIds.map((roleId) => (
              <span key={roleId} className="rounded bg-blue-100 text-blue-900 px-2 py-0.5">
                {roleNameById.get(roleId) ?? `Role #${roleId}`}
              </span>
            ))}
            {selectedUserRoleIds.length === 0 && <span className="text-slate-500">No roles selected.</span>}
          </div>
          <button
            type="button"
            className="border rounded px-3 py-1"
            disabled={!canManage || savingUserRoles || loadingUserRoles}
            onClick={saveUserRoles}
          >
            {savingUserRoles ? 'Saving...' : 'Save User Roles'}
          </button>
          <div className="max-h-44 overflow-auto border rounded">
            <table className="w-full text-sm">
              <thead className="bg-slate-50">
                <tr>
                  <th className="text-right px-2 py-1">User</th>
                  <th className="text-right px-2 py-1">Assigned Roles</th>
                  <th className="text-right px-2 py-1">Action</th>
                </tr>
              </thead>
              <tbody>
                {users.map((user) => {
                  const assignedRoleIds =
                    user.id === selectedUserId ? selectedUserRoleIds : (userRolesByUserId[user.id] ?? []);
                  const assignedRoleNames = assignedRoleIds.map((roleId) => roleNameById.get(roleId) ?? `#${roleId}`);

                  return (
                    <tr key={user.id} className="border-t">
                      <td className="px-2 py-1">{user.userName}</td>
                      <td className="px-2 py-1">{assignedRoleNames.join(', ') || '-'}</td>
                      <td className="px-2 py-1">
                        <button
                          type="button"
                          className="rounded border px-2 py-0.5"
                          onClick={() => setSelectedUserId(user.id)}
                        >
                          Edit
                        </button>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        </article>
      </div>

      <div className="grid gap-6 lg:grid-cols-2">
        <article className="rounded border p-3 space-y-3">
          <h3 className="font-semibold">Classification ACL</h3>
          <div className="grid gap-3 lg:grid-cols-2">
            <div className="max-h-56 overflow-auto border rounded p-2 space-y-1">
              {sortedClassifications.map((node) => (
                <button
                  key={node.id}
                  type="button"
                  className={`w-full text-right text-sm px-2 py-1 rounded ${
                    selectedClassificationId === node.id ? 'bg-blue-100' : 'hover:bg-gray-100'
                  }`}
                  onClick={() => setSelectedClassificationId(node.id)}
                >
                  <span style={{ paddingInlineStart: `${node.level * 12}px` }}>{node.title}</span>
                </button>
              ))}
            </div>
            <div className="space-y-2">
              <select
                value={classificationSubjectType}
                onChange={(event) => setClassificationSubjectType(Number(event.target.value))}
                className="border rounded px-2 py-1 w-full"
              >
                <option value={1}>User</option>
                <option value={2}>Role</option>
              </select>
              <input
                type="number"
                value={classificationSubjectId || ''}
                onChange={(event) => setClassificationSubjectId(Number(event.target.value))}
                placeholder="Subject ID"
                className="border rounded px-2 py-1 w-full"
              />
              <div className="flex gap-2 text-sm">
                <label className="flex items-center gap-1">
                  <input
                    type="checkbox"
                    checked={(classificationMask & CLASSIFICATION_READ) === CLASSIFICATION_READ}
                    onChange={() => toggleMask(CLASSIFICATION_READ, classificationMask, setClassificationMask)}
                    disabled={!canManage}
                  />
                  Read
                </label>
                <label className="flex items-center gap-1">
                  <input
                    type="checkbox"
                    checked={(classificationMask & CLASSIFICATION_WRITE) === CLASSIFICATION_WRITE}
                    onChange={() => toggleMask(CLASSIFICATION_WRITE, classificationMask, setClassificationMask)}
                    disabled={!canManage}
                  />
                  Write
                </label>
                <label className="flex items-center gap-1">
                  <input
                    type="checkbox"
                    checked={(classificationMask & CLASSIFICATION_MANAGE) === CLASSIFICATION_MANAGE}
                    onChange={() => toggleMask(CLASSIFICATION_MANAGE, classificationMask, setClassificationMask)}
                    disabled={!canManage}
                  />
                  Manage
                </label>
              </div>
              <button type="button" className="border rounded px-3 py-1" disabled={!canManage} onClick={upsertClassificationEntry}>
                Add/Update Entry
              </button>
            </div>
          </div>
          <div className="max-h-40 overflow-auto border rounded p-2 text-sm">
            {classificationAclEntries.map((entry) => (
              <div key={`${entry.subjectType}-${entry.subjectId}`} className="py-1 border-b last:border-b-0">
                {subjectLabel(entry.subjectType)}:{entry.subjectId} - mask {entry.permissionMask}
              </div>
            ))}
          </div>
          <button type="button" className="border rounded px-3 py-1" disabled={!canManage} onClick={saveClassificationAcl}>
            Save Classification ACL
          </button>
        </article>

        <article className="rounded border p-3 space-y-3">
          <h3 className="font-semibold">Tag ACL</h3>
          <div className="flex gap-2">
            <input
              value={newTagName}
              onChange={(event) => setNewTagName(event.target.value)}
              placeholder="New tag"
              className="border rounded px-2 py-1 flex-1"
            />
            <button type="button" className="border rounded px-3 py-1" disabled={!canManage} onClick={createTag}>
              Create
            </button>
          </div>
          <select
            value={selectedTagId ?? ''}
            onChange={(event) => setSelectedTagId(Number(event.target.value))}
            className="border rounded px-2 py-1 w-full"
          >
            {tags.map((tag) => (
              <option key={tag.id} value={tag.id}>
                {tag.name}
              </option>
            ))}
          </select>
          <select
            value={tagSubjectType}
            onChange={(event) => setTagSubjectType(Number(event.target.value))}
            className="border rounded px-2 py-1 w-full"
          >
            <option value={1}>User</option>
            <option value={2}>Role</option>
          </select>
          <input
            type="number"
            value={tagSubjectId || ''}
            onChange={(event) => setTagSubjectId(Number(event.target.value))}
            placeholder="Subject ID"
            className="border rounded px-2 py-1 w-full"
          />
          <div className="flex gap-3 text-sm">
            <label className="flex items-center gap-1">
              <input
                type="checkbox"
                checked={(tagMask & TAG_VIEW) === TAG_VIEW}
                onChange={() => toggleMask(TAG_VIEW, tagMask, setTagMask)}
                disabled={!canManage}
              />
              View
            </label>
            <label className="flex items-center gap-1">
              <input
                type="checkbox"
                checked={(tagMask & TAG_ASSIGN) === TAG_ASSIGN}
                onChange={() => toggleMask(TAG_ASSIGN, tagMask, setTagMask)}
                disabled={!canManage}
              />
              Assign
            </label>
          </div>
          <button type="button" className="border rounded px-3 py-1" disabled={!canManage} onClick={upsertTagEntry}>
            Add/Update Entry
          </button>
          <div className="max-h-32 overflow-auto border rounded p-2 text-sm">
            {tagAclEntries.map((entry) => (
              <div key={`${entry.subjectType}-${entry.subjectId}`} className="py-1 border-b last:border-b-0">
                {subjectLabel(entry.subjectType)}:{entry.subjectId} - mask {entry.permissionMask}
              </div>
            ))}
          </div>
          <button type="button" className="border rounded px-3 py-1" disabled={!canManage} onClick={saveTagAcl}>
            Save Tag ACL
          </button>
        </article>
      </div>
    </section>
  );
}
