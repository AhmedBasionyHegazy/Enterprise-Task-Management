import React, { useState, useEffect, useCallback } from 'react';
import { useAuthStore } from '../store/useAuthStore';
import { taskService } from '../services/taskService';
import { userService } from '../services/userService';
import { TaskItem, User, TaskQueryParams } from '../types';
import { PriorityBadge } from '../components/common/Badge';
import { Pagination } from '../components/common/Pagination';
import { Modal } from '../components/common/Modal';
import { TaskFormModal } from '../components/tasks/TaskFormModal';
import { TaskDetailsModal } from '../components/tasks/TaskDetailsModal';
import { toast } from '../store/useToastStore';
import { 
  Search, 
  Plus, 
  Edit3, 
  Trash2, 
  MessageSquare, 
  ArrowUpDown, 
  AlertTriangle,
  RotateCcw,
  Calendar,
  User as UserIcon
} from 'lucide-react';

export const TasksPage: React.FC = () => {
  const { role } = useAuthStore();
  const isAdmin = role === 'Admin';

  const [tasks, setTasks] = useState<TaskItem[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(1);
  const [isLoading, setIsLoading] = useState(true);
  const [employees, setEmployees] = useState<User[]>([]);

  // Query state
  const [queryParams, setQueryParams] = useState<TaskQueryParams>({
    search: '',
    assignedToUserId: undefined,
    status: 'All',
    priority: 'All',
    sortBy: 'DueDate',
    sortDescending: false,
    pageNumber: 1,
    pageSize: 10,
  });

  // Modals state
  const [isFormModalOpen, setIsFormModalOpen] = useState(false);
  const [taskToEdit, setTaskToEdit] = useState<TaskItem | null>(null);

  const [isDetailsModalOpen, setIsDetailsModalOpen] = useState(false);
  const [selectedTask, setSelectedTask] = useState<TaskItem | null>(null);

  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const [taskToDelete, setTaskToDelete] = useState<TaskItem | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);

  useEffect(() => {
    if (isAdmin) {
      loadEmployees();
    }
  }, [isAdmin]);

  const loadEmployees = async () => {
    try {
      const data = await userService.getEmployees();
      setEmployees(data);
    } catch {
      // Ignored
    }
  };

  const loadTasks = useCallback(async () => {
    setIsLoading(true);
    try {
      const cleanParams: TaskQueryParams = {
        ...queryParams,
        search: queryParams.search?.trim() || undefined,
        status: queryParams.status === 'All' ? undefined : queryParams.status,
        priority: queryParams.priority === 'All' ? undefined : queryParams.priority,
        assignedToUserId: queryParams.assignedToUserId && queryParams.assignedToUserId > 0 ? queryParams.assignedToUserId : undefined,
      };

      const result = await taskService.getTasks(cleanParams);
      setTasks(result.items);
      setTotalCount(result.totalCount);
      setTotalPages(result.totalPages);
    } catch {
      toast.error('Failed to load tasks list');
    } finally {
      setIsLoading(false);
    }
  }, [queryParams]);

  useEffect(() => {
    loadTasks();
  }, [loadTasks]);

  const handleSort = (columnName: string) => {
    setQueryParams((prev) => ({
      ...prev,
      sortBy: columnName,
      sortDescending: prev.sortBy === columnName ? !prev.sortDescending : false,
      pageNumber: 1,
    }));
  };

  const handleStatusChange = async (taskId: number, newStatus: string) => {
    try {
      await taskService.updateTaskStatus(taskId, newStatus);
      toast.success(`Task status updated to ${newStatus}`);
      loadTasks();
    } catch (err: any) {
      toast.error(err.response?.data?.message || 'Failed to update status');
    }
  };

  const confirmDelete = async () => {
    if (!taskToDelete) return;
    setIsDeleting(true);
    try {
      await taskService.deleteTask(taskToDelete.id);
      toast.success('Task deleted successfully');
      setIsDeleteModalOpen(false);
      setTaskToDelete(null);
      loadTasks();
    } catch (err: any) {
      toast.error(err.response?.data?.message || 'Failed to delete task');
    } finally {
      setIsDeleting(false);
    }
  };

  const handleResetFilters = () => {
    setQueryParams({
      search: '',
      assignedToUserId: undefined,
      status: 'All',
      priority: 'All',
      sortBy: 'DueDate',
      sortDescending: false,
      pageNumber: 1,
      pageSize: 10,
    });
  };

  const formatDate = (dateStr: string) => {
    return new Date(dateStr).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
    });
  };

  return (
    <div className="space-y-4 sm:space-y-6 animate-in fade-in duration-300">
      {/* Top Header & New Task button */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 sm:gap-4">
        <div>
          <h1 className="text-xl sm:text-2xl font-bold tracking-tight text-slate-900">
            {isAdmin ? 'Task Administration & Assignments' : 'My Assigned Tasks'}
          </h1>
          <p className="text-xs text-slate-500 mt-1">
            {isAdmin 
              ? 'Manage organizational deliverables, employee assignments, and schedules.'
              : 'Keep track of your active deliverables, update statuses, and communicate progress.'}
          </p>
        </div>

        {isAdmin && (
          <button
            onClick={() => {
              setTaskToEdit(null);
              setIsFormModalOpen(true);
            }}
            className="w-full sm:w-auto inline-flex items-center justify-center gap-2 px-4 py-2.5 rounded-xl text-xs font-semibold text-white bg-indigo-600 hover:bg-indigo-700 transition shadow-xs"
          >
            <Plus className="w-4 h-4" />
            <span>Create New Task</span>
          </button>
        )}
      </div>

      {/* Filter and Search Bar */}
      <div className="p-3.5 sm:p-4 bg-white rounded-2xl border border-slate-200/80 shadow-xs space-y-3">
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-12 gap-2.5 sm:gap-3">
          {/* Search bar */}
          <div className="sm:col-span-2 lg:col-span-4 relative">
            <Search className="w-4 h-4 absolute left-3.5 top-1/2 -translate-y-1/2 text-slate-400" />
            <input
              type="text"
              value={queryParams.search}
              onChange={(e) =>
                setQueryParams((prev) => ({ ...prev, search: e.target.value, pageNumber: 1 }))
              }
              placeholder="Search by title or description..."
              className="w-full pl-9 pr-4 py-2.5 sm:py-2 text-xs rounded-xl border border-slate-200 bg-slate-50 focus:bg-white focus:outline-hidden focus:ring-1 focus:ring-indigo-500 transition"
            />
          </div>

          {/* Employee Filter (Admin only) */}
          {isAdmin && (
            <div className="lg:col-span-3">
              <select
                value={queryParams.assignedToUserId ?? 0}
                onChange={(e) =>
                  setQueryParams((prev) => ({
                    ...prev,
                    assignedToUserId: Number(e.target.value) || undefined,
                    pageNumber: 1,
                  }))
                }
                className="w-full py-2.5 sm:py-2 px-3 text-xs rounded-xl border border-slate-200 bg-slate-50 focus:bg-white focus:outline-hidden focus:ring-1 focus:ring-indigo-500 transition"
              >
                <option value={0}>All Employees</option>
                {employees.map((emp) => (
                  <option key={emp.id} value={emp.id}>
                    {emp.fullName}
                  </option>
                ))}
              </select>
            </div>
          )}

          {/* Status Filter */}
          <div className={isAdmin ? 'lg:col-span-2' : 'lg:col-span-3'}>
            <select
              value={queryParams.status}
              onChange={(e) =>
                setQueryParams((prev) => ({ ...prev, status: e.target.value, pageNumber: 1 }))
              }
              className="w-full py-2.5 sm:py-2 px-3 text-xs rounded-xl border border-slate-200 bg-slate-50 focus:bg-white focus:outline-hidden focus:ring-1 focus:ring-indigo-500 transition"
            >
              <option value="All">All Statuses</option>
              <option value="New">New</option>
              <option value="In Progress">In Progress</option>
              <option value="Completed">Completed</option>
            </select>
          </div>

          {/* Priority Filter */}
          <div className={isAdmin ? 'lg:col-span-2' : 'lg:col-span-3'}>
            <select
              value={queryParams.priority}
              onChange={(e) =>
                setQueryParams((prev) => ({ ...prev, priority: e.target.value, pageNumber: 1 }))
              }
              className="w-full py-2.5 sm:py-2 px-3 text-xs rounded-xl border border-slate-200 bg-slate-50 focus:bg-white focus:outline-hidden focus:ring-1 focus:ring-indigo-500 transition"
            >
              <option value="All">All Priorities</option>
              <option value="Low">Low</option>
              <option value="Medium">Medium</option>
              <option value="High">High</option>
            </select>
          </div>

          {/* Mobile Sort & Reset Controls */}
          <div className={`flex items-center justify-between lg:justify-end gap-2 ${isAdmin ? 'sm:col-span-2 lg:col-span-1' : 'sm:col-span-2 lg:col-span-2'}`}>
            <div className="flex md:hidden items-center gap-1.5 flex-1">
              <select
                value={queryParams.sortBy}
                onChange={(e) => handleSort(e.target.value)}
                className="flex-1 py-2.5 px-3 text-xs rounded-xl border border-slate-200 bg-slate-50 text-slate-700"
              >
                <option value="DueDate">Sort: Due Date</option>
                <option value="Priority">Sort: Priority</option>
                <option value="Status">Sort: Status</option>
                <option value="Title">Sort: Title</option>
              </select>
            </div>
            <button
              onClick={handleResetFilters}
              title="Reset Filters"
              className="p-2.5 sm:p-2 text-slate-500 hover:text-slate-700 hover:bg-slate-100 border border-slate-200 sm:border-transparent rounded-xl transition flex items-center gap-1.5 text-xs"
            >
              <RotateCcw className="w-4 h-4" />
              <span className="md:hidden">Reset</span>
            </button>
          </div>
        </div>
      </div>

      {/* Tasks List Container */}
      <div className="bg-white rounded-2xl border border-slate-200/80 shadow-xs overflow-hidden">
        {/* 1. MOBILE CARD LIST VIEW (< 768px) */}
        <div className="md:hidden divide-y divide-slate-100">
          {isLoading ? (
            <div className="py-12 text-center text-slate-400 text-xs">
              <div className="flex items-center justify-center gap-2">
                <span className="w-4 h-4 border-2 border-indigo-600 border-t-transparent rounded-full animate-spin" />
                <span>Loading records...</span>
              </div>
            </div>
          ) : tasks.length === 0 ? (
            <div className="py-12 px-4 text-center text-slate-400 text-xs">
              No tasks match the active filter criteria.
            </div>
          ) : (
            tasks.map((task) => (
              <div key={task.id} className="p-4 space-y-3 hover:bg-slate-50/60 transition">
                {/* Card Header: Title & Priority */}
                <div className="flex items-start justify-between gap-2">
                  <div className="min-w-0 flex-1">
                    <div
                      onClick={() => {
                        setSelectedTask(task);
                        setIsDetailsModalOpen(true);
                      }}
                      className="font-semibold text-sm text-slate-900 hover:text-indigo-600 cursor-pointer leading-snug"
                    >
                      {task.title}
                    </div>
                    {task.description && (
                      <p className="text-xs text-slate-500 line-clamp-2 mt-1">
                        {task.description}
                      </p>
                    )}
                  </div>
                  <div className="flex flex-col items-end gap-1 shrink-0">
                    <PriorityBadge priority={task.priority} size="sm" />
                    {task.isOverdue && (
                      <span className="text-[10px] font-bold uppercase tracking-wider px-1.5 py-0.5 rounded-sm bg-rose-100 text-rose-700 border border-rose-200">
                        Overdue
                      </span>
                    )}
                  </div>
                </div>

                {/* Meta Row: Due Date & Assignee */}
                <div className="flex flex-wrap items-center justify-between gap-2 text-xs text-slate-500 pt-1">
                  <div className={`flex items-center gap-1.5 ${task.isOverdue ? 'text-rose-600 font-semibold' : ''}`}>
                    <Calendar className="w-3.5 h-3.5 opacity-75" />
                    <span>Due: {formatDate(task.dueDate)}</span>
                  </div>
                  {isAdmin && (
                    <div className="flex items-center gap-1.5 text-slate-700 font-medium">
                      <UserIcon className="w-3.5 h-3.5 text-slate-400" />
                      <span>{task.assignedToFullName}</span>
                    </div>
                  )}
                </div>

                {/* Controls Row: Status Select & Action Buttons */}
                <div className="flex items-center justify-between gap-2 pt-2 border-t border-slate-100">
                  <select
                    value={task.status}
                    onChange={(e) => handleStatusChange(task.id, e.target.value)}
                    className="text-xs font-medium rounded-lg border border-slate-200 bg-slate-50 py-1.5 px-2.5 focus:ring-1 focus:ring-indigo-500 focus:outline-hidden"
                  >
                    <option value="New">New</option>
                    <option value="In Progress">In Progress</option>
                    <option value="Completed">Completed</option>
                  </select>

                  <div className="flex items-center gap-1.5">
                    <button
                      onClick={() => {
                        setSelectedTask(task);
                        setIsDetailsModalOpen(true);
                      }}
                      className="inline-flex items-center gap-1 px-2.5 py-1.5 rounded-lg text-xs font-medium text-indigo-600 bg-indigo-50 hover:bg-indigo-100 transition"
                    >
                      <MessageSquare className="w-3.5 h-3.5" />
                      <span>{task.commentsCount}</span>
                      <span className="ml-0.5">Details</span>
                    </button>

                    {isAdmin && (
                      <>
                        <button
                          onClick={() => {
                            setTaskToEdit(task);
                            setIsFormModalOpen(true);
                          }}
                          title="Edit Task"
                          className="p-2 text-slate-500 hover:text-indigo-600 hover:bg-indigo-50 rounded-lg border border-slate-200 transition"
                        >
                          <Edit3 className="w-3.5 h-3.5" />
                        </button>
                        <button
                          onClick={() => {
                            setTaskToDelete(task);
                            setIsDeleteModalOpen(true);
                          }}
                          title="Delete Task"
                          className="p-2 text-slate-500 hover:text-rose-600 hover:bg-rose-50 rounded-lg border border-slate-200 transition"
                        >
                          <Trash2 className="w-3.5 h-3.5" />
                        </button>
                      </>
                    )}
                  </div>
                </div>
              </div>
            ))
          )}
        </div>

        {/* 2. DESKTOP TABLE VIEW (>= 768px) */}
        <div className="hidden md:block overflow-x-auto">
          <table className="w-full text-left border-collapse">
            <thead>
              <tr className="bg-slate-50/75 border-b border-slate-200 text-[11px] font-semibold text-slate-500 uppercase tracking-wider select-none">
                <th
                  onClick={() => handleSort('Title')}
                  className="py-3.5 px-6 cursor-pointer hover:text-slate-900 transition"
                >
                  <div className="flex items-center gap-1.5">
                    <span>Task Title</span>
                    <ArrowUpDown className="w-3.5 h-3.5 text-slate-400" />
                  </div>
                </th>
                <th
                  onClick={() => handleSort('Priority')}
                  className="py-3.5 px-4 cursor-pointer hover:text-slate-900 transition"
                >
                  <div className="flex items-center gap-1.5">
                    <span>Priority</span>
                    <ArrowUpDown className="w-3.5 h-3.5 text-slate-400" />
                  </div>
                </th>
                <th
                  onClick={() => handleSort('Status')}
                  className="py-3.5 px-4 cursor-pointer hover:text-slate-900 transition"
                >
                  <div className="flex items-center gap-1.5">
                    <span>Status</span>
                    <ArrowUpDown className="w-3.5 h-3.5 text-slate-400" />
                  </div>
                </th>
                <th
                  onClick={() => handleSort('DueDate')}
                  className="py-3.5 px-4 cursor-pointer hover:text-slate-900 transition"
                >
                  <div className="flex items-center gap-1.5">
                    <span>Due Date</span>
                    <ArrowUpDown className="w-3.5 h-3.5 text-slate-400" />
                  </div>
                </th>
                {isAdmin && <th className="py-3.5 px-4">Assigned To</th>}
                <th className="py-3.5 px-4 text-center">Comments</th>
                <th className="py-3.5 px-6 text-right">Actions</th>
              </tr>
            </thead>

            <tbody className="divide-y divide-slate-100 text-xs text-slate-700">
              {isLoading ? (
                <tr>
                  <td colSpan={isAdmin ? 7 : 6} className="py-12 text-center text-slate-400">
                    <div className="flex items-center justify-center gap-2">
                      <span className="w-4 h-4 border-2 border-indigo-600 border-t-transparent rounded-full animate-spin" />
                      <span>Loading records...</span>
                    </div>
                  </td>
                </tr>
              ) : tasks.length === 0 ? (
                <tr>
                  <td colSpan={isAdmin ? 7 : 6} className="py-12 text-center text-slate-400">
                    No tasks match the active filter criteria.
                  </td>
                </tr>
              ) : (
                tasks.map((task) => (
                  <tr key={task.id} className="hover:bg-slate-50/70 transition">
                    {/* Title & overdue badge */}
                    <td className="py-4 px-6 font-medium text-slate-900">
                      <div className="flex items-start gap-2">
                        <div>
                          <div
                            onClick={() => {
                              setSelectedTask(task);
                              setIsDetailsModalOpen(true);
                            }}
                            className="font-semibold text-slate-800 hover:text-indigo-600 cursor-pointer transition"
                          >
                            {task.title}
                          </div>
                          {task.description && (
                            <p className="text-[11px] text-slate-400 line-clamp-1 mt-0.5">
                              {task.description}
                            </p>
                          )}
                        </div>
                        {task.isOverdue && (
                          <span
                            title="Overdue"
                            className="inline-flex items-center gap-0.5 text-[10px] font-bold uppercase tracking-wider px-1.5 py-0.5 rounded-sm bg-rose-100 text-rose-700 border border-rose-200 shrink-0"
                          >
                            Overdue
                          </span>
                        )}
                      </div>
                    </td>

                    {/* Priority */}
                    <td className="py-4 px-4">
                      <PriorityBadge priority={task.priority} size="sm" />
                    </td>

                    {/* Status with inline quick change */}
                    <td className="py-4 px-4">
                      <select
                        value={task.status}
                        onChange={(e) => handleStatusChange(task.id, e.target.value)}
                        className="text-xs font-medium rounded-lg border border-slate-200 bg-white py-1 px-2 focus:ring-1 focus:ring-indigo-500 focus:outline-hidden"
                      >
                        <option value="New">New</option>
                        <option value="In Progress">In Progress</option>
                        <option value="Completed">Completed</option>
                      </select>
                    </td>

                    {/* Due Date */}
                    <td className="py-4 px-4">
                      <div
                        className={`flex items-center gap-1.5 text-xs ${
                          task.isOverdue ? 'text-rose-600 font-semibold' : 'text-slate-600'
                        }`}
                      >
                        <Calendar className="w-3.5 h-3.5 opacity-70" />
                        <span>{formatDate(task.dueDate)}</span>
                      </div>
                    </td>

                    {/* Assigned To (Admin only) */}
                    {isAdmin && (
                      <td className="py-4 px-4">
                        <div className="text-xs font-medium text-slate-800">{task.assignedToFullName}</div>
                        {task.assignedToDepartment && (
                          <div className="text-[11px] text-slate-400">{task.assignedToDepartment}</div>
                        )}
                      </td>
                    )}

                    {/* Comments count */}
                    <td className="py-4 px-4 text-center">
                      <button
                        onClick={() => {
                          setSelectedTask(task);
                          setIsDetailsModalOpen(true);
                        }}
                        className="inline-flex items-center gap-1 px-2 py-1 rounded-md text-slate-500 hover:text-indigo-600 hover:bg-indigo-50 transition"
                      >
                        <MessageSquare className="w-3.5 h-3.5" />
                        <span className="text-xs font-semibold">{task.commentsCount}</span>
                      </button>
                    </td>

                    {/* Actions */}
                    <td className="py-4 px-6 text-right">
                      <div className="flex items-center justify-end gap-1.5">
                        <button
                          onClick={() => {
                            setSelectedTask(task);
                            setIsDetailsModalOpen(true);
                          }}
                          className="px-2 py-1 rounded-lg text-xs font-medium text-slate-600 hover:bg-slate-100 transition"
                        >
                          View Details
                        </button>

                        {isAdmin && (
                          <>
                            <button
                              onClick={() => {
                                setTaskToEdit(task);
                                setIsFormModalOpen(true);
                              }}
                              title="Edit Task"
                              className="p-1.5 text-slate-400 hover:text-indigo-600 hover:bg-indigo-50 rounded-lg transition"
                            >
                              <Edit3 className="w-4 h-4" />
                            </button>

                            <button
                              onClick={() => {
                                setTaskToDelete(task);
                                setIsDeleteModalOpen(true);
                              }}
                              title="Delete Task"
                              className="p-1.5 text-slate-400 hover:text-rose-600 hover:bg-rose-50 rounded-lg transition"
                            >
                              <Trash2 className="w-4 h-4" />
                            </button>
                          </>
                        )}
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        {/* Server-side Pagination controls */}
        <Pagination
          currentPage={queryParams.pageNumber || 1}
          totalPages={totalPages}
          totalCount={totalCount}
          pageSize={queryParams.pageSize || 10}
          onPageChange={(p) => setQueryParams((prev) => ({ ...prev, pageNumber: p }))}
          onPageSizeChange={(s) => setQueryParams((prev) => ({ ...prev, pageSize: s, pageNumber: 1 }))}
        />
      </div>

      {/* Task Create/Edit Modal (Admin Only) */}
      {isAdmin && (
        <TaskFormModal
          isOpen={isFormModalOpen}
          onClose={() => {
            setIsFormModalOpen(false);
            setTaskToEdit(null);
          }}
          taskToEdit={taskToEdit}
          onSuccess={loadTasks}
        />
      )}

      {/* Task Details & Comments Modal */}
      <TaskDetailsModal
        task={selectedTask}
        isOpen={isDetailsModalOpen}
        onClose={() => {
          setIsDetailsModalOpen(false);
          setSelectedTask(null);
        }}
        onTaskUpdated={loadTasks}
      />

      {/* Delete Confirmation Modal (Admin only) */}
      {isAdmin && (
        <Modal
          isOpen={isDeleteModalOpen}
          onClose={() => setIsDeleteModalOpen(false)}
          title="Confirm Task Deletion"
          maxWidth="sm"
        >
          <div className="space-y-4">
            <div className="flex items-start gap-3 p-3 bg-rose-50 border border-rose-200 rounded-xl text-xs text-rose-800">
              <AlertTriangle className="w-5 h-5 text-rose-600 shrink-0" />
              <div>
                Are you sure you want to permanently delete task:
                <div className="font-bold text-slate-900 mt-1">"{taskToDelete?.title}"</div>
                This action cannot be undone. All associated comments will also be removed.
              </div>
            </div>

            <div className="flex flex-col-reverse sm:flex-row items-stretch sm:items-center justify-end gap-2 pt-2">
              <button
                type="button"
                onClick={() => setIsDeleteModalOpen(false)}
                className="px-4 py-2.5 sm:py-2 rounded-xl text-xs font-medium text-slate-600 hover:bg-slate-100 border border-slate-200 sm:border-transparent transition"
              >
                Cancel
              </button>
              <button
                type="button"
                onClick={confirmDelete}
                disabled={isDeleting}
                className="px-4 py-2.5 sm:py-2 rounded-xl text-xs font-semibold text-white bg-rose-600 hover:bg-rose-700 disabled:opacity-50 transition shadow-xs"
              >
                {isDeleting ? 'Deleting...' : 'Delete Task'}
              </button>
            </div>
          </div>
        </Modal>
      )}
    </div>
  );
};
