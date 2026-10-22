export interface ApiResult<T> {
  success: boolean;
  errorCode?: string;
  message: string;
  data?: T;
  errors?: Record<string, string[]>;
}

export interface PagedResponse<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export type TaxpayerType = "NaturalPerson" | "LegalEntity" | 1 | 2;

export interface TaxpayerListItemDTO {
  id: string;
  taxpayerType: TaxpayerType;
  firstName?: string | null;
  lastName?: string | null;
  nationalId?: string | null;
  companyName?: string | null;
  nationalCompanyId?: string | null;
  mobile?: string | null;
}

export interface TaxpayerDTO extends TaxpayerListItemDTO {
  identityNumber?: string | null;
  landlinePhone?: string | null;
  postalCode?: string | null;
  address?: string | null;
  createdAt: string;
  modifiedAt?: string | null;
}

export interface TaxpayerListQueryParams {
  pageNumber: number;
  pageSize: number;
  searchTerm?: string;
  firstName?: string;
  lastName?: string;
  nationalId?: string;
  columnFilterField?: string;
  columnFilterOperator?: string;
  columnFilterValue?: string;
}

export interface CreateTaxpayerPayload {
  taxpayerType: TaxpayerType;
  firstName?: string;
  lastName?: string;
  nationalId?: string;
  companyName?: string;
  nationalCompanyId?: string;
  identityNumber?: string;
  mobile?: string;
  landlinePhone?: string;
  postalCode?: string;
  address?: string;
}

export type UpdateTaxpayerPayload = CreateTaxpayerPayload;

export interface TaxpayerFilterState {
  firstName: string;
  lastName: string;
  nationalId: string;
}
