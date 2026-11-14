import React from 'react';
import { Monitor, Smartphone, Globe, Trash2, LogOut } from 'lucide-react';
import { Modal } from '../common/Modal';
import { useRevokeSessionMutation, useSessionsQuery } from '../../hooks/useQueries';
import { useAuthStore } from '../../store/useAuthStore';
import { toast } from '../../store/useToastStore';

interface SessionsModalProps {
  isOpen: boolean;
  onClose: () => void;
}

export const SessionsModal: React.FC<SessionsModalProps> = ({ isOpen, onClose }) => {
  const { logoutAll } = useAuthStore();
  const { data: sessions = [], isLoading } = useSessionsQuery(isOpen);
  const revokeMutation = useRevokeSessionMutation();

  const handleRevoke = async (id: string, isCurrent: boolean) => {
    try {
      await revokeMutation.mutateAsync(id);
      toast.success('Session revoked');
      if (isCurrent) {
        await useAuthStore.getState().logout();
        onClose();
      }
    } catch {
      toast.error('Failed to revoke session');
    }
  };

  const handleLogoutAll = async () => {
    await logoutAll();
    onClose();
    toast.success('Signed out from all active devices');
  };

  const formatDate = (iso?: string | null) => {
    if (!iso) return '—';
    return new Date(iso).toLocaleString('en-US', {
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title="Active Sessions & Security"
      subtitle="Manage signed-in devices holding refresh token sessions"
      maxWidth="lg"
    >
      <div className="space-y-4">
        {isLoading ? (
          <div className="py-8 flex items-center justify-center gap-2 text-xs text-slate-500">
            <div className="w-5 h-5 border-2 border-blue-600 border-t-transparent rounded-full animate-spin" />
            <span>Loading active sessions...</span>
          </div>
        ) : sessions.length === 0 ? (
          <div className="py-8 text-center text-xs text-slate-400">
            No active refresh sessions found.
          </div>
        ) : (
          <div className="space-y-2.5">
            {sessions.map((s) => {
              const isMobile =
                (s.deviceName || '').includes('Android') || (s.deviceName || '').includes('iOS');
              return (
                <div
                  key={s.id}
                  className={`p-3.5 rounded-xl border flex flex-col sm:flex-row sm:items-center justify-between gap-3 ${
                    s.isCurrent ? 'border-blue-200 bg-blue-50/40' : 'border-slate-200 bg-white'
                  }`}
                >
                  <div className="flex items-start gap-3 min-w-0">
                    <div className="p-2 rounded-lg bg-slate-100 text-slate-600 shrink-0">
                      {isMobile ? <Smartphone className="w-4 h-4" /> : <Monitor className="w-4 h-4" />}
                    </div>
                    <div className="min-w-0">
                      <div className="flex flex-wrap items-center gap-2">
                        <span className="text-xs font-semibold text-slate-900">
                          {s.deviceName || 'Browser session'}
                        </span>
                        {s.isCurrent && (
                          <span className="px-2 py-0.5 rounded-full text-[10px] font-semibold bg-emerald-50 text-emerald-700 border border-emerald-200">
                            Current device
                          </span>
                        )}
                      </div>
                      <div className="flex flex-wrap items-center gap-x-3 gap-y-1 text-[11px] text-slate-500 mt-1">
                        {s.ipAddress && (
                          <span className="inline-flex items-center gap-1">
                            <Globe className="w-3 h-3" />
                            {s.ipAddress}
                          </span>
                        )}
                        <span>Started: {formatDate(s.sessionStartedUtc)}</span>
                        <span>Last active: {formatDate(s.lastUsedUtc || s.sessionStartedUtc)}</span>
                      </div>
                    </div>
                  </div>

                  <button
                    type="button"
                    onClick={() => handleRevoke(s.id, s.isCurrent)}
                    disabled={revokeMutation.isPending}
                    className="inline-flex items-center justify-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-medium text-rose-600 hover:bg-rose-50 border border-rose-200 transition shrink-0"
                  >
                    <Trash2 className="w-3.5 h-3.5" />
                    <span>{s.isCurrent ? 'Sign out' : 'Revoke'}</span>
                  </button>
                </div>
              );
            })}
          </div>
        )}

        <div className="pt-3 border-t border-slate-100 flex flex-col sm:flex-row sm:items-center justify-between gap-2">
          <span className="text-[11px] text-slate-500">
            Tokens automatically rotate and revoke on reuse detection.
          </span>
          <button
            type="button"
            onClick={handleLogoutAll}
            className="inline-flex items-center justify-center gap-1.5 px-3.5 py-2 rounded-xl text-xs font-semibold text-white bg-rose-600 hover:bg-rose-700 transition"
          >
            <LogOut className="w-3.5 h-3.5" />
            <span>Sign out all devices</span>
          </button>
        </div>
      </div>
    </Modal>
  );
};
