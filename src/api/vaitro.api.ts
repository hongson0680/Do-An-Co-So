import { apiClient } from './client';

export interface VaiTro {
  maVaiTro: number;
  maVaiTroHienThi: string;
  tenVaiTro: string;
  moTa?: string;
  laVaiTroHeThong: boolean;
  trangThai: boolean;
}

export const vaiTroApi = {
  getAll: (params?: { activeOnly?: boolean }) =>
    apiClient.get<VaiTro[]>('/vaitro', { params }),

  getById: (id: number) =>
    apiClient.get<VaiTro>(`/vaitro/${id}`),
};
