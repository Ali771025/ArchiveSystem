// src/stores/useFileStore.ts (Updated)
import { create } from 'zustand';
import { FileMetadata, Folder, TreeNode, SearchResult } from '../types';
import { fileApi } from '../services/fileApi';
import { mockData } from '../services/mockData';

interface FileState {
  files: FileMetadata[];
  folders: Folder[];
  treeData: TreeNode[];
  currentPath: string[];
  selectedFiles: string[];
  searchResults: SearchResult[];
  loading: boolean;
  error: string | null;

  // Navigation
  setCurrentPath: (path: string[]) => void;
  navigateToFolder: (folderId: string) => void;
  goBack: () => void;

  // File operations
  loadFiles: (folderId?: string) => Promise<void>;
  selectFile: (fileId: string) => void;
  selectAllFiles: () => void;
  clearSelection: () => void;

  // Search
  searchFiles: (query: string, filters?: any) => Promise<void>;

  // Upload
  uploadFile: (file: File, onProgress?: (progress: number) => void) => Promise<void>;

  // Download
  downloadFile: (fileId: string) => Promise<string>;

  // Delete
  deleteFile: (fileId: string) => Promise<void>;

  // Bootstrap
  initializeMockData: () => void;
}

export const useFileStore = create<FileState>((set, get) => ({
  files: [],
  folders: [],
  treeData: [],
  currentPath: [],
  selectedFiles: [],
  searchResults: [],
  loading: false,
  error: null,

  initializeMockData: () => {
    set({
      files: mockData.files,
      folders: mockData.folders,
      treeData: mockData.treeData
    });
  },

  setCurrentPath: (path) => set({ currentPath: path }),

  navigateToFolder: (folderId) => {
    const { folders } = get();
    const folder = folders.find(f => f.id === folderId);
    if (folder) {
      set(state => ({
        currentPath: [...state.currentPath, folderId]
      }));
    }
  },

  goBack: () => {
    set(state => ({
      currentPath: state.currentPath.slice(0, -1)
    }));
  },

  loadFiles: async (folderId) => {
    set({ loading: true, error: null });
    try {
      const response = await fileApi.getFiles({ page: 1, pageSize: 100 });
      set({
        files: response.items.map(item => ({
          id: item.id.toString(),
          name: item.name,
          size: item.size,
          type: item.extension,
          owner: 'system', // Will be populated from user info
          createdAt: item.createdAt,
          updatedAt: item.modifiedAt || item.createdAt,
          tags: [],
          path: '',
          parentId: item.caseId?.toString() || undefined,
          version: 1,
          checksum: '',
          meta: {}
        })),
        loading: false
      });
    } catch (error) {
      set({ loading: false, error: 'Failed to load files' });
    }
  },

  selectFile: (fileId) => {
    set(state => ({
      selectedFiles: state.selectedFiles.includes(fileId)
        ? state.selectedFiles.filter(id => id !== fileId)
        : [...state.selectedFiles, fileId]
    }));
  },

  selectAllFiles: () => {
    const { files } = get();
    set({ selectedFiles: files.map(f => f.id) });
  },

  clearSelection: () => {
    set({ selectedFiles: [] });
  },

  searchFiles: async (query, filters) => {
    set({ loading: true, error: null });
    try {
      const response = await fileApi.searchFiles(query, filters);
      set({
        searchResults: response.items.map(item => ({
          id: item.id.toString(),
          title: item.name,
          snippet: '', // Will be populated from search highlights
          type: item.extension,
          size: item.size,
          date: item.createdAt,
          path: item.storagePath,
          score: 1.0
        })),
        loading: false
      });
    } catch (error) {
      set({ loading: false, error: 'Search failed' });
    }
  },

  uploadFile: async (file, onProgress) => {
    try {
      await fileApi.uploadFile(file, onProgress);
      // Reload files after upload
      await get().loadFiles();
    } catch (error) {
      console.error('Upload failed:', error);
      set({ error: 'Upload failed' });
      throw error;
    }
  },

  downloadFile: async (fileId) => {
    try {
      const response = await fileApi.getDownloadUrl(fileId);
      return response.url;
    } catch (error) {
      console.error('Download failed:', error);
      throw error;
    }
  },

  deleteFile: async (fileId) => {
    try {
      await fileApi.deleteFile(fileId);
      set((state) => ({
        files: state.files.filter((file) => file.id !== fileId),
        selectedFiles: state.selectedFiles.filter((id) => id !== fileId),
      }));
    } catch (error) {
      console.error('Delete failed:', error);
      set({ error: 'Delete failed' });
      throw error;
    }
  }
}));

