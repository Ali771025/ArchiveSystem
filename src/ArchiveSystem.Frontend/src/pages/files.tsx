// src/pages/files.tsx
import React, { useEffect } from 'react';
import { Card, CardContent } from '@/components/ui/card';
import FileList from '@/components/features/FileList';
import TaxonomyTree from '@/components/features/TaxonomyTree';
import { useFileStore } from '@/stores/useFileStore';

const FileManagerPage = () => {
  const { files, treeData, initializeMockData } = useFileStore();

  useEffect(() => {
    initializeMockData();
  }, []);

  return (
    <div className="grid h-full gap-4 lg:grid-cols-[minmax(0,18rem)_minmax(0,1fr)_minmax(0,18rem)]">
      <div className="order-2 rounded-lg border p-3 sm:p-4 lg:order-1 lg:overflow-y-auto">
        <h2 className="text-lg font-semibold mb-4">Navigation</h2>
        <TaxonomyTree nodes={treeData} />
      </div>

      <div className="order-1 lg:order-2">
        <div className="mb-4">
          <h1 className="text-2xl font-bold">File Manager</h1>
          <p className="text-muted-foreground">Browse and manage your documents</p>
        </div>

        <Card>
          <CardContent className="p-0">
            <div className="flex flex-wrap items-center justify-between gap-2 border-b p-3 sm:p-4">
              <h3 className="font-medium">Documents</h3>
              <div className="flex flex-wrap gap-2">
                <button type="button" className="px-3 py-1 text-sm bg-primary text-white rounded hover:bg-primary/90">
                  Upload
                </button>
                <button type="button" className="px-3 py-1 text-sm border rounded hover:bg-accent">
                  Create Folder
                </button>
              </div>
            </div>

            <div className="h-[50vh] sm:h-[60vh] lg:h-[calc(100vh-220px)]">
              <FileList files={files} />
            </div>
          </CardContent>
        </Card>
      </div>

      <div className="order-3 rounded-lg border p-3 sm:p-4 lg:overflow-y-auto">
        <h2 className="text-lg font-semibold mb-4">File Details</h2>
        <div className="text-center text-muted-foreground py-8">
          Select a file to view details
        </div>
      </div>
    </div>
  );
};

export default FileManagerPage;

