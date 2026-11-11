import { apiClient } from './client';

export interface TaiSanDamBao {
  maTaiSan: number;
  maHoSo: number;
  maHoSoHienThi: string;
  loaiTaiSan: string;
  chuSoHuu?: string;
  soGiayChungNhan?: string;
  diaChiTaiSan?: string;
  giaTriUocTinh: number;
  tyLeVayGiaTriTS: number;
  tinhTrangPhapLy?: string;
  ngayDinhGia?: string;
  donViDinhGia?: string;
  soHopDongBaoHiem?: string;
  ngayHetHanBaoHiem?: string;
  ngayTao: string;
  ngayCapNhat: string;
  giaTriChoVayToiDa: number;
}

export interface CreateTaiSanDamBaoDTO {
  maHoSo: number;
  loaiTaiSan: string;
  chuSoHuu?: string;
  soGiayChungNhan?: string;
  diaChiTaiSan?: string;
  giaTriUocTinh: number;
  tyLeVayGiaTriTS: number;
  tinhTrangPhapLy?: string;
  ngayDinhGia?: string;
  donViDinhGia?: string;
  soHopDongBaoHiem?: string;
  ngayHetHanBaoHiem?: string;
}

export const LOAI_TAI_SAN = [
  { value: 'BĐS - Nhà ở', label: 'Bất động sản - Nhà ở' },
  { value: 'BĐS - Đất', label: 'Bất động sản - Đất' },
  { value: 'BĐS - Căn hộ', label: 'Bất động sản - Căn hộ' },
  { value: 'BĐS - Thương mại', label: 'Bất động sản - Thương mại' },
  { value: 'Ô tô', label: 'Ô tô' },
  { value: 'Xe máy', label: 'Xe máy' },
  { value: 'Sổ tiết kiệm', label: 'Sổ tiết kiệm' },
  { value: 'Trái phiếu', label: 'Trái phiếu / Chứng chỉ tiền gửi' },
  { value: 'Máy móc thiết bị', label: 'Máy móc thiết bị' },
  { value: 'Hàng hóa', label: 'Hàng hóa trong kho' },
  { value: 'Khác', label: 'Khác' },
] as const;

export const TINH_TRANG_PHAP_LY = [
  { value: 'SO_HUU_RIENG', label: 'Sở hữu riêng' },
  { value: 'CHUNG_VO_CHONG', label: 'Chung vợ/chồng' },
  { value: 'DONG_SO_HUU', label: 'Đồng sở hữu' },
  { value: 'DANG_THE_CHAP', label: 'Đang thế chấp' },
  { value: 'KHAC', label: 'Khác' },
] as const;

export const taiSanDamBaoApi = {
  getAll: (params?: { maHoSo?: number; loaiTaiSan?: string; pageNumber?: number; pageSize?: number }) =>
    apiClient.get<{ items: TaiSanDamBao[]; totalCount: number }>('/tai-san-dam-bao', { params }),

  getById: (id: number) =>
    apiClient.get<TaiSanDamBao>(`/tai-san-dam-bao/${id}`),

  create: (data: CreateTaiSanDamBaoDTO) =>
    apiClient.post<TaiSanDamBao>('/tai-san-dam-bao', data),

  update: (id: number, data: CreateTaiSanDamBaoDTO) =>
    apiClient.put(`/tai-san-dam-bao/${id}`, data),

  delete: (id: number) =>
    apiClient.delete(`/tai-san-dam-bao/${id}`),
};
