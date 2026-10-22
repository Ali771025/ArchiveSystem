import { Box, Chip, IconButton, Skeleton, Stack, Tooltip, Typography, useMediaQuery, useTheme } from '@mui/material';
import { DataGrid, type GridColDef, type GridFilterModel, type GridPaginationModel, type GridRenderCellParams } from '@mui/x-data-grid';
import { Eye, Pencil, Trash2 } from 'lucide-react';
import { useMemo } from 'react';
import type { CaseLoanListItemDTO, CaseLoanStatus, PagedResponse } from '@/features/case-loan-management/types';
import { useStableRowCount } from '@/hooks/useStableRowCount';

interface CaseLoansDataGridProps {
  data?: PagedResponse<CaseLoanListItemDTO>;
  isLoading: boolean;
  paginationModel: GridPaginationModel;
  onPaginationModelChange: (model: GridPaginationModel) => void;
  filterModel: GridFilterModel;
  onFilterModelChange: (model: GridFilterModel) => void;
  onEdit: (caseLoan: CaseLoanListItemDTO) => void;
  onDelete: (caseLoan: CaseLoanListItemDTO) => void;
}

const formatDateTime = (value?: string | null) => {
  if (!value) {
    return '-';
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return value;
  }

  return new Intl.DateTimeFormat('fa-IR', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
  }).format(date);
};

const statusLabels: Record<CaseLoanStatus, string> = {
  Loaned: 'امانت داده شده',
  Returned: 'برگشت داده شده',
  Overdue: 'دیرکرد',
};

const statusColors: Record<CaseLoanStatus, 'default' | 'success' | 'warning' | 'error'> = {
  Loaned: 'default',
  Returned: 'success',
  Overdue: 'warning',
};

export const CaseLoansDataGrid = ({
  data,
  isLoading,
  paginationModel,
  onPaginationModelChange,
  filterModel,
  onFilterModelChange,
  onEdit,
  onDelete,
}: CaseLoansDataGridProps) => {
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('md'));

  const rows = data?.items ?? [];
  const rowCount = useStableRowCount(data?.totalCount);

  const columns = useMemo<GridColDef<CaseLoanListItemDTO>[]>(
    () => [
      {
        field: 'rowIndex',
        headerName: 'ردیف',
        width: 56,
        minWidth: 56,
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
        width: 84,
        align: 'center',
        headerAlign: 'center',
        renderCell: (params: GridRenderCellParams<CaseLoanListItemDTO>) => {
          const isClosed =
            Boolean(params.row.actualReturnDate) &&
            (params.row.status === 'Returned' || params.row.isReturned);

          if (isClosed) {
            return (
              <Tooltip title="مشاهده سابقه">
                <IconButton
                  aria-label={`مشاهده ${params.row.borrowerFullName}`}
                  size="small"
                  onClick={() => onEdit(params.row)}
                >
                  <Eye size={17} />
                </IconButton>
              </Tooltip>
            );
          }

          return (
            <Stack direction="row" spacing={0.5}>
              <Tooltip title="ویرایش">
                <IconButton aria-label={`ویرایش ${params.row.borrowerFullName}`} size="small" onClick={() => onEdit(params.row)}>
                  <Pencil size={16} />
                </IconButton>
              </Tooltip>
              <Tooltip title="حذف">
                <IconButton aria-label={`حذف ${params.row.borrowerFullName}`} size="small" color="error" onClick={() => onDelete(params.row)}>
                  <Trash2 size={16} />
                </IconButton>
              </Tooltip>
            </Stack>
          );
        },
      },
      {
        field: 'caseNumber',
        headerName: 'شماره پرونده',
        minWidth: 76,
        flex: 0.8,
        align: 'center',headerAlign: 'center',
      },
      {
        field: 'checkoutDate',
        filterable: false,
        headerName: 'تاریخ خروج پرونده',
        minWidth: 88,
        flex: 0.9,
        align: 'center',headerAlign: 'center',
        valueGetter: (value) => formatDateTime(value as string | null | undefined),
      },
      {
        field: 'expectedReturnDate',
        filterable: false,
        headerName: 'تاریخ بازگشت مورد انتظار',
        minWidth: 96,
        flex: 1,
        align: 'center',headerAlign: 'center',
        valueGetter: (value) => formatDateTime(value as string | null | undefined),
      },
      {
        field: 'actualReturnDate',
        filterable: false,
        headerName: 'تاریخ واقعی بازگشت',
        minWidth: 88,
        flex: 0.9,
        align: 'center',headerAlign: 'center',
        valueGetter: (value) => formatDateTime(value as string | null | undefined),
      },
      {
        field: 'borrowerFullName',
        headerName: 'نام کامل دریافت‌کننده',
        minWidth: 104,
        flex: 1.2,
       align: 'center',headerAlign: 'center',
      },
      {
        field: 'status',
        headerName: 'وضعیت امانت',
        minWidth: 88,
        flex: 0.9,
        align: 'center',headerAlign: 'center',
        renderCell: (params: GridRenderCellParams<CaseLoanListItemDTO>) => {
          const status = params.row.status ?? (params.row.isReturned ? 'Returned' : 'Loaned');
          return (
            <Chip
              size="small"
              variant="outlined"
              color={statusColors[status]}
              label={statusLabels[status]}
            />
          );
        },
      },
      {
        field: 'receiverUnit',
        headerName: 'واحد/منطقه دریافت‌کننده',
        minWidth: 98,
        flex: 1.1,
        align: 'center',headerAlign: 'center',
        valueGetter: (_value, row) => row.receiverUnit || '-',
      },
      {
        field: 'registeredBy',
        headerName: 'ثبت‌کننده امانت',
        minWidth: 86,
        flex: 0.9,
        align: 'center',headerAlign: 'center',
        valueGetter: (_value, row) => row.registeredBy || '-',
      },
      {
        field: 'nationalId',
        headerName: 'کد ملی',
        minWidth: 74,
        flex: 0.75,
        align: 'center',headerAlign: 'center',
      },
      {
        field: 'contactNumber',
        headerName: 'شماره تماس',
        minWidth: 82,
        flex: 0.8,
        align: 'center',headerAlign: 'center',
      },
    ],
    [onDelete, onEdit, paginationModel.page, paginationModel.pageSize],
  );

  return (
    <Box
      dir="rtl"
      sx={{
        width: '100%',
        maxWidth: '100%',
        minWidth: 0,
        overflowX: 'auto',
      }}
    >
      <Box
        sx={{
          height: isMobile ? 560 : 640,
          width: '100%',
          minWidth: 0,
        }}
      >
        <DataGrid
          rows={rows}
          columns={columns}
          loading={isLoading}
          rowCount={rowCount}
          paginationMode="server"
          filterMode="server"
          disableRowSelectionOnClick
          paginationModel={paginationModel}
          onPaginationModelChange={onPaginationModelChange}
          filterModel={filterModel}
          onFilterModelChange={onFilterModelChange}
          pageSizeOptions={[10, 20, 50]}
          columnHeaderHeight={64}
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
                  امانتی یافت نشد.
                </Typography>
              </Stack>
            ),
          }}
          sx={{
            width: '100%',
            borderRadius: 2,
            borderColor: 'divider',
            direction: 'rtl',
            minWidth: 0,
            '& .MuiDataGrid-main, & .MuiDataGrid-virtualScroller, & .MuiDataGrid-footerContainer': {
              direction: 'rtl',
            },
            '& .MuiDataGrid-scrollbar--vertical': {
              // Firefox places a native vertical scrollbar on the inline-start
              // side of an RTL scroll container. MUI pins this container to the
              // physical right edge, so keeping only the scrollbar itself LTR
              // prevents the Firefox-only gap without changing the grid's RTL.
              direction: 'ltr',
              right: 0,
              left: 'auto',
              margin: 0,
            },
            '& .MuiDataGrid-cell, & .MuiDataGrid-columnHeader': {
              px: 0.5,
              boxSizing: 'border-box',
            },
            '& .MuiDataGrid-columnHeaders': {
              bgcolor: 'rgba(15, 118, 110, 0.08)',
            },
            '& .MuiDataGrid-columnHeaderTitle': {
              fontWeight: 700,
              whiteSpace: 'normal',
              lineHeight: 1.25,
              textAlign: 'right',
            },
          }}
        />
      </Box>
    </Box>
  );
};
