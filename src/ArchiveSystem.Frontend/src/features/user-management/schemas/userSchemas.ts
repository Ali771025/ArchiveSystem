import { z } from 'zod';
import type { UserFormValues } from '@/features/user-management/types';

const timeRegex = /^([01]\d|2[0-3]):[0-5]\d:[0-5]\d$/;
const nationalIdRegex = /^\d{10}$/;
const mobileRegex = /^(09|۰۹|٠٩)[0-9۰-۹٠-٩]{9}$/;

const normalizeDigits = (value?: string) =>
  value
    ?.replace(/[۰-۹]/g, (digit) => String(digit.charCodeAt(0) - 1776))
    .replace(/[٠-٩]/g, (digit) => String(digit.charCodeAt(0) - 1632));

const trimDigitsOrEmpty = (value?: string) => normalizeDigits(value)?.trim() ?? '';

export const createUserSchema = z
  .object({
    personnelId: z.number().nullable(),
    firstName: z.string().min(2, 'نام الزامی است.').max(100, 'نام نمی‌تواند بیش از 100 کاراکتر باشد.'),
    lastName: z.string().min(2, 'نام خانوادگی الزامی است.').max(100, 'نام خانوادگی نمی‌تواند بیش از 100 کاراکتر باشد.'),
    personnelCode: z.string().min(1, 'کد پرسنلی الزامی است.').max(50, 'کد پرسنلی نمی‌تواند بیش از 50 کاراکتر باشد.'),
    nationalId: z.string().regex(nationalIdRegex, 'کد ملی باید 10 رقم باشد.'),
    email: z.string().email('ایمیل معتبر نیست.').max(256, 'ایمیل نمی‌تواند بیش از 256 کاراکتر باشد.'),
    phoneNumber: z.string().trim().regex(mobileRegex, 'شماره همراه باید با 09 شروع شود و دقیقاً 11 رقم باشد.'),
    username: z.string().min(3, 'نام کاربری حداقل 3 کاراکتر است.').max(100, 'نام کاربری نمی‌تواند بیش از 100 کاراکتر باشد.'),
    password: z
      .string()
      .min(12, 'رمز عبور باید حداقل 12 کاراکتر باشد.')
      .regex(/[A-Z]/, 'رمز عبور باید حداقل یک حرف بزرگ داشته باشد.')
      .regex(/[a-z]/, 'رمز عبور باید حداقل یک حرف کوچک داشته باشد.')
      .regex(/[0-9]/, 'رمز عبور باید حداقل یک عدد داشته باشد.')
      .regex(/[^a-zA-Z0-9]/, 'رمز عبور باید حداقل یک کاراکتر ویژه داشته باشد.'),
    confirmPassword: z.string().min(1, 'تکرار رمز عبور الزامی است.'),
    activeDuration: z.string().regex(timeRegex, 'مدت فعال بودن باید به صورت HH:mm:ss باشد.'),
    autoBackupEnabled: z.boolean(),
    twoFactorEnabled: z.boolean(),
  })
  .refine((values) => values.password === values.confirmPassword, {
    path: ['confirmPassword'],
    message: 'تکرار رمز عبور با رمز عبور یکسان نیست.',
  });

export const updateUserSchema = z.object({
  personnelId: z.number().nullable(),
  firstName: z.string().min(2, 'نام الزامی است.').max(100, 'نام نمی‌تواند بیش از 100 کاراکتر باشد.'),
  lastName: z.string().min(2, 'نام خانوادگی الزامی است.').max(100, 'نام خانوادگی نمی‌تواند بیش از 100 کاراکتر باشد.'),
  personnelCode: z.string().min(1, 'کد پرسنلی الزامی است.').max(50, 'کد پرسنلی نمی‌تواند بیش از 50 کاراکتر باشد.'),
  nationalId: z.string().regex(nationalIdRegex, 'کد ملی باید 10 رقم باشد.'),
  email: z.string().email('ایمیل معتبر نیست.').max(256, 'ایمیل نمی‌تواند بیش از 256 کاراکتر باشد.'),
  phoneNumber: z.string().trim().regex(mobileRegex, 'شماره همراه باید با 09 شروع شود و دقیقاً 11 رقم باشد.'),
  username: z.string().min(3, 'نام کاربری حداقل 3 کاراکتر است.').max(100, 'نام کاربری نمی‌تواند بیش از 100 کاراکتر باشد.'),
  password: z.string().optional(),
  confirmPassword: z.string().optional(),
  activeDuration: z.string().regex(timeRegex, 'مدت فعال بودن باید به صورت HH:mm:ss باشد.'),
  autoBackupEnabled: z.boolean(),
  twoFactorEnabled: z.boolean(),
});

export type CreateUserFormSchema = z.infer<typeof createUserSchema>;
export type UpdateUserFormSchema = z.infer<typeof updateUserSchema>;

export const userFilterSchema = z.object({
  username: z.string().max(100),
  firstName: z.string().max(100),
  lastName: z.string().max(100),
  nationalId: z.string().max(20),
});

export const toCreatePayload = (values: UserFormValues) => ({
  username: values.username.trim(),
  email: values.email.trim(),
  phoneNumber: trimDigitsOrEmpty(values.phoneNumber),
  firstName: values.firstName.trim(),
  lastName: values.lastName.trim(),
  nationalId: trimDigitsOrEmpty(values.nationalId),
  personnelCode: values.personnelCode.trim(),
  activeDuration: values.activeDuration,
  autoBackupEnabled: values.autoBackupEnabled,
  password: values.password,
  roles: ['Viewer'],
  twoFactorEnabled: values.twoFactorEnabled,
});

export const toUpdatePayload = (values: UserFormValues) => ({
  username: values.username.trim(),
  email: values.email.trim(),
  phoneNumber: trimDigitsOrEmpty(values.phoneNumber),
  firstName: values.firstName.trim(),
  lastName: values.lastName.trim(),
  nationalId: trimDigitsOrEmpty(values.nationalId),
  personnelCode: values.personnelCode.trim(),
  activeDuration: values.activeDuration,
  autoBackupEnabled: values.autoBackupEnabled,
  twoFactorEnabled: values.twoFactorEnabled,
});
