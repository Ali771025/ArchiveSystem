import CompareArrowsIcon from "@mui/icons-material/CompareArrows";
import { Box, Button, Skeleton, Stack, Typography } from "@mui/material";
import {
  DataGrid,
  type GridColDef,
  type GridPaginationModel,
  type GridSortModel,
} from "@mui/x-data-grid";
import { useMemo } from "react";
import type { AuditLogResponseDto } from "@/features/reports/types";
import {
  getAuditActionLabel,
  getAuditEntityLabel,
} from "@/features/reports/reportLabels";

interface AuditLogsGridProps {
  logs: AuditLogResponseDto[];
  rowCount: number;
  loading: boolean;
  paginationModel: GridPaginationModel;
  sortModel: GridSortModel;
  onPaginationModelChange: (model: GridPaginationModel) => void;
  onSortModelChange: (model: GridSortModel) => void;
  onOpenDiff: (log: AuditLogResponseDto) => void;
}

const toPersianDateTime = (value: string) => {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return "-";
  }

  return date.toLocaleString("fa-IR");
};

const formatUserName = (userName?: string | null, userId?: string | null) => {
  if (userName?.trim()) {
    return userName;
  }

  const normalizedUserId = userId?.trim();
  return normalizedUserId && normalizedUserId !== "0" ? normalizedUserId : "-";
};

const parseJsonObject = (value?: string | null): Record<string, unknown> => {
  if (!value?.trim()) {
    return {};
  }

  try {
    const parsed = JSON.parse(value);
    return parsed && typeof parsed === "object" && !Array.isArray(parsed)
      ? (parsed as Record<string, unknown>)
      : {};
  } catch {
    return {};
  }
};

const formatAffectedColumns = (log: AuditLogResponseDto) => {
  if (log.affectedColumns?.trim()) {
    try {
      const columns = JSON.parse(log.affectedColumns) as string[];
      return columns.length > 0 ? columns.join("، ") : "-";
    } catch {
      return log.affectedColumns;
    }
  }

  const oldValues = parseJsonObject(log.oldValues);
  const newValues = parseJsonObject(log.newValues);
  const changedKeys = Array.from(
    new Set([...Object.keys(oldValues), ...Object.keys(newValues)]),
  ).filter((key) => JSON.stringify(oldValues[key]) !== JSON.stringify(newValues[key]));

  return changedKeys.length > 0 ? changedKeys.join("، ") : "-";
};

const hasAuditDetails = (log: AuditLogResponseDto) =>
  Boolean(log.oldValues?.trim() || log.newValues?.trim() || log.affectedColumns?.trim());

export const AuditLogsGrid = ({
  logs,
  rowCount,
  loading,
  paginationModel,
  sortModel,
  onPaginationModelChange,
  onSortModelChange,
  onOpenDiff,
}: AuditLogsGridProps) => {
  const columns = useMemo<GridColDef<AuditLogResponseDto>[]>(
    () => [
      {
        field: "actionDate",
        headerName: "زمان رخداد",
        minWidth: 180,
        flex: 1,
        valueFormatter: (value) => toPersianDateTime(String(value)),
      },
      {
        field: "userName",
        headerName: "نام کاربری",
        minWidth: 130,
        flex: 0.9,
        valueGetter: (_value, row) => formatUserName(row.userName, row.userId),
      },
      {
        field: "tableName",
        headerName: "موضوع عملیات",
        minWidth: 130,
        flex: 0.9,
        valueGetter: (_value, row) => getAuditEntityLabel(row.tableName),
      },
      {
        field: "recordId",
        headerName: "شناسه رکورد",
        minWidth: 130,
        flex: 1,
        valueGetter: (_value, row) => `${getAuditEntityLabel(row.tableName)} / ${row.recordId || "-"}`,
      },
      {
        field: "eventType",
        headerName: "نوع عملیات",
        minWidth: 160,
        flex: 1,
        valueFormatter: (value) =>
          getAuditActionLabel(value == null ? null : String(value)),
      },
      {
        field: "affectedColumns",
        headerName: "ستون‌های متأثر",
        minWidth: 180,
        flex: 1.2,
        sortable: false,
        valueGetter: (_value, row) => formatAffectedColumns(row),
      },
      {
        field: "actions",
        headerName: "جزئیات",
        minWidth: 130,
        sortable: false,
        filterable: false,
        renderCell: (params) => {
          if (!hasAuditDetails(params.row)) {
            return (
              <Typography variant="caption" color="text.secondary">
                جزئیاتی ثبت نشده
              </Typography>
            );
          }

          return (
            <Button
              size="small"
              variant="outlined"
              startIcon={<CompareArrowsIcon fontSize="small" />}
              onClick={() => onOpenDiff(params.row)}
            >
              مشاهده
            </Button>
          );
        },
      },
    ],
    [onOpenDiff],
  );

  return (
    <Box className="table-scroll" dir="rtl">
      <Box sx={{ height: 540, minWidth: 1100 }}>
        <DataGrid
          rows={logs}
          getRowId={(row) => row.auditId}
          columns={columns}
          loading={loading}
          rowCount={rowCount}
          paginationMode="server"
          sortingMode="server"
          disableRowSelectionOnClick
          paginationModel={paginationModel}
          onPaginationModelChange={onPaginationModelChange}
          sortModel={sortModel}
          onSortModelChange={onSortModelChange}
          pageSizeOptions={[10, 20, 50, 100]}
          slots={{
            loadingOverlay: () => (
              <Stack sx={{ p: 2 }} spacing={1}>
                <Skeleton variant="rounded" height={36} />
                <Skeleton variant="rounded" height={36} />
                <Skeleton variant="rounded" height={36} />
                <Skeleton variant="rounded" height={36} />
              </Stack>
            ),
            noRowsOverlay: () => (
              <Stack
                alignItems="center"
                justifyContent="center"
                sx={{ height: "100%" }}
              >
                <Typography variant="body2" color="text.secondary">
                  رکوردی برای نمایش وجود ندارد.
                </Typography>
              </Stack>
            ),
          }}
          sx={{
            borderRadius: 2,
            borderColor: "divider",
            "& .MuiDataGrid-columnHeaders": {
              bgcolor: "action.hover",
            },
            "& .MuiDataGrid-columnHeaderTitle": {
              fontWeight: 700,
            },
          }}
        />
      </Box>
    </Box>
  );
};
