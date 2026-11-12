import { create } from 'zustand';
import { User, LoginRequest } from '../types';
import { authService } from '../services/authService';

interface AuthState {
  user: User | null;
  token: string | null;
  role: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: string | null;
  login: (credentials: LoginRequest) => Promise<boolean>;
  logout: () => void;
  checkAuth: () => Promise<void>;
  setUser: (user: User) => void;
}

export const useAuthStore = create<AuthState>((set) => ({
  user: (() => {
    try {
      const stored = localStorage.getItem('taskflow_user');
      return stored ? JSON.parse(stored) : null;
    } catch {
      return null;
    }
  })(),
  token: localStorage.getItem('taskflow_token') || null,
  role: (() => {
    try {
      const stored = localStorage.getItem('taskflow_user');
      return stored ? JSON.parse(stored).roleName : null;
    } catch {
      return null;
    }
  })(),
  isAuthenticated: !!localStorage.getItem('taskflow_token'),
  isLoading: false,
  error: null,

  login: async (credentials: LoginRequest) => {
    set({ isLoading: true, error: null });
    try {
      const auth = await authService.login(credentials);
      localStorage.setItem('taskflow_token', auth.token);
      localStorage.setItem('taskflow_user', JSON.stringify(auth.user));
      set({
        user: auth.user,
        token: auth.token,
        role: auth.user.roleName,
        isAuthenticated: true,
        isLoading: false,
      });
      return true;
    } catch (err: any) {
      const message =
        err.response?.data?.message || err.message || 'Login failed. Please check your credentials.';
      set({ error: message, isLoading: false });
      return false;
    }
  },

  logout: () => {
    localStorage.removeItem('taskflow_token');
    localStorage.removeItem('taskflow_user');
    set({
      user: null,
      token: null,
      role: null,
      isAuthenticated: false,
      error: null,
    });
  },

  checkAuth: async () => {
    const token = localStorage.getItem('taskflow_token');
    if (!token) {
      set({ isAuthenticated: false, user: null, role: null });
      return;
    }
    try {
      const currentUser = await authService.getCurrentUser();
      localStorage.setItem('taskflow_user', JSON.stringify(currentUser));
      set({
        user: currentUser,
        role: currentUser.roleName,
        isAuthenticated: true,
      });
    } catch {
      localStorage.removeItem('taskflow_token');
      localStorage.removeItem('taskflow_user');
      set({ isAuthenticated: false, user: null, role: null });
    }
  },

  setUser: (user: User) => {
    localStorage.setItem('taskflow_user', JSON.stringify(user));
    set({ user, role: user.roleName });
  },
}));
