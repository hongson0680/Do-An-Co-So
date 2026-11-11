import { apiClient } from './client';

export interface ThamDinhTinDung {
  maThamDinh: number;
  maHoSo: number;
  maCanBoThamDinh: number;
  thuNhapXacMinh?: number;
  chiPhiHangThang?: number;
  tyLeNoThuNhap?: number;
  diemThuNhap?: number;
  diemTaiSanBaoDam?: number;
  diemCic?: number;
  ngayTraCuuCic?: string;
  diemRuiRo: number;
  khuyenNgh?: 'DE_NGHI_DUYET' | 'DE_NGHI_TU_CHOI' | 'CAN_BO_SUNG' | 'CHUYEN_HOI_DONG_RUI_RO';
  ghiChu?: string;
  ngayThamDinh: string;
}

export interface CreateThamDinhDTO {
  maHoSo: number;
  thuNhapXacMinh?: number;
  chiPhiHangThang?: number;
  diemThuNhap?: number;
  diemTaiSanBaoDam?: number;
  diemCic?: number;
  ngayTraCuuCic?: string;
  diemRuiRo: number;
  khuyenNgh?: string;
  ghiChu?: string;
}

export const thamDinhApi = {
  getAll: (params?: { pageSize?: number }) =>
    apiClient.get<{ items: ThamDinhTinDung[]; totalCount: number }>('/thamdinh', { params }),

  getByHoSo: (hoSoId: number) =>
    apiClient.get<ThamDinhTinDung>(`/thamdinh/ho-so/${hoSoId}`),

  create: (data: CreateThamDinhDTO) =>
    apiClient.post<ThamDinhTinDung>('/thamdinh', data),
  // Ghi chú: method update() đã bị gỡ vì backend KHÔNG có route
  // PUT /thamdinh/{id} (chỉ có GET/POST) → gọi là 404.
};
