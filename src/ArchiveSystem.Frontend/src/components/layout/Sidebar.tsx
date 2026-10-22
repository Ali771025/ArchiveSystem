// src/components/layout/Sidebar.tsx
import React from 'react';
import { ChevronLeft, ChevronRight, Folder, Home, Search, Settings, Upload } from 'lucide-react';
import { Button } from '../common/Button';
import { useUIStore } from '../../stores/useUIStore';

const Sidebar = () => {
  const { sidebarCollapsed, toggleSidebar } = useUIStore();

  const navItems = [
    { icon: Home, label: 'Dashboard', href: '/' },
    { icon: Folder, label: 'File Manager', href: '/files' },
    { icon: Search, label: 'Search', href: '/search' },
    { icon: Upload, label: 'Upload', href: '/upload' },
    { icon: Settings, label: 'Settings', href: '/settings' },
  ];

  return (
    <aside
      className={`h-full overflow-y-auto border-l bg-background transition-all duration-300 ${
        sidebarCollapsed ? 'w-16' : 'w-72'
      }`}
    >
      <div className="relative p-2 sm:p-3">
        <Button
          variant="ghost"
          size="icon"
          onClick={toggleSidebar}
          className="absolute left-2 top-2 hidden lg:flex"
        >
          {sidebarCollapsed ? <ChevronRight /> : <ChevronLeft />}
        </Button>

        <nav className={`${sidebarCollapsed ? 'mt-8' : 'mt-10'} space-y-1`}>
          {navItems.map((item) => (
            <a
              key={item.href}
              href={item.href}
              className={`flex items-center rounded-md px-3 py-2 text-sm font-medium hover:bg-accent ${
                sidebarCollapsed ? 'justify-center' : 'gap-3'
              }`}
              title={item.label}
            >
              <item.icon className="h-5 w-5 shrink-0" />
              {!sidebarCollapsed && <span className="truncate">{item.label}</span>}
            </a>
          ))}
        </nav>
      </div>
    </aside>
  );
};

export default Sidebar;
