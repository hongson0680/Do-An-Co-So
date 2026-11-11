import { useState, useEffect } from 'react';
import { History, Filter, User, Activity, Database, TrendingUp, Eye, X } from 'lucide-react';
import { auditLogApi, NhatKyHeThong, NhatKyThongKe, AUDIT_ACTIONS, AUDIT_ENTITIES } from '../../../api/auditlog.api';
import { exportApi } from '../../../api/export.api';
import Table from '../../../components/common/Table';
import Button from '../../../components/common/Button';
import Input from '../../../components/common/Input';
import Select from '../../../components/common/Select';
import Modal from '../../../components/common/Modal';
import Loading from '../../../components/common/Loading';
import ExportButton from '../../../components/common/ExportButton';
import { formatDateTime } from '../../../utils/formatters';

const AuditLogList = () => {
  const [logs, setLogs] = useState<NhatKyHeThong[]>([]);
  const [stats, setStats] = useState<NhatKyThongKe | null>(null);
  const [loading, setLoading] = useState(true);
  const [pagination, setPagination] = useState({
    pageNumber: 1,
    pageSize: 50,
    totalCount: 0,
  });

  // Filters
  const [filterHanhDong, setFilterHanhDong] = useState('');
  const [filterThucThe, setFilterThucThe] = useState('');
  const [filterSearch, setFilterSearch] = useState('');
  const [filterTuNgay, setFilterTuNgay] = useState('');
  const [filterDenNgay, setFilterDenNgay] = useState('');

  // Detail modal
  const [detailOpen, setDetailOpen] = useState(false);
  const [selectedLog, setSelectedLog] = useState<NhatKyHeThong | null>(null);

  useEffect(() => {
    loadLogs();
    loadStats();
  }, [pagination.pageNumber, pagination.pageSize]);

  // Reload when filters change (debounce cho search)
  useEffect(() => {
    const t = setTimeout(() => {
      setPagination((p) => ({ ...p, pageNumber: 1 }));
      loadLogs();
    }, 400);
    return () => clearTimeout(t);
  }, [filterHanhDong, filterThucThe, filterSearch, filterTuNgay, filterDenNgay]);

  const loadLogs = async () => {
    setLoading(true);
    try {
      const response = await auditLogApi.getAll({
        pageNumber: pagination.pageNumber,
        pageSize: pagination.pageSize,
        hanhDong: filterHanhDong || undefined,
        tenThucThe: filterThucThe || undefined,
        search: filterSearch || undefined,
        tuNgay: filterTuNgay ? `${filterTuNgay}T00:00:00` : undefined,
        denNgay: filterDenNgay ? `${filterDenNgay}T23:59:59` : undefined,
      });
      setLogs(response.data.items);
      setPagination((p) => ({ ...p, totalCount: response.data.totalCount }));
    } catch (err) {
      console.error('Failed to load audit logs:', err);
    } finally {
      setLoading(false);
    }
  };

  const loadStats = async () => {
    try {
      const response = await auditLogApi.getThongKe();
      setStats(response.data);
    } catch (err) {
      console.error('Failed to load stats:', err);
    }
  };

  const getActionConfig = (action: string) => {
    return AUDIT_ACTIONS.find((a) => a.value === action) ?? { value: action, label: action, color: 'gray' };
  };

  const getEntityLabel = (entity: string) => {
    return AUDIT_ENTITIES.find((e) => e.value === entity)?.label ?? entity;
  };

  const clearFilters = () => {
    setFilterHanhDong('');
    setFilterThucThe('');
    setFilterSearch('');
    setFilterTuNgay('');
    setFilterDenNgay('');
  };

  const hasActiveFilter = filterHanhDong || filterThucThe || filterSearch || filterTuNgay || filterDenNgay;

  const columns = [
    {
      key: 'ngayTao',
      title: 'Thời gian',
      width: '160px',
      render: (r: NhatKyHeThong) => (
        <span className="text-xs font-mono text-gray-600">{formatDateTime(r.ngayTao)}</span>
      ),
    },
    {
      key: 'tenNguoiDung',
      title: 'Người thực hiện',
      render: (r: NhatKyHeThong) => (
        <div className="flex items-center gap-2">
          <div className="w-7 h-7 rounded-full bg-blue-100 text-blue-600 flex items-center justify-center text-xs font-semibold">
            {r.tenNguoiDung?.charAt(0) ?? '?'}
          </div>
          <span className="font-medium">{r.tenNguoiDung ?? 'Hệ thống'}</span>
        </div>
      ),
    },
    {
      key: 'hanhDong',
      title: 'Hành động',
      width: '180px',
      render: (r: NhatKyHeThong) => {
        const cfg = getActionConfig(r.hanhDong);
        return (
          <span
            className={`inline-block px-2 py-0.5 text-xs rounded border font-medium ${
              cfg.color === 'green' ? 'bg-green-50 text-green-700 border-green-200' :
              cfg.color === 'red' ? 'bg-red-50 text-red-700 border-red-200' :
              cfg.color === 'blue' ? 'bg-blue-50 text-blue-700 border-blue-200' :
              cfg.color === 'yellow' ? 'bg-yellow-50 text-yellow-700 border-yellow-200' :
              cfg.color === 'purple' ? 'bg-purple-50 text-purple-700 border-purple-200' :
              cfg.color === 'cyan' ? 'bg-cyan-50 text-cyan-700 border-cyan-200' :
              cfg.color === 'emerald' ? 'bg-emerald-50 text-emerald-700 border-emerald-200' :
              'bg-gray-50 text-gray-700 border-gray-200'
            }`}
          >
            {cfg.label}
          </span>
        );
      },
    },
    {
      key: 'tenThucThe',
      title: 'Đối tượng',
      render: (r: NhatKyHeThong) => (
        <div className="text-sm">
          <div className="font-medium text-gray-800">{getEntityLabel(r.tenThucThe)}</div>
          <div className="text-xs text-gray-500 font-mono">{r.maThucThe}</div>
        </div>
      ),
    },
    {
      key: 'moTa',
      title: 'Chi tiết',
      render: (r: NhatKyHeThong) => (
        <div className="text-sm text-gray-700 max-w-md truncate" title={r.giaTriMoi}>
          {r.giaTriMoi || <span className="text-gray-400 italic">Không có</span>}
        </div>
      ),
    },
    {
      key: 'actions',
      title: '',
      width: '60px',
      render: (r: NhatKyHeThong) => (
        <button
          type="button"
          onClick={() => {
            setSelectedLog(r);
            setDetailOpen(true);
          }}
          className="p-1.5 hover:bg-gray-100 rounded"
          title="Xem chi tiết"
          aria-label="Xem chi tiết log"
        >
          <Eye size={16} className="text-blue-600" />
        </button>
      ),
    },
  ];

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <History className="w-6 h-6 text-indigo-600" />
          <h1 className="text-2xl font-bold text-gray-800">Nhật ký hệ thống (Audit Log)</h1>
        </div>
        <div className="flex items-center gap-2">
          <ExportButton
            label="log"
            onExport={(format) => exportApi.exportAuditLog({
              format,
              hanhDong: filterHanhDong || undefined,
              tenThucThe: filterThucThe || undefined,
              tuNgay: filterTuNgay ? `${filterTuNgay}T00:00:00` : undefined,
              denNgay: filterDenNgay ? `${filterDenNgay}T23:59:59` : undefined,
            })}
          />
          <Button variant="outline" onClick={() => { loadLogs(); loadStats(); }}>
            Làm mới
          </Button>
        </div>
      </div>

      {/* Stats */}
      {stats && (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
          <div className="bg-white rounded-xl shadow-sm p-5 border border-gray-100">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-gray-500 text-sm">Tổng log</p>
                <p className="text-2xl font-bold text-gray-800 mt-1">{stats.tongSoLog.toLocaleString()}</p>
              </div>
              <div className="p-3 bg-indigo-50 text-indigo-600 rounded-lg">
                <Database size={24} />
              </div>
            </div>
          </div>
          <div className="bg-white rounded-xl shadow-sm p-5 border border-gray-100">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-gray-500 text-sm">Hôm nay</p>
                <p className="text-2xl font-bold text-blue-700 mt-1">{stats.soLuongHomNay}</p>
              </div>
              <div className="p-3 bg-blue-50 text-blue-600 rounded-lg">
                <Activity size={24} />
              </div>
            </div>
          </div>
          <div className="bg-white rounded-xl shadow-sm p-5 border border-gray-100">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-gray-500 text-sm">Tuần này</p>
                <p className="text-2xl font-bold text-green-700 mt-1">{stats.soLuongTuanNay}</p>
              </div>
              <div className="p-3 bg-green-50 text-green-600 rounded-lg">
                <TrendingUp size={24} />
              </div>
            </div>
          </div>
          <div className="bg-white rounded-xl shadow-sm p-5 border border-gray-100">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-gray-500 text-sm">Tháng này</p>
                <p className="text-2xl font-bold text-purple-700 mt-1">{stats.soLuongThangNay}</p>
              </div>
              <div className="p-3 bg-purple-50 text-purple-600 rounded-lg">
                <User size={24} />
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Top users + Phân bố theo thực thể */}
      {stats && (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          <div className="bg-white rounded-xl shadow-sm p-6 border border-gray-100">
            <h3 className="text-lg font-semibold text-gray-800 mb-4">Top 10 người dùng hoạt động</h3>
            {stats.nguoiDungHoatDong.length === 0 ? (
              <p className="text-sm text-gray-500">Chưa có dữ liệu</p>
            ) : (
              <div className="space-y-2">
                {stats.nguoiDungHoatDong.map((u, idx) => (
                  <div key={u.maNguoiDung} className="flex items-center justify-between text-sm py-1.5 px-2 hover:bg-gray-50 rounded">
                    <div className="flex items-center gap-3">
                      <span className={`w-6 h-6 rounded-full flex items-center justify-center text-xs font-semibold ${
                        idx === 0 ? 'bg-yellow-100 text-yellow-700' :
                        idx === 1 ? 'bg-gray-200 text-gray-700' :
                        idx === 2 ? 'bg-orange-100 text-orange-700' :
                        'bg-gray-100 text-gray-500'
                      }`}>
                        {idx + 1}
                      </span>
                      <div>
                        <div className="font-medium text-gray-800">{u.hoTen}</div>
                        <div className="text-xs text-gray-500">@{u.tenDangNhap}</div>
                      </div>
                    </div>
                    <span className="font-semibold text-indigo-600">{u.soLuotThaoTac}</span>
                  </div>
                ))}
              </div>
            )}
          </div>

          <div className="bg-white rounded-xl shadow-sm p-6 border border-gray-100">
            <h3 className="text-lg font-semibold text-gray-800 mb-4">Phân bố theo thực thể</h3>
            {Object.keys(stats.theoThucThe).length === 0 ? (
              <p className="text-sm text-gray-500">Chưa có dữ liệu</p>
            ) : (() => {
              const entries = Object.entries(stats.theoThucThe).sort((a, b) => b[1] - a[1]);
              const max = Math.max(...entries.map(([, v]) => v), 1);
              return (
                <div className="space-y-3">
                  {entries.map(([entity, count]) => (
                    <div key={entity}>
                      <div className="flex items-center justify-between text-sm mb-1">
                        <span className="text-gray-700">{getEntityLabel(entity)}</span>
                        <span className="font-semibold text-gray-800">{count}</span>
                      </div>
                      <div className="w-full bg-gray-100 rounded-full h-2 overflow-hidden">
                        <div
                          className="h-full bg-indigo-500 rounded-full"
                          style={{ width: `${(count / max) * 100}%` }}
                        />
                      </div>
                    </div>
                  ))}
                </div>
              );
            })()}
          </div>
        </div>
      )}

      {/* Filters */}
      <div className="bg-white rounded-xl shadow-sm p-4 border border-gray-100">
        <div className="flex items-center gap-2 mb-3">
          <Filter size={16} className="text-gray-500" />
          <h3 className="font-medium text-gray-700">Bộ lọc</h3>
          {hasActiveFilter && (
            <button
              type="button"
              onClick={clearFilters}
              className="ml-auto text-xs text-blue-600 hover:text-blue-800 flex items-center gap-1"
            >
              <X size={12} /> Xóa bộ lọc
            </button>
          )}
        </div>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-5 gap-3">
          <Select
            label="Hành động"
            value={filterHanhDong}
            onChange={(v) => setFilterHanhDong(String(v))}
            options={[
              { value: '', label: 'Tất cả' },
              ...AUDIT_ACTIONS.map((a) => ({ value: a.value, label: a.label })),
            ]}
          />
          <Select
            label="Đối tượng"
            value={filterThucThe}
            onChange={(v) => setFilterThucThe(String(v))}
            options={[
              { value: '', label: 'Tất cả' },
              ...AUDIT_ENTITIES.map((e) => ({ value: e.value, label: e.label })),
            ]}
          />
          <Input
            label="Từ ngày"
            type="date"
            value={filterTuNgay}
            onChange={(e) => setFilterTuNgay(e.target.value)}
          />
          <Input
            label="Đến ngày"
            type="date"
            value={filterDenNgay}
            onChange={(e) => setFilterDenNgay(e.target.value)}
          />
          <Input
            label="Tìm kiếm"
            placeholder="Mã, nội dung..."
            value={filterSearch}
            onChange={(e) => setFilterSearch(e.target.value)}
          />
        </div>
      </div>

      {/* Table */}
      <div className="bg-white rounded-xl shadow-sm border border-gray-100">
        {loading ? (
          <Loading text="Đang tải nhật ký..." />
        ) : (
          <>
            <Table
              columns={columns}
              data={logs}
              emptyText="Chưa có log nào"
            />
            {/* Pagination đơn giản */}
            <div className="flex items-center justify-between px-4 py-3 border-t border-gray-200 text-sm">
              <span className="text-gray-500">
                Tổng {pagination.totalCount.toLocaleString()} bản ghi
              </span>
              <div className="flex items-center gap-2">
                <Button
                  size="sm"
                  variant="outline"
                  disabled={pagination.pageNumber === 1}
                  onClick={() => setPagination((p) => ({ ...p, pageNumber: p.pageNumber - 1 }))}
                >
                  Trước
                </Button>
                <span className="text-gray-700">
                  Trang {pagination.pageNumber} / {Math.max(1, Math.ceil(pagination.totalCount / pagination.pageSize))}
                </span>
                <Button
                  size="sm"
                  variant="outline"
                  disabled={pagination.pageNumber * pagination.pageSize >= pagination.totalCount}
                  onClick={() => setPagination((p) => ({ ...p, pageNumber: p.pageNumber + 1 }))}
                >
                  Sau
                </Button>
              </div>
            </div>
          </>
        )}
      </div>

      {/* Detail Modal */}
      <Modal
        open={detailOpen}
        onClose={() => setDetailOpen(false)}
        title="Chi tiết nhật ký"
        size="lg"
      >
        {selectedLog && (
          <div className="space-y-4">
            <div className="grid grid-cols-2 gap-4 text-sm">
              <div>
                <p className="text-gray-500 text-xs">Mã nhật ký</p>
                <p className="font-medium">#{selectedLog.maNhatKy}</p>
              </div>
              <div>
                <p className="text-gray-500 text-xs">Thời gian</p>
                <p className="font-medium">{formatDateTime(selectedLog.ngayTao)}</p>
              </div>
              <div>
                <p className="text-gray-500 text-xs">Người thực hiện</p>
                <p className="font-medium">{selectedLog.tenNguoiDung ?? 'Hệ thống'}</p>
              </div>
              <div>
                <p className="text-gray-500 text-xs">Hành động</p>
                <p className="font-medium">{getActionConfig(selectedLog.hanhDong).label}</p>
              </div>
              <div>
                <p className="text-gray-500 text-xs">Đối tượng</p>
                <p className="font-medium">{getEntityLabel(selectedLog.tenThucThe)}</p>
              </div>
              <div>
                <p className="text-gray-500 text-xs">Mã đối tượng</p>
                <p className="font-mono">{selectedLog.maThucThe}</p>
              </div>
              <div>
                <p className="text-gray-500 text-xs">IP nguồn</p>
                <p className="font-mono text-gray-700">{selectedLog.ipNguon ?? '-'}</p>
              </div>
              <div>
                <p className="text-gray-500 text-xs">Trình duyệt</p>
                <p className="text-xs text-gray-700 truncate" title={selectedLog.trinhDuyet}>
                  {selectedLog.trinhDuyet ?? '-'}
                </p>
              </div>
            </div>

            {(selectedLog.giaTriCu || selectedLog.giaTriMoi) && (
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <p className="text-gray-500 text-xs mb-1">Giá trị cũ</p>
                  <div className="bg-red-50 border border-red-100 p-3 rounded text-sm text-gray-800 max-h-40 overflow-y-auto whitespace-pre-wrap">
                    {selectedLog.giaTriCu || <span className="text-gray-400 italic">Không có</span>}
                  </div>
                </div>
                <div>
                  <p className="text-gray-500 text-xs mb-1">Giá trị mới</p>
                  <div className="bg-green-50 border border-green-100 p-3 rounded text-sm text-gray-800 max-h-40 overflow-y-auto whitespace-pre-wrap">
                    {selectedLog.giaTriMoi || <span className="text-gray-400 italic">Không có</span>}
                  </div>
                </div>
              </div>
            )}

            {selectedLog.truongThayDoi && (
              <div>
                <p className="text-gray-500 text-xs mb-1">Trường thay đổi</p>
                <p className="text-sm">{selectedLog.truongThayDoi}</p>
              </div>
            )}
          </div>
        )}
      </Modal>
    </div>
  );
};

export default AuditLogList;
