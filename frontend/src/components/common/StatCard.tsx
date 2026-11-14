import React from 'react';

interface StatCardProps {
  title: string;
  value: number | string;
  subtitle?: string;
  icon: React.ReactNode;
  variant?: 'default' | 'primary' | 'warning' | 'success' | 'danger';
  trend?: string;
}

export const StatCard: React.FC<StatCardProps> = ({
  title,
  value,
  subtitle,
  icon,
  variant = 'default',
  trend,
}) => {
  const getIconStyles = () => {
    switch (variant) {
      case 'primary':
        return 'bg-blue-50 border-blue-100 text-blue-600';
      case 'success':
        return 'bg-emerald-50 border-emerald-100 text-emerald-600';
      case 'warning':
        return 'bg-amber-50 border-amber-100 text-amber-600';
      case 'danger':
        return 'bg-rose-50 border-rose-100 text-rose-600';
      default:
        return 'bg-slate-50 border-slate-200 text-slate-600';
    }
  };

  return (
    <div className="bg-white rounded-2xl border border-slate-200 p-3 sm:p-5 shadow-xs hover:border-slate-300 transition w-full min-w-0 overflow-hidden">
      <div className="flex items-center justify-between gap-1.5 mb-2 sm:mb-3 min-w-0">
        <span className="text-xs sm:text-sm font-medium text-slate-500 truncate">{title}</span>
        <div className={`p-1.5 sm:p-2 rounded-xl border shrink-0 ${getIconStyles()}`}>{icon}</div>
      </div>

      <div className="flex items-baseline gap-2 min-w-0">
        <span className="text-xl sm:text-3xl font-bold tracking-tight text-slate-900">{value}</span>
        {trend && <span className="text-xs font-semibold text-emerald-600">{trend}</span>}
      </div>

      {subtitle && <p className="mt-1 text-[11px] sm:text-xs text-slate-400 break-words leading-snug">{subtitle}</p>}
    </div>
  );
};
