import React, { useState } from 'react';
import {
  Shield,
  FileText,
  RotateCcw,
  ChevronDown,
  ChevronRight,
  Search,
  CheckCircle2,
  XCircle,
  Link2,
} from 'lucide-react';
import {
  useAuditActionsQuery,
  useAuditLogsQuery,
  useSystemLogsQuery,
} from '../hooks/useQueries';
import { AuditQueryParams, SystemLogQueryParams } from '../types/enterprise';
import { Pagination } from '../components/common/Pagination';

type TabKey = 'audit' | 'logs';

export const AuditPage: React.FC = () => {
  const [activeTab, setActiveTab] = useState<TabKey>('audit');

  // Audit filters
  const [auditQuery, setAuditQuery] = useState<AuditQueryParams>({
    username: '',
    action: '',
    entityName: '',
    success: undefined,
    from: '',
    to: '',
    pageNumber: 1,
    pageSize: 20,
  });
  const [expandedAuditIds, setExpandedAuditIds] = useState<Record<number, boolean>>({});

  // System Logs filters
  const [logQuery, setLogQuery] = useState<SystemLogQueryParams>({
    level: '',
    userId: undefined,
    taskId: undefined,
    action: '',
    correlationId: '',
    search: '',
    fromUtc: '',
    toUtc: '',
    pageNumber: 1,
    pageSize: 25,
  });

  const cleanAuditParams: AuditQueryParams = {
    ...auditQuery,
    username: auditQuery.username?.trim() || undefined,
    action: auditQuery.action || undefined,
    entityName: auditQuery.entityName || undefined,
    from: auditQuery.from || undefined,
    to: auditQuery.to || undefined,
  };

  const cleanLogParams: SystemLogQueryParams = {
    ...logQuery,
    level: logQuery.level || undefined,
    action: logQuery.action?.trim() || undefined,
    correlationId: logQuery.correlationId?.trim() || undefined,
    search: logQuery.search?.trim() || undefined,
    fromUtc: logQuery.fromUtc || undefined,
    toUtc: logQuery.toUtc || undefined,
  };

  const { data: actions = [] } = useAuditActionsQuery(activeTab === 'audit');
  const { data: auditData, isLoading: auditLoading } = useAuditLogsQuery(
    cleanAuditParams,
    activeTab === 'audit'
  );
  const { data: logsData, isLoading: logsLoading } = useSystemLogsQuery(
    cleanLogParams,
    activeTab === 'logs'
  );

  const toggleAuditExpand = (id: number) => {
    setExpandedAuditIds((prev) => ({ ...prev, [id]: !prev[id] }));
  };

  const inspectCorrelationInLogs = (correlationId: string) => {
    setLogQuery((prev) => ({
      ...prev,
      correlationId,
      pageNumber: 1,
    }));
    setActiveTab('logs');
  };

  const formatTs = (iso: string) =>
    new Date(iso).toLocaleString('en-US', {
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
    });

  const resetAuditFilters = () =>
    setAuditQuery({
      username: '',
      action: '',
      entityName: '',
      success: undefined,
      from: '',
      to: '',
      pageNumber: 1,
      pageSize: 20,
    });

  const resetLogFilters = () =>
    setLogQuery({
      level: '',
      userId: undefined,
      taskId: undefined,
      action: '',
      correlationId: '',
      search: '',
      fromUtc: '',
      toUtc: '',
      pageNumber: 1,
      pageSize: 25,
    });

  const auditItems = auditData?.items ?? [];
  const logItems = logsData?.items ?? [];

  return (
    <div className="w-full max-w-full min-w-0 space-y-4 sm:space-y-6 animate-in fade-in duration-200">
      {/* Header & Tabs */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 min-w-0">
        <div className="min-w-0">
          <h1 className="text-lg sm:text-2xl font-bold tracking-tight text-slate-900">
            Audit Trail & System Telemetry
          </h1>
          <p className="text-xs text-slate-500 mt-1">
            Immutable entity audit log and correlated application request/diagnostic logs.
          </p>
        </div>

        <div className="grid grid-cols-2 w-full sm:w-auto sm:inline-flex rounded-xl bg-slate-200/70 p-1">
          <button
            type="button"
            onClick={() => setActiveTab('audit')}
            className={`inline-flex items-center justify-center gap-1.5 px-3 sm:px-3.5 py-2 rounded-lg text-xs font-semibold transition ${
              activeTab === 'audit'
                ? 'bg-white text-slate-900 shadow-xs'
                : 'text-slate-600 hover:text-slate-900'
            }`}
          >
            <Shield className="w-3.5 h-3.5 text-blue-600 shrink-0" />
            <span className="truncate">Audit Trail</span>
          </button>
          <button
            type="button"
            onClick={() => setActiveTab('logs')}
            className={`inline-flex items-center justify-center gap-1.5 px-3 sm:px-3.5 py-2 rounded-lg text-xs font-semibold transition ${
              activeTab === 'logs'
                ? 'bg-white text-slate-900 shadow-xs'
                : 'text-slate-600 hover:text-slate-900'
            }`}
          >
            <FileText className="w-3.5 h-3.5 text-blue-600 shrink-0" />
            <span className="truncate">Application Logs</span>
          </button>
        </div>
      </div>

      {activeTab === 'audit' ? (
        <>
          {/* Audit Filter Bar */}
          <div className="p-3.5 sm:p-4 bg-white rounded-2xl border border-slate-200 shadow-xs w-full max-w-full min-w-0">
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-12 gap-2.5 min-w-0">
              <div className="lg:col-span-3 min-w-0 relative">
                <Search className="w-3.5 h-3.5 absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
                <input
                  type="text"
                  value={auditQuery.username || ''}
                  onChange={(e) =>
                    setAuditQuery((p) => ({ ...p, username: e.target.value, pageNumber: 1 }))
                  }
                  placeholder="Filter by username..."
                  className="w-full min-w-0 max-w-full pl-8 pr-3 py-2 text-xs rounded-xl border border-slate-200 bg-slate-50 focus:bg-white focus:outline-hidden focus:ring-1 focus:ring-blue-500"
                />
              </div>

              <div className="lg:col-span-2 min-w-0">
                <select
                  value={auditQuery.action || ''}
                  onChange={(e) =>
                    setAuditQuery((p) => ({ ...p, action: e.target.value, pageNumber: 1 }))
                  }
                  className="w-full min-w-0 max-w-full px-3 py-2 text-xs rounded-xl border border-slate-200 bg-slate-50 text-slate-700 focus:outline-hidden focus:ring-1 focus:ring-blue-500"
                >
                  <option value="">All Actions</option>
                  {actions.map((a) => (
                    <option key={a} value={a}>
                      {a}
                    </option>
                  ))}
                </select>
              </div>

              <div className="lg:col-span-2 min-w-0">
                <select
                  value={auditQuery.entityName || ''}
                  onChange={(e) =>
                    setAuditQuery((p) => ({ ...p, entityName: e.target.value, pageNumber: 1 }))
                  }
                  className="w-full min-w-0 max-w-full px-3 py-2 text-xs rounded-xl border border-slate-200 bg-slate-50 text-slate-700 focus:outline-hidden focus:ring-1 focus:ring-blue-500"
                >
                  <option value="">All Entities</option>
                  <option value="Task">Task</option>
                  <option value="TaskComment">TaskComment</option>
                  <option value="User">User</option>
                  <option value="Auth">Auth</option>
                </select>
              </div>

              <div className="lg:col-span-2 min-w-0">
                <select
                  value={
                    auditQuery.success === undefined ? '' : auditQuery.success ? 'true' : 'false'
                  }
                  onChange={(e) =>
                    setAuditQuery((p) => ({
                      ...p,
                      success: e.target.value === '' ? undefined : e.target.value === 'true',
                      pageNumber: 1,
                    }))
                  }
                  className="w-full min-w-0 max-w-full px-3 py-2 text-xs rounded-xl border border-slate-200 bg-slate-50 text-slate-700 focus:outline-hidden focus:ring-1 focus:ring-blue-500"
                >
                  <option value="">All Outcomes</option>
                  <option value="true">Succeeded</option>
                  <option value="false">Denied / Failed</option>
                </select>
              </div>

              <div className="lg:col-span-2 min-w-0">
                <input
                  type="date"
                  value={auditQuery.from || ''}
                  onChange={(e) =>
                    setAuditQuery((p) => ({ ...p, from: e.target.value, pageNumber: 1 }))
                  }
                  aria-label="From date"
                  className="w-full min-w-0 max-w-full px-3 py-2 text-xs rounded-xl border border-slate-200 bg-slate-50 text-slate-700 focus:outline-hidden focus:ring-1 focus:ring-blue-500"
                />
              </div>

              <div className="lg:col-span-1 flex justify-end min-w-0">
                <button
                  type="button"
                  onClick={resetAuditFilters}
                  title="Reset Filters"
                  className="w-full lg:w-auto inline-flex items-center justify-center gap-1.5 px-3 py-2 rounded-xl border border-slate-200 text-xs text-slate-600 hover:bg-slate-100 transition"
                >
                  <RotateCcw className="w-3.5 h-3.5" />
                  <span className="lg:hidden">Reset</span>
                </button>
              </div>
            </div>
          </div>

          {/* Audit Results */}
          <div className="bg-white rounded-2xl border border-slate-200 shadow-xs overflow-hidden">
            {auditLoading ? (
              <div className="py-12 flex items-center justify-center gap-2 text-xs text-slate-400">
                <span className="w-4 h-4 border-2 border-blue-600 border-t-transparent rounded-full animate-spin" />
                <span>Loading audit events...</span>
              </div>
            ) : auditItems.length === 0 ? (
              <div className="py-12 text-center text-xs text-slate-400">
                No audit records match the active filter criteria.
              </div>
            ) : (
              <>
                {/* Mobile & Tablet cards */}
                <div className="lg:hidden divide-y divide-slate-100">
                  {auditItems.map((row) => {
                    const expanded = !!expandedAuditIds[row.id];
                    return (
                      <div key={row.id} className="p-4 space-y-2">
                        <div className="flex flex-wrap items-center justify-between gap-2">
                          <div className="flex items-center gap-1.5 flex-wrap">
                            {row.isSuccess ? (
                              <CheckCircle2 className="w-4 h-4 text-emerald-600 shrink-0" />
                            ) : (
                              <XCircle className="w-4 h-4 text-rose-600 shrink-0" />
                            )}
                            <span className="text-xs font-bold text-slate-900">{row.action}</span>
                            <span className="text-[11px] px-1.5 py-0.5 rounded bg-slate-100 text-slate-600">
                              {row.entityName}
                              {row.recordId ? ` #${row.recordId}` : ''}
                            </span>
                          </div>
                          <span className="text-[11px] text-slate-400">{formatTs(row.timestampUtc)}</span>
                        </div>
                        <div className="text-xs text-slate-700 break-words [overflow-wrap:anywhere]">
                          <span className="font-semibold">{row.userFullName || row.username || 'system'}</span>
                          {row.description && <span> &mdash; {row.description}</span>}
                        </div>
                        {(row.changes.length > 0 || row.correlationId) && (
                          <button
                            type="button"
                            onClick={() => toggleAuditExpand(row.id)}
                            className="inline-flex items-center gap-1 text-[11px] font-semibold text-blue-600"
                          >
                            {expanded ? <ChevronDown className="w-3.5 h-3.5" /> : <ChevronRight className="w-3.5 h-3.5" />}
                            <span>{expanded ? 'Hide details' : 'Inspect diffs & trace'}</span>
                          </button>
                        )}
                        {expanded && (
                          <div className="p-2.5 rounded-xl bg-slate-50 border border-slate-200 space-y-2 text-[11px] break-all">
                            {row.changes.map((c, idx) => (
                              <div key={idx}>
                                <span className="font-semibold text-slate-700">{c.field}:</span>{' '}
                                <span className="text-rose-700 line-through">{c.oldDisplay ?? c.oldValue ?? 'Empty'}</span>{' '}
                                &rarr;{' '}
                                <span className="text-emerald-700 font-medium">{c.newDisplay ?? c.newValue ?? 'Empty'}</span>
                              </div>
                            ))}
                            {row.ipAddress && <div className="text-slate-500">IP: {row.ipAddress}</div>}
                            {row.correlationId && (
                              <button
                                type="button"
                                onClick={() => inspectCorrelationInLogs(row.correlationId!)}
                                className="inline-flex items-center gap-1 text-blue-600 font-semibold"
                              >
                                <Link2 className="w-3 h-3 shrink-0" />
                                <span>Trace {row.correlationId.slice(0, 12)}...</span>
                              </button>
                            )}
                          </div>
                        )}
                      </div>
                    );
                  })}
                </div>

                {/* Desktop Table */}
                <div className="hidden lg:block overflow-x-auto">
                  <table className="w-full text-left border-collapse">
                    <thead>
                      <tr className="bg-slate-50 border-b border-slate-200 text-[11px] font-semibold text-slate-500 uppercase tracking-wider">
                        <th className="py-3 px-4">Timestamp</th>
                        <th className="py-3 px-4">Actor</th>
                        <th className="py-3 px-4">Action</th>
                        <th className="py-3 px-4">Entity</th>
                        <th className="py-3 px-4">Summary</th>
                        <th className="py-3 px-4 text-right">Details</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-100 text-xs text-slate-700">
                      {auditItems.map((row) => {
                        const expanded = !!expandedAuditIds[row.id];
                        return (
                          <React.Fragment key={row.id}>
                            <tr className="hover:bg-slate-50/70 transition">
                              <td className="py-3 px-4 whitespace-nowrap text-slate-500">
                                {formatTs(row.timestampUtc)}
                              </td>
                              <td className="py-3 px-4 font-medium text-slate-900">
                                {row.userFullName || row.username || 'system'}
                              </td>
                              <td className="py-3 px-4">
                                <span
                                  className={`inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[11px] font-semibold border ${
                                    row.isSuccess
                                      ? 'bg-emerald-50 text-emerald-700 border-emerald-200'
                                      : 'bg-rose-50 text-rose-700 border-rose-200'
                                  }`}
                                >
                                  {row.action}
                                </span>
                              </td>
                              <td className="py-3 px-4">
                                <span className="font-medium text-slate-800">{row.entityName}</span>
                                {row.recordId && (
                                  <span className="text-slate-400"> #{row.recordId}</span>
                                )}
                              </td>
                              <td className="py-3 px-4 max-w-md truncate">{row.description}</td>
                              <td className="py-3 px-4 text-right">
                                <button
                                  type="button"
                                  onClick={() => toggleAuditExpand(row.id)}
                                  className="inline-flex items-center gap-1 px-2 py-1 rounded-lg text-[11px] font-semibold text-blue-600 hover:bg-blue-50 transition"
                                >
                                  {expanded ? <ChevronDown className="w-3.5 h-3.5" /> : <ChevronRight className="w-3.5 h-3.5" />}
                                  <span>Inspect</span>
                                </button>
                              </td>
                            </tr>
                            {expanded && (
                              <tr className="bg-slate-50/80">
                                <td colSpan={6} className="px-6 py-3">
                                  <div className="flex flex-wrap items-center gap-4 text-[11px] text-slate-500 mb-2">
                                    <span>IP: <strong className="text-slate-700">{row.ipAddress || '—'}</strong></span>
                                    {row.correlationId && (
                                      <button
                                        type="button"
                                        onClick={() => inspectCorrelationInLogs(row.correlationId!)}
                                        className="inline-flex items-center gap-1 text-blue-600 hover:underline font-medium"
                                      >
                                        <Link2 className="w-3 h-3" />
                                        <span>Correlation: {row.correlationId}</span>
                                      </button>
                                    )}
                                  </div>
                                  {row.changes.length > 0 ? (
                                    <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-2">
                                      {row.changes.map((c, idx) => (
                                        <div key={idx} className="p-2 rounded-lg bg-white border border-slate-200 text-[11px]">
                                          <div className="font-semibold text-slate-700">{c.field}</div>
                                          <div className="mt-1 break-all">
                                            {c.oldValue !== null && c.oldValue !== undefined && (
                                              <span className="text-rose-700 line-through mr-1.5">
                                                {c.oldDisplay ?? c.oldValue}
                                              </span>
                                            )}
                                            {c.newValue !== null && c.newValue !== undefined && (
                                              <span className="text-emerald-700 font-medium">
                                                {c.newDisplay ?? c.newValue}
                                              </span>
                                            )}
                                          </div>
                                        </div>
                                      ))}
                                    </div>
                                  ) : (
                                    <div className="text-[11px] text-slate-400">No field-level diffs recorded for this event.</div>
                                  )}
                                </td>
                              </tr>
                            )}
                          </React.Fragment>
                        );
                      })}
                    </tbody>
                  </table>
                </div>

                {auditData && (
                  <Pagination
                    currentPage={auditData.pageNumber}
                    totalPages={auditData.totalPages}
                    totalCount={auditData.totalCount}
                    pageSize={auditData.pageSize}
                    onPageChange={(p) => setAuditQuery((prev) => ({ ...prev, pageNumber: p }))}
                    onPageSizeChange={(s) =>
                      setAuditQuery((prev) => ({ ...prev, pageSize: s, pageNumber: 1 }))
                    }
                  />
                )}
              </>
            )}
          </div>
        </>
      ) : (
        <>
          {/* Application Logs Filter Bar */}
          <div className="p-3.5 sm:p-4 bg-white rounded-2xl border border-slate-200 shadow-xs w-full max-w-full min-w-0">
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-12 gap-2.5 min-w-0">
              <div className="lg:col-span-3 min-w-0 relative">
                <Search className="w-3.5 h-3.5 absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
                <input
                  type="text"
                  value={logQuery.search || ''}
                  onChange={(e) =>
                    setLogQuery((p) => ({ ...p, search: e.target.value, pageNumber: 1 }))
                  }
                  placeholder="Search message, path, category..."
                  className="w-full min-w-0 max-w-full pl-8 pr-3 py-2 text-xs rounded-xl border border-slate-200 bg-slate-50 focus:bg-white focus:outline-hidden focus:ring-1 focus:ring-blue-500"
                />
              </div>

              <div className="lg:col-span-2 min-w-0">
                <select
                  value={logQuery.level || ''}
                  onChange={(e) =>
                    setLogQuery((p) => ({ ...p, level: e.target.value, pageNumber: 1 }))
                  }
                  className="w-full min-w-0 max-w-full px-3 py-2 text-xs rounded-xl border border-slate-200 bg-slate-50 text-slate-700 focus:outline-hidden focus:ring-1 focus:ring-blue-500"
                >
                  <option value="">All Severities</option>
                  <option value="Information">Information</option>
                  <option value="Warning">Warning</option>
                  <option value="Error">Error</option>
                  <option value="Critical">Critical</option>
                </select>
              </div>

              <div className="lg:col-span-2 min-w-0">
                <input
                  type="number"
                  value={logQuery.userId ?? ''}
                  onChange={(e) =>
                    setLogQuery((p) => ({
                      ...p,
                      userId: e.target.value ? Number(e.target.value) : undefined,
                      pageNumber: 1,
                    }))
                  }
                  placeholder="User ID"
                  className="w-full min-w-0 max-w-full px-3 py-2 text-xs rounded-xl border border-slate-200 bg-slate-50 focus:bg-white focus:outline-hidden focus:ring-1 focus:ring-blue-500"
                />
              </div>

              <div className="lg:col-span-2 min-w-0">
                <input
                  type="number"
                  value={logQuery.taskId ?? ''}
                  onChange={(e) =>
                    setLogQuery((p) => ({
                      ...p,
                      taskId: e.target.value ? Number(e.target.value) : undefined,
                      pageNumber: 1,
                    }))
                  }
                  placeholder="Task ID"
                  className="w-full min-w-0 max-w-full px-3 py-2 text-xs rounded-xl border border-slate-200 bg-slate-50 focus:bg-white focus:outline-hidden focus:ring-1 focus:ring-blue-500"
                />
              </div>

              <div className="lg:col-span-2 min-w-0">
                <input
                  type="text"
                  value={logQuery.correlationId || ''}
                  onChange={(e) =>
                    setLogQuery((p) => ({ ...p, correlationId: e.target.value, pageNumber: 1 }))
                  }
                  placeholder="Correlation ID..."
                  className="w-full min-w-0 max-w-full px-3 py-2 text-xs rounded-xl border border-slate-200 bg-slate-50 focus:bg-white focus:outline-hidden focus:ring-1 focus:ring-blue-500"
                />
              </div>

              <div className="lg:col-span-1 flex justify-end min-w-0">
                <button
                  type="button"
                  onClick={resetLogFilters}
                  title="Reset Filters"
                  className="w-full lg:w-auto inline-flex items-center justify-center gap-1.5 px-3 py-2 rounded-xl border border-slate-200 text-xs text-slate-600 hover:bg-slate-100 transition"
                >
                  <RotateCcw className="w-3.5 h-3.5" />
                  <span className="lg:hidden">Reset</span>
                </button>
              </div>
            </div>
          </div>

          {/* System Logs List */}
          <div className="bg-white rounded-2xl border border-slate-200 shadow-xs overflow-hidden">
            {logsLoading ? (
              <div className="py-12 flex items-center justify-center gap-2 text-xs text-slate-400">
                <span className="w-4 h-4 border-2 border-blue-600 border-t-transparent rounded-full animate-spin" />
                <span>Scanning structured log files...</span>
              </div>
            ) : logItems.length === 0 ? (
              <div className="py-12 text-center text-xs text-slate-400">
                No log entries match the active filters.
              </div>
            ) : (
              <>
                <div className="divide-y divide-slate-100">
                  {logItems.map((entry, idx) => {
                    const levelStyle =
                      entry.level === 'Error' || entry.level === 'Critical'
                        ? 'bg-rose-50 text-rose-700 border-rose-200'
                        : entry.level === 'Warning'
                        ? 'bg-amber-50 text-amber-700 border-amber-200'
                        : 'bg-blue-50 text-blue-700 border-blue-200';
                    return (
                      <div key={`${entry.timestampUtc}-${idx}`} className="p-3.5 sm:px-5 hover:bg-slate-50/70 transition space-y-1.5">
                        <div className="flex flex-wrap items-center justify-between gap-2">
                          <div className="flex flex-wrap items-center gap-2">
                            <span className={`px-2 py-0.5 rounded-full text-[10px] font-semibold border ${levelStyle}`}>
                              {entry.level}
                            </span>
                            <span className="text-[11px] font-mono text-slate-500">
                              {entry.category.split('.').pop()}
                            </span>
                            {entry.action && (
                              <span className="px-1.5 py-0.5 rounded bg-slate-100 text-slate-700 text-[10px] font-semibold">
                                {entry.action}
                              </span>
                            )}
                            {entry.statusCode != null && (
                              <span className="text-[11px] font-mono text-slate-600">
                                HTTP {entry.statusCode} ({entry.elapsedMs}ms)
                              </span>
                            )}
                          </div>
                          <span className="text-[11px] text-slate-400">{formatTs(entry.timestampUtc)}</span>
                        </div>

                        <p className="text-xs text-slate-800 [overflow-wrap:anywhere] font-medium">{entry.message}</p>

                        <div className="flex flex-wrap items-center gap-x-4 gap-y-1 text-[11px] text-slate-500">
                          {entry.userId && <span>User: {entry.username ? `${entry.username} (#${entry.userId})` : entry.userId}</span>}
                          {entry.taskId && <span>Task #{entry.taskId}</span>}
                          {entry.clientIp && <span>IP: {entry.clientIp}</span>}
                          {entry.correlationId && (
                            <span className="font-mono text-slate-600 break-all">Corr: {entry.correlationId}</span>
                          )}
                        </div>

                        {entry.exceptionMessage && (
                          <div className="p-2 rounded-lg bg-rose-50 border border-rose-200 text-[11px] text-rose-800 break-words [overflow-wrap:anywhere]">
                            <strong>{entry.exceptionType}:</strong> {entry.exceptionMessage}
                          </div>
                        )}
                      </div>
                    );
                  })}
                </div>

                {logsData && (
                  <Pagination
                    currentPage={logsData.pageNumber}
                    totalPages={logsData.totalPages}
                    totalCount={logsData.totalCount}
                    pageSize={logsData.pageSize}
                    onPageChange={(p) => setLogQuery((prev) => ({ ...prev, pageNumber: p }))}
                    onPageSizeChange={(s) =>
                      setLogQuery((prev) => ({ ...prev, pageSize: s, pageNumber: 1 }))
                    }
                  />
                )}
              </>
            )}
          </div>
        </>
      )}
    </div>
  );
};
