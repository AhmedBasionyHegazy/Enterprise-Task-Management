import api from './api';
import { ApiResponse, DashboardSummary } from '../types';

export const dashboardService = {
  getStats: async (): Promise<DashboardSummary> => {
    const res = await api.get<ApiResponse<DashboardSummary>>('/dashboard/stats');
    return res.data.data;
  },
};
