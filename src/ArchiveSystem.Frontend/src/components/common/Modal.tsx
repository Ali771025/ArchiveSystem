// src/components/common/Modal.tsx
import React from 'react';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';

interface ModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  title: string;
  children: React.ReactNode;
  size?: 'sm' | 'md' | 'lg' | 'xl';
}

const Modal = ({ open, onOpenChange, title, children, size = 'md' }: ModalProps) => {
  const sizeClasses = {
    sm: 'max-w-sm',
    md: 'max-w-md',
    lg: 'max-w-lg',
    xl: 'max-w-xl'
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className={`w-[calc(100vw-1.25rem)] sm:w-full ${sizeClasses[size]} p-0`}>
        <DialogHeader className="p-4 pb-0 sm:p-6 sm:pb-0">
          <DialogTitle>{title}</DialogTitle>
        </DialogHeader>
        <div className="p-4 pt-0 sm:p-6 sm:pt-0">
          {children}
        </div>
      </DialogContent>
    </Dialog>
  );
};

export default Modal;
