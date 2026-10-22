import { zodResolver } from '@hookform/resolvers/zod';
import SearchIcon from '@mui/icons-material/Search';
import {
  Autocomplete,
  Box,
  Button,
  MenuItem,
  Paper,
  Radio,
  RadioGroup,
  FormControlLabel,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import { useEffect, useState } from 'react';
import { Controller, useForm, useWatch } from 'react-hook-form';
import { z } from 'zod';
import { JalaliDateInput } from '@/components/common/JalaliDateInputs';
import { useAdvancedSearchStore } from '@/stores/useAdvancedSearchStore';
import { searchAdvancedSearchUsers } from '@/services/useAdvancedSearchApi';
import type {
  AdvancedSearchParams,
  AdvancedSearchUserOption,
  TagSearchMode,
} from '@/types/advancedSearch';
import { useDebouncedValue } from '@/utils/useDebouncedValue';

const advancedSearchFormSchema = z.object({
  generalSearchTerm: z.string(),
  dossierNumber: z.string(),
  postalCode: z.string(),
  ownerFirstName: z.string(),
  ownerLastName: z.string(),
  fromDate: z.string(),
  toDate: z.string(),
  createdBy: z.string(),
  tags: z.array(z.string()),
  tagMode: z.enum(['Any', 'All']),
  sortBy: z.enum(['Relevance', 'Date', 'FileName']),
  sortDirection: z.enum(['Desc', 'Asc']),
  pageSize: z.number().min(1).max(100),
});

type AdvancedSearchFormValues = z.infer<typeof advancedSearchFormSchema>;

export const AdvancedSearchForm = () => {
  const searchParams = useAdvancedSearchStore((state) => state.searchParams);
  const setSearchParam = useAdvancedSearchStore((state) => state.setSearchParam);
  const setSearchParams = useAdvancedSearchStore((state) => state.setSearchParams);
  const executeSearch = useAdvancedSearchStore((state) => state.executeSearch);
  const resetSearch = useAdvancedSearchStore((state) => state.resetSearch);
  const isLoading = useAdvancedSearchStore((state) => state.isLoading);
  const [createdByInput, setCreatedByInput] = useState('');
  const debouncedCreatedByInput = useDebouncedValue(createdByInput, 300);
  const usersQuery = useQuery({
    queryKey: ['advanced-search-users', debouncedCreatedByInput],
    queryFn: () => searchAdvancedSearchUsers(debouncedCreatedByInput),
  });
  const userOptions = usersQuery.data ?? [];

  const { control, register, handleSubmit, reset } = useForm<AdvancedSearchFormValues>({
    resolver: zodResolver(advancedSearchFormSchema),
    defaultValues: {
      generalSearchTerm: searchParams.generalSearchTerm,
      dossierNumber: searchParams.dossierNumber,
      postalCode: searchParams.postalCode,
      ownerFirstName: searchParams.ownerFirstName,
      ownerLastName: searchParams.ownerLastName,
      fromDate: searchParams.fromDate,
      toDate: searchParams.toDate,
      createdBy: searchParams.createdBy,
      tags: searchParams.tags,
      tagMode: searchParams.tagMode,
      sortBy: searchParams.sortBy,
      sortDirection: searchParams.sortDirection,
      pageSize: searchParams.pageSize,
    },
  });

  const generalSearchTerm = useWatch({ control, name: 'generalSearchTerm' });
  const tagMode = useWatch({ control, name: 'tagMode' });

  useEffect(() => {
    reset({
      generalSearchTerm: searchParams.generalSearchTerm,
      dossierNumber: searchParams.dossierNumber,
      postalCode: searchParams.postalCode,
      ownerFirstName: searchParams.ownerFirstName,
      ownerLastName: searchParams.ownerLastName,
      fromDate: searchParams.fromDate,
      toDate: searchParams.toDate,
      createdBy: searchParams.createdBy,
      tags: searchParams.tags,
      tagMode: searchParams.tagMode,
      sortBy: searchParams.sortBy,
      sortDirection: searchParams.sortDirection,
      pageSize: searchParams.pageSize,
    });
  }, [searchParams, reset]);

  useEffect(() => {
    const timer = window.setTimeout(() => {
      if ((generalSearchTerm ?? '').trim() !== searchParams.generalSearchTerm) {
        setSearchParam('generalSearchTerm', generalSearchTerm ?? '');
        void executeSearch(true);
      }
    }, 300);

    return () => window.clearTimeout(timer);
  }, [executeSearch, generalSearchTerm, searchParams.generalSearchTerm, setSearchParam]);

  useEffect(() => {
    if (tagMode && tagMode !== searchParams.tagMode) {
      setSearchParam('tagMode', tagMode as TagSearchMode);
    }
  }, [searchParams.tagMode, setSearchParam, tagMode]);

  const onSubmit = handleSubmit(async (values) => {
    const nextParams: Partial<AdvancedSearchParams> = {
      generalSearchTerm: values.generalSearchTerm,
      dossierNumber: values.dossierNumber,
      postalCode: values.postalCode,
      ownerFirstName: values.ownerFirstName,
      ownerLastName: values.ownerLastName,
      fromDate: values.fromDate,
      toDate: values.toDate,
      createdBy: values.createdBy,
      tags: values.tags,
      tagMode: values.tagMode,
      sortBy: values.sortBy,
      sortDirection: values.sortDirection,
      pageSize: values.pageSize,
      pageNumber: 1,
    };

    setSearchParams(nextParams);
    await executeSearch(true);
  });

  return (
    <Paper variant="outlined" sx={{ p: 2 }}>
      <Stack component="form" onSubmit={onSubmit} spacing={2}>
        <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
          جستجوی پیشرفته اسناد
        </Typography>

        <TextField
          label="عبارت عمومی (OCR / نام فایل / تگ)"
          size="small"
          {...register('generalSearchTerm')}
        />

        <Box
          sx={{
            display: 'grid',
            gap: 1.25,
            gridTemplateColumns: {
              xs: '1fr',
              sm: 'repeat(2, minmax(0, 1fr))',
              lg: 'repeat(3, minmax(0, 1fr))',
            },
          }}
        >
          <TextField size="small" label="شماره پرونده" {...register('dossierNumber')} />
          <TextField size="small" label="کد پستی" {...register('postalCode')} />
          <TextField size="small" label="نام مالک" {...register('ownerFirstName')} />
          <TextField size="small" label="نام خانوادگی مالک" {...register('ownerLastName')} />
          <Controller
            control={control}
            name="fromDate"
            render={({ field }) => (
              <Stack spacing={0.5}>
                <Typography
                  variant="caption"
                  color="text.secondary"
                  sx={{ minHeight: 20, fontWeight: 500 }}
                >
                  از تاریخ
                </Typography>
                <JalaliDateInput
                  value={field.value}
                  onChange={field.onChange}
                  inputClassName="text-sm"
                />
              </Stack>
            )}
          />
          <Controller
            control={control}
            name="toDate"
            render={({ field }) => (
              <Stack spacing={0.5}>
                <Typography
                  variant="caption"
                  color="text.secondary"
                  sx={{ minHeight: 20, fontWeight: 500 }}
                >
                  تا تاریخ
                </Typography>
                <JalaliDateInput
                  value={field.value}
                  onChange={field.onChange}
                  inputClassName="text-sm"
                />
              </Stack>
            )}
          />
          <Controller
            control={control}
            name="createdBy"
            render={({ field }) => (
              <Autocomplete<AdvancedSearchUserOption, false, false, false>
                options={userOptions}
                value={
                  userOptions.find((option) => option.username === field.value) ??
                  null
                }
                loading={usersQuery.isFetching}
                filterOptions={(options) => options}
                isOptionEqualToValue={(option, value) => option.id === value.id}
                getOptionLabel={(option) =>
                  option.fullName
                    ? `${option.fullName} (${option.username})`
                    : option.username
                }
                noOptionsText="کاربری یافت نشد."
                onInputChange={(_event, value, reason) => {
                  if (reason === 'input') {
                    setCreatedByInput(value);
                  }
                }}
                onChange={(_event, value) => {
                  field.onChange(value?.username ?? '');
                }}
                renderInput={(params) => (
                  <TextField
                    {...params}
                    size="small"
                    label="کاربر ثبت‌کننده"
                    placeholder="نام یا نام کاربری"
                  />
                )}
              />
            )}
          />

          <Controller
            control={control}
            name="pageSize"
            render={({ field }) => (
              <TextField
                select
                label="تعداد در هر صفحه"
                size="small"
                value={field.value}
                onChange={(event) => field.onChange(Number(event.target.value))}
              >
                <MenuItem value={10}>10</MenuItem>
                <MenuItem value={20}>20</MenuItem>
                <MenuItem value={50}>50</MenuItem>
                <MenuItem value={100}>100</MenuItem>
              </TextField>
            )}
          />

          <Controller
            control={control}
            name="sortBy"
            render={({ field }) => (
              <TextField
                select
                label="مرتب‌سازی"
                size="small"
                value={field.value}
                onChange={(event) => field.onChange(event.target.value)}
              >
                <MenuItem value="Relevance">بیشترین ارتباط</MenuItem>
                <MenuItem value="Date">تاریخ</MenuItem>
                <MenuItem value="FileName">نام فایل</MenuItem>
              </TextField>
            )}
          />

          <Controller
            control={control}
            name="sortDirection"
            render={({ field }) => (
              <TextField
                select
                label="جهت مرتب‌سازی"
                size="small"
                value={field.value}
                onChange={(event) => field.onChange(event.target.value)}
              >
                <MenuItem value="Desc">نزولی</MenuItem>
                <MenuItem value="Asc">صعودی</MenuItem>
              </TextField>
            )}
          />
        </Box>

        <Controller
          control={control}
          name="tags"
          render={({ field }) => (
            <Autocomplete
              multiple
              freeSolo
              options={[]}
              value={field.value}
              onChange={(_event, value) => field.onChange(value)}
              renderInput={(params) => <TextField {...params} label="تگ‌ها" size="small" placeholder="برای افزودن Enter بزنید" />}
            />
          )}
        />

        <Controller
          control={control}
          name="tagMode"
          render={({ field }) => (
            <RadioGroup row value={field.value} onChange={(event) => field.onChange(event.target.value)}>
              <FormControlLabel value="Any" control={<Radio size="small" />} label="تطبیق هر تگ (OR)" />
              <FormControlLabel value="All" control={<Radio size="small" />} label="تطبیق همه تگ‌ها (AND)" />
            </RadioGroup>
          )}
        />

        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1}>
          <Button type="submit" variant="contained" startIcon={<SearchIcon />} disabled={isLoading}>
            جستجو
          </Button>
          <Button
            type="button"
            variant="outlined"
            disabled={isLoading}
            onClick={() => {
              resetSearch();
              reset({
                generalSearchTerm: '',
                dossierNumber: '',
                postalCode: '',
                ownerFirstName: '',
                ownerLastName: '',
                fromDate: '',
                toDate: '',
                createdBy: '',
                tags: [],
                tagMode: 'Any',
                sortBy: 'Relevance',
                sortDirection: 'Desc',
                pageSize: 20,
              });
            }}
          >
            پاک‌سازی
          </Button>
        </Stack>
      </Stack>
    </Paper>
  );
};
