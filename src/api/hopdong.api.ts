import { apiClient } from './client';

export interface HopDongVay {
  maHopDong: number;
  soHopDong: string;
  maHoSo: number;
  tienGoc: number;
  laiSuat: number;
  ngayBatDau: string;
  ngayDaoHan: string;
  ngayGiaiNgan?: string;
  tongPhaiTra?: number;
  trangThai: 'HOAT_DONG' | 'QUA_HAN' | 'QUA_HAN_30' | 'QUA_HAN_60' | 'DA_DONG' | 'DA_HUY';
  ghiChu?: string;
}

export interface CreateHopDongDTO {
  maHoSo: number;
  ngayBatDau: string;
  ngayDaoHan: string;
  ghiChu?: string;
}

export const hopDongApi = {
  getAll: (params?: { pageSize?: number }) =>
    apiClient.get<{ items: HopDongVay[]; totalCount: number }>('/hopdong', { params }),

  getById: (id: number) =>
    apiClient.get<HopDongVay>(`/hopdong/${id}`),

  getByHoSo: (hoSoId: number) =>
    apiClient.get<HopDongVay>(`/hopdong/ho-so/${hoSoId}`),

  create: (data: CreateHopDongDTO) =>
    apiClient.post<HopDongVay>('/hopdong', data),

  getLichTraNo: (id: number) =>
    apiClient.get(`/hopdong/${id}/lich-tra-no`),

  giaiNgan: (id: number) =>
    apiClient.post(`/hopdong/${id}/giai-ngan`),
};
