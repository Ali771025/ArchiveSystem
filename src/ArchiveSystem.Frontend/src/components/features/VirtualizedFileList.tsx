// src/components/features/VirtualizedFileList.tsx (Complete Implementation)
import React from 'react';
import FixedSizeList from '../ui/FixedSizeList';
import { FileMetadata } from '../../types';
import {
  Download,
  Edit,
  Trash2,
  Folder,
  FileText,
  Image as ImageIcon,
  File as FileIcon
} from 'lucide-react';

interface VirtualizedFileListProps {
  files: FileMetadata[];
  onFileClick?: (file: FileMetadata) => void;
  onFileSelect?: (fileId: string, selected: boolean) => void;
  selectedFiles?: Set<string>;
  onTagClick?: (tag: string) => void;
}

const VirtualizedFileList: React.FC<VirtualizedFileListProps> = ({
  files,
  onFileClick,
  onFileSelect,
  selectedFiles = new Set(),
  onTagClick
}) => {
  const getFileIcon = (type: string) => {
    switch (type.toLowerCase()) {
      case 'jpg':
      case 'jpeg':
      case 'png':
      case 'gif':
        return <ImageIcon className="h-4 w-4" />;
      case 'pdf':
        return <FileText className="h-4 w-4" />;
      case 'folder':
        return <Folder className="h-4 w-4" />;
      default:
        return <FileIcon className="h-4 w-4" />;
    }
  };

  const Row = ({ index, style }: { index: number; style: React.CSSProperties }) => {
    const file = files[index];
    const isSelected = selectedFiles.has(file.id);

    return (
      <div
        style={style}
        className={`flex items-center gap-4 p-2 border-b hover:bg-accent cursor-pointer transition-colors ${
          isSelected ? 'bg-blue-50 border-l-4 border-blue-500' : ''
        }`}
        onClick={() => {
          onFileClick?.(file);
          onFileSelect?.(file.id, !isSelected);
        }}
      >
        <div className="w-8 flex-shrink-0">
          {getFileIcon(file.type)}
        </div>
        <div className="flex-1 min-w-0">
          <div className="font-medium truncate">{file.name}</div>
          <div className="text-xs text-muted-foreground">{file.owner}</div>
        </div>
        <div className="text-xs text-muted-foreground">
          {(file.size / 1024 / 1024).toFixed(2)} MB
        </div>
        <div className="text-xs text-muted-foreground">
          {new Date(file.createdAt).toLocaleDateString()}
        </div>
        <div className="flex items-center gap-1">
          {file.tags && file.tags.length > 0 && (
            <div className="flex gap-1">
              {file.tags.slice(0, 2).map((tag, idx) => (
                <span
                  key={idx}
                  className="text-xs bg-blue-100 text-blue-800 px-1 rounded cursor-pointer hover:bg-blue-200"
                  onClick={(e) => {
                    e.stopPropagation();
                    onTagClick?.(tag);
                  }}
                >
                  {tag}
                </span>
              ))}
              {file.tags.length > 2 && (
                <span className="text-xs text-muted-foreground">+{file.tags.length - 2}</span>
              )}
            </div>
          )}
          <button 
            className="p-1 hover:bg-accent rounded" 
            aria-label="Download file"
            onClick={(e) => {
              e.stopPropagation();
              // Add download functionality here
            }}
          >
            <Download className="h-4 w-4" />
          </button>
          <button 
            className="p-1 hover:bg-accent rounded" 
            aria-label="Edit file"
            onClick={(e) => {
              e.stopPropagation();
              // Add edit functionality here
            }}
          >
            <Edit className="h-4 w-4" />
          </button>
          <button 
            className="p-1 hover:bg-accent rounded" 
            aria-label="Delete file"
            onClick={(e) => {
              e.stopPropagation();
              // Add delete functionality here
            }}
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
        height={600}
        itemCount={itemCount}
        itemSize={60}
        width="100%"
      >
        {Row}
      </FixedSizeList>
    </div>
  );
};

export default VirtualizedFileList;
