import { z } from 'zod';

export const iranianMobileRegex = /^(?:\+98|0)?9\d{9}$/;

export const normalizeIranianMobile = (input: string): string => {
  const compact = input.replace(/[\s-]+/g, '').trim();

  if (compact.startsWith('+98')) {
    return `0${compact.slice(3)}`;
  }

  if (compact.startsWith('98') && compact.length === 12) {
    return `0${compact.slice(2)}`;
  }

  return compact;
};

export const usernamePasswordSchema = z.object({
  username: z.string().trim().min(1, 'نام کاربری الزامی است.'),
  password: z.string().min(1, 'گذرواژه الزامی است.'),
});

export const enterMobileSchema = z.object({
  mobileNumber: z
    .string()
    .trim()
    .transform(normalizeIranianMobile)
    .refine((value) => iranianMobileRegex.test(value), 'شماره موبایل ایران معتبر نیست.'),
});

export const verifyOtpSchema = z.object({
  otpCode: z.string().regex(/^\d{5}$/, 'کد تایید باید ۵ رقم باشد.'),
});

export type UsernamePasswordSchema = z.infer<typeof usernamePasswordSchema>;
export type EnterMobileSchema = z.infer<typeof enterMobileSchema>;
export type VerifyOtpSchema = z.infer<typeof verifyOtpSchema>;
