import React, { useState } from 'react';
import { Outlet } from 'react-router-dom';
import { Sidebar } from '../common/Sidebar';
import { Navbar } from '../common/Navbar';
import { ToastContainer } from '../common/ToastContainer';

export const MainLayout: React.FC = () => {
  const [isSidebarOpen, setIsSidebarOpen] = useState(false);

  return (
    <div className="flex h-screen bg-slate-50 overflow-hidden font-sans">
      {/* Sidebar with Mobile Drawer Support */}
      <Sidebar isOpen={isSidebarOpen} onClose={() => setIsSidebarOpen(false)} />

      <div className="flex-1 flex flex-col min-w-0 overflow-hidden">
        {/* Navbar with Hamburger Toggle */}
        <Navbar onToggleSidebar={() => setIsSidebarOpen((prev) => !prev)} />

        {/* Responsive Main Content */}
        <main className="flex-1 overflow-y-auto p-3.5 sm:p-6 lg:p-8">
          <Outlet />
        </main>
      </div>
      <ToastContainer />
    </div>
  );
};
