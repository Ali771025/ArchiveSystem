import { create } from 'zustand';
import type { UserDTO, UserFilterState } from '@/features/user-management/types';

interface UserManagementUiState {
  isDialogOpen: boolean;
  selectedUser: UserDTO | null;
  deletingUserId: number | null;
  filters: UserFilterState;
  pageNumber: number;
  pageSize: number;
  sortBy: string;
  sortDescending: boolean;
  openCreateDialog: () => void;
  openEditDialog: (user: UserDTO) => void;
  closeDialog: () => void;
  setDeletingUserId: (userId: number | null) => void;
  setFilter: <K extends keyof UserFilterState>(key: K, value: UserFilterState[K]) => void;
  resetFilters: () => void;
  setPagination: (pageNumber: number, pageSize: number) => void;
  setSorting: (sortBy: string, sortDescending: boolean) => void;
}

const defaultFilters: UserFilterState = {
  username: '',
  firstName: '',
  lastName: '',
  nationalId: '',
};

export const useUserManagementStore = create<UserManagementUiState>((set) => ({
  isDialogOpen: false,
  selectedUser: null,
  deletingUserId: null,
  filters: defaultFilters,
  pageNumber: 1,
  pageSize: 10,
  sortBy: 'CreatedAt',
  sortDescending: true,
  openCreateDialog: () =>
    set({
      isDialogOpen: true,
      selectedUser: null,
    }),
  openEditDialog: (user) =>
    set({
      isDialogOpen: true,
      selectedUser: user,
    }),
  closeDialog: () =>
    set({
      isDialogOpen: false,
      selectedUser: null,
    }),
  setDeletingUserId: (userId) => set({ deletingUserId: userId }),
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
  setSorting: (sortBy, sortDescending) =>
    set({
      sortBy,
      sortDescending,
    }),
}));
