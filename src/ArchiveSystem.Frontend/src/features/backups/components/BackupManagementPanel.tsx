import BackupRoundedIcon from '@mui/icons-material/BackupRounded';
import DownloadRoundedIcon from '@mui/icons-material/DownloadRounded';
import RefreshRoundedIcon from '@mui/icons-material/RefreshRounded';
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Divider,
  Paper,
  Stack,
  Typography,
} from '@mui/material';
import {
  useBackupsQuery,
  useCreateBackupMutation,
  useDownloadBackupMutation,
} from '@/features/backups/hooks/useBackupsQuery';
import { mapApiError } from '@/services/errorMapper';
import { useToastStore } from '@/stores/useToastStore';

const formatSize = (bytes: number): string => {
  if (bytes < 1024) {
    return `${bytes} B`;
  }

  if (bytes < 1024 * 1024) {
    return `${(bytes / 1024).toFixed(1)} KB`;
  }

  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
};

const formatDateTime = (value: string): string => {
  return new Intl.DateTimeFormat('fa-IR', {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value));
};

export const BackupManagementPanel = () => {
  const pushToast = useToastStore((state) => state.push);
  const backupsQuery = useBackupsQuery();
  const createBackupMutation = useCreateBackupMutation();
  const downloadBackupMutation = useDownloadBackupMutation();

  const backups = backupsQuery.data ?? [];

  const handleCreateBackup = async () => {
    try {
      await createBackupMutation.mutateAsync({
        includeDatabase: true,
        includeFileObjects: true,
      });
    } catch (error) {
      pushToast(mapApiError(error).message || 'پشتیبان‌گیری ناموفق بود.', 'error');
    }
  };

  const handleDownload = async (fileName: string) => {
    try {
      await downloadBackupMutation.mutateAsync(fileName);
    } catch (error) {
      pushToast(mapApiError(error).message || 'دانلود فایل پشتیبان ناموفق بود.', 'error');
    }
  };

  return (
    <Paper variant="outlined" sx={{ p: 2, borderRadius: 2 }}>
      <Stack spacing={1.5}>
        <Stack
          direction={{ xs: 'column', md: 'row' }}
          spacing={1.5}
          justifyContent="space-between"
          alignItems={{ xs: 'stretch', md: 'flex-start' }}
        >
          <Box>
            <Stack direction="row" spacing={1} alignItems="center">
              <BackupRoundedIcon color="primary" />
              <Typography variant="h6" fontWeight={800}>
                پشتیبان‌گیری سامانه
              </Typography>
            </Stack>
            <Typography variant="body2" color="text.secondary" sx={{ mt: 0.75, lineHeight: 1.9 }}>
              پشتیبان به صورت فایل ZIP روی سرور ذخیره می‌شود و شامل خروجی منطقی جدول‌های دیتابیس،
              فایل‌های object storage، manifest و گزارش فایل‌های ناموجود است. مسیر سروری:
              <Box component="span" dir="ltr" sx={{ mx: 0.5, fontFamily: 'monospace' }}>
                App_Data/Backups
              </Box>
            </Typography>
          </Box>

          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1}>
            <Button
              variant="outlined"
              startIcon={<RefreshRoundedIcon />}
              onClick={() => backupsQuery.refetch()}
              disabled={backupsQuery.isFetching}
            >
              بازخوانی لیست
            </Button>
            <Button
              variant="contained"
              startIcon={createBackupMutation.isPending ? <CircularProgress size={18} color="inherit" /> : <BackupRoundedIcon />}
              onClick={handleCreateBackup}
              disabled={createBackupMutation.isPending}
            >
              {createBackupMutation.isPending ? 'در حال پشتیبان‌گیری...' : 'شروع پشتیبان‌گیری'}
            </Button>
          </Stack>
        </Stack>

        {createBackupMutation.isPending ? (
          <Alert severity="info">
            فرآیند پشتیبان‌گیری آغاز شده است. تا پایان عملیات این صفحه را باز نگه دارید.
          </Alert>
        ) : null}

        {backupsQuery.isError ? (
          <Alert severity="error">
            {mapApiError(backupsQuery.error).message || 'فهرست پشتیبان‌ها دریافت نشد.'}
          </Alert>
        ) : null}

        <Divider />

        <Stack spacing={1}>
          <Typography variant="subtitle2" fontWeight={800}>
            پشتیبان‌های موجود
          </Typography>

          {backupsQuery.isLoading ? (
            <Typography variant="body2" color="text.secondary">
              در حال بارگذاری...
            </Typography>
          ) : backups.length === 0 ? (
            <Alert severity="warning">
              هنوز هیچ فایل پشتیبانی ثبت نشده است. برای ایجاد اولین خروجی، روی «شروع پشتیبان‌گیری» کلیک کنید.
            </Alert>
          ) : (
            backups.map((backup) => (
              <Stack
                key={backup.fileName}
                direction={{ xs: 'column', md: 'row' }}
                spacing={1}
                alignItems={{ xs: 'stretch', md: 'center' }}
                justifyContent="space-between"
                sx={{
                  p: 1,
                  border: '1px solid',
                  borderColor: 'divider',
                  borderRadius: 1,
                }}
              >
                <Box>
                  <Typography variant="body2" fontWeight={800} dir="ltr">
                    {backup.fileName}
                  </Typography>
                  <Typography variant="caption" color="text.secondary">
                    {formatDateTime(backup.createdAtUtc)} - {formatSize(backup.sizeBytes)}
                  </Typography>
                </Box>
                <Button
                  size="small"
                  variant="outlined"
                  startIcon={<DownloadRoundedIcon />}
                  onClick={() => handleDownload(backup.fileName)}
                  disabled={downloadBackupMutation.isPending}
                >
                  دانلود
                </Button>
              </Stack>
            ))
          )}
        </Stack>
      </Stack>
    </Paper>
  );
};
