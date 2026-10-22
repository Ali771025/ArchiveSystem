import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { usersApi } from '@/features/user-management/api/usersApi';
import { useToastStore } from '@/stores/useToastStore';
import type { CreateUserPayload, ResetUserPasswordPayload, UpdateUserPayload, UserListQueryParams } from '@/features/user-management/types';

export const userQueryKeys = {
  users: (query: UserListQueryParams) => ['users-v1', query] as const,
  userById: (userId: number) => ['users-v1', userId] as const,
  personnelSearch: (term: string) => ['personnel-search', term] as const,
};

export type UserQueryKeys = typeof userQueryKeys;

export const useUsersQuery = (query: UserListQueryParams) =>
  useQuery({
    queryKey: userQueryKeys.users(query),
    queryFn: () => usersApi.getUsers(query),
    placeholderData: keepPreviousData,
  });

export const useUserByIdQuery = (userId: number | null, enabled = true) =>
  useQuery({
    queryKey: userId ? userQueryKeys.userById(userId) : ['users-v1', 'null'],
    queryFn: () => usersApi.getUserById(userId ?? 0),
    enabled: enabled && Boolean(userId),
  });

export const usePersonnelSearchQuery = (term: string) =>
  useQuery({
    queryKey: userQueryKeys.personnelSearch(term),
    queryFn: () => usersApi.searchPersonnel(term),
    enabled: term.trim().length >= 2,
    placeholderData: keepPreviousData,
  });

export const useCreateUserMutation = () => {
  const queryClient = useQueryClient();
  const pushToast = useToastStore((state) => state.push);

  return useMutation({
    mutationFn: (payload: CreateUserPayload) => usersApi.createUser(payload),
    onSuccess: () => {
      pushToast('کاربر با موفقیت ایجاد شد.', 'success');
      queryClient.invalidateQueries({ queryKey: ['users-v1'] });
    },
  });
};

export const useUpdateUserMutation = () => {
  const queryClient = useQueryClient();
  const pushToast = useToastStore((state) => state.push);

  return useMutation({
    mutationFn: ({ userId, payload }: { userId: number; payload: UpdateUserPayload }) => usersApi.updateUser(userId, payload),
    onSuccess: async (_result, variables) => {
      pushToast('کاربر با موفقیت ویرایش شد.', 'success');
      await Promise.all([
        queryClient.refetchQueries({ queryKey: ['users-v1'], type: 'active' }),
        queryClient.invalidateQueries({ queryKey: userQueryKeys.userById(variables.userId) }),
      ]);
    },
  });
};

export const useDeleteUserMutation = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (userId: number) => usersApi.deleteUser(userId),
    onSuccess: async () => {
      await queryClient.refetchQueries({ queryKey: ['users-v1'], type: 'active' });
    },
  });
};

export const useSetUserStatusMutation = () => {
  const queryClient = useQueryClient();
  const pushToast = useToastStore((state) => state.push);

  return useMutation({
    mutationFn: ({ userId, isActive }: { userId: number; isActive: boolean }) =>
      usersApi.setUserStatus(userId, isActive),
    onSuccess: async (_result, variables) => {
      pushToast(variables.isActive ? 'کاربر با موفقیت فعال شد.' : 'کاربر با موفقیت غیرفعال شد.', 'success');
      await Promise.all([
        queryClient.refetchQueries({ queryKey: ['users-v1'], type: 'active' }),
        queryClient.invalidateQueries({ queryKey: userQueryKeys.userById(variables.userId) }),
      ]);
    },
  });
};

export const useResetUserPasswordMutation = () => {
  const queryClient = useQueryClient();
  const pushToast = useToastStore((state) => state.push);

  return useMutation({
    mutationFn: ({ userId, payload }: { userId: number; payload: ResetUserPasswordPayload }) =>
      usersApi.resetPassword(userId, payload),
    onSuccess: async (_result, variables) => {
      pushToast('رمز عبور کاربر با موفقیت تغییر کرد.', 'success');
      await Promise.all([
        queryClient.refetchQueries({ queryKey: ['users-v1'], type: 'active' }),
        queryClient.invalidateQueries({ queryKey: userQueryKeys.userById(variables.userId) }),
      ]);
    },
  });
};
