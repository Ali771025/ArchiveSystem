export type PermissionKey =
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

export interface PermissionItem {
  key: PermissionKey;
  label: string;
  description: string;
}

export interface PermissionGroup {
  module: 'DynamicFields' | 'Categories' | 'Reports' | 'Tags' | 'Dossiers' | 'Files' | 'Roles' | 'CaseLoans' | 'Taxpayers' | 'Users' | 'Behsaz';
  title: string;
  permissions: PermissionItem[];
}

export interface RoleSummary {
  id: number;
  name: string;
  isSystem: boolean;
}

export interface UserSummary {
  id: number;
  userName: string;
  email: string;
}

export const permissionGroups: PermissionGroup[] = [
  {
    module: 'DynamicFields',
    title: 'فیلدهای پویا',
    permissions: [
      {
        key: 'dynamicfields.manage',
        label: 'مدیریت',
        description: 'ایجاد، ویرایش، حذف و پیکربندی قالب‌های فیلد پویا.',
      },
    ],
  },
  {
    module: 'Categories',
    title: 'دسته‌بندی‌ها',
    permissions: [
      {
        key: 'categories.create',
        label: 'ایجاد',
        description: 'ایجاد رکوردهای دسته‌بندی.',
      },
      {
        key: 'categories.edit',
        label: 'ویرایش',
        description: 'ویرایش عنوان و مشخصات دسته‌بندی.',
      },
      {
        key: 'categories.read',
        label: 'مشاهده',
        description: 'مشاهده اطلاعات دسته‌بندی.',
      },
      {
        key: 'categories.manage',
        label: 'مدیریت',
        description: 'مدیریت کامل دسته‌بندی (CRUD + نگهداری).',
      },
    ],
  },
  {
    module: 'Reports',
    title: 'گزارش‌ها',
    permissions: [
      {
        key: 'reports.exportexcel',
        label: 'خروجی اکسل',
        description: 'خروجی گزارش‌ها در قالب Excel.',
      },
    ],
  },
  {
    module: 'Tags',
    title: 'برچسب‌ها',
    permissions: [
      {
        key: 'tags.manage',
        label: 'مدیریت',
        description: 'مدیریت چرخه حیات و فراداده برچسب‌ها.',
      },
    ],
  },
  {
    module: 'Dossiers',
    title: 'پرونده‌ها',
    permissions: [
      {
        key: 'dossiers.manage',
        label: 'مدیریت',
        description: 'مدیریت اطلاعات و تنظیمات پرونده‌ها.',
      },
    ],
  },
  {
    module: 'Files',
    title: 'فایل‌ها',
    permissions: [
      {
        key: 'files.read',
        label: 'مشاهده',
        description: 'مشاهده، پیش‌نمایش و دانلود فایل‌های پرونده.',
      },
      {
        key: 'files.manage',
        label: 'مدیریت',
        description: 'بارگذاری و مدیریت برچسب‌های فایل.',
      },
      {
        key: 'files.delete',
        label: 'حذف سند',
        description: 'حذف نرم سند یا نسخه مجاز، بدون حذف فیزیکی محتوای آرشیوی.',
      },
    ],
  },
  {
    module: 'Roles',
    title: 'نقش‌ها',
    permissions: [
      {
        key: 'roles.manage',
        label: 'مدیریت',
        description: 'ایجاد، ویرایش و تخصیص نقش‌های سامانه.',
      },
    ],
  },
  {
    module: 'CaseLoans',
    title: 'امانات پرونده فیزیکی',
    permissions: [
      {
        key: 'caseloans.manage',
        label: 'مدیریت',
        description: 'ثبت، تحویل، بازگشت و مدیریت امانات پرونده‌های فیزیکی.',
      },
    ],
  },
  {
    module: 'Taxpayers',
    title: 'مودیان',
    permissions: [
      {
        key: 'taxpayers.manage',
        label: 'مدیریت',
        description: 'ایجاد، ویرایش و مدیریت اطلاعات مودیان.',
      },
    ],
  },
  {
    module: 'Users',
    title: 'کاربران',
    permissions: [
      {
        key: 'users.manage',
        label: 'مدیریت',
        description: 'ایجاد، ویرایش و مدیریت کاربران سامانه.',
      },
    ],
  },
  {
    module: 'Behsaz',
    title: 'بهساز',
    permissions: [
      {
        key: 'behsaz.search',
        label: 'جستجو',
        description: 'جستجو و دریافت اطلاعات از سامانه بهساز.',
      },
    ],
  },
];

export const softwareTabPermissions: PermissionItem[] = [
  { key: 'tabs.dashboard.view', label: 'داشبورد', description: 'نمایش تب داشبورد.' },
  { key: 'tabs.archive.view', label: 'بایگانی', description: 'نمایش تب اصلی پرونده‌ها و بایگانی.' },
  { key: 'tabs.roles.view', label: 'نقش‌ها و دسترسی‌ها', description: 'نمایش تب مدیریت نقش‌ها و کنترل دسترسی.' },
  { key: 'tabs.settings.view', label: 'تنظیمات', description: 'نمایش تب‌های مدیریتی و تنظیمات سامانه.' },
  { key: 'tabs.reports.view', label: 'گزارش‌ها', description: 'نمایش تب گزارش‌های سامانه.' },
  { key: 'tabs.accesscontrol.view', label: 'کنترل دسترسی', description: 'نمایش تب مدیریت نقش‌ها و کنترل دسترسی.' },
  { key: 'tabs.caseloans.view', label: 'امانات پرونده فیزیکی', description: 'نمایش تب امانات پرونده‌های فیزیکی.' },
  { key: 'tabs.taxpayers.view', label: 'مدیریت مودیان', description: 'نمایش تب مدیریت اطلاعات مودیان.' },
  { key: 'tabs.users.view', label: 'مدیریت کاربران', description: 'نمایش تب مدیریت کاربران سامانه.' },
];
