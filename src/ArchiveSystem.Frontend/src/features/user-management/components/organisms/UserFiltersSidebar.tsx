import FilterListIcon from '@mui/icons-material/FilterList';
import RestartAltRoundedIcon from '@mui/icons-material/RestartAltRounded';
import { Button, Paper, Stack, TextField, Typography } from '@mui/material';
import type { UserFilterState } from '@/features/user-management/types';

interface UserFiltersSidebarProps {
  filters: UserFilterState;
  onFilterChange: <K extends keyof UserFilterState>(key: K, value: UserFilterState[K]) => void;
  onResetFilters: () => void;
}

const rtlTextInputProps = {
  dir: 'rtl',
  style: { textAlign: 'right' },
} as const;

export const UserFiltersSidebar = ({ filters, onFilterChange, onResetFilters }: UserFiltersSidebarProps) => {
  return (
    <Paper
      component="aside"
      aria-label="فیلتر کاربران"
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
          sx={{
            display: 'grid',
            gridTemplateColumns: { xs: '1fr', md: 'repeat(5, minmax(0, 1fr))' },
            gap: 1.5,
          }}
        >
          <TextField
            label="نام کاربری"
            value={filters.username}
            onChange={(event) => onFilterChange('username', event.target.value)}
            inputProps={{ 'aria-label': 'فیلتر نام کاربری' }}
          />
          <TextField
            label="نام"
            value={filters.firstName}
            onChange={(event) => onFilterChange('firstName', event.target.value)}
            inputProps={{ ...rtlTextInputProps, 'aria-label': 'فیلتر نام' }}
          />
          <TextField
            label="نام خانوادگی"
            value={filters.lastName}
            onChange={(event) => onFilterChange('lastName', event.target.value)}
            inputProps={{ ...rtlTextInputProps, 'aria-label': 'فیلتر نام خانوادگی' }}
          />
          <TextField
            label="کد ملی"
            value={filters.nationalId}
            onChange={(event) => onFilterChange('nationalId', event.target.value)}
            inputProps={{ inputMode: 'numeric', dir: 'ltr', 'aria-label': 'فیلتر کد ملی' }}
          />
          <Button
            onClick={onResetFilters}
            variant="outlined"
            startIcon={<RestartAltRoundedIcon />}
            fullWidth
            aria-label="پاک کردن فیلترها"
            sx={{ minHeight: 56 }}
          >
            پاک کردن فیلترها
          </Button>
        </Stack>
      </Stack>
    </Paper>
  );
};
