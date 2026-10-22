import DriveFileMoveIcon from "@mui/icons-material/DriveFileMove";
import FolderOutlinedIcon from "@mui/icons-material/FolderOutlined";
import DownloadIcon from "@mui/icons-material/Download";
import PhotoCameraIcon from "@mui/icons-material/PhotoCamera";
import SaveIcon from "@mui/icons-material/Save";
import CloseRoundedIcon from "@mui/icons-material/CloseRounded";
import {
  Alert,
  Avatar,
  Box,
  Button,
  CircularProgress,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Divider,
  List,
  ListItem,
  ListItemText,
  Paper,
  Skeleton,
  Stack,
  Tab,
  Tabs,
  TextField,
  Typography,
  useMediaQuery,
  useTheme,
} from "@mui/material";
import { ChangeEvent, useEffect, useMemo, useRef, useState } from "react";
import {
  useBehsazConfig,
  useCaseMoveAccessIds,
  useCaseDetails,
  useCaseDynamicValues,
  useEffectiveDynamicTemplate,
  useMoveCase,
  useSaveCaseDynamicValues,
  useUploadCaseAvatar,
  useUpdateCase,
  useWritableClassificationIds,
} from "@/services/archiveQueries";
import { useSessionStore } from "@/stores/useSessionStore";
import { useToastStore } from "@/stores/useToastStore";
import type {
  CaseDetails,
  CaseDynamicValue,
  ClassificationNode,
} from "@/types/archive";
import {
  DynamicFormRenderer,
  validateDynamicValues,
} from "@/components/dynamic/DynamicFormRenderer";
import { FileManager } from "@/components/files/FileManager";
import { BehsazPopup } from "@/components/integration/BehsazPopup";
import { DossierOwnersTab } from "@/components/owners/DossierOwnersTab";
import { resolveMediaUrl } from "@/utils/mediaUrl";
import { useDebouncedValue } from "@/utils/useDebouncedValue";
import { archiveApi } from "@/services/archiveApi";
import {
  mapApiError,
  stagnantArchiveReadOnlyMessage,
} from "@/services/errorMapper";
import { useConfirm } from "@/components/common/useConfirm";
import { formatServerDateTime } from "@/utils/serverDateTime";

interface CaseDetailsTabsProps {
  caseId: number;
  classifications: ClassificationNode[];
  onCancel: () => void;
}

const getActionTypeLabel = (actionType: string): string => {
  const normalized = actionType.trim().toLowerCase();

  if (normalized.includes("create")) {
    return "ایجاد";
  }
  if (normalized.includes("update") || normalized.includes("edit")) {
    return "ویرایش";
  }
  if (normalized.includes("delete") || normalized.includes("remove")) {
    return "حذف";
  }
  if (normalized.includes("move") || normalized.includes("transfer")) {
    return "انتقال";
  }
  if (normalized.includes("upload")) {
    return "بارگذاری";
  }
  if (normalized.includes("download")) {
    return "دانلود";
  }
  if (normalized.includes("login") || normalized.includes("signin")) {
    return "ورود";
  }
  if (normalized.includes("logout") || normalized.includes("signout")) {
    return "خروج";
  }

  return actionType;
};

export const CaseDetailsTabs = ({
  caseId,
  classifications,
  onCancel,
}: CaseDetailsTabsProps) => {
  const theme = useTheme();
  const isCompact = useMediaQuery(theme.breakpoints.down("md"));
  const [activeTab, setActiveTab] = useState(0);
  const [isExportingExcel, setIsExportingExcel] = useState(false);
  const [editModel, setEditModel] = useState<CaseDetails | null>(null);
  const [dynamicValues, setDynamicValues] = useState<CaseDynamicValue[]>([]);
  const [moveDialogOpen, setMoveDialogOpen] = useState(false);
  const [targetClassification, setTargetClassification] = useState<
    number | null
  >(null);
  const [moveSearch, setMoveSearch] = useState("");
  const [behsazOpen, setBehsazOpen] = useState(false);
  const [avatarCacheBuster, setAvatarCacheBuster] = useState(() => Date.now());
  const avatarInputRef = useRef<HTMLInputElement | null>(null);

  const detailsQuery = useCaseDetails(caseId);
  const updateMutation = useUpdateCase();
  const moveMutation = useMoveCase();
  const saveDynamicMutation = useSaveCaseDynamicValues();
  const uploadAvatarMutation = useUploadCaseAvatar();
  const pushToast = useToastStore((state) => state.push);
  const behsazConfigQuery = useBehsazConfig();
  const isBehsazIntegrationEnabled = behsazConfigQuery.data === true;
  const confirm = useConfirm();

  const details = detailsQuery.data;
  const templateQuery = useEffectiveDynamicTemplate(details?.classificationId ?? null);
  const dynamicValuesQuery = useCaseDynamicValues(caseId);
  const {
    data: writableClassificationIds = [],
    isLoading: isWritableClassificationsLoading,
  } = useWritableClassificationIds();
  const { data: movableClassificationIds = [] } = useCaseMoveAccessIds();
  const canWrite = Boolean(
    editModel && writableClassificationIds.includes(editModel.classificationId),
  );
  const canMoveFromCurrentClassification = Boolean(
    editModel && movableClassificationIds.includes(editModel.classificationId),
  );
  const hasDossierManagementPermission = useSessionStore((state) =>
    state.hasPermission("dossiers.manage"),
  );
  const hasTaxpayerManagementPermission = useSessionStore((state) =>
    state.hasPermission("taxpayers.manage"),
  );
  const canManageDossier = hasDossierManagementPermission && canWrite;
  const canReadFiles = useSessionStore((state) =>
    state.hasPermission("files.read"),
  );
  const leafClassificationSet = useMemo(() => {
    const activeNodes = classifications.filter((node) => node.isActive);
    const parentIds = new Set(
      activeNodes
        .filter((node) => node.parentId != null)
        .map((node) => node.parentId as number),
    );

    return new Set(
      activeNodes
        .filter((node) => !parentIds.has(node.id))
        .map((node) => node.id),
    );
  }, [classifications]);
  const moveTargetClassifications = useMemo(
    () =>
      classifications.filter(
        (node) =>
          node.isActive &&
          node.id !== editModel?.classificationId &&
          leafClassificationSet.has(node.id) &&
          movableClassificationIds.includes(node.id),
      ),
    [
      classifications,
      editModel?.classificationId,
      leafClassificationSet,
      movableClassificationIds,
    ],
  );

  useEffect(() => {
    if (details) {
      setEditModel(details);
      setTargetClassification(details.classificationId);
    }
  }, [details]);

  useEffect(() => {
    if (dynamicValuesQuery.data) {
      setDynamicValues(dynamicValuesQuery.data);
    }
  }, [dynamicValuesQuery.data]);

  const canMoveCurrentCase = Boolean(
    canMoveFromCurrentClassification &&
      editModel &&
      moveTargetClassifications.length > 0,
  );
  const selectedMoveTargetIsValid = moveTargetClassifications.some(
    (classification) => classification.id === targetClassification,
  );

  const saveOverview = async () => {
    if (!editModel) {
      return false;
    }

    try {
      const updated = await updateMutation.mutateAsync({
        ...editModel,
        caseNumber: details?.caseNumber ?? editModel.caseNumber,
        externalCaseNumber: editModel.externalCaseNumber.trim(),
      });
      setEditModel(updated);
      await detailsQuery.refetch();
      pushToast("اطلاعات پرونده ذخیره شد.", "success");
      return true;
    } catch (error) {
      pushToast(
        mapApiError(error).message || "ذخیره اطلاعات پرونده ناموفق بود.",
        "error",
      );
      return false;
    }
  };

  const saveDynamic = async () => {
    const errors = validateDynamicValues(
      templateQuery.data ?? null,
      dynamicValues,
    );
    if (Object.keys(errors).length > 0) {
      pushToast("اعتبارسنجی اطلاعات تکمیلی ناموفق بود.", "error");
      return false;
    }

    try {
      const savedValues = await saveDynamicMutation.mutateAsync({
        caseId,
        values: dynamicValues,
      });
      setDynamicValues(savedValues);
      pushToast("اطلاعات تکمیلی ذخیره شد.", "success");
      return true;
    } catch (error) {
      pushToast(
        mapApiError(error).message || "ذخیره اطلاعات تکمیلی ناموفق بود.",
        "error",
      );
      return false;
    }
  };

  const downloadExcel = async () => {
    if (!editModel || isExportingExcel) return;

    setIsExportingExcel(true);
    try {
      await archiveApi.downloadCaseWorkbook(caseId, editModel.caseNumber);
      pushToast("خروجی اکسل پرونده دریافت شد.", "success");
    } catch (error) {
      pushToast(
        mapApiError(error).message || "دریافت خروجی اکسل ناموفق بود.",
        "error",
      );
    } finally {
      setIsExportingExcel(false);
    }
  };

  const doMoveCase = async () => {
    if (!editModel || !targetClassification) {
      return;
    }

    if (!canMoveFromCurrentClassification) {
      pushToast("مجوز انتقال پرونده از طبقه‌بندی مبدأ برای شما فعال نیست.", "warning");
      return;
    }

    if (
      !moveTargetClassifications.some(
        (item) => item.id === targetClassification,
      )
    ) {
      pushToast("دسته‌بندی مقصد انتخاب‌شده برای انتقال مجاز نیست.", "warning");
      return;
    }

    await moveMutation.mutateAsync({
      caseId,
      sourceClassificationId: editModel.classificationId,
      classificationId: targetClassification,
    });
    setEditModel((current) =>
      current
        ? {
            ...current,
            classificationId: targetClassification,
            classificationTitle:
              classifications.find((item) => item.id === targetClassification)
                ?.title ?? current.classificationTitle,
          }
        : current,
    );
    pushToast("پرونده با موفقیت منتقل شد.", "success");
    setMoveDialogOpen(false);
  };

  const debouncedMoveSearch = useDebouncedValue(moveSearch);
  const moveCandidates = useMemo(() => {
    if (!debouncedMoveSearch.trim()) {
      return moveTargetClassifications;
    }

    const needle = debouncedMoveSearch.toLowerCase();
    return moveTargetClassifications.filter((item) =>
      `${item.title} ${item.path}`.toLowerCase().includes(needle),
    );
  }, [moveTargetClassifications, debouncedMoveSearch]);

  const avatarInitials = useMemo(() => {
    if (!editModel) {
      return "";
    }

    const source =
      `${editModel.externalCaseNumber || editModel.caseNumber}`.trim();
    if (!source) {
      return "";
    }

    const compact = source.replace(/\s+/g, "");
    return compact.slice(0, 2).toUpperCase();
  }, [editModel]);

  const avatarBaseUrl = resolveMediaUrl(editModel?.avatarImagePath ?? null);
  const avatarUrl = avatarBaseUrl
    ? `${avatarBaseUrl}${avatarBaseUrl.includes("?") ? "&" : "?"}v=${avatarCacheBuster}`
    : null;
  const currentClassification = classifications.find(
    (item) => item.id === editModel?.classificationId,
  );
  const isStagnantArchive =
    currentClassification?.systemKey === "ArchivedCases";
  const overviewHasChanges = Boolean(
    details &&
      editModel &&
      (editModel.externalCaseNumber.trim() !==
        details.externalCaseNumber.trim() ||
        editModel.address !== details.address ||
        (editModel.postalCode ?? "") !== (details.postalCode ?? "")),
  );
  const dynamicValuesHaveChanges = useMemo(() => {
    const normalize = (values: CaseDynamicValue[]) =>
      [...values]
        .sort((left, right) => left.fieldId.localeCompare(right.fieldId))
        .map(({ fieldId, value, values: selectedValues }) => ({
          fieldId,
          value,
          values: selectedValues,
        }));

    return (
      JSON.stringify(normalize(dynamicValues)) !==
      JSON.stringify(normalize(dynamicValuesQuery.data ?? []))
    );
  }, [dynamicValues, dynamicValuesQuery.data]);
  const hasUnsavedChanges = overviewHasChanges || dynamicValuesHaveChanges;
  const isSaving =
    updateMutation.isPending || saveDynamicMutation.isPending;

  const saveAllChanges = async () => {
    if (!canWrite || isSaving || !hasUnsavedChanges) {
      return;
    }

    if (overviewHasChanges) {
      const overviewSaved = await saveOverview();
      if (!overviewSaved) {
        return;
      }
    }

    if (dynamicValuesHaveChanges) {
      await saveDynamic();
    }
  };

  const cancelAndReturnToList = async () => {
    if (isSaving) {
      return;
    }

    if (hasUnsavedChanges) {
      const shouldLeave = await confirm({
        title: "خروج بدون ذخیره",
        message:
          "تغییرات ذخیره‌نشده وجود دارد. آیا می‌خواهید بدون ذخیره به فهرست پرونده‌ها بازگردید؟",
        confirmText: "خروج بدون ذخیره",
        cancelText: "ادامه ویرایش",
        variant: "danger",
      });
      if (!shouldLeave) {
        return;
      }
    }

    onCancel();
  };

  const triggerAvatarPicker = () => {
    if (!canManageDossier || uploadAvatarMutation.isPending) {
      return;
    }

    avatarInputRef.current?.click();
  };

  const onAvatarFileSelected = async (event: ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) {
      return;
    }

    if (!["image/jpeg", "image/jpg", "image/png"].includes(file.type)) {
      pushToast("فقط فایل‌های JPG و PNG مجاز هستند.", "error");
      event.target.value = "";
      return;
    }

    if (file.size > 2 * 1024 * 1024) {
      pushToast("حجم تصویر باید حداکثر 2 مگابایت باشد.", "error");
      event.target.value = "";
      return;
    }

    try {
      const result = await uploadAvatarMutation.mutateAsync({ caseId, file });
      setEditModel((current) =>
        current
          ? { ...current, avatarImagePath: result.avatarImagePath }
          : current,
      );
      setAvatarCacheBuster(Date.now());
      pushToast("تصویر پرونده با موفقیت بارگذاری شد.", "success");
    } catch {
      pushToast("بارگذاری تصویر پرونده ناموفق بود.", "error");
    } finally {
      event.target.value = "";
    }
  };

  if (detailsQuery.isLoading || !editModel) {
    return (
      <Stack spacing={1}>
        <Skeleton variant="rounded" height={36} />
        <Skeleton variant="rounded" height={42} />
        <Skeleton variant="rounded" height={160} />
      </Stack>
    );
  }

  return (
    <Stack
      spacing={1.5}
      sx={{
        width: "100%",
        maxWidth: "100%",
        minWidth: 0,
        overflowX: "hidden",
        pb: 11,
      }}
    >
      <input
        ref={avatarInputRef}
        type="file"
        accept=".jpg,.jpeg,.png,image/jpeg,image/png"
        hidden
        onChange={onAvatarFileSelected}
      />

      {!isWritableClassificationsLoading && !canWrite && (
        <Alert severity="warning">
          {isStagnantArchive
            ? stagnantArchiveReadOnlyMessage
            : "دسترسی شما به این طبقه‌بندی فقط در سطح مشاهده است؛ عملیات ویرایش، حذف و انتقال مجاز نیست."}
        </Alert>
      )}

      <Stack direction="row" spacing={1.5} alignItems="center">
        <Box
          onClick={triggerAvatarPicker}
          sx={{
            position: "relative",
            width: 96,
            height: 96,
            cursor: canManageDossier ? "pointer" : "default",
          }}
        >
          <Avatar
            src={avatarUrl ?? undefined}
            sx={{
              width: 96,
              height: 96,
              bgcolor: "primary.main",
              fontWeight: 700,
            }}
            imgProps={{ loading: "lazy" }}
          >
            {avatarInitials || <FolderOutlinedIcon />}
          </Avatar>
          <Box
            sx={{
              position: "absolute",
              inset: 0,
              borderRadius: "50%",
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              bgcolor: "rgba(0,0,0,0.45)",
              color: "common.white",
              opacity: uploadAvatarMutation.isPending ? 1 : 0,
              transition: "opacity 0.2s ease-in-out",
              ...(canManageDossier ? { "&:hover": { opacity: 1 } } : {}),
            }}
          >
            {uploadAvatarMutation.isPending ? (
              <CircularProgress size={24} color="inherit" />
            ) : canManageDossier ? (
              <PhotoCameraIcon />
            ) : null}
          </Box>
        </Box>
        <Stack spacing={0.25}>
          <Typography variant="h5">{editModel.caseNumber}</Typography>
          <Typography variant="body2" color="text.secondary">
            برای تغییر تصویر پرونده، آواتار را انتخاب کنید.
          </Typography>
        </Stack>
      </Stack>

      <Stack
        direction={{ xs: "column", sm: "row" }}
        spacing={1}
        justifyContent="space-between"
        alignItems={{ xs: "stretch", sm: "center" }}
      >
        <Typography variant="subtitle2" color="text.secondary">
          شماره مرجع: {editModel.externalCaseNumber}
        </Typography>
        <Stack
          direction="row"
          spacing={1}
          justifyContent={{ xs: "flex-start", sm: "flex-end" }}
        >
          <Button
            variant="outlined"
            startIcon={
              isExportingExcel ? (
                <CircularProgress size={16} />
              ) : (
                <DownloadIcon />
              )
            }
            onClick={() => void downloadExcel()}
            disabled={isExportingExcel}
          >
            {isExportingExcel ? "در حال ساخت اکسل..." : "خروجی اکسل پرونده"}
          </Button>
          <Button
            variant="outlined"
            startIcon={<DriveFileMoveIcon />}
            onClick={() => {
              setTargetClassification(null);
              setMoveDialogOpen(true);
            }}
            disabled={!canMoveCurrentCase || moveMutation.isPending}
          >
            انتقال پرونده
          </Button>
        </Stack>
      </Stack>

      <Tabs
        value={activeTab}
        onChange={(_event, value) => setActiveTab(value)}
        variant="scrollable"
        scrollButtons="auto"
      >
        <Tab label="اطلاعات پرونده" />
        <Tab label="مالکین" />
        <Tab label="اطلاعات تکمیلی" />
        <Tab label="فایل‌ها" />
        <Tab label="تاریخچه / لاگ‌ها" />
      </Tabs>

      <Divider />

      {activeTab === 0 && (
        <Stack spacing={1.25} alignItems="stretch">
          <Stack
            direction={{ xs: "column", md: "row" }}
            spacing={1}
            alignItems={{ xs: "stretch", md: "flex-start" }}
          >
            <TextField
              fullWidth
              size="small"
              label="شماره پرونده"
              value={editModel.caseNumber}
              onChange={(event) =>
                setEditModel((current) =>
                  current
                    ? { ...current, caseNumber: event.target.value }
                    : current,
                )
              }
              disabled
            />
            <TextField
              fullWidth
              size="small"
              label="شماره مرجع"
              value={editModel.externalCaseNumber}
              onChange={(event) =>
                setEditModel((current) =>
                  current
                    ? { ...current, externalCaseNumber: event.target.value }
                    : current,
                )
              }
              disabled={!canWrite || isBehsazIntegrationEnabled}
            />
            <Button
              variant="contained"
              size="small"
              startIcon={<DownloadIcon />}
              onClick={() => setBehsazOpen(true)}
              disabled={!behsazConfigQuery.data}
              sx={{
                minHeight: 40,
                minWidth: { md: 142 },
                whiteSpace: "nowrap",
                alignSelf: { xs: "stretch", md: "auto" },
              }}
            >
              دریافت از بهساز
            </Button>
          </Stack>
          <TextField
            label="آدرس"
            size="small"
            multiline
            minRows={1}
            maxRows={2}
            value={editModel.address}
            onChange={(event) =>
              setEditModel((current) =>
                current ? { ...current, address: event.target.value } : current,
              )
            }
            disabled={!canWrite}
          />
          <Stack direction="row" spacing={1} flexWrap="wrap">
            <Chip label={`طبقه: ${editModel.classificationTitle}`} />
            <Chip
              label={`آخرین بروزرسانی: ${formatServerDateTime(editModel.updatedAt)}`}
            />
          </Stack>
        </Stack>
      )}

      {activeTab === 1 && hasDossierManagementPermission && (
        <DossierOwnersTab
          dossierId={caseId}
          caseNumber={editModel.caseNumber || editModel.externalCaseNumber}
          canWrite={canManageDossier}
          canEditOwner={hasTaxpayerManagementPermission}
        />
      )}

      {activeTab === 1 && !hasDossierManagementPermission && (
        <Alert severity="warning">
          شما مجوز مشاهده یا مدیریت مالکین پرونده را ندارید.
        </Alert>
      )}

      {activeTab === 2 && (
        <Stack spacing={1}>
          {dynamicValuesQuery.isError && (
            <Alert severity="error">
              {mapApiError(dynamicValuesQuery.error).message ||
                "بارگذاری اطلاعات تکمیلی ناموفق بود."}
            </Alert>
          )}
          <DynamicFormRenderer
            template={templateQuery.data ?? null}
            values={dynamicValues}
            onChange={setDynamicValues}
            disabled={!canWrite}
          />
        </Stack>
      )}

      {activeTab === 3 && canReadFiles && (
        <div className="flex flex-col w-full max-w-full overflow-hidden box-border">
          <FileManager
            caseId={caseId}
            caseClassificationId={editModel.classificationId}
            canWriteClassification={canWrite}
          />
        </div>
      )}

      {activeTab === 3 && !canReadFiles && (
        <Alert severity="warning">
          شما مجوز مشاهده فایل‌های پرونده را ندارید.
        </Alert>
      )}

      {activeTab === 4 && (
        <List dense>
          {editModel.history.map((row) => (
            <ListItem key={row.id} divider>
              <ListItemText
                primary={`عملیات: ${getActionTypeLabel(row.actionType)}${row.detail ? ` - ${row.detail}` : ""}`}
                secondary={`کاربر: ${row.actorName} | زمان: ${formatServerDateTime(row.timestamp)}`}
              />
            </ListItem>
          ))}
          {editModel.history.length === 0 && (
            <ListItem>
              <ListItemText primary="تاریخچه‌ای ثبت نشده است." />
            </ListItem>
          )}
        </List>
      )}

      <Paper
        elevation={0}
        sx={{
          position: "fixed",
          left: { xs: "50%", lg: "calc(65% - 160px)" },
          bottom: { xs: 16, sm: 50 },
          transform: "translateX(-50%)",
          zIndex: (muiTheme) => muiTheme.zIndex.drawer + 1,
          width: { xs: "calc(100vw - 24px)", sm: "max-content" },
          maxWidth: "100%",
          borderRadius: 3,
          border: "3px solid",
          borderColor: "divider",
          bgcolor: "background.paper",
          backdropFilter: "blur(12px)",
          boxShadow: (muiTheme) =>
            `0 12px 36px ${muiTheme.palette.action.disabledBackground}`,
          p: 1,
        }}
      >
        <Stack
          direction="row"
          spacing={5}
          justifyContent="flex-end"
          alignItems="center"
        >
          
          <Button
            variant="contained"
            size="large"
            startIcon={
              isSaving ? <CircularProgress size={18} color="inherit" /> : <SaveIcon />
            }
            onClick={() => void saveAllChanges()}
            disabled={!canWrite || isSaving || !hasUnsavedChanges}
            sx={{
              minWidth: { xs: 0, sm: 300 },
              flex: { xs: 1, sm: "none" },
              borderRadius: 2,
              boxShadow: 2,
              "&:hover": { boxShadow: 4 },
            }}
          >
            {isSaving ? "در حال ذخیره..." : "ذخیره"}
          </Button>
          <Button
            variant="outlined"
            color="inherit"
            size="large"
            startIcon={<CloseRoundedIcon />}
            onClick={() => void cancelAndReturnToList()}
            disabled={isSaving}
            sx={{
              minWidth: { xs: 0, sm: 300 },
              flex: { xs: 1, sm: "none" },
              borderRadius: 2,
              borderColor: "divider",
              color: "text.secondary",
              "&:hover": {
                borderColor: "text.secondary",
                bgcolor: "action.hover",
              },
            }}
          >
            انصراف
          </Button>
        </Stack>
      </Paper>

      <Dialog
        open={moveDialogOpen}
        onClose={() => setMoveDialogOpen(false)}
        fullWidth
        maxWidth="sm"
        fullScreen={isCompact}
      >
        <DialogTitle>انتقال پرونده</DialogTitle>
        <DialogContent>
          <TextField
            fullWidth
            label="جستجوی طبقه"
            value={moveSearch}
            onChange={(event) => setMoveSearch(event.target.value)}
            sx={{ mb: 1.5 }}
          />
          <Stack spacing={0.5} sx={{ maxHeight: 300, overflow: "auto" }}>
            {moveCandidates.map((node) => (
              <Button
                key={node.id}
                variant={
                  targetClassification === node.id ? "contained" : "text"
                }
                onClick={() => setTargetClassification(node.id)}
                sx={{ justifyContent: "flex-start", width: "100%" }}
              >
                <span style={{ paddingInlineStart: node.level * 12 }}>
                  {node.title}
                </span>
              </Button>
            ))}
            {moveCandidates.length === 0 && (
              <Typography variant="body2" color="text.secondary">
                دسته‌بندی مقصد مجاز برای انتقال یافت نشد.
              </Typography>
            )}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button
            onClick={() => setMoveDialogOpen(false)}
            disabled={moveMutation.isPending}
          >
            انصراف
          </Button>
          <Button
            variant="contained"
            onClick={doMoveCase}
            disabled={
              !canMoveCurrentCase ||
              !selectedMoveTargetIsValid ||
              moveMutation.isPending
            }
            startIcon={
              moveMutation.isPending ? <CircularProgress size={16} /> : null
            }
          >
            {moveMutation.isPending ? "در حال انتقال..." : "انتقال"}
          </Button>
        </DialogActions>
      </Dialog>

      <BehsazPopup
        open={behsazOpen}
        currentExternalCaseNumber={editModel.externalCaseNumber}
        onClose={() => setBehsazOpen(false)}
        onSelect={(externalCaseNumber) => {
          setEditModel((current) =>
            current ? { ...current, externalCaseNumber } : current,
          );
          setBehsazOpen(false);
        }}
      />
    </Stack>
  );
};
