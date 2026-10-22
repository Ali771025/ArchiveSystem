'use client';

import * as React from 'react';
import { cn } from '@/utils/helpers';

interface PopoverProps {
  children: React.ReactNode;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

interface PopoverTriggerProps extends React.HTMLAttributes<HTMLDivElement> {
  children: React.ReactNode;
}

interface PopoverContentProps extends React.HTMLAttributes<HTMLDivElement> {
  children: React.ReactNode;
}

const PopoverContext = React.createContext<{
  open: boolean;
  setOpen: (open: boolean) => void;
}>({
  open: false,
  setOpen: () => {},
});

const Popover = ({ children, open, onOpenChange }: PopoverProps) => {
  return (
    <PopoverContext.Provider value={{ open, setOpen: onOpenChange }}>
      {children}
    </PopoverContext.Provider>
  );
};

const PopoverTrigger = ({ children, ...props }: PopoverTriggerProps) => {
  const { open, setOpen } = React.useContext(PopoverContext);

  return (
    <div
      onClick={() => setOpen(!open)}
      {...props}
    >
      {children}
    </div>
  );
};

const PopoverContent = ({ children, className, ...props }: PopoverContentProps) => {
  const { open } = React.useContext(PopoverContext);

  if (!open) return null;

  return (
    <div
      className={cn(
        'z-50 w-72 rounded-md border bg-popover p-4 text-popover-foreground shadow-md outline-none',
        className
      )}
      {...props}
    >
      {children}
    </div>
  );
};

export { Popover, PopoverTrigger, PopoverContent };
