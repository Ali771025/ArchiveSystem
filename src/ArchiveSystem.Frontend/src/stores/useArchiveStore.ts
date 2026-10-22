import { create } from 'zustand';
import type { GlobalFilters, UploadTask } from '@/types/archive';

interface ArchiveState {
  selectedClassificationId: number | null;
  selectedCaseId: number | null;
  filters: GlobalFilters;
  openUploadDialog: boolean;
  uploads: UploadTask[];
  setSelectedClassificationId: (id: number | null) => void;
  setSelectedCaseId: (id: number | null) => void;
  setFilters: (updater: (current: GlobalFilters) => GlobalFilters) => void;
  toggleUploadDialog: (open: boolean) => void;
  upsertUploadTask: (task: UploadTask) => void;
  removeUploadTask: (id: string) => void;
  clearUploadTasks: (caseId: number) => void;
}

const defaultFilters: GlobalFilters = {
  globalSearch: '',
  ownerName: '',
  fromDate: '',
  toDate: '',
  tags: [],
};

export const useArchiveStore = create<ArchiveState>((set) => ({
  selectedClassificationId: null,
  selectedCaseId: null,
  filters: defaultFilters,
  openUploadDialog: false,
  uploads: [],
  setSelectedClassificationId: (id) =>
    set(() => ({
      selectedClassificationId: id,
      selectedCaseId: null,
    })),
  setSelectedCaseId: (id) => set(() => ({ selectedCaseId: id })),
  setFilters: (updater) => set((state) => ({ filters: updater(state.filters) })),
  toggleUploadDialog: (open) => set(() => ({ openUploadDialog: open })),
  upsertUploadTask: (task) =>
    set((state) => {
      const existingIndex = state.uploads.findIndex((item) => item.id === task.id);
      if (existingIndex === -1) {
        return { uploads: [...state.uploads, task] };
      }

      const uploads = [...state.uploads];
      uploads[existingIndex] = task;
      return { uploads };
    }),
  removeUploadTask: (id) =>
    set((state) => ({ uploads: state.uploads.filter((task) => task.id !== id) })),
  clearUploadTasks: (caseId) =>
    set((state) =>
      state.uploads.some((task) => task.caseId === caseId)
        ? { uploads: state.uploads.filter((task) => task.caseId !== caseId) }
        : state,
    ),
}));

