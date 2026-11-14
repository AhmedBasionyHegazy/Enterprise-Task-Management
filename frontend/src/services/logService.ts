import api from './api';
import { ApiResponse, PaginatedResult } from '../types';
import { SystemLogEntry, SystemLogQueryParams } from '../types/enterprise';

export const logService = {
  getSystemLogs: async (params: SystemLogQueryParams = {}): Promise<PaginatedResult<SystemLogEntry>> => {
    const res = await api.get<ApiResponse<PaginatedResult<SystemLogEntry>>>('/logs', { params });
    return res.data.data;
  },
};
