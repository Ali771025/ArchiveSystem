import { Stack, Typography } from '@mui/material';
import { TaxpayerManagement } from '@/features/taxpayer-management/components/templates/TaxpayerManagement';

export const TaxpayerManagementPage = () => {
  return (
    <Stack spacing={1.5}>
      {/* <Typography variant="body2" color="text.secondary">
        مدیریت متمرکز مودیان با فیلتر، صفحه‌بندی و عملیات کامل ایجاد، ویرایش و حذف.
      </Typography> */}
      <TaxpayerManagement />
    </Stack>
  );
};
