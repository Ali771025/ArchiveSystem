import AddIcon from '@mui/icons-material/Add';
import RefreshIcon from '@mui/icons-material/Refresh';
import { Alert, Box, Button, Paper, Stack, Typography } from '@mui/material';
import { useMemo, useState } from 'react';
import type { GridFilterModel, GridPaginationModel, GridSortModel } from '@mui/x-data-grid';
import { UserFiltersSidebar } from '@/features/user-management/components/organisms/UserFiltersSidebar';
import { UserFormDialog } from '@/features/user-management/components/organisms/UserFormDialog';
import { ResetUserPasswordDialog } from '@/features/user-management/components/organisms/ResetUserPasswordDialog';
import { UsersDataGrid } from '@/features/user-management/components/organisms/UsersDataGrid';
import { BackupManagementPanel } from '@/features/backups/components/BackupManagementPanel';
import {
  useCreateUserMutation,
  useSetUserStatusMutation,
  useResetUserPasswordMutation,
  useUpdateUserMutation,
  useUsersQuery,
} from '@/features/user-management/hooks/useUsersQuery';
import { useUserManagementStore } from '@/features/user-management/stores/useUserManagementStore';
import { mapApiError } from '@/services/errorMapper';
import { useToastStore } from '@/stores/useToastStore';
import { useDebouncedValue } from '@/utils/useDebouncedValue';
import type { UserDTO, UserListItemDTO, UserListQueryParams } from '@/features/user-management/types';
import { useConfirm } from '@/components/common/useConfirm';
import { buildDeleteConfirmOptions } from '@/components/common/confirmPresets';
import { getServerColumnFilter } from '@/utils/gridColumnFilter';

export const UserManagementTemplate = () => {
  const gridSortFieldToApi: Record<string, string> = {
    username: 'UserName',
    email: 'Email',
    activeDuration: 'CreatedAt',
    autoBackupEnabled: 'CreatedAt',
    isActive: 'IsActive',
  };

  const apiSortFieldToGrid: Record<string, string> = {
    UserName: 'username',
    Email: 'email',
    IsActive: 'isActive',
  };

  const {
    filters,
    pageNumber,
    pageSize,
    sortBy,
    sortDescending,
    isDialogOpen,
    selectedUser,
    setFilter,
    resetFilters,
    setPagination,
    setSorting,
    openCreateDialog,
    openEditDialog,
    closeDialog,
  } = useUserManagementStore();
  const pushToast = useToastStore((state) => state.push);

  const debouncedFilters = useDebouncedValue(filters, 450);
  const [gridFilterModel, setGridFilterModel] = useState<GridFilterModel>({ items: [] });
  const debouncedGridFilter = useDebouncedValue(gridFilterModel, 350);

  const query: UserListQueryParams = useMemo(
    () => ({
      pageNumber,
      pageSize,
      sortBy,
      sortDescending,
      username: debouncedFilters.username,
      firstName: debouncedFilters.firstName,
      lastName: debouncedFilters.lastName,
      nationalId: debouncedFilters.nationalId,
      ...getServerColumnFilter(debouncedGridFilter),
    }),
    [debouncedFilters, debouncedGridFilter, pageNumber, pageSize, sortBy, sortDescending],
  );

  const usersQuery = useUsersQuery(query);
  const createMutation = useCreateUserMutation();
  const updateMutation = useUpdateUserMutation();
  const statusMutation = useSetUserStatusMutation();
  const resetPasswordMutation = useResetUserPasswordMutation();
  const confirm = useConfirm();
  const [passwordResetUser, setPasswordResetUser] = useState<UserListItemDTO | null>(null);

  const isSubmitting = createMutation.isPending || updateMutation.isPending;
  const isPasswordResetSubmitting = resetPasswordMutation.isPending;

  const paginationModel: GridPaginationModel = useMemo(
    () => ({ page: pageNumber - 1, pageSize }),
    [pageNumber, pageSize],
  );

  const sortModel: GridSortModel = useMemo(() => {
    const mappedField = apiSortFieldToGrid[sortBy];
    if (!mappedField) {
      return [];
    }
    return [{ field: mappedField, sort: sortDescending ? 'desc' : 'asc' }];
  }, [sortBy, sortDescending]);

  return (
    <Stack spacing={1.5}>
      <Paper variant="outlined" sx={{ p: 2, borderRadius: 2 }}>
        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1} justifyContent="space-between" alignItems={{ xs: 'stretch', sm: 'center' }}>
          <Typography variant="h5" fontWeight={700}>
            مدیریت کاربران
          </Typography>
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1}>
            <Button variant="outlined" startIcon={<RefreshIcon />} onClick={() => usersQuery.refetch()} disabled={usersQuery.isFetching}>
              بازخوانی
            </Button>
            <Button variant="contained" startIcon={<AddIcon />} onClick={openCreateDialog}>
              کاربر جدید
            </Button>
          </Stack>
        </Stack>
      </Paper>

      {/* <BackupManagementPanel /> */}

      <Stack spacing={1.5} alignItems="stretch">
        <Box sx={{ width: '100%', minWidth: 0 }}>
          <UserFiltersSidebar filters={filters} onFilterChange={setFilter} onResetFilters={resetFilters} />
        </Box>

        <Box sx={{ minWidth: 0, width: '100%' }}>
          {usersQuery.isError ? (
            <Alert severity="error">
              {mapApiError(usersQuery.error).message || 'بارگذاری کاربران ناموفق بود.'}
            </Alert>
          ) : (
            <UsersDataGrid
              data={usersQuery.data}
              isLoading={usersQuery.isLoading || usersQuery.isFetching}
              paginationModel={paginationModel}
              onPaginationModelChange={(model) =>
                setPagination(
                  model.page + 1,
                  model.pageSize || paginationModel.pageSize,
                )
              }
              filterModel={gridFilterModel}
              onFilterModelChange={(model) => {
                setGridFilterModel(model);
                setPagination(1, pageSize);
              }}
              sortModel={sortModel}
              onSortModelChange={(model) => {
                const next = model[0];
                if (next?.field && next.sort) {
                  setSorting(gridSortFieldToApi[next.field] ?? 'CreatedAt', next.sort !== 'asc');
                } else {
                  setSorting('CreatedAt', true);
                }
              }}
              onEdit={(user) => {
                const mappedUser: UserDTO = {
                  id: user.id,
                  username: user.username,
                  email: user.email,
                  phoneNumber: user.phoneNumber,
                  firstName: user.firstName,
                  lastName: user.lastName,
                  nationalId: user.nationalId,
                  personnelCode: user.personnelCode,
                  isActive: user.isActive,
                  twoFactorEnabled: user.twoFactorEnabled,
                  autoBackupEnabled: user.autoBackupEnabled,
                  activeDuration: user.activeDuration,
                  createdAt: new Date().toISOString(),
                };
                openEditDialog(mappedUser);
              }}
              onResetPassword={(user) => setPasswordResetUser(user)}
              onToggleStatus={async (user) => {
                const nextStatus = !user.isActive;
                const shouldChange = await confirm({
                  ...buildDeleteConfirmOptions(`کاربر ${user.username}`),
                  title: nextStatus ? 'فعال‌سازی کاربر' : 'غیرفعال‌سازی کاربر',
                  message: `آیا از ${nextStatus ? 'فعال‌سازی' : 'غیرفعال‌سازی'} کاربر ${user.username} مطمئن هستید؟`,
                  confirmText: nextStatus ? 'بله، فعال کن' : 'بله، غیرفعال کن',
                });
                if (!shouldChange) {
                  return;
                }
                statusMutation.mutate({ userId: user.id, isActive: nextStatus }, {
                  onError: (error) => {
                    const mapped = mapApiError(error);
                    pushToast(mapped.message, 'error');
                  },
                });
              }}
            />
          )}
        </Box>
      </Stack>

      <UserFormDialog
        open={isDialogOpen}
        selectedUser={selectedUser}
        onClose={closeDialog}
        onCreate={async (payload) => {
          await createMutation.mutateAsync(payload, {
            onError: (error) => {
              const mapped = mapApiError(error);
              pushToast(mapped.message, 'error');
            },
          });
        }}
        onUpdate={async (payload) => {
          if (!selectedUser) {
            return;
          }
          await updateMutation.mutateAsync(
            { userId: selectedUser.id, payload },
            {
              onError: (error) => {
                const mapped = mapApiError(error);
                pushToast(mapped.message, 'error');
              },
            },
          );
        }}
        isSubmitting={isSubmitting}
      />

      <ResetUserPasswordDialog
        open={Boolean(passwordResetUser)}
        user={passwordResetUser}
        isSubmitting={isPasswordResetSubmitting}
        onClose={() => setPasswordResetUser(null)}
        onSubmit={async (newPassword) => {
          if (!passwordResetUser) {
            return;
          }

          await resetPasswordMutation.mutateAsync(
            { userId: passwordResetUser.id, payload: { newPassword } },
            {
              onError: (error) => {
                const mapped = mapApiError(error);
                pushToast(mapped.message, 'error');
              },
            },
          );
        }}
      />
    </Stack>
  );
};
