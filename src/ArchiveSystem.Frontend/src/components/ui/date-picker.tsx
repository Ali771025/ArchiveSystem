'use client';

import * as React from 'react';
import { Calendar as CalendarIcon } from 'lucide-react';
import { cn } from '@/utils/helpers';

import { Button } from '@/components/common/Button';
import { Calendar } from './calendar';
import { Popover, PopoverContent, PopoverTrigger } from './popover';

interface DatePickerProps {
  className?: string;
  selected?: Date | null;
  onChange?: (date: Date | null) => void;
  placeholder?: string;
  disabled?: boolean;
}

const DatePicker = React.forwardRef<HTMLInputElement, DatePickerProps>(
  ({ selected, onChange, className, placeholder = 'Pick a date', disabled = false }, ref) => {
    const [isOpen, setIsOpen] = React.useState(false);

    return (
      <div className="relative">
        <Popover open={isOpen} onOpenChange={setIsOpen}>
          <PopoverTrigger>
            <Button
              type="button"
              variant={'outline'}
              disabled={disabled}
              className={cn(
                'w-full justify-start text-right font-normal',
                !selected && 'text-muted-foreground',
                className
              )}
            >
              <CalendarIcon className="ml-2 h-4 w-4" />
              {selected ? selected.toLocaleDateString('fa-IR') : <span>{placeholder}</span>}
            </Button>
          </PopoverTrigger>
          <PopoverContent className="w-auto p-0">
            <Calendar
              selected={selected || undefined}
              className={disabled ? 'pointer-events-none opacity-60' : undefined}
              onSelect={(date) => {
                onChange?.(date || null);
                setIsOpen(false);
              }}
            />
          </PopoverContent>
        </Popover>
      </div>
    );
  }
);
DatePicker.displayName = 'DatePicker';

export { DatePicker };
