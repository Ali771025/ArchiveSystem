export type SubjectType = 1 | 2;

export type PermissionKey =
  | 'admin.access'
  | 'classification.read'
  | 'classification.write'
  | 'classification.manage'
  | 'tag.view'
  | 'tag.assign'
  | 'reports.view'
  | 'exports.create'
  | 'dynamicfields.manage'
  | 'categories.create'
  | 'categories.edit'
  | 'categories.read'
  | 'categories.manage'
  | 'reports.exportexcel'
  | 'tags.manage'
  | 'dossiers.manage'
  | 'files.read'
  | 'files.manage'
  | 'files.delete'
  | 'roles.manage'
  | 'caseloans.manage'
  | 'taxpayers.manage'
  | 'users.manage'
  | 'behsaz.search'
  | 'tabs.dashboard.view'
  | 'tabs.archive.view'
  | 'tabs.roles.view'
  | 'tabs.settings.view'
  | 'tabs.reports.view'
  | 'tabs.accesscontrol.view'
  | 'tabs.caseloans.view'
  | 'tabs.taxpayers.view'
  | 'tabs.users.view';

export interface SessionUser {
  id: number;
  username: string;
  email: string;
  firstName: string;
  lastName: string;
  personnelCode?: string;
  roles: string[];
  permissions: PermissionKey[];
}

export interface ApiErrorPayload {
  status?: number;
  title?: string;
  message?: string;
  detail?: string;
  statusCode?: number;
  severity?: 'Critical' | 'Warning' | 'Information' | 'Error';
  errors?: Record<string, string[]>;
  correlationId?: string;
  traceId?: string;
}

export interface AppError {
  status: number;
  message: string;
  details?: string;
  traceId?: string;
  fieldErrors?: Record<string, string[]>;
}

export interface ClassificationNode {
  id: number;
  title: string;
  parentId: number | null;
  level: number;
  path: string;
  caseCount: number;
  isActive: boolean;
  systemKey?: string | null;
}

export interface CaseOwner {
  id: number;
  fullName: string;
  nationalCode: string;
  taxpayerId?: string | null;
  phone: string;
}

export interface CaseSummary {
  id: number;
  caseNumber: string;
  externalCaseNumber: string;
  avatarImagePath?: string | null;
  address: string;
  postalCode?: string | null;
  updatedAt: string;
  classificationId: number;
  classificationTitle: string;
  createdByUserId?: number | null;
  createdByUserName?: string | null;
  loanStatus?: 'Loaned' | 'Returned' | 'Overdue' | 'Cancelled' | string | null;
  fileCount?: number;
  ownerNames: string[];
  tags: string[];
}

export interface CaseDetails extends CaseSummary {
  owners: CaseOwner[];
  history: AuditRecord[];
}

export interface CaseSearchRequest {
  classificationId?: number;
  page: number;
  pageSize: number;
  sortBy: keyof CaseSummary | 'updatedAt';
  sortDirection: 'asc' | 'desc';
  ownerName?: string;
  createdBy?: string;
  fromDate?: string;
  toDate?: string;
  tags?: number[];
  externalCaseNumber?: string;
  dynamicFilterExpression?: DynamicFilterExpression;
}

export type DynamicFilterLogic = 'and' | 'or';

export interface DynamicFilterCondition {
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

export interface DynamicFilterGroup {
  logic: DynamicFilterLogic;
  filters: DynamicFilterCondition[];
}

export interface DynamicFilterExpression {
  logic: DynamicFilterLogic;
  groups: DynamicFilterGroup[];
}

export interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export type DynamicFieldType =
  | 'text'
  | 'dropdown'
  | 'date'
  | 'datetime'
  | 'time'
  | 'int'
  | 'decimal'
  | 'checkbox'
  | 'multiselect'
  | 'textarea';

export type DynamicFieldFilterType =
  | 'contains'
  | 'equals'
  | 'notEquals'
  | 'lessThan'
  | 'greaterThan'
  | 'lessThanOrEqual'
  | 'greaterThanOrEqual'
  | 'range';

export interface DynamicFieldOption {
  id: string;
  label: string;
  value: string;
}

export interface DynamicFieldRule {
  regex?: string;
  errorMessage?: string;
  min?: number;
  max?: number;
}

export interface DynamicFieldDefinition {
  id: string;
  apiName: string;
  label: string;
  type: DynamicFieldType;
  required: boolean;
  showAsFilter: boolean;
  filterType: DynamicFieldFilterType;
  order: number;
  options: DynamicFieldOption[];
  rules: DynamicFieldRule;
}

export interface DynamicFieldGroup {
  id: string;
  title: string;
  apiName: string;
  order: number;
  fields: DynamicFieldDefinition[];
}

export interface DynamicTemplate {
  id: string;
  classificationId: number;
  name?: string;
  isActive?: boolean;
  version: number;
  groups: DynamicFieldGroup[];
  publishedAt: string;
}

export interface CaseDynamicValue {
  fieldId: string;
  fieldType?: DynamicFieldType;
  value: string | number | boolean | null;
  values: string[];
}

export interface ArchiveTag {
  id: number;
  name: string;
  scope: 'global' | 'classification';
  classificationId?: number;
  description?: string;
}

export interface FileVersion {
  id: number;
  versionNumber: number;
  size: number;
  uploadedAt: string;
  uploader: string;
  ocrStatus: 'Pending' | 'Processing' | 'Completed' | 'Failed';
  ocrError?: string | null;
  thumbnailStatus: 'Pending' | 'Completed' | 'Failed';
  ocrWordCount: number;
  ocrLanguage: string;
  ocrTextPreview: string;
}

export interface ArchiveFile {
  id: number;
  caseId: number;
  name: string;
  extension: string;
  contentType: string;
  size: number;
  uploader: string;
  uploadedAt: string;
  latestVersion: number;
  status: number;
  tags: number[];
  ocrError?: string | null;
  processing: {
    ocr: 'Pending' | 'Processing' | 'Completed' | 'Failed';
    thumbnail: 'Pending' | 'Completed' | 'Failed';
  };
}

export interface RoleDto {
  id: number;
  name: string;
  isSystem: boolean;
  description?: string | null;
}

export interface PermissionDto {
  id: number;
  key: string;
  description: string;
}

export interface AdminUserDto {
  id: number;
  userName: string;
  email: string;
  firstName?: string | null;
  lastName?: string | null;
}

export interface ClassificationAclEntryDto {
  id: number;
  subjectType: SubjectType;
  subjectId: number;
  classificationId: number;
  permissionMask: number;
  isExplicit: boolean;
  inheritMode: number;
  cascade?: boolean;
}

export interface TagAclEntryDto {
  id: number;
  subjectType: SubjectType;
  subjectId: number;
  tagId: number;
  permissionMask: number;
}

export interface AuditRecord {
  id: number;
  timestamp: string;
  actorName: string;
  actionType: string;
  entityType: string;
  detail: string;
}

export interface ExportJob {
  id: string;
  type: string;
  status: 'pending' | 'running' | 'completed' | 'failed' | 'expired';
  progressPercent: number;
  createdAt: string;
  completedAt?: string;
  errorMessage?: string;
}

export interface BehsazResult {
  externalCaseNumber: string;
  title: string;
  ownerName: string;
  nationalCode: string;
  registeredAt: string;
}

export interface BehsazQuery {
  text: string;
  fromDate?: string;
  toDate?: string;
}

export interface BehsazRecord {
  number: string;
  manualNumber: string;
  isApartment: boolean;
  stateLocation: string;
  instaurationCode: string;
  peopleProperty: string;
  mobile: string;
  peopleKey: string;
  tBasPeopleGroupID?: number | null;
  peopleGroupName: string;
  address: string;
  plaque: string;
  archive: string;
  tBasZoneID?: number | null;
  userName: string;
}

export interface BehsazRecordQuery {
  tInfBuildingId?: number;
  tBasPeopleGroupId?: 1 | 2;
  instaurationArea?: number;
  instaurationZone?: number;
  instaurationBlock?: number;
  instaurationLand?: number;
  manualNumber?: string;
  number?: string;
  pageSize: number;
  pageIndex: number;
}

export interface SystemSettingsResponse {
  behsazIntegration: {
    isEnabled: boolean;
    userCode: number;
  };
}

export interface BehsazSearchRow {
  zoningCode: string;
  systemNumber: string;
  fileNumber: string;
  archiveType: string;
  status: string;
  taxpayerGroup: string;
  taxpayerDetails: string;
  taxpayerKey?: string;
  firstName?: string;
  lastName?: string;
  nationalCode?: string;
  companyName?: string;
  nationalCompanyId?: string;
  registerNumber?: string;
  user: string;
  registrationPlate: string;
  address: string;
  postalCode?: string;
  phone: string;
}

export interface BehsazSearchRequest {
  page?: number;
  pageSize: number;
  pageIndex: number;
  userCode?: number;
  instaurationFilterType: number;
  area?: number;
  zone?: number;
  block?: number;
  land?: number;
  instaurationArea?: number;
  instaurationZone?: number;
  instaurationBlock?: number;
  instaurationLand?: number;
  instaurationBuilding?: number;
  instaurationApartment?: number;
  instaurationBusiness?: number;
  systemNumber?: string;
  fileNumber?: string;
  ghateCode?: number;
  ghate?: number;
  registrationPlateMain?: number;
  registrationPlateSub?: number;
  mainPlate?: number;
  subPlate?: number;
  address?: string;
  propertyStatus?: number;
  gisIndicated?: boolean;
  taxpayerGroup?: 1 | 2;
  firstName?: string;
  lastName?: string;
  nationalCode?: string;
  companyName?: string;
  nationalCompanyId?: string;
  categoryId?: number;
}

export interface UploadTask {
  id: string;
  caseId: number;
  fileName: string;
  progress: number;
  status: 'uploading' | 'paused' | 'completed' | 'error' | 'cancelled';
  sessionId?: string;
}

export interface GlobalFilters {
  globalSearch: string;
  ownerName: string;
  fromDate: string;
  toDate: string;
  tags: number[];
}
