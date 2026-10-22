import BlockOutlinedIcon from '@mui/icons-material/BlockOutlined';
import CheckCircleOutlineIcon from '@mui/icons-material/CheckCircleOutline';
import EditOutlinedIcon from '@mui/icons-material/EditOutlined';
import VpnKeyOutlinedIcon from '@mui/icons-material/VpnKeyOutlined';
import {
  Avatar,
  Box,
  Button,
  Chip,
  IconButton,
  Skeleton,
  Stack,
  Tooltip,
  Typography,
  useMediaQuery,
  useTheme,
} from '@mui/material';
import {
  DataGrid,
  type GridColDef,
  type GridLocaleText,
  type GridFilterModel,
  type GridPaginationModel,
  type GridRenderCellParams,
  type GridSortModel,
} from '@mui/x-data-grid';
import { useMemo } from 'react';
import type { PagedResponse, UserListItemDTO } from '@/features/user-management/types';
import { useStableRowCount } from '@/hooks/useStableRowCount';

interface UsersDataGridProps {
  data?: PagedResponse<UserListItemDTO>;
  isLoading: boolean;
  paginationModel: GridPaginationModel;
  onPaginationModelChange: (model: GridPaginationModel) => void;
  filterModel: GridFilterModel;
  onFilterModelChange: (model: GridFilterModel) => void;
  sortModel: GridSortModel;
  onSortModelChange: (model: GridSortModel) => void;
  onEdit: (user: UserListItemDTO) => void;
  onResetPassword: (user: UserListItemDTO) => void;
  onToggleStatus: (user: UserListItemDTO) => void;
}

const getInitials = (firstName: string, lastName: string) => {
  const first = firstName.trim().charAt(0);
  const last = lastName.trim().charAt(0);
  return `${first}${last}`.toUpperCase();
};

const toHumanDuration = (value: string) => {
  if (!value) {
    return '00:00:00';
  }

  const parts = value.split(':');
  if (parts.length < 3) {
    return value;
  }

  return `${parts[0]}:${parts[1]}:${parts[2].split('.')[0]}`;
};

const toPersianNumber = (value: number) => value.toLocaleString('fa-IR');

const usersGridLocaleText: Partial<GridLocaleText> = {
  paginationRowsPerPage: 'تعداد ردیف در صفحه',
  paginationDisplayedRows: ({ from, to, count, estimated }) => {
    const total =
      estimated && estimated > to
        ? `بیش از ${toPersianNumber(to)}`
        : count !== -1
          ? toPersianNumber(count)
          : `بیش از ${toPersianNumber(to)}`;

    return `${toPersianNumber(from)}-${toPersianNumber(to)} از ${total}`;
  },
};

export const UsersDataGrid = ({
  data,
  isLoading,
  paginationModel,
  onPaginationModelChange,
  filterModel,
  onFilterModelChange,
  sortModel,
  onSortModelChange,
  onEdit,
  onResetPassword,
  onToggleStatus,
}: UsersDataGridProps) => {
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('md'));

  const rows = data?.items ?? [];
  const rowCount = useStableRowCount(data?.totalCount);

  const columns = useMemo<GridColDef<UserListItemDTO>[]>(
    () => [
      {
        field: 'rowIndex',
        headerName: 'ردیف',
        width: 88,
        sortable: false,
        filterable: false,
        align: 'center',
        headerAlign: 'center',
        renderCell: (params) => {
          const index = paginationModel.page * paginationModel.pageSize + params.api.getRowIndexRelativeToVisibleRows(params.id) + 1;
          return <Typography variant="body2">{index}</Typography>;
        },
      },
      {
        field: 'actions',
        headerName: 'عملیات',
        sortable: false,
        filterable: false,
        width: 260,
        align: 'center',
        headerAlign: 'center',
        renderCell: (params: GridRenderCellParams<UserListItemDTO>) => (
          <Stack direction="row" spacing={0.5}>
            <Tooltip title="ویرایش کاربر">
              <IconButton
                aria-label={`ویرایش ${params.row.username}`}
                size="small"
                onClick={(event) => {
                  event.stopPropagation();
                  onEdit(params.row);
                }}
              >
                <EditOutlinedIcon fontSize="small" />
              </IconButton>
            </Tooltip>
            <Tooltip title="تغییر رمز عبور">
              <IconButton
                aria-label={`تغییر رمز عبور ${params.row.username}`}
                size="small"
                color="primary"
                onClick={(event) => {
                  event.stopPropagation();
                  onResetPassword(params.row);
                }}
              >
                <VpnKeyOutlinedIcon fontSize="small" />
              </IconButton>
            </Tooltip>
            <Tooltip title={params.row.isActive ? 'غیرفعال‌سازی کاربر' : 'فعال‌سازی کاربر'}>
              <Button
                aria-label={`${params.row.isActive ? 'غیرفعال‌سازی' : 'فعال‌سازی'} ${params.row.username}`}
                size="small"
                color={params.row.isActive ? 'error' : 'success'}
                variant="outlined"
                startIcon={params.row.isActive ? <BlockOutlinedIcon fontSize="small" /> : <CheckCircleOutlineIcon fontSize="small" />}
                onClick={(event) => {
                  event.stopPropagation();
                  onToggleStatus(params.row);
                }}
              >
                {params.row.isActive ? 'غیرفعال' : 'فعال‌سازی'}
              </Button>
            </Tooltip>
          </Stack>
        ),
      },
      {
        field: 'isActive',
        headerName: 'وضعیت کاربر',
        minWidth: 120,
        
        align: 'center',
        headerAlign: 'center',
        renderCell: (params: GridRenderCellParams<UserListItemDTO>) =>
          params.row.isActive ? <Chip label="فعال" color="success" size="small" /> : <Chip label="غیرفعال" size="small" />,
      },
      {
        field: 'twoFactorEnabled',
        headerName: 'ورود پیامکی',
        align: 'center',
        headerAlign: 'center',
        minWidth: 125,
        sortable: false,
        renderCell: (params: GridRenderCellParams<UserListItemDTO>) =>
          params.row.twoFactorEnabled ? <Chip label="فعال" color="info" size="small" /> : <Chip label="غیرفعال" variant="outlined" size="small" />,
      },
      {
        field: 'personnel',
        headerName: 'اطلاعات پرسنلی',
        align: 'center',
        headerAlign: 'center',
        flex: 1.8,
        minWidth: 270,
        sortable: false,
        renderCell: (params: GridRenderCellParams<UserListItemDTO>) => (
          <Stack direction="row" spacing={1.25} alignItems="center" sx={{ minWidth: 0 }}>
            <Avatar sx={{ bgcolor: 'primary.main', width: 32, height: 32, fontSize: 13 }}>
              {getInitials(params.row.firstName, params.row.lastName)}
            </Avatar>
            <Stack spacing={0.25} sx={{ minWidth: 0 }}>
              <Typography noWrap variant="body2" fontWeight={700}>
                {params.row.firstName} {params.row.lastName}
              </Typography>
              <Typography noWrap variant="caption" color="text.secondary">
                کد ملی: {params.row.nationalId || '-'}
              </Typography>
            </Stack>
          </Stack>
        ),
      },
      {
        field: 'username',
        headerName: 'نام کاربری',
        minWidth: 150,
        flex: 1,
        align: 'center',
        headerAlign: 'center',
      },
      {
        field: 'activeDuration',
        headerName: 'مدت زمان فعال بودن',
        minWidth: 180,
        sortable: false,
        filterable: false,
        align: 'center',
        headerAlign: 'center',
        valueGetter: (_value, row) => toHumanDuration(row.activeDuration),
      },
      {
        field: 'autoBackupEnabled',
        headerName: 'پشتیبان‌گیری خودکار',
        minWidth: 170,
        align: 'center',
        headerAlign: 'center',
        sortable: false,
        renderCell: (params: GridRenderCellParams<UserListItemDTO>) =>
          params.row.autoBackupEnabled ? (
            <Chip label="فعال" color="success" size="small" />
          ) : (
            <Chip label="غیرفعال" variant="outlined" size="small" />
          ),
      },
    ],
    [onEdit, onResetPassword, onToggleStatus, paginationModel.page, paginationModel.pageSize],
  );

  return (
    <Box className="table-scroll" dir="rtl">
      <Box sx={{ minWidth: isMobile ? 960 : 1120 }}>
        <DataGrid
          autoHeight
          rows={rows}
          columns={columns}
          loading={isLoading}
          rowCount={rowCount}
          paginationMode="server"
          filterMode="server"
          sortingMode="server"
          disableRowSelectionOnClick
          paginationModel={paginationModel}
          onPaginationModelChange={onPaginationModelChange}
          filterModel={filterModel}
          onFilterModelChange={onFilterModelChange}
          sortModel={sortModel}
          onSortModelChange={onSortModelChange}
          pageSizeOptions={[10, 20, 50]}
          localeText={usersGridLocaleText}
          slots={{
            loadingOverlay: () => (
              <Stack sx={{ p: 2 }} spacing={1}>
                <Skeleton variant="rounded" height={38} />
                <Skeleton variant="rounded" height={38} />
                <Skeleton variant="rounded" height={38} />
                <Skeleton variant="rounded" height={38} />
              </Stack>
            ),
            noRowsOverlay: () => (
              <Stack alignItems="center" justifyContent="center" sx={{ height: '100%' }}>
                <Typography variant="body2" color="text.secondary">
                  کاربری یافت نشد.
                </Typography>
              </Stack>
            ),
          }}
          sx={{
            borderRadius: 2,
            borderColor: 'divider',
            '& .MuiDataGrid-main': {
              minHeight: rows.length === 0 && !isLoading ? 240 : undefined,
            },
            '& .MuiDataGrid-columnHeaders': {
              bgcolor: 'rgba(15, 118, 110, 0.08)',
            },
            '& .MuiDataGrid-columnHeaderTitle': {
              fontWeight: 700,
            },
          }}
        />
      </Box>
    </Box>
  );
};
