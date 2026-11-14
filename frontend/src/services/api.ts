import axios, { AxiosError, InternalAxiosRequestConfig } from 'axios';
import { ApiResponse, AuthResponse, User } from '../types';

const BASE_URL = import.meta.env.VITE_API_URL || import.meta.env.VITE_API_BASE_URL || '/api';
const UNSAFE_METHODS = new Set(['post', 'put', 'patch', 'delete']);
const SESSION_EXP_KEY = 'tf_session_exp';
const LOGGED_OUT_MARKER = '0';
const EXPIRY_REFRESH_BUFFER_MS = 20_000;

// Keep CSRF token only in memory (never localStorage/sessionStorage).
let memoryCsrfToken: string | null = null;
let csrfPromise: Promise<string | null> | null = null;
let refreshPromise: Promise<AuthResponse | null> | null = null;
let unauthorizedHandler: (() => void) | null = null;
let sessionRefreshedHandler: ((auth: AuthResponse) => void) | null = null;

interface RetriableConfig extends InternalAxiosRequestConfig {
  _retryAuth?: boolean;
  _retryCsrf?: boolean;
}

export const registerUnauthorizedHandler = (handler: () => void) => {
  unauthorizedHandler = handler;
};

export const registerSessionRefreshedHandler = (handler: (auth: AuthResponse) => void) => {
  sessionRefreshedHandler = handler;
};

export const getStoredAccessExpiry = (): number | 'logged_out' | null => {
  try {
    const raw = localStorage.getItem(SESSION_EXP_KEY);
    if (raw === null) return null;
    if (raw === LOGGED_OUT_MARKER) return 'logged_out';
    const ts = Number(raw);
    return Number.isFinite(ts) && ts > 0 ? ts : null;
  } catch {
    return null;
  }
};

export const setStoredAccessExpiry = (expiresAtIso: string): void => {
  try {
    const ts = Date.parse(expiresAtIso);
    if (Number.isFinite(ts) && ts > 0) {
      localStorage.setItem(SESSION_EXP_KEY, String(ts));
    }
  } catch {
    // Ignore storage access restrictions
  }
};

export const markSessionLoggedOut = (): void => {
  try {
    localStorage.setItem(SESSION_EXP_KEY, LOGGED_OUT_MARKER);
  } catch {
    // Ignore storage access restrictions
  }
};

export const isAccessTokenExpiringSoon = (bufferMs = EXPIRY_REFRESH_BUFFER_MS): boolean => {
  const exp = getStoredAccessExpiry();
  return typeof exp === 'number' && Date.now() >= exp - bufferMs;
};

export const getSessionStorageKey = (): string => SESSION_EXP_KEY;

export const clearCsrfToken = () => {
  memoryCsrfToken = null;
};

export const getSignalRHubUrl = (): string => {
  if (BASE_URL.startsWith('http://') || BASE_URL.startsWith('https://')) {
    return `${BASE_URL.replace(/\/api\/?$/, '')}/hubs/notifications`;
  }
  return '/hubs/notifications';
};

const rawClient = axios.create({
  baseURL: BASE_URL,
  withCredentials: true,
  timeout: 15000,
  headers: { 'Content-Type': 'application/json' },
});

export async function ensureCsrfToken(forceRefresh = false): Promise<string | null> {
  if (!forceRefresh && memoryCsrfToken) return memoryCsrfToken;
  if (!csrfPromise || forceRefresh) {
    csrfPromise = rawClient
      .get<{ data?: string }>('/auth/csrf')
      .then((res) => {
        memoryCsrfToken = res.data?.data ?? null;
        return memoryCsrfToken;
      })
      .catch(() => null)
      .finally(() => {
        csrfPromise = null;
      });
  }
  return csrfPromise;
}

export async function performSingleFlightRefresh(): Promise<AuthResponse | null> {
  if (!refreshPromise) {
    refreshPromise = (async () => {
      const postRefresh = async (forceCsrf: boolean) => {
        const csrf = await ensureCsrfToken(forceCsrf);
        return rawClient.post<ApiResponse<AuthResponse>>(
          '/auth/refresh',
          {},
          { headers: csrf ? { 'X-CSRF-TOKEN': csrf } : undefined }
        );
      };

      try {
        let res;
        try {
          res = await postRefresh(false);
        } catch (err) {
          const axErr = err as AxiosError<{ errors?: string[] }>;
          if (axErr.response?.status === 403 && axErr.response?.data?.errors?.includes('csrf')) {
            res = await postRefresh(true);
          } else {
            throw err;
          }
        }

        const authData = res.data?.data;
        if (authData?.expiresAt) {
          setStoredAccessExpiry(authData.expiresAt);
          sessionRefreshedHandler?.(authData);
        }
        return authData ?? null;
      } catch (err) {
        const axErr = err as AxiosError;
        // Sibling tab/request rotated the token milliseconds earlier within grace window
        if (axErr.response?.status === 409) {
          await new Promise((r) => setTimeout(r, 120));
          try {
            const meRes = await rawClient.get<ApiResponse<User>>('/auth/me');
            const user = meRes.data?.data;
            if (user) {
              const fallbackAuth: AuthResponse = {
                user,
                expiresAt: new Date(Date.now() + 14 * 60_000).toISOString(),
              };
              setStoredAccessExpiry(fallbackAuth.expiresAt);
              sessionRefreshedHandler?.(fallbackAuth);
              return fallbackAuth;
            }
            return null;
          } catch {
            return null;
          }
        }
        return null;
      } finally {
        refreshPromise = null;
      }
    })();
  }
  return refreshPromise;
}

export async function ensureFreshSession(force = false): Promise<boolean> {
  const exp = getStoredAccessExpiry();
  if (exp === 'logged_out') return false;
  if (!force && typeof exp === 'number' && Date.now() < exp - EXPIRY_REFRESH_BUFFER_MS) {
    return true;
  }
  if (!force && exp === null) {
    return true;
  }
  const refreshed = await performSingleFlightRefresh();
  if (!refreshed) {
    memoryCsrfToken = null;
    markSessionLoggedOut();
    unauthorizedHandler?.();
    return false;
  }
  return true;
}

const api = axios.create({
  baseURL: BASE_URL,
  withCredentials: true,
  headers: { 'Content-Type': 'application/json' },
  timeout: 15000,
});

api.interceptors.request.use(
  async (config) => {
    const method = (config.method || 'get').toLowerCase();
    const url = config.url || '';
    const isAuthControl =
      url.includes('/auth/login') ||
      url.includes('/auth/refresh') ||
      url.includes('/auth/csrf') ||
      url.includes('/auth/logout');

    // Silently refresh an expiring access cookie before hitting protected endpoints to avoid spurious 401s.
    if (!isAuthControl && isAccessTokenExpiringSoon()) {
      const ok = await ensureFreshSession();
      if (!ok) {
        return Promise.reject(new axios.CanceledError('Session expired before request dispatch.'));
      }
    }

    const isCsrfExempt = url.includes('/auth/login');
    if (UNSAFE_METHODS.has(method) && !isCsrfExempt) {
      const token = await ensureCsrfToken();
      if (token && config.headers) {
        config.headers['X-CSRF-TOKEN'] = token;
      }
    }
    return config;
  },
  (error) => Promise.reject(error)
);

api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError<{ message?: string; errors?: string[] }>) => {
    const original = error.config as RetriableConfig | undefined;
    const status = error.response?.status;
    const url = original?.url || '';

    // Auto-recover CSRF mismatch once (e.g., server restart or cookie rotation across tabs)
    if (
      status === 403 &&
      original &&
      !original._retryCsrf &&
      error.response?.data?.errors?.includes('csrf')
    ) {
      original._retryCsrf = true;
      const freshCsrf = await ensureCsrfToken(true);
      if (freshCsrf && original.headers) {
        original.headers['X-CSRF-TOKEN'] = freshCsrf;
      }
      return api(original);
    }

    const skipAuthRefresh =
      url.includes('/auth/login') ||
      url.includes('/auth/refresh') ||
      url.includes('/auth/csrf');

    if (status === 401 && original && !original._retryAuth && !skipAuthRefresh) {
      original._retryAuth = true;
      const refreshed = await performSingleFlightRefresh();
      if (refreshed) {
        return api(original);
      }
      memoryCsrfToken = null;
      markSessionLoggedOut();
      unauthorizedHandler?.();
    }

    return Promise.reject(error);
  }
);

export default api;
