import { apiClient } from './client';

export interface SanPhamVay {
  maSanPham: number;
  maSanPhamVay: string;
  tenSanPham: string;
  moTa?: string;
  laiSuat: number;
  kyHanToiThieu: number;
  kyHanToiDa: number;
  hanMucToiThieu: number;
  hanMucToiDa?: number;
  doiTuongKh?: string;
  yeuCauTsdb: boolean;
  trangThai: 'HOAT_DONG' | 'NGUNG_HOAT_DONG' | 'NGUNG_CUNG_CAP';
  ngayTao: string;
}

export interface CreateSanPhamVayDTO {
  maSanPhamVay: string;
  tenSanPham: string;
  moTa?: string;
  laiSuat: number;
  kyHanToiThieu: number;
  kyHanToiDa: number;
  hanMucToiThieu: number;
  hanMucToiDa?: number;
  doiTuongKh?: string;
  yeuCauTsdb: boolean;
}

export const sanPhamVayApi = {
  getAll: (params?: { pageSize?: number }) =>
    apiClient.get<{ items: SanPhamVay[]; totalCount: number }>('/sanphamvay', { params }),

  getById: (id: number) =>
    apiClient.get<SanPhamVay>(`/sanphamvay/${id}`),

  create: (data: CreateSanPhamVayDTO) =>
    apiClient.post<SanPhamVay>('/sanphamvay', data),

  update: (id: number, data: CreateSanPhamVayDTO) =>
    apiClient.put<SanPhamVay>(`/sanphamvay/${id}`, data),

  delete: (id: number) =>
    apiClient.delete(`/sanphamvay/${id}`),
};
