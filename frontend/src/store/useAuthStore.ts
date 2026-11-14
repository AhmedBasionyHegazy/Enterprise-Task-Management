import { create } from 'zustand';
import { LoginRequest, User } from '../types';
import { authService } from '../services/authService';
import {
  clearCsrfToken,
  ensureCsrfToken,
  ensureFreshSession,
  getSessionStorageKey,
  getStoredAccessExpiry,
  isAccessTokenExpiringSoon,
  markSessionLoggedOut,
  performSingleFlightRefresh,
  registerSessionRefreshedHandler,
  registerUnauthorizedHandler,
  setStoredAccessExpiry,
} from '../services/api';
import { queryClient } from '../lib/queryClient';

interface AuthState {
  user: User | null;
  role: 'Admin' | 'Employee' | null;
  isAuthenticated: boolean;
  isInitializing: boolean;
  isLoading: boolean;
  error: string | null;

  login: (credentials: LoginRequest) => Promise<boolean>;
  logout: () => Promise<void>;
  logoutAll: () => Promise<void>;
  checkAuth: () => Promise<void>;
  clearSession: () => void;
  clearError: () => void;
}

// Single-flight lock so StrictMode double-mounts never trigger concurrent /auth/me calls
let checkAuthPromise: Promise<void> | null = null;
let proactiveRefreshTimer: number | null = null;
let lifecycleListenersBound = false;

const clearProactiveRefreshTimer = () => {
  if (proactiveRefreshTimer !== null) {
    window.clearTimeout(proactiveRefreshTimer);
    proactiveRefreshTimer = null;
  }
};

const scheduleProactiveRefresh = (expiresAtIso?: string) => {
  clearProactiveRefreshTimer();
  const ts = expiresAtIso ? Date.parse(expiresAtIso) : getStoredAccessExpiry();
  if (typeof ts !== 'number' || !Number.isFinite(ts) || ts <= 0) return;

  // Refresh 60s before the access cookie expires (minimum 5s)
  const delay = Math.max(5_000, ts - Date.now() - 60_000);
  proactiveRefreshTimer = window.setTimeout(() => {
    if (useAuthStore.getState().isAuthenticated) {
      void ensureFreshSession();
    }
  }, delay);
};

// Cleanup any legacy tokens from earlier localStorage builds on boot
const purgeLegacyTokenStorage = () => {
  try {
    localStorage.removeItem('taskflow_token');
    localStorage.removeItem('taskflow_user');
    sessionStorage.removeItem('taskflow_token');
    sessionStorage.removeItem('taskflow_user');
  } catch {
    // Ignore storage access restrictions
  }
};

export const useAuthStore = create<AuthState>((set) => ({
  user: null,
  role: null,
  isAuthenticated: false,
  isInitializing: true,
  isLoading: false,
  error: null,

  login: async (credentials: LoginRequest) => {
    set({ isLoading: true, error: null });
    try {
      const authData = await authService.login(credentials);
      if (authData.expiresAt) {
        setStoredAccessExpiry(authData.expiresAt);
        scheduleProactiveRefresh(authData.expiresAt);
      }
      queryClient.clear();
      set({
        user: authData.user,
        role: (authData.user.roleName as 'Admin' | 'Employee') || 'Employee',
        isAuthenticated: true,
        isInitializing: false,
        isLoading: false,
        error: null,
      });
      return true;
    } catch (err: any) {
      const msg =
        err.response?.data?.message ||
        'Authentication failed. Check your credentials and try again.';
      set({
        isLoading: false,
        error: msg,
        isAuthenticated: false,
      });
      return false;
    }
  },

  logout: async () => {
    clearProactiveRefreshTimer();
    markSessionLoggedOut();
    try {
      await authService.logout();
    } catch {
      // Clear local session regardless
    }
    purgeLegacyTokenStorage();
    clearCsrfToken();
    queryClient.clear();
    set({
      user: null,
      role: null,
      isAuthenticated: false,
      isInitializing: false,
      error: null,
    });
  },

  logoutAll: async () => {
    clearProactiveRefreshTimer();
    markSessionLoggedOut();
    try {
      await authService.logoutAll();
    } catch {
      // Clear local session regardless
    }
    purgeLegacyTokenStorage();
    clearCsrfToken();
    queryClient.clear();
    set({
      user: null,
      role: null,
      isAuthenticated: false,
      isInitializing: false,
      error: null,
    });
  },

  clearSession: () => {
    clearProactiveRefreshTimer();
    markSessionLoggedOut();
    purgeLegacyTokenStorage();
    clearCsrfToken();
    queryClient.clear();
    set({
      user: null,
      role: null,
      isAuthenticated: false,
      isInitializing: false,
    });
  },

  checkAuth: async () => {
    if (!checkAuthPromise) {
      checkAuthPromise = (async () => {
        purgeLegacyTokenStorage();
        const storedExpiry = getStoredAccessExpiry();
        const onLoginRoute =
          typeof window !== 'undefined' && window.location.pathname === '/login';

        // Never probe /api/auth/me when the user is known logged-out or loading /login fresh.
        if (storedExpiry === 'logged_out' || (storedExpiry === null && onLoginRoute)) {
          clearProactiveRefreshTimer();
          set({
            user: null,
            role: null,
            isAuthenticated: false,
            isInitializing: false,
          });
          return;
        }

        set({ isInitializing: true });
        try {
          // If the access cookie already expired while we have a prior session, refresh first so /auth/me never 401s.
          if (typeof storedExpiry === 'number' && Date.now() >= storedExpiry - 30_000) {
            const refreshed = await performSingleFlightRefresh();
            if (refreshed?.user) {
              scheduleProactiveRefresh(refreshed.expiresAt);
              set({
                user: refreshed.user,
                role: (refreshed.user.roleName as 'Admin' | 'Employee') || 'Employee',
                isAuthenticated: true,
                isInitializing: false,
              });
              return;
            }
            markSessionLoggedOut();
            clearProactiveRefreshTimer();
            set({
              user: null,
              role: null,
              isAuthenticated: false,
              isInitializing: false,
            });
            return;
          }

          await ensureCsrfToken();
          const user = await authService.getCurrentUser();

          // Backfill exact server expiry if loaded with an existing cookie predating tf_session_exp tracking.
          if (storedExpiry === null) {
            const refreshed = await performSingleFlightRefresh();
            scheduleProactiveRefresh(refreshed?.expiresAt);
          } else {
            scheduleProactiveRefresh();
          }

          set({
            user,
            role: (user.roleName as 'Admin' | 'Employee') || 'Employee',
            isAuthenticated: true,
            isInitializing: false,
          });
        } catch {
          markSessionLoggedOut();
          clearProactiveRefreshTimer();
          set({
            user: null,
            role: null,
            isAuthenticated: false,
            isInitializing: false,
          });
        }
      })().finally(() => {
        checkAuthPromise = null;
      });
    }
    return checkAuthPromise;
  },

  clearError: () => set({ error: null }),
}));

registerUnauthorizedHandler(() => {
  useAuthStore.getState().clearSession();
});

registerSessionRefreshedHandler((auth) => {
  scheduleProactiveRefresh(auth.expiresAt);
  if (auth.user) {
    useAuthStore.setState({
      user: auth.user,
      role: (auth.user.roleName as 'Admin' | 'Employee') || 'Employee',
      isAuthenticated: true,
      isInitializing: false,
    });
  }
});

if (typeof window !== 'undefined' && !lifecycleListenersBound) {
  lifecycleListenersBound = true;
  const sessionKey = getSessionStorageKey();

  const refreshIfNeededOnResume = () => {
    if (document.visibilityState === 'visible' && useAuthStore.getState().isAuthenticated) {
      if (isAccessTokenExpiringSoon(60_000)) {
        void ensureFreshSession();
      }
    }
  };

  document.addEventListener('visibilitychange', refreshIfNeededOnResume);
  window.addEventListener('focus', refreshIfNeededOnResume);
  window.addEventListener('storage', (e) => {
    if (e.key === sessionKey && e.newValue === '0' && useAuthStore.getState().isAuthenticated) {
      useAuthStore.getState().clearSession();
    }
  });
}
