import { useState, useEffect } from 'react';
import { useSearchParams } from 'react-router-dom';
import { Plus, Eye, FileText, Calendar, DollarSign, Percent, Hash, CheckCircle2, Clock, CreditCard } from 'lucide-react';
import { hopDongApi, HopDongVay } from '../../../api/hopdong.api';
import { hoSoVayApi } from '../../../api/hosovay.api';
import { thanhToanApi } from '../../../api/thanhtoan.api';
import { exportApi } from '../../../api/export.api';
import Table from '../../../components/common/Table';
import Button from '../../../components/common/Button';
import Input from '../../../components/common/Input';
import Select from '../../../components/common/Select';
import Modal from '../../../components/common/Modal';
import StatusBadge from '../../../components/common/StatusBadge';
import Loading from '../../../components/common/Loading';
import ExportButton from '../../../components/common/ExportButton';
import { formatCurrency, formatDate } from '../../../utils/formatters';
import { TRANG_THAI_HOP_DONG } from '../../../utils/constants';
import { PERM } from '../../../utils/permissions';
import { usePermission } from '../../../hooks/usePermission';

interface ContractFormData {
  maHoSo: number;
  ngayBatDau: string;
  ngayDaoHan: string;
  ghiChu?: string;
}

interface LichTraNoItem {
  maLichTraNo: number;
  maHopDong: number;
  kyThanhToan: number;
  ngayDenHan: string;
  gocPhaiTra: number;
  laiPhaiTra: number;
  phiPhat: number;
  trangThai: string;
}

const ContractList = () => {
  const { can } = usePermission();
  const [searchParams, setSearchParams] = useSearchParams();
  const [contracts, setContracts] = useState<HopDongVay[]>([]);
  const [loading, setLoading] = useState(true);
  const [modalOpen, setModalOpen] = useState(false);
  const [approvedLoans, setApprovedLoans] = useState<any[]>([]);
  const [formData, setFormData] = useState<ContractFormData>({
    maHoSo: 0,
    ngayBatDau: '',
    ngayDaoHan: '',
    ghiChu: '',
  });
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  // Chi tiết hợp đồng
  const [detailOpen, setDetailOpen] = useState(false);
  const [selectedDetail, setSelectedDetail] = useState<HopDongVay | null>(null);
  const [lichTraNo, setLichTraNo] = useState<LichTraNoItem[]>([]);
  const [detailLoading, setDetailLoading] = useState(false);

  // Modal tạo thanh toán từ lịch trả nợ
  const [paymentModalOpen, setPaymentModalOpen] = useState(false);
  const [payingSchedule, setPayingSchedule] = useState<LichTraNoItem | null>(null);
  const [paymentForm, setPaymentForm] = useState({
    soTienApGoc: 0,
    soTienApLai: 0,
    soTienApPhi: 0,
    ngayThanhToan: new Date().toISOString().split('T')[0],
    phuongThucThanhToan: 'TIEN_MAT',
    ghiChu: '',
  });
  const [paymentSaving, setPaymentSaving] = useState(false);
  const [paymentError, setPaymentError] = useState('');

  useEffect(() => {
    loadContracts();
    loadApprovedLoans();
  }, []);

  // Deep-link: /contracts?maHopDong=123 → mở modal chi tiết của hợp đồng đó
  // (trước đây param này không được đọc nên link từ PaymentList chỉ "chết").
  useEffect(() => {
    const maHopDong = searchParams.get('maHopDong');
    if (!maHopDong) return;
    const id = Number(maHopDong);
    if (!id) return;

    const openDetail = async () => {
      try {
        const res = await hopDongApi.getById(id);
        if (res.data) handleViewDetail(res.data);
      } catch (err) {
        console.error('Không mở được hợp đồng từ deep-link:', err);
      }
    };
    openDetail();
    // Xoá param sau khi dùng để F5 không mở lại modal
    searchParams.delete('maHopDong');
    setSearchParams(searchParams, { replace: true });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);
  const loadContracts = async () => {
    setLoading(true);
    try {
      const response = await hopDongApi.getAll({ pageSize: 100 });
      setContracts(response.data.items);
    } catch (err) {
      console.error('Failed to load:', err);
    } finally {
      setLoading(false);
    }
  };

  const loadApprovedLoans = async () => {
    try {
      const response = await hoSoVayApi.getAll({ pageSize: 100, trangThai: 'DA_PHE_DUYET' });
      setApprovedLoans(response.data.items);
    } catch (err) {
      console.error('Failed to load loans:', err);
    }
  };

  const handleOpenModal = () => {
    setError('');
    setFormData({ maHoSo: 0, ngayBatDau: '', ngayDaoHan: '', ghiChu: '' });
    setModalOpen(true);
  };

  const handleSave = async () => {
    if (!formData.maHoSo || !formData.ngayBatDau || !formData.ngayDaoHan) {
      setError('Vui long dien day du thong tin bat buoc');
      return;
    }
    setSaving(true);
    setError('');
    try {
      await hopDongApi.create(formData as any);
      setModalOpen(false);
      // Phải reload cả danh sách hồ sơ chờ: sau khi tạo hợp đồng, hồ sơ đó
      // đã có hợp đồng → không còn hợp lệ để chọn. Trước đây danh sách cũ
      // không được refresh → submit lần 2 bị backend trả 400.
      await Promise.all([loadContracts(), loadApprovedLoans()]);
    } catch (err: any) {
      setError(err.response?.data?.message || 'Luu that bai');
    } finally {
      setSaving(false);
    }
  };

  const handleGiaiNgan = async (id: number) => {
    if (confirm('Xac nhan giai ngan?')) {
      try {
        await hopDongApi.giaiNgan(id);
        loadContracts();
      } catch (err) {
        console.error('Failed:', err);
      }
    }
  };

  const handleViewDetail = async (contract: HopDongVay) => {
    setSelectedDetail(contract);
    setDetailOpen(true);
    setDetailLoading(true);
    setLichTraNo([]);
    try {
      const res = await hopDongApi.getLichTraNo(contract.maHopDong);
      setLichTraNo(res.data as LichTraNoItem[]);
    } catch (err) {
      console.error('Failed to load lich tra no:', err);
    } finally {
      setDetailLoading(false);
    }
  };

  const getLoanOptions = (loans: any[]) => {
    return loans.map((loan) => ({
      value: loan.maHoSo,
      label: `${loan.maHoSoHienThi} - ${loan.tenKhachHang} - ${formatCurrency(loan.soTienDeNghi)}`,
    }));
  };

  const getPaymentStatusConfig = (status: string) => {
    switch (status) {
      case 'DA_THANH_TOAN':
        return { label: 'Đã TT', color: 'green' as const };
      case 'THANH_TOAN_MOT_PHAN':
        return { label: 'TT 1 phần', color: 'blue' as const };
      case 'TRE_HAN':
        return { label: 'Trễ hạn', color: 'red' as const };
      case 'CHUA_THANH_TOAN':
      default:
        return { label: 'Chưa TT', color: 'yellow' as const };
    }
  };

  const handleOpenPayment = (item: LichTraNoItem) => {
    if (!selectedDetail) return;
    setPayingSchedule(item);
    setPaymentForm({
      soTienApGoc: item.gocPhaiTra,
      soTienApLai: item.laiPhaiTra,
      soTienApPhi: 0,
      ngayThanhToan: new Date().toISOString().split('T')[0],
      phuongThucThanhToan: 'TIEN_MAT',
      ghiChu: '',
    });
    setPaymentError('');
    setPaymentModalOpen(true);
  };

  const handleSubmitPayment = async () => {
    if (!selectedDetail || !payingSchedule) return;
    setPaymentSaving(true);
    setPaymentError('');
    try {
      await thanhToanApi.create({
        maLichTraNo: payingSchedule.maLichTraNo,
        maHopDong: selectedDetail.maHopDong,
        maHoSo: selectedDetail.maHoSo,
        soTienDaTra: paymentForm.soTienApGoc + paymentForm.soTienApLai + paymentForm.soTienApPhi,
        soTienApGoc: paymentForm.soTienApGoc,
        soTienApLai: paymentForm.soTienApLai,
        soTienApPhi: paymentForm.soTienApPhi,
        ngayThanhToan: paymentForm.ngayThanhToan,
        phuongThucThanhToan: paymentForm.phuongThucThanhToan,
        ghiChu: paymentForm.ghiChu || undefined,
      });
      setPaymentModalOpen(false);
      // Reload lich tra no de cap nhat trang thai moi
      const res = await hopDongApi.getLichTraNo(selectedDetail.maHopDong);
      setLichTraNo(res.data as LichTraNoItem[]);
    } catch (err: any) {
      setPaymentError(err.response?.data?.message || 'Lưu thanh toán thất bại');
    } finally {
      setPaymentSaving(false);
    }
  };

  const columns = [
    {
      key: 'soHopDong',
      title: 'So HD',
      render: (r: HopDongVay) => <span className="font-medium text-blue-600">{r.soHopDong || 'Chua co'}</span>,
    },
    { key: 'maHoSo', title: 'Ma HS' },
    {
      key: 'tienGoc',
      title: 'So tien vay',
      align: 'right' as const,
      render: (r: HopDongVay) => formatCurrency(r.tienGoc || 0),
    },
    {
      key: 'laiSuat',
      title: 'Lai suat',
      align: 'center' as const,
      render: (r: HopDongVay) => ((r.laiSuat || 0) * 100).toFixed(2) + '%',
    },
    {
      key: 'ngayBatDau',
      title: 'Ngay bat dau',
      render: (r: HopDongVay) => formatDate(r.ngayBatDau),
    },
    {
      key: 'ngayDaoHan',
      title: 'Ngay dao han',
      render: (r: HopDongVay) => formatDate(r.ngayDaoHan),
    },
    {
      key: 'trangThai',
      title: 'Trang thai',
      render: (r: HopDongVay) => {
        const config = TRANG_THAI_HOP_DONG[r.trangThai as keyof typeof TRANG_THAI_HOP_DONG];
        return <StatusBadge status={r.trangThai} label={config?.label || 'Khong xac dinh'} color={(config?.color as any) || 'gray'} />;
      },
    },
    {
      key: 'actions',
      title: '',
      width: '120px',
      render: (r: HopDongVay) => (
        <div className="flex gap-2">
          {!r.ngayGiaiNgan && r.trangThai === 'HOAT_DONG' && can(PERM.DISBURSE) && (
            <Button size="sm" onClick={() => handleGiaiNgan(r.maHopDong)}>Giai ngan</Button>
          )}
          <button
            type="button"
            onClick={() => handleViewDetail(r)}
            className="p-1.5 hover:bg-gray-100 rounded transition-colors"
            title="Xem chi tiết"
            aria-label="Xem chi tiết hợp đồng"
          >
            <Eye size={16} className="text-blue-600" />
          </button>
        </div>
      ),
    },
  ];

  if (loading) return <Loading text="Dang tai..." />;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-800">Hop dong vay</h1>
        <div className="flex items-center gap-2">
          <ExportButton
            label="HD"
            onExport={(format) => exportApi.exportHopDong({ format })}
          />
          {can(PERM.CREATE_CONTRACT) && (
            <Button leftIcon={<Plus size={18} />} onClick={handleOpenModal}>Tao hop dong</Button>
          )}
        </div>
      </div>

      <div className="bg-white rounded-xl shadow-sm border border-gray-100">
        <Table columns={columns} data={contracts} emptyText="Chua co hop dong" />
      </div>

      <Modal
        open={modalOpen}
        onClose={() => setModalOpen(false)}
        title="Tao hop dong moi"
        size="md"
        footer={
          <>
            <Button variant="outline" onClick={() => setModalOpen(false)}>Huy</Button>
            <Button loading={saving} onClick={handleSave}>Luu</Button>
          </>
        }
      >
        <div className="space-y-4">
          {error && <div className="bg-red-50 border border-red-200 text-red-600 px-4 py-3 rounded-lg">{error}</div>}
          <Select
            label="Ho so da phe duyet"
            value={formData.maHoSo}
            onChange={(value) => setFormData({ ...formData, maHoSo: Number(value) })}
            options={getLoanOptions(approvedLoans)}
            placeholder="Chon ho so"
          />
          <div className="grid grid-cols-2 gap-4">
            <Input
              label="Ngay bat dau"
              type="date"
              value={formData.ngayBatDau}
              onChange={(e) => setFormData({ ...formData, ngayBatDau: e.target.value })}
            />
            <Input
              label="Ngay dao han"
              type="date"
              value={formData.ngayDaoHan}
              onChange={(e) => setFormData({ ...formData, ngayDaoHan: e.target.value })}
            />
          </div>
          <Input
            label="Ghi chu"
            value={formData.ghiChu || ''}
            onChange={(e) => setFormData({ ...formData, ghiChu: e.target.value })}
          />
        </div>
      </Modal>

      {/* Modal chi tiết hợp đồng */}
      <Modal
        open={detailOpen}
        onClose={() => setDetailOpen(false)}
        title="Chi tiết hợp đồng vay"
        size="xl"
      >
        {selectedDetail && (
          <div className="space-y-5">
            {/* Thông tin chung */}
            <div className="bg-gradient-to-r from-blue-50 to-indigo-50 p-4 rounded-lg border border-blue-100">
              <div className="flex items-center justify-between mb-3">
                <div className="flex items-center gap-2">
                  <FileText className="w-5 h-5 text-blue-600" />
                  <h3 className="font-semibold text-gray-800">Thông tin hợp đồng</h3>
                </div>
                <StatusBadge
                  status={selectedDetail.trangThai}
                  label={TRANG_THAI_HOP_DONG[selectedDetail.trangThai as keyof typeof TRANG_THAI_HOP_DONG]?.label || selectedDetail.trangThai}
                  color={(TRANG_THAI_HOP_DONG[selectedDetail.trangThai as keyof typeof TRANG_THAI_HOP_DONG]?.color as any) || 'gray'}
                />
              </div>
              <div className="grid grid-cols-2 gap-3 text-sm">
                <div className="flex items-start gap-2">
                  <Hash className="w-4 h-4 text-gray-400 mt-0.5" />
                  <div>
                    <p className="text-gray-500">Số hợp đồng</p>
                    <p className="font-medium text-blue-700">{selectedDetail.soHopDong}</p>
                  </div>
                </div>
                <div className="flex items-start gap-2">
                  <FileText className="w-4 h-4 text-gray-400 mt-0.5" />
                  <div>
                    <p className="text-gray-500">Mã hồ sơ</p>
                    <p className="font-medium">{selectedDetail.maHoSo}</p>
                  </div>
                </div>
                <div className="flex items-start gap-2">
                  <DollarSign className="w-4 h-4 text-gray-400 mt-0.5" />
                  <div>
                    <p className="text-gray-500">Số tiền gốc</p>
                    <p className="font-semibold text-green-700">{formatCurrency(selectedDetail.tienGoc)}</p>
                  </div>
                </div>
                <div className="flex items-start gap-2">
                  <Percent className="w-4 h-4 text-gray-400 mt-0.5" />
                  <div>
                    <p className="text-gray-500">Lãi suất</p>
                    <p className="font-medium">{((selectedDetail.laiSuat || 0) * 100).toFixed(2)}%/năm</p>
                  </div>
                </div>
                <div className="flex items-start gap-2">
                  <Calendar className="w-4 h-4 text-gray-400 mt-0.5" />
                  <div>
                    <p className="text-gray-500">Ngày bắt đầu</p>
                    <p className="font-medium">{formatDate(selectedDetail.ngayBatDau)}</p>
                  </div>
                </div>
                <div className="flex items-start gap-2">
                  <Calendar className="w-4 h-4 text-gray-400 mt-0.5" />
                  <div>
                    <p className="text-gray-500">Ngày đáo hạn</p>
                    <p className="font-medium">{formatDate(selectedDetail.ngayDaoHan)}</p>
                  </div>
                </div>
                <div className="flex items-start gap-2">
                  <CheckCircle2 className="w-4 h-4 text-gray-400 mt-0.5" />
                  <div>
                    <p className="text-gray-500">Ngày giải ngân</p>
                    <p className="font-medium">
                      {selectedDetail.ngayGiaiNgan ? formatDate(selectedDetail.ngayGiaiNgan) : <span className="text-orange-600">Chưa giải ngân</span>}
                    </p>
                  </div>
                </div>
                <div className="flex items-start gap-2">
                  <DollarSign className="w-4 h-4 text-gray-400 mt-0.5" />
                  <div>
                    <p className="text-gray-500">Tổng phải trả</p>
                    <p className="font-medium">{selectedDetail.tongPhaiTra ? formatCurrency(selectedDetail.tongPhaiTra) : '-'}</p>
                  </div>
                </div>
              </div>
              {selectedDetail.ghiChu && (
                <div className="mt-3 pt-3 border-t border-blue-100">
                  <p className="text-gray-500 text-xs">Ghi chú</p>
                  <p className="text-sm text-gray-700">{selectedDetail.ghiChu}</p>
                </div>
              )}
            </div>

            {/* Lịch trả nợ */}
            <div>
              <div className="flex items-center justify-between mb-3">
                <div className="flex items-center gap-2">
                  <Clock className="w-5 h-5 text-indigo-600" />
                  <h3 className="font-semibold text-gray-800">
                    Lịch trả nợ ({lichTraNo.length} kỳ)
                  </h3>
                </div>
                {lichTraNo.length > 0 && (
                  <ExportButton
                    label="lịch trả nợ"
                    size="sm"
                    onExport={(format) => exportApi.exportLichTraNo(selectedDetail.maHopDong, format)}
                  />
                )}
              </div>
              {detailLoading ? (
                <div className="text-center py-8 text-gray-500">Đang tải lịch trả nợ...</div>
              ) : lichTraNo.length === 0 ? (
                <div className="text-center py-8 bg-gray-50 rounded-lg text-gray-500">
                  Hợp đồng chưa có lịch trả nợ
                </div>
              ) : (
                <div className="border rounded-lg overflow-hidden">
                  <div className="overflow-x-auto max-h-96">
                    <table className="min-w-full divide-y divide-gray-200 text-sm">
                      <thead className="bg-gray-50 sticky top-0">
                        <tr>
                          <th className="px-3 py-2 text-left text-xs font-medium text-gray-500 uppercase">Mã kỳ</th>
                          <th className="px-3 py-2 text-left text-xs font-medium text-gray-500 uppercase">Kỳ</th>
                          <th className="px-3 py-2 text-left text-xs font-medium text-gray-500 uppercase">Ngày đến hạn</th>
                          <th className="px-3 py-2 text-right text-xs font-medium text-gray-500 uppercase">Gốc</th>
                          <th className="px-3 py-2 text-right text-xs font-medium text-gray-500 uppercase">Lãi</th>
                          <th className="px-3 py-2 text-right text-xs font-medium text-gray-500 uppercase">Tổng</th>
                          <th className="px-3 py-2 text-center text-xs font-medium text-gray-500 uppercase">Trạng thái</th>
                          <th className="px-3 py-2 text-center text-xs font-medium text-gray-500 uppercase">Thao tác</th>
                        </tr>
                      </thead>
                      <tbody className="bg-white divide-y divide-gray-200">
                        {lichTraNo.map((item) => {
                          const statusCfg = getPaymentStatusConfig(item.trangThai);
                          return (
                            <tr key={item.maLichTraNo} className="hover:bg-gray-50">
                              <td className="px-3 py-2 text-gray-500 text-xs">#{item.maLichTraNo}</td>
                              <td className="px-3 py-2 font-medium">{item.kyThanhToan}</td>
                              <td className="px-3 py-2">{formatDate(item.ngayDenHan)}</td>
                              <td className="px-3 py-2 text-right">{formatCurrency(item.gocPhaiTra)}</td>
                              <td className="px-3 py-2 text-right text-orange-600">{formatCurrency(item.laiPhaiTra)}</td>
                              <td className="px-3 py-2 text-right font-semibold">
                                {formatCurrency(item.gocPhaiTra + item.laiPhaiTra + (item.phiPhat || 0))}
                              </td>
                              <td className="px-3 py-2 text-center">
                                <StatusBadge
                                  status={item.trangThai}
                                  label={statusCfg.label}
                                  color={statusCfg.color}
                                />
                              </td>
                              <td className="px-3 py-2 text-center">
                                {item.trangThai !== 'DA_THANH_TOAN' && (
                                  <button
                                    type="button"
                                    onClick={() => handleOpenPayment(item)}
                                    className="inline-flex items-center gap-1 px-2 py-1 text-xs text-white bg-blue-600 hover:bg-blue-700 rounded transition-colors"
                                    title="Tạo thanh toán cho kỳ này"
                                  >
                                    <CreditCard size={12} />
                                    Thanh toán
                                  </button>
                                )}
                              </td>
                            </tr>
                          );
                        })}
                      </tbody>
                    </table>
                  </div>
                </div>
              )}
            </div>

            {/* Tổng kết */}
            {lichTraNo.length > 0 && (
              <div className="bg-gray-50 p-4 rounded-lg grid grid-cols-3 gap-4 text-sm">
                <div>
                  <p className="text-gray-500 text-xs">Tổng gốc</p>
                  <p className="font-semibold text-gray-800">
                    {formatCurrency(lichTraNo.reduce((sum, item) => sum + item.gocPhaiTra, 0))}
                  </p>
                </div>
                <div>
                  <p className="text-gray-500 text-xs">Tổng lãi</p>
                  <p className="font-semibold text-orange-600">
                    {formatCurrency(lichTraNo.reduce((sum, item) => sum + item.laiPhaiTra, 0))}
                  </p>
                </div>
                <div>
                  <p className="text-gray-500 text-xs">Tổng phải trả</p>
                  <p className="font-semibold text-blue-700">
                    {formatCurrency(lichTraNo.reduce((sum, item) => sum + item.gocPhaiTra + item.laiPhaiTra, 0))}
                  </p>
                </div>
              </div>
            )}
          </div>
        )}
      </Modal>

      {/* Modal tạo thanh toán cho kỳ */}
      <Modal
        open={paymentModalOpen}
        onClose={() => setPaymentModalOpen(false)}
        title="Tạo thanh toán"
        size="md"
        footer={
          <>
            <Button variant="outline" onClick={() => setPaymentModalOpen(false)}>Hủy</Button>
            <Button loading={paymentSaving} onClick={handleSubmitPayment}>Lưu thanh toán</Button>
          </>
        }
      >
        {payingSchedule && selectedDetail && (
          <div className="space-y-4">
            {paymentError && (
              <div className="bg-red-50 border border-red-200 text-red-600 px-4 py-3 rounded-lg text-sm">
                {paymentError}
              </div>
            )}

            {/* Thông tin kỳ thanh toán */}
            <div className="bg-blue-50 border border-blue-100 p-4 rounded-lg space-y-1 text-sm">
              <div className="flex justify-between">
                <span className="text-gray-600">Hợp đồng:</span>
                <span className="font-medium text-blue-700">{selectedDetail.soHopDong}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-gray-600">Mã kỳ:</span>
                <span className="font-medium">#{payingSchedule.maLichTraNo}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-gray-600">Kỳ thanh toán:</span>
                <span className="font-medium">Kỳ {payingSchedule.kyThanhToan}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-gray-600">Ngày đến hạn:</span>
                <span className="font-medium">{formatDate(payingSchedule.ngayDenHan)}</span>
              </div>
              <div className="flex justify-between pt-2 border-t border-blue-200">
                <span className="text-gray-600">Tổng phải trả:</span>
                <span className="font-bold text-blue-700">
                  {formatCurrency(payingSchedule.gocPhaiTra + payingSchedule.laiPhaiTra + (payingSchedule.phiPhat || 0))}
                </span>
              </div>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <Input
                label="Số tiền áp gốc"
                type="number"
                value={paymentForm.soTienApGoc}
                onChange={(e) => setPaymentForm({ ...paymentForm, soTienApGoc: Number(e.target.value) })}
              />
              <Input
                label="Số tiền áp lãi"
                type="number"
                value={paymentForm.soTienApLai}
                onChange={(e) => setPaymentForm({ ...paymentForm, soTienApLai: Number(e.target.value) })}
              />
              <Input
                label="Số tiền phạt (nếu có)"
                type="number"
                value={paymentForm.soTienApPhi}
                onChange={(e) => setPaymentForm({ ...paymentForm, soTienApPhi: Number(e.target.value) })}
              />
              <Input
                label="Ngày thanh toán"
                type="date"
                value={paymentForm.ngayThanhToan}
                onChange={(e) => setPaymentForm({ ...paymentForm, ngayThanhToan: e.target.value })}
              />
              <Select
                label="Phương thức"
                value={paymentForm.phuongThucThanhToan}
                onChange={(value) => setPaymentForm({ ...paymentForm, phuongThucThanhToan: String(value) })}
                options={[
                  { value: 'TIEN_MAT', label: 'Tiền mặt' },
                  { value: 'CHUYEN_KHOAN', label: 'Chuyển khoản' },
                  { value: 'THE', label: 'Thẻ' },
                ]}
                className="col-span-2"
              />
              <Input
                label="Ghi chú"
                value={paymentForm.ghiChu}
                onChange={(e) => setPaymentForm({ ...paymentForm, ghiChu: e.target.value })}
                className="col-span-2"
              />
            </div>

            <div className="bg-gray-50 p-3 rounded-lg flex justify-between text-sm">
              <span className="text-gray-600">Tổng tiền thanh toán:</span>
              <span className="font-bold text-green-700">
                {formatCurrency(
                  paymentForm.soTienApGoc + paymentForm.soTienApLai + paymentForm.soTienApPhi
                )}
              </span>
            </div>
          </div>
        )}
      </Modal>
    </div>
  );
};

export default ContractList;
