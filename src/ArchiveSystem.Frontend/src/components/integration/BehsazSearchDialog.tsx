import {
  Alert,
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import Grid from '@mui/material/GridLegacy';
import { DataGrid, GridColDef, GridPaginationModel } from '@mui/x-data-grid';
import { useCallback, useEffect, useMemo, useState } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { archiveApi } from '@/services/archiveApi';
import { mapApiError } from '@/services/errorMapper';
import type { BehsazSearchRequest, BehsazSearchRow } from '@/types/archive';

type TaxpayerGroup = '' | 1 | 2;

interface BaseFormFields {
  instaurationFilterType: string;
  instaurationArea: string;
  instaurationZone: string;
  instaurationBlock: string;
  instaurationLand: string;
  instaurationBuilding: string;
  instaurationApartment: string;
  instaurationBusiness: string;
  systemNumber: string;
  fileNumber: string;
  ghateCode: string;
  mainPlate: string;
  subPlate: string;
  address: string;
  gisIndicated: '' | 'assigned' | 'unassigned';
}

interface TaxpayerFields {
  taxpayerGroup: TaxpayerGroup;
  firstName: string;
  lastName: string;
  nationalCode: string;
  companyName: string;
  nationalCompanyId: string;
}

export type BehsazSearchFormValues = BaseFormFields & TaxpayerFields;

interface BehsazSearchDialogProps {
  open: boolean;
  isEnabled: boolean;
  onClose: () => void;
  onRecordSelect: (row: BehsazSearchRow) => void;
}

const toIntOrUndefined = (value: string): number | undefined => {
  if (!value.trim()) {
    return undefined;
  }
  const parsed = Number.parseInt(value, 10);
  return Number.isFinite(parsed) ? parsed : undefined;
};

const toDecimalOrUndefined = (value: string): number | undefined => {
  if (!value.trim()) {
    return undefined;
  }
  const parsed = Number.parseFloat(value);
  return Number.isFinite(parsed) ? parsed : undefined;
};

const normalizePageDigits = (value: string): string =>
  value
    .replace(/[۰-۹]/g, (digit) => String(digit.charCodeAt(0) - 1776))
    .replace(/[٠-٩]/g, (digit) => String(digit.charCodeAt(0) - 1632));

export const resolveBehsazPageNumber = (
  value: string,
  totalPages: number,
  currentPage: number,
): number => {
  const normalized = normalizePageDigits(value.trim());
  if (!/^\d+$/.test(normalized)) {
    return currentPage;
  }

  const requestedPage = Number(normalized);
  if (!Number.isSafeInteger(requestedPage)) {
    return currentPage;
  }

  return Math.min(Math.max(requestedPage, 1), Math.max(totalPages, 1));
};

export const buildRequest = (
  values: BehsazSearchFormValues,
  pageModel: GridPaginationModel,
  userCode?: number,
): BehsazSearchRequest => {
  const common: BehsazSearchRequest = {
    page: pageModel.page + 1,
    pageIndex: pageModel.page,
    pageSize: pageModel.pageSize,
    userCode,
    instaurationFilterType: toIntOrUndefined(values.instaurationFilterType) ?? 1,
    instaurationArea: toIntOrUndefined(values.instaurationArea),
    instaurationZone: toIntOrUndefined(values.instaurationZone),
    instaurationBlock: toIntOrUndefined(values.instaurationBlock),
    instaurationLand: toIntOrUndefined(values.instaurationLand),
    instaurationBuilding: toIntOrUndefined(values.instaurationBuilding),
    instaurationApartment: toIntOrUndefined(values.instaurationApartment),
    instaurationBusiness: toIntOrUndefined(values.instaurationBusiness),
    systemNumber: values.systemNumber.trim() || undefined,
    fileNumber: values.fileNumber.trim() || undefined,
    ghateCode: toDecimalOrUndefined(values.ghateCode),
    registrationPlateMain: toDecimalOrUndefined(values.mainPlate),
    registrationPlateSub: toDecimalOrUndefined(values.subPlate),
    address: values.address.trim() || undefined,
    gisIndicated:
      values.gisIndicated === ''
        ? undefined
        : values.gisIndicated === 'assigned',
    taxpayerGroup: values.taxpayerGroup === '' ? undefined : values.taxpayerGroup,
    firstName: values.firstName.trim() || undefined,
    lastName: values.lastName.trim() || undefined,
    nationalCode: values.nationalCode.trim() || undefined,
    companyName: values.companyName.trim() || undefined,
    nationalCompanyId: values.nationalCompanyId.trim() || undefined,
  };
  return common;
};

export const BehsazSearchDialog = ({
  open,
  isEnabled,
  onClose,
  onRecordSelect,
}: BehsazSearchDialogProps) => {
  const form = useForm<BehsazSearchFormValues>({
    defaultValues: {
      instaurationFilterType: '1',
      instaurationArea: '',
      instaurationZone: '',
      instaurationBlock: '',
      instaurationLand: '',
      instaurationBuilding: '',
      instaurationApartment: '',
      instaurationBusiness: '',
      systemNumber: '',
      fileNumber: '',
      ghateCode: '',
      mainPlate: '',
      subPlate: '',
      address: '',
      gisIndicated: '',
      taxpayerGroup: '',
      firstName: '',
      lastName: '',
      nationalCode: '',
      companyName: '',
      nationalCompanyId: '',
    } as BehsazSearchFormValues,
  });

  const taxpayerGroup = form.watch('taxpayerGroup');
  const [rows, setRows] = useState<BehsazSearchRow[]>([]);
  const [rowCount, setRowCount] = useState(0);
  const [isLoading, setIsLoading] = useState(false);
  const [paginationModel, setPaginationModel] = useState<GridPaginationModel>({
    page: 0,
    pageSize: 20,
  });
  const [behsazUserCode, setBehsazUserCode] = useState<number | undefined>();
  const [lastSubmittedValues, setLastSubmittedValues] = useState<BehsazSearchFormValues | null>(null);
  const [searchError, setSearchError] = useState<string | null>(null);
  const [pageInput, setPageInput] = useState('1');
  const totalPages = Math.max(1, Math.ceil(rowCount / paginationModel.pageSize));

  useEffect(() => {
    if (!open || !isEnabled) {
      return;
    }

    let isActive = true;
    void archiveApi.getSystemSettings().then((settings) => {
      if (!isActive) {
        return;
      }
      setBehsazUserCode(settings.behsazIntegration?.userCode || undefined);
    });

    return () => {
      isActive = false;
    };
  }, [isEnabled, open]);

  const runSearch = useCallback(async (values: BehsazSearchFormValues, pageModel: GridPaginationModel) => {
    setIsLoading(true);
    setSearchError(null);
    try {
      const payload = buildRequest(values, pageModel, behsazUserCode);
      const result = await archiveApi.searchBehsazModal(payload);
      setRows(result.items);
      setRowCount(result.totalCount);
    } catch (error) {
      const mappedError = mapApiError(error);
      setRows([]);
      setRowCount(0);
      setSearchError(
        mappedError.status === 403
          ? 'شما مجوز جستجو در سامانه بهساز را ندارید. دسترسی «بهساز / جستجو» باید برای نقش شما فعال شود.'
          : mappedError.message,
      );
    } finally {
      setIsLoading(false);
    }
  }, [behsazUserCode]);

  useEffect(() => {
    if (!open || !isEnabled || !lastSubmittedValues) {
      return;
    }
    void runSearch(lastSubmittedValues, paginationModel);
  }, [isEnabled, lastSubmittedValues, open, paginationModel, runSearch]);

  useEffect(() => {
    setPageInput(String(paginationModel.page + 1));
  }, [paginationModel.page]);

  const goToPage = (requestedPage: number) => {
    const targetPage = Math.min(Math.max(requestedPage, 1), totalPages);
    setPageInput(String(targetPage));
    setPaginationModel((current) =>
      current.page === targetPage - 1
        ? current
        : { ...current, page: targetPage - 1 },
    );
  };

  const submitPageInput = () => {
    goToPage(
      resolveBehsazPageNumber(
        pageInput,
        totalPages,
        paginationModel.page + 1,
      ),
    );
  };

  const onSearchSubmit = form.handleSubmit(async (values) => {
    const snapshot = values as BehsazSearchFormValues;
    setLastSubmittedValues(snapshot);
    setPaginationModel((current) => ({ ...current, page: 0 }));
    await runSearch(snapshot, { page: 0, pageSize: paginationModel.pageSize });
  });

  const columns = useMemo<GridColDef<BehsazSearchRow>[]>(() => [
    {
      field: 'action',
      headerName: 'عملیات',
      sortable: false,
      align: 'center',
      headerAlign: 'center',
      filterable: false,
      width: 110,
      renderCell: (params) => (
        <Button
          variant="contained"
          size="small"
          onClick={() => {
            onRecordSelect(params.row);
            onClose();
          }}
        >
          انتخاب
        </Button>
      ),
    },
    { field: 'zoningCode', headerName: 'کد نوسازی', minWidth: 140, flex: 1, align: 'center',headerAlign: 'center' },
    { field: 'systemNumber', headerName: 'ش سیستمی پرونده', minWidth: 160, flex: 1, align: 'center',headerAlign: 'center' },
    { field: 'fileNumber', headerName: 'شماره پرونده', minWidth: 150, flex: 1, align: 'center',headerAlign: 'center' },
    { field: 'archiveType', headerName: 'بایگانی', minWidth: 120, flex: 0.9, align: 'center',headerAlign: 'center' },
    { field: 'status', headerName: 'وضعیت تعیین', minWidth: 130, flex: 0.9 , align: 'center',headerAlign: 'center'},
    { field: 'taxpayerGroup', headerName: 'گروه مودی', minWidth: 130, flex: 1 , align: 'center',headerAlign: 'center'},
    {
      field: 'taxpayerDetails',
      headerName: 'مشخصات کامل مودی',
      minWidth: 320,
      align: 'center',headerAlign: 'center',
      flex: 2,
    },
    { field: 'user', headerName: 'کاربر', minWidth: 120, flex: 0.9,align: 'center',headerAlign: 'center' },
    { field: 'registrationPlate', headerName: 'پلاک ثبتی', minWidth: 140, flex: 1 ,align: 'center',headerAlign: 'center'},
    { field: 'address', headerName: 'آدرس', minWidth: 220, flex: 1.8,align: 'center',headerAlign: 'center' },
    { field: 'phone', headerName: 'تلفن', minWidth: 140, flex: 1,align: 'center',headerAlign: 'center' },
  ], [onClose, onRecordSelect]);

  return (
    <Dialog
      open={open}
      onClose={onClose}
      maxWidth={false}
      fullWidth
      PaperProps={{
        sx: {
          width: '96vw',
          maxWidth: '1800px',
          height: '92vh',
        },
      }}
    >
      <DialogTitle>جستجو در بهساز</DialogTitle>
      <DialogContent dividers sx={{ py: 1.5 }}>
        <Grid container spacing={2}>
          {searchError ? (
            <Grid item xs={12}>
              <Alert severity="error" onClose={() => setSearchError(null)}>
                {searchError}
              </Alert>
            </Grid>
          ) : null}
          <Grid item xs={12}>
            <Stack spacing={1}>
              <Typography variant="subtitle2" fontWeight={700}>اجزای کد نوسازی</Typography>
              <Box
                sx={{
                  display: 'grid',
                  gridTemplateColumns: {
                    xs: '1fr',
                    sm: 'repeat(2, minmax(0, 1fr))',
                    md: 'repeat(4, minmax(0, 1fr))',
                    xl: 'repeat(8, minmax(0, 1fr))',
                  },
                  gap: 1,
                }}
              >
                <Controller
                  name="instaurationFilterType"
                  control={form.control}
                  render={({ field }) => (
                    <TextField {...field} size="small" label="نوع فیلتر کد نوسازی" inputMode="numeric" fullWidth />
                  )}
                />
                <Controller
                  name="instaurationArea"
                  control={form.control}
                  render={({ field }) => <TextField {...field} size="small" label="کد نوسازی منطقه" inputMode="numeric" fullWidth />}
                />
                <Controller
                  name="instaurationZone"
                  control={form.control}
                  render={({ field }) => <TextField {...field} size="small" label="کد نوسازی حوزه" inputMode="numeric" fullWidth />}
                />
                <Controller
                  name="instaurationBlock"
                  control={form.control}
                  render={({ field }) => <TextField {...field} size="small" label="کد نوسازی بلوک" inputMode="numeric" fullWidth />}
                />
                <Controller
                  name="instaurationLand"
                  control={form.control}
                  render={({ field }) => <TextField {...field} size="small" label="کد نوسازی ملک" inputMode="numeric" fullWidth />}
                />
                <Controller
                  name="instaurationBuilding"
                  control={form.control}
                  render={({ field }) => <TextField {...field} size="small" label="کد نوسازی ساختمان" inputMode="numeric" fullWidth />}
                />
                <Controller
                  name="instaurationApartment"
                  control={form.control}
                  render={({ field }) => <TextField {...field} size="small" label="کد نوسازی آپارتمان" inputMode="numeric" fullWidth />}
                />
                <Controller
                  name="instaurationBusiness"
                  control={form.control}
                  render={({ field }) => <TextField {...field} size="small" label="کد نوسازی صنف" inputMode="numeric" fullWidth />}
                />
              </Box>
            </Stack>
          </Grid>

          <Grid item xs={12} md={3}>
            <Stack spacing={1.25}>
              <Button
                variant="contained"
                fullWidth
                disabled={!isEnabled || isLoading}
                onClick={() => {
                  void onSearchSubmit();
                }}
              >
                جستجو
              </Button>

              <Controller
                name="systemNumber"
                control={form.control}
                render={({ field }) => <TextField {...field} size="small" label="ش سیستمی پرونده" fullWidth />}
              />

              <Controller
                name="fileNumber"
                control={form.control}
                render={({ field }) => <TextField {...field} size="small" label="شماره پرونده" fullWidth />}
              />

              <Controller
                name="ghateCode"
                control={form.control}
                render={({ field }) => <TextField {...field} size="small" label="پلاک قطعه" inputMode="decimal" fullWidth />}
              />

              <Stack direction="row" spacing={1}>
                <Controller
                  name="mainPlate"
                  control={form.control}
                  render={({ field }) => <TextField {...field} size="small" label="پلاک اصلی" fullWidth />}
                />
                <Controller
                  name="subPlate"
                  control={form.control}
                  render={({ field }) => <TextField {...field} size="small" label="پلاک فرعی" fullWidth />}
                />
              </Stack>

              <Controller
                name="address"
                control={form.control}
                render={({ field }) => <TextField {...field} size="small" label="آدرس" fullWidth />}
              />

              <Controller
                name="gisIndicated"
                control={form.control}
                render={({ field }) => (
                  <FormControl size="small" fullWidth>
                    <InputLabel id="property-status-label">وضعیت تعیین ملک</InputLabel>
                    <Select {...field} labelId="property-status-label" label="وضعیت تعیین ملک">
                      <MenuItem value="">همه</MenuItem>
                      <MenuItem value="assigned">دارای تعیین</MenuItem>
                      <MenuItem value="unassigned">فاقد تعیین</MenuItem>
                    </Select>
                  </FormControl>
                )}
              />

              <Controller
                name="taxpayerGroup"
                control={form.control}
                render={({ field }) => (
                  <FormControl size="small" fullWidth>
                    <InputLabel id="taxpayer-group-label">گروه مودی</InputLabel>
                    <Select
                      {...field}
                      labelId="taxpayer-group-label"
                      label="گروه مودی"
                      onChange={(event) => {
                        const value = event.target.value;
                        field.onChange(value === 1 || value === 2 ? value : '');
                      }}
                    >
                      <MenuItem value="">همه موارد</MenuItem>
                      <MenuItem value={1}>حقیقی</MenuItem>
                      <MenuItem value={2}>حقوقی</MenuItem>
                    </Select>
                  </FormControl>
                )}
              />

              {taxpayerGroup !== 2 ? (
                <>
                  <Controller
                    name="firstName"
                    control={form.control}
                    render={({ field }) => <TextField {...field} size="small" label="نام" fullWidth />}
                  />
                  <Controller
                    name="lastName"
                    control={form.control}
                    render={({ field }) => <TextField {...field} size="small" label="نام خانوادگی" fullWidth />}
                  />
                  <Controller
                    name="nationalCode"
                    control={form.control}
                    render={({ field }) => <TextField {...field} size="small" label="کد ملی" fullWidth />}
                  />
                </>
              ) : null}
              {taxpayerGroup !== 1 ? (
                <>
                  <Controller
                    name="companyName"
                    control={form.control}
                    render={({ field }) => <TextField {...field} size="small" label="نام شرکت" fullWidth />}
                  />
                  <Controller
                    name="nationalCompanyId"
                    control={form.control}
                    render={({ field }) => (
                      <TextField
                        {...field}
                        size="small"
                        label="شناسه ملی / کد اقتصادی شرکت"
                        fullWidth
                      />
                    )}
                  />
                </>
              ) : null}
            </Stack>
          </Grid>

          <Grid item xs={12} md={9}>
            <Box dir="rtl" sx={{ height: '78vh', minHeight: 700 }}>
              <Stack spacing={1} sx={{ height: '100%' }}>
                <Stack
                  direction={{ xs: 'column', sm: 'row' }}
                  spacing={1}
                  alignItems={{ xs: 'stretch', sm: 'center' }}
                  justifyContent="flex-end"
                >
                  <Typography variant="body2" color="text.secondary">
                    صفحه {paginationModel.page + 1} از {totalPages}
                  </Typography>
                  <Button
                    size="small"
                    variant="outlined"
                    disabled={isLoading || !lastSubmittedValues || paginationModel.page === 0}
                    onClick={() => goToPage(1)}
                  >
                    اولین صفحه
                  </Button>
                  <TextField
                    size="small"
                    label="شماره صفحه"
                    value={pageInput}
                    onChange={(event) => setPageInput(event.target.value)}
                    onKeyDown={(event) => {
                      if (event.key === 'Enter') {
                        event.preventDefault();
                        submitPageInput();
                      }
                    }}
                    disabled={isLoading || !lastSubmittedValues}
                    slotProps={{
                      htmlInput: {
                        inputMode: 'numeric',
                        'aria-label': 'شماره صفحه مقصد',
                      },
                    }}
                    sx={{ width: { xs: '100%', sm: 120 } }}
                  />
                  <Button
                    size="small"
                    variant="contained"
                    disabled={isLoading || !lastSubmittedValues}
                    onClick={submitPageInput}
                  >
                    رفتن
                  </Button>
                  <Button
                    size="small"
                    variant="outlined"
                    disabled={
                      isLoading ||
                      !lastSubmittedValues ||
                      paginationModel.page >= totalPages - 1
                    }
                    onClick={() => goToPage(totalPages)}
                  >
                    آخرین صفحه
                  </Button>
                </Stack>
                <Box sx={{ flex: 1, minHeight: 0 }}>
                  <DataGrid
                    rows={rows}
                    columns={columns}
                    getRowId={(row) =>
                      [
                        row.systemNumber,
                        row.fileNumber,
                        row.registrationPlate,
                        row.zoningCode,
                        row.taxpayerDetails,
                      ].filter(Boolean).join('-')
                    }
                    loading={isLoading}
                    paginationMode="server"
                    rowCount={rowCount}
                    paginationModel={paginationModel}
                    onPaginationModelChange={setPaginationModel}
                    pageSizeOptions={[10, 20, 50]}
                    disableRowSelectionOnClick
                  />
                </Box>
              </Stack>
            </Box>
          </Grid>
        </Grid>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>بستن</Button>
      </DialogActions>
    </Dialog>
  );
};
