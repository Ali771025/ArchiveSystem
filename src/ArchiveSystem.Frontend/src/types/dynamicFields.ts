export enum DynamicFieldType {
  String = 1,
  Number = 2,
  Date = 3,
  Bool = 4,
  Enum = 5,
  Decimal = 6,
  MultiLineText = 7,
  MultiSelect = 8,
  CheckBox = 9,
}

export enum DynamicFieldFilterType {
  Contains = 1,
  Equals = 2,
  NotEquals = 3,
  LessThan = 4,
  GreaterThan = 5,
  LessThanOrEqual = 6,
  GreaterThanOrEqual = 7,
  Range = 8,
}

export interface DynamicFieldOption {
  id: string;
  value: string;
  label: string;
  order: number;
  isActive: boolean;
}

export interface DynamicFieldDefinition {
  id: string;
  key: string;
  label: string;
  fieldType: DynamicFieldType;
  isRequired: boolean;
  minNumber?: number;
  maxNumber?: number;
  minLength?: number;
  maxLength?: number;
  regex?: string;
  isMultiSelect: boolean;
  showAsFilter: boolean;
  filterType: DynamicFieldFilterType;
  order: number;
  isActive: boolean;
  options: DynamicFieldOption[];
}

export interface DynamicFieldGroup {
  id: string;
  title: string;
  order: number;
  fields: DynamicFieldDefinition[];
}

export interface DynamicTemplate {
  id: string;
  classificationId: number;
  name: string;
  isActive: boolean;
  groups: DynamicFieldGroup[];
}

export interface CaseDynamicFieldValue {
  fieldId: string;
  key: string;
  label: string;
  fieldType: DynamicFieldType;
  isRequired: boolean;
  isMultiSelect: boolean;
  stringValue?: string;
  numberValue?: number;
  dateValue?: string;
  boolValue?: boolean;
  optionIds: string[];
  options: DynamicFieldOption[];
}

export interface CaseDynamicFields {
  caseId: number;
  classificationId: number;
  templateId: string;
  values: CaseDynamicFieldValue[];
}

export interface DynamicFilter {
  fieldId: string;
  op:
    | 'contains'
    | 'equals'
    | 'notEquals'
    | 'lessThan'
    | 'greaterThan'
    | 'lessThanOrEqual'
    | 'greaterThanOrEqual'
    | 'range'
    | 'in'
    | 'isEmpty';
  value?: unknown;
  fromValue?: unknown;
  toValue?: unknown;
  values?: unknown[];
}

export interface CaseSearchItem {
  id: number;
  caseNumber: string;
  title: string;
  classificationId: number;
  createdAt: string;
  closedAt?: string;
}

export interface CaseSearchResult {
  items: CaseSearchItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}
