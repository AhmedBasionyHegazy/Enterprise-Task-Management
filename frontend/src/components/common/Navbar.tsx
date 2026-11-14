import React, { useState } from 'react';
import { useAuthStore } from '../../store/useAuthStore';
import { RoleBadge } from './Badge';
import { NotificationBell } from '../notifications/NotificationBell';
import { SessionsModal } from '../auth/SessionsModal';
import { LogOut, User as UserIcon, Menu, Shield } from 'lucide-react';

interface NavbarProps {
  onToggleSidebar?: () => void;
}

export const Navbar: React.FC<NavbarProps> = ({ onToggleSidebar }) => {
  const { user, logout } = useAuthStore();
  const [isSessionsOpen, setIsSessionsOpen] = useState(false);

  return (
    <>
      <header className="fixed top-0 left-0 right-0 lg:static z-30 flex items-center justify-between h-16 w-full px-3 sm:px-6 bg-white border-b border-slate-200 shadow-xs">
        {/* Left: Mobile hamburger toggle & Greeting */}
        <div className="flex items-center gap-1.5 sm:gap-3 flex-1 min-w-0 pr-2">
          <button
            type="button"
            onClick={onToggleSidebar}
            className="lg:hidden p-2 -ml-1 text-slate-600 hover:text-slate-900 hover:bg-slate-100 rounded-xl transition focus:outline-hidden shrink-0"
            aria-label="Open mobile menu"
          >
            <Menu className="w-5 h-5" />
          </button>

          <div className="min-w-0 truncate">
            <h2 className="text-sm sm:text-base font-semibold text-slate-800 truncate">
              <span className="hidden sm:inline font-medium text-slate-500">Welcome, </span>
              <span className="text-blue-600">{user?.fullName || 'User'}</span>
            </h2>
            <p className="text-[11px] sm:text-xs text-slate-400 truncate hidden sm:block">
              {user?.department ? `${user.department} Department` : 'Employee Task Management System'}
            </p>
          </div>
        </div>

        {/* Right controls */}
        <div className="flex items-center gap-1 sm:gap-2.5 shrink-0">
          <div className="hidden lg:flex items-center">
            {user?.roleName && <RoleBadge role={user.roleName} size="sm" />}
          </div>

          <NotificationBell />

          <button
            type="button"
            onClick={() => setIsSessionsOpen(true)}
            title="Active Sessions & Security"
            aria-label="Active Sessions"
            className="p-2 rounded-xl text-slate-500 hover:text-slate-900 hover:bg-slate-100 border border-transparent hover:border-slate-200 transition"
          >
            <Shield className="w-4 h-4 sm:w-5 sm:h-5" />
          </button>

          {/* User Card */}
          <div className="hidden min-[380px]:flex items-center gap-2 pl-1.5 sm:pl-2 border-l border-slate-200">
            <div className="w-8 h-8 rounded-full bg-blue-600 text-white flex items-center justify-center font-bold text-xs shrink-0">
              {user?.fullName ? user.fullName.charAt(0).toUpperCase() : <UserIcon className="w-4 h-4" />}
            </div>
            <div className="hidden lg:block text-left">
              <div className="text-xs font-semibold text-slate-800 leading-tight">{user?.fullName}</div>
              <div className="text-[11px] text-slate-400">{user?.email}</div>
            </div>
          </div>

          {/* Logout */}
          <button
            type="button"
            onClick={logout}
            title="Sign Out"
            aria-label="Sign Out"
            className="flex items-center justify-center gap-1.5 px-2.5 sm:px-3 py-2 sm:py-1.5 rounded-xl text-xs font-medium text-slate-600 hover:text-rose-600 hover:bg-rose-50 border border-slate-200 hover:border-rose-200 transition"
          >
            <LogOut className="w-4 h-4 sm:w-3.5 sm:h-3.5" />
            <span className="hidden sm:inline">Logout</span>
          </button>
        </div>
      </header>

      <SessionsModal isOpen={isSessionsOpen} onClose={() => setIsSessionsOpen(false)} />
    </>
  );
};
