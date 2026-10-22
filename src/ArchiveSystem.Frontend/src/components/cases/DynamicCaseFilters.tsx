import AddIcon from "@mui/icons-material/Add";
import DeleteOutlineIcon from "@mui/icons-material/DeleteOutline";
import {
  Alert,
  Box,
  Button,
  FormControl,
  IconButton,
  InputLabel,
  MenuItem,
  Paper,
  Select,
  Stack,
  TextField,
  Typography,
} from "@mui/material";
import { useEffect, useMemo, useState } from "react";
import { JalaliDateInput } from "@/components/common/JalaliDateInputs";
import { useEffectiveDynamicTemplate } from "@/services/archiveQueries";
import type {
  DynamicFieldDefinition,
  DynamicFilterCondition,
  DynamicFilterExpression,
  DynamicFilterLogic,
} from "@/types/archive";
import {
  parseDateLikeValue,
  toGregorianDateString,
  toGregorianDateTimeString,
  toIsoUtcString,
} from "@/utils/jalaliDate";

interface DynamicCaseFiltersProps {
  classificationId: number;
  value?: DynamicFilterExpression;
  onApply: (expression: DynamicFilterExpression | undefined) => void;
  onClose: () => void;
}

interface DraftCondition {
  id: string;
  fieldId: string;
  op: DynamicFilterCondition["op"];
  value: string;
  values: string[];
  fromValue: string;
  toValue: string;
}

interface DraftGroup {
  id: string;
  logic: DynamicFilterLogic;
  conditions: DraftCondition[];
}

const operatorLabels: Record<DynamicFilterCondition["op"], string> = {
  contains: "شامل باشد",
  equals: "مساوی باشد",
  notEquals: "مخالف باشد",
  lessThan: "کوچک‌تر باشد",
  greaterThan: "بزرگ‌تر باشد",
  lessThanOrEqual: "کوچک‌تر یا مساوی باشد",
  greaterThanOrEqual: "بزرگ‌تر یا مساوی باشد",
  range: "بین دو مقدار باشد",
  in: "یکی از مقادیر باشد",
  isEmpty: "بدون مقدار باشد",
};

const createId = () =>
  globalThis.crypto?.randomUUID?.() ??
  `${Date.now()}-${Math.random().toString(16).slice(2)}`;

const operatorsForField = (
  field: DynamicFieldDefinition,
): DynamicFilterCondition["op"][] => {
  if (field.type === "text" || field.type === "textarea") {
    return ["contains", "equals", "notEquals", "isEmpty"];
  }

  if (
    field.type === "int" ||
    field.type === "decimal" ||
    field.type === "date" ||
    field.type === "datetime"
  ) {
    return [
      "equals",
      "notEquals",
      "lessThan",
      "lessThanOrEqual",
      "greaterThan",
      "greaterThanOrEqual",
      "range",
      "isEmpty",
    ];
  }

  if (field.type === "multiselect") {
    return ["in", "notEquals", "isEmpty"];
  }

  return ["equals", "notEquals", "isEmpty"];
};

const createCondition = (
  fields: DynamicFieldDefinition[],
): DraftCondition => {
  const field = fields[0];
  return {
    id: createId(),
    fieldId: field?.id ?? "",
    op: field ? operatorsForField(field)[0] : "equals",
    value: "",
    values: [],
    fromValue: "",
    toValue: "",
  };
};

const normalizeScalar = (
  field: DynamicFieldDefinition,
  value: string,
): unknown => {
  if (field.type === "int" || field.type === "decimal") {
    return Number(value);
  }
  if (field.type === "checkbox") {
    return value === "true";
  }
  if (field.type === "date" || field.type === "datetime") {
    return toIsoUtcString(value);
  }
  return value;
};

const toDraftScalar = (
  field: DynamicFieldDefinition,
  value: unknown,
): string => {
  if (value == null) {
    return "";
  }

  if (field.type === "date" || field.type === "datetime") {
    const parsed = parseDateLikeValue(String(value));
    if (!parsed) {
      return String(value);
    }

    return field.type === "datetime"
      ? toGregorianDateTimeString(parsed)
      : toGregorianDateString(parsed);
  }

  return String(value);
};

const buildFilter = (
  condition: DraftCondition,
  field: DynamicFieldDefinition,
): DynamicFilterCondition | null => {
  if (condition.op === "isEmpty") {
    return { fieldId: field.id, op: condition.op };
  }

  if (condition.op === "range") {
    if (!condition.fromValue || !condition.toValue) {
      return null;
    }
    return {
      fieldId: field.id,
      op: condition.op,
      fromValue: normalizeScalar(field, condition.fromValue),
      toValue: normalizeScalar(field, condition.toValue),
    };
  }

  if (condition.op === "in") {
    return condition.values.length > 0
      ? { fieldId: field.id, op: condition.op, values: condition.values }
      : null;
  }

  if (!condition.value) {
    return null;
  }

  return {
    fieldId: field.id,
    op: condition.op,
    value: normalizeScalar(field, condition.value),
  };
};

export const DynamicCaseFilters = ({
  classificationId,
  value,
  onApply,
  onClose,
}: DynamicCaseFiltersProps) => {
  const templateQuery = useEffectiveDynamicTemplate(classificationId);
  const fields = useMemo(
    () =>
      (templateQuery.data?.groups ?? [])
        .flatMap((group) => group.fields)
        .filter((field) => field.showAsFilter)
        .sort((left, right) => left.order - right.order),
    [templateQuery.data],
  );
  const fieldsById = useMemo(
    () => new Map(fields.map((field) => [field.id, field])),
    [fields],
  );
  const [topLogic, setTopLogic] = useState<DynamicFilterLogic>("and");
  const [groups, setGroups] = useState<DraftGroup[]>([]);

  useEffect(() => {
    setTopLogic(value?.logic ?? "and");
    setGroups(
      (value?.groups ?? [])
        .map((group) => ({
          id: createId(),
          logic: group.logic,
          conditions: group.filters
            .map((filter): DraftCondition | null => {
              const field = fieldsById.get(filter.fieldId);
              if (!field) {
                return null;
              }

              return {
                id: createId(),
                fieldId: filter.fieldId,
                op: filter.op,
                value: toDraftScalar(field, filter.value),
                values: (filter.values ?? []).map(String),
                fromValue: toDraftScalar(field, filter.fromValue),
                toValue: toDraftScalar(field, filter.toValue),
              };
            })
            .filter(
              (condition): condition is DraftCondition => condition != null,
            ),
        }))
        .filter((group) => group.conditions.length > 0),
    );
  }, [classificationId, fieldsById, value]);

  const addGroup = () => {
    setGroups((current) => [
      ...current,
      {
        id: createId(),
        logic: "and",
        conditions: [createCondition(fields)],
      },
    ]);
  };

  const apply = () => {
    const effectiveGroups = groups
      .map((group) => ({
        logic: group.logic,
        filters: group.conditions
          .map((condition) => {
            const field = fieldsById.get(condition.fieldId);
            return field ? buildFilter(condition, field) : null;
          })
          .filter((filter): filter is DynamicFilterCondition => filter != null),
      }))
      .filter((group) => group.filters.length > 0);

    onApply(
      effectiveGroups.length > 0
        ? { logic: topLogic, groups: effectiveGroups }
        : undefined,
    );
  };

  if (templateQuery.isLoading) {
    return <Paper variant="outlined" sx={{ p: 2 }}>در حال بارگذاری فیلدهای قابل فیلتر...</Paper>;
  }

  if (templateQuery.isError) {
    return <Alert severity="error">بارگذاری فیلدهای اطلاعات تکمیلی ناموفق بود.</Alert>;
  }

  return (
    <Paper variant="outlined" sx={{ p: 2, bgcolor: "background.paper" }}>
      <Stack spacing={2}>
        <Stack direction={{ xs: "column", md: "row" }} justifyContent="space-between" gap={1}>
          <Box>
            <Typography fontWeight={700}>فیلتر براساس اطلاعات تکمیلی</Typography>
            <Typography variant="caption" color="text.secondary">
              هر کادر یک گروه شرطی داخل پرانتز است.
            </Typography>
          </Box>
          <Stack direction="row" gap={1}>
            {groups.length > 1 && (
              <FormControl size="small" sx={{ minWidth: 160 }}>
                <InputLabel id="dynamic-groups-logic-label">رابط بین گروه‌ها</InputLabel>
                <Select
                  labelId="dynamic-groups-logic-label"
                  label="رابط بین گروه‌ها"
                  value={topLogic}
                  onChange={(event) => setTopLogic(event.target.value as DynamicFilterLogic)}
                >
                  <MenuItem value="and">و (&&)</MenuItem>
                  <MenuItem value="or">یا (||)</MenuItem>
                </Select>
              </FormControl>
            )}
            <Button startIcon={<AddIcon />} variant="outlined" onClick={addGroup} disabled={fields.length === 0}>
              افزودن گروه
            </Button>
          </Stack>
        </Stack>

        {fields.length === 0 && (
          <Alert severity="info">
            در این دسته‌بندی فیلد فعالی با گزینه «نمایش جهت فیلتر» وجود ندارد.
          </Alert>
        )}

        {groups.map((group, groupIndex) => (
          <Paper key={group.id} variant="outlined" sx={{ p: 1.5, borderStyle: "dashed" }}>
            <Stack spacing={1.5}>
              <Stack direction="row" alignItems="center" justifyContent="space-between">
                <Typography fontWeight={700}>( گروه {groupIndex + 1} )</Typography>
                <Stack direction="row" gap={1}>
                  {group.conditions.length > 1 && (
                    <FormControl size="small" sx={{ minWidth: 145 }}>
                      <InputLabel>رابط شرط‌ها</InputLabel>
                      <Select
                        label="رابط شرط‌ها"
                        value={group.logic}
                        onChange={(event) =>
                          setGroups((current) =>
                            current.map((item) =>
                              item.id === group.id
                                ? { ...item, logic: event.target.value as DynamicFilterLogic }
                                : item,
                            ),
                          )
                        }
                      >
                        <MenuItem value="and">و (&&)</MenuItem>
                        <MenuItem value="or">یا (||)</MenuItem>
                      </Select>
                    </FormControl>
                  )}
                  <IconButton
                    color="error"
                    aria-label={`حذف گروه ${groupIndex + 1}`}
                    onClick={() => setGroups((current) => current.filter((item) => item.id !== group.id))}
                  >
                    <DeleteOutlineIcon />
                  </IconButton>
                </Stack>
              </Stack>

              {group.conditions.map((condition) => {
                const field = fieldsById.get(condition.fieldId);
                const operators = field ? operatorsForField(field) : [];
                return (
                  <Stack key={condition.id} direction={{ xs: "column", lg: "row" }} gap={1} alignItems={{ lg: "center" }}>
                    <FormControl size="small" sx={{ minWidth: 220 }}>
                      <InputLabel>فیلد</InputLabel>
                      <Select
                        label="فیلد"
                        value={condition.fieldId}
                        onChange={(event) => {
                          const nextField = fieldsById.get(event.target.value);
                          setGroups((current) =>
                            current.map((item) =>
                              item.id !== group.id
                                ? item
                                : {
                                    ...item,
                                    conditions: item.conditions.map((entry) =>
                                      entry.id === condition.id
                                        ? {
                                            ...createCondition(nextField ? [nextField] : []),
                                            id: entry.id,
                                          }
                                        : entry,
                                    ),
                                  },
                            ),
                          );
                        }}
                      >
                        {fields.map((item) => (
                          <MenuItem key={item.id} value={item.id}>{item.label}</MenuItem>
                        ))}
                      </Select>
                    </FormControl>

                    <FormControl size="small" sx={{ minWidth: 190 }}>
                      <InputLabel>عملگر</InputLabel>
                      <Select
                        label="عملگر"
                        value={condition.op}
                        onChange={(event) =>
                          setGroups((current) =>
                            current.map((item) =>
                              item.id !== group.id
                                ? item
                                : {
                                    ...item,
                                    conditions: item.conditions.map((entry) =>
                                      entry.id === condition.id
                                        ? { ...entry, op: event.target.value as DynamicFilterCondition["op"] }
                                        : entry,
                                    ),
                                  },
                            ),
                          )
                        }
                      >
                        {operators.map((op) => (
                          <MenuItem key={op} value={op}>{operatorLabels[op]}</MenuItem>
                        ))}
                      </Select>
                    </FormControl>

                    {field && condition.op !== "isEmpty" && condition.op !== "range" && (
                      field.type === "dropdown" || field.type === "multiselect" ? (
                        <FormControl size="small" sx={{ minWidth: 240, flex: 1 }}>
                          <InputLabel>مقدار</InputLabel>
                          <Select
                            multiple={condition.op === "in"}
                            label="مقدار"
                            value={condition.op === "in" ? condition.values : condition.value}
                            onChange={(event) => {
                              const raw = event.target.value;
                              setGroups((current) =>
                                current.map((item) =>
                                  item.id !== group.id
                                    ? item
                                    : {
                                        ...item,
                                        conditions: item.conditions.map((entry) =>
                                          entry.id === condition.id
                                            ? {
                                                ...entry,
                                                value: typeof raw === "string" ? raw : "",
                                                values: Array.isArray(raw) ? raw : [],
                                              }
                                            : entry,
                                        ),
                                      },
                                ),
                              );
                            }}
                          >
                            {field.options.map((option) => (
                              <MenuItem key={option.id} value={option.id}>{option.label}</MenuItem>
                            ))}
                          </Select>
                        </FormControl>
                      ) : field.type === "checkbox" ? (
                        <FormControl size="small" sx={{ minWidth: 180 }}>
                          <InputLabel>مقدار</InputLabel>
                          <Select
                            label="مقدار"
                            value={condition.value}
                            onChange={(event) =>
                              setGroups((current) =>
                                current.map((item) =>
                                  item.id !== group.id
                                    ? item
                                    : {
                                        ...item,
                                        conditions: item.conditions.map((entry) =>
                                          entry.id === condition.id ? { ...entry, value: event.target.value } : entry,
                                        ),
                                      },
                                ),
                              )
                            }
                          >
                            <MenuItem value="true">بله</MenuItem>
                            <MenuItem value="false">خیر</MenuItem>
                          </Select>
                        </FormControl>
                      ) : field.type === "date" || field.type === "datetime" ? (
                        <Box sx={{ minWidth: 230, flex: 1 }}>
                          <JalaliDateInput
                            value={condition.value}
                            onChange={(value) =>
                              setGroups((current) =>
                                current.map((item) =>
                                  item.id !== group.id
                                    ? item
                                    : {
                                        ...item,
                                        conditions: item.conditions.map((entry) =>
                                          entry.id === condition.id ? { ...entry, value } : entry,
                                        ),
                                      },
                                ),
                              )
                            }
                          />
                        </Box>
                      ) : (
                        <TextField
                          size="small"
                          type={field.type === "int" || field.type === "decimal" ? "number" : "text"}
                          label="مقدار"
                          value={condition.value}
                          onChange={(event) =>
                            setGroups((current) =>
                              current.map((item) =>
                                item.id !== group.id
                                  ? item
                                  : {
                                      ...item,
                                      conditions: item.conditions.map((entry) =>
                                        entry.id === condition.id ? { ...entry, value: event.target.value } : entry,
                                      ),
                                    },
                              ),
                            )
                          }
                          sx={{ minWidth: 240, flex: 1 }}
                        />
                      )
                    )}

                    {field && condition.op === "range" && (
                      <>
                        <TextField
                          size="small"
                          label={field.type === "date" ? "از تاریخ" : "از"}
                          type={field.type === "date" ? "text" : "number"}
                          value={condition.fromValue}
                          onChange={(event) =>
                            setGroups((current) =>
                              current.map((item) =>
                                item.id !== group.id
                                  ? item
                                  : {
                                      ...item,
                                      conditions: item.conditions.map((entry) =>
                                        entry.id === condition.id ? { ...entry, fromValue: event.target.value } : entry,
                                      ),
                                    },
                              ),
                            )
                          }
                        />
                        <TextField
                          size="small"
                          label={field.type === "date" ? "تا تاریخ" : "تا"}
                          type={field.type === "date" ? "text" : "number"}
                          value={condition.toValue}
                          onChange={(event) =>
                            setGroups((current) =>
                              current.map((item) =>
                                item.id !== group.id
                                  ? item
                                  : {
                                      ...item,
                                      conditions: item.conditions.map((entry) =>
                                        entry.id === condition.id ? { ...entry, toValue: event.target.value } : entry,
                                      ),
                                    },
                              ),
                            )
                          }
                        />
                      </>
                    )}

                    <IconButton
                      color="error"
                      aria-label="حذف شرط"
                      onClick={() =>
                        setGroups((current) =>
                          current.map((item) =>
                            item.id === group.id
                              ? { ...item, conditions: item.conditions.filter((entry) => entry.id !== condition.id) }
                              : item,
                          ),
                        )
                      }
                    >
                      <DeleteOutlineIcon />
                    </IconButton>
                  </Stack>
                );
              })}

              <Button
                size="small"
                startIcon={<AddIcon />}
                onClick={() =>
                  setGroups((current) =>
                    current.map((item) =>
                      item.id === group.id
                        ? { ...item, conditions: [...item.conditions, createCondition(fields)] }
                        : item,
                    ),
                  )
                }
              >
                افزودن شرط
              </Button>
            </Stack>
          </Paper>
        ))}

        <Stack direction="row" gap={1} justifyContent="flex-end">
          <Button onClick={onClose}>بستن</Button>
          <Button
            color="inherit"
            onClick={() => {
              setGroups([]);
              onApply(undefined);
            }}
          >
            پاک‌کردن فیلتر پویا
          </Button>
          <Button variant="contained" onClick={apply}>اعمال فیلتر</Button>
        </Stack>
      </Stack>
    </Paper>
  );
};
