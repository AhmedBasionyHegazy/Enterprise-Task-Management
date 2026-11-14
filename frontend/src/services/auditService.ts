import api from './api';
import { ApiResponse, PaginatedResult } from '../types';
import { AuditLogEntry, AuditQueryParams } from '../types/enterprise';

export const auditService = {
  getLogs: async (params: AuditQueryParams = {}): Promise<PaginatedResult<AuditLogEntry>> => {
    const res = await api.get<ApiResponse<PaginatedResult<AuditLogEntry>>>('/audit', { params });
    return res.data.data;
  },

  getTaskHistory: async (
    taskId: number,
    params: Pick<AuditQueryParams, 'action' | 'from' | 'to' | 'pageNumber' | 'pageSize'> = {}
  ): Promise<PaginatedResult<AuditLogEntry>> => {
    const res = await api.get<ApiResponse<PaginatedResult<AuditLogEntry>>>(`/tasks/${taskId}/history`, { params });
    return res.data.data;
  },

  getActions: async (): Promise<string[]> => {
    const res = await api.get<ApiResponse<string[]>>('/audit/actions');
    return res.data.data;
  },
};
