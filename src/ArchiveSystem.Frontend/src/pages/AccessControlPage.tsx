import { Stack, Typography } from '@mui/material';
import { AccessControlPanel } from '@/components/access/AccessControlPanel';

export const AccessControlPage = () => {
  return (
    <Stack spacing={1.5}>
      {/* <Typography variant="h5">مدیریت نقش‌ها و ACL</Typography> */}
      <AccessControlPanel />
    </Stack>
  );
};

