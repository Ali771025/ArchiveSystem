import { useMemo, useState } from 'react';
import { JalaliDateInput } from '@/components/common/JalaliDateInputs';
import { dynamicFieldsApi } from '@/services/dynamicFieldsApi';
import {
  CaseSearchResult,
  DynamicFieldDefinition,
  DynamicFieldFilterType,
  DynamicFieldType,
  DynamicFilter,
  DynamicTemplate,
} from '@/types/dynamicFields';
import { toIsoUtcString } from '@/utils/jalaliDate';

interface DynamicFieldSearchUIProps {
  initialClassificationId?: number;
}

interface FilterValueState {
  value: string;
  fromValue: string;
  toValue: string;
  selectedIds: string[];
}

const defaultFilterState: FilterValueState = {
  value: '',
  fromValue: '',
  toValue: '',
  selectedIds: [],
};

const mapFilterTypeToOp = (filterType: DynamicFieldFilterType): DynamicFilter['op'] => {
  switch (filterType) {
    case DynamicFieldFilterType.Contains:
      return 'contains';
    case DynamicFieldFilterType.Equals:
      return 'equals';
    case DynamicFieldFilterType.NotEquals:
      return 'notEquals';
    case DynamicFieldFilterType.LessThan:
      return 'lessThan';
    case DynamicFieldFilterType.GreaterThan:
      return 'greaterThan';
    case DynamicFieldFilterType.LessThanOrEqual:
      return 'lessThanOrEqual';
    case DynamicFieldFilterType.GreaterThanOrEqual:
      return 'greaterThanOrEqual';
    case DynamicFieldFilterType.Range:
      return 'range';
    default:
      return 'contains';
  }
};

const isDateType = (field: DynamicFieldDefinition): boolean => {
  return field.fieldType === DynamicFieldType.Date;
};

const isTextType = (field: DynamicFieldDefinition): boolean => {
  return field.fieldType === DynamicFieldType.String || field.fieldType === DynamicFieldType.MultiLineText;
};

const isNumberType = (field: DynamicFieldDefinition): boolean => {
  return field.fieldType === DynamicFieldType.Number || field.fieldType === DynamicFieldType.Decimal;
};

const isSelectType = (field: DynamicFieldDefinition): boolean => {
  return field.fieldType === DynamicFieldType.Enum || field.fieldType === DynamicFieldType.MultiSelect;
};

const isBooleanType = (field: DynamicFieldDefinition): boolean => {
  return field.fieldType === DynamicFieldType.Bool || field.fieldType === DynamicFieldType.CheckBox;
};

const normalizeDateValue = (value: string): string => {
  return toIsoUtcString(value) ?? '';
};

const buildFilterPayload = (field: DynamicFieldDefinition, state: FilterValueState): DynamicFilter | null => {
  const op = mapFilterTypeToOp(field.filterType);

  if (op === 'range') {
    if (!state.fromValue.trim() || !state.toValue.trim()) {
      return null;
    }

    if (isDateType(field)) {
      return {
        fieldId: field.id,
        op,
        fromValue: normalizeDateValue(state.fromValue),
        toValue: normalizeDateValue(state.toValue),
      };
    }

    return {
      fieldId: field.id,
      op,
      fromValue: Number(state.fromValue),
      toValue: Number(state.toValue),
    };
  }

  if (isSelectType(field) && field.isMultiSelect) {
    if (state.selectedIds.length === 0) {
      return null;
    }

    return {
      fieldId: field.id,
      op: 'in',
      values: state.selectedIds,
    };
  }

  if (!state.value.trim()) {
    return null;
  }

  if (isDateType(field)) {
    return {
      fieldId: field.id,
      op,
      value: normalizeDateValue(state.value),
    };
  }

  if (isNumberType(field)) {
    return {
      fieldId: field.id,
      op,
      value: Number(state.value),
    };
  }

  if (isBooleanType(field)) {
    return {
      fieldId: field.id,
      op,
      value: state.value === 'true',
    };
  }

  return {
    fieldId: field.id,
    op,
    value: state.value,
  };
};

export const DynamicFieldSearchUI = ({ initialClassificationId = 1 }: DynamicFieldSearchUIProps) => {
  const [classificationId, setClassificationId] = useState<number>(initialClassificationId);
  const [template, setTemplate] = useState<DynamicTemplate | null>(null);
  const [filtersState, setFiltersState] = useState<Record<string, FilterValueState>>({});
  const [result, setResult] = useState<CaseSearchResult | null>(null);
  const [loadingTemplate, setLoadingTemplate] = useState(false);
  const [searching, setSearching] = useState(false);
  const [error, setError] = useState('');

  const filterableFields = useMemo(() => {
    if (!template) {
      return [];
    }

    return template.groups
      .flatMap((group) => group.fields)
      .filter((field) => field.isActive && field.showAsFilter)
      .sort((a, b) => a.order - b.order);
  }, [template]);

  const setFieldState = (fieldId: string, updater: (current: FilterValueState) => FilterValueState) => {
    setFiltersState((current) => ({
      ...current,
      [fieldId]: updater(current[fieldId] ?? defaultFilterState),
    }));
  };

  const loadTemplate = async () => {
    setLoadingTemplate(true);
    setError('');
    try {
      const loaded = await dynamicFieldsApi.getTemplate(classificationId);
      setTemplate(loaded);
      setResult(null);
    } catch (loadError) {
      console.error(loadError);
      setError('بارگذاری قالب فیلد پویا ناموفق بود.');
    } finally {
      setLoadingTemplate(false);
    }
  };

  const runSearch = async () => {
    setSearching(true);
    setError('');
    try {
      const dynamicFilters = filterableFields
        .map((field) => buildFilterPayload(field, filtersState[field.id] ?? defaultFilterState))
        .filter((item): item is DynamicFilter => Boolean(item));

      const searchResult = await dynamicFieldsApi.searchCases({
        classificationId,
        page: 1,
        pageSize: 50,
        dynamicFilters,
      });

      setResult(searchResult);
    } catch (searchError) {
      console.error(searchError);
      setError('جستجوی پیشرفته ناموفق بود.');
    } finally {
      setSearching(false);
    }
  };

  return (
    <section className="rounded border p-4 space-y-4">
      <h2 className="text-xl font-semibold">Advanced Dynamic Search</h2>

      <div className="flex flex-wrap gap-3 items-end">
        <label className="flex flex-col text-sm">
          Classification ID
          <input
            type="number"
            className="border rounded px-2 py-1"
            value={classificationId}
            onChange={(event) => setClassificationId(Number(event.target.value) || 0)}
          />
        </label>
        <button className="px-3 py-2 border rounded" onClick={loadTemplate} disabled={loadingTemplate}>
          {loadingTemplate ? 'Loading...' : 'Load Filters'}
        </button>
      </div>

      {filterableFields.length === 0 && template && (
        <p className="text-sm text-gray-600">هیچ فیلد فعالی برای فیلتر تعریف نشده است.</p>
      )}

      {filterableFields.map((field) => {
        const state = filtersState[field.id] ?? defaultFilterState;
        const isRange = field.filterType === DynamicFieldFilterType.Range;

        return (
          <div key={field.id} className="rounded border p-3 space-y-2">
            <p className="font-medium">{field.label}</p>

            {isRange ? (
              <div className="grid gap-2 md:grid-cols-2">
                <div className="space-y-1">
                  <p className="text-xs text-gray-600">{isDateType(field) ? 'از تاریخ' : 'از'}</p>
                  {isDateType(field) ? (
                    <JalaliDateInput
                      value={state.fromValue}
                      inputClassName="border rounded px-2 py-1 w-full text-sm"
                      onChange={(value) => setFieldState(field.id, (current) => ({ ...current, fromValue: value }))}
                    />
                  ) : (
                    <input
                      type="number"
                      className="border rounded px-2 py-1 w-full"
                      value={state.fromValue}
                      onChange={(event) =>
                        setFieldState(field.id, (current) => ({ ...current, fromValue: event.target.value }))
                      }
                    />
                  )}
                </div>
                <div className="space-y-1">
                  <p className="text-xs text-gray-600">{isDateType(field) ? 'تا تاریخ' : 'تا'}</p>
                  {isDateType(field) ? (
                    <JalaliDateInput
                      value={state.toValue}
                      inputClassName="border rounded px-2 py-1 w-full text-sm"
                      onChange={(value) => setFieldState(field.id, (current) => ({ ...current, toValue: value }))}
                    />
                  ) : (
                    <input
                      type="number"
                      className="border rounded px-2 py-1 w-full"
                      value={state.toValue}
                      onChange={(event) =>
                        setFieldState(field.id, (current) => ({ ...current, toValue: event.target.value }))
                      }
                    />
                  )}
                </div>
              </div>
            ) : null}

            {!isRange && isTextType(field) && (
              <input
                className="border rounded px-2 py-1 w-full"
                value={state.value}
                onChange={(event) => setFieldState(field.id, (current) => ({ ...current, value: event.target.value }))}
              />
            )}

            {!isRange && isNumberType(field) && (
              <input
                type="number"
                className="border rounded px-2 py-1 w-full"
                value={state.value}
                onChange={(event) => setFieldState(field.id, (current) => ({ ...current, value: event.target.value }))}
              />
            )}

            {!isRange && isDateType(field) && (
              <JalaliDateInput
                value={state.value}
                inputClassName="border rounded px-2 py-1 w-full text-sm"
                onChange={(value) => setFieldState(field.id, (current) => ({ ...current, value }))}
              />
            )}

            {!isRange && isBooleanType(field) && (
              <select
                className="border rounded px-2 py-1 w-full"
                value={state.value}
                onChange={(event) => setFieldState(field.id, (current) => ({ ...current, value: event.target.value }))}
              >
                <option value="">انتخاب کنید</option>
                <option value="true">بله</option>
                <option value="false">خیر</option>
              </select>
            )}

            {!isRange && isSelectType(field) && !field.isMultiSelect && (
              <select
                className="border rounded px-2 py-1 w-full"
                value={state.value}
                onChange={(event) => setFieldState(field.id, (current) => ({ ...current, value: event.target.value }))}
              >
                <option value="">انتخاب کنید</option>
                {field.options.map((option) => (
                  <option key={option.id} value={option.id}>
                    {option.label}
                  </option>
                ))}
              </select>
            )}

            {!isRange && isSelectType(field) && field.isMultiSelect && (
              <select
                className="border rounded px-2 py-1 w-full h-28"
                multiple
                value={state.selectedIds}
                onChange={(event) =>
                  setFieldState(field.id, (current) => ({
                    ...current,
                    selectedIds: Array.from(event.target.selectedOptions).map((option) => option.value),
                  }))
                }
              >
                {field.options.map((option) => (
                  <option key={option.id} value={option.id}>
                    {option.label}
                  </option>
                ))}
              </select>
            )}
          </div>
        );
      })}

      <div className="flex gap-2">
        <button className="px-3 py-2 border rounded" onClick={runSearch} disabled={searching || loadingTemplate}>
          {searching ? 'Searching...' : 'Search Cases'}
        </button>
      </div>

      {error && <p className="text-red-700 text-sm">{error}</p>}

      {result && (
        <div className="space-y-2">
          <p className="text-sm text-gray-700">
            Results: {result.totalCount} (showing {result.items.length})
          </p>
          <div className="overflow-x-auto">
            <table className="min-w-full border text-sm">
              <thead>
                <tr>
                  <th className="border px-2 py-1 text-right">ID</th>
                  <th className="border px-2 py-1 text-right">Case Number</th>
                  <th className="border px-2 py-1 text-right">Title</th>
                  <th className="border px-2 py-1 text-right">Classification</th>
                </tr>
              </thead>
              <tbody>
                {result.items.map((item) => (
                  <tr key={item.id}>
                    <td className="border px-2 py-1">{item.id}</td>
                    <td className="border px-2 py-1">{item.caseNumber}</td>
                    <td className="border px-2 py-1">{item.title}</td>
                    <td className="border px-2 py-1">{item.classificationId}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </section>
  );
};

export default DynamicFieldSearchUI;

