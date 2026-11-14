import React, { useState } from 'react';
import { Outlet } from 'react-router-dom';
import { Sidebar, MobileBottomNav } from '../common/Sidebar';
import { Navbar } from '../common/Navbar';
import { ToastContainer } from '../common/ToastContainer';
import { useRealtimeNotifications } from '../../hooks/useRealtimeNotifications';

export const MainLayout: React.FC = () => {
  const [isSidebarOpen, setIsSidebarOpen] = useState(false);
  useRealtimeNotifications();

  return (
    <div className="min-h-dvh lg:h-screen w-full flex bg-slate-50 lg:overflow-hidden font-sans">
      <Sidebar isOpen={isSidebarOpen} onClose={() => setIsSidebarOpen(false)} />

      <div className="flex-1 flex flex-col min-w-0 w-full lg:overflow-hidden">
        <Navbar onToggleSidebar={() => setIsSidebarOpen((prev) => !prev)} />

        <main className="flex-1 min-w-0 w-full overflow-x-hidden lg:overflow-y-auto px-3 sm:px-6 pt-20 pb-24 lg:p-8">
          <Outlet />
        </main>
      </div>

      <MobileBottomNav />
      <ToastContainer />
    </div>
  );
};
