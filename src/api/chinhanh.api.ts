import { apiClient } from './client';

export interface ChiNhanh {
  maChiNhanh: number;
  maChiNhanhHienThi: string;
  tenChiNhanh: string;
  diaChi?: string;
  soDienThoai?: string;
  truongChiNhanh?: string;
  vungDiaLy?: string;
  trangThai: 'HOAT_DONG' | 'DA_DONG' | 'BAO_TRI';
  ngayTao: string;
}

export interface CreateChiNhanhDTO {
  maChiNhanhHienThi: string;
  tenChiNhanh: string;
  diaChi?: string;
  soDienThoai?: string;
  truongChiNhanh?: string;
  vungDiaLy?: string;
  trangThai?: 'HOAT_DONG' | 'DA_DONG' | 'BAO_TRI';
}

export const chiNhanhApi = {
  getAll: () =>
    apiClient.get<ChiNhanh[]>('/chinhanh'),

  getById: (id: number) =>
    apiClient.get<ChiNhanh>(`/chinhanh/${id}`),

  create: (data: CreateChiNhanhDTO) =>
    apiClient.post<ChiNhanh>('/chinhanh', data),

  update: (id: number, data: CreateChiNhanhDTO) =>
    apiClient.put<ChiNhanh>(`/chinhanh/${id}`, data),

  delete: (id: number) =>
    apiClient.delete(`/chinhanh/${id}`),
};
