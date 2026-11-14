import api from './api';
import { ApiResponse, PaginatedResult } from '../types';
import { NotificationItem, NotificationPreference, NotificationQueryParams } from '../types/enterprise';

export const notificationService = {
  getNotifications: async (params: NotificationQueryParams = {}): Promise<PaginatedResult<NotificationItem>> => {
    const res = await api.get<ApiResponse<PaginatedResult<NotificationItem>>>('/notifications', { params });
    return res.data.data;
  },

  getUnreadCount: async (): Promise<number> => {
    const res = await api.get<ApiResponse<number>>('/notifications/unread-count');
    return res.data.data;
  },

  markRead: async (id: number): Promise<boolean> => {
    const res = await api.post<ApiResponse<boolean>>(`/notifications/${id}/read`);
    return res.data.data;
  },

  markAllRead: async (): Promise<number> => {
    const res = await api.post<ApiResponse<number>>('/notifications/read-all');
    return res.data.data;
  },

  getPreferences: async (): Promise<NotificationPreference[]> => {
    const res = await api.get<ApiResponse<NotificationPreference[]>>('/notifications/preferences');
    return res.data.data;
  },

  setPreference: async (category: string, inAppEnabled: boolean): Promise<NotificationPreference> => {
    const res = await api.put<ApiResponse<NotificationPreference>>(`/notifications/preferences/${encodeURIComponent(category)}`, {
      inAppEnabled,
    });
    return res.data.data;
  },
};
