import api from './api';
import { 
  ApiResponse, 
  PaginatedResult, 
  TaskItem, 
  TaskComment, 
  TaskQueryParams, 
  TaskCreatePayload, 
  TaskUpdatePayload 
} from '../types';

export const taskService = {
  getTasks: async (params?: TaskQueryParams): Promise<PaginatedResult<TaskItem>> => {
    const res = await api.get<ApiResponse<PaginatedResult<TaskItem>>>('/tasks', { params });
    return res.data.data;
  },

  getTaskById: async (id: number): Promise<TaskItem> => {
    const res = await api.get<ApiResponse<TaskItem>>(`/tasks/${id}`);
    return res.data.data;
  },

  createTask: async (payload: TaskCreatePayload): Promise<TaskItem> => {
    const res = await api.post<ApiResponse<TaskItem>>('/tasks', payload);
    return res.data.data;
  },

  updateTask: async (id: number, payload: TaskUpdatePayload): Promise<TaskItem> => {
    const res = await api.put<ApiResponse<TaskItem>>(`/tasks/${id}`, payload);
    return res.data.data;
  },

  deleteTask: async (id: number): Promise<boolean> => {
    const res = await api.delete<ApiResponse<boolean>>(`/tasks/${id}`);
    return res.data.data;
  },

  updateTaskStatus: async (id: number, status: string): Promise<TaskItem> => {
    const res = await api.patch<ApiResponse<TaskItem>>(`/tasks/${id}/status`, { status });
    return res.data.data;
  },

  getComments: async (taskId: number): Promise<TaskComment[]> => {
    const res = await api.get<ApiResponse<TaskComment[]>>(`/tasks/${taskId}/comments`);
    return res.data.data;
  },

  addComment: async (taskId: number, comment: string): Promise<TaskComment> => {
    const res = await api.post<ApiResponse<TaskComment>>(`/tasks/${taskId}/comments`, {
      taskId,
      comment
    });
    return res.data.data;
  },
};
