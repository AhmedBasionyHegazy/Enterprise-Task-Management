import React, { useState, useEffect } from 'react';
import { TaskItem, TaskComment } from '../../types';
import { taskService } from '../../services/taskService';
import { Modal } from '../common/Modal';
import { PriorityBadge, StatusBadge, RoleBadge } from '../common/Badge';
import { toast } from '../../store/useToastStore';
import { 
  Calendar, 
  User as UserIcon, 
  Send, 
  Clock, 
  AlertTriangle,
  MessageSquare,
  CheckCircle2
} from 'lucide-react';

interface TaskDetailsModalProps {
  task: TaskItem | null;
  isOpen: boolean;
  onClose: () => void;
  onTaskUpdated?: () => void;
}

export const TaskDetailsModal: React.FC<TaskDetailsModalProps> = ({
  task,
  isOpen,
  onClose,
  onTaskUpdated,
}) => {
  const [comments, setComments] = useState<TaskComment[]>([]);
  const [newComment, setNewComment] = useState('');
  const [isLoadingComments, setIsLoadingComments] = useState(false);
  const [isSubmittingComment, setIsSubmittingComment] = useState(false);
  const [isUpdatingStatus, setIsUpdatingStatus] = useState(false);
  const [currentStatus, setCurrentStatus] = useState(task?.status || 'New');

  useEffect(() => {
    if (task && isOpen) {
      setCurrentStatus(task.status);
      loadComments(task.id);
    }
  }, [task, isOpen]);

  const loadComments = async (taskId: number) => {
    setIsLoadingComments(true);
    try {
      const data = await taskService.getComments(taskId);
      setComments(data);
    } catch {
      toast.error('Failed to load comments');
    } finally {
      setIsLoadingComments(false);
    }
  };

  const handleStatusChange = async (newStatus: string) => {
    if (!task) return;
    setIsUpdatingStatus(true);
    try {
      await taskService.updateTaskStatus(task.id, newStatus);
      setCurrentStatus(newStatus as any);
      toast.success(`Task status updated to ${newStatus}`);
      onTaskUpdated?.();
    } catch (err: any) {
      toast.error(err.response?.data?.message || 'Failed to update status');
    } finally {
      setIsUpdatingStatus(false);
    }
  };

  const handleAddComment = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!task || !newComment.trim()) return;

    setIsSubmittingComment(true);
    try {
      const added = await taskService.addComment(task.id, newComment.trim());
      setComments((prev) => [...prev, added]);
      setNewComment('');
      toast.success('Comment posted');
      onTaskUpdated?.();
    } catch (err: any) {
      toast.error(err.response?.data?.message || 'Failed to add comment');
    } finally {
      setIsSubmittingComment(false);
    }
  };

  if (!task) return null;

  const formatDate = (dateStr: string) => {
    return new Date(dateStr).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
    });
  };

  const formatDateTime = (dateStr: string) => {
    return new Date(dateStr).toLocaleString('en-US', {
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title={task.title} subtitle={`Task ID: #${task.id}`} maxWidth="2xl">
      <div className="space-y-6">
        {/* Overdue alert banner if applicable */}
        {task.isOverdue && (
          <div className="flex items-center gap-2 p-3 bg-rose-50 border border-rose-200 rounded-xl text-rose-700 text-xs font-medium">
            <AlertTriangle className="w-4 h-4 text-rose-500 shrink-0" />
            <span>This task is overdue! The due date was {formatDate(task.dueDate)}.</span>
          </div>
        )}

        {/* Status & Priority Overview Bar */}
        <div className="flex flex-wrap items-center justify-between gap-4 p-4 rounded-xl bg-slate-50 border border-slate-200">
          <div className="flex items-center gap-3">
            <div>
              <span className="text-xs text-slate-400 block mb-1">Priority</span>
              <PriorityBadge priority={task.priority} />
            </div>
            <div className="pl-3 border-l border-slate-200">
              <span className="text-xs text-slate-400 block mb-1">Status</span>
              <StatusBadge status={currentStatus} />
            </div>
          </div>

          {/* Quick status update */}
          <div className="flex items-center gap-2">
            <span className="text-xs font-medium text-slate-600">Change Status:</span>
            <select
              value={currentStatus}
              onChange={(e) => handleStatusChange(e.target.value)}
              disabled={isUpdatingStatus}
              className="text-xs font-medium bg-white border border-slate-300 rounded-lg px-2.5 py-1.5 focus:ring-1 focus:ring-indigo-500 focus:outline-hidden disabled:opacity-50"
            >
              <option value="New">New</option>
              <option value="In Progress">In Progress</option>
              <option value="Completed">Completed</option>
            </select>
          </div>
        </div>

        {/* Description */}
        <div>
          <h4 className="text-xs font-semibold uppercase tracking-wider text-slate-400 mb-2">Description</h4>
          <p className="text-sm text-slate-700 leading-relaxed bg-slate-50/50 p-3.5 rounded-xl border border-slate-100 whitespace-pre-wrap">
            {task.description || 'No description provided for this task.'}
          </p>
        </div>

        {/* Task Metadata Grid */}
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <div className="p-3 rounded-xl border border-slate-200 bg-white">
            <div className="flex items-center gap-2 text-xs text-slate-400 mb-1">
              <Calendar className="w-3.5 h-3.5 text-indigo-500" />
              <span>Timeline</span>
            </div>
            <div className="text-xs text-slate-700">
              <span className="font-semibold">Start:</span> {formatDate(task.startDate)}
            </div>
            <div className="text-xs text-slate-700 mt-0.5">
              <span className="font-semibold">Due:</span> {formatDate(task.dueDate)}
            </div>
          </div>

          <div className="p-3 rounded-xl border border-slate-200 bg-white">
            <div className="flex items-center gap-2 text-xs text-slate-400 mb-1">
              <UserIcon className="w-3.5 h-3.5 text-indigo-500" />
              <span>People</span>
            </div>
            <div className="text-xs text-slate-700">
              <span className="font-semibold">Assigned To:</span> {task.assignedToFullName}
              {task.assignedToDepartment && (
                <span className="text-slate-400"> ({task.assignedToDepartment})</span>
              )}
            </div>
            <div className="text-xs text-slate-700 mt-0.5">
              <span className="font-semibold">Created By:</span> {task.createdByFullName}
            </div>
          </div>
        </div>

        {/* Comments Section */}
        <div className="pt-4 border-t border-slate-200">
          <div className="flex items-center justify-between mb-3">
            <h4 className="text-sm font-semibold text-slate-800 flex items-center gap-2">
              <MessageSquare className="w-4 h-4 text-indigo-600" />
              Discussion & Comments ({comments.length})
            </h4>
          </div>

          {/* Comments list */}
          <div className="space-y-3 mb-4 max-h-60 overflow-y-auto pr-1">
            {isLoadingComments ? (
              <div className="text-center py-6 text-xs text-slate-400">Loading comments...</div>
            ) : comments.length === 0 ? (
              <div className="text-center py-6 text-xs text-slate-400 bg-slate-50 rounded-xl border border-dashed border-slate-200">
                No comments yet. Start the conversation!
              </div>
            ) : (
              comments.map((c) => (
                <div key={c.id} className="p-3 rounded-xl bg-slate-50 border border-slate-200/80 text-xs">
                  <div className="flex items-center justify-between mb-1">
                    <div className="flex items-center gap-2">
                      <span className="font-semibold text-slate-800">{c.userFullName}</span>
                      <RoleBadge role={c.userRole} />
                    </div>
                    <span className="text-[11px] text-slate-400 flex items-center gap-1">
                      <Clock className="w-3 h-3" />
                      {formatDateTime(c.createdDate)}
                    </span>
                  </div>
                  <p className="text-slate-700 leading-relaxed whitespace-pre-wrap mt-1">{c.comment}</p>
                </div>
              ))
            )}
          </div>

          {/* Add comment form */}
          <form onSubmit={handleAddComment} className="flex gap-2">
            <input
              type="text"
              value={newComment}
              onChange={(e) => setNewComment(e.target.value)}
              placeholder="Add your note or progress comment..."
              className="flex-1 text-xs px-3.5 py-2.5 rounded-xl border border-slate-300 focus:outline-hidden focus:ring-1 focus:ring-indigo-500 bg-white"
            />
            <button
              type="submit"
              disabled={isSubmittingComment || !newComment.trim()}
              className="px-4 py-2.5 rounded-xl bg-indigo-600 text-white font-medium text-xs flex items-center gap-1.5 hover:bg-indigo-700 disabled:opacity-50 transition shadow-xs"
            >
              <Send className="w-3.5 h-3.5" />
              <span>{isSubmittingComment ? 'Sending...' : 'Post'}</span>
            </button>
          </form>
        </div>
      </div>
    </Modal>
  );
};
