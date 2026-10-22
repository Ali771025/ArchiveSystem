import { zodResolver } from "@hookform/resolvers/zod";
import CloseIcon from "@mui/icons-material/Close";
import CorporateFareIcon from "@mui/icons-material/CorporateFare";
import PersonIcon from "@mui/icons-material/Person";
import {
  Box,
  Button,
  CircularProgress,
  Dialog,
  DialogContent,
  DialogTitle,
  IconButton,
  Stack,
  Tab,
  Tabs,
  TextField,
  Typography,
} from "@mui/material";
import { Controller, type Control, useForm } from "react-hook-form";
import { type HTMLAttributes, useEffect } from "react";
import { z } from "zod";
import { useTaxpayerByIdQuery } from "@/features/taxpayer-management/hooks/useTaxpayersQuery";
import type {
  CreateTaxpayerPayload,
  TaxpayerType,
  UpdateTaxpayerPayload,
} from "@/features/taxpayer-management/types";
import { mapApiError } from "@/services/errorMapper";
import "./TaxpayerFormModal.css";

const normalizeDigits = (value?: string) =>
  value
    ?.replace(/[۰-۹]/g, (digit) => String(digit.charCodeAt(0) - 1776))
    .replace(/[٠-٩]/g, (digit) => String(digit.charCodeAt(0) - 1632));

const mobileRegex = /^09[0-9]{9}$/;

const taxpayerFormSchema = z
  .object({
    taxpayerType: z.enum(["NaturalPerson", "LegalEntity"]),
    firstName: z
      .string()
      .trim()
      .max(100, "نام نباید بیش از ۱۰۰ کاراکتر باشد.")
      .optional(),
    lastName: z
      .string()
      .trim()
      .max(100, "نام خانوادگی نباید بیش از ۱۰۰ کاراکتر باشد.")
      .optional(),
    nationalId: z.string().trim().optional(),
    companyName: z
      .string()
      .trim()
      .max(100, "نام شرکت/سازمان نباید بیش از ۱۰۰ کاراکتر باشد.")
      .optional(),
    nationalCompanyId: z.string().trim().optional(),
    identityNumber: z
      .string()
      .trim()
      .max(50, "شماره شناسنامه نباید بیش از ۵۰ کاراکتر باشد.")
      .optional(),
    mobile: z.string().trim().optional(),
    landlinePhone: z
      .string()
      .trim()
      .max(20, "تلفن ثابت نباید بیش از ۲۰ کاراکتر باشد.")
      .optional(),
    postalCode: z.string().trim().optional(),
    address: z
      .string()
      .trim()
      .max(1000, "آدرس نباید بیش از ۱۰۰۰ کاراکتر باشد.")
      .optional(),
  })
  .superRefine((values, context) => {
    if (values.taxpayerType === "NaturalPerson") {
      if (!values.firstName?.trim()) {
        context.addIssue({
          code: z.ZodIssueCode.custom,
          path: ["firstName"],
          message: "نام الزامی است.",
        });
      }
      if (!values.lastName?.trim()) {
        context.addIssue({
          code: z.ZodIssueCode.custom,
          path: ["lastName"],
          message: "نام خانوادگی الزامی است.",
        });
      }
      if (!values.nationalId?.trim()) {
        context.addIssue({
          code: z.ZodIssueCode.custom,
          path: ["nationalId"],
          message: "کد ملی الزامی است.",
        });
      } else if (!/^[0-9۰-۹٠-٩]{10}$/.test(values.nationalId.trim())) {
        context.addIssue({
          code: z.ZodIssueCode.custom,
          path: ["nationalId"],
          message: "کد ملی باید دقیقاً ۱۰ رقم باشد.",
        });
      }
    }

    if (values.taxpayerType === "LegalEntity") {
      if (!values.companyName?.trim()) {
        context.addIssue({
          code: z.ZodIssueCode.custom,
          path: ["companyName"],
          message: "نام شرکت/سازمان الزامی است.",
        });
      }
      if (!values.nationalCompanyId?.trim()) {
        context.addIssue({
          code: z.ZodIssueCode.custom,
          path: ["nationalCompanyId"],
          message: "شناسه ملی الزامی است.",
        });
      } else if (!/^[0-9۰-۹٠-٩]{11}$/.test(values.nationalCompanyId.trim())) {
        context.addIssue({
          code: z.ZodIssueCode.custom,
          path: ["nationalCompanyId"],
          message: "شناسه ملی باید دقیقاً ۱۱ رقم باشد.",
        });
      }
    }

    if (
      values.postalCode?.trim() &&
      !/^[0-9۰-۹٠-٩]{10}$/.test(values.postalCode.trim())
    ) {
      context.addIssue({
        code: z.ZodIssueCode.custom,
        path: ["postalCode"],
        message: "کد پستی باید دقیقاً ۱۰ رقم باشد.",
      });
    }

    const normalizedMobile = normalizeDigits(values.mobile)?.trim();
    if (normalizedMobile && !mobileRegex.test(normalizedMobile)) {
      context.addIssue({
        code: z.ZodIssueCode.custom,
        path: ["mobile"],
        message: "شماره همراه باید با 09 شروع شود و دقیقاً 11 رقم باشد.",
      });
    }
  });

type TaxpayerFormValues = z.infer<typeof taxpayerFormSchema>;

interface TaxpayerFormModalProps {
  open: boolean;
  taxpayerId: string | null;
  onClose: () => void;
  onCreate: (payload: CreateTaxpayerPayload) => Promise<void>;
  onUpdate: (payload: UpdateTaxpayerPayload) => Promise<void>;
  isSubmitting: boolean;
}

const defaultValues: TaxpayerFormValues = {
  taxpayerType: "NaturalPerson",
  firstName: "",
  lastName: "",
  nationalId: "",
  companyName: "",
  nationalCompanyId: "",
  identityNumber: "",
  mobile: "",
  landlinePhone: "",
  postalCode: "",
  address: "",
};

const trimOrEmpty = (value?: string) => normalizeDigits(value)?.trim() ?? "";
const trimOrUndefined = (value?: string) => {
  const normalized = normalizeDigits(value)?.trim();
  return normalized ? normalized : undefined;
};

const toPayload = (values: TaxpayerFormValues): CreateTaxpayerPayload => {
  const sharedPayload = {
    taxpayerType: (values.taxpayerType === "LegalEntity"
      ? 2
      : 1) as TaxpayerType,
    mobile: trimOrUndefined(values.mobile),
    landlinePhone: trimOrUndefined(values.landlinePhone),
    postalCode: trimOrUndefined(values.postalCode),
    address: trimOrUndefined(values.address),
  };

  if (values.taxpayerType === "LegalEntity") {
    return {
      ...sharedPayload,
      companyName: trimOrEmpty(values.companyName),
      nationalCompanyId: trimOrEmpty(values.nationalCompanyId),
    };
  }

  return {
    ...sharedPayload,
    firstName: trimOrEmpty(values.firstName),
    lastName: trimOrEmpty(values.lastName),
    nationalId: trimOrEmpty(values.nationalId),
    identityNumber: trimOrUndefined(values.identityNumber),
  };
};

export const TaxpayerFormModal = ({
  open,
  taxpayerId,
  onClose,
  onCreate,
  onUpdate,
  isSubmitting,
}: TaxpayerFormModalProps) => {
  const isEditMode = Boolean(taxpayerId);
  const taxpayerQuery = useTaxpayerByIdQuery(taxpayerId, open && isEditMode);

  const {
    control,
    watch,
    handleSubmit,
    setValue,
    reset,
    setError,
    formState: { errors },
  } = useForm<TaxpayerFormValues>({
    resolver: zodResolver(taxpayerFormSchema),
    mode: "onChange",
    defaultValues,
  });

  const taxpayerType = watch("taxpayerType");

  useEffect(() => {
    if (!open) {
      reset(defaultValues);
      return;
    }

    if (!isEditMode) {
      reset(defaultValues);
      return;
    }

    if (taxpayerQuery.data) {
      reset({
        taxpayerType:
          taxpayerQuery.data.taxpayerType === "LegalEntity" ||
          taxpayerQuery.data.taxpayerType === 2
            ? "LegalEntity"
            : "NaturalPerson",
        firstName: taxpayerQuery.data.firstName ?? "",
        lastName: taxpayerQuery.data.lastName ?? "",
        nationalId: taxpayerQuery.data.nationalId ?? "",
        companyName: taxpayerQuery.data.companyName ?? "",
        nationalCompanyId: taxpayerQuery.data.nationalCompanyId ?? "",
        identityNumber: taxpayerQuery.data.identityNumber ?? "",
        mobile: taxpayerQuery.data.mobile ?? "",
        landlinePhone: taxpayerQuery.data.landlinePhone ?? "",
        postalCode: taxpayerQuery.data.postalCode ?? "",
        address: taxpayerQuery.data.address ?? "",
      });
    }
  }, [isEditMode, open, reset, taxpayerQuery.data]);

  const submit = handleSubmit(async (values) => {
    try {
      if (isEditMode) {
        await onUpdate(toPayload(values));
      } else {
        await onCreate(toPayload(values));
      }
      onClose();
    } catch (error) {
      const mapped = mapApiError(error);
      const detail =
        `${mapped.message ?? ""} ${mapped.details ?? ""}`.toLowerCase();
      if (
        mapped.status === 409 &&
        (detail.includes("شماره همراه") || detail.includes("mobile"))
      ) {
        setError("mobile", {
          message: mapped.message || "این شماره همراه قبلاً ثبت شده است.",
        });
      }
    }
  });

  const isLoadingDetails = isEditMode && taxpayerQuery.isLoading;

  const changeTaxpayerType = (newValue: TaxpayerFormValues["taxpayerType"]) => {
    setValue("taxpayerType", newValue, {
      shouldValidate: true,
      shouldDirty: true,
    });

    if (newValue === "LegalEntity") {
      setValue("firstName", "", { shouldValidate: true, shouldDirty: true });
      setValue("lastName", "", { shouldValidate: true, shouldDirty: true });
      setValue("nationalId", "", { shouldValidate: true, shouldDirty: true });
      setValue("identityNumber", "", {
        shouldValidate: true,
        shouldDirty: true,
      });
      return;
    }

    setValue("companyName", "", { shouldValidate: true, shouldDirty: true });
    setValue("nationalCompanyId", "", {
      shouldValidate: true,
      shouldDirty: true,
    });
  };

  return (
    <Dialog
      open={open}
      onClose={(_event, reason) => {
        if (reason === "backdropClick" || isSubmitting) {
          return;
        }

        onClose();
      }}
      disableEscapeKeyDown
      maxWidth="md"
      fullWidth
      PaperProps={{
        sx: {
          direction: "rtl",
          borderRadius: 3,
          overflow: "hidden",
          border: "1px solid",
          borderColor: "rgba(15, 23, 42, 0.08)",
          boxShadow: "0 24px 70px rgba(15, 23, 42, 0.18)",
        },
      }}
      slotProps={{
        backdrop: {
          sx: {
            backdropFilter: "blur(6px)",
            backgroundColor: "rgba(15, 23, 42, 0.32)",
          },
        },
      }}
    >
      <DialogTitle
        className="taxpayer-form-title"
        sx={{
          px: 3,
          pt: 2.5,
          pb: 1.5,
          display: "flex",
          alignItems: "flex-start",
          gap: 1.5,
          borderBottom: "1px solid #e5e7eb",
        }}
      >
        <Box sx={{ flex: 1, minWidth: 0 }}>
          <Typography
            variant="h6"
            sx={{ fontWeight: 700, color: "text.primary" }}
          >
            {isEditMode ? "ویرایش مودی" : "ایجاد مودی جدید"}
          </Typography>
          <Typography
            variant="body2"
            sx={{ mt: 0.75, color: "text.secondary" }}
          >
            لطفاً اطلاعات مودی را بر اساس نوع حقیقی یا حقوقی وارد کنید
          </Typography>
        </Box>
        <IconButton
          onClick={onClose}
          sx={{ flexShrink: 0, mt: -0.5 }}
          aria-label="بستن"
          disabled={isSubmitting}
        >
          <CloseIcon />
        </IconButton>
      </DialogTitle>

      <DialogContent className="taxpayer-form-content" sx={{ px: 3, py: 2.5 }}>
        {isLoadingDetails ? (
          <Stack
            alignItems="center"
            justifyContent="center"
            sx={{ minHeight: 240 }}
          >
            <CircularProgress />
          </Stack>
        ) : (
          <>
            <Box
              sx={{
                p: 0.5,
                borderRadius: 2,
                backgroundColor: "action.hover",
                border: "1px solid #e2e8f0",
              }}
            >
              <Tabs
                value={taxpayerType}
                onChange={(
                  _event,
                  newValue: TaxpayerFormValues["taxpayerType"],
                ) => changeTaxpayerType(newValue)}
                variant="fullWidth"
                sx={{
                  minHeight: 44,
                  "& .MuiTabs-indicator": { display: "none" },
                }}
              >
                <Tab
                  icon={<PersonIcon fontSize="small" />}
                  iconPosition="start"
                  value="NaturalPerson"
                  label="شخص حقیقی"
                  sx={segmentedTabSx(taxpayerType === "NaturalPerson")}
                />
                <Tab
                  icon={<CorporateFareIcon fontSize="small" />}
                  iconPosition="start"
                  value="LegalEntity"
                  label="شخص حقوقی"
                  sx={segmentedTabSx(taxpayerType === "LegalEntity")}
                />
              </Tabs>
            </Box>

            <Box
              component="form"
              id="taxpayer-form"
              onSubmit={submit}
              className="taxpayer-rtl-form mt-5 grid grid-cols-1 gap-4 md:grid-cols-2"
            >
              {taxpayerType === "NaturalPerson" ? (
                <>
                  <FormTextField
                    control={control}
                    name="firstName"
                    label="نام"
                    placeholder="مثال: علی"
                    errorMessage={errors.firstName?.message}
                  />
                  <FormTextField
                    control={control}
                    name="lastName"
                    label="نام خانوادگی"
                    placeholder="مثال: رضایی"
                    errorMessage={errors.lastName?.message}
                  />
                  <FormTextField
                    control={control}
                    name="nationalId"
                    label="کد ملی"
                    placeholder="0012345678"
                    inputMode="numeric"
                    errorMessage={errors.nationalId?.message}
                  />
                  <FormTextField
                    control={control}
                    name="identityNumber"
                    label="شماره شناسنامه"
                    placeholder="123456"
                    inputMode="numeric"
                    errorMessage={errors.identityNumber?.message}
                  />
                </>
              ) : (
                <>
                  <FormTextField
                    control={control}
                    name="companyName"
                    label="نام سازمان/شرکت"
                    placeholder="مثال: شرکت توسعه آرشیو"
                    errorMessage={errors.companyName?.message}
                  />
                  <FormTextField
                    control={control}
                    name="nationalCompanyId"
                    label="شناسه ملی"
                    placeholder="10101234567"
                    inputMode="numeric"
                    errorMessage={errors.nationalCompanyId?.message}
                  />
                </>
              )}

              <FormTextField
                control={control}
                name="mobile"
                label="شماره همراه"
                placeholder="09121234567"
                inputMode="tel"
                errorMessage={errors.mobile?.message}
              />
              <FormTextField
                control={control}
                name="landlinePhone"
                label="تلفن ثابت"
                placeholder="02112345678"
                inputMode="tel"
                errorMessage={errors.landlinePhone?.message}
              />
              <FormTextField
                control={control}
                name="postalCode"
                label="کد پستی"
                placeholder="1234567890"
                inputMode="numeric"
                errorMessage={errors.postalCode?.message}
              />
              <Box className="md:col-span-2">
                <FormTextField
                  control={control}
                  name="address"
                  label="آدرس"
                  placeholder="آدرس کامل را وارد کنید"
                  multiline
                  minRows={3}
                  errorMessage={errors.address?.message}
                />
              </Box>
            </Box>

            <Box className="mt-6 border-t border-slate-200 pt-4">
              <Box
                sx={{ display: "flex", justifyContent: "flex-start", gap: 1.5 }}
              >
                <Button
                  type="submit"
                  form="taxpayer-form"
                  variant="contained"
                  disabled={isSubmitting || isLoadingDetails}
                  sx={{ minWidth: 130, fontWeight: 700 }}
                >
                  {isSubmitting ? (
                    <Box
                      sx={{
                        display: "inline-flex",
                        alignItems: "center",
                        gap: 1,
                      }}
                    >
                      <CircularProgress size={16} color="inherit" />
                      {isEditMode ? "در حال ذخیره..." : "در حال ثبت..."}
                    </Box>
                  ) : isEditMode ? (
                    "ذخیره تغییرات"
                  ) : (
                    "ثبت مودی"
                  )}
                </Button>
                <Button
                  onClick={onClose}
                  variant="outlined"
                  color="inherit"
                  sx={{ minWidth: 110 }}
                  disabled={isSubmitting}
                >
                  انصراف
                </Button>
              </Box>
            </Box>
          </>
        )}
      </DialogContent>
    </Dialog>
  );
};

const segmentedTabSx = (selected: boolean) => ({
  minHeight: 40,
  borderRadius: 1.5,
  textTransform: "none",
  fontWeight: 700,
  color: selected ? "text.primary" : "text.secondary",
  backgroundColor: selected ? "background.paper" : "transparent",
  border: selected ? "1px solid #cbd5e1" : "1px solid transparent",
  boxShadow: selected ? "0 4px 14px rgba(15, 23, 42, 0.10)" : "none",
});

interface FormTextFieldProps {
  control: Control<TaxpayerFormValues>;
  name: keyof TaxpayerFormValues;
  label: string;
  placeholder?: string;
  inputMode?: HTMLAttributes<HTMLInputElement>["inputMode"];
  multiline?: boolean;
  minRows?: number;
  errorMessage?: string;
}

const FormTextField = ({
  control,
  name,
  label,
  placeholder,
  inputMode,
  multiline,
  minRows,
  errorMessage,
}: FormTextFieldProps) => {
  const resolvedDir = "rtl";
  const resolvedTextAlign = "right";

  return (
    <Controller
      control={control}
      name={name}
      render={({ field }) => (
        <TextField
          {...field}
          value={(field.value as string | undefined) ?? ""}
          fullWidth
          variant="outlined"
          label={label}
          placeholder={placeholder}
          inputMode={inputMode}
          dir={resolvedDir}
          className="taxpayer-rtl-field"
          inputProps={{
            dir: resolvedDir,
            style: {
              direction: resolvedDir,
              textAlign: resolvedTextAlign,
            },
          }}
          slotProps={{
            htmlInput: {
              dir: resolvedDir,
              style: {
                direction: resolvedDir,
                textAlign: resolvedTextAlign,
              },
            },
          }}
          multiline={multiline}
          minRows={minRows}
          error={Boolean(errorMessage)}
          helperText={errorMessage ?? " "}
          sx={{
            direction: resolvedDir,
            "& .MuiInputBase-input, & input, & textarea": {
              direction: `${resolvedDir} !important`,
              textAlign: `${resolvedTextAlign} !important`,
            },
            "& textarea": {
              unicodeBidi: "plaintext",
            },
          }}
          InputLabelProps={{
            shrink: true,
            sx: {
              right: 14,
              left: "auto",
              px: 0.75,
              backgroundColor: "background.paper",
              transformOrigin: "top right",
              "&.MuiInputLabel-shrink": {
                transform: "translate(0, -9px) scale(0.75)",
              },
            },
          }}
          InputProps={{
            sx: {
              direction: resolvedDir,
              borderRadius: 1.75,
              backgroundColor: "background.paper",
              mt: 0.75,
              "& .MuiInputBase-input": {
                direction: `${resolvedDir} !important`,
                textAlign: `${resolvedTextAlign} !important`,
              },
            },
          }}
        />
      )}
    />
  );
};
