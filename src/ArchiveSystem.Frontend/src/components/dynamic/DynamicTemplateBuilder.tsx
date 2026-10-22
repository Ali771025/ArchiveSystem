import AddIcon from '@mui/icons-material/Add';
import DeleteOutlineIcon from '@mui/icons-material/DeleteOutline';
import DragIndicatorIcon from '@mui/icons-material/DragIndicator';
import SaveIcon from '@mui/icons-material/Save';
import {
  Alert,
  Box,
  Button,
  Divider,
  FormControl,
  FormControlLabel,
  IconButton,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  Switch,
  Tab,
  Tabs,
  TextField,
  Typography,
  useMediaQuery,
  useTheme,
} from '@mui/material';
import { useEffect, useMemo, useState } from 'react';
import { useSaveTemplate, useDynamicTemplate } from '@/services/archiveQueries';
import {
  dynamicTemplateWriteForbiddenMessage,
  mapApiError,
} from '@/services/errorMapper';
import { useToastStore } from '@/stores/useToastStore';
import type { DynamicFieldDefinition, DynamicFieldGroup, DynamicTemplate } from '@/types/archive';
import { createClientId } from '@/utils/helpers';

const fieldTypes: Array<DynamicFieldDefinition['type']> = [
  'text',
  'dropdown',
  'multiselect',
  'date',
  'datetime',
  'time',
  'int',
  'decimal',
  'checkbox',
  'textarea',
];

const filterTypeOptions: Array<{ value: DynamicFieldDefinition['filterType']; label: string }> = [
  { value: 'contains', label: 'شامل شدن' },
  { value: 'equals', label: 'برابر بودن' },
  { value: 'notEquals', label: 'مخالف' },
  { value: 'lessThan', label: 'کوچکتر' },
  { value: 'greaterThan', label: 'بزرگتر' },
  { value: 'lessThanOrEqual', label: 'کوچکتر مساوی' },
  { value: 'greaterThanOrEqual', label: 'بزرگتر مساوی' },
  { value: 'range', label: 'بازه' },
];

const filterTypesForField = (fieldType: DynamicFieldDefinition['type']): DynamicFieldDefinition['filterType'][] => {
  if (fieldType === 'text' || fieldType === 'textarea') {
    return ['contains', 'equals', 'notEquals'];
  }

  if (fieldType === 'date' || fieldType === 'datetime' || fieldType === 'time' || fieldType === 'int' || fieldType === 'decimal') {
    return ['equals', 'notEquals', 'lessThan', 'greaterThan', 'lessThanOrEqual', 'greaterThanOrEqual', 'range'];
  }

  return ['equals', 'notEquals'];
};

const createEmptyTemplate = (classificationId: number): DynamicTemplate => ({
  id: createClientId(),
  classificationId,
  name: `Template ${classificationId}`,
  isActive: true,
  version: 1,
  publishedAt: new Date().toISOString(),
  groups: [],
});

const createField = (): DynamicFieldDefinition => ({
  id: createClientId(),
  apiName: '',
  label: '',
  type: 'text',
  required: false,
  showAsFilter: false,
  filterType: 'contains',
  order: 0,
  options: [],
  rules: {},
});

interface DynamicTemplateBuilderProps {
  classificationId: number;
}

export const DynamicTemplateBuilder = ({ classificationId }: DynamicTemplateBuilderProps) => {
  const theme = useTheme();
  const isCompact = useMediaQuery(theme.breakpoints.down('md'));
  const templateQuery = useDynamicTemplate(classificationId);
  const saveMutation = useSaveTemplate();
  const pushToast = useToastStore((state) => state.push);

  const [template, setTemplate] = useState<DynamicTemplate>(createEmptyTemplate(classificationId));
  const [activeGroup, setActiveGroup] = useState(0);
  const [draggingGroupId, setDraggingGroupId] = useState<string | null>(null);
  const [draggingFieldId, setDraggingFieldId] = useState<string | null>(null);

  useEffect(() => {
    const fetched = templateQuery.data;
    if (fetched) {
      setTemplate(fetched);
      setActiveGroup(0);
      return;
    }

    setTemplate(createEmptyTemplate(classificationId));
  }, [templateQuery.data, classificationId]);

  const groups = useMemo(() => template.groups.slice().sort((a, b) => a.order - b.order), [template.groups]);
  const active = groups[activeGroup] ?? null;

  const updateGroup = (groupId: string, updater: (group: DynamicFieldGroup) => DynamicFieldGroup) => {
    setTemplate((current) => ({
      ...current,
      groups: current.groups.map((group) => (group.id === groupId ? updater(group) : group)),
    }));
  };

  const normalizeOrders = (value: DynamicTemplate): DynamicTemplate => ({
    ...value,
    groups: value.groups
      .map((group, groupIndex) => ({
        ...group,
        order: groupIndex,
        fields: group.fields
          .sort((a, b) => a.order - b.order)
          .map((field, fieldIndex) => ({
            ...field,
            order: fieldIndex,
          })),
      }))
      .sort((a, b) => a.order - b.order),
  });

  const saveTemplate = async () => {
    const payload = normalizeOrders(template);
    try {
      await saveMutation.mutateAsync({ classificationId, template: payload });
      pushToast('الگو با موفقیت ذخیره شد.', 'success');
    } catch (error) {
      const mappedError = mapApiError(error);
      pushToast(
        mappedError.status === 403
          ? dynamicTemplateWriteForbiddenMessage
          : mappedError.message,
        'error',
      );
    }
  };

  return (
    <Stack spacing={2}>
      <Alert severity="warning">ذخیره الگو ممکن است روی پرونده‌های موجود اثر بگذارد. قبل از انتشار، تغییرات را بررسی کنید.</Alert>

      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1} justifyContent="space-between" alignItems={{ xs: 'stretch', sm: 'center' }}>
        <Typography variant="h5">مدیریت الگوی فیلدهای پویا</Typography>
        <Button
          variant="contained"
          startIcon={<SaveIcon />}
          onClick={saveTemplate}
          disabled={saveMutation.isPending}
          fullWidth={isCompact}
        >
          ذخیره و انتشار نسخه
        </Button>
      </Stack>

      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1} alignItems={{ xs: 'stretch', sm: 'center' }}>
        <TextField
          label="نام قالب"
          value={template.name ?? ''}
          onChange={(event) => setTemplate((current) => ({ ...current, name: event.target.value }))}
          fullWidth
        />
        <Button
          variant="outlined"
          startIcon={<AddIcon />}
          onClick={() => {
            setTemplate((current) => ({
              ...current,
              groups: [
                ...current.groups,
                        {
                          id: createClientId(),
                  title: 'گروه جدید',
                  apiName: `group_${current.groups.length + 1}`,
                  order: current.groups.length,
                  fields: [],
                },
              ],
            }));
          }}
          sx={{ width: { xs: '100%', sm: 'auto' } }}
        >
          افزودن گروه
        </Button>
      </Stack>

      {groups.length === 0 ? (
        <Alert severity="info">برای این طبقه‌بندی گروهی تعریف نشده است.</Alert>
      ) : (
        <>
          <Tabs
            value={activeGroup}
            onChange={(_event, value) => setActiveGroup(value)}
            variant="scrollable"
            scrollButtons="auto"
          >
            {groups.map((group, index) => (
              <Tab key={group.id} label={group.title || `گروه ${index + 1}`} />
            ))}
          </Tabs>

          {active && (
            <Stack spacing={2}>
              <Stack
                direction={{ xs: 'column', md: 'row' }}
                spacing={1}
                alignItems={{ xs: 'stretch', md: 'center' }}
                draggable
                onDragStart={() => setDraggingGroupId(active.id)}
                onDragOver={(event) => event.preventDefault()}
                onDrop={() => {
                  if (!draggingGroupId || draggingGroupId === active.id) {
                    return;
                  }

                  const items = [...groups];
                  const from = items.findIndex((item) => item.id === draggingGroupId);
                  const to = items.findIndex((item) => item.id === active.id);
                  if (from < 0 || to < 0) {
                    return;
                  }

                  const [dragged] = items.splice(from, 1);
                  items.splice(to, 0, dragged);
                  setTemplate((current) => ({ ...current, groups: items.map((item, index) => ({ ...item, order: index })) }));
                }}
              >
                <DragIndicatorIcon color="action" />
                <TextField
                  label="عنوان گروه"
                  value={active.title}
                  onChange={(event) => updateGroup(active.id, (group) => ({ ...group, title: event.target.value }))}
                  fullWidth
                />
                <TextField
                  label="apiName"
                  value={active.apiName}
                  onChange={(event) => updateGroup(active.id, (group) => ({ ...group, apiName: event.target.value }))}
                  fullWidth
                />
                <IconButton
                  color="error"
                  onClick={() => {
                    setTemplate((current) => ({ ...current, groups: current.groups.filter((group) => group.id !== active.id) }));
                    setActiveGroup(0);
                  }}
                >
                  <DeleteOutlineIcon />
                </IconButton>
              </Stack>

              <Divider />

              <Button
                variant="outlined"
                onClick={() => {
                  updateGroup(active.id, (group) => ({
                    ...group,
                    fields: [
                      ...group.fields,
                      {
                        ...createField(),
                        order: group.fields.length,
                      },
                    ],
                  }));
                }}
                startIcon={<AddIcon />}
              >
                افزودن فیلد
              </Button>

              {active.fields.length === 0 ? (
                <Alert severity="info">فیلدی در این گروه تعریف نشده است.</Alert>
              ) : (
                active.fields
                  .slice()
                  .sort((a, b) => a.order - b.order)
                  .map((field) => (
                    <Box
                      key={field.id}
                      sx={{ border: '1px solid', borderColor: 'divider', borderRadius: 1, p: 2 }}
                      draggable
                      onDragStart={() => setDraggingFieldId(field.id)}
                      onDragOver={(event) => event.preventDefault()}
                      onDrop={() => {
                        if (!draggingFieldId || draggingFieldId === field.id) {
                          return;
                        }

                        updateGroup(active.id, (group) => {
                          const fields = [...group.fields];
                          const from = fields.findIndex((item) => item.id === draggingFieldId);
                          const to = fields.findIndex((item) => item.id === field.id);
                          if (from < 0 || to < 0) {
                            return group;
                          }

                          const [dragged] = fields.splice(from, 1);
                          fields.splice(to, 0, dragged);
                          return {
                            ...group,
                            fields: fields.map((item, index) => ({ ...item, order: index })),
                          };
                        });
                      }}
                    >
                      <Stack spacing={2}>
                        <Stack direction={{ xs: 'column', md: 'row' }} spacing={1} alignItems={{ xs: 'stretch', md: 'center' }}>
                          <DragIndicatorIcon color="action" />
                          <TextField
                            fullWidth
                            label="برچسب"
                            value={field.label}
                            onChange={(event) =>
                              updateGroup(active.id, (group) => ({
                                ...group,
                                fields: group.fields.map((item) =>
                                  item.id === field.id ? { ...item, label: event.target.value } : item,
                                ),
                              }))
                            }
                          />
                          <TextField
                            fullWidth
                            label="apiName"
                            value={field.apiName}
                            onChange={(event) =>
                              updateGroup(active.id, (group) => ({
                                ...group,
                                fields: group.fields.map((item) =>
                                  item.id === field.id ? { ...item, apiName: event.target.value } : item,
                                ),
                              }))
                            }
                          />
                          <FormControl sx={{ minWidth: { md: 160 }, width: { xs: '100%', md: 'auto' } }}>
                            <InputLabel>نوع</InputLabel>
                            <Select
                              label="نوع"
                              value={field.type}
                              onChange={(event) => {
                                const nextType = event.target.value as DynamicFieldDefinition['type'];
                                const allowedFilterTypes = filterTypesForField(nextType);
                                updateGroup(active.id, (group) => ({
                                  ...group,
                                  fields: group.fields.map((item) =>
                                    item.id === field.id
                                      ? {
                                          ...item,
                                          type: nextType,
                                          filterType: allowedFilterTypes.includes(item.filterType)
                                            ? item.filterType
                                            : allowedFilterTypes[0],
                                        }
                                      : item,
                                  ),
                                }));
                              }}
                            >
                              {fieldTypes.map((type) => (
                                <MenuItem key={type} value={type}>
                                  {type}
                                </MenuItem>
                              ))}
                            </Select>
                          </FormControl>
                          <IconButton
                            color="error"
                            onClick={() =>
                              updateGroup(active.id, (group) => ({
                                ...group,
                                fields: group.fields.filter((item) => item.id !== field.id),
                              }))
                            }
                          >
                            <DeleteOutlineIcon />
                          </IconButton>
                        </Stack>

                        <Stack direction={{ xs: 'column', md: 'row' }} spacing={2} alignItems={{ xs: 'stretch', md: 'center' }} flexWrap="wrap">
                          <FormControlLabel
                            control={
                              <Switch
                                checked={field.required}
                                onChange={(event) =>
                                  updateGroup(active.id, (group) => ({
                                    ...group,
                                    fields: group.fields.map((item) =>
                                      item.id === field.id ? { ...item, required: event.target.checked } : item,
                                    ),
                                  }))
                                }
                              />
                            }
                            label="الزامی"
                          />
                          <FormControlLabel
                            control={
                              <Switch
                                checked={field.showAsFilter}
                                onChange={(event) =>
                                  updateGroup(active.id, (group) => ({
                                    ...group,
                                    fields: group.fields.map((item) =>
                                      item.id === field.id ? { ...item, showAsFilter: event.target.checked } : item,
                                    ),
                                  }))
                                }
                              />
                            }
                            label="نمایش به عنوان فیلتر"
                          />
                          <FormControl sx={{ minWidth: { md: 220 }, width: { xs: '100%', md: 'auto' } }}>
                            <InputLabel>نوع فیلتر</InputLabel>
                            <Select
                              label="نوع فیلتر"
                              value={field.filterType}
                              onChange={(event) =>
                                updateGroup(active.id, (group) => ({
                                  ...group,
                                  fields: group.fields.map((item) =>
                                    item.id === field.id
                                      ? {
                                          ...item,
                                          filterType: event.target.value as DynamicFieldDefinition['filterType'],
                                        }
                                      : item,
                                  ),
                                }))
                              }
                            >
                              {filterTypeOptions
                                .filter((option) => filterTypesForField(field.type).includes(option.value))
                                .map((option) => (
                                  <MenuItem key={option.value} value={option.value}>
                                    {option.label}
                                  </MenuItem>
                                ))}
                            </Select>
                          </FormControl>
                          <TextField
                            label="Regex"
                            value={field.rules.regex ?? ''}
                            onChange={(event) =>
                              updateGroup(active.id, (group) => ({
                                ...group,
                                fields: group.fields.map((item) =>
                                  item.id === field.id
                                    ? { ...item, rules: { ...item.rules, regex: event.target.value } }
                                    : item,
                                ),
                              }))
                            }
                          />
                          <TextField
                            label="پیام خطا"
                            value={field.rules.errorMessage ?? ''}
                            onChange={(event) =>
                              updateGroup(active.id, (group) => ({
                                ...group,
                                fields: group.fields.map((item) =>
                                  item.id === field.id
                                    ? { ...item, rules: { ...item.rules, errorMessage: event.target.value } }
                                    : item,
                                ),
                              }))
                            }
                          />
                          {(field.type === 'int' || field.type === 'decimal') && (
                            <>
                              <TextField
                                type="number"
                                label="حداقل"
                                value={field.rules.min ?? ''}
                                onChange={(event) =>
                                  updateGroup(active.id, (group) => ({
                                    ...group,
                                    fields: group.fields.map((item) =>
                                      item.id === field.id
                                        ? {
                                            ...item,
                                            rules: {
                                              ...item.rules,
                                              min: event.target.value === '' ? undefined : Number(event.target.value),
                                            },
                                          }
                                        : item,
                                    ),
                                  }))
                                }
                              />
                              <TextField
                                type="number"
                                label="حداکثر"
                                value={field.rules.max ?? ''}
                                onChange={(event) =>
                                  updateGroup(active.id, (group) => ({
                                    ...group,
                                    fields: group.fields.map((item) =>
                                      item.id === field.id
                                        ? {
                                            ...item,
                                            rules: {
                                              ...item.rules,
                                              max: event.target.value === '' ? undefined : Number(event.target.value),
                                            },
                                          }
                                        : item,
                                    ),
                                  }))
                                }
                              />
                            </>
                          )}
                        </Stack>

                        {(field.type === 'dropdown' || field.type === 'multiselect') && (
                          <Stack spacing={1}>
                            <Typography variant="subtitle2">گزینه‌ها</Typography>
                            {field.options.map((option) => (
                              <Stack key={option.id} direction={{ xs: 'column', sm: 'row' }} spacing={1} alignItems={{ xs: 'stretch', sm: 'center' }}>
                                <TextField
                                  label="مقدار"
                                  value={option.value}
                                  onChange={(event) =>
                                    updateGroup(active.id, (group) => ({
                                      ...group,
                                      fields: group.fields.map((item) =>
                                        item.id === field.id
                                          ? {
                                              ...item,
                                              options: item.options.map((itemOption) =>
                                                itemOption.id === option.id
                                                  ? { ...itemOption, value: event.target.value }
                                                  : itemOption,
                                              ),
                                            }
                                          : item,
                                      ),
                                    }))
                                  }
                                />
                                <TextField
                                  label="برچسب"
                                  value={option.label}
                                  onChange={(event) =>
                                    updateGroup(active.id, (group) => ({
                                      ...group,
                                      fields: group.fields.map((item) =>
                                        item.id === field.id
                                          ? {
                                              ...item,
                                              options: item.options.map((itemOption) =>
                                                itemOption.id === option.id
                                                  ? { ...itemOption, label: event.target.value }
                                                  : itemOption,
                                              ),
                                            }
                                          : item,
                                      ),
                                    }))
                                  }
                                />
                                <IconButton
                                  color="error"
                                  onClick={() =>
                                    updateGroup(active.id, (group) => ({
                                      ...group,
                                      fields: group.fields.map((item) =>
                                        item.id === field.id
                                          ? {
                                              ...item,
                                              options: item.options.filter((itemOption) => itemOption.id !== option.id),
                                            }
                                          : item,
                                      ),
                                    }))
                                  }
                                >
                                  <DeleteOutlineIcon />
                                </IconButton>
                              </Stack>
                            ))}
                            <Button
                              variant="text"
                              startIcon={<AddIcon />}
                              onClick={() =>
                                updateGroup(active.id, (group) => ({
                                  ...group,
                                  fields: group.fields.map((item) =>
                                    item.id === field.id
                                      ? {
                                          ...item,
                                          options: [
                                            ...item.options,
                                            {
                                              id: createClientId(),
                                              label: 'گزینه جدید',
                                              value: `option_${item.options.length + 1}`,
                                            },
                                          ],
                                        }
                                      : item,
                                  ),
                                }))
                              }
                            >
                              افزودن گزینه
                            </Button>
                          </Stack>
                        )}
                      </Stack>
                    </Box>
                  ))
              )}
            </Stack>
          )}
        </>
      )}
    </Stack>
  );
};
