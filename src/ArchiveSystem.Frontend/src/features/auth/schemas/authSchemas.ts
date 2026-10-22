import { z } from 'zod';

export const iranianMobileRegex = /^(?:\+98|0)?9\d{9}$/;
const strongPasswordRegex = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z\d]).{12,64}$/;

export const normalizeIranianPhone = (value: string): string => {
  const compact = value.replace(/[\s-]+/g, '');

  if (compact.startsWith('+98')) {
    return `0${compact.slice(3)}`;
  }

  if (compact.startsWith('98') && compact.length === 12) {
    return `0${compact.slice(2)}`;
  }

  return compact;
};

export const loginSchema = z.object({
  username: z.string().trim().min(3, 'نام کاربری حداقل 3 کاراکتر باشد.').max(64),
  password: z.string().min(8, 'گذرواژه حداقل 8 کاراکتر باشد.').max(128),
});

export const requestOtpSchema = z.object({
  phoneNumber: z
    .string()
    .trim()
    .transform(normalizeIranianPhone)
    .refine((value) => iranianMobileRegex.test(value), 'شماره موبایل ایران معتبر نیست.'),
});

export const verifyOtpSchema = z.object({
  otp: z.string().regex(/^\d{5}$/, 'رمز یکبار مصرف باید 5 رقم باشد.'),
});

export const resetPasswordSchema = z
  .object({
    newPassword: z
      .string()
      .min(12, 'گذرواژه جدید باید حداقل 12 کاراکتر باشد.')
      .max(64, 'گذرواژه جدید حداکثر 64 کاراکتر باشد.')
      .regex(
        strongPasswordRegex,
        'گذرواژه باید شامل حروف بزرگ، حروف کوچک، عدد و نماد باشد.',
      ),
    confirmPassword: z.string().min(1, 'تکرار گذرواژه الزامی است.'),
  })
  .superRefine((value, ctx) => {
    if (value.newPassword !== value.confirmPassword) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        path: ['confirmPassword'],
        message: 'تکرار گذرواژه با گذرواژه جدید یکسان نیست.',
      });
    }
  });

export type LoginSchema = z.infer<typeof loginSchema>;
export type RequestOtpSchema = z.infer<typeof requestOtpSchema>;
export type VerifyOtpSchema = z.infer<typeof verifyOtpSchema>;
export type ResetPasswordSchema = z.infer<typeof resetPasswordSchema>;
