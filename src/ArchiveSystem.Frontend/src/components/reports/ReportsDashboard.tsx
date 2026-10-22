import HistoryEduIcon from "@mui/icons-material/HistoryEdu";
import LockPersonIcon from "@mui/icons-material/LockPerson";
import SourceIcon from "@mui/icons-material/Source";
import {
  Alert,
  Autocomplete,
  Box,
  Button,
  FormControl,
  InputLabel,
  MenuItem,
  Paper,
  Select,
  Stack,
  Tab,
  Tabs,
  TextField,
  Typography,
} from "@mui/material";
import type { GridPaginationModel, GridSortModel } from "@mui/x-data-grid";
import { useMemo, useState } from "react";
import { AuditDiffModal } from "@/components/reports/AuditDiffModal";
import { AuditFilterBar } from "@/components/reports/AuditFilterBar";
import { JalaliDatePicker } from "@/components/reports/JalaliDatePicker";
import { AuditLogsGrid } from "@/components/reports/AuditLogsGrid";
import { SecurityLogsGrid } from "@/components/reports/SecurityLogsGrid";
import {
  useAuditLogs,
  useAuditFilterOptions,
  useAuditUsers,
  useSecurityLogs,
} from "@/features/reports/hooks/useAuditLogs";
import { useReportsDashboardStore } from "@/features/reports/stores/useReportsDashboardStore";
import type {
  SecurityTableFilters,
  TablePaginationState,
} from "@/features/reports/types";
import { useDebounce } from "@/hooks/useDebounce";
import { useStableRowCount } from "@/hooks/useStableRowCount";
import { mapApiError } from "@/services/errorMapper";
import { parseDateLikeValue } from "@/utils/jalaliDate";

const auditEntityOptions = ["Cases", "Dossiers", "Files", "AspNetUsers"];
const fileDossierEntityOptions = ["Cases", "Dossiers", "Files"];
const auditActionOptions = [
  "Create",
  "Update",
  "Delete",
  "CASE_MOVE_CLASSIFICATION",
  "UPLOAD_FINALIZED",
];
const securityEventOptions = [
  { value: "LoginSuccess", label: "ورود موفق" },
  { value: "LoginFailed", label: "ورود ناموفق" },
  { value: "PasswordChanged", label: "تغییر گذرواژه" },
  { value: "UserActivated", label: "فعال‌سازی کاربر" },
  { value: "UserDeactivated", label: "غیرفعال‌سازی کاربر" },
  { value: "RoleAssigned", label: "تخصیص نقش" },
];

const tabSx = {
  minHeight: 48,
  alignItems: "start",
  textAlign: "right",
};

const convertPaginationToGrid = (
  state: TablePaginationState,
): GridPaginationModel => ({
  page: Math.max(state.pageNumber - 1, 0),
  pageSize: state.pageSize,
});

const convertSortToGrid = (state: TablePaginationState): GridSortModel => [
  {
    field: state.sortBy,
    sort: state.sortDirection,
  },
];

const toGridSortFallback = (
  sortModel: GridSortModel,
  fallback: TablePaginationState,
): TablePaginationState => {
  if (!sortModel[0] || !sortModel[0].sort) {
    return {
      ...fallback,
      sortBy: "ActionDate",
      sortDirection: "desc",
    };
  }

  return {
    ...fallback,
    sortBy: sortModel[0].field,
    sortDirection: sortModel[0].sort,
  };
};

interface SecurityFilterBarProps {
  filters: SecurityTableFilters;
  actorUsers: Array<{
    id: number;
    label: string;
    username: string;
    fullName: string;
  }>;
  targetUsers: Array<{
    id: number;
    label: string;
    username: string;
    fullName: string;
  }>;
  actorUserSearchText: string;
  targetUserSearchText: string;
  usersLoading: boolean;
  dateError?: string;
  onChange: (filters: Partial<SecurityTableFilters>) => void;
  onActorUserSearchTextChange: (search: string) => void;
  onTargetUserSearchTextChange: (search: string) => void;
  onReset: () => void;
}

const filterInputSx = {
  "& .MuiInputBase-root": { height: 40, borderRadius: 1.5, fontSize: 14 },
};

interface QueryErrorAlertProps {
  error: unknown;
  onRetry: () => void;
}

const QueryErrorAlert = ({ error, onRetry }: QueryErrorAlertProps) => (
  <Alert
    severity="error"
    action={
      <Button color="inherit" size="small" onClick={onRetry}>
        تلاش مجدد
      </Button>
    }
  >
    {mapApiError(error).message}
  </Alert>
);

const isInvalidDateRange = (startDate: string, endDate: string) => {
  const start = parseDateLikeValue(startDate);
  const end = parseDateLikeValue(endDate);
  return Boolean(start && end && start.getTime() > end.getTime());
};

const dateRangeErrorText = "از تاریخ نباید بزرگتر از تا تاریخ باشد.";

const SecurityFilterBar = ({
  filters,
  actorUsers,
  targetUsers,
  actorUserSearchText,
  targetUserSearchText,
  usersLoading,
  dateError,
  onChange,
  onActorUserSearchTextChange,
  onTargetUserSearchTextChange,
  onReset,
}: SecurityFilterBarProps) => {
  const selectedActorUser =
    actorUsers.find((option) => String(option.id) === filters.actorUserId) ??
    (filters.actorUserId && actorUserSearchText.trim()
      ? {
          id: Number(filters.actorUserId),
          username: actorUserSearchText,
          fullName: "",
          label: actorUserSearchText,
        }
      : null);
  const selectedTargetUser =
    targetUsers.find((option) => String(option.id) === filters.targetUserId) ??
    (filters.targetUserId && targetUserSearchText.trim()
      ? {
          id: Number(filters.targetUserId),
          username: targetUserSearchText,
          fullName: "",
          label: targetUserSearchText,
        }
      : null);

  return (
    <Paper
      variant="outlined"
      sx={{
        position: "sticky",
        top: 12,
        zIndex: 5,
        p: 2,
        borderRadius: 2,
        bgcolor: "background.paper",
        backdropFilter: "blur(6px)",
      }}
    >
      <Stack spacing={1.5}>
        <Stack
          direction={{ xs: "column", md: "row" }}
          justifyContent="space-between"
          alignItems={{ xs: "flex-start", md: "center" }}
        >
          <Typography variant="subtitle1" fontWeight={700}>
            فیلتر لاگ‌های امنیتی
          </Typography>
          <Typography
            variant="body2"
            color="primary.main"
            sx={{ cursor: "pointer", userSelect: "none" }}
            onClick={onReset}
          >
            پاک‌سازی فیلترها
          </Typography>
        </Stack>

        <Box
          sx={{
            display: "grid",
            gap: 1.5,
            alignItems: "end",
            gridTemplateColumns: {
              xs: "1fr",
              md: "repeat(10, minmax(0, 1fr))",
            },
          }}
        >
          <Box sx={{ gridColumn: { md: "span 2" } }}>
            <JalaliDatePicker
              label="از تاریخ"
              value={filters.startDate}
              onChange={(value) => onChange({ startDate: value })}
            />
          </Box>
          <Box sx={{ gridColumn: { md: "span 2" } }}>
            <JalaliDatePicker
              label="تا تاریخ"
              value={filters.endDate}
              onChange={(value) => onChange({ endDate: value })}
            />
          </Box>
          <Box sx={{ gridColumn: { md: "span 2" } }}>
            <Autocomplete
              options={actorUsers}
              loading={usersLoading}
              value={selectedActorUser}
              inputValue={actorUserSearchText}
              onInputChange={(_, newValue, reason) => {
                if (reason !== "reset") {
                  onActorUserSearchTextChange(newValue);
                }
              }}
              onChange={(_, user) => {
                onChange({ actorUserId: user ? String(user.id) : "" });
                onActorUserSearchTextChange(user?.label ?? "");
              }}
              getOptionLabel={(option) => option.label}
              isOptionEqualToValue={(option, value) => option.id === value.id}
              renderInput={(params) => (
                <TextField
                  {...params}
                  label="کاربر اجراکننده"
                  sx={filterInputSx}
                />
              )}
              clearOnBlur={false}
            />
          </Box>
          <Box sx={{ gridColumn: { md: "span 2" } }}>
            <Autocomplete
              options={targetUsers}
              loading={usersLoading}
              value={selectedTargetUser}
              inputValue={targetUserSearchText}
              onInputChange={(_, newValue, reason) => {
                if (reason !== "reset") {
                  onTargetUserSearchTextChange(newValue);
                }
              }}
              onChange={(_, user) => {
                onChange({ targetUserId: user ? String(user.id) : "" });
                onTargetUserSearchTextChange(user?.label ?? "");
              }}
              getOptionLabel={(option) => option.label}
              isOptionEqualToValue={(option, value) => option.id === value.id}
              renderInput={(params) => (
                <TextField {...params} label="کاربر هدف" sx={filterInputSx} />
              )}
              clearOnBlur={false}
            />
          </Box>
          <Box sx={{ gridColumn: { md: "span 2" } }}>
            <FormControl fullWidth sx={filterInputSx}>
              <InputLabel id="security-event-type">نوع رخداد</InputLabel>
              <Select
                labelId="security-event-type"
                label="نوع رخداد"
                value={filters.eventTypes[0] ?? ""}
                onChange={(event) =>
                  onChange({
                    eventTypes: event.target.value
                      ? [String(event.target.value)]
                      : [],
                  })
                }
              >
                <MenuItem value="">همه رخدادها</MenuItem>
                {securityEventOptions.map((eventType) => (
                  <MenuItem key={eventType.value} value={eventType.value}>
                    {eventType.label}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
          </Box>
        </Box>
        {dateError && (
          <Typography variant="caption" color="error">
            {dateError}
          </Typography>
        )}
      </Stack>
    </Paper>
  );
};

export const ReportsDashboard = () => {
  const {
    activeTab,
    auditFilters,
    auditPagination,
    fileDossierFilters,
    fileDossierPagination,
    securityFilters,
    securityPagination,
    selectedDiffLog,
    isDiffModalOpen,
    setActiveTab,
    setAuditFilters,
    setAuditPagination,
    setFileDossierFilters,
    setFileDossierPagination,
    setSecurityFilters,
    setSecurityPagination,
    openDiffModal,
    closeDiffModal,
  } = useReportsDashboardStore();

  const [auditUserSearch, setAuditUserSearch] = useState("");
  const [fileDossierUserSearch, setFileDossierUserSearch] = useState("");
  const [securityActorUserSearch, setSecurityActorUserSearch] = useState("");
  const [securityTargetUserSearch, setSecurityTargetUserSearch] = useState("");
  const [auditDateError, setAuditDateError] = useState("");
  const [fileDossierDateError, setFileDossierDateError] = useState("");
  const [securityDateError, setSecurityDateError] = useState("");

  const debouncedAuditUserSearch = useDebounce(auditUserSearch, 350);
  const debouncedFileDossierSearch = useDebounce(fileDossierUserSearch, 350);
  const debouncedSecurityActorUserSearch = useDebounce(
    securityActorUserSearch,
    350,
  );
  const debouncedSecurityTargetUserSearch = useDebounce(
    securityTargetUserSearch,
    350,
  );

  const auditUsersQuery = useAuditUsers(debouncedAuditUserSearch);
  const auditFilterOptionsQuery = useAuditFilterOptions();
  const fileDossierUsersQuery = useAuditUsers(debouncedFileDossierSearch);
  const securityActorUsersQuery = useAuditUsers(
    debouncedSecurityActorUserSearch,
  );
  const securityTargetUsersQuery = useAuditUsers(
    debouncedSecurityTargetUserSearch,
  );
  const availableAuditEntityOptions =
    auditFilterOptionsQuery.data?.entityTypes.length
      ? auditFilterOptionsQuery.data.entityTypes
      : auditEntityOptions;
  const availableAuditActionOptions =
    auditFilterOptionsQuery.data?.actionTypes.length
      ? auditFilterOptionsQuery.data.actionTypes
      : auditActionOptions;

  const auditLogsQuery = useAuditLogs(auditFilters, auditPagination);

  const fileDossierLogsQuery = useAuditLogs(
    fileDossierFilters,
    fileDossierPagination,
  );

  const securityLogsQuery = useSecurityLogs({
    ...securityFilters,
    ...securityPagination,
  });

  const auditPaginationModel = useMemo(
    () => convertPaginationToGrid(auditPagination),
    [auditPagination],
  );
  const filePaginationModel = useMemo(
    () => convertPaginationToGrid(fileDossierPagination),
    [fileDossierPagination],
  );
  const securityPaginationModel = useMemo(
    () => convertPaginationToGrid(securityPagination),
    [securityPagination],
  );

  const auditSortModel = useMemo(
    () => convertSortToGrid(auditPagination),
    [auditPagination],
  );
  const fileSortModel = useMemo(
    () => convertSortToGrid(fileDossierPagination),
    [fileDossierPagination],
  );
  const securitySortModel = useMemo(
    () => convertSortToGrid(securityPagination),
    [securityPagination],
  );
  const auditRowCount = useStableRowCount(auditLogsQuery.data?.totalCount);
  const fileDossierRowCount = useStableRowCount(
    fileDossierLogsQuery.data?.totalCount,
  );
  const securityRowCount = useStableRowCount(
    securityLogsQuery.data?.totalCount,
  );

  const updateAuditFilters = (filters: Partial<typeof auditFilters>) => {
    const nextStartDate = filters.startDate ?? auditFilters.startDate;
    const nextEndDate = filters.endDate ?? auditFilters.endDate;
    if (isInvalidDateRange(nextStartDate, nextEndDate)) {
      setAuditDateError(dateRangeErrorText);
      return;
    }

    setAuditDateError("");
    setAuditFilters(filters);
  };

  const updateFileDossierFilters = (
    filters: Partial<typeof fileDossierFilters>,
  ) => {
    const nextStartDate = filters.startDate ?? fileDossierFilters.startDate;
    const nextEndDate = filters.endDate ?? fileDossierFilters.endDate;
    if (isInvalidDateRange(nextStartDate, nextEndDate)) {
      setFileDossierDateError(dateRangeErrorText);
      return;
    }

    setFileDossierDateError("");
    setFileDossierFilters(filters);
  };

  const updateSecurityFilters = (filters: Partial<SecurityTableFilters>) => {
    const nextStartDate = filters.startDate ?? securityFilters.startDate;
    const nextEndDate = filters.endDate ?? securityFilters.endDate;
    if (isInvalidDateRange(nextStartDate, nextEndDate)) {
      setSecurityDateError(dateRangeErrorText);
      return;
    }

    setSecurityDateError("");
    setSecurityFilters(filters);
  };

  const handleResetAuditFilters = () => {
    setAuditFilters({
      startDate: "",
      endDate: "",
      userId: "",
      entityTypes: [],
      actionTypes: [],
    });
    setAuditUserSearch("");
    setAuditDateError("");
  };

  const handleResetFileFilters = () => {
    setFileDossierFilters({
      startDate: "",
      endDate: "",
      userId: "",
      entityTypes: ["Cases", "Dossiers", "Files"],
      actionTypes: [],
    });
    setFileDossierUserSearch("");
    setFileDossierDateError("");
  };

  const handleResetSecurityFilters = () => {
    setSecurityFilters({
      startDate: "",
      endDate: "",
      actorUserId: "",
      targetUserId: "",
      eventTypes: [],
    });
    setSecurityActorUserSearch("");
    setSecurityTargetUserSearch("");
    setSecurityDateError("");
  };

  return (
    <Stack spacing={2.5}>
      <Stack spacing={0.5}>
        <Typography variant="h5" fontWeight={700}>
          داشبورد ممیزی، لاگینگ و گزارش‌گیری
        </Typography>
        {/* <Typography variant="body2" color="text.secondary">
          بررسی کامل تغییرات داده‌ها، تاریخچه موجودیت‌ها، و رخدادهای امنیتی به‌صورت مجزا و مبتنی بر سطح دسترسی.
        </Typography> */}
      </Stack>

      <Paper variant="outlined" sx={{ borderRadius: 2 }}>
        <Tabs
          value={activeTab}
          onChange={(_event, value) => setActiveTab(value)}
          variant="scrollable"
          scrollButtons="auto"
          sx={{
            px: 1,
            "& .MuiTabs-indicator": {
              height: 2,
            },
          }}
        >
          {/* <Tab
            sx={tabSx}
            icon={<HistoryEduIcon fontSize="small" />}
            iconPosition="start"
            label="تاریخچه فعالیت‌ها"
          /> */}
          <Tab
            sx={tabSx}
            icon={<SourceIcon fontSize="small" />}
            iconPosition="start"
            label="تاریخچه  اطلاعات"
          />
          <Tab
            sx={tabSx}
            icon={<LockPersonIcon fontSize="small" />}
            iconPosition="start"
            label="لاگ احراز هویت و امنیت"
          />
        </Tabs>
      </Paper>

      {/* {activeTab === 0 && (
        <Stack spacing={1.5}>
          <AuditFilterBar
            title="نوع عملیات"
            filters={auditFilters}
            users={auditUsersQuery.data ?? []}
            usersLoading={auditUsersQuery.isLoading}
            userSearchText={auditUserSearch}
            onUserSearchTextChange={setAuditUserSearch}
            entityOptions={auditEntityOptions}
            actionOptions={availableAuditActionOptions}
            dateError={auditDateError}
            onChange={updateAuditFilters}
            onReset={handleResetAuditFilters}
          />

          {auditLogsQuery.isError && (
            <QueryErrorAlert
              error={auditLogsQuery.error}
              onRetry={() => void auditLogsQuery.refetch()}
            />
          )}

          <AuditLogsGrid
            logs={auditLogsQuery.data?.items ?? []}
            rowCount={auditRowCount}
            loading={auditLogsQuery.isFetching}
            paginationModel={auditPaginationModel}
            sortModel={auditSortModel}
            onPaginationModelChange={(model) =>
              setAuditPagination({
                pageNumber: model.page + 1,
                pageSize: model.pageSize,
              })
            }
            onSortModelChange={(model) =>
              setAuditPagination(toGridSortFallback(model, auditPagination))
            }
            onOpenDiff={openDiffModal}
          />
        </Stack>
      )} */}

      {activeTab === 0 && (
        <Stack spacing={1.5}>
          <AuditFilterBar
            title="فیلتر تاریخچه اختصاصی پرونده/فایل"
            filters={fileDossierFilters}
            users={fileDossierUsersQuery.data ?? []}
            usersLoading={fileDossierUsersQuery.isLoading}
            userSearchText={fileDossierUserSearch}
            onUserSearchTextChange={setFileDossierUserSearch}
            entityOptions={
              auditFilterOptionsQuery.data?.entityTypes.length
                ? availableAuditEntityOptions
                : fileDossierEntityOptions
            }
            actionOptions={availableAuditActionOptions}
            dateError={fileDossierDateError}
            onChange={updateFileDossierFilters}
            onReset={handleResetFileFilters}
          />

          {fileDossierLogsQuery.isError && (
            <QueryErrorAlert
              error={fileDossierLogsQuery.error}
              onRetry={() => void fileDossierLogsQuery.refetch()}
            />
          )}

          <AuditLogsGrid
            logs={fileDossierLogsQuery.data?.items ?? []}
            rowCount={fileDossierRowCount}
            loading={fileDossierLogsQuery.isFetching}
            paginationModel={filePaginationModel}
            sortModel={fileSortModel}
            onPaginationModelChange={(model) =>
              setFileDossierPagination({
                pageNumber: model.page + 1,
                pageSize: model.pageSize,
              })
            }
            onSortModelChange={(model) =>
              setFileDossierPagination(
                toGridSortFallback(model, fileDossierPagination),
              )
            }
            onOpenDiff={openDiffModal}
          />
        </Stack>
      )}

      {activeTab === 1 && (
        <Stack spacing={1.5}>
          <SecurityFilterBar
            filters={securityFilters}
            actorUsers={securityActorUsersQuery.data ?? []}
            targetUsers={securityTargetUsersQuery.data ?? []}
            actorUserSearchText={securityActorUserSearch}
            targetUserSearchText={securityTargetUserSearch}
            usersLoading={
              securityActorUsersQuery.isLoading ||
              securityTargetUsersQuery.isLoading
            }
            dateError={securityDateError}
            onChange={updateSecurityFilters}
            onActorUserSearchTextChange={setSecurityActorUserSearch}
            onTargetUserSearchTextChange={setSecurityTargetUserSearch}
            onReset={handleResetSecurityFilters}
          />

          {securityLogsQuery.isError && (
            <QueryErrorAlert
              error={securityLogsQuery.error}
              onRetry={() => void securityLogsQuery.refetch()}
            />
          )}

          <SecurityLogsGrid
            logs={securityLogsQuery.data?.items ?? []}
            rowCount={securityRowCount}
            loading={securityLogsQuery.isFetching}
            paginationModel={securityPaginationModel}
            sortModel={securitySortModel}
            onPaginationModelChange={(model) =>
              setSecurityPagination({
                pageNumber: model.page + 1,
                pageSize: model.pageSize,
              })
            }
            onSortModelChange={(model) =>
              setSecurityPagination(
                toGridSortFallback(model, securityPagination),
              )
            }
          />
        </Stack>
      )}

      <AuditDiffModal
        open={isDiffModalOpen}
        log={selectedDiffLog}
        onClose={closeDiffModal}
      />
    </Stack>
  );
};
