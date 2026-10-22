export type DossierLogicalOperator = 'And' | 'Or';
export type DossierSearchFieldSource = 'Dossier' | 'Owner' | 'DynamicField';
export type DossierSearchValueType = 'Text' | 'Integer' | 'Decimal' | 'Date' | 'DateTime' | 'Time' | 'Boolean';
export type DossierComparisonOperator =
  | 'Equals'
  | 'NotEquals'
  | 'Contains'
  | 'StartsWith'
  | 'EndsWith'
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

export type DossierDynamicFieldType =
  | 'Text'
  | 'Dropdown'
  | 'Date'
  | 'DateTime'
  | 'Time'
  | 'Integer'
  | 'Decimal'
  | 'Checkbox'
  | 'MultilineText';

export interface DossierSearchFieldOption {
  value: string;
  label: string;
}

export interface DossierSearchFieldDescriptor {
  key: string;
  title: string;
  source: DossierSearchFieldSource;
  field: string;
  valueType: DossierSearchValueType;
  isNullable: boolean;
  supportedOperators: DossierComparisonOperator[];
  dynamicFieldApiName?: string;
  dynamicFieldType?: DossierDynamicFieldType;
  dynamicFieldId?: string;
  classificationId?: number;
  options: DossierSearchFieldOption[];
}

export interface DossierAdvancedSearchFieldCatalog {
  fields: DossierSearchFieldDescriptor[];
  generatedAtUtc: string;
}

export interface DossierConditionRule {
  id: string;
  source: DossierSearchFieldSource;
  field: string;
  operator: DossierComparisonOperator;
  value?: unknown;
  values?: unknown[];
  valueType?: DossierSearchValueType;
  dynamicFieldApiName?: string;
  dynamicFieldType?: DossierDynamicFieldType;
}

export interface DossierConditionGroup {
  id: string;
  operator: DossierLogicalOperator;
  groups: DossierConditionGroup[];
  rules: DossierConditionRule[];
}

export interface DossierAdvancedSearchRequest {
  root: DossierConditionGroup;
  page: number;
  pageSize: number;
  sortBy: string;
  sortDirection: 'asc' | 'desc';
  includeDeleted: boolean;
}

export interface DossierAdvancedSearchItem {
  id: number;
  dossierNumber: string;
  externalCaseNumber: string;
  address: string;
  usageType: number;
  latitude?: number;
  longitude?: number;
  classificationId: number;
  createdAt: string;
  modifiedAt?: string;
  isDeleted: boolean;
  ownerNames: string[];
}

export interface DossierAdvancedSearchResult {
  items: DossierAdvancedSearchItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}
