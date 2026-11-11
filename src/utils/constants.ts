// Application constants

export const APP_NAME = 'QLTD Bank';
export const APP_VERSION = '1.0.0';

// API URLs
export const API_BASE_URL = 'http://localhost:5071/api';

// Pagination defaults
export const DEFAULT_PAGE_SIZE = 10;
export const PAGE_SIZE_OPTIONS = [10, 20, 50, 100];

// Status labels
export const TRANG_THAI_HO_SO = {
  MOI: { label: 'Mới', color: 'blue' },
  DA_NOP: { label: 'Đã nộp', color: 'cyan' },
  DANG_XEM_XET: { label: 'Đang xem xét', color: 'yellow' },
  DU_TAI_LIEU: { label: 'Đủ tài liệu', color: 'teal' },
  DANG_THAM_DINH: { label: 'Đang thẩm định', color: 'orange' },
  CHO_PHE_DUYET: { label: 'Chờ phê duyệt', color: 'purple' },
  DA_PHE_DUYET: { label: 'Đã phê duyệt', color: 'green' },
  TU_CHOI: { label: 'Từ chối', color: 'red' },
  DA_LAP_HOP_DONG: { label: 'Đã lập HĐ', color: 'emerald' },
  DA_GIAI_NGAN: { label: 'Đã giải ngân', color: 'teal' },
  DA_DONG: { label: 'Đã đóng', color: 'gray' },
} as const;

export const TRANG_THAI_HOP_DONG = {
  HOAT_DONG: { label: 'Hoạt động', color: 'green' },
  QUA_HAN: { label: 'Quá hạn', color: 'yellow' },
  QUA_HAN_30: { label: 'Quá hạn 30 ngày', color: 'orange' },
  QUA_HAN_60: { label: 'Quá hạn 60 ngày', color: 'red' },
  DA_DONG: { label: 'Đã đóng', color: 'gray' },
  DA_HUY: { label: 'Đã hủy', color: 'red' },
} as const;

export const TRANG_THAI_THANH_TOAN = {
  CHUA_THANH_TOAN: { label: 'Chưa thanh toán', color: 'yellow' },
  DA_THANH_TOAN: { label: 'Đã thanh toán', color: 'green' },
  TRE_HAN: { label: 'Trễ hạn', color: 'orange' },
  THANH_TOAN_MOT_PHAN: { label: 'Thanh toán 1 phần', color: 'blue' },
} as const;

export const LOAI_KHACH_HANG = {
  CA_NHAN: { label: 'Cá nhân', color: 'blue' },
  DOANH_NGHIEP: { label: 'Doanh nghiệp', color: 'purple' },
} as const;

export const MUC_DO_RUI_RO = {
  Thap: { label: 'Thấp', color: 'green' },
  TrungBinh: { label: 'Trung bình', color: 'yellow' },
  Cao: { label: 'Cao', color: 'red' },
} as const;

// Role permissions
export const PERMISSIONS = {
  ADMIN: ['all'],
  CREDIT_OFFICER: ['khachhang', 'hosovay', 'taive'],
  APPRAISER: ['thamdinh', 'hosovay'],
  APPROVER_L1: ['pheduyet', 'hosovay'],
  APPROVER_L2: ['pheduyet', 'hosovay'],
  ACCOUNTANT: ['thanhtoan', 'hopdong', 'giaingan'],
  REPORT_VIEWER: ['baocao'],
} as const;

// Storage keys
export const STORAGE_KEYS = {
  TOKEN: 'token',
  USER: 'user',
  THEME: 'theme',
  SIDEBAR_OPEN: 'sidebar_open',
} as const;
