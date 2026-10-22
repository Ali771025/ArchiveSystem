import DatePicker from 'react-multi-date-picker';
import type { Value } from 'react-multi-date-picker';
import DateObject from 'react-date-object';
import persian from 'react-date-object/calendars/persian';
import persian_fa from 'react-date-object/locales/persian_fa';
import { FormHelperText, Paper, Stack, Typography } from '@mui/material';
import { Controller, type Control, type FieldValues, type Path } from 'react-hook-form';

interface ControlledJalaliDatePickerProps<TFieldValues extends FieldValues> {
  control: Control<TFieldValues>;
  name: Path<TFieldValues>;
  label: string;
}

export const ControlledJalaliDatePicker = <TFieldValues extends FieldValues>({
  control,
  name,
  label,
}: ControlledJalaliDatePickerProps<TFieldValues>) => {
  const mapDateChange = (value: Value): DateObject | null => {
    if (value === null || value === undefined || value === '') {
      return null;
    }

    if (value instanceof DateObject) {
      return value;
    }

    if (Array.isArray(value)) {
      return value[0] instanceof DateObject ? value[0] : null;
    }

    return null;
  };

  return (
    <Controller
      control={control}
      name={name}
      render={({ field, fieldState }) => (
        <Stack spacing={0.75}>
          <Typography variant="subtitle2" fontWeight={700} color="text.primary">
            {label}
          </Typography>

          <Paper
            variant="outlined"
            sx={{
              px: 1,
              py: 0.5,
              borderRadius: 1.5,
              borderColor: fieldState.error ? 'error.main' : 'divider',
              '&:focus-within': {
                borderColor: fieldState.error ? 'error.main' : 'primary.main',
                boxShadow: fieldState.error
                  ? '0 0 0 3px rgba(211, 47, 47, 0.14)'
                  : '0 0 0 3px rgba(25, 118, 210, 0.14)',
              },
            }}
          >
            <DatePicker
              value={field.value as Value}
              onChange={(value) => field.onChange(mapDateChange(value))}
              calendar={persian}
              locale={persian_fa}
              format="YYYY/MM/DD"
              calendarPosition="bottom-right"
              inputClass="archive-jalali-date-input"
              containerClassName="archive-jalali-date-container"
              className="archive-jalali-calendar"
            />
          </Paper>

          {fieldState.error ? <FormHelperText error>{fieldState.error.message}</FormHelperText> : null}
        </Stack>
      )}
    />
  );
};
