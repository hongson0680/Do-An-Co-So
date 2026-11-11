import { apiClient } from './client';

export interface LichTraNo {
  maLichTraNo: number;
  maHopDong: number;
  kyThanhToan: number;
  ngayDenHan: string;
  gocPhaiTra: number;
  laiPhaiTra: number;
  phiPhat: number;
  trangThai: 'CHUA_THANH_TOAN' | 'DA_THANH_TOAN' | 'TRE_HAN' | 'THANH_TOAN_MOT_PHAN';
  hanThanhToan?: string;
}

export interface ThanhToan {
  maThanhToan: number;
  maLichTraNo: number;
  maHopDong: number;
  maHoSo: number;
  soHopDong?: string;
  maHoSoHienThi?: string;
  soTienDaTra: number;
  soTienApGoc: number;
  soTienApLai: number;
  soTienApPhi: number;
  ngayThanhToan: string;
  phuongThucThanhToan?: string;
  maGiaoDich?: string;
  kenhThanhToan?: string;
  nguoiThu?: number;
  ghiChu?: string;
}

export interface CreateThanhToanDTO {
  maLichTraNo: number;
  maHopDong: number;
  maHoSo: number;
  soTienDaTra: number;
  soTienApGoc: number;
  soTienApLai: number;
  soTienApPhi: number;
  ngayThanhToan: string;
  phuongThucThanhToan?: string;
  maGiaoDich?: string;
  kenhThanhToan?: string;
  ghiChu?: string;
}

export const thanhToanApi = {
  getAll: (params?: { pageSize?: number }) =>
    apiClient.get<{ items: ThanhToan[]; totalCount: number }>('/thanhtoan', { params }),

  getByHopDong: (hopDongId: number) =>
    apiClient.get<ThanhToan[]>(`/thanhtoan/hop-dong/${hopDongId}`),

  // Ghi chú: getByLichTraNo() đã bị gỡ vì backend không có route
  // GET /thanhtoan/lich-tra-no/{id} → gọi là 404.

  create: (data: CreateThanhToanDTO) =>
    apiClient.post<ThanhToan>('/thanhtoan', data),
};
