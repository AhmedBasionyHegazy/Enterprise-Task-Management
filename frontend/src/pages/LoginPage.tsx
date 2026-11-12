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
  Sparkles,
  Zap,
  CheckCircle2
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
      toast.success('Welcome back to TaskFlow Pro!');
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
    <div className="min-h-screen bg-gradient-to-br from-slate-900 via-indigo-950 to-slate-900 flex items-center justify-center p-3 sm:p-6 lg:p-8 font-sans overflow-x-hidden relative">
      {/* Background glowing orbs */}
      <div className="absolute top-1/4 left-1/4 w-64 sm:w-96 h-64 sm:h-96 bg-indigo-600/20 rounded-full blur-3xl pointer-events-none" />
      <div className="absolute bottom-1/4 right-1/4 w-64 sm:w-96 h-64 sm:h-96 bg-violet-600/20 rounded-full blur-3xl pointer-events-none" />

      <div className="relative w-full max-w-4xl grid grid-cols-1 lg:grid-cols-12 bg-white/10 backdrop-blur-xl border border-white/15 rounded-2xl sm:rounded-3xl shadow-2xl overflow-hidden">
        {/* Left Side: Brand & Quick Demo Roles */}
        <div className="lg:col-span-5 p-5 sm:p-8 lg:p-10 bg-gradient-to-br from-indigo-900/60 to-slate-900/80 border-b lg:border-b-0 lg:border-r border-white/10 flex flex-col justify-between text-white">
          <div>
            <div className="flex items-center gap-3 mb-6">
              <div className="w-11 h-11 rounded-2xl bg-indigo-600 flex items-center justify-center text-white shadow-glow-primary">
                <ClipboardList className="w-6 h-6" />
              </div>
              <div>
                <h1 className="text-xl font-bold tracking-tight">TaskFlow Pro</h1>
                <p className="text-xs text-indigo-300 font-medium">Enterprise Task Management</p>
              </div>
            </div>

            <p className="text-sm text-slate-300 leading-relaxed mb-6">
              Production-ready task tracking, role-based workflows, dynamic analytics, and collaborative team communication.
            </p>

            <div className="space-y-3 mb-8">
              <div className="flex items-center gap-2.5 text-xs text-slate-300">
                <CheckCircle2 className="w-4 h-4 text-emerald-400 shrink-0" />
                <span>JWT Authentication & Dual-Role RBAC</span>
              </div>
              <div className="flex items-center gap-2.5 text-xs text-slate-300">
                <CheckCircle2 className="w-4 h-4 text-emerald-400 shrink-0" />
                <span>Dynamic Repository & Unit of Work (.NET 10)</span>
              </div>
              <div className="flex items-center gap-2.5 text-xs text-slate-300">
                <CheckCircle2 className="w-4 h-4 text-emerald-400 shrink-0" />
                <span>React 19 & Zustand State Engine</span>
              </div>
            </div>
          </div>

          {/* Quick Demo Logins Box */}
          <div className="p-4 rounded-2xl bg-white/5 border border-white/10 backdrop-blur-xs">
            <div className="flex items-center gap-2 text-xs font-semibold text-indigo-200 mb-3">
              <Zap className="w-4 h-4 text-amber-400" />
              <span>1-Click Evaluator Fast Login</span>
            </div>
            <div className="grid grid-cols-1 gap-2">
              <button
                type="button"
                onClick={() => handleQuickLogin('admin', 'Admin@123', 'System Administrator (Admin)')}
                disabled={isLoading}
                className="w-full py-2 px-3 rounded-xl bg-indigo-600/40 hover:bg-indigo-600/70 border border-indigo-400/30 text-left text-xs font-medium text-white transition flex items-center justify-between group"
              >
                <span>👑 Admin (Sarah Jenkins)</span>
                <span className="text-[10px] text-indigo-200 group-hover:translate-x-0.5 transition">Click to log in &rarr;</span>
              </button>

              <button
                type="button"
                onClick={() => handleQuickLogin('john.doe', 'Employee@123', 'John Doe (Employee)')}
                disabled={isLoading}
                className="w-full py-2 px-3 rounded-xl bg-emerald-600/30 hover:bg-emerald-600/60 border border-emerald-400/30 text-left text-xs font-medium text-white transition flex items-center justify-between group"
              >
                <span>💼 Employee (John Doe - Dev)</span>
                <span className="text-[10px] text-emerald-200 group-hover:translate-x-0.5 transition">Click to log in &rarr;</span>
              </button>

              <button
                type="button"
                onClick={() => handleQuickLogin('jane.smith', 'Employee@123', 'Jane Smith (Employee)')}
                disabled={isLoading}
                className="w-full py-2 px-3 rounded-xl bg-purple-600/30 hover:bg-purple-600/60 border border-purple-400/30 text-left text-xs font-medium text-white transition flex items-center justify-between group"
              >
                <span>🧪 Employee (Jane Smith - QA)</span>
                <span className="text-[10px] text-purple-200 group-hover:translate-x-0.5 transition">Click to log in &rarr;</span>
              </button>
            </div>
          </div>
        </div>

        {/* Right Side: Sign-In Form */}
        <div className="lg:col-span-7 p-5 sm:p-8 lg:p-10 bg-white flex flex-col justify-center">
          <div className="max-w-md mx-auto w-full">
            <div className="mb-6">
              <h2 className="text-2xl font-bold text-slate-900 tracking-tight">Sign in to your account</h2>
              <p className="text-xs text-slate-500 mt-1">
                Enter your credentials or use the demo buttons on the left
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
                    className="w-full pl-10 pr-4 py-2.5 rounded-xl border border-slate-300 text-xs focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 focus:outline-hidden bg-slate-50/50 focus:bg-white transition"
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
                    className="w-full pl-10 pr-4 py-2.5 rounded-xl border border-slate-300 text-xs focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 focus:outline-hidden bg-slate-50/50 focus:bg-white transition"
                  />
                </div>
              </div>

              <button
                type="submit"
                disabled={isLoading}
                className="w-full mt-2 py-3 px-4 rounded-xl bg-indigo-600 hover:bg-indigo-700 text-white font-semibold text-xs tracking-wide shadow-md shadow-indigo-600/20 disabled:opacity-50 transition flex items-center justify-center gap-2"
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
