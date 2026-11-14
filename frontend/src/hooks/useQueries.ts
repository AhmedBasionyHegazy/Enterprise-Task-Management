import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { queryKeys } from '../lib/queryClient';
import { taskService } from '../services/taskService';
import { dashboardService } from '../services/dashboardService';
import { userService } from '../services/userService';
import { authService } from '../services/authService';
import { auditService } from '../services/auditService';
import { notificationService } from '../services/notificationService';
import { logService } from '../services/logService';
import {
  PaginatedResult,
  TaskCreatePayload,
  TaskItem,
  TaskQueryParams,
  TaskUpdatePayload,
  UserCreatePayload,
  UserUpdatePayload,
} from '../types';
import {
  AuditQueryParams,
  NotificationItem,
  NotificationQueryParams,
  SystemLogQueryParams,
} from '../types/enterprise';

export const useDashboardQuery = (enabled = true) =>
  useQuery({
    queryKey: queryKeys.dashboard,
    queryFn: dashboardService.getStats,
    enabled,
  });

export const useTasksQuery = (params: TaskQueryParams, enabled = true) =>
  useQuery({
    queryKey: queryKeys.tasks(params as Record<string, any>),
    queryFn: () => taskService.getTasks(params),
    placeholderData: keepPreviousData,
    enabled,
  });

export const useTaskCommentsQuery = (taskId: number | null, enabled = true) =>
  useQuery({
    queryKey: queryKeys.taskComments(taskId ?? 0),
    queryFn: () => taskService.getComments(taskId!),
    enabled: enabled && !!taskId,
  });

export const useTaskHistoryQuery = (
  taskId: number | null,
  params: Pick<AuditQueryParams, 'action' | 'from' | 'to' | 'pageNumber' | 'pageSize'>,
  enabled = true
) =>
  useQuery({
    queryKey: queryKeys.taskHistory(taskId ?? 0, params as Record<string, any>),
    queryFn: () => auditService.getTaskHistory(taskId!, params),
    placeholderData: keepPreviousData,
    enabled: enabled && !!taskId,
  });

export const useEmployeesQuery = (enabled = true) =>
  useQuery({
    queryKey: queryKeys.employees,
    queryFn: userService.getEmployees,
    enabled,
    staleTime: 2 * 60_000,
  });

export const useUsersQuery = (enabled = true) =>
  useQuery({
    queryKey: queryKeys.users,
    queryFn: userService.getAllUsers,
    enabled,
  });

export const useRolesQuery = (enabled = true) =>
  useQuery({
    queryKey: queryKeys.roles,
    queryFn: userService.getRoles,
    enabled,
    staleTime: 10 * 60_000,
  });

export const useSessionsQuery = (enabled = true) =>
  useQuery({
    queryKey: queryKeys.sessions,
    queryFn: authService.getSessions,
    enabled,
  });

export const useAuditLogsQuery = (params: AuditQueryParams, enabled = true) =>
  useQuery({
    queryKey: queryKeys.audit(params as Record<string, any>),
    queryFn: () => auditService.getLogs(params),
    placeholderData: keepPreviousData,
    enabled,
  });

export const useAuditActionsQuery = (enabled = true) =>
  useQuery({
    queryKey: queryKeys.auditActions,
    queryFn: auditService.getActions,
    enabled,
    staleTime: 30 * 60_000,
  });

export const useSystemLogsQuery = (params: SystemLogQueryParams, enabled = true) =>
  useQuery({
    queryKey: queryKeys.logs(params as Record<string, any>),
    queryFn: () => logService.getSystemLogs(params),
    placeholderData: keepPreviousData,
    enabled,
  });

export const useNotificationsQuery = (params: NotificationQueryParams, enabled = true) =>
  useQuery({
    queryKey: queryKeys.notifications(params as Record<string, any>),
    queryFn: () => notificationService.getNotifications(params),
    enabled,
  });

export const useUnreadCountQuery = (enabled = true) =>
  useQuery({
    queryKey: queryKeys.unreadCount,
    queryFn: notificationService.getUnreadCount,
    enabled,
    refetchInterval: 60_000,
  });

export const useNotificationPreferencesQuery = (enabled = true) =>
  useQuery({
    queryKey: queryKeys.notificationPreferences,
    queryFn: notificationService.getPreferences,
    enabled,
  });

export const useCreateTaskMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: TaskCreatePayload) => taskService.createTask(payload),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKeys.tasksRoot });
      qc.invalidateQueries({ queryKey: queryKeys.dashboard });
      qc.invalidateQueries({ queryKey: ['audit'] });
    },
  });
};

export const useUpdateTaskMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, payload }: { id: number; payload: TaskUpdatePayload }) =>
      taskService.updateTask(id, payload),
    onSuccess: (_, vars) => {
      qc.invalidateQueries({ queryKey: queryKeys.tasksRoot });
      qc.invalidateQueries({ queryKey: queryKeys.dashboard });
      qc.invalidateQueries({ queryKey: queryKeys.taskHistoryRoot(vars.id) });
      qc.invalidateQueries({ queryKey: ['audit'] });
    },
  });
};

export const useDeleteTaskMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => taskService.deleteTask(id),
    onSuccess: (_, id) => {
      qc.invalidateQueries({ queryKey: queryKeys.tasksRoot });
      qc.invalidateQueries({ queryKey: queryKeys.dashboard });
      qc.invalidateQueries({ queryKey: queryKeys.taskHistoryRoot(id) });
      qc.invalidateQueries({ queryKey: ['audit'] });
    },
  });
};

export const useUpdateTaskStatusMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, status }: { id: number; status: string }) =>
      taskService.updateTaskStatus(id, status),
    onMutate: async ({ id, status }) => {
      await qc.cancelQueries({ queryKey: queryKeys.tasksRoot });
      const previous = qc.getQueriesData<PaginatedResult<TaskItem>>({ queryKey: queryKeys.tasksRoot });
      qc.setQueriesData<PaginatedResult<TaskItem>>({ queryKey: queryKeys.tasksRoot }, (old) => {
        if (!old) return old;
        return {
          ...old,
          items: old.items.map((t) =>
            t.id === id ? { ...t, status: status as TaskItem['status'] } : t
          ),
        };
      });
      return { previous };
    },
    onError: (_err, _vars, ctx) => {
      ctx?.previous?.forEach(([key, data]) => qc.setQueryData(key, data));
    },
    onSettled: (_, __, vars) => {
      qc.invalidateQueries({ queryKey: queryKeys.tasksRoot });
      qc.invalidateQueries({ queryKey: queryKeys.dashboard });
      qc.invalidateQueries({ queryKey: queryKeys.taskHistoryRoot(vars.id) });
      qc.invalidateQueries({ queryKey: ['audit'] });
    },
  });
};

export const useAddCommentMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ taskId, comment }: { taskId: number; comment: string }) =>
      taskService.addComment(taskId, comment),
    onSuccess: (_, vars) => {
      qc.invalidateQueries({ queryKey: queryKeys.taskComments(vars.taskId) });
      qc.invalidateQueries({ queryKey: queryKeys.tasksRoot });
      qc.invalidateQueries({ queryKey: queryKeys.taskHistoryRoot(vars.taskId) });
      qc.invalidateQueries({ queryKey: ['audit'] });
    },
  });
};

export const useCreateUserMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: UserCreatePayload) => userService.createUser(payload),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKeys.users });
      qc.invalidateQueries({ queryKey: queryKeys.employees });
      qc.invalidateQueries({ queryKey: queryKeys.dashboard });
      qc.invalidateQueries({ queryKey: ['audit'] });
    },
  });
};

export const useUpdateUserMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, payload }: { id: number; payload: UserUpdatePayload }) =>
      userService.updateUser(id, payload),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKeys.users });
      qc.invalidateQueries({ queryKey: queryKeys.employees });
      qc.invalidateQueries({ queryKey: queryKeys.dashboard });
      qc.invalidateQueries({ queryKey: ['audit'] });
    },
  });
};

export const useToggleUserStatusMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => userService.toggleUserStatus(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKeys.users });
      qc.invalidateQueries({ queryKey: queryKeys.employees });
      qc.invalidateQueries({ queryKey: queryKeys.dashboard });
      qc.invalidateQueries({ queryKey: ['audit'] });
    },
  });
};

export const useRevokeSessionMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => authService.revokeSession(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKeys.sessions });
    },
  });
};

export const useMarkNotificationReadMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => notificationService.markRead(id),
    onMutate: async (id) => {
      await qc.cancelQueries({ queryKey: queryKeys.notificationsRoot });
      const prevCount = qc.getQueryData<number>(queryKeys.unreadCount);
      if (typeof prevCount === 'number' && prevCount > 0) {
        qc.setQueryData<number>(queryKeys.unreadCount, prevCount - 1);
      }
      qc.setQueriesData<PaginatedResult<NotificationItem>>(
        { queryKey: queryKeys.notificationsRoot },
        (old) =>
          old
            ? { ...old, items: old.items.map((n) => (n.id === id ? { ...n, isRead: true } : n)) }
            : old
      );
      return { prevCount };
    },
    onError: (_err, _id, ctx) => {
      if (ctx?.prevCount !== undefined) qc.setQueryData(queryKeys.unreadCount, ctx.prevCount);
    },
    onSettled: () => {
      qc.invalidateQueries({ queryKey: queryKeys.notificationsRoot });
      qc.invalidateQueries({ queryKey: queryKeys.unreadCount });
    },
  });
};

export const useMarkAllNotificationsReadMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: notificationService.markAllRead,
    onMutate: async () => {
      const prevCount = qc.getQueryData<number>(queryKeys.unreadCount);
      qc.setQueryData<number>(queryKeys.unreadCount, 0);
      return { prevCount };
    },
    onError: (_err, _vars, ctx) => {
      if (ctx?.prevCount !== undefined) qc.setQueryData(queryKeys.unreadCount, ctx.prevCount);
    },
    onSettled: () => {
      qc.invalidateQueries({ queryKey: queryKeys.notificationsRoot });
      qc.invalidateQueries({ queryKey: queryKeys.unreadCount });
    },
  });
};

export const useUpdateNotificationPreferenceMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ category, inAppEnabled }: { category: string; inAppEnabled: boolean }) =>
      notificationService.setPreference(category, inAppEnabled),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKeys.notificationPreferences });
    },
  });
};
