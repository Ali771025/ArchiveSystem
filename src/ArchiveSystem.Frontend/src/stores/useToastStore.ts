import { create } from 'zustand';

export type ToastSeverity = 'success' | 'error' | 'info' | 'warning';

export interface ToastItem {
  id: string;
  message: string;
  severity: ToastSeverity;
}

interface ToastState {
  queue: ToastItem[];
  push: (message: string, severity: ToastSeverity) => void;
  shift: () => void;
}

const createToastId = (): string => {
  if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
    return crypto.randomUUID();
  }

  return `${Date.now()}-${Math.random().toString(16).slice(2)}`;
};

export const useToastStore = create<ToastState>((set) => ({
  queue: [],
  push: (message, severity) =>
    set((state) => ({
      queue: [...state.queue, { id: createToastId(), message, severity }],
    })),
  shift: () =>
    set((state) => ({
      queue: state.queue.slice(1),
    })),
}));

