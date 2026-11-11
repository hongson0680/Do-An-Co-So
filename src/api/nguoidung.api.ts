import { apiClient } from './client';

export interface NguoiDung {
  maNguoiDung: number;
  tenDangNhap: string;
  hoTen: string;
  email: string;
  soDienThoai?: string;
  maVaiTro: number;
  tenVaiTro: string;
  maChiNhanh: number;
  tenChiNhanh: string;
  trangThai: 'HOAT_DONG' | 'NGUNG_HOAT_DONG' | 'TAM_KHOA';
  lanDangNhapCuoi?: string;
  ngayTao: string;
}

export interface CreateNguoiDungDTO {
  tenDangNhap: string;
  matKhau: string;
  hoTen: string;
  email: string;
  soDienThoai?: string | undefined;
  maVaiTro: number;
  maChiNhanh: number;
}

export const nguoiDungApi = {
  getAll: (params?: { pageSize?: number }) =>
    apiClient.get<{ items: NguoiDung[]; totalCount: number }>('/nguoidung', { params }),

  getById: (id: number) =>
    apiClient.get<NguoiDung>(`/nguoidung/${id}`),

  create: (data: CreateNguoiDungDTO) =>
    apiClient.post<NguoiDung>('/nguoidung', data),

  update: (id: number, data: Partial<CreateNguoiDungDTO>) =>
    apiClient.put<NguoiDung>(`/nguoidung/${id}`, data),

  delete: (id: number) =>
    apiClient.delete(`/nguoidung/${id}`),
};
