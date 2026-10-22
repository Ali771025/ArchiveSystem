// src/components/features/FileList.tsx
import React, { MouseEvent, useEffect, useState } from 'react';
import FixedSizeList from '../ui/FixedSizeList';
import { FileMetadata } from '../../types';
import { useFileStore } from '../../stores/useFileStore';
import { useUIStore } from '../../stores/useUIStore';
import { useAuthStore } from '../../stores/useAuthStore';
import {
  Download,
  Edit,
  Trash2,
  Folder,
  FileText,
  Image,
  File
} from 'lucide-react';
import { buildDeleteConfirmOptions } from '@/components/common/confirmPresets';
import { useConfirm } from '@/components/common/useConfirm';
import { useToastStore } from '@/stores/useToastStore';

interface FileListProps {
  files: FileMetadata[];
  onFileClick?: (file: FileMetadata) => void;
}

const FileList = ({ files, onFileClick }: FileListProps) => {
  const { selectedFiles, selectFile, downloadFile, deleteFile } = useFileStore();
  const { setSelectedFile } = useUIStore();
  const user = useAuthStore((state) => state.user);
  const confirm = useConfirm();
  const pushToast = useToastStore((state) => state.push);
  const [listHeight, setListHeight] = useState(560);
  const canDownload = Boolean(user?.permissions?.includes('read') || user?.permissions?.includes('manage'));
  const canEdit = Boolean(user?.permissions?.includes('write') || user?.permissions?.includes('manage'));
  const canDelete = Boolean(user?.permissions?.includes('delete') || user?.permissions?.includes('manage'));

  useEffect(() => {
    const updateHeight = () => {
      const next = Math.max(320, window.innerHeight - 260);
      setListHeight(next);
    };
    updateHeight();
    window.addEventListener('resize', updateHeight);
    return () => window.removeEventListener('resize', updateHeight);
  }, []);

  const getFileIcon = (type: string) => {
    switch (type.toLowerCase()) {
      case 'jpg':
      case 'jpeg':
      case 'png':
      case 'gif':
        return <Image className="h-4 w-4" />;
      case 'pdf':
        return <FileText className="h-4 w-4" />;
      case 'folder':
        return <Folder className="h-4 w-4" />;
      default:
        return <File className="h-4 w-4" />;
    }
  };

  const handleDownload = async (event: MouseEvent<HTMLButtonElement>, file: FileMetadata) => {
    event.stopPropagation();
    if (!canDownload) {
      return;
    }

    try {
      const url = await downloadFile(file.id);
      const anchor = document.createElement('a');
      anchor.href = url;
      anchor.download = file.name;
      document.body.appendChild(anchor);
      anchor.click();
      anchor.remove();
      URL.revokeObjectURL(url);
    } catch {
      pushToast('دانلود فایل ناموفق بود.', 'error');
    }
  };

  const handleDelete = async (event: MouseEvent<HTMLButtonElement>, file: FileMetadata) => {
    event.stopPropagation();
    if (!canDelete) {
      return;
    }

    const approved = await confirm(buildDeleteConfirmOptions(`فایل «${file.name}»`));
    if (!approved) {
      return;
    }

    try {
      await deleteFile(file.id);
      pushToast('فایل با موفقیت حذف شد.', 'success');
    } catch {
      pushToast('حذف فایل ناموفق بود.', 'error');
    }
  };

  const Row = ({ index, style }: any) => {
    const file = files[index];
    const isSelected = selectedFiles.includes(file.id);

    return (
      <div
        style={style}
        className={`flex items-center gap-2 border-b p-2 hover:bg-accent sm:gap-4 cursor-pointer ${
          isSelected ? 'bg-blue-50 border-l-4 border-blue-500' : ''
        }`}
        onClick={() => {
          selectFile(file.id);
          setSelectedFile(file.id);
          if (onFileClick) onFileClick(file);
        }}
      >
        <div className="w-8 flex-shrink-0">
          {getFileIcon(file.type)}
        </div>
        <div className="flex-1 min-w-0">
          <div className="font-medium truncate">{file.name}</div>
          <div className="text-xs text-muted-foreground">{file.owner}</div>
        </div>
        <div className="hidden text-xs text-muted-foreground sm:block">
          {(file.size / 1024 / 1024).toFixed(2)} MB
        </div>
        <div className="hidden text-xs text-muted-foreground md:block">
          {new Date(file.createdAt).toLocaleDateString()}
        </div>
        <div className="flex items-center gap-1 sm:gap-1.5">
          <button 
            type="button" 
            className="p-1 hover:bg-accent rounded" 
            aria-label="Download file"
            disabled={!canDownload}
            onClick={(event) => handleDownload(event, file)}
          >
            <Download className="h-4 w-4" />
          </button>
          <button 
            type="button" 
            className="p-1 hover:bg-accent rounded" 
            aria-label="Edit file"
            disabled={!canEdit}
            onClick={(event) => event.stopPropagation()}
          >
            <Edit className="h-4 w-4" />
          </button>
          <button 
            type="button" 
            className="p-1 hover:bg-red-50 text-red-600 rounded disabled:text-muted-foreground"
            aria-label="Delete file"
            disabled={!canDelete}
            onClick={(event) => handleDelete(event, file)}
          >
            <Trash2 className="h-4 w-4" />
          </button>
        </div>
      </div>
    );
  };

  const itemCount = files.length;

  return (
    <div className="h-full">
      <FixedSizeList
        height={listHeight}
        itemCount={itemCount}
        itemSize={60}
        width="100%"
      >
        {Row}
      </FixedSizeList>
    </div>
  );
};

export default FileList;
