'use client';

import * as React from 'react';
import { cn } from '@/utils/helpers';
import { ChevronDown } from 'lucide-react';

interface SelectContextProps {
  value: string;
  onValueChange: (value: string) => void;
  disabled?: boolean;
}

const SelectContext = React.createContext<SelectContextProps>({
  value: '',
  onValueChange: () => {},
  disabled: false,
});

interface SelectProps {
  value?: string;
  onValueChange?: (value: string) => void;
  disabled?: boolean;
  children: React.ReactNode;
}

const Select = ({ value = '', onValueChange = () => {}, disabled = false, children }: SelectProps) => {
  const contextValue = React.useMemo(
    () => ({
      value,
      onValueChange,
      disabled,
    }),
    [value, onValueChange, disabled]
  );

  return <SelectContext.Provider value={contextValue}>{children}</SelectContext.Provider>;
};

interface SelectTriggerProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  children: React.ReactNode;
}

const SelectTrigger = React.forwardRef<HTMLButtonElement, SelectTriggerProps>(
  ({ className, children, ...props }, ref) => {
    const { value, disabled } = React.useContext(SelectContext);

    return (
      <button
        type="button"
        ref={ref}
        className={cn(
          'flex h-10 w-full items-center justify-between rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50',
          className
        )}
        disabled={disabled}
        {...props}
      >
        {value || <span className="opacity-50">Select an option</span>}
        <ChevronDown className="h-4 w-4 opacity-50" />
      </button>
    );
  }
);
SelectTrigger.displayName = 'SelectTrigger';

interface SelectValueProps {
  placeholder?: string;
}

const SelectValue = ({ placeholder }: SelectValueProps) => {
  const { value } = React.useContext(SelectContext);
  
  return <>{value || placeholder}</>;
};

interface SelectContentProps {
  children: React.ReactNode;
}

const SelectContent = ({ children }: SelectContentProps) => {
  // In a real implementation, this would be a dropdown menu
  // For now, we'll just return the children directly
  return <div className="mt-1 bg-white border rounded-md shadow-lg z-50">{children}</div>;
};

interface SelectItemProps extends React.LiHTMLAttributes<HTMLLIElement> {
  value: string;
  children: React.ReactNode;
}

const SelectItem = React.forwardRef<HTMLLIElement, SelectItemProps>(
  ({ value, children, ...props }, ref) => {
    const { value: currentValue, onValueChange } = React.useContext(SelectContext);

    const handleClick = () => {
      onValueChange(value);
    };

    return (
      <li
        ref={ref}
        className={cn(
          'relative flex cursor-default select-none items-center rounded-sm px-2 py-1.5 text-sm outline-none transition-colors hover:bg-accent hover:text-accent-foreground focus:bg-accent focus:text-accent-foreground',
          currentValue === value ? 'bg-accent font-medium' : '',
          props.className
        )}
        onClick={handleClick}
        {...props}
      >
        {children}
      </li>
    );
  }
);
SelectItem.displayName = 'SelectItem';

export { Select, SelectContent, SelectItem, SelectTrigger, SelectValue };
