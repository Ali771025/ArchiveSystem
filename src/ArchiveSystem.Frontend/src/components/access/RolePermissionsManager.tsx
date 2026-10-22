import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  Alert,
  Button,
  Checkbox,
  FormControlLabel,
  MenuItem,
  Paper,
  Select,
  Stack,
  TextField,
  Typography,
  useMediaQuery,
  useTheme,
} from '@mui/material';
import { accessControlApi } from '@/services/accessControlApi';
import { permissionGroups, softwareTabPermissions } from './types';

const schema = z.object({
  roleName: z.string().min(1, 'انتخاب نقش الزامی است.'),
  permissions: z.array(z.string()),
});

type FormValues = z.infer<typeof schema>;

const allPermissionKeys = [
  ...permissionGroups.flatMap((group) => group.permissions.map((item) => item.key)),
  ...softwareTabPermissions.map((item) => item.key),
];

interface RolePermissionsManagerProps {
  onRolesChanged?: () => void;
}

export function RolePermissionsManager({ onRolesChanged }: RolePermissionsManagerProps) {
  const theme = useTheme();
  const isCompact = useMediaQuery(theme.breakpoints.down('sm'));
  const queryClient = useQueryClient();
  const [newRoleName, setNewRoleName] = useState('');

  const rolesQuery = useQuery({
    queryKey: ['identity-roles'],
    queryFn: accessControlApi.getRoles,
  });

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      roleName: '',
      permissions: [],
    },
  });

  const selectedRoleName = form.watch('roleName');
  const selectedPermissions = form.watch('permissions');
  const selectedRoleHasFullAccess = ['admin', 'superuser'].includes(selectedRoleName.trim().toLowerCase());

  const rolePermissionsQuery = useQuery({
    queryKey: ['identity-role-permissions', selectedRoleName],
    queryFn: () => accessControlApi.getIdentityRolePermissions(selectedRoleName),
    enabled: Boolean(selectedRoleName) && !selectedRoleHasFullAccess,
  });
  const isLoadingEditableRolePermissions =
    Boolean(selectedRoleName) && !selectedRoleHasFullAccess && rolePermissionsQuery.isFetching;

  useEffect(() => {
    if (!rolesQuery.data || rolesQuery.data.length === 0) {
      return;
    }

    if (!selectedRoleName) {
      form.setValue('roleName', rolesQuery.data[0].name, { shouldValidate: true });
    }
  }, [form, rolesQuery.data, selectedRoleName]);

  useEffect(() => {
    if (!selectedRoleName) {
      return;
    }

    if (selectedRoleHasFullAccess) {
      form.setValue('permissions', allPermissionKeys, { shouldDirty: false });
      return;
    }

    if (!rolePermissionsQuery.data) {
      return;
    }

    const normalized = rolePermissionsQuery.data
      .filter((permission): permission is (typeof allPermissionKeys)[number] =>
        allPermissionKeys.includes(permission as (typeof allPermissionKeys)[number]),
      );

    form.setValue('permissions', normalized, { shouldDirty: false });
  }, [form, rolePermissionsQuery.data, selectedRoleHasFullAccess, selectedRoleName]);

  const updateMutation = useMutation({
    mutationFn: async (values: FormValues) => {
      await accessControlApi.updateIdentityRolePermissions(values.roleName, values.permissions);
    },
    onSuccess: async (_data, variables) => {
      await queryClient.invalidateQueries({ queryKey: ['identity-role-permissions', variables.roleName] });
    },
  });

  const createRoleMutation = useMutation({
    mutationFn: async (roleName: string) => {
      return await accessControlApi.createRole(roleName, false);
    },
    onSuccess: async (createdRole) => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['identity-roles'] }),
        queryClient.invalidateQueries({ queryKey: ['roles'] }),
      ]);

      form.setValue('roleName', createdRole.name, { shouldValidate: true });
      form.setValue('permissions', [], { shouldDirty: false, shouldValidate: true });
      setNewRoleName('');
      onRolesChanged?.();
    },
  });

  const togglePermission = (permission: string, checked: boolean) => {
    const current = form.getValues('permissions');
    const next = checked
      ? Array.from(new Set([...current, permission]))
      : current.filter((item) => item !== permission);

    form.setValue('permissions', next, { shouldDirty: true, shouldValidate: true });
  };

  const onSubmit = form.handleSubmit(async (values) => {
    if (selectedRoleHasFullAccess) {
      return;
    }

    await updateMutation.mutateAsync(values);
  });

  const createRole = async () => {
    const normalized = newRoleName.trim();
    if (!normalized) {
      return;
    }

    await createRoleMutation.mutateAsync(normalized);
  };

  return (
    <Paper
      variant="outlined"
      sx={{
        p: { xs: 1.5, md: 2.5 },
        borderRadius: 2,
        bgcolor: 'background.paper',
      }}
      dir="rtl"
    >
      <Stack spacing={2} component="form" onSubmit={onSubmit}>
        <Stack spacing={0.5} sx={{ pb: 1.5, borderBottom: '1px solid', borderColor: 'divider' }}>
          <Typography variant="h6" fontWeight={700}>مدیریت مجوزهای نقش</Typography>
          <Typography variant="body2" color="text.secondary">
            مجوزهای مبتنی بر Claim را برای هر نقش تنظیم کنید.
          </Typography>
        </Stack>

        <Stack spacing={0.75}>
          <Typography variant="subtitle2" fontWeight={700}>ایجاد نقش سفارشی</Typography>
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1}>
            <TextField
              size="small"
              value={newRoleName}
              onChange={(event) => setNewRoleName(event.target.value)}
              onKeyDown={(event) => {
                if (event.key === 'Enter') {
                  event.preventDefault();
                  void createRole();
                }
              }}
              placeholder="نام نقش جدید"
              fullWidth
            />
            <Button
              type="button"
              variant="outlined"
              onClick={createRole}
              disabled={createRoleMutation.isPending || !newRoleName.trim()}
            >
              {createRoleMutation.isPending ? 'در حال ایجاد...' : 'ایجاد نقش'}
            </Button>
          </Stack>
        </Stack>

        <Stack spacing={0.75}>
          <Typography variant="subtitle2" fontWeight={700}>نقش</Typography>
          <Select
            size="small"
            value={selectedRoleName}
            onChange={(event) => form.setValue('roleName', String(event.target.value), { shouldValidate: true })}
            disabled={rolesQuery.isPending || rolesQuery.isError}
          >
            {rolesQuery.data?.map((role) => (
              <MenuItem key={role.id} value={role.name}>
                {role.name}
              </MenuItem>
            ))}
          </Select>
          {form.formState.errors.roleName && (
            <Typography variant="caption" color="error">{form.formState.errors.roleName.message}</Typography>
          )}
        </Stack>

        <Stack direction={{ xs: 'column', md: 'row' }} spacing={1.5} flexWrap="wrap" useFlexGap>
          {permissionGroups.map((group) => (
            <Paper
              key={group.module}
              variant="outlined"
              sx={{
                p: 1.25,
                flex: { xs: '1 1 100%', md: '1 1 calc(50% - 12px)', xl: '1 1 calc(33.333% - 12px)' },
                borderRadius: 1.5,
              }}
            >
              <Stack spacing={1}>
                <Typography variant="subtitle2" fontWeight={700}>{group.title}</Typography>
                {group.permissions.map((permission) => {
                  const checked = selectedPermissions.includes(permission.key);
                  return (
                    <Paper
                      key={permission.key}
                      variant="outlined"
                      sx={{ p: 1, borderRadius: 1, borderColor: checked ? 'primary.main' : 'divider' }}
                    >
                      <FormControlLabel
                        sx={{ alignItems: 'flex-start', m: 0, width: '100%' }}
                        control={
                          <Checkbox
                            size="small"
                            checked={checked}
                            disabled={selectedRoleHasFullAccess}
                            onChange={(event) => togglePermission(permission.key, event.target.checked)}
                          />
                        }
                        label={
                          <Stack spacing={0.25}>
                            <Typography variant="body2" fontWeight={700}>{permission.label}</Typography>
                            <Typography variant="caption" color="text.secondary">{permission.description}</Typography>
                            {/* <Typography
                              variant="caption"
                              sx={{
                                display: 'inline-block',
                                px: 0.75,
                                py: 0.125,
                                borderRadius: 4,
                                bgcolor: 'action.hover',
                                width: 'fit-content',
                              }}
                            >
                              {permission.key}
                            </Typography> */}
                          </Stack>
                        }
                      />
                    </Paper>
                  );
                })}
              </Stack>
            </Paper>
          ))}
        </Stack>

        <Paper variant="outlined" sx={{ p: 1.5, borderRadius: 1.5 }}>
          <Stack spacing={1}>
            <Typography variant="h6" fontWeight={700}>مدیریت تب‌های نرم‌افزار</Typography>
            <Typography variant="body2" color="text.secondary">
              تب‌هایی را که این نقش اجازه مشاهده آن‌ها را دارد انتخاب کنید.
            </Typography>
            <Stack direction={{ xs: 'column', md: 'row' }} spacing={1} flexWrap="wrap" useFlexGap>
              {softwareTabPermissions.map((permission) => {
                const checked = selectedPermissions.includes(permission.key);
                return (
                  <Paper
                    key={permission.key}
                    variant="outlined"
                    sx={{ p: 1, flex: '1 1 220px', borderColor: checked ? 'primary.main' : 'divider' }}
                  >
                    <FormControlLabel
                      sx={{ alignItems: 'flex-start', m: 0 }}
                      control={
                        <Checkbox
                          size="small"
                          checked={checked}
                          disabled={selectedRoleHasFullAccess}
                          onChange={(event) => togglePermission(permission.key, event.target.checked)}
                        />
                      }
                      label={
                        <Stack spacing={0.25}>
                          <Typography variant="body2" fontWeight={700}>{permission.label}</Typography>
                          <Typography variant="caption" color="text.secondary">{permission.description}</Typography>
                        </Stack>
                      }
                    />
                  </Paper>
                );
              })}
            </Stack>
          </Stack>
        </Paper>

        {(rolesQuery.isPending || isLoadingEditableRolePermissions) && (
          <Alert severity="info">در حال بارگذاری مجوزها...</Alert>
        )}
        {selectedRoleHasFullAccess && (
          <Alert severity="info">نقش مدیر سامانه به‌صورت سیستمی به همه مجوزها دسترسی دارد.</Alert>
        )}
        {(rolesQuery.isError || rolePermissionsQuery.isError) && (
          <Alert severity="error">بارگذاری مجوزهای نقش ناموفق بود.</Alert>
        )}
        {updateMutation.isError && (
          <Alert severity="error">ذخیره مجوزهای نقش ناموفق بود.</Alert>
        )}
        {updateMutation.isSuccess && (
          <Alert severity="success">مجوزهای نقش با موفقیت ذخیره شد.</Alert>
        )}
        {createRoleMutation.isError && (
          <Alert severity="error">ایجاد نقش سفارشی ناموفق بود.</Alert>
        )}
        {createRoleMutation.isSuccess && (
          <Alert severity="success">نقش سفارشی با موفقیت ایجاد شد.</Alert>
        )}

        <Button
          type="submit"
          variant="contained"
          disabled={updateMutation.isPending || !selectedRoleName || selectedRoleHasFullAccess}
          fullWidth={isCompact}
          sx={{ alignSelf: isCompact ? 'stretch' : 'flex-start' }}
        >
          {updateMutation.isPending ? 'در حال ذخیره...' : 'ذخیره مجوزهای نقش'}
        </Button>
      </Stack>
    </Paper>
  );
}
