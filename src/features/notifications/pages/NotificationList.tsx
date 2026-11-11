import { useState, useEffect } from 'react';
import { Bell, Filter, CheckCheck, Trash2, Inbox, Check } from 'lucide-react';
import { thongBaoApi, ThongBao, ThongBaoThongKe } from '../../../api/thongbao.api';
import Button from '../../../components/common/Button';
import Select from '../../../components/common/Select';
import Loading from '../../../components/common/Loading';
import { formatDateTime } from '../../../utils/formatters';

const NotificationList = () => {
  const [notifications, setNotifications] = useState<ThongBao[]>([]);
  const [stats, setStats] = useState<ThongBaoThongKe | null>(null);
  const [loading, setLoading] = useState(true);
  const [pagination, setPagination] = useState({
    pageNumber: 1,
    pageSize: 20,
    totalCount: 0,
  });
  const [filterChuaDoc, setFilterChuaDoc] = useState<'all' | 'unread' | 'read'>('all');
  const [filterLoai, setFilterLoai] = useState('');

  useEffect(() => {
    loadAll();
  }, [pagination.pageNumber, pagination.pageSize, filterChuaDoc, filterLoai]);

  const loadAll = async () => {
    setLoading(true);
    try {
      const [listRes, statsRes] = await Promise.all([
        thongBaoApi.getAll({
          pageNumber: pagination.pageNumber,
          pageSize: pagination.pageSize,
          chuaDoc: filterChuaDoc === 'unread' ? true : filterChuaDoc === 'read' ? false : undefined,
        }),
        thongBaoApi.getThongKe(),
      ]);
      let items = listRes.data.items;
      if (filterLoai) items = items.filter((n) => n.loai === filterLoai);
      setNotifications(items);
      setPagination((p) => ({ ...p, totalCount: listRes.data.totalCount }));
      setStats(statsRes.data);
    } catch (err) {
      console.error('Failed to load notifications:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleMarkAsRead = async (id: number) => {
    try {
      await thongBaoApi.markAsRead(id);
      setNotifications((prev) => prev.map((n) => (n.maThongBao === id ? { ...n, daDoc: true } : n)));
      setStats((prev) => prev ? { ...prev, chuaDoc: Math.max(0, prev.chuaDoc - 1) } : prev);
    } catch (err) {
      console.error('Failed:', err);
    }
  };

  const handleMarkAllAsRead = async () => {
    try {
      await thongBaoApi.markAllAsRead();
      setNotifications((prev) => prev.map((n) => ({ ...n, daDoc: true })));
      setStats((prev) => prev ? { ...prev, chuaDoc: 0 } : prev);
    } catch (err) {
      console.error('Failed:', err);
    }
  };

  const handleDelete = async (id: number) => {
    if (!confirm('Xóa thông báo này?')) return;
    try {
      await thongBaoApi.delete(id);
      const removed = notifications.find((n) => n.maThongBao === id);
      setNotifications((prev) => prev.filter((n) => n.maThongBao !== id));
      if (removed && !removed.daDoc) {
        setStats((prev) => prev ? { ...prev, chuaDoc: Math.max(0, prev.chuaDoc - 1) } : prev);
      }
    } catch (err) {
      console.error('Failed:', err);
    }
  };

  const loaiOptions = stats
    ? Object.keys(stats.theoLoai).map((k) => ({ value: k, label: k }))
    : [];

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <Bell className="w-6 h-6 text-blue-600" />
          <h1 className="text-2xl font-bold text-gray-800">Thông báo của tôi</h1>
        </div>
        {stats && stats.chuaDoc > 0 && (
          <Button leftIcon={<CheckCheck size={18} />} onClick={handleMarkAllAsRead}>
            Đọc tất cả ({stats.chuaDoc})
          </Button>
        )}
      </div>

      {/* Stats */}
      {stats && (
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <div className="bg-white rounded-xl shadow-sm p-5 border border-gray-100">
            <p className="text-gray-500 text-sm">Tổng thông báo</p>
            <p className="text-2xl font-bold text-gray-800 mt-1">{stats.tongSoThongBao}</p>
          </div>
          <div className="bg-white rounded-xl shadow-sm p-5 border border-gray-100">
            <p className="text-gray-500 text-sm">Chưa đọc</p>
            <p className="text-2xl font-bold text-red-600 mt-1">{stats.chuaDoc}</p>
          </div>
          <div className="bg-white rounded-xl shadow-sm p-5 border border-gray-100">
            <p className="text-gray-500 text-sm">Đã đọc</p>
            <p className="text-2xl font-bold text-green-600 mt-1">
              {stats.tongSoThongBao - stats.chuaDoc}
            </p>
          </div>
        </div>
      )}

      {/* Filters */}
      <div className="bg-white rounded-xl shadow-sm p-4 border border-gray-100">
        <div className="flex items-center gap-2 mb-3">
          <Filter size={16} className="text-gray-500" />
          <h3 className="font-medium text-gray-700">Bộ lọc</h3>
        </div>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
          <Select
            label="Trạng thái"
            value={filterChuaDoc}
            onChange={(v) => setFilterChuaDoc(v as 'all' | 'unread' | 'read')}
            options={[
              { value: 'all', label: 'Tất cả' },
              { value: 'unread', label: 'Chưa đọc' },
              { value: 'read', label: 'Đã đọc' },
            ]}
          />
          <Select
            label="Loại thông báo"
            value={filterLoai}
            onChange={(v) => setFilterLoai(String(v))}
            options={[{ value: '', label: 'Tất cả' }, ...loaiOptions]}
          />
        </div>
      </div>

      {/* List */}
      <div className="bg-white rounded-xl shadow-sm border border-gray-100">
        {loading ? (
          <Loading text="Đang tải thông báo..." />
        ) : notifications.length === 0 ? (
          <div className="p-12 text-center text-gray-500">
            <Inbox size={48} className="mx-auto mb-3 text-gray-300" />
            <p className="font-medium">Không có thông báo nào</p>
            <p className="text-sm text-gray-400 mt-1">Các thông báo mới sẽ xuất hiện tại đây</p>
          </div>
        ) : (
          <>
            <ul className="divide-y divide-gray-100">
              {notifications.map((n) => (
                <li
                  key={n.maThongBao}
                  className={`p-4 hover:bg-gray-50 transition-colors ${
                    !n.daDoc ? 'bg-blue-50/30' : ''
                  }`}
                >
                  <div className="flex gap-3">
                    <div
                      className={`w-10 h-10 rounded-full flex items-center justify-center flex-shrink-0 ${
                        n.daDoc ? 'bg-gray-100 text-gray-500' : 'bg-blue-100 text-blue-600'
                      }`}
                    >
                      <Bell size={18} />
                    </div>
                    <div className="flex-1 min-w-0">
                      <div className="flex items-start justify-between gap-2">
                        <p className={`text-sm ${!n.daDoc ? 'font-semibold text-gray-900' : 'text-gray-700'}`}>
                          {n.tieuDe}
                          {!n.daDoc && (
                            <span className="ml-2 inline-block w-2 h-2 rounded-full bg-blue-500" />
                          )}
                        </p>
                        <span className="text-xs text-gray-400 whitespace-nowrap">{formatDateTime(n.ngayGui)}</span>
                      </div>
                      <p className="text-sm text-gray-600 mt-1">{n.noiDung}</p>
                      <div className="flex items-center gap-3 mt-2">
                        {n.maHoSoHienThi && (
                          <span className="text-xs text-indigo-600 font-mono bg-indigo-50 px-2 py-0.5 rounded">
                            📎 {n.maHoSoHienThi}
                          </span>
                        )}
                        <span className="text-xs text-gray-400 bg-gray-100 px-2 py-0.5 rounded">
                          {n.loai}
                        </span>
                        <div className="flex-1" />
                        {!n.daDoc && (
                          <Button
                            size="sm"
                            variant="outline"
                            onClick={() => handleMarkAsRead(n.maThongBao)}
                            leftIcon={<Check size={12} />}
                          >
                            Đã đọc
                          </Button>
                        )}
                        <Button
                          size="sm"
                          variant="outline"
                          onClick={() => handleDelete(n.maThongBao)}
                          leftIcon={<Trash2 size={12} />}
                        >
                          Xóa
                        </Button>
                      </div>
                    </div>
                  </div>
                </li>
              ))}
            </ul>

            {/* Pagination */}
            <div className="flex items-center justify-between px-4 py-3 border-t border-gray-200 text-sm">
              <span className="text-gray-500">
                Tổng {pagination.totalCount.toLocaleString()} thông báo
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
                <span className="text-gray-700 px-3">
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
    </div>
  );
};

export default NotificationList;
