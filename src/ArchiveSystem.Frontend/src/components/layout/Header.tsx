// src/components/layout/Header.tsx
import React from 'react';
import { Bell, Menu, Search, Sun, User, Moon } from 'lucide-react';
import { Button } from '../common/Button';
import { useAuthStore } from '../../stores/useAuthStore';
import { useMovementLogStore } from '../../stores/useMovementLogStore';
import { useUIStore } from '../../stores/useUIStore';

const Header = () => {
  const { logout } = useAuthStore();
  const { toggleSidebar, toggleDarkMode, darkMode, openUploadPanel } = useUIStore();
  const openMovementModal = () => useMovementLogStore.getState().openModal();

  return (
    <header className="sticky top-0 z-50 border-b bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
      <div className="flex min-h-16 flex-wrap items-center gap-3 px-3 py-2 sm:px-4">
        <div className="flex min-w-0 flex-1 items-center gap-2">
          <Button variant="ghost" size="icon" onClick={toggleSidebar} className="lg:hidden">
            <Menu className="h-5 w-5" />
          </Button>
          <h1 className="truncate text-base font-bold sm:text-xl">Enterprise DMS</h1>
        </div>

        <div className="order-3 w-full sm:order-2 sm:w-auto sm:max-w-md sm:flex-1">
          <div className="relative">
            <Search className="absolute right-2 top-2.5 h-4 w-4 text-muted-foreground" />
            <input
              type="text"
              placeholder="Search documents..."
              className="w-full rounded-md bg-muted py-2 pr-8 pl-4 text-sm"
            />
          </div>
        </div>

        <div className="order-2 flex w-full flex-wrap items-center justify-end gap-1 sm:order-3 sm:w-auto sm:gap-2">
          <Button variant="ghost" size="icon" onClick={openUploadPanel}>
            <span className="sr-only">Upload</span>
            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" />
              <polyline points="17 8 12 3 7 8" />
              <line x1="12" x2="12" y1="3" y2="15" />
            </svg>
          </Button>

          <Button variant="ghost" size="icon">
            <Bell className="h-5 w-5" />
          </Button>

          <Button variant="ghost" size="icon" onClick={toggleDarkMode}>
            {darkMode ? <Sun className="h-5 w-5" /> : <Moon className="h-5 w-5" />}
          </Button>

          <Button variant="ghost" size="icon">
            <User className="h-5 w-5" />
          </Button>

          <Button variant="outline" size="sm" onClick={openMovementModal}>
            ثبت امانت پرونده
          </Button>

          <Button variant="outline" size="sm" onClick={logout}>
            Logout
          </Button>
        </div>
      </div>
    </header>
  );
};

export default Header;
