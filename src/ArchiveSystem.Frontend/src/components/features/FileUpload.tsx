// src/components/features/FileUpload.tsx
import React, { useState, useRef } from 'react';
import { UploadProgress } from '../../types';
import { useUIStore } from '../../stores/useUIStore';
import { useFileStore } from '../../stores/useFileStore';
import { Upload as UploadIcon, X } from 'lucide-react';

interface FileUploadProps {
  onUploadComplete?: (file: any) => void;
}

const FileUpload = ({ onUploadComplete }: FileUploadProps) => {
  const [dragActive, setDragActive] = useState(false);
  const [uploads, setUploads] = useState<UploadProgress[]>([]);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const { closeUploadPanel } = useUIStore();
  const { uploadFile } = useFileStore();

  const handleFiles = (files: FileList) => {
    Array.from(files).forEach(file => {
      const uploadId = `upload_${Date.now()}_${file.name}`;
      
      const newUpload: UploadProgress = {
        id: uploadId,
        fileName: file.name,
        progress: 0,
        status: 'uploading',
        size: file.size,
        uploadedBytes: 0
      };

      setUploads(prev => [...prev, newUpload]);

      void uploadSingleFile(uploadId, file);
    });
  };

  const uploadSingleFile = async (uploadId: string, file: File) => {
    try {
      await uploadFile(file, (progress) => {
        setUploads(prev =>
          prev.map(upload =>
            upload.id === uploadId
              ? {
                  ...upload,
                  progress: Math.min(progress, 100),
                  uploadedBytes: Math.floor((Math.min(progress, 100) / 100) * file.size)
                }
              : upload
          )
        );
      });

      setUploads(prev =>
        prev.map(upload =>
          upload.id === uploadId
            ? { ...upload, progress: 100, status: 'completed', uploadedBytes: file.size }
            : upload
        )
      );

      onUploadComplete?.({
        id: uploadId,
        fileName: file.name,
        size: file.size
      });
    } catch (error) {
      setUploads(prev =>
        prev.map(upload =>
          upload.id === uploadId
            ? { ...upload, status: 'error' }
            : upload
        )
      );
    }
  };

  const handleDrag = (e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    if (e.type === 'dragenter' || e.type === 'dragover') {
      setDragActive(true);
    } else if (e.type === 'dragleave') {
      setDragActive(false);
    }
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setDragActive(false);
    
    if (e.dataTransfer.files && e.dataTransfer.files[0]) {
      handleFiles(e.dataTransfer.files);
    }
  };

  const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files[0]) {
      handleFiles(e.target.files);
    }
  };

  const cancelUpload = (uploadId: string) => {
    setUploads(prev => prev.filter(upload => upload.id !== uploadId));
  };

  return (
    <div className="p-4 sm:p-6">
      <div className="flex items-center justify-between mb-4">
        <h2 className="text-lg font-semibold">Upload Documents</h2>
        <button
          type="button"
          onClick={closeUploadPanel}
          className="p-1 hover:bg-accent rounded"
          aria-label="Close upload panel"
        >
          <X className="h-5 w-5" />
        </button>
      </div>

      <div
        className={`cursor-pointer rounded-lg border-2 border-dashed p-5 text-center transition-colors sm:p-8 ${
          dragActive ? 'border-blue-500 bg-blue-50' : 'border-gray-300'
        }`}
        onDragEnter={handleDrag}
        onDragLeave={handleDrag}
        onDragOver={handleDrag}
        onDrop={handleDrop}
        onClick={() => fileInputRef.current?.click()}
      >
        <UploadIcon className="mx-auto h-12 w-12 text-gray-400 mb-4" />
        <p className="mb-2 text-base font-medium sm:text-lg">Drag and drop files here</p>
        <p className="text-sm text-gray-500 mb-4">or click to browse</p>
        <button type="button" className="px-4 py-2 bg-primary text-white rounded-md hover:bg-primary/90">
          Select Files
        </button>
        <input
          ref={fileInputRef}
          type="file"
          multiple
          className="hidden"
          onChange={handleFileSelect}
          aria-label="Select files to upload"
        />
      </div>

      {uploads.length > 0 && (
        <div className="mt-6 space-y-3">
          <h3 className="font-medium">Upload Queue</h3>
          {uploads.map(upload => (
            <div key={upload.id} className="rounded-lg border p-3">
              <div className="mb-2 flex items-start justify-between gap-2">
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium truncate">{upload.fileName}</p>
                  <p className="text-xs text-gray-500">
                    {Math.round(upload.uploadedBytes / 1024 / 1024)} MB / {Math.round(upload.size / 1024 / 1024)} MB
                  </p>
                </div>
                <div className="flex items-center gap-1">
                  <button
                    type="button"
                    onClick={() => cancelUpload(upload.id)}
                    className="p-1 hover:bg-accent rounded text-red-500"
                    aria-label="Cancel upload"
                  >
                    <X className="h-4 w-4" />
                  </button>
                </div>
              </div>
              <div className="w-full bg-gray-200 rounded-full h-2">
                <div
                  className={`h-2 rounded-full transition-all duration-300 ${
                    upload.status === 'error' ? 'bg-red-500' : 'bg-blue-600'
                  }`}
                  style={{ width: `${upload.progress}%` }}
                ></div>
              </div>
              <div className="text-xs text-gray-500 mt-1">
                {upload.status === 'error' ? 'Upload failed' : `${Math.round(upload.progress)}% complete`}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};

export default FileUpload;

