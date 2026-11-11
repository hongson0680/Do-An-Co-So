import { useState, useEffect, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import { Bell, Check, CheckCheck, Trash2, Inbox } from 'lucide-react';
import { thongBaoApi, ThongBao } from '../../api/thongbao.api';
import { formatDateTime } from '../../utils/formatters';

const NotificationBell = () => {
  const navigate = useNavigate();
  const [open, setOpen] = useState(false);
  const [notifications, setNotifications] = useState<ThongBao[]>([]);
  const [unread, setUnread] = useState(0);
  const [loading, setLoading] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);

  // Polling mỗi 30s để cập nhật badge
  useEffect(() => {
    loadUnread();
    const interval = setInterval(loadUnread, 30000);
    return () => clearInterval(interval);
  }, []);

  // Click outside để đóng
  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(e.target as Node)) {
        setOpen(false);
      }
    };
    if (open) {
      document.addEventListener('mousedown', handleClickOutside);
      return () => document.removeEventListener('mousedown', handleClickOutside);
    }
  }, [open]);

  // Load danh sách khi mở
  useEffect(() => {
    if (open) loadRecent();
  }, [open]);

  const loadUnread = async () => {
    try {
      const res = await thongBaoApi.getUnreadCount();
      setUnread(res.data.unread);
    } catch (err) {
      console.error('Failed to load unread count:', err);
    }
  };

  const loadRecent = async () => {
    setLoading(true);
    try {
      const res = await thongBaoApi.getRecent(10);
      setNotifications(res.data);
    } catch (err) {
      console.error('Failed to load notifications:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleMarkAsRead = async (id: number, e: React.MouseEvent) => {
    e.stopPropagation();
    try {
      await thongBaoApi.markAsRead(id);
      setNotifications((prev) => prev.map((n) => (n.maThongBao === id ? { ...n, daDoc: true } : n)));
      setUnread((prev) => Math.max(0, prev - 1));
    } catch (err) {
      console.error('Failed to mark as read:', err);
    }
  };

  const handleMarkAllAsRead = async () => {
    try {
      await thongBaoApi.markAllAsRead();
      setNotifications((prev) => prev.map((n) => ({ ...n, daDoc: true })));
      setUnread(0);
    } catch (err) {
      console.error('Failed to mark all as read:', err);
    }
  };

  const handleDelete = async (id: number, e: React.MouseEvent) => {
    e.stopPropagation();
    try {
      await thongBaoApi.delete(id);
      const removed = notifications.find((n) => n.maThongBao === id);
      setNotifications((prev) => prev.filter((n) => n.maThongBao !== id));
      if (removed && !removed.daDoc) setUnread((prev) => Math.max(0, prev - 1));
    } catch (err) {
      console.error('Failed to delete:', err);
    }
  };

  const handleClickNotification = (n: ThongBao) => {
    if (!n.daDoc) {
      thongBaoApi.markAsRead(n.maThongBao).catch(() => {});
      setNotifications((prev) => prev.map((x) => (x.maThongBao === n.maThongBao ? { ...x, daDoc: true } : x)));
      setUnread((prev) => Math.max(0, prev - 1));
    }
    // Đóng dropdown
    setOpen(false);
    // Nếu có liên kết hồ sơ → điều hướng tới đúng hồ sơ (trước đây điều hướng
    // tới /loans không truyền id → không mở được hồ sơ cụ thể)
    if (n.maHoSo) {
      navigate(`/loans?maHoSo=${n.maHoSo}`);
    } else {
      navigate('/notifications');
    }
  };

  const getIconColor = (loai: string) => {
    if (loai === 'DA_PHE_DUYET' || loai === 'APPROVE') return 'bg-green-100 text-green-600';
    if (loai === 'TU_CHOI' || loai === 'REJECT') return 'bg-red-100 text-red-600';
    return 'bg-blue-100 text-blue-600';
  };

  return (
    <div className="relative" ref={dropdownRef}>
      {/* Bell button */}
      <button
        type="button"
        onClick={() => setOpen(!open)}
        className="p-2 hover:bg-gray-100 rounded-lg relative"
        aria-label="Thông báo"
        title="Thông báo"
      >
        <Bell size={20} className="text-gray-600" />
        {unread > 0 && (
          <span className="absolute -top-0.5 -right-0.5 min-w-[18px] h-[18px] px-1 bg-red-500 text-white text-[10px] font-semibold rounded-full flex items-center justify-center">
            {unread > 99 ? '99+' : unread}
          </span>
        )}
      </button>

      {/* Dropdown */}
      {open && (
        <div className="absolute right-0 mt-2 w-96 max-h-[600px] bg-white rounded-lg shadow-xl border border-gray-200 z-50 flex flex-col">
          {/* Header */}
          <div className="flex items-center justify-between px-4 py-3 border-b">
            <h3 className="font-semibold text-gray-800 flex items-center gap-2">
              <Bell size={16} />
              Thông báo
              {unread > 0 && (
                <span className="bg-red-500 text-white text-xs px-2 py-0.5 rounded-full">{unread} mới</span>
              )}
            </h3>
            {unread > 0 && (
              <button
                type="button"
                onClick={handleMarkAllAsRead}
                className="text-xs text-blue-600 hover:text-blue-800 flex items-center gap-1"
                title="Đánh dấu tất cả là đã đọc"
              >
                <CheckCheck size={14} /> Đọc tất cả
              </button>
            )}
          </div>

          {/* Body */}
          <div className="flex-1 overflow-y-auto">
            {loading ? (
              <div className="p-8 text-center text-sm text-gray-500">Đang tải...</div>
            ) : notifications.length === 0 ? (
              <div className="p-8 text-center text-gray-500">
                <Inbox size={36} className="mx-auto mb-2 text-gray-300" />
                <p className="text-sm">Chưa có thông báo nào</p>
              </div>
            ) : (
              <ul className="divide-y divide-gray-100">
                {notifications.map((n) => (
                  <li
                    key={n.maThongBao}
                    onClick={() => handleClickNotification(n)}
                    className={`p-3 hover:bg-gray-50 cursor-pointer transition-colors ${
                      !n.daDoc ? 'bg-blue-50/50' : ''
                    }`}
                  >
                    <div className="flex gap-3">
                      <div
                        className={`w-9 h-9 rounded-full flex items-center justify-center flex-shrink-0 ${
                          n.daDoc ? 'bg-gray-100 text-gray-500' : getIconColor(n.loai)
                        }`}
                      >
                        <Bell size={16} />
                      </div>
                      <div className="flex-1 min-w-0">
                        <div className="flex items-start justify-between gap-2">
                          <p className={`text-sm ${!n.daDoc ? 'font-semibold text-gray-900' : 'text-gray-700'}`}>
                            {n.tieuDe}
                          </p>
                          {!n.daDoc && (
                            <span className="w-2 h-2 rounded-full bg-blue-500 flex-shrink-0 mt-1.5" />
                          )}
                        </div>
                        <p className="text-xs text-gray-600 mt-0.5 line-clamp-2">{n.noiDung}</p>
                        {n.maHoSoHienThi && (
                          <p className="text-xs text-indigo-600 mt-1 font-mono">📎 {n.maHoSoHienThi}</p>
                        )}
                        <div className="flex items-center justify-between mt-1.5">
                          <p className="text-xs text-gray-400">{formatDateTime(n.ngayGui)}</p>
                          <div className="flex items-center gap-1">
                            {!n.daDoc && (
                              <button
                                type="button"
                                onClick={(e) => handleMarkAsRead(n.maThongBao, e)}
                                className="p-1 hover:bg-gray-200 rounded"
                                title="Đánh dấu đã đọc"
                                aria-label="Đánh dấu đã đọc"
                              >
                                <Check size={12} className="text-gray-500" />
                              </button>
                            )}
                            <button
                              type="button"
                              onClick={(e) => handleDelete(n.maThongBao, e)}
                              className="p-1 hover:bg-red-100 rounded"
                              title="Xóa"
                              aria-label="Xóa thông báo"
                            >
                              <Trash2 size={12} className="text-red-500" />
                            </button>
                          </div>
                        </div>
                      </div>
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </div>

          {/* Footer */}
          <div className="border-t px-4 py-2 bg-gray-50 flex justify-center">
            <button
              type="button"
              onClick={() => {
                setOpen(false);
                navigate('/notifications');
              }}
              className="text-sm text-blue-600 hover:text-blue-800 font-medium"
            >
              Xem tất cả thông báo →
            </button>
          </div>
        </div>
      )}
    </div>
  );
};

export default NotificationBell;
