// src/stores/useUIStore.ts
import { create } from 'zustand';

interface UIState {
  darkMode: boolean;
  sidebarCollapsed: boolean;
  uploadPanelOpen: boolean;
  selectedFile: string | null;
  notifications: Array<{
    id: string;
    message: string;
    type: 'success' | 'error' | 'info';
    timestamp: number;
  }>;

  toggleDarkMode: () => void;
  toggleSidebar: () => void;
  openUploadPanel: () => void;
  closeUploadPanel: () => void;
  setSelectedFile: (fileId: string | null) => void;
  addNotification: (message: string, type: 'success' | 'error' | 'info') => void;
  removeNotification: (id: string) => void;
}

export const useUIStore = create<UIState>((set, get) => ({
  darkMode: typeof window !== 'undefined' ? localStorage.getItem('darkMode') === 'true' : false,
  sidebarCollapsed: typeof window !== 'undefined' ? window.innerWidth < 1024 : false,
  uploadPanelOpen: false,
  selectedFile: null,
  notifications: [],

  toggleDarkMode: () => {
    const newMode = !get().darkMode;
    localStorage.setItem('darkMode', String(newMode));
    set({ darkMode: newMode });
  },

  toggleSidebar: () => {
    set(state => ({ sidebarCollapsed: !state.sidebarCollapsed }));
  },

  openUploadPanel: () => set({ uploadPanelOpen: true }),
  closeUploadPanel: () => set({ uploadPanelOpen: false }),

  setSelectedFile: (fileId) => set({ selectedFile: fileId }),

  addNotification: (message, type) => {
    const id = Date.now().toString();
    set(state => ({
      notifications: [
        ...state.notifications,
        { id, message, type, timestamp: Date.now() }
      ]
    }));

    // Auto-remove notification after 5 seconds
    setTimeout(() => {
      set(state => ({
        notifications: state.notifications.filter(n => n.id !== id)
      }));
    }, 5000);
  },

  removeNotification: (id) => {
    set(state => ({
      notifications: state.notifications.filter(n => n.id !== id)
    }));
  }
}));
