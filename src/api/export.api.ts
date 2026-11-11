import { apiClient } from './client';

export type ExportFormat = 'excel' | 'pdf';

export interface ExportParams {
  format?: ExportFormat;
  search?: string;
  loaiKhachHang?: string;
  trangThai?: string;
  tuNgay?: string;
  denNgay?: string;
  hanhDong?: string;
  tenThucThe?: string;
}

/**
 * Trigger download file từ blob response
 */
function downloadFile(blob: Blob, filename: string) {
  const url = window.URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = filename;
  document.body.appendChild(a);
  a.click();
  document.body.removeChild(a);
  window.URL.revokeObjectURL(url);
}

/**
 * Lấy filename từ Content-Disposition header hoặc dùng tên mặc định
 */
function extractFilename(disposition: string | null, fallback: string): string {
  if (!disposition) return fallback;
  // RFC 5987: filename*=UTF-8''<encoded>
  const rfc5987 = /filename\*=UTF-8''([^;]+)/i.exec(disposition);
  if (rfc5987) {
    try {
      return decodeURIComponent(rfc5987[1]);
    } catch {
      /* fall through */
    }
  }
  // Standard: filename="..."
  const standard = /filename="?([^";]+)"?/i.exec(disposition);
  if (standard) return standard[1];
  return fallback;
}

export const exportApi = {
  async exportKhachHang(params: ExportParams = {}): Promise<void> {
    const res = await apiClient.get('/export/khach-hang', {
      params,
      responseType: 'blob',
    });
    const ext = params.format === 'pdf' ? 'pdf' : 'xlsx';
    const filename = extractFilename(
      res.headers['content-disposition'] ?? null,
      `danh-sach-khach-hang-${new Date().toISOString().slice(0, 10)}.${ext}`
    );
    downloadFile(res.data, filename);
  },

  async exportHoSoVay(params: ExportParams = {}): Promise<void> {
    const res = await apiClient.get('/export/ho-so-vay', {
      params,
      responseType: 'blob',
    });
    const ext = params.format === 'pdf' ? 'pdf' : 'xlsx';
    const filename = extractFilename(
      res.headers['content-disposition'] ?? null,
      `danh-sach-ho-so-vay-${new Date().toISOString().slice(0, 10)}.${ext}`
    );
    downloadFile(res.data, filename);
  },

  async exportHopDong(params: ExportParams = {}): Promise<void> {
    const res = await apiClient.get('/export/hop-dong', {
      params,
      responseType: 'blob',
    });
    const ext = params.format === 'pdf' ? 'pdf' : 'xlsx';
    const filename = extractFilename(
      res.headers['content-disposition'] ?? null,
      `danh-sach-hop-dong-${new Date().toISOString().slice(0, 10)}.${ext}`
    );
    downloadFile(res.data, filename);
  },

  async exportLichTraNo(hopDongId: number, format: ExportFormat = 'excel'): Promise<void> {
    const res = await apiClient.get(`/export/hop-dong/${hopDongId}/lich-tra-no`, {
      params: { format },
      responseType: 'blob',
    });
    const ext = format === 'pdf' ? 'pdf' : 'xlsx';
    const filename = extractFilename(
      res.headers['content-disposition'] ?? null,
      `lich-tra-no-${hopDongId}.${ext}`
    );
    downloadFile(res.data, filename);
  },

  async exportThanhToan(params: ExportParams = {}): Promise<void> {
    const res = await apiClient.get('/export/thanh-toan', {
      params,
      responseType: 'blob',
    });
    const ext = params.format === 'pdf' ? 'pdf' : 'xlsx';
    const filename = extractFilename(
      res.headers['content-disposition'] ?? null,
      `danh-sach-thanh-toan-${new Date().toISOString().slice(0, 10)}.${ext}`
    );
    downloadFile(res.data, filename);
  },

  async exportDashboard(format: ExportFormat = 'excel'): Promise<void> {
    const res = await apiClient.get('/export/dashboard', {
      params: { format },
      responseType: 'blob',
    });
    const ext = format === 'pdf' ? 'pdf' : 'xlsx';
    const filename = extractFilename(
      res.headers['content-disposition'] ?? null,
      `bao-cao-thong-ke-${new Date().toISOString().slice(0, 10)}.${ext}`
    );
    downloadFile(res.data, filename);
  },

  async exportAuditLog(params: ExportParams = {}): Promise<void> {
    const res = await apiClient.get('/export/audit-log', {
      params,
      responseType: 'blob',
    });
    const ext = params.format === 'pdf' ? 'pdf' : 'xlsx';
    const filename = extractFilename(
      res.headers['content-disposition'] ?? null,
      `nhat-ky-he-thong-${new Date().toISOString().slice(0, 10)}.${ext}`
    );
    downloadFile(res.data, filename);
  },
};
