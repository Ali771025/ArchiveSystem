// src/pages/_app.tsx
import React, { useEffect } from 'react';
import { Toaster } from 'sonner';
import { useAuthStore } from '../stores/useAuthStore';
import { useUIStore } from '../stores/useUIStore';
import Layout from '../components/layout/Layout';
import FileUpload from '../components/features/FileUpload';
import '../styles/globals.css';

interface AppShellProps {
  Component: React.ComponentType<Record<string, unknown>>;
  pageProps?: Record<string, unknown>;
}

function MyApp({ Component, pageProps = {} }: AppShellProps) {
  const { isAuthenticated } = useAuthStore();
  const { uploadPanelOpen, closeUploadPanel } = useUIStore();

  useEffect(() => {
    // Initialize mock data
    console.log('Initializing mock data...');
  }, []);

  if (!isAuthenticated && Component.name !== 'LoginPage') {
    return <Component {...pageProps} />;
  }

  return (
    <>
      <Layout>
        <Component {...pageProps} />
      </Layout>
      
      {uploadPanelOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50 p-3 sm:p-4">
          <div className="max-h-[90vh] w-full max-w-2xl overflow-hidden rounded-lg bg-white shadow-xl">
            <FileUpload />
          </div>
        </div>
      )}
      
      <Toaster position="top-right" />
    </>
  );
}

export default MyApp;

