import { create } from 'zustand';
import type { TaxpayerFilterState } from '@/features/taxpayer-management/types';

interface TaxpayerManagementUiState {
  isModalOpen: boolean;
  selectedTaxpayerId: string | null;
  filters: TaxpayerFilterState;
  pageNumber: number;
  pageSize: number;
  openCreateModal: () => void;
  openEditModal: (taxpayerId: string) => void;
  closeModal: () => void;
  setFilter: <K extends keyof TaxpayerFilterState>(key: K, value: TaxpayerFilterState[K]) => void;
  resetFilters: () => void;
  setPagination: (pageNumber: number, pageSize: number) => void;
}

const defaultFilters: TaxpayerFilterState = {
  firstName: '',
  lastName: '',
  nationalId: '',
};

export const useTaxpayerManagementStore = create<TaxpayerManagementUiState>((set) => ({
  isModalOpen: false,
  selectedTaxpayerId: null,
  filters: defaultFilters,
  pageNumber: 1,
  pageSize: 10,
  openCreateModal: () =>
    set({
      isModalOpen: true,
      selectedTaxpayerId: null,
    }),
  openEditModal: (taxpayerId) =>
    set({
      isModalOpen: true,
      selectedTaxpayerId: taxpayerId,
    }),
  closeModal: () =>
    set({
      isModalOpen: false,
      selectedTaxpayerId: null,
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
