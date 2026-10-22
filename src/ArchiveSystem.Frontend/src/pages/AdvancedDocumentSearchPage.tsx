import SearchIcon from '@mui/icons-material/Search';
import DownloadIcon from '@mui/icons-material/Download';
import FolderOpenIcon from '@mui/icons-material/FolderOpen';
import {
  Alert,
  Box,
  Button,
  Checkbox,
  FormControlLabel,
  MenuItem,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import { useEffect, useMemo, useState } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { useNavigate } from 'react-router-dom';
import { AdvancedSmartFilter } from '@/components/dossiers/AdvancedSmartFilter';
import type { Category } from '@/components/dossiers/CategoryRadioTreeView';
import { advancedDocumentSearchApi } from '@/services/advancedDocumentSearchApi';
import { archiveApi } from '@/services/archiveApi';
import { dossierSearchApi } from '@/services/dossierSearchApi';
import { useClassificationTree } from '@/services/archiveQueries';
import type {
  DocumentSearchResultDto,
  AdvancedDocumentSearchRequest,
  DynamicFilter,
} from '@/types/advancedDocumentSearch';
import { formatOwnerNames } from '@/utils/ownerDisplay';
import type { ClassificationNode } from '@/types/archive';
import { compareClassificationsArchiveLast } from '@/utils/classificationOrder';
import type {
  DossierConditionGroup,
  DossierConditionRule,
  DossierSearchFieldDescriptor,
} from '@/types/dossierSearch';

interface AdvancedDocumentSearchFormValues {
  generalQuery: string;
  documentName: string;
  caseNumber: string;
  ownerName: string;
  bankName: string;
  tags: string;
  includeDeleted: boolean;
  sortBy: string;
  sortDirection: 'asc' | 'desc';
  pageSize: number;
  categoryId: string | null;
}

const defaultValues: AdvancedDocumentSearchFormValues = {
  generalQuery: '',
  documentName: '',
  caseNumber: '',
  ownerName: '',
  bankName: '',
  tags: '',
  includeDeleted: false,
  sortBy: 'modifiedAt',
  sortDirection: 'desc',
  pageSize: 20,
  categoryId: null,
};

const buildCategoryTree = (nodes: ClassificationNode[]): Category[] => {
  const byId = new Map<string, Category>();
  const roots: Category[] = [];

  nodes.forEach((node) => {
    byId.set(String(node.id), {
      id: String(node.id),
      name: node.title,
      systemKey: node.systemKey,
      children: [],
    });
  });

  nodes.forEach((node) => {
    const category = byId.get(String(node.id));
    if (!category) {
      return;
    }

    if (node.parentId == null) {
      roots.push(category);
      return;
    }

    const parent = byId.get(String(node.parentId));
    if (parent) {
      parent.children = [...(parent.children ?? []), category];
      return;
    }

    roots.push(category);
  });

  const sortTree = (items: Category[]): Category[] =>
    items
      .sort(compareClassificationsArchiveLast)
      .map((item) => ({
        ...item,
        children: sortTree(item.children ?? []),
      }));

  return sortTree(roots);
};

export const AdvancedDocumentSearchPage = () => {
  const navigate = useNavigate();
  const { control, register, watch } = useForm<AdvancedDocumentSearchFormValues>({
    defaultValues,
  });

  const [pageNumber, setPageNumber] = useState(1);
  const [downloadingDocumentId, setDownloadingDocumentId] = useState<number | null>(null);
  const [downloadError, setDownloadError] = useState('');
  const [smartFilters, setSmartFilters] = useState<DynamicFilter[]>([]);
  const [availableFields, setAvailableFields] = useState<DossierSearchFieldDescriptor[]>([]);
  const [fieldsError, setFieldsError] = useState('');
  const [fieldsLoading, setFieldsLoading] = useState(false);
  const [shouldRefetch, setShouldRefetch] = useState(false);
  const classificationQuery = useClassificationTree();

  const values = watch();
  const categoryTree = useMemo(
    () => buildCategoryTree(classificationQuery.data ?? []),
    [classificationQuery.data],
  );

  const payload = useMemo<AdvancedDocumentSearchRequest>(() => {
    const categoryFilter = values.categoryId
      ? [{
          source: 'dossier' as const,
          field: 'ClassificationId',
          operator: 'Equals' as const,
          value: values.categoryId,
        }]
      : [];

    const tags = values.tags
      .split(',')
      .map((tag) => tag.trim())
      .filter((tag) => tag.length > 0);

    return {
      generalQuery: values.generalQuery || undefined,
      documentName: values.documentName || undefined,
      caseNumber: values.caseNumber || undefined,
      ownerName: values.ownerName || undefined,
      bankName: values.bankName || undefined,
      tags,
      filters: [...categoryFilter, ...smartFilters],
      includeDeleted: values.includeDeleted,
      sortBy: values.sortBy,
      sortDirection: values.sortDirection,
      pageNumber,
      pageSize: values.pageSize,
    };
  }, [pageNumber, smartFilters, values]);

  const { data, error, refetch, isFetching } = useQuery({
    queryKey: ['advancedDocumentSearch', payload],
    queryFn: () => advancedDocumentSearchApi.searchDocuments(payload),
    enabled: false,
  });

  const loadFields = async () => {
    setFieldsLoading(true);
    setFieldsError('');
    try {
      const response = await dossierSearchApi.getAdvancedSearchFields();
      setAvailableFields(response.fields ?? []);
    } catch {
      setAvailableFields([]);
      setFieldsError('بارگذاری فیلدهای فیلتر هوشمند ناموفق بود.');
    } finally {
      setFieldsLoading(false);
    }
  };

  const handleSearch = () => {
    setPageNumber(1);
    setShouldRefetch(true);
  };

  useEffect(() => {
    if (!shouldRefetch) {
      return;
    }

    const run = async () => {
      try {
        await refetch();
      } finally {
        setShouldRefetch(false);
      }
    };

    void run();
  }, [refetch, shouldRefetch]);

  const handleDownload = async (item: DocumentSearchResultDto) => {
    setDownloadError('');
    setDownloadingDocumentId(item.documentId);

    try {
      const url = await archiveApi.downloadFile(item.documentId);
      const anchor = document.createElement('a');
      anchor.href = url;
      anchor.download = item.documentName || `document-${item.documentId}`;
      document.body.appendChild(anchor);
      anchor.click();
      anchor.remove();
      URL.revokeObjectURL(url);
    } catch {
      setDownloadError('دانلود فایل ناموفق بود.');
    } finally {
      setDownloadingDocumentId(null);
    }
  };

  const handleOpenDossier = (dossierNumber: string) => {
    const normalized = dossierNumber.trim();
    if (!normalized) {
      return;
    }

    navigate(`/cases/${encodeURIComponent(normalized)}`);
  };

  const totalCount = data?.totalCount ?? 0;
  const canGoPrevious = pageNumber > 1;
  const canGoNext = pageNumber * values.pageSize < totalCount;

  return (
    <Stack spacing={1.5}>
      <Typography variant="h5">جستجوی پیشرفته یکپارچه اسناد</Typography>

      <Paper variant="outlined" sx={{ p: 1.5 }}>
        <Stack spacing={1.25}>
          {/* <Typography variant="subtitle1">فیلترهای سند</Typography>
          <Stack direction={{ xs: 'column', md: 'row' }} spacing={1}>
            <TextField size="small" label="جستجوی عمومی" {...register('generalQuery')} fullWidth />
            <TextField size="small" label="نام سند" {...register('documentName')} fullWidth />
          </Stack> */}

          <Typography variant="subtitle1">فیلترهای پرونده</Typography>
          <Stack direction={{ xs: 'column', md: 'row' }} spacing={1}>
            <TextField size="small" label="شماره پرونده" {...register('caseNumber')} fullWidth />
            <TextField size="small" label="نام مالک" {...register('ownerName')} fullWidth />
            {/* <TextField size="small" label="نام بانک" {...register('bankName')} fullWidth /> */}
          </Stack>

          <Stack
            spacing={1}
            sx={{
              display: 'grid',
              gridTemplateColumns: {
                xs: '1fr',
                md: 'minmax(12rem, 1.25fr) minmax(10rem, 1fr) minmax(8rem, 0.8fr) minmax(8rem, 0.75fr)',
              },
            }}
          >
            {/* <TextField
              size="small"
              label="تگ‌ها (با , جدا کنید)"
              {...register('tags')}
              fullWidth
            /> */}

            <TextField
              select
              size="small"
              label="مرتب‌سازی"
              {...register('sortBy')}
              defaultValue="modifiedAt"
              fullWidth
            >
              <MenuItem value="modifiedAt">آخرین تغییر</MenuItem>
              <MenuItem value="createdAt">تاریخ ایجاد</MenuItem>
              <MenuItem value="documentName">نام سند</MenuItem>
              <MenuItem value="dossierNumber">شماره پرونده</MenuItem>
              <MenuItem value="ownerName">مالک</MenuItem>
            </TextField>

            <TextField
              select
              size="small"
              label="جهت"
              {...register('sortDirection')}
              defaultValue="desc"
              fullWidth
            >
              <MenuItem value="desc">نزولی</MenuItem>
              <MenuItem value="asc">صعودی</MenuItem>
            </TextField>

            <TextField
              select
              size="small"
              label="اندازه صفحه"
              {...register('pageSize', {
                setValueAs: (value) => Number(value),
              })}
              defaultValue={20}
              fullWidth
            >
              <MenuItem value={10}>10</MenuItem>
              <MenuItem value={20}>20</MenuItem>
              <MenuItem value={50}>50</MenuItem>
              <MenuItem value={100}>100</MenuItem>
            </TextField>
          </Stack>

          <FormControlLabel
            control={<Checkbox {...register('includeDeleted')} />}
            label="نمایش موارد حذف نرم"
          />

          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1}>
            <Button type="button" variant="outlined" onClick={() => void loadFields()} disabled={fieldsLoading}>
              بارگذاری فیلتر هوشمند
            </Button>
            <Button
              type="button"
              variant="contained"
              startIcon={<SearchIcon />}
              disabled={isFetching}
              onClick={() => {
                handleSearch();
              }}
            >
              جستجو
            </Button>
          </Stack>
        </Stack>
      </Paper>

      {fieldsError && <Alert severity="error">{fieldsError}</Alert>}

      {availableFields.length > 0 && (
        <Paper variant="outlined" sx={{ p: 1.5 }}>
          <Controller
            name="categoryId"
            control={control}
            render={({ field }) => (
              <AdvancedSmartFilter
                fields={availableFields}
                categories={categoryTree}
                categoryValue={field.value ?? null}
                loading={fieldsLoading || isFetching || classificationQuery.isLoading}
                onCategoryChange={field.onChange}
                onApply={(root: DossierConditionGroup) => {
                  const mapped = flattenRules(root).map(mapRuleToDynamicFilter).filter((x): x is DynamicFilter => Boolean(x));
                  setSmartFilters(mapped);
                  setPageNumber(1);
                  setShouldRefetch(true);
                }}
              />
            )}
          />
        </Paper>
      )}

      {error && <Alert severity="error">درخواست جستجو ناموفق بود.</Alert>}
      {downloadError && <Alert severity="error">{downloadError}</Alert>}

      <Paper variant="outlined" sx={{ p: 1.5 }}>
        <Stack direction={{ xs: 'column', sm: 'row' }} justifyContent="space-between" spacing={1}>
          <Typography variant="subtitle1">تعداد نتایج: {totalCount}</Typography>
          <Stack direction="row" spacing={1}>
            <Button
              size="small"
              variant="outlined"
              disabled={isFetching || !canGoPrevious}
              onClick={() => {
                const nextPage = pageNumber - 1;
                setPageNumber(nextPage);
                setShouldRefetch(true);
              }}
            >
              قبلی
            </Button>
            <Button
              size="small"
              variant="outlined"
              disabled={isFetching || !canGoNext}
              onClick={() => {
                const nextPage = pageNumber + 1;
                setPageNumber(nextPage);
                setShouldRefetch(true);
              }}
            >
              بعدی
            </Button>
          </Stack>
        </Stack>

        <Box className="table-scroll" sx={{ mt: 1 }}>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>شناسه سند</TableCell>
                <TableCell>نام سند</TableCell>
                <TableCell>شماره پرونده</TableCell>
                <TableCell>مالک</TableCell>
                <TableCell>بانک</TableCell>
                <TableCell>پسوند</TableCell>
                <TableCell>نوع محتوا</TableCell>
                <TableCell>آخرین تغییر</TableCell>
                <TableCell>حذف‌شده</TableCell>
                <TableCell align="center">عملیات</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {(data?.items ?? []).map((item) => {
                const dossierNumber = item.dossierNumber.trim();
                const isDownloading = downloadingDocumentId === item.documentId;

                return (
                  <TableRow key={item.documentId} hover>
                    <TableCell>{item.documentId}</TableCell>
                    <TableCell>{item.documentName}</TableCell>
                    <TableCell>{item.dossierNumber}</TableCell>
                    <TableCell>{formatOwnerNames(item.owners)}</TableCell>
                    <TableCell>{item.bankName ?? '-'}</TableCell>
                    <TableCell>{item.extension}</TableCell>
                    <TableCell>{item.contentType}</TableCell>
                    <TableCell>{new Date(item.modifiedAt).toLocaleString('fa-IR')}</TableCell>
                    <TableCell>{item.isDeleted ? 'بله' : 'خیر'}</TableCell>
                    <TableCell align="center">
                      <Stack direction="row" spacing={0.5} justifyContent="center">
                        <Tooltip title="دانلود فایل">
                          <span>
                            <Button
                              size="small"
                              variant="outlined"
                              startIcon={<DownloadIcon fontSize="small" />}
                              disabled={isDownloading}
                              onClick={() => void handleDownload(item)}
                            >
                              دانلود فایل
                            </Button>
                          </span>
                        </Tooltip>
                        <Tooltip title="مشاهده پرونده">
                          <span>
                            <Button
                              size="small"
                              variant="outlined"
                              startIcon={<FolderOpenIcon fontSize="small" />}
                              disabled={!dossierNumber}
                              onClick={() => handleOpenDossier(dossierNumber)}
                            >
                              مشاهده پرونده
                            </Button>
                          </span>
                        </Tooltip>
                      </Stack>
                    </TableCell>
                  </TableRow>
                );
              })}

              {(data?.items.length ?? 0) === 0 && (
                <TableRow>
                  <TableCell colSpan={10}>
                    <Typography variant="body2" color="text.secondary">
                      نتیجه‌ای یافت نشد.
                    </Typography>
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </Box>
      </Paper>
    </Stack>
  );
};

const flattenRules = (group: DossierConditionGroup): DossierConditionRule[] => {
  const current = group.rules ?? [];
  const nested = (group.groups ?? []).flatMap((child) => flattenRules(child));
  return [...current, ...nested];
};

const mapRuleToDynamicFilter = (rule: DossierConditionRule): DynamicFilter | null => {
  const fallbackValue = rule.values?.[0];
  const value = rule.value ?? fallbackValue;

  return {
    source: mapSource(rule.source),
    field: rule.dynamicFieldApiName ?? rule.field,
    operator: mapOperator(rule.operator),
    value: value == null ? undefined : String(value),
    valueTo: rule.values?.[1] == null ? undefined : String(rule.values[1]),
    values: rule.values?.map((item) => String(item)),
    valueType: rule.valueType,
  };
};

const mapSource = (source: string): DynamicFilter['source'] => {
  switch (source) {
    case 'Owner':
      return 'owner';
    case 'DynamicField':
      return 'dynamicField';
    case 'Dossier':
    default:
      return 'dossier';
  }
};

const mapOperator = (operator: string): DynamicFilter['operator'] => {
  switch (operator) {
    case 'Contains':
      return 'Contains';
    case 'Equals':
      return 'Equals';
    case 'StartsWith':
      return 'StartsWith';
    case 'EndsWith':
      return 'EndsWith';
    case 'NotEquals':
      return 'NotEquals';
    case 'GreaterThan':
      return 'GreaterThan';
    case 'GreaterThanOrEqual':
      return 'GreaterThanOrEqual';
    case 'LessThan':
      return 'LessThan';
    case 'LessThanOrEqual':
      return 'LessThanOrEqual';
    case 'Between':
      return 'Between';
    case 'In':
      return 'In';
    case 'NotIn':
      return 'NotIn';
    case 'IsNull':
      return 'IsNull';
    case 'IsNotNull':
      return 'IsNotNull';
    case 'IsEmpty':
      return 'IsEmpty';
    case 'IsNotEmpty':
      return 'IsNotEmpty';
    default:
      return 'Contains';
  }
};

export default AdvancedDocumentSearchPage;
