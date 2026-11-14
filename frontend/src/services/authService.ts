import api, { clearCsrfToken, ensureCsrfToken } from './api';
import { ApiResponse, AuthResponse, LoginRequest, User } from '../types';
import { SessionInfo } from '../types/enterprise';

export const authService = {
  login: async (payload: LoginRequest): Promise<AuthResponse> => {
    const res = await api.post<ApiResponse<AuthResponse>>('/auth/login', payload);
    // Sync our in-memory CSRF token with the newly issued session cookie.
    clearCsrfToken();
    await ensureCsrfToken(true);
    return res.data.data;
  },

  refresh: async (): Promise<AuthResponse> => {
    const res = await api.post<ApiResponse<AuthResponse>>('/auth/refresh');
    return res.data.data;
  },

  logout: async (): Promise<void> => {
    try {
      await api.post('/auth/logout');
    } finally {
      clearCsrfToken();
    }
  },

  logoutAll: async (): Promise<number> => {
    try {
      const res = await api.post<ApiResponse<number>>('/auth/logout-all');
      return res.data.data;
    } finally {
      clearCsrfToken();
    }
  },

  getCurrentUser: async (): Promise<User> => {
    const res = await api.get<ApiResponse<User>>('/auth/me');
    return res.data.data;
  },

  getSessions: async (): Promise<SessionInfo[]> => {
    const res = await api.get<ApiResponse<SessionInfo[]>>('/auth/sessions');
    return res.data.data;
  },

  revokeSession: async (id: string): Promise<boolean> => {
    const res = await api.delete<ApiResponse<boolean>>(`/auth/sessions/${encodeURIComponent(id)}`);
    return res.data.data;
  },
};
