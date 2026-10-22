export const auditEntityLabels: Record<string, string> = {
  Cases: "پرونده‌ها",
  Files: "فایل‌ها/پیوست‌ها",
  FileVersions: "نسخه‌های فایل",
  Dossiers: "کلاسورها/کد نوسازی",
  AspNetUsers: "کاربران سیستم",
  Users: "کاربران سیستم",
  Classifications: "طبقه‌بندی‌ها",
  Taxpayers: "مودیان",
  Owners: "مالکین پرونده",
  Tags: "برچسب‌ها",
  CaseLoans: "امانات پرونده فیزیکی",
  Roles: "نقش‌ها",
  Permissions: "مجوزها",
  DynamicTemplates: "قالب‌های اطلاعات تکمیلی",
  DynamicFieldGroups: "گروه‌های اطلاعات تکمیلی",
  DynamicFieldDefinitions: "فیلدهای اطلاعات تکمیلی",
  DynamicFieldOptions: "گزینه‌های فیلد پویا",
  ClassificationAcl: "دسترسی طبقه‌بندی",
  TagAcl: "دسترسی برچسب",
  ExportJobs: "خروجی‌ها",
};

export const getAuditEntityLabel = (value?: string | null) => {
  const normalized = value?.trim();
  if (!normalized) {
    return "-";
  }

  return auditEntityLabels[normalized] ?? normalized;
};

export const auditActionLabels: Record<string, string> = {
  Create: "ایجاد",
  CREATE: "ایجاد",
  Update: "ویرایش",
  UPDATE: "ویرایش",
  Delete: "حذف",
  DELETE: "حذف",
  SOFT_DELETE: "حذف نرم",
  CASE_MOVE_CLASSIFICATION: "انتقال پرونده بین طبقه‌بندی‌ها",
  UPLOAD_FINALIZED: "تکمیل بارگذاری فایل",
  Download: "دانلود",
  Upload: "بارگذاری",
  View: "مشاهده",
  Read: "مشاهده",
  Search: "جستجو",
  Print: "چاپ",
  Export: "خروجی گرفتن",
  Import: "ورود اطلاعات",
  Restore: "بازیابی",
  Move: "انتقال",
  Archive: "بایگانی",
  Unarchive: "خروج از بایگانی",
  Approve: "تأیید",
  Reject: "رد",
  Assign: "اختصاص",
  Unassign: "لغو اختصاص",
  Activate: "فعال‌سازی",
  Deactivate: "غیرفعال‌سازی",
  Lock: "قفل کردن",
  Unlock: "باز کردن قفل",
  Login: "ورود",
  Logout: "خروج",
  Borrow: "امانت دادن",
  Return: "بازگرداندن",
  CheckOut: "تحویل گرفتن",
  CheckIn: "تحویل دادن",
  PasswordChanged: "تغییر گذرواژه",
  ResetRequested: "درخواست بازنشانی",
};

const normalizedAuditActionLabels = Object.fromEntries(
  Object.entries(auditActionLabels).map(([key, label]) => [
    key.toUpperCase(),
    label,
  ]),
) as Record<string, string>;

const auditActionTokenLabels: Record<string, string> = {
  CREATE: "ایجاد",
  CREATED: "ایجاد",
  ADD: "افزودن",
  ADDED: "افزودن",
  UPDATE: "ویرایش",
  UPDATED: "ویرایش",
  EDIT: "ویرایش",
  EDITED: "ویرایش",
  DELETE: "حذف",
  DELETED: "حذف",
  REMOVE: "حذف",
  REMOVED: "حذف",
  DOWNLOAD: "دانلود",
  DOWNLOADED: "دانلود",
  UPLOAD: "بارگذاری",
  UPLOADED: " بارگذاری شده",
  FINALIZE: "تکمیل",
  FINALIZED: "تکمیل",
  VIEW: "مشاهده",
  VIEWED: "مشاهده",
  READ: "مشاهده",
  SEARCH: "جستجو",
  PRINT: "چاپ",
  EXPORT: "خروجی گرفتن",
  IMPORT: "ورود اطلاعات",
  RESTORE: "بازیابی",
  RESTORED: "بازیابی",
  MOVE: "انتقال",
  MOVED: "انتقال",
  ARCHIVE: "بایگانی",
  ARCHIVED: "بایگانی",
  UNARCHIVE: "خروج از بایگانی",
  APPROVE: "تأیید",
  APPROVED: "تأیید",
  REJECT: "رد",
  REJECTED: "رد",
  ASSIGN: "اختصاص",
  ASSIGNED: "اختصاص",
  UNASSIGN: "لغو اختصاص",
  ACTIVATE: "فعال‌سازی",
  ACTIVATED: "فعال‌سازی",
  DEACTIVATE: "غیرفعال‌سازی",
  DEACTIVATED: "غیرفعال‌سازی",
  LOCK: "قفل کردن",
  LOCKED: "قفل کردن",
  UNLOCK: "باز کردن قفل",
  UNLOCKED: "باز کردن قفل",
  LOGIN: "ورود",
  LOGOUT: "خروج",
  BORROW: "امانت دادن",
  BORROWED: "امانت دادن",
  RETURN: "بازگرداندن",
  RETURNED: "بازگرداندن",
  RESET: "بازنشانی",
  REQUEST: "درخواست",
  REQUESTED: "درخواست",
  CHANGE: "تغییر",
  CHANGED: "تغییر",
};

export const getAuditActionLabel = (value?: string | null) => {
  const normalized = value?.trim();
  if (!normalized) {
    return "-";
  }

  const directLabel = normalizedAuditActionLabels[normalized.toUpperCase()];
  if (directLabel) {
    return directLabel;
  }

  const tokens = normalized
    .replace(/([a-z0-9])([A-Z])/g, "$1_$2")
    .split(/[_\s-]+/)
    .filter(Boolean);
  const translatedTokens = tokens
    .map((token) => auditActionTokenLabels[token.toUpperCase()])
    .filter(Boolean);

  if (translatedTokens.length > 0) {
    return translatedTokens.join(" ");
  }

  return normalized;
};
