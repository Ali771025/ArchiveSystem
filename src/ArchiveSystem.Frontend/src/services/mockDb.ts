import { addDays, subDays } from 'date-fns';
import type {
  AdminUserDto,
  ArchiveFile,
  ArchiveTag,
  AuditRecord,
  BehsazQuery,
  BehsazResult,
  CaseDetails,
  CaseDynamicValue,
  CaseOwner,
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
  TagAclEntryDto,
} from '@/types/archive';
import { createClientId } from '@/utils/helpers';

interface MockDatabase {
  classifications: ClassificationNode[];
  cases: CaseDetails[];
  templates: DynamicTemplate[];
  dynamicValues: Record<number, CaseDynamicValue[]>;
  files: ArchiveFile[];
  fileVersions: Record<number, FileVersion[]>;
  tags: ArchiveTag[];
  roles: RoleDto[];
  permissions: PermissionDto[];
  users: AdminUserDto[];
  classificationAcl: ClassificationAclEntryDto[];
  tagAcl: TagAclEntryDto[];
  reports: AuditRecord[];
  exportJobs: ExportJob[];
  behsazResults: BehsazResult[];
}

const owners: CaseOwner[] = [
  { id: 1, fullName: 'علی رضایی', nationalCode: '1234567890', phone: '00000000000' },
  { id: 2, fullName: 'سمیه حیدری', nationalCode: '0987654321', phone: '00000000000' },
  { id: 3, fullName: 'مهدی امیری', nationalCode: '1122334455', phone: '00000000000' },
  { id: 4, fullName: 'ندا موسوی', nationalCode: '5566778899', phone: '00000000000' },
];

const classificationsBase: ClassificationNode[] = [
  { id: 1, title: 'املاک', parentId: null, path: '/1', level: 0, caseCount: 0, isActive: true },
  { id: 2, title: 'تجاری', parentId: 1, path: '/1/2', level: 1, caseCount: 0, isActive: true },
  { id: 3, title: 'مسکونی', parentId: 1, path: '/1/3', level: 1, caseCount: 0, isActive: true },
  { id: 4, title: 'قراردادها', parentId: null, path: '/4', level: 0, caseCount: 0, isActive: true },
  { id: 5, title: 'حقوقی', parentId: 4, path: '/4/5', level: 1, caseCount: 0, isActive: true },
  { id: 6, title: 'بایگانی راکد', parentId: null, path: '/6', level: 0, caseCount: 0, isActive: true, systemKey: 'ArchivedCases' },
];

const tagsSeed: ArchiveTag[] = [
  { id: 1, name: 'مهم', scope: 'global' },
  { id: 2, name: 'فوری', scope: 'global' },
  { id: 3, name: 'محرمانه', scope: 'classification', classificationId: 5 },
  { id: 4, name: 'اسکن', scope: 'global' },
];

const now = new Date();

const caseRows: CaseDetails[] = Array.from({ length: 58 }).map((_, index) => {
  const classification = index % 3 === 0 ? classificationsBase[1] : index % 3 === 1 ? classificationsBase[2] : classificationsBase[4];
  const owner1 = owners[index % owners.length];
  const owner2 = owners[(index + 1) % owners.length];
  const caseId = index + 1001;
  const tags = index % 4 === 0 ? [1, 2] : [1];

  const history: AuditRecord[] = [
    {
      id: caseId * 10 + 1,
      timestamp: subDays(now, index % 15).toISOString(),
      actorName: 'admin',
      actionType: 'CASE_CREATED',
      entityType: 'Case',
      detail: `پرونده ${caseId} ایجاد شد.`,
    },
    {
      id: caseId * 10 + 2,
      timestamp: subDays(now, (index % 15) - 1).toISOString(),
      actorName: 'operator',
      actionType: 'CASE_UPDATED',
      entityType: 'Case',
      detail: 'اطلاعات مالک و نشانی اصلاح شد.',
    },
  ];

  return {
    id: caseId,
    caseNumber: `A-${caseId}`,
    externalCaseNumber: `EXT-${5000 + caseId}`,
    avatarImagePath: null,
    address: `تهران، منطقه ${((index % 22) + 1).toString()}, خیابان نمونه`,
    updatedAt: subDays(now, index % 12).toISOString(),
    classificationId: classification.id,
    classificationTitle: classification.title,
    ownerNames: [owner1.fullName, owner2.fullName],
    tags: tags.map((tagId) => tagsSeed.find((tag) => tag.id === tagId)?.name ?? ''),
    owners: [owner1, owner2],
    history,
  };
});

const templatesSeed: DynamicTemplate[] = [
  {
    id: 'tpl-2',
    classificationId: 2,
    version: 1,
    publishedAt: subDays(now, 5).toISOString(),
    groups: [
      {
        id: 'grp-identity',
        title: 'اطلاعات پایه',
        apiName: 'identity',
        order: 0,
        fields: [
          {
            id: 'f-registry',
            apiName: 'registryNumber',
            label: 'شماره ثبت',
            type: 'text',
            required: true,
            showAsFilter: true,
            filterType: 'contains',
            order: 0,
            options: [],
            rules: { regex: '^\\d{4,12}$', errorMessage: 'شماره ثبت باید عددی و بین 4 تا 12 رقم باشد.' },
          },
          {
            id: 'f-usage',
            apiName: 'usageType',
            label: 'نوع کاربری',
            type: 'dropdown',
            required: false,
            showAsFilter: true,
            filterType: 'equals',
            order: 1,
            options: [
              { id: 'opt-office', label: 'اداری', value: 'office' },
              { id: 'opt-shop', label: 'تجاری', value: 'shop' },
            ],
            rules: {},
          },
        ],
      },
      {
        id: 'grp-finance',
        title: 'اطلاعات مالی',
        apiName: 'finance',
        order: 1,
        fields: [
          {
            id: 'f-value',
            apiName: 'estimatedValue',
            label: 'ارزش برآوردی',
            type: 'decimal',
            required: true,
            showAsFilter: true,
            filterType: 'range',
            order: 0,
            options: [],
            rules: { min: 0 },
          },
          {
            id: 'f-contractDate',
            apiName: 'contractDate',
            label: 'تاریخ قرارداد',
            type: 'date',
            required: false,
            showAsFilter: true,
            filterType: 'range',
            order: 1,
            options: [],
            rules: {},
          },
        ],
      },
    ],
  },
  {
    id: 'tpl-5',
    classificationId: 5,
    version: 2,
    publishedAt: subDays(now, 2).toISOString(),
    groups: [
      {
        id: 'grp-legal',
        title: 'جزئیات حقوقی',
        apiName: 'legal',
        order: 0,
        fields: [
          {
            id: 'f-plaintiff',
            apiName: 'plaintiff',
            label: 'خواهان',
            type: 'text',
            required: true,
            showAsFilter: true,
            filterType: 'contains',
            order: 0,
            options: [],
            rules: {},
          },
          {
            id: 'f-defendant',
            apiName: 'defendant',
            label: 'خوانده',
            type: 'text',
            required: true,
            showAsFilter: true,
            filterType: 'contains',
            order: 1,
            options: [],
            rules: {},
          },
          {
            id: 'f-hasAppeal',
            apiName: 'hasAppeal',
            label: 'تجدیدنظر دارد؟',
            type: 'checkbox',
            required: false,
            showAsFilter: true,
            filterType: 'equals',
            order: 2,
            options: [],
            rules: {},
          },
        ],
      },
    ],
  },
];

const dynamicValueSeed: Record<number, CaseDynamicValue[]> = Object.fromEntries(
  caseRows.slice(0, 40).map((item, index) => {
    if (item.classificationId === 2) {
      return [
        item.id,
        [
          { fieldId: 'f-registry', value: `${9000 + item.id}`, values: [] },
          { fieldId: 'f-usage', value: index % 2 === 0 ? 'office' : 'shop', values: [] },
          { fieldId: 'f-value', value: 150000000 + index * 1000000, values: [] },
          { fieldId: 'f-contractDate', value: subDays(now, index).toISOString().slice(0, 10), values: [] },
        ],
      ];
    }

    if (item.classificationId === 5) {
      return [
        item.id,
        [
          { fieldId: 'f-plaintiff', value: 'شرکت الف', values: [] },
          { fieldId: 'f-defendant', value: 'شرکت ب', values: [] },
          { fieldId: 'f-hasAppeal', value: index % 2 === 0, values: [] },
        ],
      ];
    }

    return [item.id, []];
  }),
);

const filesSeed: ArchiveFile[] = caseRows.flatMap((item, index) => {
  const firstId = item.id * 10;
  return [
    {
      id: firstId,
      caseId: item.id,
      name: `scan-${item.caseNumber}.pdf`,
      extension: 'pdf',
      contentType: 'application/pdf',
      size: 2_400_000 + index * 2000,
      uploader: 'scanner-service',
      uploadedAt: subDays(now, index % 7).toISOString(),
      latestVersion: 2,
      status: 1,
      tags: [1, 4],
      processing: {
        ocr: index % 5 === 0 ? 'Pending' : 'Completed',
        thumbnail: index % 6 === 0 ? 'Pending' : 'Completed',
      },
    },
    {
      id: firstId + 1,
      caseId: item.id,
      name: `contract-${item.caseNumber}.docx`,
      extension: 'docx',
      contentType: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
      size: 800_000 + index * 1500,
      uploader: 'archive-user',
      uploadedAt: subDays(now, index % 10).toISOString(),
      latestVersion: 1,
      status: 1,
      tags: [1],
      processing: {
        ocr: 'Completed',
        thumbnail: 'Completed',
      },
    },
  ];
});

const fileVersionSeed: Record<number, FileVersion[]> = Object.fromEntries(
  filesSeed.map((file) => {
    const versions: FileVersion[] = Array.from({ length: file.latestVersion }).map((_, idx) => ({
      id: file.id * 100 + idx + 1,
      versionNumber: idx + 1,
      size: file.size - (file.latestVersion - (idx + 1)) * 5000,
      uploadedAt: subDays(new Date(file.uploadedAt), file.latestVersion - idx).toISOString(),
      uploader: file.uploader,
      ocrStatus: idx === file.latestVersion - 1 ? file.processing.ocr : 'Completed',
      thumbnailStatus: idx === file.latestVersion - 1 ? file.processing.thumbnail : 'Completed',
      ocrWordCount: 900 + idx * 120,
      ocrLanguage: 'fa',
      ocrTextPreview:
        'نمونه متن استخراج‌شده OCR برای نمایش در رابط کاربری. این متن جهت آزمون قابلیت جستجو و کپی استفاده می‌شود.',
    }));

    return [file.id, versions];
  }),
);

const permissionsSeed: PermissionDto[] = [
  { id: 1, key: 'admin.access', description: 'مدیریت سامانه' },
  { id: 2, key: 'classification.read', description: 'خواندن طبقه‌بندی' },
  { id: 3, key: 'classification.write', description: 'ویرایش طبقه‌بندی' },
  { id: 4, key: 'classification.manage', description: 'مدیریت طبقه‌بندی' },
  { id: 5, key: 'tag.view', description: 'مشاهده تگ' },
  { id: 6, key: 'tag.assign', description: 'اختصاص تگ' },
  { id: 7, key: 'reports.view', description: 'مشاهده گزارش' },
  { id: 8, key: 'exports.create', description: 'ایجاد خروجی' },
];

const rolesSeed: RoleDto[] = [
  { id: 1, name: 'Admin', isSystem: true },
  { id: 2, name: 'ArchiveOperator', isSystem: false },
  { id: 3, name: 'Viewer', isSystem: true },
];

const usersSeed: AdminUserDto[] = [
  { id: 1, userName: 'admin', email: 'admin@archive.local' },
  { id: 2, userName: 'operator', email: 'operator@archive.local' },
  { id: 3, userName: 'viewer', email: 'viewer@archive.local' },
];

const db: MockDatabase = {
  classifications: classificationsBase,
  cases: caseRows,
  templates: templatesSeed,
  dynamicValues: dynamicValueSeed,
  files: filesSeed,
  fileVersions: fileVersionSeed,
  tags: tagsSeed,
  roles: rolesSeed,
  permissions: permissionsSeed,
  users: usersSeed,
  classificationAcl: [
    {
      id: 1,
      subjectType: 2,
      subjectId: 2,
      classificationId: 1,
      permissionMask: 15,
      isExplicit: true,
      inheritMode: 1,
    },
  ],
  tagAcl: [
    {
      id: 1,
      subjectType: 2,
      subjectId: 2,
      tagId: 1,
      permissionMask: 3,
    },
  ],
  reports: caseRows.slice(0, 160).flatMap((item, index) => [
    {
      id: index * 2 + 1,
      timestamp: subDays(now, index % 20).toISOString(),
      actorName: index % 3 === 0 ? 'admin' : 'operator',
      actionType: 'CASE_UPDATED',
      entityType: 'Case',
      detail: `پرونده ${item.caseNumber} به‌روزرسانی شد.`,
    },
    {
      id: index * 2 + 2,
      timestamp: subDays(now, index % 18).toISOString(),
      actorName: 'scanner-service',
      actionType: 'FILE_UPLOADED',
      entityType: 'File',
      detail: `فایل جدید برای پرونده ${item.caseNumber} بارگذاری شد.`,
    },
  ]),
  exportJobs: [],
  behsazResults: [
    {
      externalCaseNumber: 'BZ-1404-0012',
      title: 'پرونده ملکی تهران',
      ownerName: 'علی رضایی',
      nationalCode: '1234567890',
      registeredAt: subDays(now, 70).toISOString(),
    },
    {
      externalCaseNumber: 'BZ-1404-0042',
      title: 'پرونده حقوقی تجاری',
      ownerName: 'سمیه حیدری',
      nationalCode: '0987654321',
      registeredAt: subDays(now, 40).toISOString(),
    },
    {
      externalCaseNumber: 'BZ-1404-0063',
      title: 'پرونده مطالبه وجه',
      ownerName: 'ندا موسوی',
      nationalCode: '5566778899',
      registeredAt: subDays(now, 18).toISOString(),
    },
  ],
};

const sortByPath = (a: ClassificationNode, b: ClassificationNode): number => a.path.localeCompare(b.path);

const recomputeClassificationMetadata = (): void => {
  db.classifications.forEach((node) => {
    node.caseCount = db.cases.filter((item) => item.classificationId === node.id).length;
  });

  const mapById = new Map<number, ClassificationNode>(db.classifications.map((node) => [node.id, node]));
  const calcPath = (node: ClassificationNode): string => {
    if (!node.parentId) {
      return `/${node.id}`;
    }

    const parent = mapById.get(node.parentId);
    if (!parent) {
      return `/${node.id}`;
    }

    return `${calcPath(parent)}/${node.id}`;
  };

  db.classifications.forEach((node) => {
    node.path = calcPath(node);
    node.level = Math.max(0, node.path.split('/').filter(Boolean).length - 1);
  });

  db.classifications.sort(sortByPath);
};

const toSummary = (item: CaseDetails): CaseSummary => ({
  id: item.id,
  caseNumber: item.caseNumber,
  externalCaseNumber: item.externalCaseNumber,
  avatarImagePath: item.avatarImagePath,
  address: item.address,
  updatedAt: item.updatedAt,
  classificationId: item.classificationId,
  classificationTitle: item.classificationTitle,
  ownerNames: item.ownerNames,
  tags: item.tags,
});

const paginate = <T>(items: T[], page: number, pageSize: number): PaginatedResponse<T> => {
  const totalCount = items.length;
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
  const safePage = Math.max(1, Math.min(page, totalPages));
  const start = (safePage - 1) * pageSize;
  const sliced = items.slice(start, start + pageSize);

  return {
    items: sliced,
    totalCount,
    page: safePage,
    pageSize,
    totalPages,
  };
};

const normalizeText = (value: string): string => value.trim().toLowerCase();

const nextId = (items: Array<{ id: number }>): number => {
  if (items.length === 0) {
    return 1;
  }

  return Math.max(...items.map((item) => item.id)) + 1;
};

const withDelay = async <T>(value: T): Promise<T> => {
  await new Promise((resolve) => setTimeout(resolve, 120));
  return value;
};

recomputeClassificationMetadata();

export const mockDb = {
  async getClassificationTree(): Promise<ClassificationNode[]> {
    recomputeClassificationMetadata();
    return withDelay([...db.classifications]);
  },

  async getWritableClassificationIds(): Promise<number[]> {
    recomputeClassificationMetadata();
    const parentIds = new Set(
      db.classifications
        .filter((item) => item.parentId != null)
        .map((item) => item.parentId as number),
    );

    return withDelay(
      db.classifications
        .filter((item) => item.isActive && !parentIds.has(item.id))
        .map((item) => item.id),
    );
  },

  async getCaseMoveAccessIds(): Promise<number[]> {
    recomputeClassificationMetadata();
    return withDelay(
      db.classifications
        .filter((item) => item.isActive)
        .map((item) => item.id),
    );
  },

  async createClassification(title: string, parentId: number | null): Promise<ClassificationNode> {
    const id = nextId(db.classifications);
    const node: ClassificationNode = {
      id,
      title,
      parentId,
      level: 0,
      path: '',
      caseCount: 0,
      isActive: true,
    };
    db.classifications.push(node);
    recomputeClassificationMetadata();
    return withDelay(node);
  },

  async renameClassification(id: number, title: string): Promise<void> {
    const node = db.classifications.find((item) => item.id === id);
    if (node) {
      node.title = title;
    }
    await withDelay(undefined);
  },

  async moveClassification(id: number, newParentId: number | null): Promise<void> {
    const node = db.classifications.find((item) => item.id === id);
    if (node) {
      node.parentId = newParentId;
    }
    recomputeClassificationMetadata();
    await withDelay(undefined);
  },

  async deleteClassification(id: number): Promise<void> {
    db.classifications = db.classifications.filter((item) => item.id !== id && item.parentId !== id);
    recomputeClassificationMetadata();
    await withDelay(undefined);
  },

  async searchCases(request: CaseSearchRequest): Promise<PaginatedResponse<CaseSummary>> {
    const ownerNeedle = normalizeText(request.ownerName ?? '');
    const searchNeedle = normalizeText(request.externalCaseNumber ?? '');
    const createdByNeedle = normalizeText(request.createdBy ?? '');

    const filtered = db.cases.filter((item) => {
      if (request.classificationId && item.classificationId !== request.classificationId) {
        return false;
      }

      if (ownerNeedle && !item.ownerNames.some((owner) => normalizeText(owner).includes(ownerNeedle))) {
        return false;
      }

      if (searchNeedle) {
        const combined = normalizeText(`${item.caseNumber} ${item.externalCaseNumber} ${item.address}`);
        if (!combined.includes(searchNeedle)) {
          return false;
        }
      }

      if (createdByNeedle) {
        const createdBy = normalizeText(`${item.createdByUserId ?? ''} ${item.createdByUserName ?? ''}`);
        if (!createdBy.includes(createdByNeedle)) {
          return false;
        }
      }

      if (request.fromDate && new Date(item.updatedAt) < new Date(request.fromDate)) {
        return false;
      }

      if (request.toDate && new Date(item.updatedAt) > addDays(new Date(request.toDate), 1)) {
        return false;
      }

      if (request.tags && request.tags.length > 0) {
        const tagSet = new Set(item.tags);
        const required = request.tags.map((tagId) => db.tags.find((tag) => tag.id === tagId)?.name ?? '');
        if (!required.every((tag) => tagSet.has(tag))) {
          return false;
        }
      }

      return true;
    });

    const sorted = [...filtered].sort((a, b) => {
      const key = request.sortBy;
      const direction = request.sortDirection === 'asc' ? 1 : -1;

      const numericKey = key === 'id' || key === 'classificationId';
      if (numericKey) {
        const av = key === 'id' ? a.id : a.classificationId;
        const bv = key === 'id' ? b.id : b.classificationId;
        return (av - bv) * direction;
      }

      const av = String(a[key] ?? '');
      const bv = String(b[key] ?? '');
      return av.localeCompare(bv, 'fa') * direction;
    });

    return withDelay(paginate(sorted.map(toSummary), request.page, request.pageSize));
  },

  async getCaseDetails(caseId: number): Promise<CaseDetails | null> {
    const item = db.cases.find((entry) => entry.id === caseId) ?? null;
    return withDelay(item ? { ...item } : null);
  },

  async getCaseDetailsByNumber(caseNumber: string): Promise<CaseDetails | null> {
    const normalized = caseNumber.trim().toLowerCase();
    const item = db.cases.find((entry) => entry.caseNumber.toLowerCase() === normalized) ?? null;
    return withDelay(item ? { ...item } : null);
  },

  async createCase(payload: Pick<CaseDetails, 'classificationId' | 'address' | 'externalCaseNumber' | 'postalCode'>): Promise<CaseDetails> {
    const id = nextId(db.cases);
    const classification = db.classifications.find((item) => item.id === payload.classificationId);
    const dateParts = new Intl.DateTimeFormat('fa-IR-u-ca-persian-nu-latn', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
    }).formatToParts(new Date());
    const part = (type: Intl.DateTimeFormatPartTypes) =>
      dateParts.find((item) => item.type === type)?.value ?? '';
    const dateKey = `${part('year')}${part('month')}${part('day')}`;
    const dailySerial =
      db.cases.filter((entry) => entry.caseNumber.startsWith(dateKey)).length + 1;
    const item: CaseDetails = {
      id,
      caseNumber: `${dateKey}${dailySerial.toString().padStart(5, '0')}`,
      externalCaseNumber: payload.externalCaseNumber,
      avatarImagePath: null,
      address: payload.address,
      postalCode: payload.postalCode ?? null,
      updatedAt: new Date().toISOString(),
      classificationId: payload.classificationId,
      classificationTitle: classification?.title ?? 'نامشخص',
      ownerNames: [owners[0].fullName],
      tags: ['مهم'],
      owners: [owners[0]],
      history: [
        {
          id: id * 100,
          timestamp: new Date().toISOString(),
          actorName: 'operator',
          actionType: 'CASE_CREATED',
          entityType: 'Case',
          detail: 'پرونده جدید ثبت شد.',
        },
      ],
    };

    db.cases.unshift(item);
    recomputeClassificationMetadata();
    return withDelay(item);
  },

  async updateCase(updated: CaseDetails): Promise<CaseDetails> {
    const index = db.cases.findIndex((item) => item.id === updated.id);
    if (index >= 0) {
      db.cases[index] = { ...updated, updatedAt: new Date().toISOString() };
    }

    return withDelay({ ...db.cases[index] });
  },

  async moveCase(caseId: number, classificationId: number): Promise<void> {
    const item = db.cases.find((entry) => entry.id === caseId);
    const classification = db.classifications.find((entry) => entry.id === classificationId);
    if (item && classification) {
      item.classificationId = classificationId;
      item.classificationTitle = classification.title;
      item.updatedAt = new Date().toISOString();
      item.history.unshift({
        id: Date.now(),
        timestamp: new Date().toISOString(),
        actorName: 'operator',
        actionType: 'CASE_MOVED',
        entityType: 'Case',
        detail: `پرونده به طبقه "${classification.title}" منتقل شد.`,
      });
    }

    recomputeClassificationMetadata();
    await withDelay(undefined);
  },

  async uploadCaseAvatar(caseId: number, fileName: string): Promise<{ avatarImagePath: string; thumbnailImagePath: string }> {
    const item = db.cases.find((entry) => entry.id === caseId);
    const suffix = fileName.split('.').pop()?.toLowerCase() === 'png' ? 'png' : 'jpg';
    const avatarImagePath = `/uploads/case-avatars/${caseId}/${Date.now().toString()}.${suffix}`;
    const thumbnailImagePath = avatarImagePath.replace(/\.(jpg|png)$/i, '_thumb.$1');

    if (item) {
      item.avatarImagePath = avatarImagePath;
      item.updatedAt = new Date().toISOString();
    }

    return withDelay({ avatarImagePath, thumbnailImagePath });
  },

  async getTemplate(classificationId: number): Promise<DynamicTemplate | null> {
    const template = db.templates.find((item) => item.classificationId === classificationId) ?? null;
    return withDelay(template ? structuredClone(template) : null);
  },

  async saveTemplate(classificationId: number, template: DynamicTemplate): Promise<DynamicTemplate> {
    const index = db.templates.findIndex((item) => item.classificationId === classificationId);
    const payload: DynamicTemplate = {
      ...template,
      classificationId,
      version: (db.templates[index]?.version ?? 0) + 1,
      publishedAt: new Date().toISOString(),
    };

    if (index >= 0) {
      db.templates[index] = payload;
    } else {
      db.templates.push(payload);
    }

    return withDelay(structuredClone(payload));
  },

  async getCaseDynamicValues(caseId: number): Promise<CaseDynamicValue[]> {
    return withDelay(structuredClone(db.dynamicValues[caseId] ?? []));
  },

  async saveCaseDynamicValues(caseId: number, values: CaseDynamicValue[]): Promise<CaseDynamicValue[]> {
    db.dynamicValues[caseId] = structuredClone(values);
    return withDelay(structuredClone(values));
  },

  async getCaseFiles(caseId: number): Promise<ArchiveFile[]> {
    return withDelay(
      db.files
        .filter((item) => item.caseId === caseId && item.status !== 3)
        .map((item) => ({ ...item })),
    );
  },

  async uploadCaseFile(caseId: number, fileName: string, size: number): Promise<ArchiveFile> {
    const id = nextId(db.files);
    const extension = fileName.split('.').pop() ?? 'bin';
    const entry: ArchiveFile = {
      id,
      caseId,
      name: fileName,
      extension,
      contentType: extension === 'pdf' ? 'application/pdf' : 'application/octet-stream',
      size,
      uploader: 'operator',
      uploadedAt: new Date().toISOString(),
      latestVersion: 1,
      status: 1,
      tags: [1],
      processing: {
        ocr: 'Pending',
        thumbnail: 'Pending',
      },
    };

    db.files.unshift(entry);
    db.fileVersions[id] = [
      {
        id: id * 100 + 1,
        versionNumber: 1,
        size,
        uploadedAt: new Date().toISOString(),
        uploader: 'operator',
        ocrStatus: 'Pending',
        thumbnailStatus: 'Pending',
        ocrWordCount: 0,
        ocrLanguage: 'fa',
        ocrTextPreview: '',
      },
    ];

    return withDelay({ ...entry });
  },

  async getFileVersions(fileId: number): Promise<FileVersion[]> {
    return withDelay(structuredClone(db.fileVersions[fileId] ?? []));
  },

  async setFileTags(fileId: number, tagIds: number[]): Promise<void> {
    const file = db.files.find((entry) => entry.id === fileId);
    if (file) {
      file.tags = [...new Set(tagIds)];
    }

    await withDelay(undefined);
  },

  async softDeleteFile(fileId: number): Promise<void> {
    const file = db.files.find((item) => item.id === fileId);
    if (file) {
      file.status = 3;
    }
    await withDelay(undefined);
  },

  async getRoles(): Promise<RoleDto[]> {
    return withDelay([...db.roles]);
  },

  async createRole(name: string): Promise<RoleDto> {
    const role: RoleDto = { id: nextId(db.roles), name, isSystem: false };
    db.roles.push(role);
    return withDelay(role);
  },

  async getPermissions(): Promise<PermissionDto[]> {
    return withDelay([...db.permissions]);
  },

  async getUsers(): Promise<AdminUserDto[]> {
    return withDelay([...db.users]);
  },

  async getClassificationAcl(classificationId: number): Promise<ClassificationAclEntryDto[]> {
    return withDelay(db.classificationAcl.filter((item) => item.classificationId === classificationId).map((item) => ({ ...item })));
  },

  async saveClassificationAcl(classificationId: number, entries: ClassificationAclEntryDto[]): Promise<void> {
    db.classificationAcl = [
      ...db.classificationAcl.filter((item) => item.classificationId !== classificationId),
      ...entries,
    ];
    await withDelay(undefined);
  },

  async getTags(): Promise<ArchiveTag[]> {
    return withDelay([...db.tags]);
  },

  async createTag(name: string, description?: string): Promise<ArchiveTag> {
    const normalizedName = name.trim();
    const existing = db.tags.find(
      (tag) => tag.name.trim().toLocaleLowerCase() === normalizedName.toLocaleLowerCase(),
    );
    if (existing) {
      return withDelay({ ...existing });
    }

    const tag: ArchiveTag = {
      id: nextId(db.tags),
      name: normalizedName,
      description: description?.trim() || undefined,
      scope: "global",
    };
    db.tags.push(tag);
    return withDelay({ ...tag });
  },

  async getTagAcl(tagId: number): Promise<TagAclEntryDto[]> {
    return withDelay(db.tagAcl.filter((item) => item.tagId === tagId).map((item) => ({ ...item })));
  },

  async saveTagAcl(tagId: number, entries: TagAclEntryDto[]): Promise<void> {
    db.tagAcl = [...db.tagAcl.filter((item) => item.tagId !== tagId), ...entries];
    await withDelay(undefined);
  },

  async getActivity(page: number, pageSize: number): Promise<PaginatedResponse<AuditRecord>> {
    return withDelay(paginate([...db.reports], page, pageSize));
  },

  async createExportJob(type: 'activity' | 'files'): Promise<ExportJob> {
    const job: ExportJob = {
      id: createClientId(),
      type,
      status: 'running',
      progressPercent: 12,
      createdAt: new Date().toISOString(),
    };
    db.exportJobs.unshift(job);

    setTimeout(() => {
      const found = db.exportJobs.find((entry) => entry.id === job.id);
      if (found) {
        found.status = 'completed';
        found.progressPercent = 100;
        found.completedAt = new Date().toISOString();
      }
    }, 1800);

    return withDelay({ ...job });
  },

  async getExportJobs(): Promise<ExportJob[]> {
    return withDelay([...db.exportJobs]);
  },

  async behsazSearch(query: BehsazQuery): Promise<BehsazResult[]> {
    const needle = normalizeText(query.text);
    return withDelay(
      db.behsazResults.filter((item) => {
        const composite = `${item.externalCaseNumber} ${item.title} ${item.ownerName} ${item.nationalCode}`.toLowerCase();
        return composite.includes(needle);
      }),
    );
  },
};
