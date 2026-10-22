import { createContext, type ReactNode, useCallback, useContext, useEffect, useMemo, useRef, useState } from 'react';
import { ConfirmDialog } from '@/components/common/ConfirmDialog';

export interface ConfirmOptions {
  title: string;
  message: string;
  confirmText?: string;
  cancelText?: string;
  variant?: 'danger' | 'primary';
}

interface ConfirmRequest {
  isOpen: boolean;
  title: string;
  message: string;
  confirmText: string;
  cancelText: string;
  variant: 'danger' | 'primary';
  resolve: (value: boolean) => void;
}

interface ConfirmContextValue {
  confirm: (options: ConfirmOptions) => Promise<boolean>;
}

const ConfirmContext = createContext<ConfirmContextValue | null>(null);

export const ConfirmProvider = ({ children }: { children: ReactNode }) => {
  const queueRef = useRef<ConfirmRequest[]>([]);
  const currentRef = useRef<ConfirmRequest | null>(null);
  const [current, setCurrent] = useState<ConfirmRequest | null>(null);

  useEffect(() => {
    currentRef.current = current;
  }, [current]);

  const confirm = useCallback((options: ConfirmOptions) => {
    return new Promise<boolean>((resolve) => {
      const request: ConfirmRequest = {
        isOpen: true,
        title: options.title,
        message: options.message,
        confirmText: options.confirmText ?? 'تأیید',
        cancelText: options.cancelText ?? 'انصراف',
        variant: options.variant ?? 'primary',
        resolve,
      };

      setCurrent((previous) => {
        if (previous) {
          queueRef.current.push(request);
          return previous;
        }
        return request;
      });
    });
  }, []);

  const handleClose = useCallback((result: boolean) => {
    setCurrent((previous) => {
      if (!previous) {
        return null;
      }

      previous.resolve(result);
      return queueRef.current.shift() ?? null;
    });
  }, []);

  useEffect(() => {
    return () => {
      currentRef.current?.resolve(false);
      queueRef.current.forEach((request) => request.resolve(false));
      queueRef.current = [];
    };
  }, []);

  const value = useMemo<ConfirmContextValue>(() => ({ confirm }), [confirm]);

  return (
    <ConfirmContext.Provider value={value}>
      {children}
      <ConfirmDialog
        isOpen={Boolean(current?.isOpen)}
        title={current?.title ?? ''}
        message={current?.message ?? ''}
        confirmText={current?.confirmText ?? 'تأیید'}
        cancelText={current?.cancelText ?? 'انصراف'}
        variant={current?.variant ?? 'primary'}
        onConfirm={() => handleClose(true)}
        onCancel={() => handleClose(false)}
      />
    </ConfirmContext.Provider>
  );
};

export const useConfirmContext = () => {
  const context = useContext(ConfirmContext);
  if (!context) {
    throw new Error('useConfirmContext must be used within ConfirmProvider.');
  }
  return context;
};
