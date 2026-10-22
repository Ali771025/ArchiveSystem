export type DocumentSortDirection = 'asc' | 'desc';

export interface DynamicFilter {
  source: 'document' | 'dossier' | 'owner' | 'dynamicField';
  field: string;
  operator:
    | 'Contains'
    | 'Equals'
    | 'StartsWith'
    | 'EndsWith'
    | 'NotEquals'
    | 'GreaterThan'
    | 'GreaterThanOrEqual'
    | 'LessThan'
    | 'LessThanOrEqual'
    | 'Between'
    | 'In'
    | 'NotIn'
    | 'IsNull'
    | 'IsNotNull'
    | 'IsEmpty'
    | 'IsNotEmpty';
  value?: string;
  valueTo?: string;
  values?: string[];
  valueType?: string;
}

export interface AdvancedDocumentSearchRequest {
  generalQuery?: string;
  documentName?: string;
  caseNumber?: string;
  ownerName?: string;
  bankName?: string;
  tags: string[];
  filters: DynamicFilter[];
  includeDeleted: boolean;
  createdFrom?: string;
  createdTo?: string;
  sortBy: string;
  sortDirection: DocumentSortDirection;
  pageNumber: number;
  pageSize: number;
}

export interface DocumentSearchResultDto {
  documentId: number;
  documentName: string;
  dossierNumber: string;
  owners: string;
  bankName?: string;
  modifiedAt: string;
  createdAt: string;
  contentType: string;
  extension: string;
  isDeleted: boolean;
}

export interface AdvancedDocumentSearchResponse {
  items: DocumentSearchResultDto[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}
