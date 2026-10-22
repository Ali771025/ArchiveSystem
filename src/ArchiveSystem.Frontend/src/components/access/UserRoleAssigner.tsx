import { zodResolver } from '@hookform/resolvers/zod';
import {
  Alert,
  Autocomplete,
  Box,
  Button,
  Checkbox,
  Divider,
  List,
  ListItem,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Paper,
  Skeleton,
  Stack,
  TextField,
  Typography,
  useMediaQuery,
  useTheme,
} from '@mui/material';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useEffect, useMemo, useState, type KeyboardEvent } from 'react';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import { accessControlApi } from '@/services/accessControlApi';
import type { AdminUserDto, RoleDto } from '@/types/accessControl';

const schema = z.object({
  userId: z.number().positive('انتخاب کاربر الزامی است.'),
  roleNames: z.array(z.string()),
});

type FormValues = z.infer<typeof schema>;

const identityUserRolesKey = (userId: number) => ['identity-user-roles', userId] as const;

const roleDescription = (role: RoleDto): string => {
  if (role.description && role.description.trim().length > 0) {
    return role.description;
  }

  return role.isSystem ? 'نقش سیستمی' : 'نقش سفارشی';
};

export function UserRoleAssigner() {
  const theme = useTheme();
  const isCompact = useMediaQuery(theme.breakpoints.down('md'));
  const queryClient = useQueryClient();

  const [availableSearch, setAvailableSearch] = useState('');
  const [assignedSearch, setAssignedSearch] = useState('');
  const [selectedAvailable, setSelectedAvailable] = useState<string[]>([]);
  const [selectedAssigned, setSelectedAssigned] = useState<string[]>([]);

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      userId: 0,
      roleNames: [],
    },
  });

  const usersQuery = useQuery({
    queryKey: ['identity-users'],
    queryFn: accessControlApi.getUsers,
  });

  const rolesQuery = useQuery({
    queryKey: ['identity-roles'],
    queryFn: accessControlApi.getRoles,
  });

  const selectedUserId = form.watch('userId');
  const selectedRoleNames = form.watch('roleNames');

  const userRolesQuery = useQuery({
    queryKey: identityUserRolesKey(selectedUserId),
    queryFn: () => accessControlApi.getIdentityUserRoles(selectedUserId),
    enabled: selectedUserId > 0,
  });

  useEffect(() => {
    if (!userRolesQuery.data) {
      return;
    }

    form.setValue('roleNames', userRolesQuery.data, { shouldDirty: false, shouldValidate: true });
    setSelectedAvailable([]);
    setSelectedAssigned([]);
  }, [form, userRolesQuery.data]);

  const users = usersQuery.data ?? [];
  const roles = rolesQuery.data ?? [];

  const selectedUser = useMemo(
    () => users.find((user) => user.id === selectedUserId) ?? null,
    [selectedUserId, users],
  );

  const assignedRoleSet = useMemo(() => new Set(selectedRoleNames), [selectedRoleNames]);

  const availableRoles = useMemo(() => {
    return roles.filter((role) => !assignedRoleSet.has(role.name));
  }, [assignedRoleSet, roles]);

  const assignedRoles = useMemo(() => {
    return roles.filter((role) => assignedRoleSet.has(role.name));
  }, [assignedRoleSet, roles]);

  const filteredAvailableRoles = useMemo(() => {
    const term = availableSearch.trim().toLowerCase();
    if (!term) {
      return availableRoles;
    }

    return availableRoles.filter((role) => {
      const description = roleDescription(role).toLowerCase();
      return role.name.toLowerCase().includes(term) || description.includes(term);
    });
  }, [availableRoles, availableSearch]);

  const filteredAssignedRoles = useMemo(() => {
    const term = assignedSearch.trim().toLowerCase();
    if (!term) {
      return assignedRoles;
    }

    return assignedRoles.filter((role) => {
      const description = roleDescription(role).toLowerCase();
      return role.name.toLowerCase().includes(term) || description.includes(term);
    });
  }, [assignedRoles, assignedSearch]);

  const updateMutation = useMutation({
    mutationFn: async (values: FormValues) => {
      await accessControlApi.updateIdentityUserRoles(values.userId, values.roleNames);
    },
    onMutate: async (values) => {
      await queryClient.cancelQueries({ queryKey: identityUserRolesKey(values.userId) });
      const previous = queryClient.getQueryData<string[]>(identityUserRolesKey(values.userId));
      queryClient.setQueryData(identityUserRolesKey(values.userId), values.roleNames);
      return { previous, userId: values.userId };
    },
    onError: (_error, _values, context) => {
      if (!context) {
        return;
      }

      queryClient.setQueryData(identityUserRolesKey(context.userId), context.previous ?? []);
    },
    onSettled: async (_data, _error, values) => {
      await queryClient.invalidateQueries({ queryKey: identityUserRolesKey(values.userId) });
    },
  });

  const assignSelected = () => {
    if (selectedAvailable.length === 0) {
      return;
    }

    form.setValue('roleNames', Array.from(new Set([...selectedRoleNames, ...selectedAvailable])), {
      shouldDirty: true,
      shouldValidate: true,
    });
    setSelectedAvailable([]);
  };

  const unassignSelected = () => {
    if (selectedAssigned.length === 0) {
      return;
    }

    form.setValue(
      'roleNames',
      selectedRoleNames.filter((roleName) => !selectedAssigned.includes(roleName)),
      { shouldDirty: true, shouldValidate: true },
    );
    setSelectedAssigned([]);
  };

  const assignAllVisible = () => {
    const allVisible = filteredAvailableRoles.map((role) => role.name);
    if (allVisible.length === 0) {
      return;
    }

    form.setValue('roleNames', Array.from(new Set([...selectedRoleNames, ...allVisible])), {
      shouldDirty: true,
      shouldValidate: true,
    });
    setSelectedAvailable([]);
  };

  const unassignAllVisible = () => {
    const visibleSet = new Set(filteredAssignedRoles.map((role) => role.name));
    form.setValue(
      'roleNames',
      selectedRoleNames.filter((roleName) => !visibleSet.has(roleName)),
      { shouldDirty: true, shouldValidate: true },
    );
    setSelectedAssigned([]);
  };

  const toggleAvailableSelection = (roleName: string) => {
    setSelectedAvailable((current) =>
      current.includes(roleName) ? current.filter((item) => item !== roleName) : [...current, roleName],
    );
  };

  const toggleAssignedSelection = (roleName: string) => {
    setSelectedAssigned((current) =>
      current.includes(roleName) ? current.filter((item) => item !== roleName) : [...current, roleName],
    );
  };

  const onSubmit = form.handleSubmit(async (values) => {
    await updateMutation.mutateAsync(values);
  });

  const stopSearchEnterSubmit = (event: KeyboardEvent<HTMLInputElement>) => {
    if (event.key === 'Enter') {
      event.preventDefault();
    }
  };

  const getUserDisplayName = (user: AdminUserDto) => {
    const fullName = [user.firstName, user.lastName].filter(Boolean).join(' ').trim();
    return fullName || user.userName || user.email;
  };

  const getUserOptionLabel = (user: AdminUserDto) => getUserDisplayName(user);

  const userMatchesSearch = (options: AdminUserDto[], state: { inputValue: string }) => {
    const term = state.inputValue.trim().toLowerCase();
    if (!term) {
      return options;
    }

    return options.filter((user) => {
      const userName = user.userName.toLowerCase();
      const email = user.email.toLowerCase();
      const firstName = user.firstName?.toLowerCase() ?? '';
      const lastName = user.lastName?.toLowerCase() ?? '';
      const fullName = getUserDisplayName(user).toLowerCase();
      return (
        fullName.includes(term) ||
        firstName.includes(term) ||
        lastName.includes(term) ||
        userName.includes(term) ||
        email.includes(term)
      );
    });
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
          <Typography variant="h6" fontWeight={700}>تخصیص نقش به کاربر</Typography>
          <Typography variant="body2" color="text.secondary">
            نقش‌ها را با لیست انتقالی جستجو و مدیریت کنید.
          </Typography>
        </Stack>

        <Stack spacing={1}>
          <Autocomplete
            size="small"
            options={users}
            value={selectedUser}
            getOptionLabel={getUserOptionLabel}
            filterOptions={userMatchesSearch}
            isOptionEqualToValue={(option, value) => option.id === value.id}
            onChange={(_event, user) => {
              form.setValue('userId', user?.id ?? 0, { shouldDirty: true, shouldValidate: true });
              form.setValue('roleNames', [], { shouldDirty: true, shouldValidate: true });
              setSelectedAvailable([]);
              setSelectedAssigned([]);
            }}
            clearOnEscape
            disabled={usersQuery.isPending || usersQuery.isError}
            noOptionsText="کاربری یافت نشد."
            renderInput={(params) => (
              <TextField
                {...params}
                label="کاربر"
                placeholder="نام یا نام خانوادگی را تایپ کنید"
                error={Boolean(form.formState.errors.userId)}
                helperText={form.formState.errors.userId?.message}
                onKeyDown={stopSearchEnterSubmit}
              />
            )}
          />
          <Typography variant="caption" color="text.secondary">
            برای انتخاب کاربر، داخل همین فیلد تایپ کنید.
          </Typography>
        </Stack>

        <Stack
          direction={{ xs: 'column', md: 'row' }}
          spacing={1.5}
          alignItems="stretch"
          sx={{ minHeight: 320 }}
        >
          <Paper variant="outlined" sx={{ flex: 1, p: 1, display: 'flex', flexDirection: 'column', gap: 1 }}>
            <Typography variant="subtitle2" fontWeight={700}>نقش‌های قابل تخصیص</Typography>
            <TextField
              size="small"
              value={availableSearch}
              onChange={(event) => setAvailableSearch(event.target.value)}
              onKeyDown={stopSearchEnterSubmit}
              placeholder="جستجو در نقش‌ها"
              inputProps={{ 'aria-label': 'جستجو در نقش‌های قابل تخصیص' }}
            />
            <Divider />
            <Box sx={{ flex: 1, minHeight: 180, overflowY: 'auto' }}>
              {rolesQuery.isPending || userRolesQuery.isPending ? (
                <Stack spacing={1}>
                  <Skeleton variant="rounded" height={44} />
                  <Skeleton variant="rounded" height={44} />
                  <Skeleton variant="rounded" height={44} />
                </Stack>
              ) : (
                <List dense aria-label="available roles" role="listbox">
                  {filteredAvailableRoles.map((role) => {
                    const checked = selectedAvailable.includes(role.name);
                    return (
                      <ListItem key={role.id} disablePadding>
                        <ListItemButton onClick={() => toggleAvailableSelection(role.name)} role="option" selected={checked}>
                          <ListItemIcon>
                            <Checkbox edge="start" checked={checked} tabIndex={-1} disableRipple />
                          </ListItemIcon>
                          <ListItemText
                            primary={role.name}
                            secondary={roleDescription(role)}
                            primaryTypographyProps={{ fontWeight: 700 }}
                          />
                        </ListItemButton>
                      </ListItem>
                    );
                  })}
                </List>
              )}
            </Box>
          </Paper>

          <Stack justifyContent="center" spacing={1} sx={{ minWidth: isCompact ? '100%' : 130 }}>
            <Button variant="outlined" onClick={assignSelected} disabled={selectedAvailable.length === 0}>
              افزودن {'>'}
            </Button>
            <Button variant="outlined" onClick={unassignSelected} disabled={selectedAssigned.length === 0}>
              {'<'} حذف
            </Button>
            <Button variant="text" onClick={assignAllVisible} disabled={filteredAvailableRoles.length === 0}>
              افزودن همه
            </Button>
            <Button variant="text" onClick={unassignAllVisible} disabled={filteredAssignedRoles.length === 0}>
              حذف همه
            </Button>
          </Stack>

          <Paper variant="outlined" sx={{ flex: 1, p: 1, display: 'flex', flexDirection: 'column', gap: 1 }}>
            <Typography variant="subtitle2" fontWeight={700}>نقش‌های تخصیص‌یافته</Typography>
            <TextField
              size="small"
              value={assignedSearch}
              onChange={(event) => setAssignedSearch(event.target.value)}
              onKeyDown={stopSearchEnterSubmit}
              placeholder="جستجو در نقش‌های تخصیص‌یافته"
              inputProps={{ 'aria-label': 'جستجو در نقش‌های تخصیص‌یافته' }}
            />
            <Divider />
            <Box sx={{ flex: 1, minHeight: 180, overflowY: 'auto' }}>
              {rolesQuery.isPending || userRolesQuery.isPending ? (
                <Stack spacing={1}>
                  <Skeleton variant="rounded" height={44} />
                  <Skeleton variant="rounded" height={44} />
                  <Skeleton variant="rounded" height={44} />
                </Stack>
              ) : (
                <List dense aria-label="assigned roles" role="listbox">
                  {filteredAssignedRoles.map((role) => {
                    const checked = selectedAssigned.includes(role.name);
                    return (
                      <ListItem key={role.id} disablePadding>
                        <ListItemButton onClick={() => toggleAssignedSelection(role.name)} role="option" selected={checked}>
                          <ListItemIcon>
                            <Checkbox edge="start" checked={checked} tabIndex={-1} disableRipple />
                          </ListItemIcon>
                          <ListItemText
                            primary={role.name}
                            secondary={roleDescription(role)}
                            primaryTypographyProps={{ fontWeight: 700 }}
                          />
                        </ListItemButton>
                      </ListItem>
                    );
                  })}
                </List>
              )}
            </Box>
          </Paper>
        </Stack>

        {(usersQuery.isPending || rolesQuery.isPending || userRolesQuery.isPending) && (
          <Alert severity="info">در حال بارگذاری کاربران و نقش‌ها...</Alert>
        )}
        {(usersQuery.isError || rolesQuery.isError || userRolesQuery.isError) && (
          <Alert severity="error">بارگذاری اطلاعات نقش کاربر ناموفق بود.</Alert>
        )}
        {updateMutation.isError && (
          <Alert severity="error">ذخیره تخصیص نقش کاربر ناموفق بود.</Alert>
        )}
        {updateMutation.isSuccess && (
          <Alert severity="success">نقش‌های کاربر با موفقیت ذخیره شد.</Alert>
        )}
        <Button
          type="submit"
          variant="contained"
          disabled={updateMutation.isPending || selectedUserId <= 0}
          sx={{ alignSelf: isCompact ? 'stretch' : 'flex-start' }}
        >
          {updateMutation.isPending ? 'در حال ذخیره...' : 'ذخیره نقش‌های کاربر'}
        </Button>
      </Stack>
    </Paper>
  );
}
