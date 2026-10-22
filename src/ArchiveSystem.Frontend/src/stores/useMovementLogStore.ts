import { create } from 'zustand';

interface MovementLogStoreState {
  isOpen: boolean;
  isLoading: boolean;
  error: string | null;
  openModal: () => void;
  closeModal: () => void;
  setLoading: (status: boolean) => void;
  setError: (message: string | null) => void;
}

export const useMovementLogStore = create<MovementLogStoreState>((set) => ({
  isOpen: false,
  isLoading: false,
  error: null,
  openModal: () => set({ isOpen: true }),
  closeModal: () => set({ isOpen: false, isLoading: false, error: null }),
  setLoading: (status) => set({ isLoading: status }),
  setError: (message) => set({ error: message }),
}));
