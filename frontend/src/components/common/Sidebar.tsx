import React from 'react';
import { NavLink } from 'react-router-dom';
import { useAuthStore } from '../../store/useAuthStore';
import { 
  LayoutDashboard, 
  CheckSquare, 
  Users, 
  ShieldCheck, 
  ClipboardList,
  X
} from 'lucide-react';

interface SidebarProps {
  isOpen?: boolean;
  onClose?: () => void;
}

export const Sidebar: React.FC<SidebarProps> = ({ isOpen = false, onClose }) => {
  const { role } = useAuthStore();
  const isAdmin = role === 'Admin';

  const navItems = [
    {
      label: 'Dashboard',
      path: '/',
      icon: <LayoutDashboard className="w-5 h-5" />,
      allowedRoles: ['Admin', 'Employee'],
    },
    {
      label: isAdmin ? 'All Tasks' : 'My Tasks',
      path: '/tasks',
      icon: <CheckSquare className="w-5 h-5" />,
      allowedRoles: ['Admin', 'Employee'],
    },
    {
      label: 'Employees',
      path: '/employees',
      icon: <Users className="w-5 h-5" />,
      allowedRoles: ['Admin'],
    },
  ];

  return (
    <>
      {/* Mobile Backdrop Overlay */}
      {isOpen && (
        <div
          onClick={onClose}
          className="fixed inset-0 z-40 bg-slate-950/70 backdrop-blur-xs md:hidden animate-in fade-in duration-200"
          aria-hidden="true"
        />
      )}

      {/* Sidebar Panel (Desktop Static + Mobile Off-Canvas Drawer) */}
      <aside
        className={`fixed inset-y-0 left-0 z-50 w-72 md:w-64 flex-shrink-0 bg-slate-900 text-slate-300 flex flex-col border-r border-slate-800 transition-transform duration-300 ease-in-out md:static md:translate-x-0 ${
          isOpen ? 'translate-x-0 shadow-2xl' : '-translate-x-full'
        }`}
      >
        {/* Brand Header */}
        <div className="flex items-center justify-between px-5 sm:px-6 h-16 border-b border-slate-800/80">
          <div className="flex items-center gap-3">
            <div className="w-9 h-9 rounded-xl bg-gradient-to-tr from-indigo-500 via-indigo-600 to-violet-500 flex items-center justify-center text-white shadow-glow-primary shrink-0">
              <ClipboardList className="w-5 h-5" />
            </div>
            <div>
              <div className="font-bold text-base text-white tracking-wide flex items-center gap-1.5">
                TaskFlow <span className="text-[10px] uppercase font-bold tracking-wider px-1.5 py-0.5 rounded-sm bg-indigo-500/30 text-indigo-300 border border-indigo-500/40">PRO</span>
              </div>
              <div className="text-[11px] text-slate-400">Enterprise Edition</div>
            </div>
          </div>

          {/* Close button on mobile */}
          <button
            type="button"
            onClick={onClose}
            className="md:hidden p-1.5 text-slate-400 hover:text-white hover:bg-slate-800 rounded-lg transition"
            aria-label="Close navigation menu"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Navigation Links */}
        <nav className="flex-1 px-4 py-6 space-y-1.5 overflow-y-auto">
          <div className="px-3 pb-2 text-[11px] font-semibold tracking-wider text-slate-500 uppercase">
            Menu
          </div>
          {navItems
            .filter((item) => item.allowedRoles.includes(role || 'Employee'))
            .map((item) => (
              <NavLink
                key={item.path}
                to={item.path}
                end={item.path === '/'}
                onClick={() => onClose && onClose()}
                className={({ isActive }) =>
                  `flex items-center gap-3 px-3.5 py-2.5 rounded-xl text-sm font-medium transition-all duration-150 ${
                    isActive
                      ? 'bg-indigo-600 text-white shadow-md shadow-indigo-600/20'
                      : 'text-slate-400 hover:text-slate-200 hover:bg-slate-800/60'
                  }`
                }
              >
                {item.icon}
                <span>{item.label}</span>
              </NavLink>
            ))}
        </nav>

        {/* Security Info Card */}
        <div className="p-4 m-4 rounded-xl bg-slate-800/60 border border-slate-700/50">
          <div className="flex items-center gap-2 mb-1.5 text-xs font-semibold text-slate-200">
            <ShieldCheck className="w-4 h-4 text-emerald-400" />
            <span>Role Policy Active</span>
          </div>
          <p className="text-[11px] text-slate-400 leading-relaxed">
            {isAdmin 
              ? 'Administrator: Full task lifecycle and employee management privileges.' 
              : 'Employee: Restricted to assigned tasks, status updates, and discussions.'}
          </p>
        </div>
      </aside>
    </>
  );
};
