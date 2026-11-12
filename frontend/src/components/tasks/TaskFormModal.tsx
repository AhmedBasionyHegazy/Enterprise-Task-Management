import React, { useState, useEffect } from 'react';
import { TaskItem, User, TaskCreatePayload, TaskUpdatePayload } from '../../types';
import { taskService } from '../../services/taskService';
import { userService } from '../../services/userService';
import { Modal } from '../common/Modal';
import { toast } from '../../store/useToastStore';

interface TaskFormModalProps {
  isOpen: boolean;
  onClose: () => void;
  taskToEdit?: TaskItem | null;
  onSuccess: () => void;
}

export const TaskFormModal: React.FC<TaskFormModalProps> = ({
  isOpen,
  onClose,
  taskToEdit,
  onSuccess,
}) => {
  const [employees, setEmployees] = useState<User[]>([]);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const [formData, setFormData] = useState({
    title: '',
    description: '',
    priority: 'Medium',
    status: 'New',
    startDate: new Date().toISOString().split('T')[0],
    dueDate: new Date(Date.now() + 7 * 86400000).toISOString().split('T')[0],
    assignedToUserId: 0,
  });

  const [errors, setErrors] = useState<Record<string, string>>({});

  useEffect(() => {
    if (isOpen) {
      loadEmployees();
      if (taskToEdit) {
        setFormData({
          title: taskToEdit.title,
          description: taskToEdit.description || '',
          priority: taskToEdit.priority,
          status: taskToEdit.status,
          startDate: taskToEdit.startDate.split('T')[0],
          dueDate: taskToEdit.dueDate.split('T')[0],
          assignedToUserId: taskToEdit.assignedToUserId,
        });
      } else {
        setFormData({
          title: '',
          description: '',
          priority: 'Medium',
          status: 'New',
          startDate: new Date().toISOString().split('T')[0],
          dueDate: new Date(Date.now() + 7 * 86400000).toISOString().split('T')[0],
          assignedToUserId: 0,
        });
      }
      setErrors({});
    }
  }, [isOpen, taskToEdit]);

  const loadEmployees = async () => {
    try {
      const data = await userService.getEmployees();
      setEmployees(data.filter((e) => e.isActive));
      if (!taskToEdit && data.length > 0 && formData.assignedToUserId === 0) {
        setFormData((prev) => ({ ...prev, assignedToUserId: data[0].id }));
      }
    } catch {
      toast.error('Failed to load employee list');
    }
  };

  const validate = () => {
    const errs: Record<string, string> = {};
    if (!formData.title.trim()) errs.title = 'Title is required';
    if (!formData.assignedToUserId || formData.assignedToUserId === 0) {
      errs.assignedToUserId = 'Please assign an employee';
    }
    if (new Date(formData.dueDate) < new Date(formData.startDate)) {
      errs.dueDate = 'Due date must be on or after start date';
    }
    setErrors(errs);
    return Object.keys(errs).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!validate()) return;

    setIsSubmitting(true);
    try {
      if (taskToEdit) {
        const payload: TaskUpdatePayload = {
          id: taskToEdit.id,
          title: formData.title.trim(),
          description: formData.description.trim(),
          priority: formData.priority,
          status: formData.status,
          startDate: new Date(formData.startDate).toISOString(),
          dueDate: new Date(formData.dueDate).toISOString(),
          assignedToUserId: Number(formData.assignedToUserId),
        };
        await taskService.updateTask(taskToEdit.id, payload);
        toast.success('Task updated successfully');
      } else {
        const payload: TaskCreatePayload = {
          title: formData.title.trim(),
          description: formData.description.trim(),
          priority: formData.priority,
          status: formData.status,
          startDate: new Date(formData.startDate).toISOString(),
          dueDate: new Date(formData.dueDate).toISOString(),
          assignedToUserId: Number(formData.assignedToUserId),
        };
        await taskService.createTask(payload);
        toast.success('Task created and assigned successfully');
      }
      onSuccess();
      onClose();
    } catch (err: any) {
      toast.error(err.response?.data?.message || 'Operation failed');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title={taskToEdit ? 'Edit Task' : 'Create New Task'}
      subtitle={taskToEdit ? `Updating task #${taskToEdit.id}` : 'Fill in the details to assign a new task'}
      maxWidth="lg"
    >
      <form onSubmit={handleSubmit} className="space-y-4">
        {/* Title */}
        <div>
          <label className="block text-xs font-semibold text-slate-700 mb-1">
            Task Title <span className="text-rose-500">*</span>
          </label>
          <input
            type="text"
            value={formData.title}
            onChange={(e) => setFormData({ ...formData, title: e.target.value })}
            placeholder="e.g. Conduct Security Audit and Vulnerability Scan"
            className="w-full text-xs px-3.5 py-2.5 rounded-xl border border-slate-300 focus:outline-hidden focus:ring-1 focus:ring-indigo-500 bg-white"
          />
          {errors.title && <p className="text-[11px] text-rose-500 mt-1">{errors.title}</p>}
        </div>

        {/* Description */}
        <div>
          <label className="block text-xs font-semibold text-slate-700 mb-1">Description</label>
          <textarea
            rows={3}
            value={formData.description}
            onChange={(e) => setFormData({ ...formData, description: e.target.value })}
            placeholder="Detailed description of task objectives and deliverables..."
            className="w-full text-xs px-3.5 py-2.5 rounded-xl border border-slate-300 focus:outline-hidden focus:ring-1 focus:ring-indigo-500 bg-white"
          />
        </div>

        {/* Priority & Status */}
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1">Priority</label>
            <select
              value={formData.priority}
              onChange={(e) => setFormData({ ...formData, priority: e.target.value })}
              className="w-full text-xs px-3.5 py-2.5 rounded-xl border border-slate-300 focus:outline-hidden focus:ring-1 focus:ring-indigo-500 bg-white"
            >
              <option value="Low">Low</option>
              <option value="Medium">Medium</option>
              <option value="High">High</option>
            </select>
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1">Status</label>
            <select
              value={formData.status}
              onChange={(e) => setFormData({ ...formData, status: e.target.value })}
              className="w-full text-xs px-3.5 py-2.5 rounded-xl border border-slate-300 focus:outline-hidden focus:ring-1 focus:ring-indigo-500 bg-white"
            >
              <option value="New">New</option>
              <option value="In Progress">In Progress</option>
              <option value="Completed">Completed</option>
            </select>
          </div>
        </div>

        {/* Dates */}
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1">Start Date</label>
            <input
              type="date"
              value={formData.startDate}
              onChange={(e) => setFormData({ ...formData, startDate: e.target.value })}
              className="w-full text-xs px-3.5 py-2.5 rounded-xl border border-slate-300 focus:outline-hidden focus:ring-1 focus:ring-indigo-500 bg-white"
            />
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1">
              Due Date <span className="text-rose-500">*</span>
            </label>
            <input
              type="date"
              value={formData.dueDate}
              onChange={(e) => setFormData({ ...formData, dueDate: e.target.value })}
              className="w-full text-xs px-3.5 py-2.5 rounded-xl border border-slate-300 focus:outline-hidden focus:ring-1 focus:ring-indigo-500 bg-white"
            />
            {errors.dueDate && <p className="text-[11px] text-rose-500 mt-1">{errors.dueDate}</p>}
          </div>
        </div>

        {/* Assignee */}
        <div>
          <label className="block text-xs font-semibold text-slate-700 mb-1">
            Assign To Employee <span className="text-rose-500">*</span>
          </label>
          <select
            value={formData.assignedToUserId}
            onChange={(e) => setFormData({ ...formData, assignedToUserId: Number(e.target.value) })}
            className="w-full text-xs px-3.5 py-2.5 rounded-xl border border-slate-300 focus:outline-hidden focus:ring-1 focus:ring-indigo-500 bg-white"
          >
            <option value={0}>-- Select Employee --</option>
            {employees.map((emp) => (
              <option key={emp.id} value={emp.id}>
                {emp.fullName} ({emp.department || 'Employee'})
              </option>
            ))}
          </select>
          {errors.assignedToUserId && (
            <p className="text-[11px] text-rose-500 mt-1">{errors.assignedToUserId}</p>
          )}
        </div>

        {/* Footer actions */}
        <div className="flex flex-col-reverse sm:flex-row items-stretch sm:items-center justify-end gap-2 sm:gap-3 pt-4 border-t border-slate-100">
          <button
            type="button"
            onClick={onClose}
            className="px-4 py-2.5 sm:py-2 rounded-xl text-xs font-medium text-slate-600 hover:bg-slate-100 border border-slate-200 sm:border-transparent transition"
          >
            Cancel
          </button>
          <button
            type="submit"
            disabled={isSubmitting}
            className="px-5 py-2.5 sm:py-2 rounded-xl text-xs font-semibold text-white bg-indigo-600 hover:bg-indigo-700 disabled:opacity-50 transition shadow-xs"
          >
            {isSubmitting ? 'Saving...' : taskToEdit ? 'Save Changes' : 'Create Task'}
          </button>
        </div>
      </form>
    </Modal>
  );
};
