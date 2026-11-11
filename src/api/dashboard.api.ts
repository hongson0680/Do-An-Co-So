import { apiClient } from './client';

export interface DashboardStats {
  tongKhachHang: number;
  tongHoSoVay: number;
  tongHopDong: number;
  tongGiaiNgan: number;
  hoSoMoi: number;
  hoSoDangXyLy: number;
  hoSoDaDuyet: number;
  hoSoTuChoi: number;
  tienGiaiNganThangNay: number;
  tiLeQuaHan: number;
}

export interface ChartData {
  label: string;
  value: number;
}

export interface HoSoTheoTrangThai {
  trangThai: string;
  soLuong: number;
  tongTien: number;
}

export interface TopKhachHang {
  maKhachHang: number;
  tenKh: string;
  soGiayTo: string;
  tongVay: number;
  soHopDong: number;
}

export const dashboardApi = {
  getStats: () =>
    apiClient.get<DashboardStats>('/dashboard'),

  getHoSoByStatus: () =>
    apiClient.get<HoSoTheoTrangThai[]>('/dashboard/ho-so-theo-trang-thai'),

  getGiaiNganTheoThang: () =>
    apiClient.get<ChartData[]>('/dashboard/giai-ngan-theo-thang'),

  getTopKhachHang: (top = 10) =>
    apiClient.get<TopKhachHang[]>('/dashboard/top-khach-hang', { params: { top } }),
};
