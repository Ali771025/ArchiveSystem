import DeleteOutlineIcon from "@mui/icons-material/DeleteOutline";
import EditOutlinedIcon from "@mui/icons-material/EditOutlined";
import {
  Avatar,
  Box,
  Chip,
  IconButton,
  Skeleton,
  Stack,
  Tooltip,
  Typography,
  useMediaQuery,
  useTheme,
} from "@mui/material";
import {
  DataGrid,
  type GridColDef,
  type GridPaginationModel,
  type GridFilterModel,
  type GridRenderCellParams,
} from "@mui/x-data-grid";
import { useMemo } from "react";
import type {
  PagedResponse,
  TaxpayerListItemDTO,
  TaxpayerType,
} from "@/features/taxpayer-management/types";
import { useStableRowCount } from "@/hooks/useStableRowCount";

interface TaxpayersDataGridProps {
  data?: PagedResponse<TaxpayerListItemDTO>;
  isLoading: boolean;
  paginationModel: GridPaginationModel;
  onPaginationModelChange: (model: GridPaginationModel) => void;
  filterModel: GridFilterModel;
  onFilterModelChange: (model: GridFilterModel) => void;
  onEdit: (taxpayer: TaxpayerListItemDTO) => void;
  onDelete: (taxpayer: TaxpayerListItemDTO) => void;
}

const getTaxpayerTypeLabel = (type: TaxpayerType) => {
  if (type === "LegalEntity" || type === 2) {
    return "حقوقی";
  }

  return "حقیقی";
};

const isLegalEntity = (type: TaxpayerType) =>
  type === "LegalEntity" || type === 2;

const getDisplayName = (row: TaxpayerListItemDTO) => {
  if (isLegalEntity(row.taxpayerType)) {
    return row.companyName?.trim() || "-";
  }

  return `${row.firstName ?? ""} ${row.lastName ?? ""}`.trim() || "-";
};

const getIdentifier = (row: TaxpayerListItemDTO) => {
  return isLegalEntity(row.taxpayerType)
    ? row.nationalCompanyId?.trim() || "-"
    : row.nationalId?.trim() || "-";
};

const getInitials = (row: TaxpayerListItemDTO) => {
  const displayName = getDisplayName(row).replace(/\s+/g, "").trim();
  return displayName === "-" ? "؟" : displayName.slice(0, 2).toUpperCase();
};

export const TaxpayersDataGrid = ({
  data,
  isLoading,
  paginationModel,
  onPaginationModelChange,
  filterModel,
  onFilterModelChange,
  onEdit,
  onDelete,
}: TaxpayersDataGridProps) => {
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down("md"));

  const rows = data?.items ?? [];
  const rowCount = useStableRowCount(data?.totalCount);

  const columns = useMemo<GridColDef<TaxpayerListItemDTO>[]>(
    () => [
      {
        field: "rowIndex",
        headerName: "ردیف",
        width: 72,
        minWidth: 72,
        sortable: false,
        filterable: false,
        align: "center",
        headerAlign: "center",
        renderCell: (params) => {
          const index =
            paginationModel.page * paginationModel.pageSize +
            params.api.getRowIndexRelativeToVisibleRows(params.id) +
            1;
          return <Typography variant="body2">{index}</Typography>;
        },
      },
      {
        field: "fullName",
        headerName: "نام کامل",
        flex: 1.7,
        minWidth: 180,
        align: "right",
        headerAlign: "center",
        sortable: false,
        renderCell: (params: GridRenderCellParams<TaxpayerListItemDTO>) => (
          <Stack
            direction="row"
            spacing={1.25}
            alignItems="center"
            sx={{ width: "100%", minWidth: 0, justifyContent: "flex-start" }}
          >
            <Avatar
              sx={{
                bgcolor: "primary.main",
                width: 32,
                height: 32,
                fontSize: 13,
              }}
            >
              {getInitials(params.row)}
            </Avatar>
            <Stack sx={{ minWidth: 0 }}>
              <Typography noWrap variant="body2" fontWeight={700}>
                {getDisplayName(params.row)}
              </Typography>
            </Stack>
          </Stack>
        ),
      },
      {
        field: "identifier",
        headerName: "کد ملی / شناسه ملی",
        minWidth: 165,
        flex: 1,
        align: "right",
        headerAlign: "right",
        valueGetter: (_value, row) => getIdentifier(row),
      },
      {
        field: "mobile",
        headerName: "شماره همراه",
        minWidth: 120,
        flex: 1,
        align: "right",
        headerAlign: "right",
        valueGetter: (_value, row) => row.mobile || "-",
      },
      {
        field: "taxpayerType",
        headerName: "نوع",
        minWidth: 100,
        flex: 0.8,
        align: "right",
        headerAlign: "right",
        renderCell: (params: GridRenderCellParams<TaxpayerListItemDTO>) => (
          <Chip
            label={getTaxpayerTypeLabel(params.row.taxpayerType)}
            color={
              params.row.taxpayerType === "LegalEntity" ||
              params.row.taxpayerType === 2
                ? "info"
                : "success"
            }
            variant="outlined"
            size="small"
          />
        ),
      },
      {
        field: "actions",
        headerName: "عملیات",
        sortable: false,
        filterable: false,
        width: 112,
        align: "center",
        headerAlign: "center",
        renderCell: (params: GridRenderCellParams<TaxpayerListItemDTO>) => (
          <Stack direction="row" spacing={0.5}>
            <Tooltip title="ویرایش مودی">
              <IconButton
                aria-label={`ویرایش ${getDisplayName(params.row)}`}
                size="small"
                onClick={() => onEdit(params.row)}
              >
                <EditOutlinedIcon fontSize="small" />
              </IconButton>
            </Tooltip>
            <Tooltip title="حذف مودی">
              <IconButton
                aria-label={`حذف ${getDisplayName(params.row)}`}
                size="small"
                color="error"
                onClick={() => onDelete(params.row)}
              >
                <DeleteOutlineIcon fontSize="small" />
              </IconButton>
            </Tooltip>
          </Stack>
        ),
      },
    ],
    [onDelete, onEdit, paginationModel.page, paginationModel.pageSize],
  );

  return (
    <Box
      dir="rtl"
      sx={{
        width: "100%",
        maxWidth: "100%",
        minWidth: 0,
        overflowX: "auto",
      }}
    >
      <Box
        sx={{
          height: isMobile ? 560 : 640,
          width: "100%",
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
              <Stack
                alignItems="center"
                justifyContent="center"
                sx={{ height: "100%" }}
              >
                <Typography variant="body2" color="text.secondary">
                  مودی‌ای یافت نشد.
                </Typography>
              </Stack>
            ),
          }}
          sx={{
            width: "100%",
            borderRadius: 2,
            borderColor: "divider",
            direction: "rtl",
            minWidth: 0,
            "& .MuiDataGrid-main, & .MuiDataGrid-virtualScroller, & .MuiDataGrid-footerContainer":
              {
                direction: "rtl",
              },
            "& .MuiDataGrid-scrollbar--horizontal": {
              minHeight: 14,
            },
            "& .MuiDataGrid-cell, & .MuiDataGrid-columnHeader": {
              px: 1,
            },
            "& .MuiDataGrid-cell[data-field='rowIndex'], & .MuiDataGrid-columnHeader[data-field='rowIndex']":
              {
                boxSizing: "border-box",
                minWidth: "72px !important",
                maxWidth: "72px !important",
              },
            "& .MuiDataGrid-columnHeaders": {
              bgcolor: "rgba(15, 118, 110, 0.08)",
            },
            "& .MuiDataGrid-columnHeaderTitle": {
              fontWeight: 700,
              textAlign: "right",
            },
            "& .MuiDataGrid-cell[data-field='fullName'], & .MuiDataGrid-cell[data-field='identifier']":
              {
                justifyContent: "flex-start",
                textAlign: "right",
              },
            "& .MuiDataGrid-columnHeader[data-field='fullName'] .MuiDataGrid-columnHeaderTitleContainer, & .MuiDataGrid-columnHeader[data-field='identifier'] .MuiDataGrid-columnHeaderTitleContainer":
              {
                justifyContent: "flex-start",
              },
          }}
        />
      </Box>
    </Box>
  );
};
