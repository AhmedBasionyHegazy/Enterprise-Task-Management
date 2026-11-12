import React from 'react';
import { useAuthStore } from '../../store/useAuthStore';
import { RoleBadge } from './Badge';
import { LogOut, User as UserIcon, Menu } from 'lucide-react';

interface NavbarProps {
  onToggleSidebar?: () => void;
}

export const Navbar: React.FC<NavbarProps> = ({ onToggleSidebar }) => {
  const { user, logout } = useAuthStore();

  return (
    <header className="sticky top-0 z-30 flex items-center justify-between h-16 px-4 sm:px-6 bg-white/90 backdrop-blur-md border-b border-slate-200 shadow-xs">
      {/* Left: Mobile hamburger toggle & Greeting */}
      <div className="flex items-center gap-2 sm:gap-3 min-w-0">
        <button
          type="button"
          onClick={onToggleSidebar}
          className="md:hidden p-2 -ml-1 text-slate-600 hover:text-slate-900 hover:bg-slate-100 rounded-xl transition focus:outline-hidden"
          aria-label="Open mobile menu"
        >
          <Menu className="w-5 h-5" />
        </button>

        <div className="truncate">
          <h2 className="text-sm sm:text-base font-semibold text-slate-800 truncate">
            Welcome, <span className="text-indigo-600">{user?.fullName || 'User'}</span>
          </h2>
          <p className="text-[11px] sm:text-xs text-slate-400 truncate hidden xs:block">
            {user?.department ? `${user.department} Department` : 'Employee Task Management System'}
          </p>
        </div>
      </div>

      {/* Right controls */}
      <div className="flex items-center gap-2 sm:gap-4 shrink-0">
        {/* Role Pill */}
        <div className="hidden sm:flex items-center gap-2">
          {user?.roleName && <RoleBadge role={user.roleName} size="sm" />}
        </div>

        {/* User Card */}
        <div className="flex items-center gap-2 sm:gap-3 pl-2 sm:pl-3 border-l border-slate-200">
          <div className="w-8 h-8 sm:w-9 sm:h-9 rounded-full bg-gradient-to-tr from-indigo-600 to-violet-500 text-white flex items-center justify-center font-bold text-xs sm:text-sm shadow-xs shrink-0">
            {user?.fullName ? user.fullName.charAt(0).toUpperCase() : <UserIcon className="w-4 h-4" />}
          </div>
          <div className="hidden lg:block text-left">
            <div className="text-xs font-semibold text-slate-800 leading-tight">{user?.fullName}</div>
            <div className="text-[11px] text-slate-400">{user?.email}</div>
          </div>
        </div>

        {/* Logout */}
        <button
          onClick={logout}
          title="Sign Out"
          className="flex items-center gap-1.5 px-2.5 sm:px-3 py-1.5 rounded-xl text-xs font-medium text-slate-500 hover:text-rose-600 hover:bg-rose-50 border border-slate-200 hover:border-rose-200 transition"
        >
          <LogOut className="w-3.5 h-3.5" />
          <span className="hidden sm:inline">Logout</span>
        </button>
      </div>
    </header>
  );
};
