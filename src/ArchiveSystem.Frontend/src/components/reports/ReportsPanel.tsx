import DownloadIcon from '@mui/icons-material/Download';
import FileDownloadIcon from '@mui/icons-material/FileDownload';
import {
  Alert,
  Box,
  Button,
  Chip,
  Paper,
  Stack,
  Typography,
  useMediaQuery,
  useTheme,
} from '@mui/material';
import { DataGrid, GridColDef, GridPaginationModel } from '@mui/x-data-grid';
import { useState } from 'react';
import { archiveApi } from '@/services/archiveApi';
import { useActivityReport, useCreateExportJob, useExportJobs } from '@/services/archiveQueries';
import { useStableRowCount } from '@/hooks/useStableRowCount';

const statusColorMap: Record<string, 'default' | 'warning' | 'success' | 'error'> = {
  pending: 'warning',
  running: 'warning',
  completed: 'success',
  failed: 'error',
  expired: 'default',
};

export const ReportsPanel = () => {
  const theme = useTheme();
  const isCompact = useMediaQuery(theme.breakpoints.down('md'));
  const [pagination, setPagination] = useState<GridPaginationModel>({ page: 0, pageSize: 10 });
  const activityQuery = useActivityReport(pagination.page + 1, pagination.pageSize);
  const exportJobsQuery = useExportJobs();
  const createExportMutation = useCreateExportJob();
  const activityTotal = useStableRowCount(activityQuery.data?.totalCount);
  const canNextPage = (pagination.page + 1) * pagination.pageSize < activityTotal;

  const activityColumns: GridColDef[] = [
    {
      field: 'timestamp',
      headerName: 'زمان',
      flex: 1,
      minWidth: 170,
      valueFormatter: (value) => new Date(String(value)).toLocaleString('fa-IR'),
    },
    { field: 'actorName', headerName: 'کاربر', flex: 1, minWidth: 120 },
    { field: 'actionType', headerName: 'عملیات', flex: 1, minWidth: 150 },
    { field: 'entityType', headerName: 'موجودیت', flex: 1, minWidth: 120 },
    { field: 'detail', headerName: 'جزئیات', flex: 1.8, minWidth: 260 },
  ];

  const jobColumns: GridColDef[] = [
    { field: 'id', headerName: 'شناسه', flex: 1.2, minWidth: 220 },
    { field: 'type', headerName: 'نوع', flex: 1, minWidth: 120 },
    {
      field: 'status',
      headerName: 'وضعیت',
      flex: 1,
      minWidth: 120,
      renderCell: ({ row }) => <Chip label={row.status} color={statusColorMap[row.status] ?? 'default'} size="small" />,
    },
    { field: 'progressPercent', headerName: 'پیشرفت', flex: 1, minWidth: 100 },
    {
      field: 'createdAt',
      headerName: 'ایجاد',
      flex: 1,
      minWidth: 160,
      valueFormatter: (value) => new Date(String(value)).toLocaleString('fa-IR'),
    },
    {
      field: 'download',
      headerName: 'دانلود',
      flex: 1,
      minWidth: 130,
      sortable: false,
      renderCell: ({ row }) =>
        row.status === 'completed' ? (
          <Button
            size="small"
            startIcon={<DownloadIcon />}
            onClick={() => archiveApi.downloadExportJob(row.id)}
          >
            دانلود
          </Button>
        ) : (
          <Typography variant="caption" color="text.secondary">
            آماده نیست
          </Typography>
        ),
    },
  ];

  return (
    <Stack spacing={2}>
      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1} justifyContent="space-between" alignItems={{ xs: 'stretch', sm: 'center' }}>
        <Typography variant="h5">گزارش‌ها و خروجی‌ها</Typography>
        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1}>
          <Button
            variant="outlined"
            startIcon={<FileDownloadIcon />}
            onClick={() => createExportMutation.mutate('activity')}
            disabled={createExportMutation.isPending}
            fullWidth={isCompact}
          >
            خروجی اکسل فعالیت
          </Button>
          <Button
            variant="contained"
            startIcon={<FileDownloadIcon />}
            onClick={() => createExportMutation.mutate('files')}
            disabled={createExportMutation.isPending}
            fullWidth={isCompact}
          >
            خروجی فایل‌ها
          </Button>
        </Stack>
      </Stack>

      <Paper variant="outlined" sx={{ p: 1.5 }}>
        <Typography variant="subtitle1" sx={{ mb: 1 }}>
          گزارش فعالیت
        </Typography>
        {isCompact ? (
          <Stack spacing={1}>
            {(activityQuery.data?.items ?? []).map((row) => (
              <Paper key={row.id} variant="outlined" sx={{ p: 1.25 }}>
                <Stack spacing={0.5}>
                  <Typography variant="subtitle2">{row.actionType}</Typography>
                  <Typography variant="body2">{row.detail}</Typography>
                  <Typography variant="caption" color="text.secondary">
                    {row.actorName} | {row.entityType}
                  </Typography>
                  <Typography variant="caption" color="text.secondary">
                    {new Date(row.timestamp).toLocaleString('fa-IR')}
                  </Typography>
                </Stack>
              </Paper>
            ))}
            {(activityQuery.data?.items ?? []).length === 0 && !activityQuery.isLoading && (
              <Alert severity="info">داده‌ای برای گزارش فعالیت موجود نیست.</Alert>
            )}
            <Stack direction="row" spacing={1} justifyContent="space-between" alignItems="center">
              <Button
                size="small"
                variant="outlined"
                disabled={pagination.page === 0}
                onClick={() => setPagination((current) => ({ ...current, page: current.page - 1 }))}
              >
                صفحه قبل
              </Button>
              <Typography variant="caption" color="text.secondary">
                صفحه {pagination.page + 1}
              </Typography>
              <Button
                size="small"
                variant="outlined"
                disabled={!canNextPage}
                onClick={() => setPagination((current) => ({ ...current, page: current.page + 1 }))}
              >
                صفحه بعد
              </Button>
            </Stack>
          </Stack>
        ) : (
          <Box className="table-scroll" dir="rtl">
            <Box sx={{ height: 420, minWidth: 980 }}>
              <DataGrid
                rows={activityQuery.data?.items ?? []}
                columns={activityColumns}
                loading={activityQuery.isLoading}
                rowCount={activityQuery.data?.totalCount ?? 0}
                paginationMode="server"
                paginationModel={pagination}
                onPaginationModelChange={setPagination}
                pageSizeOptions={[10, 20, 50]}
              />
            </Box>
          </Box>
        )}
      </Paper>

      <Paper variant="outlined" sx={{ p: 1.5 }}>
        <Typography variant="subtitle1" sx={{ mb: 1 }}>
          وضعیت کارهای خروجی
        </Typography>
        {isCompact ? (
          <Stack spacing={1}>
            {(exportJobsQuery.data ?? []).map((row) => (
              <Paper key={row.id} variant="outlined" sx={{ p: 1.25 }}>
                <Stack spacing={0.5}>
                  <Stack direction="row" justifyContent="space-between" spacing={1} alignItems="center">
                    <Typography variant="subtitle2">{row.type}</Typography>
                    <Chip size="small" label={row.status} color={statusColorMap[row.status] ?? 'default'} />
                  </Stack>
                  <Typography variant="caption" color="text.secondary">
                    شناسه: {row.id}
                  </Typography>
                  <Typography variant="caption" color="text.secondary">
                    پیشرفت: {row.progressPercent}%
                  </Typography>
                  <Typography variant="caption" color="text.secondary">
                    {new Date(row.createdAt).toLocaleString('fa-IR')}
                  </Typography>
                </Stack>
              </Paper>
            ))}
          </Stack>
        ) : (
          <Box className="table-scroll" dir="rtl">
            <Box sx={{ height: 330, minWidth: 920 }}>
              <DataGrid
                rows={exportJobsQuery.data ?? []}
                columns={jobColumns}
                loading={exportJobsQuery.isLoading}
                disableRowSelectionOnClick
              />
            </Box>
          </Box>
        )}
        {(exportJobsQuery.data ?? []).length === 0 && <Alert severity="info">کار خروجی ثبت نشده است.</Alert>}
      </Paper>
    </Stack>
  );
};
