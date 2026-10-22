import { AxiosError } from "axios";
import type { ApiErrorPayload, AppError } from "@/types/archive";

const fallbackMessage = "خطا در ارتباط با سرور. لطفا مجددا تلاش کنید.";
const databaseUnavailableMessage =
  "ارتباط با پایگاه داده برقرار نشد. لطفا اتصال دیتابیس را بررسی کنید و دوباره تلاش کنید.";
export const documentCreateForbiddenMessage = "شما مجوز ایجاد سند را ندارید.";
export const dynamicTemplateWriteForbiddenMessage =
  "شما مجوز ایجاد یا ویرایش اطلاعات تکمیلی در این پوشه را ندارید.";
export const caseDynamicFieldsForbiddenMessage =
  "شما مجاز به مشاهده/ویرایش اطلاعات تکمیلی این پوشه نیستید.";
export const readOnlyClassificationMessage =
  "شما برای این طبقه‌بندی فقط مجوز مشاهده دارید؛ عملیات ویرایش یا حذف مجاز نیست.";
export const stagnantArchiveReadOnlyMessage =
  "دسترسی این نقش به «بایگانی راکد» فقط در سطح مشاهده است؛ عملیات ویرایش، حذف، بارگذاری و انتقال مجاز نیست.";
const offlineFallbackEnabled =
  (import.meta.env.VITE_ENABLE_OFFLINE_FALLBACK ?? "false").toLowerCase() ===
  "true";

const normalizeUserMessage = (message?: string): string => {
  const normalized = message?.trim();

  if (!normalized) {
    return fallbackMessage;
  }

  const lowerMessage = normalized.toLowerCase();
  if (
    lowerMessage === "network error" ||
    lowerMessage.includes("database") ||
    lowerMessage.includes("sql server") ||
    lowerMessage.includes("connection refused") ||
    lowerMessage.includes("failed to fetch")
  ) {
    return databaseUnavailableMessage;
  }

  const duplicateCaseNumberMatch = normalized.match(
    /^case number '(.+)' already exists\.?$/i,
  );
  if (duplicateCaseNumberMatch) {
    return `شماره پرونده «${duplicateCaseNumberMatch[1]}» قبلاً ثبت شده است.`;
  }

  const duplicateEmailMatch = normalized.match(
    /^email ['«](.+?)['»] is already in use\.?$/i,
  );
  if (duplicateEmailMatch) {
    return `ایمیل «${duplicateEmailMatch[1]}» قبلاً ثبت شده است.`;
  }

  const duplicateUsernameMatch = normalized.match(
    /^username ['«](.+?)['»] is already in use\.?$/i,
  );
  if (duplicateUsernameMatch) {
    return `نام کاربری «${duplicateUsernameMatch[1]}» قبلاً ثبت شده است.`;
  }

  const duplicateNationalIdMatch = normalized.match(
    /^nationalid ['«](.+?)['»] is already in use\.?$/i,
  );
  if (duplicateNationalIdMatch) {
    return `کد ملی «${duplicateNationalIdMatch[1]}» قبلاً ثبت شده است.`;
  }

  const duplicatePersonnelCodeMatch = normalized.match(
    /^personnelcode ['«](.+?)['»] is already in use\.?$/i,
  );
  if (duplicatePersonnelCodeMatch) {
    return `کد پرسنلی «${duplicatePersonnelCodeMatch[1]}» قبلاً ثبت شده است.`;
  }

  const translations: Record<string, string> = {
    "one or more validation failures have occurred.":
      "یک یا چند خطای اعتبارسنجی رخ داده است.",
    "validation failed.": "اعتبارسنجی ناموفق بود.",
    "invalid credentials": "نام کاربری یا گذرواژه درست نیست.",
    "نام کاربری وارد شده یافت نشد.": "نام کاربری وارد شده یافت نشد.",
    "حساب کاربری به دلیل تلاش‌های ناموفق مکرر موقتاً قفل شده است. لطفاً بعداً دوباره تلاش کنید.":
      "حساب کاربری به دلیل تلاش‌های ناموفق مکرر موقتاً قفل شده است. لطفاً بعداً دوباره تلاش کنید.",
    "حساب کاربری به دلیل تلاش‌های ناموفق مکرر موقتاً قفل شد. لطفاً بعداً دوباره تلاش کنید.":
      "حساب کاربری به دلیل تلاش‌های ناموفق مکرر موقتاً قفل شد. لطفاً بعداً دوباره تلاش کنید.",
    "username/email and password are required.":
      "نام کاربری و گذرواژه را وارد کنید.",
    "account is deactivated": "حساب کاربری شما غیرفعال است.",
    "invalid token": "نشست شما معتبر نیست. لطفا دوباره وارد شوید.",
    "username already exists": "نام کاربری وارد شده قبلا ثبت شده است.",
    "username is already taken.": "نام کاربری وارد شده قبلا ثبت شده است.",
    "email is already taken.": "این ایمیل قبلاً استفاده شده است.",
    "invalid email.": "ایمیل معتبر نیست.",
    "email is invalid.": "ایمیل معتبر نیست.",
    "the email field is not a valid e-mail address.": "ایمیل معتبر نیست.",
    "email is not a valid email address.": "ایمیل معتبر نیست.",
    "registration failed": "ثبت نام انجام نشد.",
    "registration failed.": "ثبت نام انجام نشد.",
    "user was created but cannot be loaded.":
      "کاربر ایجاد شد اما امکان بارگذاری اطلاعات او وجود ندارد.",
    "an error occurred during registration": "خطایی هنگام ثبت نام رخ داد.",
    "an error occurred during login": "خطایی هنگام ورود رخ داد.",
    "an error occurred during token refresh": "خطایی هنگام تمدید نشست رخ داد.",
    "invalid username or password.": "نام کاربری یا گذرواژه درست نیست.",
    "request limit reached. please try again later.":
      "تعداد درخواست ها زیاد است. لطفا کمی بعد دوباره تلاش کنید.",
    "all fields are required.": "همه فیلدها را وارد کنید.",
    "passwords do not match.": "تکرار گذرواژه با گذرواژه یکسان نیست.",
    "username or email is required.": "نام کاربری یا ایمیل را وارد کنید.",
    "if the account exists, reset instructions have been sent.":
      "اگر حساب کاربری وجود داشته باشد، راهنمای بازیابی ارسال می شود.",
    "if the account exists, an otp has been sent.":
      "اگر حساب کاربری وجود داشته باشد، کد تایید ارسال شده است.",
    "a category with the same title already exists at this level.":
      "در این سطح، پوشه‌ای با این نام قبلاً ایجاد شده است.",
    "a category with the same title already exists under the target parent.":
      "در پوشه مقصد، پوشه‌ای با این نام قبلاً ایجاد شده است.",
  };

  return translations[lowerMessage] ?? normalized;
};

const normalizeForbiddenMessage = (message?: string): string => {
  const normalized = message?.trim();
  const lowerMessage = normalized?.toLowerCase() ?? "";

  if (!normalized) {
    return "اجازه انجام این عملیات را ندارید. دسترسی نقش و طبقه‌بندی خود را بررسی کنید.";
  }

  if (/^[\u0600-\u06ff]/.test(normalized)) {
    return normalized;
  }

  if (
    lowerMessage.includes("upload to this case") ||
    lowerMessage.includes("upload to this classification") ||
    lowerMessage.includes("create document")
  ) {
    return documentCreateForbiddenMessage;
  }

  if (
    lowerMessage.includes("read case dynamic fields") ||
    lowerMessage.includes("write case dynamic fields") ||
    lowerMessage.includes("modify case dynamic fields")
  ) {
    return caseDynamicFieldsForbiddenMessage;
  }

  if (lowerMessage.includes("move this case from source classification")) {
    return "مجوز مستقل انتقال پرونده از طبقه‌بندی مبدأ برای شما فعال نیست.";
  }

  if (lowerMessage.includes("move this case to target classification")) {
    return "مجوز مستقل انتقال پرونده به طبقه‌بندی مقصد برای شما فعال نیست.";
  }

  if (
    lowerMessage.includes("archive") ||
    lowerMessage.includes("delete this case")
  ) {
    return "شما فقط مجوز مشاهده این طبقه‌بندی را دارید و امکان بایگانی پرونده برای شما فعال نیست.";
  }

  if (
    lowerMessage.includes("modify this file") ||
    lowerMessage.includes("modify this upload session") ||
    lowerMessage.includes("write this classification")
  ) {
    return readOnlyClassificationMessage;
  }

  return "اجازه انجام این عملیات را ندارید. دسترسی نقش و طبقه‌بندی خود را بررسی کنید.";
};

export const mapApiError = (error: unknown): AppError => {
  if (error instanceof AxiosError) {
    if (!error.response) {
      return { status: 0, message: databaseUnavailableMessage };
    }

    const payload = (error.response?.data ?? {}) as ApiErrorPayload;
    const rawMessage = payload.detail ?? payload.message ?? payload.title;
    const message =
      error.response.status === 403
        ? normalizeForbiddenMessage(rawMessage)
        : normalizeUserMessage(rawMessage);
    const fieldErrors = payload.errors
      ? Object.fromEntries(
          Object.entries(payload.errors).map(([field, messages]) => [
            field,
            messages.map((item) => normalizeUserMessage(item)),
          ]),
        )
      : undefined;

    return {
      status: error.response?.status ?? 0,
      message,
      details: payload.detail
        ? error.response.status === 403
          ? normalizeForbiddenMessage(payload.detail)
          : normalizeUserMessage(payload.detail)
        : undefined,
      traceId: payload.correlationId ?? payload.traceId,
      fieldErrors,
    };
  }

  if (error instanceof Error) {
    return { status: 0, message: normalizeUserMessage(error.message) };
  }

  return { status: 0, message: fallbackMessage };
};

export const canUseOfflineFallback = (error: unknown): boolean => {
  if (!offlineFallbackEnabled) {
    return false;
  }

  if (!(error instanceof AxiosError)) {
    return false;
  }

  if (!error.response) {
    return true;
  }

  return error.response.status >= 500;
};
