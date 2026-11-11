import { apiClient } from './client';

export interface NhatKyHeThong {
  maNhatKy: number;
  maNguoiDung?: number;
  tenNguoiDung?: string;
  hanhDong: string;
  tenThucThe: string;
  maThucThe: string;
  giaTriCu?: string;
  giaTriMoi?: string;
  truongThayDoi?: string;
  ipNguon?: string;
  trinhDuyet?: string;
  ngayTao: string;
}

export interface NhatKyParams {
  pageNumber?: number;
  pageSize?: number;
  hanhDong?: string;
  tenThucThe?: string;
  maNguoiDung?: number;
  search?: string;
  tuNgay?: string;
  denNgay?: string;
}

export interface NguoiDungHoatDong {
  maNguoiDung: number;
  tenDangNhap: string;
  hoTen: string;
  soLuotThaoTac: number;
  lanCuoi?: string;
}

export interface NhatKyThongKe {
  tongSoLog: number;
  soLuongHomNay: number;
  soLuongTuanNay: number;
  soLuongThangNay: number;
  theoHanhDong: Record<string, number>;
  theoThucThe: Record<string, number>;
  nguoiDungHoatDong: NguoiDungHoatDong[];
}

export const auditLogApi = {
  getAll: (params?: NhatKyParams) =>
    apiClient.get<{ items: NhatKyHeThong[]; totalCount: number }>('/nhat-ky', { params }),

  getThongKe: () =>
    apiClient.get<NhatKyThongKe>('/nhat-ky/thong-ke'),

  getByEntity: (tenThucThe: string, maThucThe: string) =>
    apiClient.get<NhatKyHeThong[]>(`/nhat-ky/thuc-the/${tenThucThe}/${maThucThe}`),
};

// Định nghĩa các hằng số để dùng cho filter UI
export const AUDIT_ACTIONS = [
  { value: 'LOGIN', label: 'Đăng nhập', color: 'green' },
  { value: 'LOGIN_FAILED', label: 'Đăng nhập thất bại', color: 'red' },
  { value: 'CREATE', label: 'Tạo mới', color: 'blue' },
  { value: 'UPDATE', label: 'Cập nhật', color: 'yellow' },
  { value: 'DELETE', label: 'Xóa', color: 'red' },
  { value: 'STATUS_CHANGE', label: 'Đổi trạng thái', color: 'cyan' },
  { value: 'APPROVE', label: 'Phê duyệt', color: 'green' },
  { value: 'REJECT', label: 'Từ chối', color: 'red' },
  { value: 'APPRAISE', label: 'Thẩm định', color: 'purple' },
  { value: 'DISBURSE', label: 'Giải ngân', color: 'emerald' },
  { value: 'PAYMENT', label: 'Thanh toán', color: 'blue' },
  { value: 'PASSWORD_CHANGE', label: 'Đổi mật khẩu', color: 'gray' },
] as const;

export const AUDIT_ENTITIES = [
  { value: 'KhachHang', label: 'Khách hàng' },
  { value: 'HoSoVay', label: 'Hồ sơ vay' },
  { value: 'HopDongVay', label: 'Hợp đồng vay' },
  { value: 'ThanhToan', label: 'Thanh toán' },
  { value: 'ThamDinhTinDung', label: 'Thẩm định' },
  { value: 'PheDuyet', label: 'Phê duyệt' },
  { value: 'NguoiDung', label: 'Người dùng' },
  { value: 'SanPhamVay', label: 'Sản phẩm vay' },
  { value: 'ChiNhanh', label: 'Chi nhánh' },
  { value: 'VaiTro', label: 'Vai trò' },
] as const;
