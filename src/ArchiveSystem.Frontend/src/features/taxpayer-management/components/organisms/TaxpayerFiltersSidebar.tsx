import FilterListIcon from '@mui/icons-material/FilterList';
import RestartAltRoundedIcon from '@mui/icons-material/RestartAltRounded';
import { Button, Paper, Stack, TextField, Typography } from '@mui/material';
import type { TaxpayerFilterState } from '@/features/taxpayer-management/types';

interface TaxpayerFiltersSidebarProps {
  filters: TaxpayerFilterState;
  onFilterChange: <K extends keyof TaxpayerFilterState>(key: K, value: TaxpayerFilterState[K]) => void;
  onResetFilters: () => void;
}

export const TaxpayerFiltersSidebar = ({ filters, onFilterChange, onResetFilters }: TaxpayerFiltersSidebarProps) => {
  return (
    <Paper
      component="aside"
      aria-label="فیلتر مودیان"
      variant="outlined"
      sx={{
        width: '100%',
        p: 2,
        borderRadius: 2,
      }}
    >
      <Stack spacing={1.5}>
        <Stack direction="row" spacing={1} alignItems="center">
          <FilterListIcon fontSize="small" />
          <Typography variant="subtitle1" fontWeight={700}>
            فیلترها
          </Typography>
        </Stack>

        <Stack
          direction={{ xs: 'column', md: 'row' }}
          spacing={1.5}
          alignItems={{ xs: 'stretch', md: 'flex-start' }}
        >
          <TextField
            label="نام"
            value={filters.firstName}
            onChange={(event) => onFilterChange('firstName', event.target.value)}
            inputProps={{ 'aria-label': 'فیلتر نام' }}
            sx={{ flex: 1, minWidth: 0 }}
          />
          <TextField
            label="نام خانوادگی"
            value={filters.lastName}
            onChange={(event) => onFilterChange('lastName', event.target.value)}
            inputProps={{ 'aria-label': 'فیلتر نام خانوادگی' }}
            sx={{ flex: 1, minWidth: 0 }}
          />
          <TextField
            label="کد ملی"
            value={filters.nationalId}
            onChange={(event) => onFilterChange('nationalId', event.target.value)}
            inputProps={{ inputMode: 'numeric', dir: 'ltr', 'aria-label': 'فیلتر کد ملی' }}
            sx={{ flex: 1, minWidth: 0 }}
          />
          <Button
            onClick={onResetFilters}
            variant="outlined"
            startIcon={<RestartAltRoundedIcon />}
            aria-label="پاک کردن فیلترها"
            sx={{ minHeight: 56, px: 2.5, flexShrink: 0 }}
          >
            پاک کردن فیلترها
          </Button>
        </Stack>
      </Stack>
    </Paper>
  );
};
