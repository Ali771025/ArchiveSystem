import FilterListIcon from '@mui/icons-material/FilterList';
import RestartAltRoundedIcon from '@mui/icons-material/RestartAltRounded';
import { Button, Paper, Stack, TextField, Typography } from '@mui/material';
import type { CaseLoanFilterState } from '@/features/case-loan-management/types';

interface CaseLoanFiltersSidebarProps {
  filters: CaseLoanFilterState;
  onFilterChange: <K extends keyof CaseLoanFilterState>(key: K, value: CaseLoanFilterState[K]) => void;
  onResetFilters: () => void;
}

export const CaseLoanFiltersSidebar = ({ filters, onFilterChange, onResetFilters }: CaseLoanFiltersSidebarProps) => {
  return (
    <Paper
      component="aside"
      aria-label="فیلتر امانات پرونده فیزیکی"
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
            label="شماره پرونده یا شماره مرجع"
            value={filters.caseNumber}
            onChange={(event) => onFilterChange('caseNumber', event.target.value)}
            inputProps={{ 'aria-label': 'فیلتر شماره پرونده یا شماره مرجع' }}
            sx={{ flex: 1, minWidth: 0 }}
          />
          <TextField
            label="کد ملی"
            value={filters.nationalId}
            onChange={(event) => onFilterChange('nationalId', event.target.value)}
            inputProps={{ inputMode: 'numeric', dir: 'ltr', 'aria-label': 'فیلتر کد ملی' }}
            sx={{ flex: 1, minWidth: 0 }}
          />
          <TextField
            label="نام خانوادگی"
            value={filters.borrowerLastName}
            onChange={(event) => onFilterChange('borrowerLastName', event.target.value)}
            inputProps={{ 'aria-label': 'فیلتر نام خانوادگی' }}
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
