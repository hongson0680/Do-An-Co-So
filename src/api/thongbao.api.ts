import { apiClient } from './client';

export interface ThongBao {
  maThongBao: number;
  maNguoiNhan: number;
  loaiNguoiNhan: string;
  tieuDe: string;
  noiDung: string;
  loai: 'INAPP' | 'EMAIL' | 'SMS' | 'PUSH' | 'PAGE';
  daDoc: boolean;
  maHoSo?: number;
  maHoSoHienThi?: string;
  ngayGui: string;
}

export interface ThongBaoThongKe {
  tongSoThongBao: number;
  chuaDoc: number;
  theoLoai: Record<string, number>;
}

export const thongBaoApi = {
  getAll: (params?: { pageNumber?: number; pageSize?: number; chuaDoc?: boolean }) =>
    apiClient.get<{ items: ThongBao[]; totalCount: number }>('/thong-bao', { params }),

  getRecent: (take = 10) =>
    apiClient.get<ThongBao[]>(`/thong-bao/recent`, { params: { take } }),

  getUnreadCount: () =>
    apiClient.get<{ unread: number }>('/thong-bao/unread-count'),

  markAsRead: (id: number) =>
    apiClient.put(`/thong-bao/${id}/read`),

  markAllAsRead: () =>
    apiClient.put('/thong-bao/read-all'),

  delete: (id: number) =>
    apiClient.delete(`/thong-bao/${id}`),

  getThongKe: () =>
    apiClient.get<ThongBaoThongKe>('/thong-bao/thong-ke'),
};
