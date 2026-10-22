import React, { useMemo, useState } from 'react';
import { dynamicFieldsApi } from '@/services/dynamicFieldsApi';
import {
  DynamicFieldDefinition,
  DynamicFieldFilterType,
  DynamicFieldOption,
  DynamicFieldType,
  DynamicTemplate,
} from '@/types/dynamicFields';
import { createClientId } from '@/utils/helpers';

const newOption = (): DynamicFieldOption => ({
  id: createClientId(),
  value: '',
  label: '',
  order: 0,
  isActive: true,
});

const newField = (): DynamicFieldDefinition => ({
  id: createClientId(),
  key: '',
  label: '',
  fieldType: DynamicFieldType.String,
  isRequired: false,
  isMultiSelect: false,
  showAsFilter: false,
  filterType: DynamicFieldFilterType.Contains,
  order: 0,
  isActive: true,
  options: [],
});

const createEmptyTemplate = (classificationId: number): DynamicTemplate => ({
  id: createClientId(),
  classificationId,
  name: 'Dynamic Template',
  isActive: true,
  groups: [],
});

const fieldTypeOptions = [
  { value: DynamicFieldType.String, label: 'String' },
  { value: DynamicFieldType.Number, label: 'Number' },
  { value: DynamicFieldType.Date, label: 'Date' },
  { value: DynamicFieldType.Bool, label: 'Boolean' },
  { value: DynamicFieldType.Enum, label: 'Enum' },
];

export const DynamicTemplateAdmin: React.FC = () => {
  const [classificationId, setClassificationId] = useState<number>(1);
  const [template, setTemplate] = useState<DynamicTemplate>(createEmptyTemplate(1));
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState<string>('');
  const [error, setError] = useState<string>('');

  const canSave = useMemo(() => template.groups.length > 0, [template.groups.length]);

  const loadTemplate = async () => {
    setLoading(true);
    setError('');
    setMessage('');
    try {
      const response = await dynamicFieldsApi.getTemplate(classificationId);
      if (response) {
        setTemplate(response);
      } else {
        setTemplate(createEmptyTemplate(classificationId));
      }
      setMessage('Template loaded.');
    } catch (loadError) {
      setError('Failed to load template.');
      console.error(loadError);
    } finally {
      setLoading(false);
    }
  };

  const saveTemplate = async () => {
    setSaving(true);
    setError('');
    setMessage('');
    try {
      const response = await dynamicFieldsApi.upsertTemplate(classificationId, {
        ...template,
        classificationId,
      });
      setTemplate(response);
      setMessage('Template saved.');
    } catch (saveError) {
      setError('Failed to save template.');
      console.error(saveError);
    } finally {
      setSaving(false);
    }
  };

  const addGroup = () => {
    setTemplate((current) => ({
      ...current,
      groups: [
        ...current.groups,
        {
          id: createClientId(),
          title: '',
          order: current.groups.length,
          fields: [],
        },
      ],
    }));
  };

  return (
    <section className="rounded border p-4 space-y-4">
      <h2 className="text-xl font-semibold">Admin: Dynamic Template</h2>

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
        <button className="px-3 py-2 border rounded" onClick={loadTemplate} disabled={loading}>
          {loading ? 'Loading...' : 'Load'}
        </button>
      </div>

      <div className="grid gap-3 md:grid-cols-2">
        <label className="flex flex-col text-sm">
          Template Name
          <input
            type="text"
            className="border rounded px-2 py-1"
            value={template.name}
            onChange={(event) => setTemplate((current) => ({ ...current, name: event.target.value }))}
          />
        </label>
        <label className="flex items-center gap-2 text-sm mt-6">
          <input
            type="checkbox"
            checked={template.isActive}
            onChange={(event) => setTemplate((current) => ({ ...current, isActive: event.target.checked }))}
          />
          Active
        </label>
      </div>

      <div className="space-y-3">
        {template.groups.map((group, groupIndex) => (
          <div key={group.id} className="rounded border p-3 space-y-3">
            <div className="flex flex-wrap gap-2">
              <input
                className="border rounded px-2 py-1 flex-1"
                placeholder="Group title"
                value={group.title}
                onChange={(event) =>
                  setTemplate((current) => ({
                    ...current,
                    groups: current.groups.map((item) =>
                      item.id === group.id ? { ...item, title: event.target.value } : item
                    ),
                  }))
                }
              />
              <input
                type="number"
                className="border rounded px-2 py-1 w-24"
                placeholder="Order"
                value={group.order}
                onChange={(event) =>
                  setTemplate((current) => ({
                    ...current,
                    groups: current.groups.map((item) =>
                      item.id === group.id ? { ...item, order: Number(event.target.value) || 0 } : item
                    ),
                  }))
                }
              />
              <button
                className="px-2 py-1 border rounded"
                onClick={() =>
                  setTemplate((current) => ({
                    ...current,
                    groups: current.groups.filter((item) => item.id !== group.id),
                  }))
                }
              >
                Remove Group
              </button>
            </div>

            <div className="space-y-2">
              {group.fields.map((field) => (
                <div key={field.id} className="border rounded p-3 space-y-2">
                  <div className="grid gap-2 md:grid-cols-5">
                    <input
                      className="border rounded px-2 py-1"
                      placeholder="Key"
                      value={field.key}
                      onChange={(event) => {
                        const value = event.target.value;
                        setTemplate((current) => ({
                          ...current,
                          groups: current.groups.map((item) =>
                            item.id !== group.id
                              ? item
                              : {
                                  ...item,
                                  fields: item.fields.map((currentField) =>
                                    currentField.id === field.id ? { ...currentField, key: value } : currentField
                                  ),
                                }
                          ),
                        }));
                      }}
                    />
                    <input
                      className="border rounded px-2 py-1"
                      placeholder="Label"
                      value={field.label}
                      onChange={(event) => {
                        const value = event.target.value;
                        setTemplate((current) => ({
                          ...current,
                          groups: current.groups.map((item) =>
                            item.id !== group.id
                              ? item
                              : {
                                  ...item,
                                  fields: item.fields.map((currentField) =>
                                    currentField.id === field.id ? { ...currentField, label: value } : currentField
                                  ),
                                }
                          ),
                        }));
                      }}
                    />
                    <select
                      className="border rounded px-2 py-1"
                      value={field.fieldType}
                      onChange={(event) => {
                        const value = Number(event.target.value) as DynamicFieldType;
                        setTemplate((current) => ({
                          ...current,
                          groups: current.groups.map((item) =>
                            item.id !== group.id
                              ? item
                              : {
                                  ...item,
                                  fields: item.fields.map((currentField) =>
                                    currentField.id === field.id
                                      ? {
                                          ...currentField,
                                          fieldType: value,
                                          options: value === DynamicFieldType.Enum ? currentField.options : [],
                                          isMultiSelect:
                                            value === DynamicFieldType.Enum ? currentField.isMultiSelect : false,
                                        }
                                      : currentField
                                  ),
                                }
                          ),
                        }));
                      }}
                    >
                      {fieldTypeOptions.map((option) => (
                        <option key={option.value} value={option.value}>
                          {option.label}
                        </option>
                      ))}
                    </select>
                    <input
                      type="number"
                      className="border rounded px-2 py-1"
                      placeholder="Order"
                      value={field.order}
                      onChange={(event) => {
                        const value = Number(event.target.value) || 0;
                        setTemplate((current) => ({
                          ...current,
                          groups: current.groups.map((item) =>
                            item.id !== group.id
                              ? item
                              : {
                                  ...item,
                                  fields: item.fields.map((currentField) =>
                                    currentField.id === field.id ? { ...currentField, order: value } : currentField
                                  ),
                                }
                          ),
                        }));
                      }}
                    />
                    <button
                      className="px-2 py-1 border rounded"
                      onClick={() =>
                        setTemplate((current) => ({
                          ...current,
                          groups: current.groups.map((item) =>
                            item.id !== group.id
                              ? item
                              : { ...item, fields: item.fields.filter((currentField) => currentField.id !== field.id) }
                          ),
                        }))
                      }
                    >
                      Remove Field
                    </button>
                  </div>

                  <div className="grid gap-2 md:grid-cols-4">
                    <label className="text-xs flex items-center gap-2">
                      <input
                        type="checkbox"
                        checked={field.isRequired}
                        onChange={(event) => {
                          const value = event.target.checked;
                          setTemplate((current) => ({
                            ...current,
                            groups: current.groups.map((item) =>
                              item.id !== group.id
                                ? item
                                : {
                                    ...item,
                                    fields: item.fields.map((currentField) =>
                                      currentField.id === field.id ? { ...currentField, isRequired: value } : currentField
                                    ),
                                  }
                            ),
                          }));
                        }}
                      />
                      Required
                    </label>
                    <label className="text-xs flex items-center gap-2">
                      <input
                        type="checkbox"
                        checked={field.isActive}
                        onChange={(event) => {
                          const value = event.target.checked;
                          setTemplate((current) => ({
                            ...current,
                            groups: current.groups.map((item) =>
                              item.id !== group.id
                                ? item
                                : {
                                    ...item,
                                    fields: item.fields.map((currentField) =>
                                      currentField.id === field.id ? { ...currentField, isActive: value } : currentField
                                    ),
                                  }
                            ),
                          }));
                        }}
                      />
                      Active
                    </label>
                    <label className="text-xs flex items-center gap-2">
                      <input
                        type="checkbox"
                        disabled={field.fieldType !== DynamicFieldType.Enum}
                        checked={field.isMultiSelect}
                        onChange={(event) => {
                          const value = event.target.checked;
                          setTemplate((current) => ({
                            ...current,
                            groups: current.groups.map((item) =>
                              item.id !== group.id
                                ? item
                                : {
                                    ...item,
                                    fields: item.fields.map((currentField) =>
                                      currentField.id === field.id
                                        ? { ...currentField, isMultiSelect: value }
                                        : currentField
                                    ),
                                  }
                            ),
                          }));
                        }}
                      />
                      Multi Select
                    </label>
                  </div>

                  {(field.fieldType === DynamicFieldType.String || field.fieldType === DynamicFieldType.Number) && (
                    <div className="grid gap-2 md:grid-cols-4">
                      {field.fieldType === DynamicFieldType.String && (
                        <>
                          <input
                            type="number"
                            className="border rounded px-2 py-1"
                            placeholder="Min length"
                            value={field.minLength ?? ''}
                            onChange={(event) => {
                              const value = event.target.value === '' ? undefined : Number(event.target.value);
                              setTemplate((current) => ({
                                ...current,
                                groups: current.groups.map((item) =>
                                  item.id !== group.id
                                    ? item
                                    : {
                                        ...item,
                                        fields: item.fields.map((currentField) =>
                                          currentField.id === field.id
                                            ? { ...currentField, minLength: value }
                                            : currentField
                                        ),
                                      }
                                ),
                              }));
                            }}
                          />
                          <input
                            type="number"
                            className="border rounded px-2 py-1"
                            placeholder="Max length"
                            value={field.maxLength ?? ''}
                            onChange={(event) => {
                              const value = event.target.value === '' ? undefined : Number(event.target.value);
                              setTemplate((current) => ({
                                ...current,
                                groups: current.groups.map((item) =>
                                  item.id !== group.id
                                    ? item
                                    : {
                                        ...item,
                                        fields: item.fields.map((currentField) =>
                                          currentField.id === field.id
                                            ? { ...currentField, maxLength: value }
                                            : currentField
                                        ),
                                      }
                                ),
                              }));
                            }}
                          />
                          <input
                            className="border rounded px-2 py-1 md:col-span-2"
                            placeholder="Regex"
                            value={field.regex ?? ''}
                            onChange={(event) => {
                              const value = event.target.value === '' ? undefined : event.target.value;
                              setTemplate((current) => ({
                                ...current,
                                groups: current.groups.map((item) =>
                                  item.id !== group.id
                                    ? item
                                    : {
                                        ...item,
                                        fields: item.fields.map((currentField) =>
                                          currentField.id === field.id ? { ...currentField, regex: value } : currentField
                                        ),
                                      }
                                ),
                              }));
                            }}
                          />
                        </>
                      )}

                      {field.fieldType === DynamicFieldType.Number && (
                        <>
                          <input
                            type="number"
                            className="border rounded px-2 py-1"
                            placeholder="Min number"
                            value={field.minNumber ?? ''}
                            onChange={(event) => {
                              const value = event.target.value === '' ? undefined : Number(event.target.value);
                              setTemplate((current) => ({
                                ...current,
                                groups: current.groups.map((item) =>
                                  item.id !== group.id
                                    ? item
                                    : {
                                        ...item,
                                        fields: item.fields.map((currentField) =>
                                          currentField.id === field.id
                                            ? { ...currentField, minNumber: value }
                                            : currentField
                                        ),
                                      }
                                ),
                              }));
                            }}
                          />
                          <input
                            type="number"
                            className="border rounded px-2 py-1"
                            placeholder="Max number"
                            value={field.maxNumber ?? ''}
                            onChange={(event) => {
                              const value = event.target.value === '' ? undefined : Number(event.target.value);
                              setTemplate((current) => ({
                                ...current,
                                groups: current.groups.map((item) =>
                                  item.id !== group.id
                                    ? item
                                    : {
                                        ...item,
                                        fields: item.fields.map((currentField) =>
                                          currentField.id === field.id
                                            ? { ...currentField, maxNumber: value }
                                            : currentField
                                        ),
                                      }
                                ),
                              }));
                            }}
                          />
                        </>
                      )}
                    </div>
                  )}

                  {field.fieldType === DynamicFieldType.Enum && (
                    <div className="space-y-2 rounded border p-2">
                      <div className="flex items-center justify-between">
                        <span className="text-sm font-medium">Options</span>
                        <button
                          className="px-2 py-1 border rounded text-sm"
                          onClick={() =>
                            setTemplate((current) => ({
                              ...current,
                              groups: current.groups.map((item) =>
                                item.id !== group.id
                                  ? item
                                  : {
                                      ...item,
                                      fields: item.fields.map((currentField) =>
                                        currentField.id === field.id
                                          ? {
                                              ...currentField,
                                              options: [
                                                ...currentField.options,
                                                { ...newOption(), order: currentField.options.length },
                                              ],
                                            }
                                          : currentField
                                      ),
                                    }
                              ),
                            }))
                          }
                        >
                          Add Option
                        </button>
                      </div>

                      {field.options.map((option) => (
                        <div key={option.id} className="grid gap-2 md:grid-cols-4">
                          <input
                            className="border rounded px-2 py-1"
                            placeholder="Value"
                            value={option.value}
                            onChange={(event) => {
                              const value = event.target.value;
                              setTemplate((current) => ({
                                ...current,
                                groups: current.groups.map((item) =>
                                  item.id !== group.id
                                    ? item
                                    : {
                                        ...item,
                                        fields: item.fields.map((currentField) =>
                                          currentField.id !== field.id
                                            ? currentField
                                            : {
                                                ...currentField,
                                                options: currentField.options.map((currentOption) =>
                                                  currentOption.id === option.id
                                                    ? { ...currentOption, value }
                                                    : currentOption
                                                ),
                                              }
                                        ),
                                      }
                                ),
                              }));
                            }}
                          />
                          <input
                            className="border rounded px-2 py-1"
                            placeholder="Label"
                            value={option.label}
                            onChange={(event) => {
                              const value = event.target.value;
                              setTemplate((current) => ({
                                ...current,
                                groups: current.groups.map((item) =>
                                  item.id !== group.id
                                    ? item
                                    : {
                                        ...item,
                                        fields: item.fields.map((currentField) =>
                                          currentField.id !== field.id
                                            ? currentField
                                            : {
                                                ...currentField,
                                                options: currentField.options.map((currentOption) =>
                                                  currentOption.id === option.id
                                                    ? { ...currentOption, label: value }
                                                    : currentOption
                                                ),
                                              }
                                        ),
                                      }
                                ),
                              }));
                            }}
                          />
                          <button
                            className="px-2 py-1 border rounded"
                            onClick={() =>
                              setTemplate((current) => ({
                                ...current,
                                groups: current.groups.map((item) =>
                                  item.id !== group.id
                                    ? item
                                    : {
                                        ...item,
                                        fields: item.fields.map((currentField) =>
                                          currentField.id !== field.id
                                            ? currentField
                                            : {
                                                ...currentField,
                                                options: currentField.options.filter((currentOption) => currentOption.id !== option.id),
                                              }
                                        ),
                                      }
                                ),
                              }))
                            }
                          >
                            Remove
                          </button>
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              ))}
            </div>

            <button
              className="px-2 py-1 border rounded text-sm"
              onClick={() =>
                setTemplate((current) => ({
                  ...current,
                  groups: current.groups.map((item) =>
                    item.id === group.id
                      ? {
                          ...item,
                          fields: [...item.fields, { ...newField(), order: item.fields.length }],
                        }
                      : item
                  ),
                }))
              }
            >
              Add Field
            </button>
          </div>
        ))}
      </div>

      <div className="flex gap-2">
        <button className="px-3 py-2 border rounded" onClick={addGroup}>
          Add Group
        </button>
        <button className="px-3 py-2 border rounded" disabled={!canSave || saving} onClick={saveTemplate}>
          {saving ? 'Saving...' : 'Save Template'}
        </button>
      </div>

      {message && <p className="text-green-700 text-sm">{message}</p>}
      {error && <p className="text-red-700 text-sm">{error}</p>}
      <p className="text-xs text-gray-600">Group {groupIndexHint(template.groups.length)}.</p>
    </section>
  );
};

const groupIndexHint = (count: number): string => {
  if (count === 0) {
    return 'list is empty';
  }

  return `count: ${count}`;
};

export default DynamicTemplateAdmin;

