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

export interface CaseLoanListItemDTO {
  id: string;
  borrowerFirstName: string;
  borrowerLastName: string;
  borrowerFullName: string;
  caseNumber: string;
  nationalId: string;
  contactNumber: string;
  checkoutDate: string;
  expectedReturnDate?: string | null;
  actualReturnDate?: string | null;
  status: CaseLoanStatus;
  receiverUnit?: string | null;
  registeredBy?: string | null;
  description?: string | null;
  isReturned: boolean;
  createdAt: string;
  modifiedAt?: string | null;
}

export interface CaseLoanDTO {
  id: string;
  borrowerFirstName: string;
  borrowerLastName: string;
  caseNumber: string;
  nationalId: string;
  contactNumber: string;
  checkoutDate: string;
  expectedReturnDate?: string | null;
  actualReturnDate?: string | null;
  status: CaseLoanStatus;
  receiverUnit?: string | null;
  registeredBy?: string | null;
  description?: string | null;
  isReturned: boolean;
  createdAt: string;
  modifiedAt?: string | null;
}

export interface CaseLoanListQueryParams {
  pageNumber: number;
  pageSize: number;
  caseNumber?: string;
  nationalId?: string;
  borrowerLastName?: string;
  columnFilterField?: string;
  columnFilterOperator?: string;
  columnFilterValue?: string;
}

export interface CreateCaseLoanPayload {
  borrowerFirstName: string;
  borrowerLastName: string;
  caseNumber: string;
  nationalId: string;
  contactNumber: string;
  checkoutDate: string;
  expectedReturnDate?: string;
  actualReturnDate?: string;
  status: CaseLoanStatus;
  receiverUnit?: string;
  registeredBy?: string;
  description?: string;
  isReturned?: boolean;
}

export type UpdateCaseLoanPayload = Omit<CreateCaseLoanPayload, 'isReturned'> & { isReturned: boolean };

export type CaseLoanStatus = 'Loaned' | 'Returned' | 'Overdue';

export interface CaseLoanFilterState {
  caseNumber: string;
  nationalId: string;
  borrowerLastName: string;
}
