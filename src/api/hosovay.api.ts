import { apiClient } from './client';

export type TrangThaiHoSo =
  | 'MOI'
  | 'DA_NOP'
  | 'DANG_XEM_XET'
  | 'DU_TAI_LIEU'
  | 'DANG_THAM_DINH'
  | 'CHO_PHE_DUYET'
  | 'DA_PHE_DUYET'
  | 'TU_CHOI'
  | 'DA_LAP_HOP_DONG'
  | 'DA_GIAI_NGAN'
  | 'DA_DONG';

export interface HoSoVay {
  maHoSo: number;
  maHoSoHienThi: string;
  maKhachHang: number;
  tenKhachHang: string;
  maSanPham: number;
  tenSanPham: string;
  soTienDeNghi: number;
  kyHanThang: number;
  mucDich: string;
  trangThai: TrangThaiHoSo;
  ngayTao: string;
  ngayNop?: string;
  maCanBo: number;
  tenCanBo: string;
}

export interface CreateHoSoVayDTO {
  maKhachHang: number;
  maSanPham: number;
  soTienDeNghi: number;
  kyHanThang: number;
  mucDich: string;
}

export interface HoSoVayParams {
  pageNumber?: number;
  pageSize?: number;
  search?: string;
  trangThai?: TrangThaiHoSo;
  maKhachHang?: number;
  tuNgay?: string;
  denNgay?: string;
}

export const hoSoVayApi = {
  getAll: (params?: HoSoVayParams) =>
    apiClient.get<{ items: HoSoVay[]; totalCount: number }>('/hosovay', { params }),

  getById: (id: number) =>
    apiClient.get<HoSoVay>(`/hosovay/${id}`),

  // Ghi chú: getByCustomer() đã bị gỡ vì backend không có route
  // GET /hosovay/khach-hang/{id} → gọi là 404.

  create: (data: CreateHoSoVayDTO) =>
    apiClient.post<HoSoVay>('/hosovay', data),

  updateStatus: (id: number, trangThai: TrangThaiHoSo, ghiChu?: string) =>
    apiClient.put(`/hosovay/${id}/trang-thai`, { trangThai, ghiChu }),

  delete: (id: number) =>
    apiClient.delete(`/hosovay/${id}`),
};
