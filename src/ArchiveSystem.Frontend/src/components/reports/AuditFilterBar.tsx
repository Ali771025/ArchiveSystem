import {
  Autocomplete,
  Box,
  Button,
  Checkbox,
  Chip,
  FormControl,
  InputLabel,
  ListItemText,
  MenuItem,
  OutlinedInput,
  Paper,
  Select,
  Stack,
  TextField,
  Typography,
} from "@mui/material";
import { JalaliDatePicker } from "@/components/reports/JalaliDatePicker";
import type {
  AuditActionType,
  AuditTableFilters,
  ReportUserOption,
} from "@/features/reports/types";
import {
  getAuditActionLabel,
  getAuditEntityLabel,
} from "@/features/reports/reportLabels";

const normalizeMultiSelectValue = (value: string | string[]) =>
  Array.isArray(value)
    ? value
    : value
        .split(",")
        .map((item) => item.trim())
        .filter(Boolean);

interface AuditFilterBarProps {
  title: string;
  filters: AuditTableFilters;
  users: ReportUserOption[];
  userSearchText: string;
  usersLoading: boolean;
  entityOptions: string[];
  actionOptions: AuditActionType[];
  dateError?: string;
  onChange: (filters: Partial<AuditTableFilters>) => void;
  onUserSearchTextChange: (search: string) => void;
  onReset: () => void;
}

export const AuditFilterBar = ({
  title,
  filters,
  users,
  userSearchText,
  usersLoading,
  entityOptions,
  actionOptions,
  dateError,
  onChange,
  onUserSearchTextChange,
  onReset,
}: AuditFilterBarProps) => {
  const selectedUser =
    users.find((option) => String(option.id) === filters.userId) ??
    (filters.userId && userSearchText.trim()
      ? {
          id: Number(filters.userId),
          username: userSearchText,
          fullName: "",
          label: userSearchText,
        }
      : null);
  const filterInputSx = {
    "& .MuiInputBase-root": { height: 40, borderRadius: 1.5, fontSize: 14 },
  };

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
          spacing={1}
          justifyContent="space-between"
          alignItems={{ xs: "flex-start", md: "center" }}
        >
          <Typography variant="subtitle1" fontWeight={700}>
            {title}
          </Typography>
          <Button size="small" variant="text" onClick={onReset}>
            پاک‌سازی فیلترها
          </Button>
        </Stack>

        <Box
          sx={{
            display: "grid",
            gap: 1.5,
            alignItems: "end",
            gridTemplateColumns: {
              xs: "1fr",
              md: "repeat(12, minmax(0, 1fr))",
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

          <Box sx={{ gridColumn: { md: "span 3" } }}>
            <Autocomplete
              options={users}
              loading={usersLoading}
              value={selectedUser}
              inputValue={userSearchText}
              onInputChange={(_, newValue, reason) => {
                if (reason !== "reset") {
                  onUserSearchTextChange(newValue);
                }
              }}
              onChange={(_, user) => {
                onChange({ userId: user ? String(user.id) : "" });
                onUserSearchTextChange(user?.label ?? "");
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

          <Box sx={{ gridColumn: { md: "span 3" } }}>
            <FormControl fullWidth sx={filterInputSx}>
              <InputLabel id="entity-types-label">موضوع عملیات</InputLabel>
              <Select
                labelId="entity-types-label"
                multiple
                value={filters.entityTypes}
                onChange={(event) =>
                  onChange({
                    entityTypes: normalizeMultiSelectValue(event.target.value),
                  })
                }
                input={<OutlinedInput label="موضوع عملیات" />}
                renderValue={(selected) => (
                  <Box sx={{ display: "flex", flexWrap: "wrap", gap: 0.5 }}>
                    {(selected as string[]).map((value) => (
                      <Chip key={value} label={getAuditEntityLabel(value)} size="small" />
                    ))}
                  </Box>
                )}
              >
                {entityOptions.map((entity) => (
                  <MenuItem key={entity} value={entity}>
                    <Checkbox
                      checked={filters.entityTypes.includes(entity)}
                      size="small"
                    />
                    <ListItemText primary={getAuditEntityLabel(entity)} />
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
          </Box>

          <Box sx={{ gridColumn: { md: "span 2" } }}>
            <FormControl fullWidth sx={filterInputSx}>
              <InputLabel id="action-type-label">نوع عملیات</InputLabel>
              <Select
                labelId="action-type-label"
                multiple
                value={filters.actionTypes}
                onChange={(event) =>
                  onChange({
                    actionTypes: normalizeMultiSelectValue(
                      event.target.value,
                    ) as AuditActionType[],
                  })
                }
                input={<OutlinedInput label="نوع عملیات" />}
                renderValue={(selected) => (
                  <Box sx={{ display: "flex", flexWrap: "wrap", gap: 0.5 }}>
                    {(selected as string[]).map((value) => (
                      <Chip key={value} label={getAuditActionLabel(value)} size="small" />
                    ))}
                  </Box>
                )}
              >
                {actionOptions.map((action) => (
                  <MenuItem key={action} value={action}>
                    <Checkbox
                      checked={filters.actionTypes.includes(action)}
                      size="small"
                    />
                    <ListItemText primary={getAuditActionLabel(action)} />
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
