import { useState, useEffect } from 'react';
import { Check, Eye } from 'lucide-react';
import { hoSoVayApi, HoSoVay } from '../../../api/hosovay.api';
import { thamDinhApi } from '../../../api/thamdinh.api';
import { pheDuyetApi } from '../../../api/pheduyet.api';
import Table from '../../../components/common/Table';
import Button from '../../../components/common/Button';
import Input from '../../../components/common/Input';
import Select from '../../../components/common/Select';
import Modal from '../../../components/common/Modal';
import StatusBadge from '../../../components/common/StatusBadge';
import Loading from '../../../components/common/Loading';
import { formatCurrency, formatDate } from '../../../utils/formatters';
import { TRANG_THAI_HO_SO } from '../../../utils/constants';
import { PERM } from '../../../utils/permissions';
import { usePermission } from '../../../hooks/usePermission';

const ApprovalList = () => {
  const { can } = usePermission();
  const [loans, setLoans] = useState<HoSoVay[]>([]);
  const [loading, setLoading] = useState(true);
  const [modalOpen, setModalOpen] = useState(false);
  const [detailModalOpen, setDetailModalOpen] = useState(false);
  const [selectedLoan, setSelectedLoan] = useState<HoSoVay | null>(null);
  const [appraisal, setAppraisal] = useState<any>(null);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [formData, setFormData] = useState({
    quyetDinh: 'DA_PHE_DUYET',
    soTienDuocDuyet: 0,
    laiSuatDuocDuyet: 0,
    dieuKienPheDuyet: '',
    lyDoTuChoi: '',
  });

  useEffect(() => {
    loadPendingLoans();
  }, []);

  const loadPendingLoans = async () => {
    setLoading(true);
    try {
      const response = await hoSoVayApi.getAll({ pageSize: 100, trangThai: 'CHO_PHE_DUYET' as any });
      setLoans(response.data.items);
    } catch (err) {
      console.error('Failed to load:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleOpenModal = async (loan: HoSoVay) => {
    setSelectedLoan(loan);
    // Lãi suất hiển thị dạng phần trăm (12 = 12%), sẽ convert sang decimal (0.12) khi gửi API
    setFormData({
      quyetDinh: 'DA_PHE_DUYET',
      soTienDuocDuyet: loan.soTienDeNghi,
      laiSuatDuocDuyet: 12,
      dieuKienPheDuyet: '',
      lyDoTuChoi: '',
    });
    setError('');
    setModalOpen(true);
    setAppraisal(null);

    // Load appraisal data
    try {
      const res = await thamDinhApi.getByHoSo(loan.maHoSo);
      setAppraisal(res.data);
    } catch (err) {
      console.error('Failed to load appraisal:', err);
    }
  };

  /** Mở modal xem chi tiết hồ sơ (nút Eye) — không mở form phê duyệt */
  const handleViewDetail = async (loan: HoSoVay) => {
    setSelectedLoan(loan);
    setDetailModalOpen(true);
    setAppraisal(null);
    try {
      const res = await thamDinhApi.getByHoSo(loan.maHoSo);
      setAppraisal(res.data);
    } catch (err) {
      console.error('Failed to load appraisal:', err);
    }
  };

  const handleApprove = async () => {
    if (!selectedLoan) return;
    setSaving(true);
    setError('');
    try {
      // Convert lãi suất từ % (12) sang decimal (0.12) vì backend lưu dạng decimal
      const laiSuatDecimal = formData.laiSuatDuocDuyet / 100;

      // Tạo bản ghi phê duyệt. Backend đã đồng bộ trạng thái hồ sơ trong cùng
      // transaction (DA_PHE_DUYET / TU_CHOI khi đang CHỜ PHÊ DUYET;
      // YEU_CAU_BO_SUNG giữ nguyên trạng thái để hồ sơ tiếp tục bổ sung).
      // → Không gọi thêm updateStatus ở đây (trước đây bước này làm
      // YEU_CAU_BO_SUNG bị ghi đè thành TU_CHOI).
      await pheDuyetApi.create({
        maHoSo: selectedLoan.maHoSo,
        quyetDinh: formData.quyetDinh,
        soTienDuocDuyet: formData.soTienDuocDuyet,
        laiSuatDuocDuyet: laiSuatDecimal,
        dieuKienPheDuyet: formData.dieuKienPheDuyet || undefined,
        lyDoTuChoi: formData.lyDoTuChoi || undefined,
      });

      setModalOpen(false);
      loadPendingLoans();
    } catch (err: any) {
      setError(err.response?.data?.message || err.message || 'Phê duyệt thất bại');
    } finally {
      setSaving(false);
    }
  };

  const columns = [
    {
      key: 'maHoSoHienThi',
      title: 'Mã hồ sơ',
      render: (r: HoSoVay) => <span className="font-medium text-blue-600">{r.maHoSoHienThi}</span>,
    },
    { key: 'tenKhachHang', title: 'Khách hàng' },
    {
      key: 'soTienDeNghi',
      title: 'Số tiền',
      align: 'right' as const,
      render: (r: HoSoVay) => formatCurrency(r.soTienDeNghi),
    },
    { key: 'kyHanThang', title: 'Kỳ hạn', render: (r: HoSoVay) => `${r.kyHanThang} tháng` },
    {
      key: 'trangThai',
      title: 'Trạng thái',
      render: (r: HoSoVay) => {
        const config = TRANG_THAI_HO_SO[r.trangThai as keyof typeof TRANG_THAI_HO_SO];
        return <StatusBadge status={r.trangThai} label={config?.label} color={config?.color as any} />;
      },
    },
    { key: 'ngayTao', title: 'Ngày tạo', render: (r: HoSoVay) => formatDate(r.ngayTao) },
    {
      key: 'actions',
      title: '',
      width: '100px',
      align: 'center' as const,
      render: (r: HoSoVay) => (
        <div className="flex gap-2 justify-center">
          {can(PERM.APPROVE_LOAN) && (
            <button onClick={() => handleOpenModal(r)} className="p-1.5 bg-green-100 hover:bg-green-200 rounded" title="Phê duyệt">
              <Check size={16} className="text-green-600" />
            </button>
          )}
          <button onClick={() => handleViewDetail(r)} className="p-1.5 hover:bg-gray-100 rounded" title="Xem chi tiết">
            <Eye size={16} className="text-gray-600" />
          </button>
        </div>
      ),
    },
  ];

  if (loading) return <Loading text="Đang tải..." />;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-800">Phê duyệt hồ sơ</h1>
        <span className="text-sm text-gray-500">{loans.length} hồ sơ chờ phê duyệt</span>
      </div>

      <div className="bg-white rounded-xl shadow-sm border border-gray-100">
        <Table columns={columns} data={loans} emptyText="Không có hồ sơ chờ phê duyệt" />
      </div>

      <Modal
        open={modalOpen}
        onClose={() => setModalOpen(false)}
        title="Phê duyệt hồ sơ"
        size="lg"
        footer={
          <>
            <Button variant="outline" onClick={() => setModalOpen(false)}>Hủy</Button>
            <Button variant="danger" onClick={() => setFormData({ ...formData, quyetDinh: 'TU_CHOI' })}>Từ chối</Button>
            <Button onClick={handleApprove} loading={saving}>Phê duyệt</Button>
          </>
        }
      >
        {error && <div className="mb-4 bg-red-50 border border-red-200 text-red-600 px-4 py-3 rounded-lg text-sm">{error}</div>}

        {selectedLoan && (
          <div className="space-y-4">
            <div className="bg-gray-50 p-4 rounded-lg">
              <h3 className="font-semibold mb-2">Thông tin hồ sơ</h3>
              <div className="grid grid-cols-2 gap-2 text-sm">
                <p><strong>Mã hồ sơ:</strong> {selectedLoan.maHoSoHienThi}</p>
                <p><strong>Khách hàng:</strong> {selectedLoan.tenKhachHang}</p>
                <p><strong>Số tiền đề nghị:</strong> {formatCurrency(selectedLoan.soTienDeNghi)}</p>
                <p><strong>Kỳ hạn:</strong> {selectedLoan.kyHanThang} tháng</p>
              </div>
            </div>

            {appraisal && (
              <div className="bg-yellow-50 p-4 rounded-lg">
                <h3 className="font-semibold mb-2">Kết quả thẩm định</h3>
                <div className="grid grid-cols-2 gap-2 text-sm">
                  <p><strong>Điểm rủi ro:</strong> {appraisal.diemRuiRo}</p>
                  <p><strong>Khuyến nghị:</strong> {appraisal.khuyenNgh}</p>
                  <p><strong>Thu nhập xác minh:</strong> {formatCurrency(appraisal.thuNhapXacMinh || 0)}</p>
                  <p><strong>Chi phí hàng tháng:</strong> {formatCurrency(appraisal.chiPhiHangThang || 0)}</p>
                </div>
              </div>
            )}

            <div className="grid grid-cols-2 gap-4">
              <Select
                label="Quyết định"
                value={formData.quyetDinh}
                onChange={(value) => setFormData({ ...formData, quyetDinh: String(value) })}
                options={[
                  { value: 'DA_PHE_DUYET', label: 'Đồng ý duyệt' },
                  { value: 'TU_CHOI', label: 'Từ chối' },
                  { value: 'YEU_CAU_BO_SUNG', label: 'Yêu cầu bổ sung' },
                ]}
              />
              <Input
                label="Số tiền được duyệt"
                type="number"
                value={formData.soTienDuocDuyet}
                onChange={(e) => setFormData({ ...formData, soTienDuocDuyet: Number(e.target.value) })}
              />
              <Input
                label="Lãi suất được duyệt (%/năm, VD: 12 = 12%)"
                type="number"
                step="0.01"
                min="0.01"
                max="30"
                value={formData.laiSuatDuocDuyet}
                onChange={(e) => setFormData({ ...formData, laiSuatDuocDuyet: Number(e.target.value) })}
                helperText="Nhập dạng phần trăm, từ 0.01% đến 30%"
              />
              <Input
                label="Điều kiện phê duyệt"
                value={formData.dieuKienPheDuyet}
                onChange={(e) => setFormData({ ...formData, dieuKienPheDuyet: e.target.value })}
              />
              <Input
                label="Lý do từ chối"
                value={formData.lyDoTuChoi}
                onChange={(e) => setFormData({ ...formData, lyDoTuChoi: e.target.value })}
                className="col-span-2"
              />
            </div>
          </div>
        )}
      </Modal>

      {/* Modal xem chi tiết hồ sơ */}
      <Modal
        open={detailModalOpen}
        onClose={() => setDetailModalOpen(false)}
        title="Chi tiết hồ sơ vay"
        size="lg"
        footer={
          <Button variant="outline" onClick={() => setDetailModalOpen(false)}>Đóng</Button>
        }
      >
        {selectedLoan && (
          <div className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div>
                <p className="text-sm text-gray-500">Mã hồ sơ</p>
                <p className="font-medium">{selectedLoan.maHoSoHienThi}</p>
              </div>
              <div>
                <p className="text-sm text-gray-500">Khách hàng</p>
                <p className="font-medium">{selectedLoan.tenKhachHang}</p>
              </div>
              <div>
                <p className="text-sm text-gray-500">Sản phẩm vay</p>
                <p className="font-medium">{selectedLoan.tenSanPham}</p>
              </div>
              <div>
                <p className="text-sm text-gray-500">Số tiền đề nghị</p>
                <p className="font-medium">{formatCurrency(selectedLoan.soTienDeNghi)}</p>
              </div>
              <div>
                <p className="text-sm text-gray-500">Kỳ hạn</p>
                <p className="font-medium">{selectedLoan.kyHanThang} tháng</p>
              </div>
              <div>
                <p className="text-sm text-gray-500">Cán bộ xử lý</p>
                <p className="font-medium">{selectedLoan.tenCanBo}</p>
              </div>
              <div className="col-span-2">
                <p className="text-sm text-gray-500">Mục đích vay</p>
                <p className="font-medium">{selectedLoan.mucDich}</p>
              </div>
            </div>

            {appraisal && (
              <div className="border-t pt-4">
                <h4 className="font-semibold text-gray-800 mb-2">Kết quả thẩm định</h4>
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <p className="text-sm text-gray-500">Tỷ lệ nợ/thu nhập</p>
                    <p className="font-medium">
                      {appraisal.tyLeNoThuNhap != null
                        ? `${(appraisal.tyLeNoThuNhap * 100).toFixed(2)}%`
                        : '—'}
                    </p>
                  </div>
                  <div>
                    <p className="text-sm text-gray-500">Điểm rủi ro</p>
                    <p className="font-medium">{appraisal.diemRuiRo}</p>
                  </div>
                  <div>
                    <p className="text-sm text-gray-500">Khuyến nghị</p>
                    <p className="font-medium">{appraisal.khuyenNgh || '—'}</p>
                  </div>
                  <div>
                    <p className="text-sm text-gray-500">Ngày thẩm định</p>
                    <p className="font-medium">{formatDate(appraisal.ngayThamDinh)}</p>
                  </div>
                  {appraisal.ghiChu && (
                    <div className="col-span-2">
                      <p className="text-sm text-gray-500">Ghi chú</p>
                      <p className="font-medium">{appraisal.ghiChu}</p>
                    </div>
                  )}
                </div>
              </div>
            )}
          </div>
        )}
      </Modal>
    </div>
  );
};

export default ApprovalList;
