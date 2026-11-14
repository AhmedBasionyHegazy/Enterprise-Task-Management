import React from 'react';

interface StatusBadgeProps {
  status: 'New' | 'In Progress' | 'Completed' | string;
  size?: 'sm' | 'md';
}

export const StatusBadge: React.FC<StatusBadgeProps> = ({ status, size = 'md' }) => {
  const getStyle = () => {
    switch (status) {
      case 'New':
        return 'bg-sky-50 text-sky-700 border-sky-200 ring-1 ring-sky-500/10';
      case 'In Progress':
        return 'bg-amber-50 text-amber-700 border-amber-200 ring-1 ring-amber-500/10';
      case 'Completed':
        return 'bg-emerald-50 text-emerald-700 border-emerald-200 ring-1 ring-emerald-500/10';
      default:
        return 'bg-slate-100 text-slate-700 border-slate-200';
    }
  };

  const getDotStyle = () => {
    switch (status) {
      case 'New':
        return 'bg-blue-500';
      case 'In Progress':
        return 'bg-amber-500 animate-pulse';
      case 'Completed':
        return 'bg-emerald-500';
      default:
        return 'bg-slate-400';
    }
  };

  const padding = size === 'sm' ? 'px-2 py-0.5 text-xs' : 'px-2.5 py-1 text-xs';

  return (
    <span
      className={`inline-flex items-center gap-1.5 font-medium rounded-full border shadow-xs ${getStyle()} ${padding}`}
    >
      <span className={`w-1.5 h-1.5 rounded-full ${getDotStyle()}`} />
      {status}
    </span>
  );
};

interface PriorityBadgeProps {
  priority: 'Low' | 'Medium' | 'High' | string;
  size?: 'sm' | 'md';
}

export const PriorityBadge: React.FC<PriorityBadgeProps> = ({ priority, size = 'md' }) => {
  const getStyle = () => {
    switch (priority) {
      case 'High':
        return 'bg-rose-50 text-rose-700 border-rose-200 ring-1 ring-rose-500/10';
      case 'Medium':
        return 'bg-orange-50 text-orange-700 border-orange-200 ring-1 ring-orange-500/10';
      case 'Low':
        return 'bg-slate-50 text-slate-600 border-slate-200';
      default:
        return 'bg-slate-50 text-slate-600 border-slate-200';
    }
  };

  const padding = size === 'sm' ? 'px-2 py-0.5 text-xs' : 'px-2.5 py-1 text-xs';

  return (
    <span className={`inline-flex items-center font-medium rounded-md border shadow-xs ${getStyle()} ${padding}`}>
      {priority}
    </span>
  );
};

export const RoleBadge: React.FC<{ role: string; size?: 'sm' | 'md' }> = ({ role, size = 'md' }) => {
  const isAdmin = role === 'Admin';
  const sizing = size === 'sm' ? 'px-2 py-0.5 text-[11px]' : 'px-2 py-0.5 text-xs';
  return (
    <span
      className={`inline-flex items-center rounded-md font-semibold ${sizing} ${
        isAdmin
          ? 'bg-slate-800 text-white border border-slate-800'
          : 'bg-emerald-50 text-emerald-700 border border-emerald-200'
      }`}
    >
      {role}
    </span>
  );
};
