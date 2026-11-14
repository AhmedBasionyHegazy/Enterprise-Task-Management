import React, { useState, useEffect } from 'react';
import { TaskItem } from '../types';
import { useDashboardQuery } from '../hooks/useQueries';
import { StatCard } from '../components/common/StatCard';
import { PriorityBadge, StatusBadge } from '../components/common/Badge';
import { TaskDetailsModal } from '../components/tasks/TaskDetailsModal';
import { TaskHistoryDrawer } from '../components/tasks/TaskHistoryDrawer';
import { useAuthStore } from '../store/useAuthStore';
import { toast } from '../store/useToastStore';
import {
  CheckSquare,
  Clock,
  AlertCircle,
  CheckCircle2,
  ListTodo,
  AlertTriangle,
  ArrowUpRight,
  ExternalLink,
  History,
} from 'lucide-react';
import { Link } from 'react-router-dom';

export const DashboardPage: React.FC = () => {
  const { role } = useAuthStore();
  const isAdmin = role === 'Admin';

  const { data: stats, isLoading, isError } = useDashboardQuery();
  const [selectedTask, setSelectedTask] = useState<TaskItem | null>(null);
  const [isDetailsOpen, setIsDetailsOpen] = useState(false);
  const [historyTask, setHistoryTask] = useState<TaskItem | null>(null);

  useEffect(() => {
    if (isError) {
      toast.error('Failed to load dashboard metrics');
    }
  }, [isError]);

  const handleOpenTask = (task: TaskItem) => {
    setSelectedTask(task);
    setIsDetailsOpen(true);
  };

  if (isLoading) {
    return (
      <div className="flex flex-col items-center justify-center min-h-[60vh] gap-3">
        <div className="w-10 h-10 border-4 border-blue-600 border-t-transparent rounded-full animate-spin" />
        <p className="text-xs text-slate-500 font-medium">Loading live dashboard telemetry...</p>
      </div>
    );
  }

  if (!stats) return null;

  return (
    <div className="w-full max-w-full min-w-0 space-y-5 sm:space-y-8 animate-in fade-in duration-300">
      {/* Top Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 sm:gap-4 min-w-0">
        <div className="min-w-0">
          <h1 className="text-lg sm:text-2xl font-bold tracking-tight text-slate-900">
            {isAdmin ? 'Executive Analytics & Task Overview' : 'My Workload & Performance Dashboard'}
          </h1>
          <p className="text-xs text-slate-500 mt-1">
            Real-time metric telemetry, status progressions, and schedule tracking.
          </p>
        </div>
        <div className="flex items-center gap-2">
          <Link
            to="/tasks"
            className="w-full sm:w-auto inline-flex items-center justify-center gap-1.5 px-4 py-2.5 sm:py-2 rounded-xl text-xs font-semibold text-white bg-blue-600 hover:bg-blue-700 transition shadow-xs"
          >
            <span>View Tasks List</span>
            <ArrowUpRight className="w-3.5 h-3.5" />
          </Link>
        </div>
      </div>

      {/* KPI Cards Row (Total, New, In Progress, Completed, Overdue) */}
      <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-5 gap-2.5 sm:gap-4 min-w-0">
        <StatCard
          title="Total Tasks"
          value={stats.totalTasks}
          subtitle={isAdmin ? 'All organizational tasks' : 'Assigned to your queue'}
          icon={<CheckSquare className="w-5 h-5 text-blue-600" />}
          variant="primary"
        />

        <StatCard
          title="New Tasks"
          value={stats.newTasks}
          subtitle="Pending start / intake"
          icon={<ListTodo className="w-5 h-5 text-blue-600" />}
          variant="default"
        />

        <StatCard
          title="In Progress"
          value={stats.inProgressTasks}
          subtitle="Currently underway"
          icon={<Clock className="w-5 h-5 text-amber-600" />}
          variant="warning"
        />

        <StatCard
          title="Completed"
          value={stats.completedTasks}
          subtitle="Successfully closed"
          icon={<CheckCircle2 className="w-5 h-5 text-emerald-600" />}
          variant="success"
        />

        <div className="col-span-2 sm:col-span-2 lg:col-span-1 min-w-0">
          <StatCard
            title="Overdue Tasks"
            value={stats.overdueTasks}
            subtitle={stats.overdueTasks > 0 ? 'Requires immediate action!' : 'All schedules on track'}
            icon={<AlertCircle className="w-5 h-5 text-rose-600" />}
            variant="danger"
          />
        </div>
      </div>

      {/* Overdue Alert Banner if tasks are overdue */}
      {stats.overdueTasks > 0 && (
        <div className="p-4 rounded-2xl bg-rose-50 border border-rose-200 flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-3 sm:gap-4">
          <div className="flex items-start sm:items-center gap-3">
            <div className="p-2.5 rounded-xl bg-rose-600 text-white shrink-0">
              <AlertTriangle className="w-5 h-5" />
            </div>
            <div>
              <h3 className="text-sm font-semibold text-rose-950">
                Action Required: {stats.overdueTasks} Overdue Task{stats.overdueTasks > 1 ? 's' : ''} Detected
              </h3>
              <p className="text-xs text-rose-700 mt-0.5">
                Tasks that have surpassed their due date without reaching completed status.
              </p>
            </div>
          </div>
          <Link
            to="/tasks?status=In+Progress"
            className="text-center px-3.5 py-2 sm:py-1.5 rounded-xl bg-rose-600 hover:bg-rose-700 text-white text-xs font-semibold shrink-0 transition shadow-xs"
          >
            Review Overdue Tasks
          </Link>
        </div>
      )}

      {/* Progress & Breakdown Section */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-4 sm:gap-6 min-w-0">
        {/* Priority Breakdown (5 cols) */}
        <div className="lg:col-span-5 min-w-0 bg-white p-4 sm:p-6 rounded-2xl border border-slate-200/80 shadow-xs flex flex-col justify-between">
          <div>
            <div className="flex items-center justify-between mb-4">
              <h3 className="text-sm font-semibold text-slate-800">Priority Distribution</h3>
              <span className="text-xs text-slate-400">By Severity</span>
            </div>

            <div className="space-y-4">
              {stats.priorityBreakdown.map((item) => (
                <div key={item.label}>
                  <div className="flex items-center justify-between text-xs font-medium mb-1.5">
                    <span className="flex items-center gap-2">
                      <PriorityBadge priority={item.label} size="sm" />
                    </span>
                    <span className="text-slate-500 font-semibold">
                      {item.count} tasks ({item.percentage}%)
                    </span>
                  </div>
                  <div className="w-full h-2 rounded-full bg-slate-100 overflow-hidden">
                    <div
                      className={`h-full rounded-full transition-all duration-500 ${
                        item.label === 'High'
                          ? 'bg-rose-500'
                          : item.label === 'Medium'
                          ? 'bg-amber-500'
                          : 'bg-slate-400'
                      }`}
                      style={{ width: `${item.percentage}%` }}
                    />
                  </div>
                </div>
              ))}
            </div>
          </div>

          <div className="pt-4 mt-6 border-t border-slate-100 text-xs text-slate-400 flex items-center justify-between">
            <span>Overall completion rate:</span>
            <span className="font-bold text-slate-800">
              {stats.totalTasks > 0
                ? Math.round((stats.completedTasks / stats.totalTasks) * 100)
                : 0}
              %
            </span>
          </div>
        </div>

        {/* Status Breakdown (7 cols) */}
        <div className="lg:col-span-7 min-w-0 bg-white p-4 sm:p-6 rounded-2xl border border-slate-200/80 shadow-xs">
          <div className="flex items-center justify-between mb-4">
            <h3 className="text-sm font-semibold text-slate-800">Lifecycle Progress</h3>
            <span className="text-xs text-slate-400">Status Metrics</span>
          </div>

          <div className="grid grid-cols-3 gap-2 sm:gap-4 mb-6">
            {stats.statusBreakdown.map((s) => (
              <div
                key={s.label}
                className="p-2.5 sm:p-4 rounded-xl border border-slate-100 bg-slate-50/60 text-center min-w-0"
              >
                <div className="text-[11px] sm:text-xs text-slate-500 mb-1 truncate">{s.label}</div>
                <div className="text-lg sm:text-2xl font-bold text-slate-800">{s.count}</div>
                <div className="text-[10px] sm:text-[11px] text-slate-400 mt-0.5">{s.percentage}%</div>
              </div>
            ))}
          </div>

          {/* Recent tasks preview */}
          <div>
            <div className="text-xs font-semibold uppercase tracking-wider text-slate-400 mb-3">
              Recently Assigned Tasks
            </div>
            <div className="divide-y divide-slate-100">
              {stats.recentTasks.map((t) => (
                <div
                  key={t.id}
                  onClick={() => handleOpenTask(t)}
                  className="py-2.5 flex items-center justify-between gap-2 hover:bg-slate-50 px-2 rounded-xl cursor-pointer transition min-w-0"
                >
                  <div className="flex-1 min-w-0 pr-2">
                    <div className="text-xs font-semibold text-slate-800 truncate">{t.title}</div>
                    <div className="text-[11px] text-slate-400 truncate">
                      Due: {new Date(t.dueDate).toLocaleDateString()} &bull;{' '}
                      <span className="text-slate-600">{t.assignedToFullName}</span>
                    </div>
                  </div>
                  <div className="flex items-center gap-1.5 shrink-0">
                    <StatusBadge status={t.status} size="sm" />
                    <button
                      type="button"
                      onClick={(e) => {
                        e.stopPropagation();
                        setHistoryTask(t);
                      }}
                      title="Task History"
                      aria-label={`View history for ${t.title}`}
                      className="p-1.5 text-slate-400 hover:text-blue-600 hover:bg-blue-50 rounded-lg transition"
                    >
                      <History className="w-3.5 h-3.5" />
                    </button>
                    <ExternalLink className="w-3.5 h-3.5 text-slate-300 hidden sm:block" />
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>
      </div>

      {/* Employee Workloads Section (Admin Only) */}
      {isAdmin && stats.employeeWorkloads.length > 0 && (
        <div className="bg-white rounded-2xl border border-slate-200/80 shadow-xs overflow-hidden min-w-0 w-full">
          <div className="px-4 sm:px-6 py-4 border-b border-slate-100 flex flex-wrap items-center justify-between gap-2">
            <div className="min-w-0">
              <h3 className="text-sm font-semibold text-slate-800">Employee Workload & Capacity</h3>
              <p className="text-xs text-slate-400 mt-0.5">Distribution of tasks across active staff members</p>
            </div>
            <Link
              to="/employees"
              className="text-xs font-medium text-blue-600 hover:text-blue-800 shrink-0 transition"
            >
              Manage &rarr;
            </Link>
          </div>

          {/* Mobile & Tablet Workload Cards (< 1024px) */}
          <div className="lg:hidden divide-y divide-slate-100">
            {stats.employeeWorkloads.map((emp) => (
              <div key={emp.userId} className="p-4 space-y-2.5">
                <div className="flex items-center justify-between">
                  <div className="font-semibold text-xs text-slate-900">{emp.fullName}</div>
                  <div className="text-[11px] text-slate-400">{emp.department}</div>
                </div>
                <div className="grid grid-cols-2 sm:grid-cols-4 gap-2 text-center">
                  <div className="p-2 rounded-lg bg-slate-50 border border-slate-100">
                    <div className="text-[10px] text-slate-400">Assigned</div>
                    <div className="text-xs font-bold text-slate-800 mt-0.5">{emp.assignedTasksCount}</div>
                  </div>
                  <div className="p-2 rounded-lg bg-amber-50/60 border border-amber-100">
                    <div className="text-[10px] text-amber-600">Pending</div>
                    <div className="text-xs font-bold text-amber-700 mt-0.5">{emp.pendingTasksCount}</div>
                  </div>
                  <div className="p-2 rounded-lg bg-emerald-50/60 border border-emerald-100">
                    <div className="text-[10px] text-emerald-600">Done</div>
                    <div className="text-xs font-bold text-emerald-700 mt-0.5">{emp.completedTasksCount}</div>
                  </div>
                  <div className="p-2 rounded-lg bg-rose-50/60 border border-rose-100">
                    <div className="text-[10px] text-rose-600">Overdue</div>
                    <div className="text-xs font-bold text-rose-700 mt-0.5">{emp.overdueTasksCount}</div>
                  </div>
                </div>
              </div>
            ))}
          </div>

          {/* Desktop Workload Table (>= 1024px) */}
          <div className="hidden lg:block overflow-x-auto">
            <table className="w-full text-left border-collapse">
              <thead>
                <tr className="bg-slate-50/75 border-b border-slate-100 text-[11px] font-semibold text-slate-500 uppercase tracking-wider">
                  <th className="py-3 px-6">Employee</th>
                  <th className="py-3 px-6">Department</th>
                  <th className="py-3 px-6 text-center">Assigned</th>
                  <th className="py-3 px-6 text-center">Pending</th>
                  <th className="py-3 px-6 text-center">Completed</th>
                  <th className="py-3 px-6 text-center">Overdue</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 text-xs text-slate-700">
                {stats.employeeWorkloads.map((emp) => (
                  <tr key={emp.userId} className="hover:bg-slate-50/50 transition">
                    <td className="py-3.5 px-6 font-semibold text-slate-800">{emp.fullName}</td>
                    <td className="py-3.5 px-6 text-slate-500">{emp.department}</td>
                    <td className="py-3.5 px-6 text-center font-bold text-slate-800">
                      {emp.assignedTasksCount}
                    </td>
                    <td className="py-3.5 px-6 text-center">
                      <span className="inline-flex px-2 py-0.5 rounded-full bg-amber-50 text-amber-700 font-medium">
                        {emp.pendingTasksCount}
                      </span>
                    </td>
                    <td className="py-3.5 px-6 text-center">
                      <span className="inline-flex px-2 py-0.5 rounded-full bg-emerald-50 text-emerald-700 font-medium">
                        {emp.completedTasksCount}
                      </span>
                    </td>
                    <td className="py-3.5 px-6 text-center">
                      {emp.overdueTasksCount > 0 ? (
                        <span className="inline-flex px-2 py-0.5 rounded-full bg-rose-50 text-rose-700 font-bold">
                          {emp.overdueTasksCount}
                        </span>
                      ) : (
                        <span className="text-slate-300">0</span>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Task Details & Comments Modal */}
      <TaskDetailsModal
        task={selectedTask}
        isOpen={isDetailsOpen}
        onClose={() => setIsDetailsOpen(false)}
        onOpenHistory={(t) => setHistoryTask(t)}
      />

      {/* Task History Drawer */}
      <TaskHistoryDrawer
        task={historyTask}
        isOpen={!!historyTask}
        onClose={() => setHistoryTask(null)}
      />
    </div>
  );
};
