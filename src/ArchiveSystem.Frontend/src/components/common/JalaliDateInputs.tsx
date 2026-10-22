import DatePicker from 'react-multi-date-picker';
import type { Value } from 'react-multi-date-picker';
import DateObject from 'react-date-object';
import persian from 'react-date-object/calendars/persian';
import persian_fa from 'react-date-object/locales/persian_fa';
import TimePicker from 'react-multi-date-picker/plugins/time_picker';
import { Paper } from '@mui/material';
import { toDateObject, toGregorianDateString, toGregorianDateTimeString } from '@/utils/jalaliDate';

interface JalaliDateInputProps {
  value: string;
  onChange: (value: string) => void;
  disabled?: boolean;
  inputClassName?: string;
}

interface JalaliDateTimeInputProps {
  value: string;
  onChange: (value: string) => void;
  disabled?: boolean;
  inputClassName?: string;
}

const normalizePickerValue = (value: Value): DateObject | null => {
  if (value == null || value === '') {
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

const defaultInputClass = 'archive-jalali-date-input';
const pickerContainerClass = 'archive-jalali-date-container';
const pickerCalendarClass = 'archive-jalali-calendar';
const getInputClass = (inputClassName?: string) =>
  inputClassName ? `${defaultInputClass} ${inputClassName}` : defaultInputClass;

export const JalaliDateInput = ({ value, onChange, disabled = false, inputClassName }: JalaliDateInputProps) => {
  return (
    <Paper variant="outlined" sx={{ p: 0.5, borderRadius: 1.5 }}>
      <DatePicker
        value={toDateObject(value)}
        onChange={(next) => {
          const parsed = normalizePickerValue(next);
          onChange(parsed ? toGregorianDateString(parsed.toDate()) : '');
        }}
        calendar={persian}
        locale={persian_fa}
        format="YYYY/MM/DD"
        calendarPosition="bottom-right"
        disabled={disabled}
        inputClass={getInputClass(inputClassName)}
        containerClassName={pickerContainerClass}
        className={pickerCalendarClass}
      />
    </Paper>
  );
};

export const JalaliDateTimeInput = ({
  value,
  onChange,
  disabled = false,
  inputClassName,
}: JalaliDateTimeInputProps) => {
  return (
    <Paper variant="outlined" sx={{ p: 0.5, borderRadius: 1.5 }}>
      <DatePicker
        value={toDateObject(value)}
        onChange={(next) => {
          const parsed = normalizePickerValue(next);
          onChange(parsed ? toGregorianDateTimeString(parsed.toDate()) : '');
        }}
        calendar={persian}
        locale={persian_fa}
        format="YYYY/MM/DD HH:mm"
        plugins={[<TimePicker key="time-picker" position="bottom" hideSeconds />]}
        calendarPosition="bottom-right"
        disabled={disabled}
        inputClass={getInputClass(inputClassName)}
        containerClassName={pickerContainerClass}
        className={pickerCalendarClass}
      />
    </Paper>
  );
};
