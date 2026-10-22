// @vitest-environment jsdom

import { ThemeProvider } from '@mui/material/styles';
import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { DynamicFormRenderer, validateDynamicValues } from '@/components/dynamic/DynamicFormRenderer';
import { appTheme } from '@/theme';
import type { DynamicTemplate } from '@/types/archive';

const template: DynamicTemplate = {
  id: 'tpl-1',
  classificationId: 1,
  version: 1,
  publishedAt: new Date().toISOString(),
  groups: [
    {
      id: 'group-1',
      title: 'اطلاعات پایه',
      apiName: 'base',
      order: 0,
      fields: [
        {
          id: 'f1',
          apiName: 'registryNumber',
          label: 'شماره ثبت',
          type: 'text',
          required: true,
          showAsFilter: true,
          filterType: 'contains',
          order: 0,
          options: [],
          rules: { regex: '^\\d+$', errorMessage: 'فقط عدد وارد کنید.' },
        },
      ],
    },
  ],
};

describe('DynamicFormRenderer', () => {
  it('renders fields and emits changes', () => {
    const onChange = vi.fn();

    render(
      <ThemeProvider theme={appTheme}>
        <DynamicFormRenderer template={template} values={[]} onChange={onChange} />
      </ThemeProvider>,
    );

    const input = screen.getByLabelText('شماره ثبت');
    fireEvent.change(input, { target: { value: '12345' } });

    expect(onChange).toHaveBeenCalled();
  });

  it('validates required and regex rules', () => {
    const requiredErrors = validateDynamicValues(template, []);
    expect(requiredErrors.f1).toBeTruthy();

    const regexErrors = validateDynamicValues(template, [{ fieldId: 'f1', value: 'ABC', values: [] }]);
    expect(regexErrors.f1).toBe('فقط عدد وارد کنید.');

    const noErrors = validateDynamicValues(template, [{ fieldId: 'f1', value: '123', values: [] }]);
    expect(noErrors.f1).toBeUndefined();
  });

  it('renders multiselect options and emits selected option ids', () => {
    const onChange = vi.fn();
    const multiTemplate: DynamicTemplate = {
      ...template,
      groups: [{
        ...template.groups[0],
        fields: [{
          ...template.groups[0].fields[0],
          id: 'multi-1',
          label: 'نوع خدمات',
          type: 'multiselect',
          rules: {},
          options: [
            { id: 'option-1', value: 'one', label: 'خدمت اول' },
            { id: 'option-2', value: 'two', label: 'خدمت دوم' },
          ],
        }],
      }],
    };

    render(
      <ThemeProvider theme={appTheme}>
        <DynamicFormRenderer template={multiTemplate} values={[]} onChange={onChange} />
      </ThemeProvider>,
    );

    fireEvent.mouseDown(screen.getByLabelText('نوع خدمات'));
    fireEvent.click(screen.getByText('خدمت اول'));

    expect(onChange).toHaveBeenCalledWith([
      { fieldId: 'multi-1', fieldType: 'multiselect', value: null, values: ['option-1'] },
    ]);
  });
});
