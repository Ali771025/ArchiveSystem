import AddIcon from "@mui/icons-material/Add";
import FilterAltIcon from "@mui/icons-material/FilterAlt";
import FolderOutlinedIcon from "@mui/icons-material/FolderOutlined";
import {
  Alert,
  Avatar,
  Box,
  Button,
  Chip,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  InputLabel,
  IconButton,
  Paper,
  MenuItem,
  Select,
  Skeleton,
  Stack,
  Tooltip,
  TextField,
  Typography,
  useMediaQuery,
  useTheme,
} from "@mui/material";
import {
  DataGrid,
  GridColDef,
  GridRenderCellParams,
  GridPaginationModel,
  GridSortModel,
} from "@mui/x-data-grid";
import { useEffect, useMemo, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import DeleteOutlineIcon from "@mui/icons-material/DeleteOutline";
import DriveFileMoveOutlinedIcon from "@mui/icons-material/DriveFileMoveOutlined";
import EditOutlinedIcon from "@mui/icons-material/EditOutlined";
import VisibilityOutlinedIcon from "@mui/icons-material/VisibilityOutlined";
import { formatServerDateTime } from "@/utils/serverDateTime";
import { JalaliDateInput } from "@/components/common/JalaliDateInputs";
import {
  useBehsazConfig,
  useCaseMoveAccessIds,
  useCaseSearch,
  useClassificationTree,
  useCreateCase,
  useDeleteCase,
  useMoveCase,
  useTags,
  useWritableClassificationIds,
} from "@/services/archiveQueries";
import { useArchiveStore } from "@/stores/useArchiveStore";
import { useSessionStore } from "@/stores/useSessionStore";
import { useToastStore } from "@/stores/useToastStore";
import {
  mapApiError,
  stagnantArchiveReadOnlyMessage,
} from "@/services/errorMapper";
import type {
  CaseSearchRequest,
  CaseSummary,
} from "@/types/archive";
import { resolveMediaUrl } from "@/utils/mediaUrl";
import { CreateDossierModal } from "@/components/cases/CreateDossierModal";
import { DynamicCaseFilters } from "@/components/cases/DynamicCaseFilters";
import { useConfirm } from "@/components/common/useConfirm";
import { useStableRowCount } from "@/hooks/useStableRowCount";
import { parseDateLikeValue } from "@/utils/jalaliDate";
import {
  CASE_LIST_QUERY_KEYS,
  parseCaseListUrlState,
  serializeDynamicFilters,
  setOptionalQueryParam,
} from "@/utils/caseListUrlState";

interface CaseListPanelProps {
  classificationId: number | null;
}

const buildThumbnailUrl = (avatarImagePath?: string | null): string | null => {
  if (!avatarImagePath) {
    return null;
  }

  const thumbnailPath = avatarImagePath.replace(
    /\.(jpg|jpeg|png)$/i,
    "_thumb.$1",
  );
  return resolveMediaUrl(thumbnailPath);
};

const getCaseInitials = (
  row: Pick<CaseSummary, "externalCaseNumber" | "caseNumber">,
): string => {
  const source = `${row.externalCaseNumber || row.caseNumber}`
    .replace(/\s+/g, "")
    .trim();
  return source.slice(0, 2).toUpperCase();
};

const loanStatusLabels: Record<string, string> = {
  Loaned: "امانت داده شده",
  Returned: "برگشت داده شده",
  Overdue: "دیرکرد",
};

const loanStatusColors: Record<
  string,
  "default" | "success" | "warning" | "error"
> = {
  Loaned: "warning",
  Overdue: "error",
  Returned: "success",
};

export const CaseListPanel = ({ classificationId }: CaseListPanelProps) => {
  const theme = useTheme();
  const isCompact = useMediaQuery(theme.breakpoints.down("md"));
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();
  const urlState = useMemo(
    () => parseCaseListUrlState(searchParams),
    [searchParams],
  );
  const ownerName = urlState.ownerName;
  const externalCaseNumber = urlState.caseNumber;
  const fromDate = urlState.fromDate;
  const toDate = urlState.toDate;
  const selectedTag: number | "all" = urlState.tagId ?? "all";
  const dynamicFilterExpression = urlState.dynamicFilterExpression;
  const pagination = useMemo<GridPaginationModel>(
    () => ({
      page: urlState.page - 1,
      pageSize: urlState.pageSize,
    }),
    [urlState.page, urlState.pageSize],
  );
  const sortModel = useMemo<GridSortModel>(
    () => [{ field: urlState.sortBy, sort: urlState.sortDirection }],
    [urlState.sortBy, urlState.sortDirection],
  );
  const [draftOwnerName, setDraftOwnerName] = useState(ownerName);
  const [draftExternalCaseNumber, setDraftExternalCaseNumber] =
    useState(externalCaseNumber);
  const [draftFromDate, setDraftFromDate] = useState(fromDate);
  const [draftToDate, setDraftToDate] = useState(toDate);
  const [draftSelectedTag, setDraftSelectedTag] = useState<number | "all">(
    selectedTag,
  );
  const [filterError, setFilterError] = useState("");
  const [dynamicFiltersOpen, setDynamicFiltersOpen] = useState(false);
  const [createOpen, setCreateOpen] = useState(false);
  const [moveDialogOpen, setMoveDialogOpen] = useState(false);
  const [selectedCase, setSelectedCase] = useState<CaseSummary | null>(null);
  const [moveTargetClassification, setMoveTargetClassification] = useState<
    number | ""
  >("");

  const { data: tags = [] } = useTags();
  const { data: classifications = [] } = useClassificationTree();
  const {
    data: writableClassificationIds = [],
    isLoading: isWritableClassificationsLoading,
  } = useWritableClassificationIds();
  const { data: movableClassificationIds = [] } = useCaseMoveAccessIds();
  const createMutation = useCreateCase();
  const behsazConfigQuery = useBehsazConfig();
  const moveMutation = useMoveCase();
  const deleteMutation = useDeleteCase();
  const pushToast = useToastStore((state) => state.push);
  const globalSearch = useArchiveStore((state) => state.filters.globalSearch);
  const canWriteByRole = useSessionStore((state) =>
    state.hasPermission("dossiers.manage"),
  );
  const confirm = useConfirm();

  const updateSearchParams = (update: (next: URLSearchParams) => void) => {
    const next = new URLSearchParams(searchParams);
    update(next);

    if (next.toString() !== searchParams.toString()) {
      setSearchParams(next);
    }
  };

  const openCase = (caseNumber?: string | null) => {
    const normalized = (caseNumber ?? "").trim();
    if (!normalized || normalized.toLowerCase() === "undefined") {
      pushToast("شناسه پرونده نامعتبر است.", "warning");
      return;
    }

    navigate({
      pathname: `/cases/${encodeURIComponent(normalized)}`,
      search: searchParams.toString(),
    });
  };

  const hasActiveFilters = Boolean(
    ownerName ||
    externalCaseNumber ||
    fromDate ||
    toDate ||
    selectedTag !== "all" ||
    dynamicFilterExpression != null ||
    globalSearch,
  );

  const applyFilters = () => {
    const from = parseDateLikeValue(draftFromDate);
    const to = parseDateLikeValue(draftToDate);
    if (from && to && from.getTime() > to.getTime()) {
      setFilterError("از تاریخ نباید بزرگتر از تا تاریخ باشد.");
      return;
    }

    setFilterError("");
    updateSearchParams((next) => {
      setOptionalQueryParam(
        next,
        CASE_LIST_QUERY_KEYS.ownerName,
        draftOwnerName.trim(),
      );
      setOptionalQueryParam(
        next,
        CASE_LIST_QUERY_KEYS.caseNumber,
        draftExternalCaseNumber.trim(),
      );
      setOptionalQueryParam(next, CASE_LIST_QUERY_KEYS.fromDate, draftFromDate);
      setOptionalQueryParam(next, CASE_LIST_QUERY_KEYS.toDate, draftToDate);
      setOptionalQueryParam(
        next,
        CASE_LIST_QUERY_KEYS.tagId,
        draftSelectedTag === "all" ? null : draftSelectedTag,
      );
      next.delete(CASE_LIST_QUERY_KEYS.page);
    });
  };

  const clearFilters = () => {
    setDraftOwnerName("");
    setDraftExternalCaseNumber("");
    setDraftFromDate("");
    setDraftToDate("");
    setDraftSelectedTag("all");
    setFilterError("");
    updateSearchParams((next) => {
      next.delete(CASE_LIST_QUERY_KEYS.ownerName);
      next.delete(CASE_LIST_QUERY_KEYS.caseNumber);
      next.delete(CASE_LIST_QUERY_KEYS.fromDate);
      next.delete(CASE_LIST_QUERY_KEYS.toDate);
      next.delete(CASE_LIST_QUERY_KEYS.tagId);
      next.delete(CASE_LIST_QUERY_KEYS.dynamicFilters);
      next.delete(CASE_LIST_QUERY_KEYS.page);
    });
  };

  const request: CaseSearchRequest = useMemo(
    () => ({
      classificationId: classificationId ?? undefined,
      page: pagination.page + 1,
      pageSize: pagination.pageSize,
      sortBy:
        (sortModel[0]?.field as CaseSearchRequest["sortBy"]) ?? "updatedAt",
      sortDirection: sortModel[0]?.sort === "asc" ? "asc" : "desc",
      ownerName,
      externalCaseNumber: externalCaseNumber || globalSearch,
      fromDate,
      toDate,
      tags: selectedTag === "all" ? undefined : [selectedTag],
      dynamicFilterExpression,
    }),
    [
      classificationId,
      pagination.page,
      pagination.pageSize,
      sortModel,
      ownerName,
      externalCaseNumber,
      fromDate,
      toDate,
      selectedTag,
      dynamicFilterExpression,
      globalSearch,
    ],
  );

  useEffect(() => {
    setDraftOwnerName(ownerName);
    setDraftExternalCaseNumber(externalCaseNumber);
    setDraftFromDate(fromDate);
    setDraftToDate(toDate);
    setDraftSelectedTag(selectedTag);
    setFilterError("");
  }, [
    ownerName,
    externalCaseNumber,
    fromDate,
    toDate,
    selectedTag,
  ]);

  useEffect(() => {
    setDynamicFiltersOpen(false);
  }, [classificationId]);

  const query = useCaseSearch(request);

  const rows = query.data?.items ?? [];
  const rowCount = useStableRowCount(query.data?.totalCount);
  const hasNextPage = (pagination.page + 1) * pagination.pageSize < rowCount;
  const hasPreviousPage = pagination.page > 0;

  const leafClassificationSet = useMemo(() => {
    const activeNodes = classifications.filter(
      (node) => node.isActive && !node.systemKey,
    );
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

  const writableClassificationSet = useMemo(
    () => new Set(writableClassificationIds),
    [writableClassificationIds],
  );
  const movableClassificationSet = useMemo(
    () => new Set(movableClassificationIds),
    [movableClassificationIds],
  );
  const moveTargetClassifications = useMemo(
    () =>
      classifications.filter(
        (node) =>
          node.isActive &&
          !node.systemKey &&
          leafClassificationSet.has(node.id) &&
          movableClassificationSet.has(node.id),
      ),
    [classifications, leafClassificationSet, movableClassificationSet],
  );

  const selectedClassification = classifications.find(
    (node) => node.id === classificationId,
  );
  const isArchiveClassification =
    selectedClassification?.systemKey === "ArchivedCases";
  const selectedClassificationIsLeaf =
    isArchiveClassification ||
    (classificationId != null && leafClassificationSet.has(classificationId));
  const selectedClassificationWritable =
    !isArchiveClassification &&
    classificationId != null &&
    writableClassificationSet.has(classificationId);

  const canWriteClassification = (targetClassificationId: number) =>
    writableClassificationIds.includes(targetClassificationId);
  const canMoveClassification = (targetClassificationId: number) =>
    movableClassificationSet.has(targetClassificationId);

  const openMoveDialog = (row: CaseSummary) => {
    if (!canMoveClassification(row.classificationId)) {
      pushToast(
        "مجوز انتقال پرونده از این طبقه‌بندی برای شما فعال نیست.",
        "warning",
      );
      return;
    }

    if (
      !moveTargetClassifications.some(
        (item) => item.id !== row.classificationId,
      )
    ) {
      pushToast("دسته‌بندی مقصد مجاز برای انتقال یافت نشد.", "warning");
      return;
    }

    setSelectedCase(row);
    setMoveTargetClassification("");
    setMoveDialogOpen(true);
  };

  const handleMoveCase = async () => {
    if (!selectedCase || !moveTargetClassification) {
      return;
    }

    const targetClassificationId = Number(moveTargetClassification);
    if (!canMoveClassification(selectedCase.classificationId)) {
      pushToast(
        "مجوز انتقال پرونده از طبقه‌بندی مبدأ برای شما فعال نیست.",
        "warning",
      );
      return;
    }

    if (
      targetClassificationId === selectedCase.classificationId ||
      !moveTargetClassifications.some(
        (item) => item.id === targetClassificationId,
      )
    ) {
      pushToast("دسته‌بندی مقصد انتخاب‌شده برای انتقال مجاز نیست.", "warning");
      return;
    }

    try {
      await moveMutation.mutateAsync({
        caseId: selectedCase.id,
        sourceClassificationId: selectedCase.classificationId,
        classificationId: targetClassificationId,
      });

      pushToast("پرونده با موفقیت منتقل شد.", "success");
      setSelectedCase(null);
      setMoveTargetClassification("");
      setMoveDialogOpen(false);
    } catch (error) {
      const mapped = mapApiError(error);
      pushToast(mapped.message || "انتقال پرونده ناموفق بود.", "error");
    }
  };

  const handleDeleteCase = async (row: CaseSummary) => {
    if (!canWriteClassification(row.classificationId)) {
      pushToast(
        "شما فقط مجوز مشاهده این طبقه‌بندی را دارید و امکان بایگانی پرونده برای شما فعال نیست.",
        "warning",
      );
      return;
    }

    const shouldDelete = await confirm({
      title: "تأیید بایگانی پرونده",
      message: `پرونده ${row.caseNumber} به دسته «بایگانی راکد» منتقل می‌شود و بعداً با انتقال به دسته دیگر قابل بازیابی است. آیا ادامه می‌دهید؟`,
      confirmText: "بله، بایگانی کن",
      cancelText: "انصراف",
      variant: "danger",
    });
    if (!shouldDelete) {
      return;
    }

    try {
      await deleteMutation.mutateAsync(row.id);
      pushToast("پرونده به بایگانی راکد منتقل شد.", "success");
    } catch (error) {
      const mapped = mapApiError(error);
      pushToast(mapped.message || "بایگانی پرونده ناموفق بود.", "error");
    }
  };

  const columns: GridColDef<CaseSummary>[] = [
    {
      field: "avatarImagePath",
      headerName: "",
      width: 68,
      sortable: false,
      filterable: false,
      align: "center",
      headerAlign: "center",
      renderCell: (params) => {
        const thumbnailUrl = buildThumbnailUrl(params.row.avatarImagePath);
        const initials = getCaseInitials(params.row);
        return (
          <Avatar
            src={thumbnailUrl ?? undefined}
            sx={{
              width: 32,
              height: 32,
              bgcolor: "primary.main",
              fontSize: 12,
              fontWeight: 700,
            }}
            imgProps={{ loading: "lazy" }}
          >
            {initials || <FolderOutlinedIcon fontSize="small" />}
          </Avatar>
        );
      },
    },
    {
      field: "caseNumber",
      headerName: "شماره پرونده",
      minWidth: 140,
      flex: 1,
      align: "right",
      headerAlign: "right",
    },
    {
      field: "loanStatus",
      headerName: "وضعیت امانت",
      minWidth: 150,
      flex: 0.9,
      sortable: false,
      align: "right",
      headerAlign: "right",
      renderCell: (params: GridRenderCellParams<CaseSummary>) => {
        const status = params.row.loanStatus?.trim();
        return (
          <Chip
            size="small"
            variant="outlined"
            color={(status && loanStatusColors[status]) || "default"}
            label={
              (status && (loanStatusLabels[status] ?? status)) || "ثبت نشده"
            }
          />
        );
      },
    },
    {
      field: "ownerNames",
      headerName: "نام مالک",
      minWidth: 140,
      flex: 1,
      sortable: false,
      align: "right",
      headerAlign: "right",
      valueGetter: (_value, row) => row.ownerNames.join("، "),
    },
    {
      field: "externalCaseNumber",
      headerName: "شماره مرجع",
      minWidth: 160,
      flex: 1,
      align: "right",
      headerAlign: "right",
    },
    {
      field: "address",
      headerName: "نشانی",
      minWidth: 220,
      flex: 1.5,
      align: "right",
      headerAlign: "right",
    },
    {
      field: "updatedAt",
      headerName: "آخرین بروزرسانی",
      minWidth: 160,
      flex: 1,
      align: "right",
      headerAlign: "right",
      valueFormatter: (value) => formatServerDateTime(String(value)),
    },
    {
      field: "fileCount",
      headerName: "تعداد فایل",
      minWidth: 100,
      flex: 0.65,
      sortable: false,
      align: "center",
      headerAlign: "center",
      valueFormatter: (value) => Number(value ?? 0).toLocaleString("fa-IR"),
    },
    {
      field: "actions",
      headerName: "عملیات",
      sortable: false,
      filterable: false,
      minWidth: 150,
      flex: 1,
      align: "center",
      headerAlign: "center",
      renderCell: (params: GridRenderCellParams<CaseSummary>) => {
        const sourceIsWritable = canWriteClassification(
          params.row.classificationId,
        );
        const sourceIsMovable = canMoveClassification(
          params.row.classificationId,
        );
        const hasMovableTarget = moveTargetClassifications.some(
          (item) => item.id !== params.row.classificationId,
        );

        return (
          <Stack direction="row" spacing={0.5}>
            <Tooltip
              title={sourceIsWritable ? "ویرایش پرونده" : "مشاهده پرونده"}
            >
              <IconButton
                size="small"
                aria-label={`${sourceIsWritable ? "ویرایش" : "مشاهده"} پرونده ${params.row.caseNumber}`}
                onClick={(event) => {
                  event.stopPropagation();
                  openCase(
                    params.row.caseNumber || params.row.externalCaseNumber,
                  );
                }}
              >
                {sourceIsWritable ? (
                  <EditOutlinedIcon fontSize="small" />
                ) : (
                  <VisibilityOutlinedIcon fontSize="small" />
                )}
              </IconButton>
            </Tooltip>
            <Tooltip
              title={
                !sourceIsWritable
                  ? isArchiveClassification
                    ? stagnantArchiveReadOnlyMessage
                    : "برای بایگانی پرونده، دسترسی ویرایش این طبقه‌بندی را ندارید"
                  : isArchiveClassification
                    ? "پرونده در بایگانی راکد است"
                    : "بایگانی پرونده"
              }
            >
              <span
                onClick={(event) => {
                  if (!sourceIsWritable) {
                    event.stopPropagation();
                    pushToast(
                      isArchiveClassification
                        ? stagnantArchiveReadOnlyMessage
                        : "شما فقط مجوز مشاهده این طبقه‌بندی را دارید و امکان بایگانی پرونده برای شما فعال نیست.",
                      "warning",
                    );
                  }
                }}
              >
                <IconButton
                  size="small"
                  color="error"
                  disabled={
                    deleteMutation.isPending ||
                    isArchiveClassification ||
                    !sourceIsWritable
                  }
                  aria-label={`بایگانی پرونده ${params.row.caseNumber}`}
                  onClick={(event) => {
                    event.stopPropagation();
                    void handleDeleteCase(params.row);
                  }}
                >
                  {deleteMutation.isPending ? (
                    <CircularProgress size={16} />
                  ) : (
                    <DeleteOutlineIcon fontSize="small" />
                  )}
                </IconButton>
              </span>
            </Tooltip>
            <Tooltip
              title={
                !sourceIsMovable
                  ? "مجوز انتقال از طبقه‌بندی مبدأ را ندارید"
                  : !hasMovableTarget
                    ? "طبقه‌بندی مقصد دارای مجوز انتقال وجود ندارد"
                    : isArchiveClassification
                      ? "بازیابی / انتقال پرونده"
                      : "انتقال پرونده"
              }
            >
              <span>
                <IconButton
                  size="small"
                  color="primary"
                  disabled={
                    moveMutation.isPending ||
                    !sourceIsMovable ||
                    !hasMovableTarget
                  }
                  aria-label={`${isArchiveClassification ? "بازیابی / انتقال" : "انتقال"} پرونده ${params.row.caseNumber}`}
                  onClick={(event) => {
                    event.stopPropagation();
                    openMoveDialog(params.row);
                  }}
                >
                  <DriveFileMoveOutlinedIcon fontSize="small" />
                </IconButton>
              </span>
            </Tooltip>
          </Stack>
        );
      },
    },
    {
      field: "classificationTitle",
      headerName: "طبقه‌بندی",
      minWidth: 280,
      flex: 1.6,
      renderCell: (params: GridRenderCellParams<CaseSummary>) => (
        <Tooltip title={params.row.classificationTitle} placement="top">
          <Box
            component="span"
            sx={{
              display: "flex",
              alignItems: "center",
              width: "100%",
              height: "100%",
              minWidth: 0,
            }}
          >
            <Typography variant="body2" noWrap>
              {params.row.classificationTitle}
            </Typography>
          </Box>
        </Tooltip>
      ),
    },
  ];

  const createCase = async (values: {
    externalCaseNumber: string;
    address: string;
    postalCode?: string;
  }) => {
    if (!classificationId) {
      pushToast("ابتدا یک طبقه‌بندی انتخاب کنید.", "warning");
      return;
    }

    if (!canWriteByRole) {
      pushToast("دسترسی ایجاد پرونده ندارید.", "warning");
      return;
    }

    if (isArchiveClassification) {
      pushToast(
        "در مخزن بایگانی راکد امکان ایجاد پرونده جدید وجود ندارد.",
        "warning",
      );
      return;
    }

    if (!selectedClassificationWritable) {
      pushToast(
        "برای دسته‌بندی انتخابی دسترسی ایجاد پرونده ندارید.",
        "warning",
      );
      return;
    }

    if (!selectedClassificationIsLeaf) {
      pushToast("ایجاد پرونده فقط در آخرین سطح دسته‌بندی مجاز است.", "warning");
      return;
    }

    let created: Awaited<ReturnType<typeof createMutation.mutateAsync>>;
    try {
      created = await createMutation.mutateAsync({
        classificationId,
        address: values.address,
        externalCaseNumber: values.externalCaseNumber,
        postalCode: values.postalCode ?? null,
      });
    } catch (error) {
      const mapped = mapApiError(error);
      pushToast(mapped.message || "ایجاد پرونده ناموفق بود.", "error");
      return;
    }

    setCreateOpen(false);
    pushToast("پرونده جدید ایجاد شد.", "success");
    openCase(created.caseNumber || created.externalCaseNumber);
  };

  const openCreateDialog = () => {
    if (!classificationId) {
      pushToast("ابتدا یک طبقه‌بندی انتخاب کنید.", "warning");
      return;
    }

    if (!canWriteByRole) {
      pushToast("دسترسی ایجاد پرونده ندارید.", "warning");
      return;
    }

    if (isArchiveClassification) {
      pushToast(
        "در مخزن بایگانی راکد امکان ایجاد پرونده جدید وجود ندارد.",
        "warning",
      );
      return;
    }

    if (isWritableClassificationsLoading) {
      pushToast(
        "در حال بررسی دسترسی دسته‌بندی‌ها هستیم. دوباره تلاش کنید.",
        "warning",
      );
      return;
    }

    if (!selectedClassificationWritable) {
      pushToast(
        "برای دسته‌بندی انتخابی دسترسی ایجاد پرونده ندارید.",
        "warning",
      );
      return;
    }

    if (!selectedClassificationIsLeaf) {
      pushToast("ایجاد پرونده فقط در آخرین سطح دسته‌بندی مجاز است.", "warning");
      return;
    }

    setCreateOpen(true);
  };

  return (
    <Stack spacing={1.5}>
      {isArchiveClassification &&
        !isWritableClassificationsLoading &&
        !writableClassificationSet.has(classificationId ?? -1) && (
          <Alert severity="warning">{stagnantArchiveReadOnlyMessage}</Alert>
        )}
      <Stack
        direction={{ xs: "column", sm: "row" }}
        spacing={1}
        alignItems={{ xs: "stretch", sm: "center" }}
      >
        <Typography variant="h5">فهرست پرونده‌ها</Typography>
        <Button
          variant="contained"
          startIcon={
            createMutation.isPending ? (
              <CircularProgress size={16} />
            ) : (
              <AddIcon />
            )
          }
          onClick={openCreateDialog}
          disabled={
            createMutation.isPending ||
            !canWriteByRole ||
            isArchiveClassification ||
            isWritableClassificationsLoading ||
            !selectedClassificationWritable ||
            !selectedClassificationIsLeaf
          }
          sx={{ alignSelf: { xs: "flex-start", sm: "auto" } }}
        >
          پرونده جدید
        </Button>
      </Stack>

      <Box
        sx={{
          display: "grid",
          gap: 1,
          alignItems: "stretch",
          gridTemplateColumns: {
            xs: "1fr",
            sm: "repeat(2, minmax(0, 1fr))",
            lg: "repeat(3, minmax(0, 1fr))",
            xl: "repeat(6, minmax(0, 1fr))",
          },
        }}
      >
        <Stack spacing={0.5}>
          <Typography
            variant="caption"
            color="text.secondary"
            sx={{ minHeight: 20, fontWeight: 500 }}
          >
            نام مالک
          </Typography>
          <TextField
            size="small"
            value={draftOwnerName}
            onChange={(event) => setDraftOwnerName(event.target.value)}
            onKeyDown={(event) => {
              if (event.key === "Enter") {
                applyFilters();
              }
            }}
            placeholder="نام، نام خانوادگی یا نام شرکت"
            fullWidth
          />
        </Stack>

        <Stack spacing={0.5}>
          <Typography
            variant="caption"
            color="text.secondary"
            sx={{ minHeight: 20, fontWeight: 500 }}
          >
            شماره پرونده
          </Typography>
          <TextField
            size="small"
            value={draftExternalCaseNumber}
            onChange={(event) => setDraftExternalCaseNumber(event.target.value)}
            onKeyDown={(event) => {
              if (event.key === "Enter") {
                applyFilters();
              }
            }}
            fullWidth
          />
        </Stack>

        <Stack spacing={0.5}>
          <Typography
            variant="caption"
            color="text.secondary"
            sx={{ minHeight: 20, fontWeight: 500 }}
          >
            از تاریخ
          </Typography>
          <JalaliDateInput
            value={draftFromDate}
            onChange={setDraftFromDate}
            inputClassName="text-sm"
          />
        </Stack>

        <Stack spacing={0.5}>
          <Typography
            variant="caption"
            color="text.secondary"
            sx={{ minHeight: 20, fontWeight: 500 }}
          >
            تا تاریخ
          </Typography>
          <JalaliDateInput
            value={draftToDate}
            onChange={setDraftToDate}
            inputClassName="text-sm"
          />
        </Stack>

        {/* <Stack spacing={0.5}>
          <Typography
            variant="caption"
            color="text.secondary"
            sx={{ minHeight: 20, fontWeight: 500 }}
          >
            تگ
          </Typography>
          <Select
            size="small"
            fullWidth
            value={draftSelectedTag}
            onChange={(event) =>
              setDraftSelectedTag(event.target.value as number | "all")
            }
          >
            <MenuItem value="all">همه تگ‌ها</MenuItem>
            {tags.map((tag) => (
              <MenuItem key={tag.id} value={tag.id}>
                {tag.name}
              </MenuItem>
            ))}
          </Select>
        </Stack> */}

        <Stack spacing={0.5}>
          <Typography
            variant="caption"
            color="text.secondary"
            sx={{ minHeight: 20, visibility: "hidden", fontWeight: 500 }}
          >
            action
          </Typography>
          <Button
            size="small"
            startIcon={<FilterAltIcon />}
            onClick={applyFilters}
            fullWidth
            sx={{ minHeight: 40 }}
          >
            اعمال فیلتر
          </Button>
        </Stack>

        <Stack spacing={0.5}>
          <Typography
            variant="caption"
            color="text.secondary"
            sx={{ minHeight: 20, visibility: "hidden", fontWeight: 500 }}
          >
            action
          </Typography>
          <Button
            size="small"
            variant={dynamicFilterExpression ? "contained" : "outlined"}
            startIcon={<FilterAltIcon />}
            onClick={() => setDynamicFiltersOpen((current) => !current)}
            disabled={classificationId == null}
            fullWidth
            sx={{ minHeight: 40 }}
          >
            فیلتر براساس اطلاعات تکمیلی
          </Button>
        </Stack>
      </Box>

      {(filterError || hasActiveFilters) && (
        <Stack
          direction={{ xs: "column", sm: "row" }}
          spacing={1}
          alignItems={{ xs: "stretch", sm: "center" }}
          justifyContent="space-between"
        >
          <Typography
            variant="caption"
            color={filterError ? "error" : "text.secondary"}
          >
            {filterError || "فیلترهای فعال روی نتیجه اعمال شده‌اند."}
          </Typography>
          {hasActiveFilters && (
            <Button size="small" variant="text" onClick={clearFilters}>
              پاک‌سازی فیلترها
            </Button>
          )}
        </Stack>
      )}

      {dynamicFiltersOpen && classificationId != null && (
        <DynamicCaseFilters
          classificationId={classificationId}
          value={dynamicFilterExpression}
          onClose={() => setDynamicFiltersOpen(false)}
          onApply={(expression) => {
            updateSearchParams((next) => {
              setOptionalQueryParam(
                next,
                CASE_LIST_QUERY_KEYS.dynamicFilters,
                serializeDynamicFilters(expression),
              );
              next.delete(CASE_LIST_QUERY_KEYS.page);
            });
          }}
        />
      )}

      {classificationId == null && (
        <Alert severity="info">
          برای مشاهده پرونده‌ها یک طبقه‌بندی انتخاب کنید.
        </Alert>
      )}
      {classificationId != null &&
        !isArchiveClassification &&
        !selectedClassificationIsLeaf && (
          <Alert severity="warning">
            ایجاد پرونده فقط در آخرین سطح دسته‌بندی مجاز است.
          </Alert>
        )}
      {classificationId != null &&
        !isArchiveClassification &&
        canWriteByRole &&
        !isWritableClassificationsLoading &&
        !selectedClassificationWritable && (
          <Alert severity="warning">
            برای این دسته‌بندی دسترسی ایجاد پرونده ندارید.
          </Alert>
        )}

      {isCompact ? (
        <Stack spacing={1}>
          {query.isLoading ? (
            <Stack sx={{ p: 1 }} spacing={1}>
              <Skeleton variant="rounded" height={80} />
              <Skeleton variant="rounded" height={80} />
              <Skeleton variant="rounded" height={80} />
            </Stack>
          ) : rows.length === 0 ? (
            <Paper variant="outlined" sx={{ p: 2.5, textAlign: "center" }}>
              <Typography variant="body2" color="text.secondary">
                پرونده‌ای یافت نشد.
              </Typography>
            </Paper>
          ) : (
            rows.map((row) => (
              <Paper
                key={row.id}
                variant="outlined"
                sx={{ p: 1.25, cursor: "pointer" }}
                onClick={() =>
                  openCase(row.caseNumber || row.externalCaseNumber)
                }
              >
                <Stack spacing={0.75}>
                  <Stack direction="row" spacing={1} alignItems="center">
                    <Avatar
                      src={buildThumbnailUrl(row.avatarImagePath) ?? undefined}
                      sx={{
                        width: 32,
                        height: 32,
                        bgcolor: "primary.main",
                        fontSize: 12,
                        fontWeight: 700,
                      }}
                      imgProps={{ loading: "lazy" }}
                    >
                      {getCaseInitials(row) || (
                        <FolderOutlinedIcon fontSize="small" />
                      )}
                    </Avatar>
                    <Stack spacing={0.25}>
                      <Typography variant="subtitle2">
                        {row.caseNumber}
                      </Typography>
                      <Typography variant="body2" color="text.secondary">
                        {row.externalCaseNumber}
                      </Typography>
                    </Stack>
                  </Stack>
                  <Typography variant="body2">{row.address}</Typography>
                  <Stack
                    direction="row"
                    spacing={1}
                    justifyContent="space-between"
                    flexWrap="wrap"
                  >
                    <Typography variant="caption" color="text.secondary">
                      طبقه: {row.classificationTitle}
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      {formatServerDateTime(row.updatedAt)}
                    </Typography>
                  </Stack>
                </Stack>
              </Paper>
            ))
          )}

          <Stack
            direction="row"
            spacing={1}
            justifyContent="space-between"
            alignItems="center"
          >
            <Button
              size="small"
              variant="outlined"
              disabled={!hasPreviousPage}
              onClick={() => {
                updateSearchParams((next) => {
                  const previousPage = pagination.page;
                  setOptionalQueryParam(
                    next,
                    CASE_LIST_QUERY_KEYS.page,
                    previousPage <= 1 ? null : previousPage,
                  );
                });
              }}
            >
              صفحه قبل
            </Button>
            <Typography variant="caption" color="text.secondary">
              صفحه {pagination.page + 1} از{" "}
              {Math.max(1, Math.ceil(rowCount / pagination.pageSize))}
            </Typography>
            <Button
              size="small"
              variant="outlined"
              disabled={!hasNextPage}
              onClick={() => {
                updateSearchParams((next) => {
                  next.set(
                    CASE_LIST_QUERY_KEYS.page,
                    String(pagination.page + 2),
                  );
                });
              }}
            >
              صفحه بعد
            </Button>
          </Stack>
        </Stack>
      ) : (
        <Box className="table-scroll" dir="rtl">
          <Box sx={{ height: { xs: 460, md: 520 }, minWidth: 900 }}>
            <DataGrid
              rows={rows}
              columns={columns}
              loading={query.isLoading}
              disableRowSelectionOnClick
              rowCount={rowCount}
              paginationMode="server"
              sortingMode="server"
              paginationModel={pagination}
              onPaginationModelChange={(model) => {
                if (
                  model.page === pagination.page &&
                  model.pageSize === pagination.pageSize
                ) {
                  return;
                }

                updateSearchParams((next) => {
                  setOptionalQueryParam(
                    next,
                    CASE_LIST_QUERY_KEYS.page,
                    model.page === 0 ? null : model.page + 1,
                  );
                  setOptionalQueryParam(
                    next,
                    CASE_LIST_QUERY_KEYS.pageSize,
                    model.pageSize === 10 ? null : model.pageSize,
                  );
                });
              }}
              sortModel={sortModel}
              onSortModelChange={(model) => {
                const sortBy =
                  (model[0]?.field as CaseSearchRequest["sortBy"]) ??
                  "updatedAt";
                const sortDirection = model[0]?.sort === "asc" ? "asc" : "desc";
                if (
                  sortBy === urlState.sortBy &&
                  sortDirection === urlState.sortDirection
                ) {
                  return;
                }

                updateSearchParams((next) => {
                  setOptionalQueryParam(
                    next,
                    CASE_LIST_QUERY_KEYS.sortBy,
                    sortBy === "updatedAt" ? null : sortBy,
                  );
                  setOptionalQueryParam(
                    next,
                    CASE_LIST_QUERY_KEYS.sortDirection,
                    sortDirection === "desc" ? null : sortDirection,
                  );
                  next.delete(CASE_LIST_QUERY_KEYS.page);
                });
              }}
              onRowClick={(params) =>
                openCase(params.row.caseNumber || params.row.externalCaseNumber)
              }
              pageSizeOptions={[10, 20, 50]}
              sx={{
                width: "100%",
                minWidth: 0,
                direction: "rtl",
                "& .MuiDataGrid-columnHeaders": {
                  bgcolor: "rgba(15, 118, 110, 0.08)",
                },
                "& .MuiDataGrid-cell, & .MuiDataGrid-columnHeader": {
                  boxSizing: "border-box",
                  px: 1,
                },
                "& .MuiDataGrid-columnHeaderTitle": {
                  width: "100%",
                  fontWeight: 700,
                  textAlign: "right",
                },
                "& .MuiDataGrid-columnHeader--alignCenter .MuiDataGrid-columnHeaderTitle":
                  {
                    textAlign: "center",
                  },
              }}
              slots={{
                noRowsOverlay: () => (
                  <Stack
                    alignItems="center"
                    justifyContent="center"
                    sx={{ height: "100%" }}
                  >
                    <Typography variant="body2" color="text.secondary">
                      پرونده‌ای یافت نشد.
                    </Typography>
                  </Stack>
                ),
                loadingOverlay: () => (
                  <Stack sx={{ p: 2 }} spacing={1}>
                    <Skeleton variant="rounded" height={38} />
                    <Skeleton variant="rounded" height={38} />
                    <Skeleton variant="rounded" height={38} />
                  </Stack>
                ),
              }}
            />
          </Box>
        </Box>
      )}

      <CreateDossierModal
        open={createOpen}
        isSubmitting={createMutation.isPending}
        isBehsazEnabled={Boolean(behsazConfigQuery.data)}
        onClose={() => setCreateOpen(false)}
        onSubmit={createCase}
      />

      <Dialog
        open={moveDialogOpen}
        onClose={() => {
          if (!moveMutation.isPending) {
            setMoveDialogOpen(false);
          }
        }}
        maxWidth="sm"
        fullWidth
        PaperProps={{
          sx: {
            borderRadius: 3,
            overflow: "hidden",
          },
        }}
      >
        <DialogTitle sx={{ px: 3, py: 2.25 }}>
          <Stack direction="row" spacing={1.5} alignItems="center">
            <Box
              sx={{
                width: 42,
                height: 42,
                borderRadius: 2,
                display: "grid",
                placeItems: "center",
                bgcolor: "primary.50",
                color: "primary.main",
              }}
            >
              <DriveFileMoveOutlinedIcon />
            </Box>
            <Box>
              <Typography variant="h6" fontWeight={800}>
                {isArchiveClassification
                  ? "بازیابی / انتقال پرونده"
                  : "انتقال پرونده"}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                مقصد جدید پرونده را از دسته‌بندی‌های مجاز انتخاب کنید.
              </Typography>
            </Box>
          </Stack>
        </DialogTitle>

        <DialogContent dividers sx={{ px: 3, py: 2.5 }}>
          <Stack spacing={2}>
            <Paper
              variant="outlined"
              sx={{
                p: 1.5,
                borderRadius: 2,
                bgcolor: "grey.50",
              }}
            >
              <Stack spacing={0.5}>
                <Typography variant="caption" color="text.secondary">
                  پرونده انتخاب‌شده
                </Typography>
                <Typography variant="subtitle1" fontWeight={700}>
                  {selectedCase?.caseNumber ?? "-"}
                </Typography>
                {selectedCase?.classificationTitle ? (
                  <Typography variant="body2" color="text.secondary">
                    دسته فعلی: {selectedCase.classificationTitle}
                  </Typography>
                ) : null}
              </Stack>
            </Paper>

            <FormControl size="small" fullWidth>
              <InputLabel id="move-case-target-label">
                دسته‌بندی مقصد
              </InputLabel>
              <Select
                labelId="move-case-target-label"
                label="دسته‌بندی مقصد"
                value={moveTargetClassification}
                onChange={(event) =>
                  setMoveTargetClassification(Number(event.target.value))
                }
              >
                {moveTargetClassifications
                  .filter((item) => item.id !== selectedCase?.classificationId)
                  .map((item) => (
                    <MenuItem key={item.id} value={item.id}>
                      <Stack spacing={0.25}>
                        <Typography variant="body2" fontWeight={700}>
                          {item.title}
                        </Typography>
                        {/* <Typography variant="caption" color="text.secondary">
                          {item.path}
                        </Typography> */}
                      </Stack>
                    </MenuItem>
                  ))}
              </Select>
            </FormControl>

            <Alert severity="info" sx={{ borderRadius: 2 }}>
              فقط دسته‌بندی‌های فعال، نهایی و قابل‌نوشتن در این فهرست نمایش داده
              می‌شوند.
            </Alert>
          </Stack>
        </DialogContent>

        <DialogActions sx={{ px: 3, py: 2, gap: 1 }}>
          <Button
            onClick={() => setMoveDialogOpen(false)}
            disabled={moveMutation.isPending}
            variant="outlined"
          >
            انصراف
          </Button>
          <Button
            onClick={() => void handleMoveCase()}
            variant="contained"
            disabled={moveMutation.isPending || !moveTargetClassification}
            startIcon={
              moveMutation.isPending ? <CircularProgress size={18} /> : null
            }
          >
            {moveMutation.isPending ? "در حال انتقال..." : "تأیید انتقال"}
          </Button>
        </DialogActions>
      </Dialog>
    </Stack>
  );
};
