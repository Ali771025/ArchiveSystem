import {
  Alert,
  Button,
  Chip,
  Paper,
  Skeleton,
  Stack,
  Typography,
} from '@mui/material';
import { useAdvancedSearchStore } from '@/stores/useAdvancedSearchStore';
import { SearchResultItemView } from '@/components/search/SearchResultItem';

export const SearchResults = () => {
  const results = useAdvancedSearchStore((state) => state.results);
  const isLoading = useAdvancedSearchStore((state) => state.isLoading);
  const error = useAdvancedSearchStore((state) => state.error);
  const initialLoad = useAdvancedSearchStore((state) => state.initialLoad);
  const searchParams = useAdvancedSearchStore((state) => state.searchParams);
  const setSearchParam = useAdvancedSearchStore((state) => state.setSearchParam);
  const executeSearch = useAdvancedSearchStore((state) => state.executeSearch);

  const totalPages = results?.totalPages ?? 1;
  const canGoPrevious = searchParams.pageNumber > 1;
  const canGoNext = searchParams.pageNumber < totalPages;
  const formatFilterDate = (value: string) =>
    new Date(`${value}T00:00:00`).toLocaleDateString('fa-IR');
  const clearFilter = async (
    key: keyof typeof searchParams,
    value: string | string[],
  ) => {
    setSearchParam(key, value as never);
    await executeSearch(true);
  };
  const activeFilterChips = [
    searchParams.generalSearchTerm.trim()
      ? {
          key: 'generalSearchTerm',
          label: `عبارت: ${searchParams.generalSearchTerm.trim()}`,
          onDelete: () => clearFilter('generalSearchTerm', ''),
        }
      : null,
    searchParams.dossierNumber.trim()
      ? {
          key: 'dossierNumber',
          label: `شماره پرونده: ${searchParams.dossierNumber.trim()}`,
          onDelete: () => clearFilter('dossierNumber', ''),
        }
      : null,
    searchParams.postalCode.trim()
      ? {
          key: 'postalCode',
          label: `کد پستی: ${searchParams.postalCode.trim()}`,
          onDelete: () => clearFilter('postalCode', ''),
        }
      : null,
    searchParams.ownerFirstName.trim()
      ? {
          key: 'ownerFirstName',
          label: `نام مالک: ${searchParams.ownerFirstName.trim()}`,
          onDelete: () => clearFilter('ownerFirstName', ''),
        }
      : null,
    searchParams.ownerLastName.trim()
      ? {
          key: 'ownerLastName',
          label: `نام خانوادگی مالک: ${searchParams.ownerLastName.trim()}`,
          onDelete: () => clearFilter('ownerLastName', ''),
        }
      : null,
    searchParams.fromDate
      ? {
          key: 'fromDate',
          label: `از تاریخ: ${formatFilterDate(searchParams.fromDate)}`,
          onDelete: () => clearFilter('fromDate', ''),
        }
      : null,
    searchParams.toDate
      ? {
          key: 'toDate',
          label: `تا تاریخ: ${formatFilterDate(searchParams.toDate)}`,
          onDelete: () => clearFilter('toDate', ''),
        }
      : null,
    searchParams.createdBy.trim()
      ? {
          key: 'createdBy',
          label: `کاربر ثبت‌کننده: ${searchParams.createdBy.trim()}`,
          onDelete: () => clearFilter('createdBy', ''),
        }
      : null,
    ...searchParams.tags.map((tag, index) => ({
      key: `tag-${index}`,
      label: `تگ: ${tag}`,
      onDelete: () =>
        clearFilter(
          'tags',
          searchParams.tags.filter((_, tagIndex) => tagIndex !== index),
        ),
    })),
  ].filter(
    (
      chip,
    ): chip is { key: string; label: string; onDelete: () => Promise<void> } =>
      chip !== null,
  );

  return (
    <Paper variant="outlined" sx={{ p: 2 }} aria-live="polite">
      <Stack spacing={1.25}>
        <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
          نتایج جستجو
        </Typography>

        {!initialLoad && activeFilterChips.length > 0 ? (
          <Stack direction="row" gap={0.75} flexWrap="wrap">
            {activeFilterChips.map((chip) => (
              <Chip
                key={chip.key}
                size="small"
                label={chip.label}
                onDelete={() => void chip.onDelete()}
              />
            ))}
          </Stack>
        ) : null}

        {error ? <Alert severity="error">{error}</Alert> : null}

        {initialLoad && !isLoading ? (
          <Typography variant="body2" color="text.secondary">
            برای شروع جستجو، فرم بالا را تکمیل کنید.
          </Typography>
        ) : null}

        {isLoading ? (
          <Stack spacing={1}>
            <Skeleton variant="rounded" height={88} />
            <Skeleton variant="rounded" height={88} />
            <Skeleton variant="rounded" height={88} />
          </Stack>
        ) : null}

        {!isLoading && !initialLoad && (results?.items.length ?? 0) === 0 ? (
          <Typography variant="body2" color="text.secondary">
            نتیجه‌ای برای جستجوی شما پیدا نشد. کلیدواژه‌ها یا فیلترها را تغییر دهید.
          </Typography>
        ) : null}

        {!isLoading && (results?.items.length ?? 0) > 0 ? (
          <Stack spacing={1}>
            {results?.items.map((item) => (
              <SearchResultItemView
                key={item.id}
                item={item}
                showCreatedAt={Boolean(searchParams.fromDate || searchParams.toDate)}
                showCreatedBy={Boolean(searchParams.createdBy.trim())}
              />
            ))}
          </Stack>
        ) : null}

        {results && !isLoading ? (
          <Stack spacing={1}>
            <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
              <Typography variant="caption" color="text.secondary">
                تگ‌های پرتکرار: {Object.entries(results.aggregations?.tags ?? {}).slice(0, 5).map(([key, value]) => `${key} (${value})`).join('، ') || '-'}
              </Typography>
              <Typography variant="caption" color="text.secondary">
                پرونده‌های پرتکرار: {Object.entries(results.aggregations?.dossiers ?? {}).slice(0, 5).map(([key, value]) => `${key} (${value})`).join('، ') || '-'}
              </Typography>
            </Stack>
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1} alignItems={{ sm: 'center' }} justifyContent="space-between">
            <Typography variant="caption" color="text.secondary">
              صفحه {results.pageNumber} از {results.totalPages} - مجموع {results.totalHits} نتیجه
            </Typography>
            <Stack direction="row" spacing={1}>
              <Button
                size="small"
                variant="outlined"
                disabled={!canGoPrevious}
                onClick={async () => {
                  setSearchParam('pageNumber', Math.max(1, searchParams.pageNumber - 1));
                  await executeSearch(false);
                }}
              >
                قبلی
              </Button>
              <Button
                size="small"
                variant="outlined"
                disabled={!canGoNext}
                onClick={async () => {
                  setSearchParam('pageNumber', searchParams.pageNumber + 1);
                  await executeSearch(false);
                }}
              >
                بعدی
              </Button>
            </Stack>
          </Stack>
          </Stack>
        ) : null}
      </Stack>
    </Paper>
  );
};
