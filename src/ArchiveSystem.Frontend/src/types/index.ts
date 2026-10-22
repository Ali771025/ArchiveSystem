// src/types/index.ts
export interface User {
  id: string;
  username: string;
  email: string;
  role: string;
  permissions: string[];
}

export interface FileMetadata {
  id: string;
  name: string;
  size: number;
  type: string;
  owner: string;
  createdAt: string;
  updatedAt: string;
  tags: string[];
  description?: string;
  path: string;
  parentId?: string;
  version: number;
  checksum: string;
  meta: Record<string, any>;
  metadata?: Record<string, any>;
}

export interface Folder {
  id: string;
  name: string;
  parentId: string | null;
  createdAt: string;
  updatedAt: string;
  children: (Folder | FileMetadata)[];
}

export interface TreeNode extends Folder {
  children: TreeNode[];
  isExpanded?: boolean;
  level: number;
}

export interface SearchResult {
  id: string;
  title: string;
  snippet: string;
  type: string;
  size: number;
  date: string;
  path: string;
  score: number;
}

export interface UploadProgress {
  id: string;
  fileName: string;
  progress: number;
  status: 'uploading' | 'paused' | 'completed' | 'error';
  size: number;
  uploadedBytes: number;
}

export interface FileListItem {
  id: number;
  name: string;
  size: number;
  extension: string;
  contentType?: string;
  storagePath: string;
  createdAt: string;
  modifiedAt?: string;
  caseId?: number;
}

export interface SearchFilesResponse {
  items: FileListItem[];
  totalCount?: number;
}

export interface UploadFileResponse {
  id?: number;
  fileName?: string;
  storagePath?: string;
  [key: string]: unknown;
}

