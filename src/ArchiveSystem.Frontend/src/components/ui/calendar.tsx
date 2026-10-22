import * as React from 'react';
import { JalaliDateInput } from '@/components/common/JalaliDateInputs';
import { cn } from '@/utils/helpers';
import { parseDateLikeValue, toGregorianDateString } from '@/utils/jalaliDate';

interface CalendarProps {
  className?: string;
  selected?: Date;
  onSelect?: (date?: Date) => void;
}

const Calendar: React.FC<CalendarProps> = ({ className, selected, onSelect }) => {
  const value = selected ? toGregorianDateString(selected) : '';

  return (
    <div className={cn(className)}>
      <JalaliDateInput
        value={value}
        inputClassName="w-full rounded border p-2 text-sm"
        onChange={(dateValue) => {
          const parsed = parseDateLikeValue(dateValue);
          onSelect?.(parsed ?? undefined);
        }}
      />
    </div>
  );
};

export { Calendar };

