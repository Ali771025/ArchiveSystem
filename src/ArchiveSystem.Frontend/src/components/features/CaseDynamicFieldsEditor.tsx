import React, { useState } from 'react';
import { JalaliDateInput } from '@/components/common/JalaliDateInputs';
import { dynamicFieldsApi } from '@/services/dynamicFieldsApi';
import { CaseDynamicFieldValue, CaseDynamicFields, DynamicFieldType } from '@/types/dynamicFields';
import { parseDateLikeValue, toGregorianDateString, toIsoUtcString } from '@/utils/jalaliDate';

const toDateInputValue = (value?: string): string => {
  if (!value) {
    return '';
  }

  const parsed = parseDateLikeValue(value);
  return parsed ? toGregorianDateString(parsed) : '';
};

export const CaseDynamicFieldsEditor: React.FC = () => {
  const [caseId, setCaseId] = useState<number>(1);
  const [data, setData] = useState<CaseDynamicFields | null>(null);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');

  const loadFields = async () => {
    setLoading(true);
    setError('');
    setMessage('');
    try {
      const response = await dynamicFieldsApi.getCaseDynamicFields(caseId);
      setData(response);
      setMessage('Case fields loaded.');
    } catch (loadError) {
      console.error(loadError);
      setError('Failed to load case dynamic fields.');
    } finally {
      setLoading(false);
    }
  };

  const saveFields = async () => {
    if (!data) {
      return;
    }

    setSaving(true);
    setError('');
    setMessage('');
    try {
      const response = await dynamicFieldsApi.upsertCaseDynamicFields(caseId, data.values);
      setData(response);
      setMessage('Case dynamic fields saved.');
    } catch (saveError) {
      console.error(saveError);
      setError('Failed to save case dynamic fields.');
    } finally {
      setSaving(false);
    }
  };

  const updateField = (fieldId: string, updater: (value: CaseDynamicFieldValue) => CaseDynamicFieldValue) => {
    setData((current) => {
      if (!current) {
        return current;
      }

      return {
        ...current,
        values: current.values.map((value) => (value.fieldId === fieldId ? updater(value) : value)),
      };
    });
  };

  return (
    <section className="rounded border p-4 space-y-4">
      <h2 className="text-xl font-semibold">Case: Dynamic Fields</h2>

      <div className="flex flex-wrap gap-3 items-end">
        <label className="flex flex-col text-sm">
          Case ID
          <input
            type="number"
            className="border rounded px-2 py-1"
            value={caseId}
            onChange={(event) => setCaseId(Number(event.target.value) || 0)}
          />
        </label>
        <button className="px-3 py-2 border rounded" onClick={loadFields} disabled={loading}>
          {loading ? 'Loading...' : 'Load'}
        </button>
        <button className="px-3 py-2 border rounded" onClick={saveFields} disabled={!data || saving}>
          {saving ? 'Saving...' : 'Save'}
        </button>
      </div>

      {data && data.values.length === 0 && (
        <p className="text-sm text-gray-600">No active dynamic template for this case classification.</p>
      )}

      {data?.values.map((field) => (
        <div key={field.fieldId} className="rounded border p-3 space-y-2">
          <div className="font-medium">
            {field.label} <span className="text-xs text-gray-500">({field.key})</span>
            {field.isRequired && <span className="text-red-600 mr-1">*</span>}
          </div>

          {field.fieldType === DynamicFieldType.String && (
            <input
              className="border rounded px-2 py-1 w-full"
              value={field.stringValue ?? ''}
              onChange={(event) =>
                updateField(field.fieldId, (current) => ({
                  ...current,
                  stringValue: event.target.value,
                }))
              }
            />
          )}

          {field.fieldType === DynamicFieldType.Number && (
            <input
              type="number"
              className="border rounded px-2 py-1 w-full"
              value={field.numberValue ?? ''}
              onChange={(event) =>
                updateField(field.fieldId, (current) => ({
                  ...current,
                  numberValue: event.target.value === '' ? undefined : Number(event.target.value),
                }))
              }
            />
          )}

          {field.fieldType === DynamicFieldType.Date && (
            <JalaliDateInput
              value={toDateInputValue(field.dateValue)}
              inputClassName="border rounded px-2 py-1 w-full text-sm"
              onChange={(nextValue) =>
                updateField(field.fieldId, (current) => ({
                  ...current,
                  dateValue: nextValue === '' ? undefined : toIsoUtcString(nextValue),
                }))
              }
            />
          )}

          {field.fieldType === DynamicFieldType.Bool && (
            <label className="flex items-center gap-2 text-sm">
              <input
                type="checkbox"
                checked={field.boolValue ?? false}
                onChange={(event) =>
                  updateField(field.fieldId, (current) => ({
                    ...current,
                    boolValue: event.target.checked,
                  }))
                }
              />
              {field.boolValue ? 'True' : 'False'}
            </label>
          )}

          {field.fieldType === DynamicFieldType.Enum && !field.isMultiSelect && (
            <select
              className="border rounded px-2 py-1 w-full"
              value={field.optionIds[0] ?? ''}
              onChange={(event) =>
                updateField(field.fieldId, (current) => ({
                  ...current,
                  optionIds: event.target.value ? [event.target.value] : [],
                }))
              }
            >
              <option value="">-- Select --</option>
              {field.options.map((option) => (
                <option key={option.id} value={option.id}>
                  {option.label}
                </option>
              ))}
            </select>
          )}

          {field.fieldType === DynamicFieldType.Enum && field.isMultiSelect && (
            <div className="space-y-1">
              {field.options.map((option) => {
                const checked = field.optionIds.includes(option.id);
                return (
                  <label key={option.id} className="flex items-center gap-2 text-sm">
                    <input
                      type="checkbox"
                      checked={checked}
                      onChange={(event) =>
                        updateField(field.fieldId, (current) => ({
                          ...current,
                          optionIds: event.target.checked
                            ? [...current.optionIds, option.id]
                            : current.optionIds.filter((id) => id !== option.id),
                        }))
                      }
                    />
                    {option.label}
                  </label>
                );
              })}
            </div>
          )}
        </div>
      ))}

      {message && <p className="text-green-700 text-sm">{message}</p>}
      {error && <p className="text-red-700 text-sm">{error}</p>}
    </section>
  );
};

export default CaseDynamicFieldsEditor;
