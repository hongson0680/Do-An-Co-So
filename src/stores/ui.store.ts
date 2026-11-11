import { create } from 'zustand';

interface UIState {
  sidebarOpen: boolean;
  loading: boolean;
  notification: {
    open: boolean;
    message: string;
    type: 'success' | 'error' | 'warning' | 'info';
  };
  toggleSidebar: () => void;
  setSidebarOpen: (open: boolean) => void;
  setLoading: (loading: boolean) => void;
  showNotification: (
    message: string,
    type?: 'success' | 'error' | 'warning' | 'info'
  ) => void;
  hideNotification: () => void;
}

export const useUIStore = create<UIState>((set) => ({
  sidebarOpen: true,
  loading: false,
  notification: {
    open: false,
    message: '',
    type: 'info',
  },

  toggleSidebar: () =>
    set((state) => ({ sidebarOpen: !state.sidebarOpen })),

  setSidebarOpen: (open) => set({ sidebarOpen: open }),

  setLoading: (loading) => set({ loading }),

  showNotification: (message, type = 'info') =>
    set({
      notification: {
        open: true,
        message,
        type,
      },
    }),

  hideNotification: () =>
    set((state) => ({
      notification: {
        ...state.notification,
        open: false,
      },
    })),
}));
