import apiClient from './apiClient';
import {
  CaseDynamicFields,
  CaseSearchResult,
  DynamicFilter,
  DynamicTemplate,
} from '@/types/dynamicFields';
import { unwrapApiResultOrRaw, type ApiResult } from './apiResult';

export const dynamicFieldsApi = {
  async getTemplate(classificationId: number): Promise<DynamicTemplate | null> {
    const response = await apiClient.get<ApiResult<DynamicTemplate | null> | DynamicTemplate | null>(
      `/classifications/${classificationId}/dynamic-template`,
    );
    return response.data === null
      ? null
      : unwrapApiResultOrRaw(response.data, 'قالب فیلدهای پویا دریافت نشد.');
  },

  async upsertTemplate(classificationId: number, template: DynamicTemplate): Promise<DynamicTemplate> {
    const response = await apiClient.post<ApiResult<DynamicTemplate> | DynamicTemplate>(
      `/classifications/${classificationId}/dynamic-template`,
      template,
    );
    return unwrapApiResultOrRaw(response.data, 'ذخیره قالب فیلدهای پویا انجام نشد.');
  },

  async getCaseDynamicFields(caseId: number): Promise<CaseDynamicFields> {
    const response = await apiClient.get<ApiResult<CaseDynamicFields> | CaseDynamicFields>(`/cases/${caseId}/dynamic-fields`);
    return unwrapApiResultOrRaw(response.data, 'مقادیر فیلدهای پویا دریافت نشد.');
  },

  async upsertCaseDynamicFields(
    caseId: number,
    values: CaseDynamicFields['values']
  ): Promise<CaseDynamicFields> {
    const response = await apiClient.put<ApiResult<CaseDynamicFields> | CaseDynamicFields>(`/cases/${caseId}/dynamic-fields`, {
      values: values.map((value) => ({
        fieldId: value.fieldId,
        stringValue: value.stringValue ?? null,
        numberValue: value.numberValue ?? null,
        dateValue: value.dateValue ?? null,
        boolValue: value.boolValue ?? null,
        optionIds: value.optionIds ?? [],
      })),
    });

    return unwrapApiResultOrRaw(response.data, 'ذخیره مقادیر فیلدهای پویا انجام نشد.');
  },

  async searchCases(request: {
    classificationId?: number;
    page?: number;
    pageSize?: number;
    dynamicFilters: DynamicFilter[];
  }): Promise<CaseSearchResult> {
    const response = await apiClient.post<ApiResult<CaseSearchResult> | CaseSearchResult>('/cases/search', request);
    return unwrapApiResultOrRaw(response.data, 'جستجوی پرونده‌ها انجام نشد.');
  },
};

