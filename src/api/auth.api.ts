import { apiClient } from './client';

export interface LoginRequest {
  tenDangNhap: string;
  matKhau: string;
}

export interface LoginResponse {
  token: string;
  refreshToken: string;
  expiresIn: number;
  user: {
    maNguoiDung: number;
    tenDangNhap: string;
    hoTen: string;
    email: string;
    maVaiTro: string;
    tenVaiTro: string;
    maChiNhanh: number;
    tenChiNhanh: string;
  };
}

export interface ChangePasswordRequest {
  matKhauCu: string;
  matKhauMoi: string;
  xacNhanMatKhau: string;
}

export const authApi = {
  login: (data: LoginRequest) =>
    apiClient.post<LoginResponse>('/auth/login', data),

  getProfile: () =>
    apiClient.get('/auth/profile'),

  changePassword: (data: ChangePasswordRequest) =>
    apiClient.post('/auth/change-password', data),
};
