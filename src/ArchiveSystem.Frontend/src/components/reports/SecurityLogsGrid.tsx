import { Box, Chip, Skeleton, Stack, Typography } from "@mui/material";
import {
  DataGrid,
  type GridColDef,
  type GridPaginationModel,
  type GridSortModel,
} from "@mui/x-data-grid";
import { useMemo } from "react";
import type { SecurityLogResponseDto } from "@/features/reports/types";

interface SecurityLogsGridProps {
  logs: SecurityLogResponseDto[];
  rowCount: number;
  loading: boolean;
  paginationModel: GridPaginationModel;
  sortModel: GridSortModel;
  onPaginationModelChange: (model: GridPaginationModel) => void;
  onSortModelChange: (model: GridSortModel) => void;
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

const getLevelColor = (level?: string | null) => {
  if (!level) {
    return "default";
  }

  const normalized = level.toLowerCase();
  if (normalized.includes("error") || normalized.includes("fatal")) {
    return "error";
  }

  if (normalized.includes("warn")) {
    return "warning";
  }

  return "default";
};

const extractQuotedValue = (message: string) => {
  const match = message.match(/'([^']*)'/);
  return match?.[1] ?? "";
};

const formatWithTarget = (template: string, target: string) => {
  if (!target.trim()) {
    return template;
  }

  return `${template} «${target}».`;
};

const toPersianSecurityEventType = (eventType?: string | null) => {
  switch (eventType?.trim().toLowerCase()) {
    case "loginsuccess":
      return "ورود موفق";
    case "loginfailed":
      return "ورود ناموفق";
    case "userpasswordreset":
    case "passwordchanged":
      return "تغییر رمز عبور";
    case "useractivated":
      return "فعال‌سازی کاربر";
    case "userdeactivated":
      return "غیرفعال‌سازی کاربر";
    case "roleassigned":
      return "تخصیص نقش";
    default:
      return eventType?.trim() || "-";
  }
};

const toPersianSecurityMessage = (row: SecurityLogResponseDto) => {
  const message = row.message?.trim() ?? "";
  if (!message) {
    return "-";
  }

  const target = extractQuotedValue(message);
  const eventType = row.eventType?.toLowerCase();

  if (
    eventType === "loginsuccess" &&
    /authenticated successfully/i.test(message)
  ) {
    return formatWithTarget("کاربر با موفقیت وارد سامانه شد", target);
  }

  if (eventType === "loginfailed") {
    if (/unknown user/i.test(message)) {
      return formatWithTarget("تلاش ناموفق برای ورود با کاربر ناشناس", target);
    }

    if (/deactivated user/i.test(message)) {
      return formatWithTarget("تلاش ناموفق برای ورود با کاربر غیرفعال", target);
    }

    if (/failed login attempt/i.test(message)) {
      return formatWithTarget("تلاش ناموفق برای ورود کاربر", target);
    }
  }

  if (eventType === "userpasswordreset" || eventType === "passwordchanged") {
    return formatWithTarget("رمز عبور کاربر بازنشانی شد", target);
  }

  if (eventType === "useractivated") {
    return formatWithTarget("کاربر فعال شد", target);
  }

  if (eventType === "userdeactivated") {
    return formatWithTarget("کاربر غیرفعال شد", target);
  }

  return message;
};

export const SecurityLogsGrid = ({
  logs,
  rowCount,
  loading,
  paginationModel,
  sortModel,
  onPaginationModelChange,
  onSortModelChange,
}: SecurityLogsGridProps) => {
  const columns = useMemo<GridColDef<SecurityLogResponseDto>[]>(
    () => [
      {
        field: "actionDate",
        headerName: "زمان",
        minWidth: 180,
        flex: 1,
        valueFormatter: (value) => toPersianDateTime(String(value)),
      },
      {
        field: "eventType",
        headerName: "رویداد امنیتی",
        minWidth: 160,
        flex: 1,
        valueGetter: (_value, row) => toPersianSecurityEventType(row.eventType),
      },
      {
        field: "actorUserName",
        headerName: "کاربر اجراکننده",
        minWidth: 130,
        flex: 0.9,
        valueGetter: (_value, row) =>
          formatUserName(row.actorUserName, row.actorUserId),
      },
      {
        field: "targetUserName",
        headerName: "کاربر هدف",
        minWidth: 130,
        flex: 0.9,
        valueGetter: (_value, row) =>
          formatUserName(row.targetUserName, row.targetUserId),
      },
      // {
      //   field: "level",
      //   headerName: "سطح",
      //   minWidth: 100,
      //   flex: 0.7,
      //   renderCell: (params) => (
      //     <Chip
      //       size="small"
      //       label={params.row.level ?? "Info"}
      //       color={
      //         getLevelColor(params.row.level) as "default" | "error" | "warning"
      //       }
      //       variant="outlined"
      //     />
      //   ),
      // },
      {
        field: "clientIpAddress",
        headerName: "IP کاربر",
        minWidth: 130,
        flex: 1,
      },
      {
        field: "message",
        headerName: "توضیح",
        minWidth: 280,
        flex: 1.6,
        renderCell: (params) => (
          <Typography variant="body2" title={params.row.message}>
            {toPersianSecurityMessage(params.row)}
          </Typography>
        ),
      },
    ],
    [],
  );

  return (
    <Box className="table-scroll" dir="rtl">
      <Box sx={{ height: 540, minWidth: 980 }}>
        <DataGrid
          rows={logs}
          getRowId={(row) => row.securityLogId}
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
              </Stack>
            ),
            noRowsOverlay: () => (
              <Stack
                alignItems="center"
                justifyContent="center"
                sx={{ height: "100%" }}
              >
                <Typography variant="body2" color="text.secondary">
                  لاگ امنیتی یافت نشد.
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
