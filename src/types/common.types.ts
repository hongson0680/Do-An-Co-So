// Common types used across the application

export interface PaginationParams {
  pageNumber?: number;
  pageSize?: number;
}

export interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

export interface ApiError {
  message: string;
  errors?: Record<string, string[]>;
  statusCode: number;
}

export interface SelectOption {
  value: string | number;
  label: string;
}

export type TrangThai =
  | 'HOAT_DONG'
  | 'NGUNG_HOAT_DONG'
  | 'DA_DONG'
  | 'TAM_KHOA'
  | 'BAO_TRI'
  | 'HUY'
  | 'CHO_KIEM_TRA'
  | 'DA_XAC_MINH'
  | 'TU_CHOI';

export interface User {
  maNguoiDung: number;
  tenDangNhap: string;
  hoTen: string;
  email: string;
  maVaiTro: string;
  tenVaiTro: string;
  maChiNhanh: number;
  tenChiNhanh: string;
}

export interface Notification {
  maThongBao: number;
  tieuDe: string;
  noiDung: string;
  loai: 'SMS' | 'EMAIL' | 'PUSH' | 'PAGE' | 'INAPP';
  daDoc: boolean;
  ngayGui: string;
  maHoSo?: number;
}
