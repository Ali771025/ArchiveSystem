import { create } from 'zustand';
import type { CaseLoanFilterState } from '@/features/case-loan-management/types';

interface CaseLoanManagementUiState {
  isModalOpen: boolean;
  selectedCaseLoanId: string | null;
  filters: CaseLoanFilterState;
  pageNumber: number;
  pageSize: number;
  openCreateModal: () => void;
  openEditModal: (caseLoanId: string) => void;
  closeModal: () => void;
  setFilter: <K extends keyof CaseLoanFilterState>(key: K, value: CaseLoanFilterState[K]) => void;
  resetFilters: () => void;
  setPagination: (pageNumber: number, pageSize: number) => void;
}

const defaultFilters: CaseLoanFilterState = {
  caseNumber: '',
  nationalId: '',
  borrowerLastName: '',
};

export const useCaseLoanManagementStore = create<CaseLoanManagementUiState>((set) => ({
  isModalOpen: false,
  selectedCaseLoanId: null,
  filters: defaultFilters,
  pageNumber: 1,
  pageSize: 10,
  openCreateModal: () =>
    set({
      isModalOpen: true,
      selectedCaseLoanId: null,
    }),
  openEditModal: (caseLoanId) =>
    set({
      isModalOpen: true,
      selectedCaseLoanId: caseLoanId,
    }),
  closeModal: () =>
    set({
      isModalOpen: false,
      selectedCaseLoanId: null,
    }),
  setFilter: (key, value) =>
    set((state) => ({
      filters: {
        ...state.filters,
        [key]: value,
      },
      pageNumber: 1,
    })),
  resetFilters: () =>
    set({
      filters: defaultFilters,
      pageNumber: 1,
    }),
  setPagination: (pageNumber, pageSize) =>
    set({
      pageNumber,
      pageSize,
    }),
}));
