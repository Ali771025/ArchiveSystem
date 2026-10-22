import {
  Box,
  Checkbox,
  Chip,
  FormControl,
  FormControlLabel,
  FormHelperText,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  Tab,
  Tabs,
  TextField,
} from '@mui/material';
import { useMemo, useState } from 'react';
import { JalaliDateInput, JalaliDateTimeInput } from '@/components/common/JalaliDateInputs';
import type { CaseDynamicValue, DynamicFieldDefinition, DynamicTemplate } from '@/types/archive';

interface DynamicFormRendererProps {
  template: DynamicTemplate | null;
  values: CaseDynamicValue[];
  disabled?: boolean;
  onChange: (values: CaseDynamicValue[]) => void;
}

export type ValidationErrors = Record<string, string>;

const getValueMap = (values: CaseDynamicValue[]): Map<string, CaseDynamicValue> =>
  new Map(values.map((item) => [item.fieldId, item]));

const normalizeValue = (field: DynamicFieldDefinition, raw: string | number | boolean): string | number | boolean | null => {
  if (field.type === 'checkbox') {
    return Boolean(raw);
  }

  if (field.type === 'int' || field.type === 'decimal') {
    if (raw === '') {
      return null;
    }

    const parsed = Number(raw);
    return Number.isFinite(parsed) ? parsed : null;
  }

  if (raw === '') {
    return null;
  }

  return String(raw);
};

export const validateDynamicValues = (
  template: DynamicTemplate | null,
  values: CaseDynamicValue[],
): ValidationErrors => {
  if (!template) {
    return {};
  }

  const valueMap = getValueMap(values);
  const errors: ValidationErrors = {};

  template.groups.forEach((group) => {
    group.fields.forEach((field) => {
      const current = valueMap.get(field.id);
      const rawValue = current?.value;
      const stringValue = rawValue == null ? '' : String(rawValue);

      if (field.required) {
        const isOptionField = field.type === 'dropdown' || field.type === 'multiselect';
        const isMissing = isOptionField
          ? (current?.values?.length ?? 0) === 0
          : field.type === 'checkbox'
            ? rawValue === null || rawValue === undefined
            : stringValue.trim() === '';
        if (isMissing) {
          errors[field.id] = 'این فیلد الزامی است.';
          return;
        }
      }

      if (field.type !== 'multiselect' && field.type !== 'dropdown' && field.rules.regex && stringValue) {
        const regex = new RegExp(field.rules.regex);
        if (!regex.test(stringValue)) {
          errors[field.id] = field.rules.errorMessage || 'مقدار وارد شده معتبر نیست.';
          return;
        }
      }

      if ((field.type === 'int' || field.type === 'decimal') && rawValue != null) {
        const numeric = Number(rawValue);
        if (Number.isFinite(field.rules.min) && numeric < Number(field.rules.min)) {
          errors[field.id] = `حداقل مقدار ${field.rules.min} است.`;
          return;
        }

        if (Number.isFinite(field.rules.max) && numeric > Number(field.rules.max)) {
          errors[field.id] = `حداکثر مقدار ${field.rules.max} است.`;
        }
      }
    });
  });

  return errors;
};

const upsertValue = (
  values: CaseDynamicValue[],
  next: CaseDynamicValue,
): CaseDynamicValue[] => {
  const index = values.findIndex((item) => item.fieldId === next.fieldId);
  if (index === -1) {
    return [...values, next];
  }

  const copied = [...values];
  copied[index] = next;
  return copied;
};

export const DynamicFormRenderer = ({ template, values, onChange, disabled = false }: DynamicFormRendererProps) => {
  const [activeGroup, setActiveGroup] = useState(0);
  const valueMap = useMemo(() => getValueMap(values), [values]);
  const validation = useMemo(() => validateDynamicValues(template, values), [template, values]);

  if (!template || template.groups.length === 0) {
    return <Box sx={{ p: 2, color: 'text.secondary' }}>الگوی فعالی برای این طبقه‌بندی ثبت نشده است.</Box>;
  }

  const groups = [...template.groups].sort((a, b) => a.order - b.order);
  const group = groups[activeGroup] ?? groups[0];

  return (
    <Stack spacing={2}>
      <Tabs
        value={activeGroup}
        onChange={(_event, value) => setActiveGroup(value)}
        variant="scrollable"
        scrollButtons="auto"
        aria-label="dynamic-field-groups"
      >
        {groups.map((item, index) => (
          <Tab key={item.id} label={item.title} value={index} />
        ))}
      </Tabs>

      <Stack spacing={2}>
        {group.fields
          .slice()
          .sort((a, b) => a.order - b.order)
          .map((field) => {
            const current = valueMap.get(field.id);
            const value = current?.value;
            const error = validation[field.id];

            if (field.type === 'date' || field.type === 'datetime') {
              return (
                <FormControl key={field.id} fullWidth error={Boolean(error)} disabled={disabled}>
                  <FormHelperText sx={{ margin: 0, mb: 0.5, color: 'text.primary' }}>{field.label}</FormHelperText>
                  {field.type === 'date' ? (
                    <JalaliDateInput
                      value={value == null ? '' : String(value)}
                      onChange={(nextValue) => {
                        onChange(
                          upsertValue(values, {
                            fieldId: field.id,
                            fieldType: field.type,
                            value: normalizeValue(field, nextValue),
                            values: [],
                          }),
                        );
                      }}
                      disabled={disabled}
                    />
                  ) : (
                    <JalaliDateTimeInput
                      value={value == null ? '' : String(value)}
                      onChange={(nextValue) => {
                        onChange(
                          upsertValue(values, {
                            fieldId: field.id,
                            fieldType: field.type,
                            value: normalizeValue(field, nextValue),
                            values: [],
                          }),
                        );
                      }}
                      disabled={disabled}
                    />
                  )}
                  {error && <FormHelperText>{error}</FormHelperText>}
                </FormControl>
              );
            }

            if (field.type === 'checkbox') {
              return (
                <FormControl key={field.id} error={Boolean(error)}>
                  <FormControlLabel
                    label={field.label}
                    control={
                      <Checkbox
                        checked={Boolean(value)}
                        disabled={disabled}
                        onChange={(event) => {
                          onChange(
                            upsertValue(values, {
                              fieldId: field.id,
                              fieldType: field.type,
                              value: event.target.checked,
                              values: [],
                            }),
                          );
                        }}
                      />
                    }
                  />
                  {error && <FormHelperText>{error}</FormHelperText>}
                </FormControl>
              );
            }

            if (field.type === 'dropdown') {
              const labelId = `dynamic-field-${field.id}-label`;
              const selectedOptionId = current?.values?.[0]
                ?? field.options.find((option) => option.id === value || option.value === value)?.id
                ?? '';

              return (
                <FormControl key={field.id} fullWidth error={Boolean(error)}>
                  <InputLabel id={labelId}>{field.label}</InputLabel>
                  <Select
                    labelId={labelId}
                    label={field.label}
                    disabled={disabled}
                    value={selectedOptionId}
                    onChange={(event) => {
                      const optionId = String(event.target.value);
                      onChange(
                        upsertValue(values, {
                          fieldId: field.id,
                          fieldType: field.type,
                          value: null,
                          values: optionId ? [optionId] : [],
                        }),
                      );
                    }}
                  >
                    <MenuItem value="">
                      <em>انتخاب نشده</em>
                    </MenuItem>
                    {field.options.map((option) => (
                      <MenuItem key={option.id} value={option.id}>
                        {option.label}
                      </MenuItem>
                    ))}
                  </Select>
                  {error && <FormHelperText>{error}</FormHelperText>}
                </FormControl>
              );
            }

            if (field.type === 'multiselect') {
              const labelId = `dynamic-field-${field.id}-label`;
              const selectedOptionIds = current?.values ?? [];

              return (
                <FormControl key={field.id} fullWidth error={Boolean(error)} disabled={disabled}>
                  <InputLabel id={labelId}>{field.label}</InputLabel>
                  <Select
                    labelId={labelId}
                    multiple
                    label={field.label}
                    value={selectedOptionIds}
                    onChange={(event) => {
                      const selected = event.target.value;
                      const optionIds = typeof selected === 'string' ? selected.split(',') : selected;
                      onChange(
                        upsertValue(values, {
                          fieldId: field.id,
                          fieldType: field.type,
                          value: null,
                          values: optionIds,
                        }),
                      );
                    }}
                    renderValue={(selected) => (
                      <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5 }}>
                        {(selected as string[]).map((optionId) => {
                          const option = field.options.find((item) => item.id === optionId);
                          return <Chip key={optionId} size="small" label={option?.label ?? optionId} />;
                        })}
                      </Box>
                    )}
                  >
                    {field.options.map((option) => (
                      <MenuItem key={option.id} value={option.id}>
                        <Checkbox checked={selectedOptionIds.includes(option.id)} size="small" />
                        {option.label}
                      </MenuItem>
                    ))}
                  </Select>
                  {field.options.length === 0 && (
                    <FormHelperText>برای این فیلد گزینه‌ای تعریف نشده است.</FormHelperText>
                  )}
                  {error && <FormHelperText>{error}</FormHelperText>}
                </FormControl>
              );
            }

            const inputType =
              field.type === 'time'
                ? 'time'
                : field.type === 'int' || field.type === 'decimal'
                  ? 'number'
                  : 'text';

            return (
              <TextField
                key={field.id}
                fullWidth
                type={inputType}
                label={field.label}
                disabled={disabled}
                multiline={field.type === 'textarea'}
                minRows={field.type === 'textarea' ? 3 : undefined}
                value={value == null ? '' : String(value)}
                error={Boolean(error)}
                helperText={error}
                onChange={(event) => {
                  onChange(
                    upsertValue(values, {
                      fieldId: field.id,
                      fieldType: field.type,
                      value: normalizeValue(field, event.target.value),
                      values: [],
                    }),
                  );
                }}
              />
            );
          })}
      </Stack>
    </Stack>
  );
};
