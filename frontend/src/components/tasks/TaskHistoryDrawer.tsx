import React, { useEffect, useState } from 'react';
import {
  X,
  History,
  PlusCircle,
  Edit3,
  RefreshCw,
  UserCheck,
  MessageSquare,
  Trash2,
  ShieldAlert,
  ChevronDown,
  ChevronRight,
  Clock,
  ChevronLeft,
} from 'lucide-react';
import { useTaskHistoryQuery } from '../../hooks/useQueries';
import { AuditChange, AuditLogEntry } from '../../types/enterprise';
import { TaskItem } from '../../types';

interface TaskHistoryDrawerProps {
  task: Pick<TaskItem, 'id' | 'title'> | null;
  isOpen: boolean;
  onClose: () => void;
}

const ACTION_OPTIONS = [
  { value: '', label: 'All Actions' },
  { value: 'Create', label: 'Created' },
  { value: 'Update', label: 'Updated' },
  { value: 'StatusChange', label: 'Status Change' },
  { value: 'Assign', label: 'Reassigned' },
  { value: 'Comment', label: 'Commented' },
  { value: 'Delete', label: 'Deleted' },
  { value: 'AccessDenied', label: 'Access Denied' },
];

const FIELD_LABELS: Record<string, string> = {
  Title: 'Title',
  Description: 'Description',
  Priority: 'Priority',
  Status: 'Status',
  StartDate: 'Start Date',
  DueDate: 'Due Date',
  AssignedToUserId: 'Assignee',
  CreatedByUserId: 'Created By',
  Comment: 'Comment',
};

const formatTimestamp = (iso: string) => {
  const date = new Date(iso);
  return {
    full: date.toLocaleString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
    }),
    short: date.toLocaleString('en-US', {
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    }),
  };
};

const formatChangeValue = (field: string, val?: string | null): string => {
  if (val == null || val === '') return 'Empty';
  if ((field === 'StartDate' || field === 'DueDate') && val.includes('T')) {
    const parsed = new Date(val);
    if (!Number.isNaN(parsed.getTime())) {
      return parsed.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
    }
  }
  return val;
};

const ActionIcon: React.FC<{ action: string }> = ({ action }) => {
  const base = 'w-7 h-7 rounded-full flex items-center justify-center border shrink-0';
  switch (action) {
    case 'Create':
      return (
        <span className={`${base} bg-emerald-50 border-emerald-200 text-emerald-600`}>
          <PlusCircle className="w-3.5 h-3.5" />
        </span>
      );
    case 'StatusChange':
      return (
        <span className={`${base} bg-blue-50 border-blue-200 text-blue-600`}>
          <RefreshCw className="w-3.5 h-3.5" />
        </span>
      );
    case 'Assign':
      return (
        <span className={`${base} bg-sky-50 border-sky-200 text-sky-600`}>
          <UserCheck className="w-3.5 h-3.5" />
        </span>
      );
    case 'Comment':
      return (
        <span className={`${base} bg-slate-100 border-slate-200 text-slate-600`}>
          <MessageSquare className="w-3.5 h-3.5" />
        </span>
      );
    case 'Delete':
    case 'AccessDenied':
      return (
        <span className={`${base} bg-rose-50 border-rose-200 text-rose-600`}>
          {action === 'Delete' ? <Trash2 className="w-3.5 h-3.5" /> : <ShieldAlert className="w-3.5 h-3.5" />}
        </span>
      );
    default:
      return (
        <span className={`${base} bg-amber-50 border-amber-200 text-amber-600`}>
          <Edit3 className="w-3.5 h-3.5" />
        </span>
      );
  }
};

const ActionBadgePill: React.FC<{ action: string; isSuccess: boolean }> = ({ action, isSuccess }) => {
  const styles: Record<string, string> = {
    Create: 'bg-emerald-50 text-emerald-700 border-emerald-200',
    StatusChange: 'bg-blue-50 text-blue-700 border-blue-200',
    Assign: 'bg-sky-50 text-sky-700 border-sky-200',
    Comment: 'bg-slate-100 text-slate-700 border-slate-200',
    Update: 'bg-amber-50 text-amber-700 border-amber-200',
    Delete: 'bg-rose-50 text-rose-700 border-rose-200',
    AccessDenied: 'bg-rose-50 text-rose-700 border-rose-200',
  };
  return (
    <span
      className={`inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-semibold border ${
        styles[action] || 'bg-slate-100 text-slate-700 border-slate-200'
      }`}
    >
      {action}
      {!isSuccess && <span className="text-rose-600">(Blocked)</span>}
    </span>
  );
};

export const TaskHistoryDrawer: React.FC<TaskHistoryDrawerProps> = ({ task, isOpen, onClose }) => {
  const [actionFilter, setActionFilter] = useState('');
  const [pageNumber, setPageNumber] = useState(1);
  const [expandedIds, setExpandedIds] = useState<Record<number, boolean>>({});

  useEffect(() => {
    setPageNumber(1);
    setActionFilter('');
    setExpandedIds({});
  }, [task?.id, isOpen]);

  useEffect(() => {
    const onKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && isOpen) onClose();
    };
    window.addEventListener('keydown', onKeyDown);
    return () => window.removeEventListener('keydown', onKeyDown);
  }, [isOpen, onClose]);

  const { data, isLoading, isFetching, error } = useTaskHistoryQuery(
    task?.id ?? null,
    {
      action: actionFilter || undefined,
      pageNumber,
      pageSize: 15,
    },
    isOpen && !!task
  );

  if (!isOpen || !task) return null;

  const toggleExpand = (id: number) => {
    setExpandedIds((prev) => ({ ...prev, [id]: !prev[id] }));
  };

  const renderDiff = (change: AuditChange, idx: number) => {
    const label = FIELD_LABELS[change.field] || change.field;
    const oldVal = formatChangeValue(change.field, change.oldDisplay ?? change.oldValue);
    const newVal = formatChangeValue(change.field, change.newDisplay ?? change.newValue);

    return (
      <div key={`${change.field}-${idx}`} className="text-[11px] bg-white rounded-lg border border-slate-200/80 p-2">
        <div className="font-semibold text-slate-600 mb-1">{label}</div>
        <div className="flex flex-wrap items-center gap-1.5 break-all">
          {change.oldValue !== null && change.oldValue !== undefined && (
            <span className="px-1.5 py-0.5 rounded bg-rose-50 text-rose-700 line-through">
              {oldVal}
            </span>
          )}
          {change.oldValue !== null && change.oldValue !== undefined && change.newValue !== null && change.newValue !== undefined && (
            <span className="text-slate-400">&rarr;</span>
          )}
          {change.newValue !== null && change.newValue !== undefined && (
            <span className="px-1.5 py-0.5 rounded bg-emerald-50 text-emerald-700 font-medium">
              {newVal}
            </span>
          )}
        </div>
      </div>
    );
  };

  const entries: AuditLogEntry[] = data?.items ?? [];

  return (
    <div
      onClick={onClose}
      className="fixed inset-0 z-50 flex justify-end bg-slate-900/50 backdrop-blur-[1px]"
    >
      <aside
        onClick={(e) => e.stopPropagation()}
        className="w-full sm:max-w-lg bg-white h-full shadow-2xl border-l border-slate-200 flex flex-col"
        aria-label="Task History Timeline"
      >
        {/* Header */}
        <div className="px-4 sm:px-6 py-4 border-b border-slate-200 bg-slate-50 flex items-start justify-between gap-3">
          <div className="min-w-0">
            <div className="flex items-center gap-2 text-blue-600 text-xs font-semibold">
              <History className="w-4 h-4" />
              <span>Audit Timeline &bull; Task #{task.id}</span>
            </div>
            <h3 className="text-base font-bold text-slate-900 truncate mt-1">{task.title}</h3>
          </div>
          <button
            type="button"
            onClick={onClose}
            aria-label="Close history drawer"
            className="p-1.5 rounded-lg text-slate-400 hover:text-slate-700 hover:bg-slate-200/70 transition shrink-0"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Filter Bar */}
        <div className="px-4 sm:px-6 py-3 border-b border-slate-100 flex items-center justify-between gap-3 bg-white">
          <select
            value={actionFilter}
            onChange={(e) => {
              setActionFilter(e.target.value);
              setPageNumber(1);
            }}
            className="flex-1 text-xs rounded-lg border border-slate-200 bg-slate-50 px-3 py-2 text-slate-700 focus:outline-hidden focus:ring-1 focus:ring-blue-500"
          >
            {ACTION_OPTIONS.map((opt) => (
              <option key={opt.value || 'all'} value={opt.value}>
                {opt.label}
              </option>
            ))}
          </select>
          <span className="text-[11px] font-medium text-slate-500 shrink-0">
            {data ? `${data.totalCount} event${data.totalCount === 1 ? '' : 's'}` : ''}
          </span>
        </div>

        {/* Timeline Body */}
        <div className="flex-1 overflow-y-auto px-4 sm:px-6 py-5">
          {isLoading ? (
            <div className="py-12 flex flex-col items-center gap-2 text-xs text-slate-500">
              <div className="w-6 h-6 border-2 border-blue-600 border-t-transparent rounded-full animate-spin" />
              <span>Loading chronological history...</span>
            </div>
          ) : error ? (
            <div className="p-4 rounded-xl bg-rose-50 border border-rose-200 text-xs text-rose-700">
              Unable to load task history right now.
            </div>
          ) : entries.length === 0 ? (
            <div className="py-12 text-center text-xs text-slate-400 border border-dashed border-slate-200 rounded-xl bg-slate-50/50">
              No history entries match the selected filter.
            </div>
          ) : (
            <div className="relative space-y-5 before:content-[''] before:absolute before:left-[13px] before:top-2 before:bottom-2 before:w-px before:bg-slate-200">
              {entries.map((entry) => {
                const actorName = entry.userFullName || entry.username || 'System';
                const initials = actorName
                  .split(' ')
                  .slice(0, 2)
                  .map((p) => p[0])
                  .join('')
                  .toUpperCase();
                const ts = formatTimestamp(entry.timestampUtc);
                const hasChanges = entry.changes && entry.changes.length > 0;
                const isExpanded = !!expandedIds[entry.id];

                return (
                  <div key={entry.id} className="relative pl-10">
                    <div className="absolute left-0 top-0.5 bg-white">
                      <ActionIcon action={entry.action} />
                    </div>

                    <div className="rounded-xl border border-slate-200/90 bg-slate-50/70 p-3.5 hover:border-slate-300 transition">
                      {/* Top row */}
                      <div className="flex flex-wrap items-center justify-between gap-2 mb-1.5">
                        <div className="flex items-center gap-2 min-w-0">
                          <span className="w-5 h-5 rounded-full bg-blue-600 text-white text-[10px] font-bold flex items-center justify-center shrink-0">
                            {initials || 'S'}
                          </span>
                          <span className="text-xs font-semibold text-slate-900 truncate">{actorName}</span>
                          <ActionBadgePill action={entry.action} isSuccess={entry.isSuccess} />
                        </div>
                        <span title={ts.full} className="text-[11px] text-slate-400 flex items-center gap-1">
                          <Clock className="w-3 h-3" />
                          {ts.short}
                        </span>
                      </div>

                      {/* Description */}
                      {entry.description && (
                        <p className="text-xs text-slate-700 leading-relaxed break-words">
                          {entry.description}
                        </p>
                      )}

                      {/* Expandable Diffs */}
                      {hasChanges && (
                        <div className="mt-2.5">
                          <button
                            type="button"
                            onClick={() => toggleExpand(entry.id)}
                            className="inline-flex items-center gap-1 text-[11px] font-semibold text-blue-600 hover:text-blue-800 transition"
                          >
                            {isExpanded ? <ChevronDown className="w-3.5 h-3.5" /> : <ChevronRight className="w-3.5 h-3.5" />}
                            <span>
                              {isExpanded ? 'Hide field changes' : `View ${entry.changes.length} field change${entry.changes.length === 1 ? '' : 's'}`}
                            </span>
                          </button>

                          {isExpanded && (
                            <div className="mt-2 space-y-1.5">
                              {entry.changes.map((c, idx) => renderDiff(c, idx))}
                            </div>
                          )}
                        </div>
                      )}
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </div>

        {/* Footer Pagination */}
        {data && data.totalPages > 1 && (
          <div className="px-4 sm:px-6 py-3 border-t border-slate-200 bg-slate-50 flex items-center justify-between text-xs">
            <button
              type="button"
              onClick={() => setPageNumber((p) => Math.max(1, p - 1))}
              disabled={!data.hasPreviousPage || isFetching}
              className="inline-flex items-center gap-1 px-2.5 py-1.5 rounded-lg border border-slate-200 bg-white text-slate-700 hover:bg-slate-100 disabled:opacity-40 transition"
            >
              <ChevronLeft className="w-3.5 h-3.5" />
              <span>Previous</span>
            </button>
            <span className="text-slate-600 font-medium">
              Page {data.pageNumber} of {data.totalPages}
            </span>
            <button
              type="button"
              onClick={() => setPageNumber((p) => p + 1)}
              disabled={!data.hasNextPage || isFetching}
              className="inline-flex items-center gap-1 px-2.5 py-1.5 rounded-lg border border-slate-200 bg-white text-slate-700 hover:bg-slate-100 disabled:opacity-40 transition"
            >
              <span>Next</span>
              <ChevronRight className="w-3.5 h-3.5" />
            </button>
          </div>
        )}
      </aside>
    </div>
  );
};
