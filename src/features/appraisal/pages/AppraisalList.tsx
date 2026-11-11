import { useState, useEffect, useMemo } from 'react';
import { thamDinhApi, ThamDinhTinDung, CreateThamDinhDTO } from '../../../api/thamdinh.api';
import { hoSoVayApi, HoSoVay } from '../../../api/hosovay.api';
import Table from '../../../components/common/Table';
import Button from '../../../components/common/Button';
import Input from '../../../components/common/Input';
import Select from '../../../components/common/Select';
import Modal from '../../../components/common/Modal';
import StatusBadge from '../../../components/common/StatusBadge';
import Loading from '../../../components/common/Loading';
import { formatCurrency, formatDate, formatPercent } from '../../../utils/formatters';

const AppraisalList = () => {
  const [appraisals, setAppraisals] = useState<(ThamDinhTinDung & { tenKhachHang?: string; maHoSoHienThi?: string })[]>([]);
  const [loading, setLoading] = useState(true);
  const [modalOpen, setModalOpen] = useState(false);
  const [pendingLoans, setPendingLoans] = useState<HoSoVay[]>([]);
  const [selectedLoan, setSelectedLoan] = useState<HoSoVay | null>(null);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [formData, setFormData] = useState<CreateThamDinhDTO>({
    maHoSo: 0,
    thuNhapXacMinh: 0,
    chiPhiHangThang: 0,
    diemThuNhap: 0,
    diemTaiSanBaoDam: 0,
    diemCic: 0,
    diemRuiRo: 50,
    khuyenNgh: 'DE_NGHI_DUYET',
    ghiChu: '',
  });

  // Tính tỷ lệ nợ/thu nhập realtime khi user nhập
  const tyLeNoThuNhap = useMemo(() => {
    if (!selectedLoan || !formData.thuNhapXacMinh || formData.thuNhapXacMinh <= 0) return null;
    const thuNhapNam = formData.thuNhapXacMinh * 12;
    return selectedLoan.soTienDeNghi / thuNhapNam;
  }, [selectedLoan, formData.thuNhapXacMinh]);

  // Tailwind chỉ generate class xuất hiện LITERAL trong source → không dùng
  // template interpolation (`text-${color}-600`) vì class động sẽ không được
  // build ra CSS (mất màu phân loại rủi ro). Trả về sẵn class cụ thể.
  const getTyLeRating = (tyLe: number) => {
    if (tyLe <= 0.3)
      return {
        label: 'Tốt',
        text: 'text-green-600',
        badge: 'text-xs px-1.5 py-0.5 rounded bg-green-50 text-green-700',
        panel: 'col-span-2 p-3 rounded-lg border bg-green-50 border-green-200',
        panelText: 'text-2xl font-bold text-green-700',
        panelBadge: 'px-3 py-2 rounded-md text-sm font-medium bg-green-100 text-green-800',
      };
    if (tyLe <= 0.5)
      return {
        label: 'Trung bình',
        text: 'text-yellow-600',
        badge: 'text-xs px-1.5 py-0.5 rounded bg-yellow-50 text-yellow-700',
        panel: 'col-span-2 p-3 rounded-lg border bg-yellow-50 border-yellow-200',
        panelText: 'text-2xl font-bold text-yellow-700',
        panelBadge: 'px-3 py-2 rounded-md text-sm font-medium bg-yellow-100 text-yellow-800',
      };
    if (tyLe <= 0.7)
      return {
        label: 'Cao',
        text: 'text-orange-600',
        badge: 'text-xs px-1.5 py-0.5 rounded bg-orange-50 text-orange-700',
        panel: 'col-span-2 p-3 rounded-lg border bg-orange-50 border-orange-200',
        panelText: 'text-2xl font-bold text-orange-700',
        panelBadge: 'px-3 py-2 rounded-md text-sm font-medium bg-orange-100 text-orange-800',
      };
    return {
      label: 'Rất cao',
      text: 'text-red-600',
      badge: 'text-xs px-1.5 py-0.5 rounded bg-red-50 text-red-700',
      panel: 'col-span-2 p-3 rounded-lg border bg-red-50 border-red-200',
      panelText: 'text-2xl font-bold text-red-700',
      panelBadge: 'px-3 py-2 rounded-md text-sm font-medium bg-red-100 text-red-800',
    };
  };

  useEffect(() => {
    loadAppraisals();
    loadPendingLoans();
  }, []);

  const loadAppraisals = async () => {
    setLoading(true);
    try {
      const response = await thamDinhApi.getAll({ pageSize: 100 });
      setAppraisals(response.data.items);
    } catch (error) {
      console.error('Failed to load:', error);
    } finally {
      setLoading(false);
    }
  };

  const loadPendingLoans = async () => {
    try {
      const response = await hoSoVayApi.getAll({ pageSize: 100 });
      // Filter: chỉ lấy hồ sơ ở trạng thái DANG_THAM_DINH hoặc CHO_PHE_DUYET
      setPendingLoans(response.data.items.filter((l) =>
        ['DANG_THAM_DINH', 'CHO_PHE_DUYET'].includes(l.trangThai)
      ));
    } catch (error) {
      console.error('Failed to load loans:', error);
    }
  };

  const handleSubmit = async () => {
    setSaving(true);
    setError('');
    try {
      await thamDinhApi.create(formData);
      // Cập nhật trạng thái hồ sơ
      if (selectedLoan) {
        await hoSoVayApi.updateStatus(selectedLoan.maHoSo, 'CHO_PHE_DUYET');
      }
      setModalOpen(false);
      loadAppraisals();
      loadPendingLoans();
    } catch (err: unknown) {
      const error = err as { response?: { data?: { message?: string } } };
      setError(error.response?.data?.message || 'Lưu thất bại');
    } finally {
      setSaving(false);
    }
  };

  const handleNewAppraisal = (loan: HoSoVay) => {
    setSelectedLoan(loan);
    setFormData({
      maHoSo: loan.maHoSo,
      thuNhapXacMinh: 0,
      chiPhiHangThang: 0,
      diemThuNhap: 0,
      diemTaiSanBaoDam: 0,
      diemCic: 0,
      diemRuiRo: 50,
      khuyenNgh: 'DE_NGHI_DUYET',
      ghiChu: '',
    });
    setModalOpen(true);
  };

  const columns = [
    { key: 'maThamDinh', title: 'Mã TD' },
    { key: 'maHoSo', title: 'Mã HS' },
    {
      key: 'tyLeNoThuNhap',
      title: 'Tỷ lệ nợ/TN',
      render: (r: ThamDinhTinDung) => {
        if (!r.tyLeNoThuNhap) return <span className="text-gray-400">-</span>;
        const rating = getTyLeRating(r.tyLeNoThuNhap);
        return (
          <div className="flex items-center gap-2">
            <span className={`font-mono font-medium ${rating.text}`}>
              {formatPercent(r.tyLeNoThuNhap)}
            </span>
            <span className={rating.badge}>
              {rating.label}
            </span>
          </div>
        );
      },
    },
    { key: 'diemRuiRo', title: 'Điểm rủi ro', render: (r: ThamDinhTinDung) => (
      <span className={`font-medium ${r.diemRuiRo <= 30 ? 'text-green-600' : r.diemRuiRo <= 70 ? 'text-yellow-600' : 'text-red-600'}`}>
        {r.diemRuiRo}
      </span>
    )},
    { key: 'khuyenNgh', title: 'Khuyến nghị', render: (r: ThamDinhTinDung) => (
      <StatusBadge status={r.khuyenNgh || ''} label={r.khuyenNgh} color={r.khuyenNgh === 'DE_NGHI_DUYET' ? 'green' : 'red'} />
    )},
    { key: 'ngayThamDinh', title: 'Ngày TD', render: (r: ThamDinhTinDung) => formatDate(r.ngayThamDinh) },
  ];

  if (loading) return <Loading text="Đang tải..." />;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-800">Thẩm định tín dụng</h1>
      </div>

      {/* Pending Loans */}
      {pendingLoans.length > 0 && (
        <div className="bg-yellow-50 border border-yellow-200 rounded-xl p-4">
          <h2 className="font-semibold text-yellow-800 mb-3">Hồ sơ chờ thẩm định ({pendingLoans.length})</h2>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3">
            {pendingLoans.map((loan) => (
              <div key={loan.maHoSo} className="bg-white rounded-lg p-3 border border-yellow-200 flex justify-between items-center">
                <div>
                  <p className="font-medium">{loan.tenKhachHang}</p>
                  <p className="text-sm text-gray-500">{loan.tenSanPham}</p>
                </div>
                <Button size="sm" onClick={() => handleNewAppraisal(loan)}>Thẩm định</Button>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Appraisals Table */}
      <div className="bg-white rounded-xl shadow-sm border border-gray-100">
        <Table columns={columns} data={appraisals} emptyText="Chưa có kết quả thẩm định" />
      </div>

      {/* Modal */}
      <Modal open={modalOpen} onClose={() => setModalOpen(false)} title="Thẩm định tín dụng" size="lg"
        footer={<><Button variant="outline" onClick={() => setModalOpen(false)}>Hủy</Button><Button loading={saving} onClick={handleSubmit}>Lưu</Button></>}>
        {error && <div className="mb-4 bg-red-50 border border-red-200 text-red-600 px-4 py-3 rounded-lg text-sm">{error}</div>}

        {selectedLoan && (
          <div className="mb-4 bg-blue-50 border border-blue-100 rounded-lg p-3 text-sm">
            <div className="grid grid-cols-3 gap-2">
              <div>
                <p className="text-gray-500 text-xs">Mã hồ sơ</p>
                <p className="font-medium text-blue-700">{selectedLoan.maHoSoHienThi}</p>
              </div>
              <div>
                <p className="text-gray-500 text-xs">Số tiền đề nghị</p>
                <p className="font-semibold text-green-700">{formatCurrency(selectedLoan.soTienDeNghi)}</p>
              </div>
              <div>
                <p className="text-gray-500 text-xs">Kỳ hạn</p>
                <p className="font-medium">{selectedLoan.kyHanThang} tháng</p>
              </div>
            </div>
          </div>
        )}

        <div className="grid grid-cols-2 gap-4">
          <Input label="Thu nhập xác minh (tháng)" type="number" value={formData.thuNhapXacMinh || 0} onChange={(e) => setFormData({ ...formData, thuNhapXacMinh: Number(e.target.value) })} />
          <Input label="Chi phí hàng tháng" type="number" value={formData.chiPhiHangThang || 0} onChange={(e) => setFormData({ ...formData, chiPhiHangThang: Number(e.target.value) })} />

          {tyLeNoThuNhap !== null && (() => {
            const rating = getTyLeRating(tyLeNoThuNhap);
            return (
              <div className={rating.panel}>
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-xs text-gray-600">Tỷ lệ nợ / thu nhập (tự động tính)</p>
                    <p className={rating.panelText}>
                      {formatPercent(tyLeNoThuNhap)}
                    </p>
                    <p className="text-xs text-gray-500 mt-0.5">
                      = {formatCurrency(selectedLoan?.soTienDeNghi || 0)} / ({formatCurrency(formData.thuNhapXacMinh || 0)} × 12 tháng)
                    </p>
                  </div>
                  <div className={rating.panelBadge}>
                    {rating.label}
                  </div>
                </div>
              </div>
            );
          })()}

          <Input label="Điểm thu nhập (0-100)" type="number" value={formData.diemThuNhap || 0} onChange={(e) => setFormData({ ...formData, diemThuNhap: Number(e.target.value) })} />
          <Input label="Điểm TSBĐ (0-100)" type="number" value={formData.diemTaiSanBaoDam || 0} onChange={(e) => setFormData({ ...formData, diemTaiSanBaoDam: Number(e.target.value) })} />
          <Input label="Điểm CIC" type="number" value={formData.diemCic || 0} onChange={(e) => setFormData({ ...formData, diemCic: Number(e.target.value) })} />
          <Input label="Điểm rủi ro (0-100)" type="number" value={formData.diemRuiRo} onChange={(e) => setFormData({ ...formData, diemRuiRo: Number(e.target.value) })} />
          <Select label="Khuyến nghị" value={formData.khuyenNgh || ''} onChange={(value) => setFormData({ ...formData, khuyenNgh: value as CreateThamDinhDTO['khuyenNgh'] })}
            options={[
              { value: 'DE_NGHI_DUYET', label: 'Đề nghị duyệt' },
              { value: 'DE_NGHI_TU_CHOI', label: 'Đề nghị từ chối' },
              { value: 'CAN_BO_SUNG', label: 'Cần bổ sung' },
            ]} className="col-span-2" />
          <Input label="Ghi chú" value={formData.ghiChu || ''} onChange={(e) => setFormData({ ...formData, ghiChu: e.target.value })} className="col-span-2" />
        </div>
      </Modal>
    </div>
  );
};

export default AppraisalList;
