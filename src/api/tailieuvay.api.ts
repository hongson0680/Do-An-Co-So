import { apiClient } from './client';

export interface TaiLieuVay {
  maTaiLieu: number;
  maHoSo: number;
  maHoSoHienThi: string;
  loaiTaiLieu: string;
  tenTapTin: string;
  duongDanTapTin: string;
  phienBan: number;
  trangThaiXacMinh: 'CHO_KIEM_TRA' | 'DA_XAC_MINH' | 'TU_CHOI';
  maNguoiUpload?: number;
  tenNguoiUpload?: string;
  ghiChu?: string;
  ngayTao: string;
  ngayCapNhat: string;
}

export interface CreateTaiLieuVayDTO {
  maHoSo: number;
  loaiTaiLieu: string;
  tenTapTin: string;
  duongDanTapTin: string;
  phienBan?: number;
  trangThaiXacMinh?: 'CHO_KIEM_TRA' | 'DA_XAC_MINH' | 'TU_CHOI';
  ghiChu?: string;
}

export const LOAI_TAI_LIEU = [
  { value: 'CCCD', label: 'CCCD/CMND' },
  { value: 'HO_KHAU', label: 'Sổ hộ khẩu / KT3' },
  { value: 'GIAY_DKKD', label: 'Giấy đăng ký kinh doanh' },
  { value: 'SAO_KE_NGAN_HANG', label: 'Sao kê ngân hàng' },
  { value: 'XAC_NHAN_THU_NHAP', label: 'Xác nhận thu nhập' },
  { value: 'HOP_DONG_LAO_DONG', label: 'Hợp đồng lao động' },
  { value: 'SO_DO_NHA', label: 'Sổ đỏ / Giấy chứng nhận QSDĐ' },
  { value: 'DANG_KY_XE', label: 'Đăng ký xe ô tô' },
  { value: 'BAO_HIEM', label: 'Bảo hiểm' },
  { value: 'BAO_CAO_TAI_CHINH', label: 'Báo cáo tài chính' },
  { value: 'HOA_DON', label: 'Hóa đơn / Chứng từ' },
  { value: 'KHAC', label: 'Khác' },
] as const;

export const TRANG_THAI_XAC_MINH = [
  { value: 'CHO_KIEM_TRA', label: 'Chờ kiểm tra', color: 'gray' },
  { value: 'DA_XAC_MINH', label: 'Đã xác minh', color: 'green' },
  { value: 'TU_CHOI', label: 'Từ chối', color: 'red' },
] as const;

export const taiLieuVayApi = {
  getAll: (params?: {
    maHoSo?: number;
    loaiTaiLieu?: string;
    trangThai?: string;
    pageNumber?: number;
    pageSize?: number;
  }) =>
    apiClient.get<{ items: TaiLieuVay[]; totalCount: number }>('/tai-lieu-vay', { params }),

  getById: (id: number) =>
    apiClient.get<TaiLieuVay>(`/tai-lieu-vay/${id}`),

  create: (data: CreateTaiLieuVayDTO) =>
    apiClient.post<TaiLieuVay>('/tai-lieu-vay', data),

  update: (id: number, data: CreateTaiLieuVayDTO) =>
    apiClient.put(`/tai-lieu-vay/${id}`, data),

  delete: (id: number) =>
    apiClient.delete(`/tai-lieu-vay/${id}`),
};
