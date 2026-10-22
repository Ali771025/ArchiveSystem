// src/pages/upload.tsx
import React from 'react';
import FileUpload from '@/components/features/FileUpload';

const UploadPage = () => {
  return (
    <div className="mx-auto w-full max-w-4xl px-1 sm:px-0">
      <div className="mb-6">
        <h1 className="text-2xl font-bold tracking-tight sm:text-3xl">Document Upload</h1>
        <p className="text-muted-foreground">Upload and manage your documents</p>
      </div>
      
      <FileUpload />
    </div>
  );
};

export default UploadPage;
