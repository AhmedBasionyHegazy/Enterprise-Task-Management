import React, { useState, useEffect } from 'react';
import { dashboardService } from '../services/dashboardService';
import { DashboardSummary, TaskItem } from '../types';
import { StatCard } from '../components/common/StatCard';
import { PriorityBadge, StatusBadge } from '../components/common/Badge';
import { TaskDetailsModal } from '../components/tasks/TaskDetailsModal';
import { useAuthStore } from '../store/useAuthStore';
import { toast } from '../store/useToastStore';
import { 
  CheckSquare, 
  Clock, 
  AlertCircle, 
  CheckCircle2, 
  ListTodo, 
  TrendingUp, 
  Users, 
  AlertTriangle,
  ArrowUpRight,
  ExternalLink
} from 'lucide-react';
import { Link } from 'react-router-dom';

export const DashboardPage: React.FC = () => {
  const { role } = useAuthStore();
  const isAdmin = role === 'Admin';

  const [stats, setStats] = useState<DashboardSummary | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [selectedTask, setSelectedTask] = useState<TaskItem | null>(null);
  const [isDetailsOpen, setIsDetailsOpen] = useState(false);

  useEffect(() => {
    loadDashboardData();
  }, []);

  const loadDashboardData = async () => {
    setIsLoading(true);
    try {
      const data = await dashboardService.getStats();
      setStats(data);
    } catch {
      toast.error('Failed to load dashboard metrics');
    } finally {
      setIsLoading(false);
    }
  };

  const handleOpenTask = (task: TaskItem) => {
    setSelectedTask(task);
    setIsDetailsOpen(true);
  };

  if (isLoading) {
    return (
      <div className="flex flex-col items-center justify-center min-h-[60vh] gap-3">
        <div className="w-10 h-10 border-4 border-indigo-600 border-t-transparent rounded-full animate-spin" />
        <p className="text-xs text-slate-500 font-medium">Loading live dashboard telemetry...</p>
      </div>
    );
  }

  if (!stats) return null;

  return (
    <div className="space-y-8 animate-in fade-in duration-300">
      {/* Top Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-slate-900">
            {isAdmin ? 'Executive Analytics & Task Overview' : 'My Workload & Performance Dashboard'}
          </h1>
          <p className="text-xs text-slate-500 mt-1">
            Real-time metric telemetry, status progressions, and schedule tracking.
          </p>
        </div>
        <div className="flex items-center gap-2">
          <Link
            to="/tasks"
            className="inline-flex items-center gap-1.5 px-4 py-2 rounded-xl text-xs font-semibold text-white bg-indigo-600 hover:bg-indigo-700 transition shadow-xs"
          >
            <span>View Tasks List</span>
            <ArrowUpRight className="w-3.5 h-3.5" />
          </Link>
        </div>
      </div>

      {/* KPI Cards Row (Total, New, In Progress, Completed, Overdue) */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-5 gap-4">
        <StatCard
          title="Total Tasks"
          value={stats.totalTasks}
          subtitle={isAdmin ? 'All organizational tasks' : 'Assigned to your queue'}
          icon={<CheckSquare className="w-5 h-5 text-indigo-600" />}
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

        <StatCard
          title="Overdue Tasks"
          value={stats.overdueTasks}
          subtitle={stats.overdueTasks > 0 ? 'Requires immediate action!' : 'All schedules on track'}
          icon={<AlertCircle className="w-5 h-5 text-rose-600" />}
          variant="danger"
        />
      </div>

      {/* Overdue Alert Banner if tasks are overdue */}
      {stats.overdueTasks > 0 && (
        <div className="p-4 rounded-2xl bg-gradient-to-r from-rose-500/10 via-rose-50 to-amber-50 border border-rose-200 flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
          <div className="flex items-center gap-3">
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
            className="px-3.5 py-1.5 rounded-xl bg-rose-600 hover:bg-rose-700 text-white text-xs font-semibold shrink-0 transition shadow-xs"
          >
            Review Overdue Tasks
          </Link>
        </div>
      )}

      {/* Progress & Breakdown Section */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
        {/* Priority Breakdown (5 cols) */}
        <div className="lg:col-span-5 bg-white p-6 rounded-2xl border border-slate-200/80 shadow-xs flex flex-col justify-between">
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
        <div className="lg:col-span-7 bg-white p-6 rounded-2xl border border-slate-200/80 shadow-xs">
          <div className="flex items-center justify-between mb-4">
            <h3 className="text-sm font-semibold text-slate-800">Lifecycle Progress</h3>
            <span className="text-xs text-slate-400">Status Metrics</span>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 mb-6">
            {stats.statusBreakdown.map((s) => (
              <div
                key={s.label}
                className="p-4 rounded-xl border border-slate-100 bg-slate-50/60 text-center"
              >
                <div className="text-xs text-slate-500 mb-1">{s.label}</div>
                <div className="text-2xl font-bold text-slate-800">{s.count}</div>
                <div className="text-[11px] text-slate-400 mt-0.5">{s.percentage}% of workload</div>
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
                  className="py-2.5 flex items-center justify-between hover:bg-slate-50 px-2 rounded-xl cursor-pointer transition"
                >
                  <div className="min-w-0 pr-4">
                    <div className="text-xs font-semibold text-slate-800 truncate">{t.title}</div>
                    <div className="text-[11px] text-slate-400">
                      Due: {new Date(t.dueDate).toLocaleDateString()} &bull; Assigned to:{' '}
                      <span className="text-slate-600">{t.assignedToFullName}</span>
                    </div>
                  </div>
                  <div className="flex items-center gap-2 shrink-0">
                    <StatusBadge status={t.status} size="sm" />
                    <ExternalLink className="w-3.5 h-3.5 text-slate-300" />
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>
      </div>

      {/* Employee Workloads Table (Admin Only) */}
      {isAdmin && stats.employeeWorkloads.length > 0 && (
        <div className="bg-white rounded-2xl border border-slate-200/80 shadow-xs overflow-hidden">
          <div className="px-6 py-4 border-b border-slate-100 flex items-center justify-between">
            <div>
              <h3 className="text-sm font-semibold text-slate-800">Employee Workload & Capacity</h3>
              <p className="text-xs text-slate-400 mt-0.5">Distribution of tasks across active staff members</p>
            </div>
            <Link
              to="/employees"
              className="text-xs font-medium text-indigo-600 hover:text-indigo-800 transition"
            >
              Manage Employees &rarr;
            </Link>
          </div>

          <div className="overflow-x-auto">
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
        onTaskUpdated={loadDashboardData}
      />
    </div>
  );
};
