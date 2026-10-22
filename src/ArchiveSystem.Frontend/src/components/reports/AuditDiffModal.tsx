import CloseIcon from "@mui/icons-material/Close";
import {
  Box,
  Chip,
  Dialog,
  DialogContent,
  DialogTitle,
  IconButton,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Typography,
} from "@mui/material";
import { useMemo } from "react";
import type { AuditLogResponseDto } from "@/features/reports/types";
import { getAuditEntityLabel } from "@/features/reports/reportLabels";

interface AuditDiffModalProps {
  open: boolean;
  log: AuditLogResponseDto | null;
  onClose: () => void;
}

interface DiffRow {
  path: string;
  oldValue: string;
  newValue: string;
  status: "Changed" | "Added" | "Removed";
}

const safeParseJson = (value?: string | null): unknown => {
  if (!value) {
    return {};
  }

  try {
    return JSON.parse(value);
  } catch {
    return {};
  }
};

const flattenObject = (
  value: unknown,
  prefix = "",
): Record<string, unknown> => {
  if (value === null || value === undefined) {
    return prefix ? { [prefix]: value } : {};
  }

  if (Array.isArray(value)) {
    return value.reduce<Record<string, unknown>>((acc, item, index) => {
      const path = prefix ? `${prefix}[${index}]` : `[${index}]`;
      Object.assign(acc, flattenObject(item, path));
      return acc;
    }, {});
  }

  if (typeof value === "object") {
    return Object.entries(value as Record<string, unknown>).reduce<
      Record<string, unknown>
    >((acc, [key, nestedValue]) => {
      const path = prefix ? `${prefix}.${key}` : key;
      Object.assign(acc, flattenObject(nestedValue, path));
      return acc;
    }, {});
  }

  return prefix ? { [prefix]: value } : {};
};

const toDisplayValue = (value: unknown) => {
  if (value === null) {
    return "null";
  }

  if (value === undefined) {
    return "-";
  }

  if (typeof value === "string") {
    return value;
  }

  return JSON.stringify(value);
};

const getStatusColor = (status: DiffRow["status"]) => {
  switch (status) {
    case "Added":
      return "success";
    case "Removed":
      return "error";
    default:
      return "warning";
  }
};

export const AuditDiffModal = ({ open, log, onClose }: AuditDiffModalProps) => {
  const { oldJson, newJson, rows } = useMemo(() => {
    if (!log) {
      return {
        oldJson: {},
        newJson: {},
        rows: [] as DiffRow[],
      };
    }

    const oldJson = safeParseJson(log.oldValues);
    const newJson = safeParseJson(log.newValues);

    const oldFlat = flattenObject(oldJson);
    const newFlat = flattenObject(newJson);
    const keys = Array.from(
      new Set([...Object.keys(oldFlat), ...Object.keys(newFlat)]),
    ).sort((a, b) => a.localeCompare(b, "fa"));

    const rows = keys
      .map<DiffRow | null>((key) => {
        const oldValue = oldFlat[key];
        const newValue = newFlat[key];

        if (oldValue === undefined && newValue !== undefined) {
          return {
            path: key,
            oldValue: "-",
            newValue: toDisplayValue(newValue),
            status: "Added",
          };
        }

        if (oldValue !== undefined && newValue === undefined) {
          return {
            path: key,
            oldValue: toDisplayValue(oldValue),
            newValue: "-",
            status: "Removed",
          };
        }

        const before = JSON.stringify(oldValue);
        const after = JSON.stringify(newValue);
        if (before === after) {
          return null;
        }

        return {
          path: key,
          oldValue: toDisplayValue(oldValue),
          newValue: toDisplayValue(newValue),
          status: "Changed",
        };
      })
      .filter((item): item is DiffRow => item !== null);

    return { oldJson, newJson, rows };
  }, [log]);

  return (
    <Dialog open={open} onClose={onClose} maxWidth="xl" fullWidth>
      <DialogTitle sx={{ pb: 1 }}>
        <Stack
          direction="row"
          justifyContent="space-between"
          alignItems="center"
          spacing={1}
        >
          <Box>
            <Typography variant="h6" fontWeight={700}>
              مقایسه تغییرات رکورد
            </Typography>
            {log && (
              <Typography variant="caption" color="text.secondary">
                {getAuditEntityLabel(log.tableName)} / شناسه رکورد: {log.recordId}
              </Typography>
            )}
          </Box>
          <IconButton onClick={onClose} aria-label="بستن">
            <CloseIcon />
          </IconButton>
        </Stack>
      </DialogTitle>
      <DialogContent>
        <Stack spacing={2}>
          <Paper variant="outlined" sx={{ maxHeight: 320, overflow: "auto" }}>
            <Table size="small" stickyHeader>
              <TableHead>
                <TableRow>
                  <TableCell>فیلد</TableCell>
                  <TableCell>مقدار قبلی</TableCell>
                  <TableCell>مقدار جدید</TableCell>
                  <TableCell width={120}>وضعیت</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {rows.map((row) => (
                  <TableRow key={row.path} hover>
                    <TableCell sx={{ fontFamily: "monospace", fontSize: 12 }}>
                      {row.path}
                    </TableCell>
                    <TableCell
                      sx={{
                        color:
                          row.status === "Added"
                            ? "text.disabled"
                            : "error.main",
                        fontFamily: "monospace",
                      }}
                    >
                      {row.oldValue}
                    </TableCell>
                    <TableCell
                      sx={{
                        color:
                          row.status === "Removed"
                            ? "text.disabled"
                            : "success.main",
                        fontFamily: "monospace",
                      }}
                    >
                      {row.newValue}
                    </TableCell>
                    <TableCell>
                      <Chip
                        size="small"
                        label={row.status}
                        color={
                          getStatusColor(row.status) as
                            | "success"
                            | "error"
                            | "warning"
                        }
                      />
                    </TableCell>
                  </TableRow>
                ))}
                {rows.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={4} align="center">
                      تغییری برای نمایش وجود ندارد.
                    </TableCell>
                  </TableRow>
                )}
              </TableBody>
            </Table>
          </Paper>

          <Stack direction={{ xs: "column", md: "row" }} spacing={1.5}>
            <Paper
              variant="outlined"
              sx={{
                flex: 1,
                p: 1.5,
                bgcolor: "background.default",
                overflow: "auto",
                maxHeight: 260,
              }}
            >
              <Typography variant="subtitle2" gutterBottom>
                مقدار قبلی (OldValues)
              </Typography>
              <Box
                component="pre"
                sx={{ m: 0, fontSize: 12, direction: "ltr", textAlign: "left" }}
              >
                {JSON.stringify(oldJson, null, 2)}
              </Box>
            </Paper>

            <Paper
              variant="outlined"
              sx={{
                flex: 1,
                p: 1.5,
                bgcolor: "background.default",
                overflow: "auto",
                maxHeight: 260,
              }}
            >
              <Typography variant="subtitle2" gutterBottom>
                مقدار جدید (NewValues)
              </Typography>
              <Box
                component="pre"
                sx={{ m: 0, fontSize: 12, direction: "ltr", textAlign: "left" }}
              >
                {JSON.stringify(newJson, null, 2)}
              </Box>
            </Paper>
          </Stack>
        </Stack>
      </DialogContent>
    </Dialog>
  );
};
