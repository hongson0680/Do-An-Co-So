import { apiClient } from './client';

export interface PheDuyet {
  maPheDuyet: number;
  maHoSo: number;
  maNguoiPheDuyet: number;
  quyetDinh: string;
  soTienDuocDuyet: number;
  laiSuatDuocDuyet: number;
  dieuKienPheDuyet?: string;
  lyDoTuChoi?: string;
  ngayPheDuyet: string;
}

export interface CreatePheDuyetDTO {
  maHoSo: number;
  quyetDinh: string;
  soTienDuocDuyet: number;
  laiSuatDuocDuyet: number;
  dieuKienPheDuyet?: string;
  lyDoTuChoi?: string;
}

export const pheDuyetApi = {
  getByHoSo: (hoSoId: number) =>
    apiClient.get<PheDuyet>(`/phe-duyet/ho-so/${hoSoId}`),

  create: (data: CreatePheDuyetDTO) =>
    apiClient.post<PheDuyet>('/phe-duyet', data),
};
