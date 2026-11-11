import { useState, useEffect, useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import { Receipt, Filter, DollarSign, CreditCard, Wallet, TrendingUp } from 'lucide-react';
import { thanhToanApi, ThanhToan } from '../../../api/thanhtoan.api';
import { hopDongApi } from '../../../api/hopdong.api';
import { exportApi } from '../../../api/export.api';
import Table from '../../../components/common/Table';
import Button from '../../../components/common/Button';
import Modal from '../../../components/common/Modal';
import Input from '../../../components/common/Input';
import Select from '../../../components/common/Select';
import Loading from '../../../components/common/Loading';
import ExportButton from '../../../components/common/ExportButton';
import { formatCurrency, formatDateTime } from '../../../utils/formatters';
import { PERM } from '../../../utils/permissions';
import { usePermission } from '../../../hooks/usePermission';

interface ScheduleItem {
  maLichTraNo: number;
  maHopDong: number;
  kyThanhToan: number;
  ngayDenHan: string;
  gocPhaiTra: number;
  laiPhaiTra: number;
  phiPhat: number;
  trangThai: string;
}

interface ContractOption {
  maHopDong: number;
  soHopDong: string;
  maHoSo: number;
  tenKhachHang?: string;
}

const PaymentList = () => {
  const navigate = useNavigate();
  const { can } = usePermission();
  const [payments, setPayments] = useState<ThanhToan[]>([]);
  const [loading, setLoading] = useState(true);
  const [modalOpen, setModalOpen] = useState(false);

  // Bộ lọc
  const [filterTuNgay, setFilterTuNgay] = useState('');
  const [filterDenNgay, setFilterDenNgay] = useState('');
  const [filterKenh, setFilterKenh] = useState('');
  const [filterSearch, setFilterSearch] = useState('');

  // Form 2 bước
  const [contracts, setContracts] = useState<ContractOption[]>([]);
  const [formContractId, setFormContractId] = useState<number | ''>('');
  const [schedules, setSchedules] = useState<ScheduleItem[]>([]);
  const [loadingSchedules, setLoadingSchedules] = useState(false);
  const [formScheduleId, setFormScheduleId] = useState<number | ''>('');
  const [formData, setFormData] = useState({
    soTienApGoc: 0,
    soTienApLai: 0,
    soTienApPhi: 0,
    ngayThanhToan: new Date().toISOString().split('T')[0],
    phuongThucThanhToan: 'TIEN_MAT',
    ghiChu: '',
  });
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    loadPayments();
    loadContractsForForm();
  }, []);

  const loadPayments = async () => {
    setLoading(true);
    try {
      // Lấy trang đầu với pageSize lớn (client-side filtering)
      const response = await thanhToanApi.getAll({ pageSize: 500 });
      setPayments(response.data.items);
    } catch (error) {
      console.error('Failed to load:', error);
    } finally {
      setLoading(false);
    }
  };

  const loadContractsForForm = async () => {
    try {
      // Lấy danh sách hợp đồng đang hoạt động
      const res = await hopDongApi.getAll({ pageSize: 500 });
      setContracts(
        res.data.items.map((c) => ({
          maHopDong: c.maHopDong,
          soHopDong: c.soHopDong,
          maHoSo: c.maHoSo,
        }))
      );
    } catch (err) {
      console.error('Failed to load contracts:', err);
    }
  };

  // Load lịch trả nợ chưa thanh toán khi chọn hợp đồng
  const handleSelectContract = async (maHopDong: number | '') => {
    setFormContractId(maHopDong);
    setFormScheduleId('');
    setSchedules([]);
    if (!maHopDong) return;
    setLoadingSchedules(true);
    try {
      const res = await hopDongApi.getLichTraNo(maHopDong);
      const all = res.data as ScheduleItem[];
      // Lấy các kỳ chưa thanh toán hết
      const chuaThanhToan = all.filter((s) => s.trangThai !== 'DA_THANH_TOAN');
      setSchedules(chuaThanhToan);
    } catch (err) {
      console.error('Failed to load schedules:', err);
    } finally {
      setLoadingSchedules(false);
    }
  };

  // Tự điền số tiền khi chọn kỳ
  const handleSelectSchedule = (maLichTraNo: number | '') => {
    setFormScheduleId(maLichTraNo);
    if (!maLichTraNo) return;
    const item = schedules.find((s) => s.maLichTraNo === maLichTraNo);
    if (item) {
      setFormData({
        ...formData,
        soTienApGoc: item.gocPhaiTra,
        soTienApLai: item.laiPhaiTra,
        soTienApPhi: item.phiPhat || 0,
      });
    }
  };

  const resetForm = () => {
    setFormContractId('');
    setFormScheduleId('');
    setSchedules([]);
    setFormData({
      soTienApGoc: 0,
      soTienApLai: 0,
      soTienApPhi: 0,
      ngayThanhToan: new Date().toISOString().split('T')[0],
      phuongThucThanhToan: 'TIEN_MAT',
      ghiChu: '',
    });
    setError('');
  };

  const handleOpenModal = () => {
    resetForm();
    setModalOpen(true);
  };

  const handleSubmit = async () => {
    if (!formContractId || !formScheduleId) {
      setError('Vui lòng chọn hợp đồng và kỳ thanh toán');
      return;
    }
    if (formData.soTienApGoc + formData.soTienApLai + formData.soTienApPhi <= 0) {
      setError('Số tiền thanh toán phải lớn hơn 0');
      return;
    }
    setSaving(true);
    setError('');
    try {
      const contract = contracts.find((c) => c.maHopDong === formContractId);
      await thanhToanApi.create({
        maLichTraNo: formScheduleId,
        maHopDong: formContractId,
        maHoSo: contract?.maHoSo || 0,
        soTienDaTra: formData.soTienApGoc + formData.soTienApLai + formData.soTienApPhi,
        soTienApGoc: formData.soTienApGoc,
        soTienApLai: formData.soTienApLai,
        soTienApPhi: formData.soTienApPhi,
        ngayThanhToan: formData.ngayThanhToan,
        phuongThucThanhToan: formData.phuongThucThanhToan,
        ghiChu: formData.ghiChu || undefined,
      });
      setModalOpen(false);
      loadPayments();
    } catch (err: any) {
      setError(err.response?.data?.message || 'Lưu thanh toán thất bại');
    } finally {
      setSaving(false);
    }
  };

  // Lọc phía client
  const filteredPayments = useMemo(() => {
    return payments.filter((p) => {
      // Lọc theo ngày
      if (filterTuNgay) {
        const tuNgay = new Date(filterTuNgay);
        const ngayTT = new Date(p.ngayThanhToan);
        if (ngayTT < tuNgay) return false;
      }
      if (filterDenNgay) {
        const denNgay = new Date(filterDenNgay);
        denNgay.setHours(23, 59, 59, 999);
        const ngayTT = new Date(p.ngayThanhToan);
        if (ngayTT > denNgay) return false;
      }
      // Lọc theo kênh
      if (filterKenh && p.phuongThucThanhToan !== filterKenh) return false;
      // Lọc theo search
      if (filterSearch) {
        const q = filterSearch.toLowerCase();
        const match =
          String(p.maThanhToan).includes(q) ||
          String(p.maHopDong).includes(q) ||
          String(p.maHoSo).includes(q) ||
          (p.maGiaoDich || '').toLowerCase().includes(q) ||
          (p.soHopDong || '').toLowerCase().includes(q) ||
          (p.maHoSoHienThi || '').toLowerCase().includes(q);
        if (!match) return false;
      }
      return true;
    });
  }, [payments, filterTuNgay, filterDenNgay, filterKenh, filterSearch]);

  // Thống kê tổng quan (tính trên dữ liệu đã lọc)
  const stats = useMemo(() => {
    const total = filteredPayments.length;
    const totalAmount = filteredPayments.reduce((sum, p) => sum + p.soTienDaTra, 0);
    const tienMat = filteredPayments
      .filter((p) => p.phuongThucThanhToan === 'TIEN_MAT')
      .reduce((sum, p) => sum + p.soTienDaTra, 0);
    const chuyenKhoan = filteredPayments
      .filter((p) => p.phuongThucThanhToan === 'CHUYEN_KHOAN')
      .reduce((sum, p) => sum + p.soTienDaTra, 0);
    return { total, totalAmount, tienMat, chuyenKhoan };
  }, [filteredPayments]);

  const handleViewContract = (maHopDong: number) => {
    navigate(`/contracts?maHopDong=${maHopDong}`);
  };

  const columns = [
    {
      key: 'maThanhToan',
      title: 'Mã TT',
      width: '90px',
      render: (r: ThanhToan) => <span className="font-mono text-xs text-gray-600">#{r.maThanhToan}</span>,
    },
    {
      key: 'soHopDong',
      title: 'Hợp đồng',
      render: (r: ThanhToan) => (
        <button
          type="button"
          onClick={() => handleViewContract(r.maHopDong)}
          className="text-blue-600 hover:text-blue-800 hover:underline font-medium"
          title="Xem chi tiết hợp đồng"
        >
          {r.soHopDong}
        </button>
      ),
    },
    { key: 'maHoSoHienThi', title: 'Mã hồ sơ' },
    { key: 'maLichTraNo', title: 'Kỳ', align: 'center' as const },
    {
      key: 'soTienDaTra',
      title: 'Số tiền',
      align: 'right' as const,
      render: (r: ThanhToan) => (
        <span className="font-semibold text-green-700">{formatCurrency(r.soTienDaTra)}</span>
      ),
    },
    { key: 'soTienApGoc', title: 'Gốc', align: 'right' as const, render: (r: ThanhToan) => formatCurrency(r.soTienApGoc) },
    { key: 'soTienApLai', title: 'Lãi', align: 'right' as const, render: (r: ThanhToan) => formatCurrency(r.soTienApLai) },
    {
      key: 'phuongThucThanhToan',
      title: 'Kênh',
      align: 'center' as const,
      render: (r: ThanhToan) => {
        const label =
          r.phuongThucThanhToan === 'TIEN_MAT' ? 'Tiền mặt' :
          r.phuongThucThanhToan === 'CHUYEN_KHOAN' ? 'Chuyển khoản' :
          r.phuongThucThanhToan === 'THE' ? 'Thẻ' : (r.phuongThucThanhToan || '-');
        const color =
          r.phuongThucThanhToan === 'TIEN_MAT' ? 'blue' :
          r.phuongThucThanhToan === 'CHUYEN_KHOAN' ? 'purple' :
          r.phuongThucThanhToan === 'THE' ? 'cyan' : 'gray';
        return (
          <span className={`inline-block px-2 py-0.5 text-xs rounded border ${
            color === 'blue' ? 'bg-blue-50 text-blue-700 border-blue-200' :
            color === 'purple' ? 'bg-purple-50 text-purple-700 border-purple-200' :
            color === 'cyan' ? 'bg-cyan-50 text-cyan-700 border-cyan-200' :
            'bg-gray-50 text-gray-700 border-gray-200'
          }`}>{label}</span>
        );
      },
    },
    { key: 'ngayThanhToan', title: 'Ngày TT', render: (r: ThanhToan) => formatDateTime(r.ngayThanhToan) },
  ];

  if (loading) return <Loading text="Đang tải thanh toán..." />;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <Receipt className="w-6 h-6 text-blue-600" />
          <h1 className="text-2xl font-bold text-gray-800">Quản lý thanh toán</h1>
        </div>
        <div className="flex items-center gap-2">
          <ExportButton
            label="TT"
            onExport={(format) => exportApi.exportThanhToan({
              format,
              tuNgay: filterTuNgay ? `${filterTuNgay}T00:00:00` : undefined,
              denNgay: filterDenNgay ? `${filterDenNgay}T23:59:59` : undefined,
            })}
          />
          {can(PERM.CREATE_PAYMENT) && (
            <Button leftIcon={<DollarSign size={18} />} onClick={handleOpenModal}>
              Tạo thanh toán
            </Button>
          )}
        </div>
      </div>

      {/* Thống kê tổng quan */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        <div className="bg-white rounded-xl shadow-sm p-5 border border-gray-100">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-gray-500 text-sm">Số giao dịch</p>
              <p className="text-2xl font-bold text-gray-800 mt-1">{stats.total}</p>
            </div>
            <div className="p-3 bg-blue-50 text-blue-600 rounded-lg">
              <Receipt size={24} />
            </div>
          </div>
        </div>
        <div className="bg-white rounded-xl shadow-sm p-5 border border-gray-100">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-gray-500 text-sm">Tổng tiền</p>
              <p className="text-xl font-bold text-green-700 mt-1">{formatCurrency(stats.totalAmount)}</p>
            </div>
            <div className="p-3 bg-green-50 text-green-600 rounded-lg">
              <TrendingUp size={24} />
            </div>
          </div>
        </div>
        <div className="bg-white rounded-xl shadow-sm p-5 border border-gray-100">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-gray-500 text-sm">Tiền mặt</p>
              <p className="text-xl font-bold text-blue-700 mt-1">{formatCurrency(stats.tienMat)}</p>
            </div>
            <div className="p-3 bg-blue-50 text-blue-600 rounded-lg">
              <Wallet size={24} />
            </div>
          </div>
        </div>
        <div className="bg-white rounded-xl shadow-sm p-5 border border-gray-100">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-gray-500 text-sm">Chuyển khoản</p>
              <p className="text-xl font-bold text-purple-700 mt-1">{formatCurrency(stats.chuyenKhoan)}</p>
            </div>
            <div className="p-3 bg-purple-50 text-purple-600 rounded-lg">
              <CreditCard size={24} />
            </div>
          </div>
        </div>
      </div>

      {/* Bộ lọc */}
      <div className="bg-white rounded-xl shadow-sm p-4 border border-gray-100">
        <div className="flex items-center gap-2 mb-3">
          <Filter size={16} className="text-gray-500" />
          <h3 className="font-medium text-gray-700">Bộ lọc</h3>
          {(filterTuNgay || filterDenNgay || filterKenh || filterSearch) && (
            <button
              type="button"
              onClick={() => {
                setFilterTuNgay('');
                setFilterDenNgay('');
                setFilterKenh('');
                setFilterSearch('');
              }}
              className="ml-auto text-xs text-blue-600 hover:text-blue-800"
            >
              Xóa bộ lọc
            </button>
          )}
        </div>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-3">
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
          <Select
            label="Kênh thanh toán"
            value={filterKenh}
            onChange={(value) => setFilterKenh(String(value))}
            options={[
              { value: '', label: 'Tất cả' },
              { value: 'TIEN_MAT', label: 'Tiền mặt' },
              { value: 'CHUYEN_KHOAN', label: 'Chuyển khoản' },
              { value: 'THE', label: 'Thẻ' },
            ]}
          />
          <Input
            label="Tìm kiếm"
            placeholder="Mã HĐ, mã TT, mã giao dịch..."
            value={filterSearch}
            onChange={(e) => setFilterSearch(e.target.value)}
          />
        </div>
      </div>

      {/* Bảng thanh toán */}
      <div className="bg-white rounded-xl shadow-sm border border-gray-100">
        <Table
          columns={columns}
          data={filteredPayments}
          emptyText={payments.length === 0 ? 'Chưa có thanh toán nào' : 'Không có thanh toán nào khớp bộ lọc'}
        />
      </div>

      {/* Modal tạo thanh toán - 2 bước */}
      <Modal
        open={modalOpen}
        onClose={() => setModalOpen(false)}
        title="Tạo thanh toán mới"
        size="lg"
        footer={
          <>
            <Button variant="outline" onClick={() => setModalOpen(false)}>Hủy</Button>
            <Button loading={saving} onClick={handleSubmit}>Lưu thanh toán</Button>
          </>
        }
      >
        <div className="space-y-4">
          {error && (
            <div className="bg-red-50 border border-red-200 text-red-600 px-4 py-3 rounded-lg text-sm">
              {error}
            </div>
          )}

          {/* Bước 1: Chọn hợp đồng & kỳ */}
          <div className="bg-blue-50 border border-blue-100 p-4 rounded-lg">
            <div className="flex items-center gap-2 mb-3">
              <span className="w-6 h-6 rounded-full bg-blue-600 text-white text-xs flex items-center justify-center font-semibold">1</span>
              <h3 className="font-semibold text-gray-800">Chọn hợp đồng & kỳ thanh toán</h3>
            </div>
            <div className="grid grid-cols-2 gap-3">
              <Select
                label="Hợp đồng"
                value={formContractId}
                onChange={(value) => handleSelectContract(value ? Number(value) : '')}
                options={[
                  { value: '', label: '-- Chọn hợp đồng --' },
                  ...contracts.map((c) => ({
                    value: c.maHopDong,
                    label: `${c.soHopDong} - HS #${c.maHoSo}`,
                  })),
                ]}
              />
              <Select
                label="Kỳ thanh toán"
                value={formScheduleId}
                onChange={(value) => handleSelectSchedule(value ? Number(value) : '')}
                options={(() => {
                  if (!formContractId) return [{ value: '', label: '-- Chọn hợp đồng trước --' }];
                  if (loadingSchedules) return [{ value: '', label: 'Đang tải...' }];
                  if (schedules.length === 0) return [{ value: '', label: 'Hợp đồng không có kỳ nào' }];
                  return [
                    { value: '', label: '-- Chọn kỳ --' },
                    ...schedules.map((s) => ({
                      value: s.maLichTraNo,
                      label: `Kỳ ${s.kyThanhToan} - ${formatCurrency(s.gocPhaiTra + s.laiPhaiTra)}`,
                    })),
                  ];
                })()}
              />
            </div>
          </div>

          {/* Bước 2: Nhập số tiền */}
          <div className="bg-green-50 border border-green-100 p-4 rounded-lg">
            <div className="flex items-center gap-2 mb-3">
              <span className="w-6 h-6 rounded-full bg-green-600 text-white text-xs flex items-center justify-center font-semibold">2</span>
              <h3 className="font-semibold text-gray-800">Số tiền thanh toán</h3>
            </div>
            <div className="grid grid-cols-2 gap-3">
              <Input
                label="Số tiền áp gốc"
                type="number"
                value={formData.soTienApGoc}
                onChange={(e) => setFormData({ ...formData, soTienApGoc: Number(e.target.value) })}
              />
              <Input
                label="Số tiền áp lãi"
                type="number"
                value={formData.soTienApLai}
                onChange={(e) => setFormData({ ...formData, soTienApLai: Number(e.target.value) })}
              />
              <Input
                label="Số tiền phạt (nếu có)"
                type="number"
                value={formData.soTienApPhi}
                onChange={(e) => setFormData({ ...formData, soTienApPhi: Number(e.target.value) })}
              />
              <Input
                label="Ngày thanh toán"
                type="date"
                value={formData.ngayThanhToan}
                onChange={(e) => setFormData({ ...formData, ngayThanhToan: e.target.value })}
              />
              <Select
                label="Phương thức"
                value={formData.phuongThucThanhToan}
                onChange={(value) => setFormData({ ...formData, phuongThucThanhToan: String(value) })}
                options={[
                  { value: 'TIEN_MAT', label: 'Tiền mặt' },
                  { value: 'CHUYEN_KHOAN', label: 'Chuyển khoản' },
                  { value: 'THE', label: 'Thẻ' },
                ]}
                className="col-span-2"
              />
              <Input
                label="Ghi chú"
                value={formData.ghiChu}
                onChange={(e) => setFormData({ ...formData, ghiChu: e.target.value })}
                className="col-span-2"
              />
            </div>
            <div className="mt-3 pt-3 border-t border-green-200 flex justify-between items-center">
              <span className="text-gray-600 text-sm">Tổng tiền thanh toán:</span>
              <span className="text-xl font-bold text-green-700">
                {formatCurrency(formData.soTienApGoc + formData.soTienApLai + formData.soTienApPhi)}
              </span>
            </div>
          </div>
        </div>
      </Modal>
    </div>
  );
};

export default PaymentList;
