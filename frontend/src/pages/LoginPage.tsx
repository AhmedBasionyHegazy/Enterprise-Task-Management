import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuthStore } from '../store/useAuthStore';
import { toast } from '../store/useToastStore';
import {
  ClipboardList,
  Lock,
  User as UserIcon,
  ArrowRight,
  ShieldCheck,
  Briefcase,
  FlaskConical,
  CheckCircle2,
} from 'lucide-react';

export const LoginPage: React.FC = () => {
  const navigate = useNavigate();
  const { login, isLoading, error } = useAuthStore();

  const [usernameOrEmail, setUsernameOrEmail] = useState('');
  const [password, setPassword] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!usernameOrEmail.trim() || !password) {
      toast.warning('Please enter both username/email and password.');
      return;
    }

    const success = await login({ usernameOrEmail: usernameOrEmail.trim(), password });
    if (success) {
      toast.success('Welcome back to TaskFlow Pro');
      navigate('/');
    }
  };

  const handleQuickLogin = async (user: string, pass: string, roleTitle: string) => {
    setUsernameOrEmail(user);
    setPassword(pass);
    const success = await login({ usernameOrEmail: user, password: pass });
    if (success) {
      toast.success(`Logged in as ${roleTitle}`);
      navigate('/');
    }
  };

  return (
    <div className="min-h-dvh w-full max-w-full bg-slate-50 flex items-center justify-center p-3 sm:p-6 lg:p-8 font-sans overflow-x-hidden">
      <div className="w-full max-w-4xl grid grid-cols-1 lg:grid-cols-12 bg-white border border-slate-200 rounded-2xl shadow-sm overflow-hidden min-w-0">
        {/* Left Side on Desktop / Below Form on Mobile: Info & Quick Demo Roles */}
        <div className="order-2 lg:order-1 lg:col-span-5 min-w-0 p-4 sm:p-8 lg:p-10 bg-slate-50/90 border-t lg:border-t-0 lg:border-r border-slate-200 flex flex-col justify-between text-slate-700">
          <div>
            <div className="hidden lg:flex items-center gap-3 mb-6">
              <div className="w-11 h-11 rounded-xl bg-blue-600 flex items-center justify-center text-white shrink-0">
                <ClipboardList className="w-6 h-6" />
              </div>
              <div>
                <h1 className="text-xl font-bold tracking-tight text-slate-900">TaskFlow Pro</h1>
                <p className="text-xs text-slate-500 font-medium">Enterprise Task Management</p>
              </div>
            </div>

            <p className="text-xs sm:text-sm text-slate-600 leading-relaxed mb-5 lg:mb-6">
              Role-based operational task tracking, immutable change auditing, and real-time team notifications.
            </p>

            <div className="space-y-2.5 lg:space-y-3 mb-6 lg:mb-8">
              <div className="flex items-center gap-2.5 text-xs text-slate-600">
                <CheckCircle2 className="w-4 h-4 text-emerald-600 shrink-0" />
                <span>HttpOnly Cookie &amp; CSRF Session Security</span>
              </div>
              <div className="flex items-center gap-2.5 text-xs text-slate-600">
                <CheckCircle2 className="w-4 h-4 text-emerald-600 shrink-0" />
                <span>Append-Only Task History &amp; Audit Trail</span>
              </div>
              <div className="flex items-center gap-2.5 text-xs text-slate-600">
                <CheckCircle2 className="w-4 h-4 text-emerald-600 shrink-0" />
                <span>Live Activity &amp; Correlated Request Tracing</span>
              </div>
            </div>
          </div>

          {/* Quick Demo Logins Box */}
          <div className="p-4 rounded-xl bg-white border border-slate-200 shadow-2xs">
            <div className="flex items-center gap-2 text-xs font-semibold text-slate-800 mb-3">
              <ShieldCheck className="w-4 h-4 text-blue-600 shrink-0" />
              <span>Quick Demo Access</span>
            </div>
            <div className="grid grid-cols-1 gap-2">
              <button
                type="button"
                onClick={() => handleQuickLogin('admin', 'Admin@123', 'System Administrator (Admin)')}
                disabled={isLoading}
                className="w-full min-w-0 py-2.5 px-3 rounded-lg bg-blue-50 hover:bg-blue-100/80 border border-blue-200 text-left text-xs font-medium text-blue-950 transition flex items-center justify-between gap-2 disabled:opacity-50"
              >
                <span className="inline-flex items-center gap-2 min-w-0 truncate">
                  <ShieldCheck className="w-3.5 h-3.5 text-blue-600 shrink-0" />
                  <span className="truncate">Admin (Sarah Jenkins)</span>
                </span>
                <span className="text-[11px] font-semibold text-blue-600 shrink-0">Sign in &rarr;</span>
              </button>

              <button
                type="button"
                onClick={() => handleQuickLogin('john.doe', 'Employee@123', 'John Doe (Employee)')}
                disabled={isLoading}
                className="w-full py-2.5 px-3 rounded-lg bg-slate-50 hover:bg-slate-100 border border-slate-200 text-left text-xs font-medium text-slate-800 transition flex items-center justify-between gap-2 disabled:opacity-50"
              >
                <span className="inline-flex items-center gap-2 min-w-0 truncate">
                  <Briefcase className="w-3.5 h-3.5 text-emerald-600 shrink-0" />
                  <span className="truncate">Employee (John Doe - Dev)</span>
                </span>
                <span className="text-[11px] text-slate-500 shrink-0">Sign in &rarr;</span>
              </button>

              <button
                type="button"
                onClick={() => handleQuickLogin('jane.smith', 'Employee@123', 'Jane Smith (Employee)')}
                disabled={isLoading}
                className="w-full py-2.5 px-3 rounded-lg bg-slate-50 hover:bg-slate-100 border border-slate-200 text-left text-xs font-medium text-slate-800 transition flex items-center justify-between gap-2 disabled:opacity-50"
              >
                <span className="inline-flex items-center gap-2 min-w-0 truncate">
                  <FlaskConical className="w-3.5 h-3.5 text-amber-600 shrink-0" />
                  <span className="truncate">Employee (Jane Smith - QA)</span>
                </span>
                <span className="text-[11px] text-slate-500 shrink-0">Sign in &rarr;</span>
              </button>
            </div>
          </div>
        </div>

        {/* Right Side on Desktop / First on Mobile: Sign-In Form */}
        <div className="order-1 lg:order-2 lg:col-span-7 min-w-0 p-4 sm:p-8 lg:p-10 bg-white flex flex-col justify-center">
          <div className="max-w-md mx-auto w-full min-w-0">
            <div className="flex lg:hidden items-center gap-3 pb-5 mb-5 border-b border-slate-100">
              <div className="w-10 h-10 rounded-xl bg-blue-600 flex items-center justify-center text-white shrink-0">
                <ClipboardList className="w-5 h-5" />
              </div>
              <div>
                <h1 className="text-lg font-bold tracking-tight text-slate-900">TaskFlow Pro</h1>
                <p className="text-[11px] text-slate-500 font-medium">Enterprise Task Management</p>
              </div>
            </div>

            <div className="mb-6">
              <h2 className="text-xl sm:text-2xl font-bold text-slate-900 tracking-tight">Sign in to your account</h2>
              <p className="text-xs text-slate-500 mt-1">
                Enter your credentials or select a demo account below
              </p>
            </div>

            {error && (
              <div className="mb-5 p-3 rounded-xl bg-rose-50 border border-rose-200 text-rose-700 text-xs font-medium flex items-center gap-2">
                <span className="w-1.5 h-1.5 rounded-full bg-rose-500" />
                <span>{error}</span>
              </div>
            )}

            <form onSubmit={handleSubmit} className="space-y-4">
              <div>
                <label className="block text-xs font-semibold text-slate-700 mb-1">
                  Username or Email
                </label>
                <div className="relative">
                  <div className="absolute inset-y-0 left-0 pl-3.5 flex items-center pointer-events-none text-slate-400">
                    <UserIcon className="w-4 h-4" />
                  </div>
                  <input
                    type="text"
                    value={usernameOrEmail}
                    onChange={(e) => setUsernameOrEmail(e.target.value)}
                    placeholder="admin or user@taskflow.com"
                    required
                    className="w-full pl-10 pr-4 py-2.5 rounded-xl border border-slate-300 text-sm sm:text-xs focus:ring-2 focus:ring-blue-500/20 focus:border-blue-600 focus:outline-hidden bg-slate-50/50 focus:bg-white transition"
                  />
                </div>
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-700 mb-1">
                  Password
                </label>
                <div className="relative">
                  <div className="absolute inset-y-0 left-0 pl-3.5 flex items-center pointer-events-none text-slate-400">
                    <Lock className="w-4 h-4" />
                  </div>
                  <input
                    type="password"
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    placeholder="Enter your password"
                    required
                    className="w-full pl-10 pr-4 py-2.5 rounded-xl border border-slate-300 text-sm sm:text-xs focus:ring-2 focus:ring-blue-500/20 focus:border-blue-600 focus:outline-hidden bg-slate-50/50 focus:bg-white transition"
                  />
                </div>
              </div>

              <button
                type="submit"
                disabled={isLoading}
                className="w-full mt-2 py-3 px-4 rounded-xl bg-blue-600 hover:bg-blue-700 text-white font-semibold text-sm sm:text-xs tracking-wide disabled:opacity-50 transition flex items-center justify-center gap-2"
              >
                {isLoading ? (
                  <span className="flex items-center gap-2">
                    <span className="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin" />
                    Signing in...
                  </span>
                ) : (
                  <>
                    <span>Sign In</span>
                    <ArrowRight className="w-4 h-4" />
                  </>
                )}
              </button>
            </form>

            <div className="mt-6 pt-6 border-t border-slate-100 text-center">
              <span className="text-[11px] text-slate-400">
                Default Credentials: Admin@123 | Employee@123
              </span>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
