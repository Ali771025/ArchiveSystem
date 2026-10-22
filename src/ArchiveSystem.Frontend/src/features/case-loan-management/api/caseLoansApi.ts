import { api } from '@/services/api';
import { unwrapApiResult } from '@/services/apiResult';
import type {
  ApiResult,
  CaseLoanDTO,
  CaseLoanListItemDTO,
  CaseLoanListQueryParams,
  CreateCaseLoanPayload,
  PagedResponse,
  UpdateCaseLoanPayload,
} from '@/features/case-loan-management/types';

const caseLoansBasePath = '/v1/case-loans';

const normalizeDigits = (value?: string) =>
  value
    ?.replace(/[۰-۹]/g, (digit) => String(digit.charCodeAt(0) - 1776))
    .replace(/[٠-٩]/g, (digit) => String(digit.charCodeAt(0) - 1632));

const normalizeText = (value?: string) => {
  const trimmed = value?.trim();
  return trimmed || undefined;
};

const normalizeQuery = (query: CaseLoanListQueryParams) => ({
  pageNumber: query.pageNumber,
  pageSize: query.pageSize,
  caseNumber: normalizeText(query.caseNumber),
  nationalId: normalizeText(normalizeDigits(query.nationalId)),
  borrowerLastName: normalizeText(query.borrowerLastName),
  columnFilterField: normalizeText(query.columnFilterField),
  columnFilterOperator: normalizeText(query.columnFilterOperator),
  columnFilterValue: normalizeText(query.columnFilterValue),
});

const normalizeCreatePayload = (payload: CreateCaseLoanPayload): CreateCaseLoanPayload => ({
  ...payload,
  borrowerFirstName: payload.borrowerFirstName.trim(),
  borrowerLastName: payload.borrowerLastName.trim(),
  caseNumber: payload.caseNumber.trim(),
  nationalId: normalizeDigits(payload.nationalId)?.trim() ?? payload.nationalId.trim(),
  contactNumber: normalizeDigits(payload.contactNumber)?.trim() ?? payload.contactNumber.trim(),
  expectedReturnDate: normalizeText(payload.expectedReturnDate),
  actualReturnDate: normalizeText(payload.actualReturnDate),
  receiverUnit: normalizeText(payload.receiverUnit),
  registeredBy: normalizeText(payload.registeredBy),
  description: normalizeText(payload.description),
  isReturned: payload.status === 'Returned' || (payload.isReturned ?? false),
});

const normalizeUpdatePayload = (payload: UpdateCaseLoanPayload): UpdateCaseLoanPayload => ({
  ...payload,
  borrowerFirstName: payload.borrowerFirstName.trim(),
  borrowerLastName: payload.borrowerLastName.trim(),
  caseNumber: payload.caseNumber.trim(),
  nationalId: normalizeDigits(payload.nationalId)?.trim() ?? payload.nationalId.trim(),
  contactNumber: normalizeDigits(payload.contactNumber)?.trim() ?? payload.contactNumber.trim(),
  expectedReturnDate: normalizeText(payload.expectedReturnDate),
  actualReturnDate: normalizeText(payload.actualReturnDate),
  receiverUnit: normalizeText(payload.receiverUnit),
  registeredBy: normalizeText(payload.registeredBy),
  description: normalizeText(payload.description),
  isReturned: payload.status === 'Returned' || payload.isReturned,
});

export const caseLoansApi = {
  async getCaseLoans(query: CaseLoanListQueryParams): Promise<PagedResponse<CaseLoanListItemDTO>> {
    const response = await api.get<ApiResult<PagedResponse<CaseLoanListItemDTO>>>(caseLoansBasePath, {
      params: normalizeQuery(query),
    });

    return unwrapApiResult(response.data, 'دریافت فهرست امانت پرونده‌ها با خطا مواجه شد.');
  },

  async getCaseLoanById(caseLoanId: string): Promise<CaseLoanDTO> {
    const response = await api.get<ApiResult<CaseLoanDTO>>(`${caseLoansBasePath}/${caseLoanId}`);
    return unwrapApiResult(response.data, 'اطلاعات امانت پرونده دریافت نشد.');
  },

  async createCaseLoan(payload: CreateCaseLoanPayload): Promise<CaseLoanDTO> {
    const response = await api.post<ApiResult<CaseLoanDTO>>(caseLoansBasePath, normalizeCreatePayload(payload));
    return unwrapApiResult(response.data, 'ثبت امانت پرونده انجام نشد.');
  },

  async updateCaseLoan(caseLoanId: string, payload: UpdateCaseLoanPayload): Promise<CaseLoanDTO> {
    const response = await api.post<ApiResult<CaseLoanDTO>>(`${caseLoansBasePath}/${caseLoanId}`, normalizeUpdatePayload(payload));
    return unwrapApiResult(response.data, 'ویرایش امانت پرونده انجام نشد.');
  },

  async deleteCaseLoan(caseLoanId: string): Promise<void> {
    await api.post(`${caseLoansBasePath}/${caseLoanId}/delete`);
  },
};
