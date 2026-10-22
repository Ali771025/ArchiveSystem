import { Stack, Typography } from '@mui/material';
import { UserManagementTemplate } from '@/features/user-management/components/templates/UserManagementTemplate';

export const UserManagementPage = () => {
  return (
    <Stack spacing={1.5}>
      {/* <Typography variant="body2" color="text.secondary">
        مدیریت متمرکز کاربران با پشتیبانی کامل RTL، اعتبارسنجی و بهینه‌سازی درخواست‌های سرور.
      </Typography> */}
      <UserManagementTemplate />
    </Stack>
  );
};
