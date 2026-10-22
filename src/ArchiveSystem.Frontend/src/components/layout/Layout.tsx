// src/components/layout/Layout.tsx
import React from 'react';
import Header from './Header';
import Sidebar from './Sidebar';
import { useUIStore } from '../../stores/useUIStore';

interface LayoutProps {
  children: React.ReactNode;
}

const Layout = ({ children }: LayoutProps) => {
  const { sidebarCollapsed, toggleSidebar } = useUIStore();

  return (
    <div className="relative flex min-h-screen bg-background">
      <div
        className={`fixed inset-y-0 right-0 z-40 transition-transform duration-300 lg:static lg:translate-x-0 ${
          sidebarCollapsed ? 'translate-x-full lg:translate-x-0' : 'translate-x-0'
        }`}
      >
        <Sidebar />
      </div>
      {!sidebarCollapsed && (
        <button
          type="button"
          aria-label="Close sidebar overlay"
          className="fixed inset-0 z-30 bg-black/30 lg:hidden"
          onClick={toggleSidebar}
        />
      )}
      <div className="flex min-w-0 flex-1 flex-col overflow-hidden">
        <Header />
        <main className="flex-1 overflow-auto p-3 sm:p-4 lg:p-5">
          {children}
        </main>
      </div>
    </div>
  );
};

export default Layout;
