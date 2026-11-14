import { useEffect } from 'react';
import {
  HubConnectionBuilder,
  HubConnectionState,
  ILogger,
  LogLevel,
} from '@microsoft/signalr';
import { useQueryClient } from '@tanstack/react-query';
import { useAuthStore } from '../store/useAuthStore';
import { ensureFreshSession, getSignalRHubUrl } from '../services/api';
import { queryKeys } from '../lib/queryClient';
import { NotificationItem } from '../types/enterprise';
import { toast } from '../store/useToastStore';

const createSignalRLogger = (isDisposed: () => boolean): ILogger => ({
  log(logLevel: LogLevel, message: string) {
    if (logLevel < LogLevel.Warning) return;
    // Ignore transient abort noise during React StrictMode unmount or deliberate teardown.
    if (
      isDisposed() ||
      message.includes('stopped during negotiation') ||
      message.includes('connection was stopped')
    ) {
      return;
    }
    const line = `[${new Date().toISOString()}] ${LogLevel[logLevel]}: ${message}`;
    if (logLevel >= LogLevel.Error) {
      console.error(line);
    } else {
      console.warn(line);
    }
  },
});

export const useRealtimeNotifications = () => {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const isInitializing = useAuthStore((s) => s.isInitializing);
  const qc = useQueryClient();

  useEffect(() => {
    if (!isAuthenticated || isInitializing) return;

    let disposed = false;
    let startPromise: Promise<void> | null = null;

    const connection = new HubConnectionBuilder()
      .withUrl(getSignalRHubUrl(), { withCredentials: true })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(createSignalRLogger(() => disposed))
      .build();

    connection.on('notification', (item: NotificationItem) => {
      qc.setQueryData<number>(queryKeys.unreadCount, (prev) => (prev ?? 0) + 1);
      qc.invalidateQueries({ queryKey: queryKeys.notificationsRoot });

      if (item.entityName === 'Task') {
        qc.invalidateQueries({ queryKey: queryKeys.tasksRoot });
        qc.invalidateQueries({ queryKey: queryKeys.dashboard });
        if (item.entityId) {
          qc.invalidateQueries({ queryKey: queryKeys.taskHistoryRoot(item.entityId) });
          qc.invalidateQueries({ queryKey: queryKeys.taskComments(item.entityId) });
        }
      } else if (item.entityName === 'User') {
        qc.invalidateQueries({ queryKey: queryKeys.users });
        qc.invalidateQueries({ queryKey: queryKeys.employees });
        qc.invalidateQueries({ queryKey: queryKeys.dashboard });
      }

      toast.info(item.message, item.title);
    });

    connection.on('sessionsChanged', () => {
      qc.invalidateQueries({ queryKey: queryKeys.sessions });
    });

    // Defer start by a macrotask so React StrictMode's synchronous double-mount
    // cancels before negotiation begins, eliminating "stopped during negotiation".
    const startTimer = window.setTimeout(() => {
      if (disposed) return;

      startPromise = (async () => {
        const sessionValid = await ensureFreshSession();
        if (disposed || !sessionValid) return;

        try {
          await connection.start();
        } catch {
          if (disposed || !useAuthStore.getState().isAuthenticated) return;
          const refreshed = await ensureFreshSession(true);
          if (disposed || !refreshed) return;
          await connection.start().catch(() => {
            // Reconnect or periodic polling continues gracefully on transient network hiccups.
          });
        }
      })();
    }, 0);

    return () => {
      disposed = true;
      window.clearTimeout(startTimer);

      if (startPromise) {
        // Wait for any in-flight negotiation to settle before stopping cleanly.
        void startPromise
          .catch(() => {})
          .finally(() => {
            if (connection.state !== HubConnectionState.Disconnected) {
              void connection.stop().catch(() => {});
            }
          });
      } else if (connection.state !== HubConnectionState.Disconnected) {
        void connection.stop().catch(() => {});
      }
    };
  }, [isAuthenticated, isInitializing, qc]);
};
