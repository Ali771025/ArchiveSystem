import { api } from "@/services/api";
import { canUseOfflineFallback } from "@/services/errorMapper";
import { mockDb } from "@/services/mockDb";
import { fileApi } from "@/services/fileApi";
import { AxiosError } from "axios";
import { normalizeOwnerNames } from "@/utils/ownerDisplay";
import type {
  AdminUserDto,
  ArchiveFile,
  ArchiveTag,
  AuditRecord,
  BehsazRecord,
  BehsazRecordQuery,
  BehsazSearchRequest,
  BehsazSearchRow,
  BehsazQuery,
  BehsazResult,
  CaseDetails,
  DynamicFieldDefinition,
  CaseDynamicValue,
  CaseSearchRequest,
  CaseSummary,
  ClassificationAclEntryDto,
  ClassificationNode,
  DynamicTemplate,
  ExportJob,
  FileVersion,
  PaginatedResponse,
  PermissionDto,
  RoleDto,
  SystemSettingsResponse,
  TagAclEntryDto,
} from "@/types/archive";
import { buildClassificationTitleMap } from "@/utils/classificationPath";

interface ClassificationNodeApi {
  id: number;
  title: string;
  parentId?: number | null;
  path?: string;
  level?: number;
  caseCount?: number;
  dossierCount?: number;
  isActive?: boolean;
  systemKey?: string | null;
  children?: ClassificationNodeApi[];
}

const STAGNANT_ARCHIVE_TITLE = "بایگانی راکد";
const STAGNANT_ARCHIVE_SYSTEM_KEY = "ArchivedCases";

const resolveClassificationSystemKey = (
  node: ClassificationNodeApi,
): string | null => {
  if (node.systemKey) {
    return node.systemKey;
  }

  return node.title.trim() === STAGNANT_ARCHIVE_TITLE
    ? STAGNANT_ARCHIVE_SYSTEM_KEY
    : null;
};

interface CaseSearchApiItem {
  id: number;
  caseNumber: string;
  title: string;
  avatarImagePath?: string | null;
  address?: string;
  externalCaseNumber?: string;
  ownerNames?: unknown;
  classificationId: number;
  createdByUserId?: number | null;
  createdByUserName?: string | null;
  loanStatus?: CaseSummary["loanStatus"];
  fileCount?: number;
  createdAt: string;
  modifiedAt?: string;
  closedAt?: string;
}

interface CaseSearchApiResponse {
  items: CaseSearchApiItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

interface FileListApiItem {
  id: number;
  name: string;
  size: number;
  extension: string;
  contentType?: string;
  createdAt: string;
  modifiedAt?: string;
  owner?: string;
  caseId?: number;
  tags?: number[];
  latestVersion?: number;
  latestVersionNumber?: number;
  status?: number | string;
  thumbnailStatus?: number;
  ocrStatus?: number;
  ocrError?: string | null;
}

interface FileListApiResponse {
  items: FileListApiItem[];
  totalCount: number;
}

export interface OcrFieldPayload {
  key: string;
  label: string;
  value: string;
  normalizedValue: string;
  confidence: number;
  isValid: boolean;
  pageNumber: number;
  validationMessage?: string | null;
}

export interface OcrPagePayload {
  pageNumber: number;
  confidence: number;
  width?: number;
  height?: number;
  text: string;
  lines?: string[];
}

export interface OcrPayload {
  engine?: string;
  language?: string;
  detectedLanguages?: string[];
  confidence?: number;
  wordCount?: number;
  processingDurationMs?: number;
  pageCount?: number;
  hasText?: boolean;
  documentType?: "NationalIdCard" | "BirthCertificate" | "Generic" | string;
  documentConfidence?: number;
  warnings?: string[];
  fields?: OcrFieldPayload[];
  pages?: OcrPagePayload[];
  text?: string;
}

interface CaseOwnerApi {
  id: number;
  fullName: string;
  nationalCode: string;
  taxpayerId?: string | null;
  phone: string;
}

interface CaseHistoryApi {
  id: number;
  timestamp: string;
  actorName: string;
  actionType: string;
  entityType: string;
  detail: string;
}

interface CaseDetailsApi {
  id: number;
  caseNumber: string;
  externalCaseNumber?: string | null;
  avatarImagePath?: string | null;
  address: string;
  postalCode?: string | null;
  updatedAt: string;
  classificationId: number;
  classificationTitle: string;
  ownerNames: unknown;
  tags: string[];
  owners: CaseOwnerApi[];
  history: CaseHistoryApi[];
}

interface FileVersionApi {
  versionId: number;
  versionNumber: number;
  size: number;
  contentType: string;
  sha256: string;
  storagePath: string;
  fileName: string;
  isDeleted: boolean;
  thumbnailStatus: number;
  ocrStatus: number;
  ocrError?: string | null;
  createdAt: string;
  deletedAt?: string | null;
}

interface BehsazSearchResponse {
  enabled: boolean;
  items: BehsazResult[];
}

interface DossierAvatarUploadResponse {
  avatarImagePath: string;
  thumbnailImagePath: string;
}

interface DynamicTemplateApiOption {
  id: string;
  value: string;
  label: string;
  order: number;
  isActive: boolean;
}

interface DynamicTemplateApiField {
  id: string;
  key: string;
  label: string;
  fieldType: number;
  isRequired: boolean;
  minNumber?: number | null;
  maxNumber?: number | null;
  minLength?: number | null;
  maxLength?: number | null;
  regex?: string | null;
  errorMessage?: string | null;
  isMultiSelect: boolean;
  showAsFilter?: boolean;
  filterType?: number;
  order: number;
  isActive: boolean;
  options: DynamicTemplateApiOption[];
}

interface DynamicTemplateApiGroup {
  id: string;
  title: string;
  order: number;
  fields: DynamicTemplateApiField[];
}

interface DynamicTemplateApi {
  id: string;
  classificationId: number;
  name: string;
  isActive: boolean;
  groups: DynamicTemplateApiGroup[];
}

interface CaseDynamicValueApi {
  fieldId: string;
  key: string;
  label: string;
  fieldType: number;
  isRequired: boolean;
  isMultiSelect: boolean;
  stringValue?: string | null;
  numberValue?: number | null;
  dateValue?: string | null;
  boolValue?: boolean | null;
  optionIds: string[];
}

interface CaseDynamicFieldsApiResponse {
  caseId: number;
  classificationId: number;
  templateId: string;
  values: CaseDynamicValueApi[];
}

interface ApiResultEnvelope<T> {
  success: boolean;
  errorCode?: string;
  message: string;
  data?: T | null;
  errors?: Record<string, string[]>;
}

const unwrapApiResult = <T>(payload: ApiResultEnvelope<T>): T => {
  if (payload.success && payload.data !== undefined && payload.data !== null) {
    return payload.data;
  }

  throw new Error(
    payload.message || payload.errorCode || "API operation failed.",
  );
};

const apiFirst = async <T>(
  onlineCall: () => Promise<T>,
  offlineCall: () => Promise<T>,
): Promise<T> => {
  try {
    return await onlineCall();
  } catch (error) {
    if (!canUseOfflineFallback(error)) {
      throw error;
    }

    return offlineCall();
  }
};

const mapCaseRow = (
  item: CaseSearchApiItem,
  classificationMap: Map<number, string>,
): CaseSummary => ({
  id: item.id,
  caseNumber: item.caseNumber,
  externalCaseNumber: item.externalCaseNumber ?? "",
  avatarImagePath: item.avatarImagePath ?? null,
  address: item.address ?? item.title ?? "",
  updatedAt: item.modifiedAt ?? item.createdAt,
  classificationId: item.classificationId,
  classificationTitle: classificationMap.get(item.classificationId) ?? "نامشخص",
  createdByUserId: item.createdByUserId ?? null,
  createdByUserName: item.createdByUserName ?? null,
  loanStatus: item.loanStatus ?? null,
  fileCount: item.fileCount ?? 0,
  ownerNames: normalizeOwnerNames(item.ownerNames),
  tags: [],
});

const normalizeFileStatus = (status?: number | string): number => {
  if (typeof status === "number") {
    return status;
  }

  const statusByName: Record<string, number> = {
    pending: 0,
    ready: 1,
    archived: 2,
    deleted: 3,
  };

  return statusByName[status?.trim().toLowerCase() ?? ""] ?? 1;
};

const mapFileRow = (item: FileListApiItem): ArchiveFile => ({
  id: item.id,
  caseId: item.caseId ?? 0,
  name: item.name,
  extension: item.extension,
  contentType: item.contentType ?? "application/octet-stream",
  size: item.size,
  uploader: item.owner ?? "system",
  uploadedAt: item.createdAt,
  latestVersion: item.latestVersionNumber ?? item.latestVersion ?? 1,
  status: normalizeFileStatus(item.status),
  tags: item.tags ?? [],
  ocrError: item.ocrError,
  processing: {
    ocr:
      item.ocrStatus === 2
        ? "Completed"
        : item.ocrStatus === 3
          ? "Failed"
          : item.ocrStatus === 1
            ? "Processing"
            : "Pending",
    thumbnail:
      item.thumbnailStatus === 2
        ? "Completed"
        : item.thumbnailStatus === 3
          ? "Failed"
          : "Pending",
  },
});

const EXPORT_JOB_IDS_STORAGE_KEY = "archive:export-job-ids";

const normalizeJob = (job: ExportJob): ExportJob => {
  if (typeof job.status === "string") {
    return { ...job, status: job.status.toLowerCase() as ExportJob["status"] };
  }

  const statusMap: Record<number, ExportJob["status"]> = {
    1: "pending",
    2: "running",
    3: "completed",
    4: "failed",
    5: "expired",
  };

  return { ...job, status: statusMap[job.status] ?? "running" };
};

const readExportJobIds = (): string[] => {
  if (typeof window === "undefined") {
    return [];
  }

  try {
    const parsed = JSON.parse(
      window.localStorage.getItem(EXPORT_JOB_IDS_STORAGE_KEY) ?? "[]",
    );
    return Array.isArray(parsed)
      ? parsed.filter(
          (id): id is string => typeof id === "string" && id.length > 0,
        )
      : [];
  } catch {
    return [];
  }
};

const rememberExportJobId = (jobId: string) => {
  if (typeof window === "undefined") {
    return;
  }

  const ids = [jobId, ...readExportJobIds().filter((id) => id !== jobId)].slice(
    0,
    20,
  );
  window.localStorage.setItem(EXPORT_JOB_IDS_STORAGE_KEY, JSON.stringify(ids));
};

const saveBlob = (blob: Blob, fileName: string) => {
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement("a");
  anchor.href = url;
  anchor.download = fileName;
  document.body.appendChild(anchor);
  anchor.click();
  anchor.remove();
  URL.revokeObjectURL(url);
};

const resolveDownloadFileName = (
  contentDisposition: string | undefined,
  fallback: string,
): string => {
  if (!contentDisposition) {
    return fallback;
  }

  const utf8Match = /filename\*=UTF-8''([^;]+)/i.exec(contentDisposition);
  if (utf8Match?.[1]) {
    return decodeURIComponent(utf8Match[1]);
  }

  const match = /filename="?([^";]+)"?/i.exec(contentDisposition);
  return match?.[1] ?? fallback;
};

const mapApiFieldTypeToFrontend = (
  fieldType: number,
): DynamicFieldDefinition["type"] => {
  switch (fieldType) {
    case 9:
      return "checkbox";
    case 8:
      return "multiselect";
    case 7:
      return "textarea";
    case 6:
      return "decimal";
    case 2:
      return "decimal";
    case 3:
      return "date";
    case 4:
      return "checkbox";
    case 5:
      return "dropdown";
    default:
      return "text";
  }
};

const mapFrontendFieldTypeToApi = (
  type: DynamicFieldDefinition["type"],
): number => {
  switch (type) {
    case "int":
    case "decimal":
      return 2;
    case "date":
    case "datetime":
    case "time":
      return 3;
    case "checkbox":
      return 4;
    case "multiselect":
      return 8;
    case "dropdown":
      return 5;
    case "textarea":
    case "text":
    default:
      return 1;
  }
};

const mapApiFilterTypeToFrontend = (
  filterType?: number,
): DynamicFieldDefinition["filterType"] => {
  switch (filterType) {
    case 2:
      return "equals";
    case 3:
      return "notEquals";
    case 4:
      return "lessThan";
    case 5:
      return "greaterThan";
    case 6:
      return "lessThanOrEqual";
    case 7:
      return "greaterThanOrEqual";
    case 8:
      return "range";
    case 1:
    default:
      return "contains";
  }
};

const mapFrontendFilterTypeToApi = (
  filterType: DynamicFieldDefinition["filterType"],
): number => {
  switch (filterType) {
    case "equals":
      return 2;
    case "notEquals":
      return 3;
    case "lessThan":
      return 4;
    case "greaterThan":
      return 5;
    case "lessThanOrEqual":
      return 6;
    case "greaterThanOrEqual":
      return 7;
    case "range":
      return 8;
    case "contains":
    default:
      return 1;
  }
};

const mapApiTemplateToFrontend = (
  template: DynamicTemplateApi,
): DynamicTemplate => ({
  id: template.id,
  classificationId: template.classificationId,
  name: template.name,
  isActive: template.isActive,
  version: 1,
  publishedAt: new Date().toISOString(),
  groups: (template.groups ?? [])
    .slice()
    .sort((a, b) => a.order - b.order)
    .map((group) => ({
      id: group.id,
      title: group.title,
      apiName: group.title,
      order: group.order,
      fields: (group.fields ?? [])
        .slice()
        .sort((a, b) => a.order - b.order)
        .map((field) => ({
          id: field.id,
          apiName: field.key,
          label: field.label,
          type: mapApiFieldTypeToFrontend(field.fieldType),
          required: field.isRequired,
          showAsFilter: Boolean(field.showAsFilter),
          filterType: mapApiFilterTypeToFrontend(field.filterType),
          order: field.order,
          options: (field.options ?? [])
            .slice()
            .sort((a, b) => a.order - b.order)
            .map((option) => ({
              id: option.id,
              value: option.value,
              label: option.label,
            })),
          rules: {
            regex: field.regex ?? undefined,
            errorMessage: field.errorMessage ?? undefined,
            min: field.minNumber ?? undefined,
            max: field.maxNumber ?? undefined,
          },
        })),
    })),
});

const mapFrontendTemplateToApi = (
  classificationId: number,
  template: DynamicTemplate,
): DynamicTemplateApi => ({
  id: template.id,
  classificationId,
  name: template.name?.trim() || `Template ${classificationId}`,
  isActive: template.isActive ?? true,
  groups: (template.groups ?? []).map((group, groupIndex) => ({
    id: group.id,
    title: group.title,
    order: group.order ?? groupIndex,
    fields: (group.fields ?? []).map((field, fieldIndex) => {
      const fieldType = mapFrontendFieldTypeToApi(field.type);
      return {
        id: field.id,
        key: field.apiName?.trim() || `field_${fieldIndex + 1}`,
        label: field.label,
        fieldType,
        isRequired: field.required,
        minNumber: field.rules?.min ?? null,
        maxNumber: field.rules?.max ?? null,
        minLength: null,
        maxLength: null,
        regex: field.rules?.regex ?? null,
        errorMessage: field.rules?.errorMessage?.trim() || null,
        isMultiSelect: field.type === "multiselect",
        showAsFilter: field.showAsFilter ?? false,
        filterType: mapFrontendFilterTypeToApi(field.filterType ?? "contains"),
        order: field.order ?? fieldIndex,
        isActive: true,
        options:
          fieldType === 5 || fieldType === 8
            ? (field.options ?? []).map((option, optionIndex) => ({
                id: option.id,
                value: option.value,
                label: option.label,
                order: optionIndex,
                isActive: true,
              }))
            : [],
      };
    }),
  })),
});

const mapApiDynamicValueToFrontend = (
  value: CaseDynamicValueApi,
): CaseDynamicValue => ({
  fieldId: value.fieldId,
  fieldType: mapApiFieldTypeToFrontend(value.fieldType),
  value:
    value.boolValue ??
    value.numberValue ??
    value.dateValue ??
    value.stringValue ??
    null,
  values: value.optionIds ?? [],
});

const isDateLikeDynamicValue = (value: CaseDynamicValue): boolean =>
  value.fieldType === "date" ||
  value.fieldType === "datetime" ||
  value.fieldType === "time";

const mapFrontendDynamicValueToApi = (value: CaseDynamicValue) => {
  const isOptionValue = value.values.length > 0;
  const isDateValue = isDateLikeDynamicValue(value);

  return {
    fieldId: value.fieldId,
    stringValue:
      !isDateValue && !isOptionValue && typeof value.value === "string"
        ? value.value
        : null,
    numberValue: typeof value.value === "number" ? value.value : null,
    boolValue: typeof value.value === "boolean" ? value.value : null,
    optionIds: value.values,
    dateValue:
      isDateValue && typeof value.value === "string" && value.value.trim()
        ? value.value
        : null,
  };
};

const getCaseDynamicValuesFromApi = async (
  caseId: number,
): Promise<CaseDynamicValue[]> => {
  const response = await api.get<
    | ApiResultEnvelope<CaseDynamicFieldsApiResponse>
    | CaseDynamicFieldsApiResponse
  >(`/cases/${caseId}/dynamic-fields`);

  const payload =
    "success" in response.data ? unwrapApiResult(response.data) : response.data;

  return (payload.values ?? []).map(mapApiDynamicValueToFrontend);
};

const fetchFileContentBlob = async (
  fileId: number,
  versionId?: number,
): Promise<Blob> =>
  apiFirst(
    async () => {
      const route = versionId
        ? `/files/${fileId}/versions/${versionId}/download`
        : `/files/${fileId}/download`;
      const response = await api.get<Blob>(route, { responseType: "blob" });
      return response.data;
    },
    async () =>
      new Blob([`Offline file ${fileId}`], {
        type: "text/plain",
      }),
  );

const isConfirmedDynamicValue = (
  expected: CaseDynamicValue,
  actual: CaseDynamicValue | undefined,
): boolean => {
  if (expected.values.length > 0) {
    const expectedOptions = [...expected.values].sort();
    const actualOptions = [...(actual?.values ?? [])].sort();
    return (
      expectedOptions.length === actualOptions.length &&
      expectedOptions.every(
        (optionId, index) => optionId === actualOptions[index],
      )
    );
  }

  if (expected.value === null || expected.value === "") {
    return true;
  }

  if (expected.fieldType === "date") {
    return (
      String(actual?.value ?? "").slice(0, 10) ===
      String(expected.value).slice(0, 10)
    );
  }

  if (expected.fieldType === "datetime" || expected.fieldType === "time") {
    const expectedTime = Date.parse(String(expected.value));
    const actualTime = Date.parse(String(actual?.value ?? ""));
    return Number.isFinite(expectedTime) && expectedTime === actualTime;
  }

  if (typeof expected.value === "string") {
    return String(actual?.value ?? "").trim() === expected.value.trim();
  }

  return actual?.value === expected.value;
};

const flattenClassificationTree = (
  nodes: ClassificationNodeApi[],
  parentId: number | null = null,
  level = 0,
): ClassificationNode[] => {
  const flat: ClassificationNode[] = [];

  nodes.forEach((node) => {
    const normalizedParentId = node.parentId ?? parentId;
    const normalizedPath =
      node.path ??
      `/${normalizedParentId == null ? "" : `${normalizedParentId}/`}${node.id}/`;
    const children = node.children ?? [];

    flat.push({
      id: node.id,
      title: node.title,
      parentId: normalizedParentId,
      path: normalizedPath,
      level: node.level ?? level,
      caseCount: node.dossierCount ?? node.caseCount ?? 0,
      isActive: node.isActive ?? true,
      systemKey: resolveClassificationSystemKey(node),
    });

    if (children.length > 0) {
      flat.push(...flattenClassificationTree(children, node.id, level + 1));
    }
  });

  return flat;
};

export const archiveApi = {
  async getClassificationTree(): Promise<ClassificationNode[]> {
    return apiFirst(
      async () => {
        const response = await api.get<ClassificationNodeApi[]>(
          "/classifications/tree",
        );
        return flattenClassificationTree(response.data);
      },
      () => mockDb.getClassificationTree(),
    );
  },

  async getWritableClassificationIds(): Promise<number[]> {
    return apiFirst(
      async () => {
        const response = await api.get<number[]>(
          "/classifications/writable-ids",
        );
        return response.data;
      },
      () => mockDb.getWritableClassificationIds(),
    );
  },

  async getCaseMoveAccessIds(): Promise<number[]> {
    return apiFirst(
      async () => {
        const response = await api.get<number[]>(
          "/classifications/movable-ids",
        );
        return response.data;
      },
      () => mockDb.getCaseMoveAccessIds(),
    );
  },

  async createClassification(
    title: string,
    parentId: number | null,
  ): Promise<void> {
    await apiFirst(
      async () => {
        await api.post("/classifications", { title, parentId });
      },
      async () => {
        await mockDb.createClassification(title, parentId);
      },
    );
  },

  async renameClassification(id: number, title: string): Promise<void> {
    await apiFirst(
      async () => {
        await api.post(`/classifications/${id}/rename`, { title });
      },
      async () => {
        await mockDb.renameClassification(id, title);
      },
    );
  },

  async moveClassification(
    id: number,
    newParentId: number | null,
  ): Promise<void> {
    await apiFirst(
      async () => {
        await api.put(`/classifications/${id}/move`, { newParentId });
      },
      async () => {
        await mockDb.moveClassification(id, newParentId);
      },
    );
  },

  async deleteClassification(id: number): Promise<void> {
    await apiFirst(
      async () => {
        await api.delete(`/classifications/${id}`);
      },
      async () => {
        await mockDb.deleteClassification(id);
      },
    );
  },

  async searchCases(
    request: CaseSearchRequest,
  ): Promise<PaginatedResponse<CaseSummary>> {
    return apiFirst(
      async () => {
        const classificationTree = await this.getClassificationTree();
        const classificationMap = buildClassificationTitleMap(classificationTree);

        const response = await api.post<
          ApiResultEnvelope<CaseSearchApiResponse>
        >("/cases/search", {
          classificationId: request.classificationId,
          page: request.page,
          pageSize: request.pageSize,
          sortBy: request.sortBy,
          sortDirection: request.sortDirection,
          ownerName: request.ownerName?.trim() || undefined,
          externalCaseNumber: request.externalCaseNumber?.trim() || undefined,
          createdBy: request.createdBy?.trim() || undefined,
          fromDate: request.fromDate || undefined,
          toDate: request.toDate || undefined,
          tags:
            request.tags && request.tags.length > 0 ? request.tags : undefined,
          dynamicFilters: [],
          dynamicFilterExpression: request.dynamicFilterExpression,
        });

        const payload = unwrapApiResult(response.data);
        return {
          items: payload.items.map((item) =>
            mapCaseRow(item, classificationMap),
          ),
          totalCount: payload.totalCount,
          page: payload.page,
          pageSize: payload.pageSize,
          totalPages: Math.max(
            1,
            Math.ceil(payload.totalCount / payload.pageSize),
          ),
        };
      },
      () => mockDb.searchCases(request),
    );
  },

  async getCaseDetails(caseId: number): Promise<CaseDetails | null> {
    return apiFirst<CaseDetails | null>(
      async () => {
        const response = await api.get<ApiResultEnvelope<CaseDetailsApi>>(
          `/cases/${caseId}`,
        );
        const payload = unwrapApiResult(response.data);
        return {
          ...payload,
          externalCaseNumber: payload.externalCaseNumber ?? "",
          avatarImagePath: payload.avatarImagePath ?? null,
          history: payload.history ?? [],
          owners: payload.owners ?? [],
          ownerNames: normalizeOwnerNames(payload.ownerNames ?? payload.owners),
          tags: payload.tags ?? [],
        };
      },
      () => mockDb.getCaseDetails(caseId),
    );
  },

  async downloadCaseWorkbook(
    caseId: number,
    caseNumber: string,
  ): Promise<void> {
    const response = await api.get<Blob>(`/cases/${caseId}/export/excel`, {
      responseType: "blob",
    });
    const fileName = resolveDownloadFileName(
      response.headers["content-disposition"] as string | undefined,
      `case-${caseNumber || caseId}.xlsx`,
    );
    saveBlob(response.data, fileName);
  },

  async getCaseDetailsByNumber(
    caseNumber: string,
  ): Promise<CaseDetails | null> {
    const normalizedCaseNumber = caseNumber.trim();
    return apiFirst<CaseDetails | null>(
      async () => {
        const response = await api.get<ApiResultEnvelope<CaseDetailsApi>>(
          `/cases/by-number/${encodeURIComponent(normalizedCaseNumber)}`,
        );
        const payload = unwrapApiResult(response.data);
        return {
          ...payload,
          externalCaseNumber: payload.externalCaseNumber ?? "",
          avatarImagePath: payload.avatarImagePath ?? null,
          history: payload.history ?? [],
          owners: payload.owners ?? [],
          ownerNames: normalizeOwnerNames(payload.ownerNames ?? payload.owners),
          tags: payload.tags ?? [],
        };
      },
      () => mockDb.getCaseDetailsByNumber(normalizedCaseNumber),
    );
  },

  async createCase(
    payload: Pick<
      CaseDetails,
      "classificationId" | "address" | "externalCaseNumber" | "postalCode"
    >,
  ): Promise<CaseDetails> {
    return apiFirst<CaseDetails>(
      async () => {
        const response = await api.post<ApiResultEnvelope<CaseDetailsApi>>(
          "/cases",
          {
            classificationId: payload.classificationId,
            externalCaseNumber: payload.externalCaseNumber,
            address: payload.address,
            postalCode: payload.postalCode ?? null,
            owners: [],
          },
        );
        const result = unwrapApiResult(response.data);
        return {
          ...result,
          externalCaseNumber: result.externalCaseNumber ?? "",
          avatarImagePath: result.avatarImagePath ?? null,
          history: result.history ?? [],
          owners: result.owners ?? [],
          ownerNames: normalizeOwnerNames(result.ownerNames ?? result.owners),
          tags: result.tags ?? [],
        };
      },
      () => mockDb.createCase(payload),
    );
  },

  async updateCase(payload: CaseDetails): Promise<CaseDetails> {
    return apiFirst<CaseDetails>(
      async () => {
        const response = await api.post<ApiResultEnvelope<CaseDetailsApi>>(
          `/cases/${payload.id}/update`,
          {
            classificationId: payload.classificationId,
            externalCaseNumber: payload.externalCaseNumber,
            address: payload.address,
            postalCode: payload.postalCode ?? null,
            owners: payload.owners ?? [],
          },
        );
        const result = unwrapApiResult(response.data);
        return {
          ...result,
          externalCaseNumber: result.externalCaseNumber ?? "",
          avatarImagePath: result.avatarImagePath ?? null,
          history: result.history ?? [],
          owners: result.owners ?? [],
          ownerNames: normalizeOwnerNames(result.ownerNames ?? result.owners),
          tags: result.tags ?? [],
        };
      },
      () => mockDb.updateCase(payload),
    );
  },

  async moveCase(
    caseId: number,
    classificationId: number,
    sourceClassificationId?: number,
  ): Promise<void> {
    return apiFirst(
      async () => {
        const response = await api.post<ApiResultEnvelope<{ moved: boolean }>>(
          `/cases/${caseId}/move`,
          {
            sourceClassificationId,
            classificationId,
          },
        );
        unwrapApiResult(response.data);
      },
      () => mockDb.moveCase(caseId, classificationId),
    );
  },

  async deleteCase(caseId: number): Promise<void> {
    return apiFirst(
      async () => {
        const response = await api.delete<
          ApiResultEnvelope<{
            archived: boolean;
            caseId: number;
            sourceClassificationId: number;
            archiveClassificationId: number;
          }>
        >(`/cases/${caseId}`);
        unwrapApiResult(response.data);
      },
      () => Promise.resolve(),
    );
  },

  async uploadCaseAvatar(
    caseId: number,
    file: File,
  ): Promise<DossierAvatarUploadResponse> {
    return apiFirst(
      async () => {
        const formData = new FormData();
        formData.append("file", file);
        const response = await api.post<DossierAvatarUploadResponse>(
          `/v1/dossiers/${caseId}/avatar`,
          formData,
          {
            headers: { "Content-Type": "multipart/form-data" },
          },
        );
        return response.data;
      },
      () => mockDb.uploadCaseAvatar(caseId, file.name),
    );
  },

  async getTemplate(classificationId: number): Promise<DynamicTemplate | null> {
    return apiFirst(
      async () => {
        const response = await api.get<DynamicTemplateApi | null>(
          `/classifications/${classificationId}/dynamic-template`,
        );
        return response.data ? mapApiTemplateToFrontend(response.data) : null;
      },
      () => mockDb.getTemplate(classificationId),
    );
  },

  async getEffectiveTemplate(classificationId: number): Promise<DynamicTemplate | null> {
    const response = await api.get<DynamicTemplateApi | null>(
      `/classifications/${classificationId}/effective-dynamic-template`,
    );
    return response.data ? mapApiTemplateToFrontend(response.data) : null;
  },

  async saveTemplate(
    classificationId: number,
    template: DynamicTemplate,
  ): Promise<DynamicTemplate> {
    return apiFirst(
      async () => {
        const payload = mapFrontendTemplateToApi(classificationId, template);
        const response = await api.post<DynamicTemplateApi>(
          `/classifications/${classificationId}/dynamic-template`,
          payload,
        );
        return mapApiTemplateToFrontend(response.data);
      },
      () => mockDb.saveTemplate(classificationId, template),
    );
  },

  async getCaseDynamicValues(caseId: number): Promise<CaseDynamicValue[]> {
    return getCaseDynamicValuesFromApi(caseId);
  },

  async saveCaseDynamicValues(
    caseId: number,
    values: CaseDynamicValue[],
  ): Promise<CaseDynamicValue[]> {
    const response = await api.put<
      | ApiResultEnvelope<CaseDynamicFieldsApiResponse>
      | CaseDynamicFieldsApiResponse
    >(`/cases/${caseId}/dynamic-fields`, {
      values: values.map(mapFrontendDynamicValueToApi),
    });

    if ("success" in response.data) {
      unwrapApiResult(response.data);
    }

    // Confirm persistence through a separate request before reporting success.
    const confirmedValues = await getCaseDynamicValuesFromApi(caseId);
    const confirmedByFieldId = new Map(
      confirmedValues.map((value) => [value.fieldId, value]),
    );
    const unconfirmedValue = values.find(
      (value) =>
        !isConfirmedDynamicValue(value, confirmedByFieldId.get(value.fieldId)),
    );

    if (unconfirmedValue) {
      throw new Error(
        "ذخیره فیلدهای پویا توسط سرور تأیید نشد. لطفاً دوباره تلاش کنید.",
      );
    }

    return confirmedValues;
  },

  async getCaseFiles(caseId: number): Promise<ArchiveFile[]> {
    return apiFirst(
      async () => {
        const response = await api.get<FileListApiResponse>("/files", {
          params: { caseId, page: 1, pageSize: 100 },
        });

        return response.data.items
          .map(mapFileRow)
          .filter((item) => item.caseId === caseId);
      },
      () => mockDb.getCaseFiles(caseId),
    );
  },

  async uploadCaseFile(
    caseId: number,
    file: File,
    onProgress: (progress: number) => void,
    options?: { classificationId?: number; fileId?: number },
  ) {
    return await apiFirst(
      async () => {
        return await fileApi.uploadFile(file, onProgress, {
          caseId,
          classificationId: options?.classificationId,
          fileId: options?.fileId,
        });
      },
      async () => {
        let progress = 0;
        while (progress < 100) {
          progress += 15;
          onProgress(Math.min(100, progress));
          await new Promise((resolve) => setTimeout(resolve, 100));
        }
        await mockDb.uploadCaseFile(caseId, file.name, file.size);
        return undefined;
      },
    );
  },

  async getFileVersions(fileId: number): Promise<FileVersion[]> {
    return apiFirst(
      async () => {
        const response = await api.get<FileVersionApi[]>(
          `/files/${fileId}/versions`,
        );
        return response.data.map((version) => ({
          id: version.versionId,
          versionNumber: version.versionNumber,
          size: version.size,
          uploadedAt: version.createdAt,
          uploader: "system",
          ocrStatus:
            version.ocrStatus === 2
              ? "Completed"
              : version.ocrStatus === 3
                ? "Failed"
                : version.ocrStatus === 1
                  ? "Processing"
                  : "Pending",
          ocrError: version.ocrError,
          thumbnailStatus:
            version.thumbnailStatus === 2
              ? "Completed"
              : version.thumbnailStatus === 3
                ? "Failed"
                : "Pending",
          ocrWordCount: 0,
          ocrLanguage: "",
          ocrTextPreview: "",
        }));
      },
      () => mockDb.getFileVersions(fileId),
    );
  },

  async deleteFile(fileId: number): Promise<void> {
    await apiFirst(
      async () => {
        await api.delete(`/files/${fileId}`);
      },
      () => mockDb.softDeleteFile(fileId),
    );
  },

  async updateFileTags(fileId: number, tagIds: number[]): Promise<void> {
    await apiFirst(
      async () => {
        await api.put(`/files/${fileId}/tags`, { tagIds });
      },
      () => mockDb.setFileTags(fileId, tagIds),
    );
  },

  async getLatestOcr(fileId: number): Promise<OcrPayload | null> {
    return apiFirst(
      async () => {
        try {
          const response = await api.get<OcrPayload | string>(
            `/files/${fileId}/latest/ocr`,
          );
          if (typeof response.data === "string") {
            return JSON.parse(response.data) as OcrPayload;
          }

          return response.data;
        } catch (error) {
          if (error instanceof AxiosError && error.response?.status === 404) {
            return null;
          }

          throw error;
        }
      },
      async () => {
        const versions = await mockDb.getFileVersions(fileId);
        const latest = versions[versions.length - 1];
        return {
          language: latest?.ocrLanguage,
          wordCount: latest?.ocrWordCount,
          text: latest?.ocrTextPreview,
        };
      },
    );
  },

  async getVersionOcr(
    fileId: number,
    versionId: number,
  ): Promise<OcrPayload | null> {
    return apiFirst(
      async () => {
        try {
          const response = await api.get<OcrPayload | string>(
            `/files/${fileId}/versions/${versionId}/ocr`,
          );
          if (typeof response.data === "string") {
            return JSON.parse(response.data) as OcrPayload;
          }

          return response.data;
        } catch (error) {
          if (error instanceof AxiosError && error.response?.status === 404) {
            return null;
          }

          throw error;
        }
      },
      async () => {
        const versions = await mockDb.getFileVersions(fileId);
        const current = versions.find((item) => item.id === versionId);
        return {
          language: current?.ocrLanguage,
          wordCount: current?.ocrWordCount,
          text: current?.ocrTextPreview,
        };
      },
    );
  },

  async retryVersionOcr(fileId: number, versionId: number): Promise<void> {
    await apiFirst(
      async () => {
        await api.post(`/files/${fileId}/versions/${versionId}/ocr/retry`);
      },
      async () => undefined,
    );
  },

  async getLatestThumbnail(fileId: number): Promise<string | null> {
    return apiFirst(
      async () => {
        try {
          const response = await api.get<Blob>(
            `/files/${fileId}/latest/thumbnail`,
            { responseType: "blob" },
          );
          return URL.createObjectURL(response.data);
        } catch (error) {
          if (error instanceof AxiosError && error.response?.status === 404) {
            return null;
          }

          throw error;
        }
      },
      async () => null,
    );
  },

  async getVersionThumbnail(
    fileId: number,
    versionId: number,
  ): Promise<string | null> {
    return apiFirst(
      async () => {
        try {
          const response = await api.get<Blob>(
            `/files/${fileId}/versions/${versionId}/thumbnail`,
            {
              responseType: "blob",
            },
          );
          return URL.createObjectURL(response.data);
        } catch (error) {
          if (error instanceof AxiosError && error.response?.status === 404) {
            return null;
          }

          throw error;
        }
      },
      async () => null,
    );
  },

  async downloadFile(fileId: number, versionId?: number): Promise<string> {
    const blob = await fetchFileContentBlob(fileId, versionId);
    return URL.createObjectURL(blob);
  },

  async getFileContent(fileId: number, versionId?: number): Promise<Blob> {
    return fetchFileContentBlob(fileId, versionId);
  },

  async getTags(): Promise<ArchiveTag[]> {
    return apiFirst(
      async () => {
        const response = await api.get<ArchiveTag[]>("/admin/tags");
        return response.data;
      },
      () => mockDb.getTags(),
    );
  },

  async getAssignableTags(): Promise<ArchiveTag[]> {
    return apiFirst(
      async () => {
        const response = await api.get<ArchiveTag[]>("/files/tags/assignable");
        return response.data;
      },
      () => mockDb.getTags(),
    );
  },

  async createTag(name: string, description?: string): Promise<ArchiveTag> {
    return apiFirst<ArchiveTag>(
      async () => {
        const response = await api.post<{
          id: number;
          name: string;
          description?: string;
        }>("/admin/tags", { name, description });
        return {
          ...response.data,
          scope: "global",
        };
      },
      () => mockDb.createTag(name, description),
    );
  },

  async getRoles(): Promise<RoleDto[]> {
    return apiFirst(
      async () => {
        const response = await api.get<RoleDto[]>("/v1/roles");
        return response.data;
      },
      () => mockDb.getRoles(),
    );
  },

  async createRole(name: string): Promise<RoleDto> {
    return apiFirst(
      async () => {
        const response = await api.post<RoleDto>("/v1/roles", {
          name,
          isSystem: false,
        });
        return response.data;
      },
      () => mockDb.createRole(name),
    );
  },

  async getPermissions(): Promise<PermissionDto[]> {
    return apiFirst(
      async () => {
        const response = await api.get<PermissionDto[]>("/admin/permissions");
        return response.data;
      },
      () => mockDb.getPermissions(),
    );
  },

  async getUsers(): Promise<AdminUserDto[]> {
    return apiFirst(
      async () => {
        const response = await api.get<AdminUserDto[]>("/admin/users");
        return response.data;
      },
      () => mockDb.getUsers(),
    );
  },

  async getClassificationAcl(
    classificationId: number,
    includeDescendants = false,
  ): Promise<ClassificationAclEntryDto[]> {
    return apiFirst(
      async () => {
        const response = await api.get<ClassificationAclEntryDto[]>(
          `/admin/classifications/${classificationId}/acl`,
          {
            params: { includeDescendants },
          },
        );
        return response.data;
      },
      () => mockDb.getClassificationAcl(classificationId),
    );
  },

  async saveClassificationAcl(
    classificationId: number,
    entries: ClassificationAclEntryDto[],
  ): Promise<void> {
    await apiFirst(
      async () => {
        await api.post(`/admin/classifications/${classificationId}/acl`, {
          entries,
        });
      },
      () => mockDb.saveClassificationAcl(classificationId, entries),
    );
  },

  async getTagAcl(tagId: number): Promise<TagAclEntryDto[]> {
    return apiFirst(
      async () => {
        const response = await api.get<TagAclEntryDto[]>("/admin/tags/acl", {
          params: { tagId },
        });
        return response.data;
      },
      () => mockDb.getTagAcl(tagId),
    );
  },

  async saveTagAcl(tagId: number, entries: TagAclEntryDto[]): Promise<void> {
    await apiFirst(
      async () => {
        await api.put("/admin/tags/acl", { tagId, entries });
      },
      () => mockDb.saveTagAcl(tagId, entries),
    );
  },

  async getActivity(
    page: number,
    pageSize: number,
  ): Promise<PaginatedResponse<AuditRecord>> {
    return apiFirst(
      async () => {
        const response = await api.get<
          ApiResultEnvelope<{
            items: Array<{
              auditId: number;
              actionDate: string;
              userId?: string | null;
              userName?: string | null;
              eventType: string;
              tableName: string;
              recordId: string;
            }>;
            totalCount: number;
            pageNumber: number;
            pageSize: number;
          }>
        >("/v1/reports/audit-trail", {
          params: { pageNumber: page, pageSize, sortDirection: "desc" },
        });
        const payload = unwrapApiResult(response.data);

        return {
          items: (payload.items ?? []).map((item) => ({
            id: item.auditId,
            timestamp: item.actionDate,
            actorName:
              item.userName ||
              (item.userId && item.userId !== "0" ? item.userId : ""),
            actionType: item.eventType,
            entityType: item.tableName,
            detail: item.recordId,
          })),
          totalCount: payload.totalCount,
          page: payload.pageNumber,
          pageSize: payload.pageSize,
          totalPages: Math.max(
            1,
            Math.ceil(payload.totalCount / Math.max(1, payload.pageSize)),
          ),
        };
      },
      () => mockDb.getActivity(page, pageSize),
    );
  },

  async createActivityExport(): Promise<ExportJob> {
    return apiFirst(
      async () => {
        const response = await api.post<ExportJob>("/exports/activity/excel", {
          filter: {},
        });
        const job = normalizeJob(response.data);
        rememberExportJobId(job.id);
        return job;
      },
      () => mockDb.createExportJob("activity"),
    );
  },

  async createFilesExport(
    request: {
      caseId?: number | null;
      fileId?: number;
      classificationId?: number;
      tagId?: number;
      term?: string;
    } = {},
  ): Promise<ExportJob> {
    return apiFirst(
      async () => {
        const response = await api.post<ExportJob>("/exports/files/zip", {
          caseId: request.caseId ?? undefined,
          fileId: request.fileId,
          classificationId: request.classificationId,
          tagId: request.tagId,
          term: request.term,
        });
        const job = normalizeJob(response.data);
        rememberExportJobId(job.id);
        return job;
      },
      () => mockDb.createExportJob("files"),
    );
  },

  async getExportJob(jobId: string): Promise<ExportJob> {
    return apiFirst(
      async () => {
        const response = await api.get<ExportJob>(`/exports/jobs/${jobId}`);
        return normalizeJob(response.data);
      },
      async () => {
        const job = (await mockDb.getExportJobs()).find(
          (item) => item.id === jobId,
        );
        if (!job) {
          throw new Error("Export job not found.");
        }

        return job;
      },
    );
  },

  async getExportJobs(): Promise<ExportJob[]> {
    return apiFirst(
      async () => {
        const ids = readExportJobIds();
        const jobs = await Promise.all(
          ids.map(async (id) => {
            try {
              const response = await api.get<ExportJob>(`/exports/jobs/${id}`);
              return normalizeJob(response.data);
            } catch {
              return null;
            }
          }),
        );

        return jobs.filter((job): job is ExportJob => job != null);
      },
      () => mockDb.getExportJobs(),
    );
  },

  async downloadExportJob(jobId: string): Promise<void> {
    const response = await api.get<Blob>(`/exports/jobs/${jobId}/download`, {
      responseType: "blob",
    });
    const fileName = resolveDownloadFileName(
      response.headers["content-disposition"] as string | undefined,
      `export-${jobId}.zip`,
    );
    saveBlob(response.data, fileName);
  },

  async searchBehsaz(query: BehsazQuery): Promise<BehsazResult[]> {
    return apiFirst(
      async () => {
        const response = await api.get<PaginatedResponse<BehsazRecord>>(
          "/behsaz/records",
          {
            params: {
              number: query.text?.trim() || undefined,
              manualNumber: query.text?.trim() || undefined,
              pageSize: 50,
              pageIndex: 0,
            },
          },
        );

        return (response.data.items ?? []).map((row) => ({
          externalCaseNumber: row.number || row.manualNumber || "",
          title: row.address || row.peopleProperty || "",
          ownerName: row.userName || row.peopleGroupName || "",
          nationalCode: row.peopleKey || "",
          registeredAt: new Date().toISOString(),
        }));
      },
      () => mockDb.behsazSearch(query),
    );
  },

  async getBehsazConfig(): Promise<boolean> {
    return apiFirst(
      async () => {
        const response =
          await api.get<SystemSettingsResponse>("/system/settings");
        return Boolean(response.data.behsazIntegration?.isEnabled);
      },
      async () => isEnabled(),
    );
  },

  async getSystemSettings(): Promise<SystemSettingsResponse> {
    return apiFirst(
      async () => {
        const response =
          await api.get<SystemSettingsResponse>("/system/settings");
        return response.data;
      },
      async () => ({
        behsazIntegration: {
          isEnabled: isEnabled(),
          userCode: 1000,
        },
      }),
    );
  },

  async searchBehsazRecords(
    query: BehsazRecordQuery,
  ): Promise<PaginatedResponse<BehsazRecord>> {
    return apiFirst(
      async () => {
        const response = await api.get<PaginatedResponse<BehsazRecord>>(
          "/behsaz/records",
          {
            params: query,
          },
        );
        return response.data;
      },
      async () => ({
        items: [],
        page: query.pageIndex + 1,
        pageSize: query.pageSize,
        totalCount: 0,
        totalPages: 0,
      }),
    );
  },

  async searchBehsazModal(
    payload: BehsazSearchRequest,
  ): Promise<PaginatedResponse<BehsazSearchRow>> {
    return apiFirst(
      async () => {
        const response = await api.post<PaginatedResponse<BehsazSearchRow>>(
          "/behsaz/search",
          payload,
        );
        return response.data;
      },
      async () => ({
        items: [],
        page: payload.page ?? payload.pageIndex + 1,
        pageSize: payload.pageSize,
        totalCount: 0,
        totalPages: 0,
      }),
    );
  },
};

const isEnabled = (): boolean =>
  String(import.meta.env.VITE_BEHSAZ_ENABLED ?? "false").toLowerCase() ===
  "true";
