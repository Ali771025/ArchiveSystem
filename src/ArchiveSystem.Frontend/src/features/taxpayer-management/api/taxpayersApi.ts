import { api } from "@/services/api";
import { unwrapApiResult } from "@/services/apiResult";
import type {
  ApiResult,
  CreateTaxpayerPayload,
  PagedResponse,
  TaxpayerDTO,
  TaxpayerListItemDTO,
  TaxpayerListQueryParams,
  UpdateTaxpayerPayload,
} from "@/features/taxpayer-management/types";

const taxpayersBasePath = "/v1/taxpayers";

const normalizeDigits = (value?: string) =>
  value
    ?.replace(/[۰-۹]/g, (digit) => String(digit.charCodeAt(0) - 1776))
    .replace(/[٠-٩]/g, (digit) => String(digit.charCodeAt(0) - 1632));

const normalizeText = (value?: string) => {
  const trimmed = value?.trim();
  return trimmed || undefined;
};

const normalizePayload = <
  T extends CreateTaxpayerPayload | UpdateTaxpayerPayload,
>(
  payload: T,
): T => ({
  ...payload,
  firstName: normalizeText(payload.firstName),
  lastName: normalizeText(payload.lastName),
  nationalId: normalizeText(normalizeDigits(payload.nationalId)),
  companyName: normalizeText(payload.companyName),
  nationalCompanyId: normalizeText(normalizeDigits(payload.nationalCompanyId)),
  identityNumber: normalizeText(payload.identityNumber),
  mobile: normalizeText(normalizeDigits(payload.mobile)),
  landlinePhone: normalizeText(normalizeDigits(payload.landlinePhone)),
  postalCode: normalizeText(normalizeDigits(payload.postalCode)),
  address: normalizeText(payload.address),
});

const normalizeQuery = (query: TaxpayerListQueryParams) => ({
  pageNumber: query.pageNumber,
  pageSize: query.pageSize,
  searchTerm: normalizeText(query.searchTerm),
  firstName: normalizeText(query.firstName),
  lastName: normalizeText(query.lastName),
  nationalId: normalizeText(normalizeDigits(query.nationalId)),
  columnFilterField: normalizeText(query.columnFilterField),
  columnFilterOperator: normalizeText(query.columnFilterOperator),
  columnFilterValue: normalizeText(query.columnFilterValue),
});

export const taxpayersApi = {
  async getTaxpayers(
    query: TaxpayerListQueryParams,
  ): Promise<PagedResponse<TaxpayerListItemDTO>> {
    const response = await api.get<
      ApiResult<PagedResponse<TaxpayerListItemDTO>>
    >(taxpayersBasePath, {
      params: normalizeQuery(query),
    });

    return unwrapApiResult(
      response.data,
      "دریافت فهرست مودیان با خطا مواجه شد.",
    );
  },

  async getOwnerCandidates(
    query: TaxpayerListQueryParams,
  ): Promise<PagedResponse<TaxpayerListItemDTO>> {
    const response = await api.get<
      ApiResult<PagedResponse<TaxpayerListItemDTO>>
    >("/dossiers/owner-candidates", {
      params: normalizeQuery(query),
    });

    return unwrapApiResult(
      response.data,
      "دریافت فهرست مودیان قابل اتصال با خطا مواجه شد.",
    );
  },

  async getTaxpayerById(taxpayerId: string): Promise<TaxpayerDTO> {
    const response = await api.get<ApiResult<TaxpayerDTO>>(
      `${taxpayersBasePath}/${taxpayerId}`,
    );
    return unwrapApiResult(response.data, "اطلاعات مودی دریافت نشد.");
  },

  async createTaxpayer(payload: CreateTaxpayerPayload): Promise<TaxpayerDTO> {
    const response = await api.post<ApiResult<TaxpayerDTO>>(
      taxpayersBasePath,
      normalizePayload(payload),
    );
    return unwrapApiResult(response.data, "ایجاد مودی انجام نشد.");
  },

  async updateTaxpayer(
    taxpayerId: string,
    payload: UpdateTaxpayerPayload,
  ): Promise<TaxpayerDTO> {
    const response = await api.put<ApiResult<TaxpayerDTO>>(
      `${taxpayersBasePath}/${taxpayerId}`,
      normalizePayload(payload),
    );
    return unwrapApiResult(response.data, "ویرایش مودی انجام نشد.");
  },

  async deleteTaxpayer(taxpayerId: string): Promise<void> {
    await api.delete(`${taxpayersBasePath}/${taxpayerId}`);
  },
};
