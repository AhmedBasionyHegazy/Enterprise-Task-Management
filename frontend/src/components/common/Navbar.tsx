import React from 'react';
import { useAuthStore } from '../../store/useAuthStore';
import { RoleBadge } from './Badge';
import { LogOut, User as UserIcon, Bell } from 'lucide-react';

interface NavbarProps {
  onToggleSidebar?: () => void;
}

export const Navbar: React.FC<NavbarProps> = () => {
  const { user, logout } = useAuthStore();

  return (
    <header className="sticky top-0 z-30 flex items-center justify-between h-16 px-6 bg-white/90 backdrop-blur-md border-b border-slate-200 shadow-xs">
      {/* Title / Greetings */}
      <div className="flex items-center gap-3">
        <div>
          <h2 className="text-base font-semibold text-slate-800">
            Welcome back, <span className="text-indigo-600">{user?.fullName || 'User'}</span>
          </h2>
          <p className="text-xs text-slate-400">
            {user?.department ? `${user.department} Department` : 'Employee Task Management System'}
          </p>
        </div>
      </div>

      {/* Right controls */}
      <div className="flex items-center gap-4">
        {/* Status Pill */}
        <div className="hidden sm:flex items-center gap-2">
          {user?.roleName && <RoleBadge role={user.roleName} />}
        </div>

        {/* User Card */}
        <div className="flex items-center gap-3 pl-3 border-l border-slate-200">
          <div className="w-9 h-9 rounded-full bg-gradient-to-tr from-indigo-600 to-violet-500 text-white flex items-center justify-center font-bold text-sm shadow-xs">
            {user?.fullName ? user.fullName.charAt(0).toUpperCase() : <UserIcon className="w-4 h-4" />}
          </div>
          <div className="hidden md:block text-left">
            <div className="text-xs font-semibold text-slate-800 leading-tight">{user?.fullName}</div>
            <div className="text-[11px] text-slate-400">{user?.email}</div>
          </div>
        </div>

        {/* Logout */}
        <button
          onClick={logout}
          title="Sign Out"
          className="flex items-center gap-1.5 px-3 py-1.5 rounded-xl text-xs font-medium text-slate-500 hover:text-rose-600 hover:bg-rose-50 border border-slate-200 hover:border-rose-200 transition"
        >
          <LogOut className="w-3.5 h-3.5" />
          <span className="hidden sm:inline">Logout</span>
        </button>
      </div>
    </header>
  );
};
