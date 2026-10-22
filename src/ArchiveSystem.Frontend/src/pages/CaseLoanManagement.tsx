import { Stack, Typography } from '@mui/material';
import { CaseLoanManagement } from '@/features/case-loan-management/components/templates/CaseLoanManagement';

export const CaseLoanManagementPage = () => {
  return (
    <Stack spacing={1.5}>
      {/* <Typography variant="body2" color="text.secondary">
        مدیریت امانات پرونده فیزیکی با فیلتر، صفحه‌بندی و عملیات کامل ایجاد، ویرایش و حذف.
      </Typography> */}
      <CaseLoanManagement />
    </Stack>
  );
};
