import { QueryClient } from '@tanstack/react-query';

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      gcTime: 5 * 60_000,
      retry: (failureCount, error: any) => {
        if (error?.name === 'CanceledError' || error?.code === 'ERR_CANCELED') return false;
        const status = error?.response?.status;
        if (status && [400, 401, 403, 404, 429].includes(status)) return false;
        return failureCount < 1;
      },
      refetchOnWindowFocus: false,
    },
    mutations: {
      retry: false,
    },
  },
});

export const queryKeys = {
  dashboard: ['dashboard'] as const,
  tasks: (params: Record<string, any>) => ['tasks', params] as const,
  tasksRoot: ['tasks'] as const,
  taskHistory: (taskId: number, params: Record<string, any>) => ['task-history', taskId, params] as const,
  taskHistoryRoot: (taskId: number) => ['task-history', taskId] as const,
  taskComments: (taskId: number) => ['task-comments', taskId] as const,
  users: ['users'] as const,
  employees: ['employees'] as const,
  roles: ['roles'] as const,
  sessions: ['sessions'] as const,
  audit: (params: Record<string, any>) => ['audit', params] as const,
  auditActions: ['audit-actions'] as const,
  logs: (params: Record<string, any>) => ['system-logs', params] as const,
  notifications: (params: Record<string, any>) => ['notifications', params] as const,
  notificationsRoot: ['notifications'] as const,
  unreadCount: ['notifications-unread'] as const,
  notificationPreferences: ['notification-preferences'] as const,
};
