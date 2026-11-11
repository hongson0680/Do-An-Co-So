import axios from 'axios';

// Use environment variable or fallback to localhost for development
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5071/api';

export const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 30000,
});

// Helper to get token from zustand persist storage
const getToken = (): string | null => {
  const stored = localStorage.getItem('auth-storage');
  if (stored) {
    try {
      const parsed = JSON.parse(stored);
      return parsed.state?.token || null;
    } catch {
      return null;
    }
  }
  return null;
};

// Request interceptor - Add auth token
apiClient.interceptors.request.use(
  (config) => {
    const token = getToken();
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Response interceptor: access token hết hạn → tự refresh nền, retry request gốc.
// Chỉ đá về /login khi refresh token cũng thất bại (đúng 401 không phải request
// /auth/refresh). Trước đây không có refresh, user bị đá ra mỗi 24 giờ.
let isRefreshing = false;
let refreshSubscribers: Array<(token: string) => void> = [];

const onRefreshed = (token: string) => {
  refreshSubscribers.forEach((cb) => cb(token));
  refreshSubscribers = [];
};

apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;
    const isAuthCall = originalRequest?.url?.includes('/auth/');

    if (error.response?.status === 401 && !originalRequest._retry && !isAuthCall) {
      if (isRefreshing) {
        // Một request khác đang refresh → chờ token mới rồi retry
        return new Promise((resolve) => {
          refreshSubscribers.push((token: string) => {
            originalRequest.headers.Authorization = `Bearer ${token}`;
            resolve(apiClient(originalRequest));
          });
        });
      }

      originalRequest._retry = true;
      isRefreshing = true;

      const stored = localStorage.getItem('auth-storage');
      let refreshToken: string | null = null;
      try {
        refreshToken = JSON.parse(stored || 'null')?.state?.refreshToken ?? null;
      } catch {
        refreshToken = null;
      }

      try {
        if (!refreshToken) throw new Error('No refresh token');

        const { data } = await axios.post(
          `${API_BASE_URL}/auth/refresh`,
          { refreshToken },
          { headers: { 'Content-Type': 'application/json' } }
        );

        const newToken = data.token;
        localStorage.setItem(
          'auth-storage',
          JSON.stringify({
            state: { ...JSON.parse(stored || '{}').state, token: newToken, refreshToken: data.refreshToken },
            version: 0,
          })
        );

        onRefreshed(newToken);
        originalRequest.headers.Authorization = `Bearer ${newToken}`;
        return apiClient(originalRequest);
      } catch {
        // Refresh thất bại → token cũ, đăng nhập lại
        localStorage.removeItem('auth-storage');
        window.location.href = '/login';
      } finally {
        isRefreshing = false;
      }
    }

    if (error.response?.status === 401 && isAuthCall) {
      // Login/profile thất bại: chỉ xóa state local, không redirect
      // (tránh vòng lặp /login → 401 → /login)
      localStorage.removeItem('auth-storage');
    }

    return Promise.reject(error);
  }
);

export default apiClient;
