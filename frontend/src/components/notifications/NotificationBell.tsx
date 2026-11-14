import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Bell, CheckCheck, Settings, X, ArrowLeft, Inbox } from 'lucide-react';
import {
  useMarkAllNotificationsReadMutation,
  useMarkNotificationReadMutation,
  useNotificationPreferencesQuery,
  useNotificationsQuery,
  useUnreadCountQuery,
  useUpdateNotificationPreferenceMutation,
} from '../../hooks/useQueries';
import { NotificationItem } from '../../types/enterprise';
import { toast } from '../../store/useToastStore';

const CATEGORIES = [
  { key: 'Task', label: 'Task assignments & updates', desc: 'When tasks assigned to you are created, updated, or reassigned' },
  { key: 'Comment', label: 'Task comments', desc: 'When a teammate comments on your tasks' },
  { key: 'Admin', label: 'Administrative events', desc: 'User account creations and status changes (Admins)' },
];

const formatRelative = (iso: string) => {
  const diffSec = Math.floor((Date.now() - new Date(iso).getTime()) / 1000);
  if (diffSec < 60) return 'Just now';
  const mins = Math.floor(diffSec / 60);
  if (mins < 60) return `${mins}m ago`;
  const hrs = Math.floor(mins / 60);
  if (hrs < 24) return `${hrs}h ago`;
  return new Date(iso).toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
};

export const NotificationBell: React.FC = () => {
  const navigate = useNavigate();
  const [isOpen, setIsOpen] = useState(false);
  const [unreadOnly, setUnreadOnly] = useState(false);
  const [showPrefs, setShowPrefs] = useState(false);

  const { data: unreadCount = 0 } = useUnreadCountQuery(true);
  const { data: listData, isLoading } = useNotificationsQuery(
    { unreadOnly, pageNumber: 1, pageSize: 20 },
    isOpen && !showPrefs
  );
  const { data: prefs = [] } = useNotificationPreferencesQuery(isOpen && showPrefs);

  const markRead = useMarkNotificationReadMutation();
  const markAllRead = useMarkAllNotificationsReadMutation();
  const setPref = useUpdateNotificationPreferenceMutation();

  useEffect(() => {
    const onKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && isOpen) setIsOpen(false);
    };
    window.addEventListener('keydown', onKeyDown);
    return () => window.removeEventListener('keydown', onKeyDown);
  }, [isOpen]);

  const handleOpenNotification = (n: NotificationItem) => {
    if (!n.isRead) {
      markRead.mutate(n.id);
    }
    setIsOpen(false);
    if (n.link) {
      navigate(n.link);
    }
  };

  const isCategoryEnabled = (cat: string) => {
    const found = prefs.find((p) => p.category === cat);
    return found ? found.inAppEnabled : true;
  };

  const handleTogglePref = async (cat: string, nextVal: boolean) => {
    try {
      await setPref.mutateAsync({ category: cat, inAppEnabled: nextVal });
      toast.success(`${cat} notifications ${nextVal ? 'enabled' : 'muted'}`);
    } catch {
      toast.error('Failed to update notification preference');
    }
  };

  const notifications = listData?.items ?? [];

  return (
    <>
      <button
        type="button"
        onClick={() => setIsOpen(true)}
        aria-label="Open notifications"
        className="relative p-2 rounded-xl text-slate-500 hover:text-slate-900 hover:bg-slate-100 border border-transparent hover:border-slate-200 transition"
      >
        <Bell className="w-5 h-5" />
        {unreadCount > 0 && (
          <span className="absolute -top-0.5 -right-0.5 min-w-[18px] h-[18px] px-1 rounded-full bg-rose-600 text-white text-[10px] font-bold flex items-center justify-center shadow-xs">
            {unreadCount > 99 ? '99+' : unreadCount}
          </span>
        )}
      </button>

      {isOpen && (
        <div
          onClick={() => setIsOpen(false)}
          className="fixed inset-0 z-50 flex justify-end bg-slate-900/40 backdrop-blur-[1px]"
        >
          <section
            onClick={(e) => e.stopPropagation()}
            className="w-full sm:max-w-md bg-white h-full shadow-2xl border-l border-slate-200 flex flex-col"
            aria-label="Notification Center"
          >
            {/* Header */}
            <div className="px-4 sm:px-5 py-4 border-b border-slate-200 bg-slate-50 flex items-center justify-between gap-2">
              <div className="flex items-center gap-2">
                {showPrefs && (
                  <button
                    type="button"
                    onClick={() => setShowPrefs(false)}
                    className="p-1 rounded-lg text-slate-500 hover:bg-slate-200 transition"
                    aria-label="Back to notifications"
                  >
                    <ArrowLeft className="w-4 h-4" />
                  </button>
                )}
                <h3 className="text-sm font-bold text-slate-900">
                  {showPrefs ? 'Notification Preferences' : 'Notifications'}
                </h3>
                {!showPrefs && unreadCount > 0 && (
                  <span className="px-2 py-0.5 rounded-full text-[11px] font-semibold bg-rose-50 text-rose-700 border border-rose-200">
                    {unreadCount} unread
                  </span>
                )}
              </div>

              <div className="flex items-center gap-1">
                {!showPrefs && (
                  <button
                    type="button"
                    onClick={() => setShowPrefs(true)}
                    title="Notification Preferences"
                    className="p-1.5 rounded-lg text-slate-500 hover:text-slate-900 hover:bg-slate-200/70 transition"
                  >
                    <Settings className="w-4 h-4" />
                  </button>
                )}
                <button
                  type="button"
                  onClick={() => setIsOpen(false)}
                  aria-label="Close notifications"
                  className="p-1.5 rounded-lg text-slate-400 hover:text-slate-700 hover:bg-slate-200/70 transition"
                >
                  <X className="w-4 h-4" />
                </button>
              </div>
            </div>

            {showPrefs ? (
              <div className="flex-1 overflow-y-auto p-4 sm:p-5 space-y-3">
                <p className="text-xs text-slate-500 mb-3">
                  Choose which notification categories appear in your in-app inbox.
                </p>
                {CATEGORIES.map((cat) => {
                  const enabled = isCategoryEnabled(cat.key);
                  return (
                    <label
                      key={cat.key}
                      className="flex items-start justify-between gap-3 p-3.5 rounded-xl border border-slate-200 bg-white hover:bg-slate-50/70 cursor-pointer transition"
                    >
                      <div>
                        <div className="text-xs font-semibold text-slate-900">{cat.label}</div>
                        <div className="text-[11px] text-slate-500 mt-0.5">{cat.desc}</div>
                      </div>
                      <input
                        type="checkbox"
                        checked={enabled}
                        onChange={(e) => handleTogglePref(cat.key, e.target.checked)}
                        className="mt-0.5 h-4 w-4 rounded border-slate-300 text-blue-600 focus:ring-blue-500"
                      />
                    </label>
                  );
                })}
              </div>
            ) : (
              <>
                {/* Sub-header controls */}
                <div className="px-4 sm:px-5 py-2.5 border-b border-slate-100 flex items-center justify-between gap-2 bg-white">
                  <div className="inline-flex rounded-lg bg-slate-100 p-0.5">
                    <button
                      type="button"
                      onClick={() => setUnreadOnly(false)}
                      className={`px-3 py-1 rounded-md text-xs font-medium transition ${
                        !unreadOnly ? 'bg-white text-slate-900 shadow-xs' : 'text-slate-600'
                      }`}
                    >
                      All
                    </button>
                    <button
                      type="button"
                      onClick={() => setUnreadOnly(true)}
                      className={`px-3 py-1 rounded-md text-xs font-medium transition ${
                        unreadOnly ? 'bg-white text-slate-900 shadow-xs' : 'text-slate-600'
                      }`}
                    >
                      Unread
                    </button>
                  </div>

                  {unreadCount > 0 && (
                    <button
                      type="button"
                      onClick={() => markAllRead.mutate()}
                      disabled={markAllRead.isPending}
                      className="inline-flex items-center gap-1 text-xs font-semibold text-blue-600 hover:text-blue-800 disabled:opacity-50 transition"
                    >
                      <CheckCheck className="w-3.5 h-3.5" />
                      <span>Mark all read</span>
                    </button>
                  )}
                </div>

                {/* Notifications List */}
                <div className="flex-1 overflow-y-auto divide-y divide-slate-100">
                  {isLoading ? (
                    <div className="py-12 flex flex-col items-center gap-2 text-xs text-slate-400">
                      <div className="w-6 h-6 border-2 border-blue-600 border-t-transparent rounded-full animate-spin" />
                      <span>Loading notifications...</span>
                    </div>
                  ) : notifications.length === 0 ? (
                    <div className="py-16 px-6 text-center text-slate-400">
                      <Inbox className="w-8 h-8 mx-auto mb-2 opacity-60" />
                      <p className="text-xs font-medium text-slate-500">
                        {unreadOnly ? 'No unread notifications' : 'You are all caught up'}
                      </p>
                    </div>
                  ) : (
                    notifications.map((n) => (
                      <button
                        key={n.id}
                        type="button"
                        onClick={() => handleOpenNotification(n)}
                        className={`w-full text-left px-4 sm:px-5 py-3.5 hover:bg-slate-50 transition flex items-start gap-3 ${
                          !n.isRead ? 'bg-blue-50/40' : 'bg-white'
                        }`}
                      >
                        <span
                          className={`mt-1.5 w-2 h-2 rounded-full shrink-0 ${
                            !n.isRead ? 'bg-blue-600' : 'bg-transparent'
                          }`}
                        />
                        <div className="min-w-0 flex-1">
                          <div className="flex items-center justify-between gap-2">
                            <span className="text-xs font-semibold text-slate-900 truncate">{n.title}</span>
                            <span className="text-[10px] text-slate-400 shrink-0">{formatRelative(n.createdUtc)}</span>
                          </div>
                          <p className="text-xs text-slate-600 mt-0.5 leading-relaxed line-clamp-2">
                            {n.message}
                          </p>
                          <div className="flex items-center gap-2 mt-1.5 text-[10px] text-slate-400">
                            <span className="px-1.5 py-0.5 rounded bg-slate-100 text-slate-600 font-medium">
                              {n.category}
                            </span>
                            {n.actorName && <span>by {n.actorName}</span>}
                          </div>
                        </div>
                      </button>
                    ))
                  )}
                </div>
              </>
            )}
          </section>
        </div>
      )}
    </>
  );
};
