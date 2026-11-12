import api from './api';
import { ApiResponse, Role, User, UserCreatePayload, UserUpdatePayload } from '../types';

export const userService = {
  getAllUsers: async (): Promise<User[]> => {
    const res = await api.get<ApiResponse<User[]>>('/users');
    return res.data.data;
  },

  getEmployees: async (): Promise<User[]> => {
    const res = await api.get<ApiResponse<User[]>>('/users/employees');
    return res.data.data;
  },

  getUserById: async (id: number): Promise<User> => {
    const res = await api.get<ApiResponse<User>>(`/users/${id}`);
    return res.data.data;
  },

  createUser: async (payload: UserCreatePayload): Promise<User> => {
    const res = await api.post<ApiResponse<User>>('/users', payload);
    return res.data.data;
  },

  updateUser: async (id: number, payload: UserUpdatePayload): Promise<User> => {
    const res = await api.put<ApiResponse<User>>(`/users/${id}`, payload);
    return res.data.data;
  },

  toggleUserStatus: async (id: number): Promise<boolean> => {
    const res = await api.patch<ApiResponse<boolean>>(`/users/${id}/toggle-status`);
    return res.data.data;
  },

  getRoles: async (): Promise<Role[]> => {
    const res = await api.get<ApiResponse<Role[]>>('/users/roles');
    return res.data.data;
  },
};
