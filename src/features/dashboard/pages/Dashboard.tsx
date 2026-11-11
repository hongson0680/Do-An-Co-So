import { useState, useEffect } from 'react';
import {
  Users,
  FileText,
  FileSignature,
  AlertCircle,
  DollarSign,
  BarChart3,
  Calendar,
  Trophy,
} from 'lucide-react';
import { dashboardApi, HoSoTheoTrangThai, TopKhachHang } from '../../../api/dashboard.api';
import { exportApi } from '../../../api/export.api';
import { formatCurrency } from '../../../utils/formatters';
import Loading from '../../../components/common/Loading';
import ExportButton from '../../../components/common/ExportButton';
import { TRANG_THAI_HO_SO } from '../../../utils/constants';

interface DashboardStats {
  tongKhachHang: number;
  tongHoSoVay: number;
  tongHopDong: number;
  tongGiaiNgan: number;
  hoSoMoi: number;
  hoSoDangXyLy: number;
  hoSoDaDuyet: number;
  hoSoTuChoi: number;
  tienGiaiNganThangNay: number;
  tiLeQuaHan: number;
}

interface ChartData {
  label: string;
  value: number;
}

const Dashboard = () => {
  const [stats, setStats] = useState<DashboardStats | null>(null);
  const [hoSoByStatus, setHoSoByStatus] = useState<HoSoTheoTrangThai[]>([]);
  const [giaiNganTheoThang, setGiaiNganTheoThang] = useState<ChartData[]>([]);
  const [topKhachHang, setTopKhachHang] = useState<TopKhachHang[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    loadAll();
  }, []);

  const loadAll = async () => {
    try {
      // Gọi song song 4 endpoint
      const [statsRes, statusRes, monthRes, topRes] = await Promise.all([
        dashboardApi.getStats(),
        dashboardApi.getHoSoByStatus(),
        dashboardApi.getGiaiNganTheoThang(),
        dashboardApi.getTopKhachHang(10),
      ]);
      setStats(statsRes.data);
      setHoSoByStatus(statusRes.data);
      setGiaiNganTheoThang(monthRes.data);
      setTopKhachHang(topRes.data);
    } catch (error) {
      console.error('Failed to load dashboard:', error);
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return <Loading text="Đang tải dashboard..." />;
  }

  // KHÔNG hiển thị % thay đổi bịa đặt (trước đây hardcode +12%, +8%, +5%, +15%
  // không dựa trên dữ liệu nào). Thay bằng kỳ thực tế mà số liệu đại diện.
  const statCards = [
    {
      title: 'Tổng khách hàng',
      value: stats?.tongKhachHang || 0,
      icon: Users,
      color: 'blue',
      caption: 'toàn hệ thống',
    },
    {
      title: 'Hồ sơ vay',
      value: stats?.tongHoSoVay || 0,
      icon: FileText,
      color: 'green',
      caption: `${stats?.hoSoMoi || 0} hồ sơ mới`,
    },
    {
      title: 'Hợp đồng',
      value: stats?.tongHopDong || 0,
      icon: FileSignature,
      color: 'purple',
      caption: `${stats?.hoSoDaDuyet || 0} hồ sơ đã duyệt`,
    },
    {
      title: 'Tổng giải ngân',
      value: formatCurrency(stats?.tongGiaiNgan || 0),
      icon: DollarSign,
      color: 'teal',
      caption: `tháng này: ${formatCurrency(stats?.tienGiaiNganThangNay || 0)}`,
    },
  ];

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-800">Dashboard</h1>
          <p className="text-gray-500 text-sm">
            Cập nhật: {new Date().toLocaleDateString('vi-VN')}
          </p>
        </div>
        <ExportButton
          label="báo cáo"
          onExport={(format) => exportApi.exportDashboard(format)}
        />
      </div>

      {/* Stats Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        {statCards.map((stat) => {
          const Icon = stat.icon;
          const colorClasses = {
            blue: 'bg-blue-50 text-blue-600',
            green: 'bg-green-50 text-green-600',
            purple: 'bg-purple-50 text-purple-600',
            teal: 'bg-teal-50 text-teal-600',
          };

          return (
            <div
              key={stat.title}
              className="bg-white rounded-xl shadow-sm p-6 border border-gray-100"
            >
              <div className="flex items-center justify-between mb-4">
                <div
                  className={`p-3 rounded-lg ${colorClasses[stat.color as keyof typeof colorClasses]}`}
                >
                  <Icon size={24} />
                </div>
                <span className="text-sm text-gray-400 font-medium">
                  {stat.caption}
                </span>
              </div>
              <p className="text-gray-500 text-sm">{stat.title}</p>
              <p className="text-2xl font-bold text-gray-800 mt-1">
                {stat.value}
              </p>
            </div>
          );
        })}
      </div>

      {/* Two Column Layout */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Hồ sơ theo trạng thái (data thật từ API) */}
        <div className="bg-white rounded-xl shadow-sm p-6 border border-gray-100">
          <div className="flex items-center gap-2 mb-4">
            <BarChart3 className="w-5 h-5 text-indigo-600" />
            <h2 className="text-lg font-semibold text-gray-800">
              Hồ sơ vay theo trạng thái
            </h2>
          </div>
          {hoSoByStatus.length === 0 ? (
            <p className="text-gray-500 text-sm text-center py-4">Chưa có dữ liệu</p>
          ) : (() => {
            const max = Math.max(...hoSoByStatus.map((x) => x.soLuong), 1);
            return (
              <div className="space-y-3">
                {hoSoByStatus.map((item) => {
                  const config = TRANG_THAI_HO_SO[item.trangThai as keyof typeof TRANG_THAI_HO_SO];
                  const widthPct = (item.soLuong / max) * 100;
                  const colorClass = config?.color === 'green' ? 'bg-green-500'
                    : config?.color === 'red' ? 'bg-red-500'
                    : config?.color === 'yellow' ? 'bg-yellow-500'
                    : config?.color === 'purple' ? 'bg-purple-500'
                    : 'bg-blue-500';
                  return (
                    <div key={item.trangThai}>
                      <div className="flex items-center justify-between text-sm mb-1">
                        <span className="text-gray-700">{config?.label || item.trangThai}</span>
                        <span className="font-semibold text-gray-800">
                          {item.soLuong} <span className="text-xs text-gray-500">({formatCurrency(item.tongTien)})</span>
                        </span>
                      </div>
                      <div className="w-full bg-gray-100 rounded-full h-2 overflow-hidden">
                        <div
                          className={`h-full ${colorClass} rounded-full transition-all`}
                          style={{ width: `${widthPct}%` }}
                        />
                      </div>
                    </div>
                  );
                })}
              </div>
            );
          })()}
        </div>

        {/* Cảnh báo */}
        <div className="bg-white rounded-xl shadow-sm p-6 border border-gray-100">
          <h2 className="text-lg font-semibold text-gray-800 mb-4">
            Cảnh báo & Thông tin
          </h2>
          <div className="space-y-4">
            <div className="flex items-start gap-3 p-3 bg-yellow-50 rounded-lg">
              <AlertCircle className="text-yellow-600 flex-shrink-0 mt-0.5" />
              <div>
                <p className="font-medium text-yellow-800">
                  Tỷ lệ quá hạn
                </p>
                <p className="text-sm text-yellow-700">
                  {(stats?.tiLeQuaHan || 0).toFixed(2)}%
                </p>
              </div>
            </div>
            <div className="flex items-start gap-3 p-3 bg-blue-50 rounded-lg">
              <DollarSign className="text-blue-600 flex-shrink-0 mt-0.5" />
              <div>
                <p className="font-medium text-blue-800">
                  Giải ngân tháng này
                </p>
                <p className="text-sm text-blue-700">
                  {formatCurrency(stats?.tienGiaiNganThangNay || 0)}
                </p>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Giải ngân 12 tháng gần nhất */}
      <div className="bg-white rounded-xl shadow-sm p-6 border border-gray-100">
        <div className="flex items-center gap-2 mb-4">
          <Calendar className="w-5 h-5 text-emerald-600" />
          <h2 className="text-lg font-semibold text-gray-800">Giải ngân 12 tháng gần nhất</h2>
        </div>
        {giaiNganTheoThang.length === 0 ? (
          <p className="text-gray-500 text-sm text-center py-8">Chưa có dữ liệu giải ngân</p>
        ) : (() => {
          const max = Math.max(...giaiNganTheoThang.map((x) => x.value), 1);
          return (
            <div className="flex items-end justify-between gap-2 h-48 px-2">
              {giaiNganTheoThang.map((item) => {
                const heightPct = (item.value / max) * 100;
                return (
                  <div key={item.label} className="flex-1 flex flex-col items-center gap-1 group">
                    <div className="relative w-full flex items-end justify-center h-full">
                      <div
                        className="w-full bg-emerald-500 hover:bg-emerald-600 rounded-t transition-all cursor-pointer"
                        style={{ height: `${heightPct}%`, minHeight: item.value > 0 ? '4px' : '0' }}
                        title={`${item.label}: ${formatCurrency(item.value)}`}
                      />
                    </div>
                    <span className="text-xs text-gray-500 group-hover:text-gray-700">{item.label}</span>
                  </div>
                );
              })}
            </div>
          );
        })()}
      </div>

      {/* Top 10 khách hàng */}
      <div className="bg-white rounded-xl shadow-sm p-6 border border-gray-100">
        <div className="flex items-center gap-2 mb-4">
          <Trophy className="w-5 h-5 text-yellow-500" />
          <h2 className="text-lg font-semibold text-gray-800">Top 10 khách hàng vay nhiều nhất</h2>
        </div>
        {topKhachHang.length === 0 ? (
          <p className="text-gray-500 text-sm text-center py-4">Chưa có dữ liệu</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-gray-200 text-sm">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-3 py-2 text-left text-xs font-medium text-gray-500 uppercase">#</th>
                  <th className="px-3 py-2 text-left text-xs font-medium text-gray-500 uppercase">Khách hàng</th>
                  <th className="px-3 py-2 text-left text-xs font-medium text-gray-500 uppercase">Số giấy tờ</th>
                  <th className="px-3 py-2 text-center text-xs font-medium text-gray-500 uppercase">Số HĐ</th>
                  <th className="px-3 py-2 text-right text-xs font-medium text-gray-500 uppercase">Tổng vay</th>
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-gray-200">
                {topKhachHang.map((kh, idx) => (
                  <tr key={kh.maKhachHang} className="hover:bg-gray-50">
                    <td className="px-3 py-2 text-gray-500">{idx + 1}</td>
                    <td className="px-3 py-2 font-medium text-gray-800">{kh.tenKh}</td>
                    <td className="px-3 py-2 text-gray-600">{kh.soGiayTo}</td>
                    <td className="px-3 py-2 text-center text-gray-600">{kh.soHopDong}</td>
                    <td className="px-3 py-2 text-right font-semibold text-green-700">
                      {formatCurrency(kh.tongVay)}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
};

export default Dashboard;
