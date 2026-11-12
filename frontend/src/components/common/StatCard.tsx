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
  const getVariantStyles = () => {
    switch (variant) {
      case 'primary':
        return 'from-indigo-500/10 to-indigo-50/30 border-indigo-200/80 text-indigo-600';
      case 'success':
        return 'from-emerald-500/10 to-emerald-50/30 border-emerald-200/80 text-emerald-600';
      case 'warning':
        return 'from-amber-500/10 to-amber-50/30 border-amber-200/80 text-amber-600';
      case 'danger':
        return 'from-rose-500/10 to-rose-50/30 border-rose-200/80 text-rose-600';
      default:
        return 'from-slate-500/10 to-slate-50/30 border-slate-200 text-slate-600';
    }
  };

  return (
    <div className="relative overflow-hidden bg-white rounded-2xl border border-slate-200/80 p-5 shadow-xs hover:shadow-md transition-all duration-200">
      <div className={`absolute top-0 right-0 w-32 h-32 bg-gradient-to-bl ${getVariantStyles()} rounded-bl-full pointer-events-none opacity-60`} />

      <div className="flex items-center justify-between mb-3 relative z-10">
        <span className="text-sm font-medium text-slate-500 tracking-wide">{title}</span>
        <div className={`p-2.5 rounded-xl bg-slate-50 border border-slate-100 ${getVariantStyles()}`}>
          {icon}
        </div>
      </div>

      <div className="flex items-baseline gap-2 relative z-10">
        <span className="text-3xl font-bold tracking-tight text-slate-900">{value}</span>
        {trend && <span className="text-xs font-semibold text-emerald-600">{trend}</span>}
      </div>

      {subtitle && (
        <p className="mt-1 text-xs text-slate-400 relative z-10">{subtitle}</p>
      )}
    </div>
  );
};
