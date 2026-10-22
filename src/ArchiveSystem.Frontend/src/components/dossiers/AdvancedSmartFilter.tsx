import AddIcon from '@mui/icons-material/Add';
import DeleteOutlineIcon from '@mui/icons-material/DeleteOutline';
import FilterAltIcon from '@mui/icons-material/FilterAlt';
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Divider,
  MenuItem,
  Select,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { useMemo, useState } from 'react';
import { JalaliDateInput, JalaliDateTimeInput } from '@/components/common/JalaliDateInputs';
import { CategoryRadioTreeView, type Category } from '@/components/dossiers/CategoryRadioTreeView';
import type {
  DossierComparisonOperator,
  DossierConditionGroup,
  DossierConditionRule,
  DossierLogicalOperator,
  DossierSearchFieldDescriptor,
} from '@/types/dossierSearch';

interface AdvancedSmartFilterProps {
  fields: DossierSearchFieldDescriptor[];
  categories?: Category[];
  categoryValue?: string | null;
  loading?: boolean;
  onCategoryChange?: (id: string) => void;
  onApply: (root: DossierConditionGroup) => void;
}

interface RuleDraft {
  id: string;
  fieldKey: string;
  operator: DossierComparisonOperator;
  value: string;
  secondValue: string;
  valuesText: string;
  selectedValues: string[];
}

interface GroupDraft {
  id: string;
  operator: DossierLogicalOperator;
  rules: RuleDraft[];
  groups: GroupDraft[];
}

const noValueOperators = new Set<DossierComparisonOperator>(['IsNull', 'IsNotNull', 'IsEmpty', 'IsNotEmpty']);
const listOperators = new Set<DossierComparisonOperator>(['In', 'NotIn']);

const operatorLabels: Record<DossierComparisonOperator, string> = {
  Equals: 'مساوی',
  NotEquals: 'نامساوی',
  Contains: 'شامل',
  StartsWith: 'شروع با',
  EndsWith: 'پایان با',
  GreaterThan: 'بزرگ تر از',
  GreaterThanOrEqual: 'بزرگ تر یا مساوی',
  LessThan: 'کوچک تر از',
  LessThanOrEqual: 'کوچک تر یا مساوی',
  Between: 'بین',
  In: 'در میان',
  NotIn: 'خارج از',
  IsNull: 'تهی',
  IsNotNull: 'غیر تهی',
  IsEmpty: 'خالی',
  IsNotEmpty: 'غیر خالی',
};

const sourceLabels = {
  Dossier: 'پرونده',
  Owner: 'مالکین',
  DynamicField: 'فیلد متادیتا',
} as const;

const fixedFieldLabels: Record<string, string> = {
  'Dossier.DossierNumber': 'شماره پرونده',
  'Dossier.ExternalCaseNumber': 'شماره پرونده مرجع',
  'Dossier.Address': 'نشانی',
  'Dossier.DefaultImagePath': 'مسیر تصویر پیش‌فرض',
  'Dossier.RenovationCode': 'کد نوسازی',
  'Dossier.PostalCode': 'کد پستی',
  'Dossier.RegistrationPlate': 'پلاک ثبتی',
  'Dossier.UsageType': 'نوع کاربری',
  'Dossier.Latitude': 'عرض جغرافیایی',
  'Dossier.Longitude': 'طول جغرافیایی',
  'Dossier.ClassificationId': 'دسته‌بندی',
  'Owner.FullName': 'نام کامل مالک',
  'Owner.NationalCode': 'کد ملی مالک',
  'Owner.NationalId': 'شناسه ملی مالک',
  'Owner.Mobile': 'شماره همراه مالک',
  'Owner.HomePhone': 'تلفن ثابت مالک',
  'Owner.OwnerType': 'نوع مالک',
};

const getFieldDisplayTitle = (field: DossierSearchFieldDescriptor): string => {
  return fixedFieldLabels[`${field.source}.${field.field}`] ?? field.title;
};

const createId = (): string => {
  if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
    return crypto.randomUUID();
  }

  return `${Date.now()}-${Math.random().toString(36).slice(2, 10)}`;
};

const createRule = (): RuleDraft => ({
  id: createId(),
  fieldKey: '',
  operator: 'Equals',
  value: '',
  secondValue: '',
  valuesText: '',
  selectedValues: [],
});

const createGroup = (): GroupDraft => ({
  id: createId(),
  operator: 'And',
  rules: [createRule()],
  groups: [],
});

export const AdvancedSmartFilter = ({
  fields,
  categories = [],
  categoryValue,
  loading = false,
  onCategoryChange,
  onApply,
}: AdvancedSmartFilterProps) => {
  const [root, setRoot] = useState<GroupDraft>(createGroup);
  const [error, setError] = useState('');
  const [localCategoryValue, setLocalCategoryValue] = useState<string | null>(null);

  const selectedCategoryId = categoryValue ?? localCategoryValue;

  const fieldLookup = useMemo(
    () => new Map(fields.map((field) => [field.key, field])),
    [fields],
  );

  const fieldsBySource = useMemo(
    () => ({
      Dossier: fields.filter((field) => field.source === 'Dossier'),
      Owner: fields.filter((field) => field.source === 'Owner'),
      DynamicField: fields.filter((field) => field.source === 'DynamicField'),
    }),
    [fields],
  );

  const updateGroup = (groupId: string, updater: (group: GroupDraft) => GroupDraft) => {
    setRoot((current) => patchGroup(current, groupId, updater));
  };

  const applyFilter = () => {
    const dtoRoot = mapGroupToDto(root, fieldLookup);
    if (!dtoRoot) {
      setError('حداقل یک شرط معتبر برای جستجو وارد کنید.');
      return;
    }

    setError('');
    onApply(dtoRoot);
  };

  const renderRuleValueInput = (
    rule: RuleDraft,
    field: DossierSearchFieldDescriptor,
    onRuleChange: (updater: (rule: RuleDraft) => RuleDraft) => void,
  ) => {
    if (noValueOperators.has(rule.operator)) {
      return null;
    }

    if (rule.operator === 'Between') {
      return (
        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1}>
          {renderSingleValueInput(rule, field, 'value', onRuleChange)}
          {renderSingleValueInput(rule, field, 'secondValue', onRuleChange)}
        </Stack>
      );
    }

    if (listOperators.has(rule.operator)) {
      if (field.options.length > 0) {
        return (
          <Select
            fullWidth
            multiple
            value={rule.selectedValues}
            size="small"
            onChange={(event) => {
              const selected = event.target.value;
              onRuleChange((current) => ({
                ...current,
                selectedValues: typeof selected === 'string' ? selected.split(',') : selected,
              }));
            }}
          >
            {field.options.map((option) => (
              <MenuItem key={option.value} value={option.value}>
                {option.label}
              </MenuItem>
            ))}
          </Select>
        );
      }

      return (
        <TextField
          fullWidth
          size="small"
          placeholder="مقادیر را با , جدا کنید"
          value={rule.valuesText}
          onChange={(event) => onRuleChange((current) => ({ ...current, valuesText: event.target.value }))}
        />
      );
    }

    return renderSingleValueInput(rule, field, 'value', onRuleChange);
  };

  const renderGroup = (group: GroupDraft, depth: number, isRoot: boolean) => {
    return (
      <Card key={group.id} variant="outlined" sx={{ borderStyle: isRoot ? 'solid' : 'dashed' }}>
        <CardContent>
          <Stack spacing={1.25}>
            <Stack
              direction={{ xs: 'column', md: 'row' }}
              spacing={1}
              justifyContent="space-between"
              alignItems={{ xs: 'stretch', md: 'center' }}
            >
              <Stack direction="row" spacing={1} alignItems="center">
                <Typography variant="subtitle2">
                  {isRoot ? 'گروه اصلی' : `گروه ${depth}`}
                </Typography>
                <Select
                  size="small"
                  value={group.operator}
                  onChange={(event) =>
                    updateGroup(group.id, (current) => ({ ...current, operator: event.target.value as DossierLogicalOperator }))
                  }
                >
                  <MenuItem value="And">و</MenuItem>
                  <MenuItem value="Or">یا</MenuItem>
                </Select>
              </Stack>

              <Stack direction="row" spacing={1}>
                <Button
                  size="small"
                  variant="outlined"
                  startIcon={<AddIcon />}
                  onClick={() =>
                    updateGroup(group.id, (current) => ({
                      ...current,
                      rules: [...current.rules, createRule()],
                    }))
                  }
                >
                  شرط
                </Button>
                <Button
                  size="small"
                  variant="outlined"
                  startIcon={<AddIcon />}
                  onClick={() =>
                    updateGroup(group.id, (current) => ({
                      ...current,
                      groups: [...current.groups, createGroup()],
                    }))
                  }
                >
                  زیرگروه
                </Button>
                {!isRoot && (
                  <Button
                    size="small"
                    color="error"
                    variant="outlined"
                    startIcon={<DeleteOutlineIcon />}
                    onClick={() => {
                      setRoot((current) => removeGroup(current, group.id));
                    }}
                  >
                    حذف
                  </Button>
                )}
              </Stack>
            </Stack>

            <Divider />

            <Stack spacing={1}>
              {group.rules.map((rule) => {
                const field = fieldLookup.get(rule.fieldKey);
                const operators = field?.supportedOperators ?? ['Equals'];

                return (
                  <Stack key={rule.id} spacing={1}>
                    <Stack direction={{ xs: 'column', lg: 'row' }} spacing={1}>
                      <Select
                        fullWidth
                        size="small"
                        value={rule.fieldKey}
                        displayEmpty
                        onChange={(event) =>
                          updateGroup(group.id, (current) => ({
                            ...current,
                            rules: current.rules.map((candidate) => {
                              if (candidate.id !== rule.id) {
                                return candidate;
                              }

                              const nextField = fieldLookup.get(event.target.value);
                              return {
                                ...candidate,
                                fieldKey: event.target.value,
                                operator: nextField?.supportedOperators[0] ?? 'Equals',
                                value: '',
                                secondValue: '',
                                valuesText: '',
                                selectedValues: [],
                              };
                            }),
                          }))
                        }
                      >
                        <MenuItem value="">
                          <em>انتخاب فیلد...</em>
                        </MenuItem>
                        {(['Dossier', 'Owner', 'DynamicField'] as const).map((source) =>
                          fieldsBySource[source].map((candidate) => (
                            <MenuItem key={candidate.key} value={candidate.key}>
                              [{sourceLabels[source]}] {getFieldDisplayTitle(candidate)}
                            </MenuItem>
                          )),
                        )}
                      </Select>

                      <Select
                        size="small"
                        value={rule.operator}
                        onChange={(event) =>
                          updateGroup(group.id, (current) => ({
                            ...current,
                            rules: current.rules.map((candidate) =>
                              candidate.id === rule.id
                                ? {
                                    ...candidate,
                                    operator: event.target.value as DossierComparisonOperator,
                                    value: '',
                                    secondValue: '',
                                    valuesText: '',
                                    selectedValues: [],
                                  }
                                : candidate,
                            ),
                          }))
                        }
                        sx={{ minWidth: 220 }}
                      >
                        {operators.map((operator) => (
                          <MenuItem key={operator} value={operator}>
                            {operatorLabels[operator]}
                          </MenuItem>
                        ))}
                      </Select>

                      <Button
                        color="error"
                        variant="outlined"
                        startIcon={<DeleteOutlineIcon />}
                        onClick={() =>
                          updateGroup(group.id, (current) => ({
                            ...current,
                            rules: current.rules.filter((candidate) => candidate.id !== rule.id),
                          }))
                        }
                      >
                        حذف
                      </Button>
                    </Stack>

                    {field && (
                      <Box>
                        {renderRuleValueInput(rule, field, (updater) =>
                          updateGroup(group.id, (current) => ({
                            ...current,
                            rules: current.rules.map((candidate) =>
                              candidate.id === rule.id ? updater(candidate) : candidate,
                            ),
                          })),
                        )}
                      </Box>
                    )}
                  </Stack>
                );
              })}
            </Stack>

            {group.groups.length > 0 && (
              <Stack spacing={1.25}>
                {group.groups.map((child) => renderGroup(child, depth + 1, false))}
              </Stack>
            )}
          </Stack>
        </CardContent>
      </Card>
    );
  };

  return (
    <Stack
      spacing={1.25}
      sx={{
        direction: 'rtl',
        textAlign: 'right',
      }}
    >
      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1} justifyContent="space-between" alignItems={{ sm: 'center' }}>
        <Typography variant="h6">فیلتر هوشمند پیشرفته</Typography>
        <Button
          variant="contained"
          startIcon={<FilterAltIcon />}
          disabled={loading || fields.length === 0}
          onClick={applyFilter}
        >
          اعمال فیلتر
        </Button>
      </Stack>

      {error && <Alert severity="warning">{error}</Alert>}
      {fields.length === 0 && <Alert severity="info">فیلد قابل جستجو در دسترس نیست.</Alert>}

      {categories.length > 0 && (
        <CategoryRadioTreeView
          categories={categories}
          value={selectedCategoryId}
          onChange={(id) => {
            setLocalCategoryValue(id);
            onCategoryChange?.(id);
          }}
        />
      )}

      {renderGroup(root, 1, true)}
    </Stack>
  );
};

const patchGroup = (group: GroupDraft, groupId: string, updater: (group: GroupDraft) => GroupDraft): GroupDraft => {
  if (group.id === groupId) {
    return updater(group);
  }

  return {
    ...group,
    groups: group.groups.map((child) => patchGroup(child, groupId, updater)),
  };
};

const removeGroup = (group: GroupDraft, groupId: string): GroupDraft => {
  return {
    ...group,
    groups: group.groups
      .filter((child) => child.id !== groupId)
      .map((child) => removeGroup(child, groupId)),
  };
};

const mapGroupToDto = (
  group: GroupDraft,
  fields: Map<string, DossierSearchFieldDescriptor>,
): DossierConditionGroup | null => {
  const mappedRules = group.rules
    .map((rule) => mapRuleToDto(rule, fields.get(rule.fieldKey)))
    .filter((rule): rule is DossierConditionRule => Boolean(rule));

  const mappedGroups = group.groups
    .map((child) => mapGroupToDto(child, fields))
    .filter((child): child is DossierConditionGroup => Boolean(child));

  if (mappedRules.length === 0 && mappedGroups.length === 0) {
    return null;
  }

  return {
    id: group.id,
    operator: group.operator,
    rules: mappedRules,
    groups: mappedGroups,
  };
};

const mapRuleToDto = (rule: RuleDraft, field?: DossierSearchFieldDescriptor): DossierConditionRule | null => {
  if (!field) {
    return null;
  }

  const base: DossierConditionRule = {
    id: rule.id,
    source: field.source,
    field: field.field,
    operator: rule.operator,
    valueType: field.valueType,
    dynamicFieldApiName: field.dynamicFieldApiName,
    dynamicFieldType: field.dynamicFieldType,
  };

  if (noValueOperators.has(rule.operator)) {
    return base;
  }

  if (rule.operator === 'Between') {
    return {
      ...base,
      values: [
        parseValue(rule.value, field),
        parseValue(rule.secondValue, field),
      ],
    };
  }

  if (listOperators.has(rule.operator)) {
    const values = field.options.length > 0
      ? rule.selectedValues.map((value) => parseValue(value, field))
      : rule.valuesText
          .split(',')
          .map((value) => value.trim())
          .filter((value) => value.length > 0)
          .map((value) => parseValue(value, field));

    return {
      ...base,
      values,
    };
  }

  return {
    ...base,
    value: parseValue(rule.value, field),
  };
};

const renderSingleValueInput = (
  rule: RuleDraft,
  field: DossierSearchFieldDescriptor,
  key: 'value' | 'secondValue',
  onRuleChange: (updater: (rule: RuleDraft) => RuleDraft) => void,
) => {
  if (field.options.length > 0) {
    return (
      <Select
        fullWidth
        size="small"
        value={rule[key]}
        onChange={(event) => onRuleChange((current) => ({ ...current, [key]: event.target.value }))}
      >
        <MenuItem value="">
          <em>انتخاب...</em>
        </MenuItem>
        {field.options.map((option) => (
          <MenuItem key={option.value} value={option.value}>
            {option.label}
          </MenuItem>
        ))}
      </Select>
    );
  }

  if (field.valueType === 'Boolean') {
    return (
      <Select
        fullWidth
        size="small"
        value={rule[key]}
        onChange={(event) => onRuleChange((current) => ({ ...current, [key]: event.target.value }))}
      >
        <MenuItem value="true">بله</MenuItem>
        <MenuItem value="false">خیر</MenuItem>
      </Select>
    );
  }

  if (field.valueType === 'Date') {
    return (
      <JalaliDateInput
        value={rule[key]}
        onChange={(value) => onRuleChange((current) => ({ ...current, [key]: value }))}
      />
    );
  }

  if (field.valueType === 'DateTime') {
    return (
      <JalaliDateTimeInput
        value={rule[key]}
        onChange={(value) => onRuleChange((current) => ({ ...current, [key]: value }))}
      />
    );
  }

  if (field.valueType === 'Time') {
    return (
      <TextField
        fullWidth
        size="small"
        type="time"
        value={rule[key]}
        onChange={(event) => onRuleChange((current) => ({ ...current, [key]: event.target.value }))}
        InputLabelProps={{ shrink: true }}
      />
    );
  }

  if (field.valueType === 'Integer' || field.valueType === 'Decimal') {
    return (
      <TextField
        fullWidth
        size="small"
        type="number"
        value={rule[key]}
        onChange={(event) => onRuleChange((current) => ({ ...current, [key]: event.target.value }))}
      />
    );
  }

  return (
    <TextField
      fullWidth
      size="small"
      multiline={field.dynamicFieldType === 'MultilineText'}
      minRows={field.dynamicFieldType === 'MultilineText' ? 2 : 1}
      value={rule[key]}
      onChange={(event) => onRuleChange((current) => ({ ...current, [key]: event.target.value }))}
    />
  );
};

const parseValue = (rawValue: string, field: DossierSearchFieldDescriptor): unknown => {
  if (rawValue === '') {
    return null;
  }

  switch (field.valueType) {
    case 'Integer': {
      const parsed = Number.parseInt(rawValue, 10);
      return Number.isNaN(parsed) ? null : parsed;
    }
    case 'Decimal': {
      const parsed = Number.parseFloat(rawValue);
      return Number.isNaN(parsed) ? null : parsed;
    }
    case 'Boolean':
      return rawValue === 'true';
    case 'Date':
      return rawValue;
    case 'DateTime': {
      const parsed = new Date(rawValue);
      return Number.isNaN(parsed.getTime()) ? null : parsed.toISOString();
    }
    case 'Time':
      return rawValue;
    default:
      return rawValue;
  }
};

export default AdvancedSmartFilter;
