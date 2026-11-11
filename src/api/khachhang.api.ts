import { apiClient } from './client';

export interface KhachHang {
  maKhachHang: number;
  loaiKhachHang: 'CA_NHAN' | 'DOANH_NGHIEP';
  hoTen: string;
  soGiayTo: string;
  gioiTinh?: 'Nam' | 'Nữ';
  ngaySinh?: string;
  soDienThoai?: string;
  email?: string;
  diaChi?: string;
  ngheNghiep?: string;
  mucDoRuiRo: 'Thap' | 'TrungBinh' | 'Cao';
  ngayTao: string;
  ngayCapNhat: string;
}

export interface CreateKhachHangDTO {
  loaiKhachHang: 'CA_NHAN' | 'DOANH_NGHIEP';
  hoTen: string;
  soGiayTo: string;
  gioiTinh?: 'Nam' | 'Nữ';
  ngaySinh?: string;
  soDienThoai?: string;
  email?: string;
  diaChi?: string;
  ngheNghiep?: string;
}

export interface KhachHangParams {
  pageNumber?: number;
  pageSize?: number;
  search?: string;
  loaiKhachHang?: string;
  mucDoRuiRo?: string;
}

export const khachHangApi = {
  getAll: (params?: KhachHangParams) =>
    apiClient.get<{ items: KhachHang[]; totalCount: number }>('/khachhang', { params }),

  getById: (id: number) =>
    apiClient.get<KhachHang>(`/khachhang/${id}`),

  create: (data: CreateKhachHangDTO) =>
    apiClient.post<KhachHang>('/khachhang', data),

  update: (id: number, data: CreateKhachHangDTO) =>
    apiClient.put<KhachHang>(`/khachhang/${id}`, data),

  delete: (id: number) =>
    apiClient.delete(`/khachhang/${id}`),
};
