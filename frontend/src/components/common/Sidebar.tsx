import React from 'react';
import { NavLink } from 'react-router-dom';
import { useAuthStore } from '../../store/useAuthStore';
import {
  LayoutDashboard,
  CheckSquare,
  Users,
  ShieldCheck,
  ClipboardList,
  ScrollText,
  X,
} from 'lucide-react';

interface NavItem {
  label: string;
  shortLabel: string;
  path: string;
  icon: React.ReactNode;
  shortIcon: React.ReactNode;
  allowedRoles: Array<'Admin' | 'Employee'>;
}

const getVisibleNavItems = (role: string | null | undefined): NavItem[] => {
  const isAdmin = role === 'Admin';
  const items: NavItem[] = [
    {
      label: 'Dashboard',
      shortLabel: 'Dashboard',
      path: '/',
      icon: <LayoutDashboard className="w-5 h-5" />,
      shortIcon: <LayoutDashboard className="w-[18px] h-[18px]" />,
      allowedRoles: ['Admin', 'Employee'],
    },
    {
      label: isAdmin ? 'All Tasks' : 'My Tasks',
      shortLabel: 'Tasks',
      path: '/tasks',
      icon: <CheckSquare className="w-5 h-5" />,
      shortIcon: <CheckSquare className="w-[18px] h-[18px]" />,
      allowedRoles: ['Admin', 'Employee'],
    },
    {
      label: 'Employees',
      shortLabel: 'Team',
      path: '/employees',
      icon: <Users className="w-5 h-5" />,
      shortIcon: <Users className="w-[18px] h-[18px]" />,
      allowedRoles: ['Admin'],
    },
    {
      label: 'Audit & Logs',
      shortLabel: 'Audit',
      path: '/audit',
      icon: <ScrollText className="w-5 h-5" />,
      shortIcon: <ScrollText className="w-[18px] h-[18px]" />,
      allowedRoles: ['Admin'],
    },
  ];

  const activeRole = (role as 'Admin' | 'Employee') || 'Employee';
  return items.filter((item) => item.allowedRoles.includes(activeRole));
};

interface SidebarProps {
  isOpen?: boolean;
  onClose?: () => void;
}

export const Sidebar: React.FC<SidebarProps> = ({ isOpen = false, onClose }) => {
  const { role } = useAuthStore();
  const isAdmin = role === 'Admin';
  const visibleItems = getVisibleNavItems(role);

  return (
    <>
      {isOpen && (
        <div
          onClick={onClose}
          className="fixed inset-0 z-40 bg-slate-900/40 backdrop-blur-[1px] lg:hidden animate-in fade-in duration-200"
          aria-hidden="true"
        />
      )}

      <aside
        aria-hidden={!isOpen ? undefined : false}
        className={`fixed inset-y-0 left-0 z-50 w-72 max-w-[85vw] lg:w-64 flex-shrink-0 bg-white text-slate-700 flex flex-col border-r border-slate-200 transition-transform duration-300 ease-in-out lg:static lg:translate-x-0 lg:visible lg:pointer-events-auto ${
          isOpen
            ? 'translate-x-0 shadow-xl lg:shadow-none visible pointer-events-auto'
            : '-translate-x-full invisible pointer-events-none lg:visible lg:pointer-events-auto'
        }`}
      >
        <div className="flex items-center justify-between px-5 sm:px-6 h-16 border-b border-slate-200">
          <div className="flex items-center gap-3">
            <div className="w-9 h-9 rounded-xl bg-blue-600 flex items-center justify-center text-white shrink-0">
              <ClipboardList className="w-5 h-5" />
            </div>
            <div>
              <div className="font-bold text-base text-slate-900 tracking-wide flex items-center gap-1.5">
                TaskFlow
              </div>
              <div className="text-[11px] text-slate-500">Enterprise Edition</div>
            </div>
          </div>

          <button
            type="button"
            onClick={onClose}
            className="lg:hidden p-2 text-slate-500 hover:text-slate-900 hover:bg-slate-100 rounded-lg transition"
            aria-label="Close navigation menu"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        <nav className="flex-1 px-3 sm:px-4 py-5 space-y-1.5 overflow-y-auto">
          <div className="px-3 pb-2 text-[11px] font-semibold tracking-wider text-slate-400 uppercase">
            Navigation
          </div>
          {visibleItems.map((item) => (
              <NavLink
                key={item.path}
                to={item.path}
                end={item.path === '/'}
                onClick={() => onClose && onClose()}
                className={({ isActive }) =>
                  `flex items-center gap-3 px-3.5 py-2.5 rounded-xl text-sm font-medium transition-all duration-150 ${
                    isActive
                      ? 'bg-blue-50 text-blue-700 border border-blue-200/80'
                      : 'text-slate-600 hover:text-slate-900 hover:bg-slate-100/80 border border-transparent'
                  }`
                }
              >
                {item.icon}
                <span>{item.label}</span>
              </NavLink>
            ))}
        </nav>

        <div className="p-4 m-4 rounded-xl bg-slate-50 border border-slate-200">
          <div className="flex items-center gap-2 mb-1.5 text-xs font-semibold text-slate-700">
            <ShieldCheck className="w-4 h-4 text-emerald-600" />
            <span>Role Policy Active</span>
          </div>
          <p className="text-[11px] text-slate-500 leading-relaxed">
            {isAdmin
              ? 'Administrator: Full task lifecycle, employee access, and audit controls.'
              : 'Employee: Assigned tasks, status updates, and task timeline.'}
          </p>
        </div>
      </aside>
    </>
  );
};

export const MobileBottomNav: React.FC = () => {
  const { role } = useAuthStore();
  const visibleItems = getVisibleNavItems(role);

  return (
    <nav
      aria-label="Mobile quick navigation"
      style={{ gridTemplateColumns: `repeat(${visibleItems.length}, minmax(0, 1fr))` }}
      className="fixed bottom-0 left-0 right-0 z-30 lg:hidden w-full bg-white/95 backdrop-blur-sm border-t border-slate-200 px-1.5 pt-1 pb-[max(0.375rem,env(safe-area-inset-bottom))] grid gap-1 shadow-[0_-4px_16px_rgba(15,23,42,0.06)]"
    >
      {visibleItems.map((item) => (
        <NavLink
          key={item.path}
          to={item.path}
          end={item.path === '/'}
          className={({ isActive }) =>
            `min-w-0 min-h-[48px] flex flex-col items-center justify-center gap-0.5 rounded-xl px-1 py-1 text-[11px] font-medium transition ${
              isActive
                ? 'text-blue-600 bg-blue-50/80 font-semibold'
                : 'text-slate-500 hover:text-slate-800 hover:bg-slate-50'
            }`
          }
        >
          {item.shortIcon}
          <span className="truncate max-w-full">{item.shortLabel}</span>
        </NavLink>
      ))}
    </nav>
  );
};
