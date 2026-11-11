import { useState, useEffect } from 'react';
import { useSearchParams } from 'react-router-dom';
import { Plus, Search, Eye, Edit2, FileText, User, Package, DollarSign, Calendar, Hash, Briefcase, ClipboardCheck } from 'lucide-react';
import { hoSoVayApi, HoSoVay, CreateHoSoVayDTO } from '../../../api/hosovay.api';
import { sanPhamVayApi } from '../../../api/sanpham.api';
import { khachHangApi } from '../../../api/khachhang.api';
import { exportApi } from '../../../api/export.api';
import Table, { Pagination } from '../../../components/common/Table';
import Button from '../../../components/common/Button';
import Input from '../../../components/common/Input';
import Select from '../../../components/common/Select';
import Modal from '../../../components/common/Modal';
import StatusBadge from '../../../components/common/StatusBadge';
import ExportButton from '../../../components/common/ExportButton';
import { formatCurrency, formatDate, formatDateTime } from '../../../utils/formatters';
import { TRANG_THAI_HO_SO } from '../../../utils/constants';
import { PERM } from '../../../utils/permissions';
import { usePermission } from '../../../hooks/usePermission';

const LoanList = () => {
  const { can } = usePermission();
  const [searchParams, setSearchParams] = useSearchParams();
  const [loans, setLoans] = useState<HoSoVay[]>([]);
  const [loading, setLoading] = useState(true);
  const [pagination, setPagination] = useState({
    pageNumber: 1,
    pageSize: 10,
    totalCount: 0,
    totalPages: 0,
  });
  const [search, setSearch] = useState('');
  const [filterStatus, setFilterStatus] = useState('');
  const [modalOpen, setModalOpen] = useState(false);
  const [statusModalOpen, setStatusModalOpen] = useState(false);
  const [selectedLoan, setSelectedLoan] = useState<HoSoVay | null>(null);
  const [newStatus, setNewStatus] = useState('');

  // Chi tiết hồ sơ
  const [detailOpen, setDetailOpen] = useState(false);
  const [detailData, setDetailData] = useState<HoSoVay | null>(null);
  const [detailLoading, setDetailLoading] = useState(false);
  const [products, setProducts] = useState<{ maSanPham: number; tenSanPham: string }[]>([]);
  const [customers, setCustomers] = useState<{ maKhachHang: number; hoTen: string }[]>([]);
  const [formData, setFormData] = useState<CreateHoSoVayDTO>({
    maKhachHang: 0,
    maSanPham: 0,
    soTienDeNghi: 0,
    kyHanThang: 12,
    mucDich: '',
  });
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    loadLoans();
    loadProducts();
    loadCustomers();
  }, [pagination.pageNumber, pagination.pageSize, search, filterStatus]);

  // Deep-link: /loans?maHoSo=123 → mở modal chi tiết hồ sơ đó
  // (dùng bởi NotificationBell khi bấm vào thông báo có liên kết hồ sơ).
  useEffect(() => {
    const maHoSo = searchParams.get('maHoSo');
    if (!maHoSo) return;
    const id = Number(maHoSo);
    if (!id) return;

    hoSoVayApi.getById(id)
      .then((res) => {
        if (res.data) {
          setSelectedLoan(res.data);
          setDetailOpen(true);
        }
      })
      .catch((err) => console.error('Không mở được hồ sơ từ deep-link:', err));

    searchParams.delete('maHoSo');
    setSearchParams(searchParams, { replace: true });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const loadLoans = async () => {
    setLoading(true);
    try {
      const response = await hoSoVayApi.getAll({
        pageNumber: pagination.pageNumber,
        pageSize: pagination.pageSize,
        search: search || undefined,
        trangThai: (filterStatus as HoSoVay['trangThai']) || undefined,
      });
      setLoans(response.data.items);
      setPagination((prev) => ({
        ...prev,
        totalCount: response.data.totalCount,
        totalPages: Math.ceil(response.data.totalCount / prev.pageSize),
      }));
    } catch (error) {
      console.error('Failed to load loans:', error);
    } finally {
      setLoading(false);
    }
  };

  const loadProducts = async () => {
    try {
      const response = await sanPhamVayApi.getAll({ pageSize: 100 });
      setProducts(response.data.items.map((p) => ({ maSanPham: p.maSanPham, tenSanPham: p.tenSanPham })));
    } catch (error) {
      console.error('Failed to load products:', error);
    }
  };

  const loadCustomers = async () => {
    try {
      const response = await khachHangApi.getAll({ pageSize: 100 });
      setCustomers(response.data.items.map((c) => ({ maKhachHang: c.maKhachHang, hoTen: c.hoTen })));
    } catch (error) {
      console.error('Failed to load customers:', error);
    }
  };

  const handleSave = async () => {
    setSaving(true);
    setError('');
    try {
      await hoSoVayApi.create(formData);
      setModalOpen(false);
      loadLoans();
      setFormData({
        maKhachHang: 0,
        maSanPham: 0,
        soTienDeNghi: 0,
        kyHanThang: 12,
        mucDich: '',
      });
    } catch (err: unknown) {
      const error = err as { response?: { data?: { message?: string } } };
      setError(error.response?.data?.message || 'Lưu thất bại');
    } finally {
      setSaving(false);
    }
  };

  const handleOpenStatusModal = (loan: HoSoVay) => {
    setSelectedLoan(loan);
    setNewStatus(loan.trangThai);
    setStatusModalOpen(true);
  };

  const handleUpdateStatus = async () => {
    if (!selectedLoan) return;
    setSaving(true);
    try {
      await hoSoVayApi.updateStatus(selectedLoan.maHoSo, newStatus as HoSoVay['trangThai']);
      setStatusModalOpen(false);
      loadLoans();
    } catch (err: unknown) {
      const error = err as { response?: { data?: { message?: string } } };
      setError(error.response?.data?.message || 'Cập nhật thất bại');
    } finally {
      setSaving(false);
    }
  };

  const handleViewDetail = async (loan: HoSoVay) => {
    setDetailOpen(true);
    setDetailLoading(true);
    setDetailData(null);
    try {
      const res = await hoSoVayApi.getById(loan.maHoSo);
      setDetailData(res.data);
    } catch (err) {
      console.error('Failed to load detail:', err);
    } finally {
      setDetailLoading(false);
    }
  };

  const columns = [
    {
      key: 'maHoSoHienThi',
      title: 'Mã hồ sơ',
      render: (r: HoSoVay) => (
        <span className="font-medium text-blue-600">{r.maHoSoHienThi}</span>
      ),
    },
    {
      key: 'tenKhachHang',
      title: 'Khách hàng',
      render: (r: HoSoVay) => (
        <div>
          <p className="font-medium">{r.tenKhachHang}</p>
          <p className="text-xs text-gray-500">{r.tenSanPham}</p>
        </div>
      ),
    },
    {
      key: 'soTienDeNghi',
      title: 'Số tiền',
      align: 'right' as const,
      render: (r: HoSoVay) => formatCurrency(r.soTienDeNghi),
    },
    { key: 'kyHanThang', title: 'Kỳ hạn', align: 'center' as const, render: (r: HoSoVay) => `${r.kyHanThang} tháng` },
    {
      key: 'trangThai',
      title: 'Trạng thái',
      render: (r: HoSoVay) => {
        const config = TRANG_THAI_HO_SO[r.trangThai as keyof typeof TRANG_THAI_HO_SO];
        return (
          <StatusBadge
            status={r.trangThai}
            label={config?.label}
            color={config?.color as 'blue' | 'green' | 'yellow' | 'red' | 'purple' | 'cyan' | 'teal' | 'orange' | 'emerald' | 'gray'}
          />
        );
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
          {can(PERM.UPDATE_LOAN_STATUS) && (
            <button onClick={() => handleOpenStatusModal(r)} className="p-1.5 hover:bg-gray-100 rounded" title="Đổi trạng thái">
              <Edit2 size={16} className="text-blue-600" />
            </button>
          )}
          <button
            type="button"
            onClick={() => handleViewDetail(r)}
            className="p-1.5 hover:bg-gray-100 rounded transition-colors"
            title="Xem chi tiết"
            aria-label="Xem chi tiết hồ sơ"
          >
            <Eye size={16} className="text-blue-600" />
          </button>
        </div>
      ),
    },
  ];

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-800">Hồ sơ vay</h1>
        <div className="flex items-center gap-2">
          <ExportButton
            label="HS"
            onExport={(format) => exportApi.exportHoSoVay({
              format,
              search: search || undefined,
              trangThai: (filterStatus as HoSoVay['trangThai']) || undefined,
            })}
          />
          {can(PERM.CREATE_LOAN) && (
            <Button leftIcon={<Plus size={18} />} onClick={() => setModalOpen(true)}>
              Tạo hồ sơ mới
            </Button>
          )}
        </div>
      </div>

      {/* Filters */}
      <div className="bg-white rounded-xl shadow-sm p-4 border border-gray-100 flex flex-wrap gap-4">
        <div className="flex-1 min-w-[200px]">
          <Input placeholder="Tìm kiếm..." value={search} onChange={(e) => setSearch(e.target.value)} leftIcon={<Search size={18} />} />
        </div>
        <div className="w-48">
          <Select
            placeholder="Trạng thái"
            value={filterStatus}
            onChange={(value) => setFilterStatus(String(value))}
            options={[
              { value: '', label: 'Tất cả' },
              ...Object.entries(TRANG_THAI_HO_SO).map(([key, val]) => ({ value: key, label: val.label })),
            ]}
          />
        </div>
      </div>

      {/* Table */}
      <div className="bg-white rounded-xl shadow-sm border border-gray-100">
        <Table columns={columns} data={loans} loading={loading} emptyText="Không có hồ sơ vay" />
        <Pagination
          currentPage={pagination.pageNumber}
          totalPages={pagination.totalPages}
          totalItems={pagination.totalCount}
          pageSize={pagination.pageSize}
          onPageChange={(page) => setPagination((prev) => ({ ...prev, pageNumber: page }))}
          onPageSizeChange={(size) => setPagination((prev) => ({ ...prev, pageSize: size, pageNumber: 1 }))}
        />
      </div>

      {/* Modal Tạo hồ sơ */}
      <Modal open={modalOpen} onClose={() => setModalOpen(false)} title="Tạo hồ sơ vay mới" size="lg"
        footer={<><Button variant="outline" onClick={() => setModalOpen(false)}>Hủy</Button><Button loading={saving} onClick={handleSave}>Lưu</Button></>}>
        {error && <div className="mb-4 bg-red-50 border border-red-200 text-red-600 px-4 py-3 rounded-lg text-sm">{error}</div>}
        <div className="grid grid-cols-2 gap-4">
          <Select
            label="Khách hàng"
            value={formData.maKhachHang}
            onChange={(value) => setFormData({ ...formData, maKhachHang: Number(value) })}
            options={customers.map((c) => ({ value: c.maKhachHang, label: c.hoTen }))}
          />
          <Select
            label="Sản phẩm vay"
            value={formData.maSanPham}
            onChange={(value) => setFormData({ ...formData, maSanPham: Number(value) })}
            options={products.map((p) => ({ value: p.maSanPham, label: p.tenSanPham }))}
          />
          <Input label="Số tiền đề nghị" type="number" value={formData.soTienDeNghi} onChange={(e) => setFormData({ ...formData, soTienDeNghi: Number(e.target.value) })} />
          <Input label="Kỳ hạn (tháng)" type="number" value={formData.kyHanThang} onChange={(e) => setFormData({ ...formData, kyHanThang: Number(e.target.value) })} />
          <Input label="Mục đích vay" value={formData.mucDich} onChange={(e) => setFormData({ ...formData, mucDich: e.target.value })} className="col-span-2" />
        </div>
      </Modal>

      {/* Modal Đổi trạng thái */}
      <Modal open={statusModalOpen} onClose={() => setStatusModalOpen(false)} title="Đổi trạng thái hồ sơ" size="md"
        footer={<><Button variant="outline" type="button" onClick={() => setStatusModalOpen(false)}>Hủy</Button><Button type="button" loading={saving} onClick={handleUpdateStatus}>Cập nhật</Button></>}>
        {selectedLoan && (
          <div className="space-y-4">
            <div className="bg-gray-50 p-3 rounded-lg">
              <p><strong>Mã hồ sơ:</strong> {selectedLoan.maHoSoHienThi}</p>
              <p><strong>Khách hàng:</strong> {selectedLoan.tenKhachHang}</p>
              <p><strong>Trạng thái hiện tại:</strong> {TRANG_THAI_HO_SO[selectedLoan.trangThai as keyof typeof TRANG_THAI_HO_SO]?.label}</p>
            </div>
            <Select
              label="Trạng thái mới"
              value={newStatus}
              onChange={(value) => setNewStatus(String(value))}
              options={Object.entries(TRANG_THAI_HO_SO).map(([key, val]) => ({ value: key, label: val.label }))}
            />
          </div>
        )}
      </Modal>

      {/* Modal Chi tiết hồ sơ vay */}
      <Modal
        open={detailOpen}
        onClose={() => setDetailOpen(false)}
        title="Chi tiết hồ sơ vay"
        size="xl"
      >
        {detailLoading ? (
          <div className="text-center py-8 text-gray-500">Đang tải chi tiết...</div>
        ) : detailData ? (
          <div className="space-y-5">
            {/* Header card với trạng thái */}
            <div className="bg-gradient-to-r from-blue-50 to-indigo-50 p-4 rounded-lg border border-blue-100">
              <div className="flex items-center justify-between mb-3">
                <div className="flex items-center gap-2">
                  <FileText className="w-5 h-5 text-blue-600" />
                  <h3 className="font-semibold text-gray-800">Thông tin hồ sơ</h3>
                </div>
                <StatusBadge
                  status={detailData.trangThai}
                  label={TRANG_THAI_HO_SO[detailData.trangThai as keyof typeof TRANG_THAI_HO_SO]?.label || detailData.trangThai}
                  color={(TRANG_THAI_HO_SO[detailData.trangThai as keyof typeof TRANG_THAI_HO_SO]?.color as any) || 'gray'}
                />
              </div>

              <div className="grid grid-cols-2 gap-3 text-sm">
                <div className="flex items-start gap-2">
                  <Hash className="w-4 h-4 text-gray-400 mt-0.5" />
                  <div>
                    <p className="text-gray-500">Mã hồ sơ</p>
                    <p className="font-medium text-blue-700">{detailData.maHoSoHienThi}</p>
                  </div>
                </div>
                <div className="flex items-start gap-2">
                  <User className="w-4 h-4 text-gray-400 mt-0.5" />
                  <div>
                    <p className="text-gray-500">Cán bộ tạo</p>
                    <p className="font-medium">{detailData.tenCanBo}</p>
                  </div>
                </div>
                <div className="flex items-start gap-2">
                  <Calendar className="w-4 h-4 text-gray-400 mt-0.5" />
                  <div>
                    <p className="text-gray-500">Ngày tạo</p>
                    <p className="font-medium">{formatDateTime(detailData.ngayTao)}</p>
                  </div>
                </div>
                <div className="flex items-start gap-2">
                  <Calendar className="w-4 h-4 text-gray-400 mt-0.5" />
                  <div>
                    <p className="text-gray-500">Ngày nộp</p>
                    <p className="font-medium">
                      {detailData.ngayNop ? formatDate(detailData.ngayNop) : <span className="text-gray-400">Chưa nộp</span>}
                    </p>
                  </div>
                </div>
              </div>
            </div>

            {/* Thông tin khách hàng */}
            <div className="border border-gray-200 rounded-lg p-4">
              <div className="flex items-center gap-2 mb-3">
                <User className="w-5 h-5 text-indigo-600" />
                <h3 className="font-semibold text-gray-800">Thông tin khách hàng</h3>
              </div>
              <div className="grid grid-cols-2 gap-3 text-sm">
                <div>
                  <p className="text-gray-500">Mã khách hàng</p>
                  <p className="font-medium">#{detailData.maKhachHang}</p>
                </div>
                <div>
                  <p className="text-gray-500">Họ tên</p>
                  <p className="font-medium text-gray-800">{detailData.tenKhachHang}</p>
                </div>
              </div>
            </div>

            {/* Thông tin sản phẩm */}
            <div className="border border-gray-200 rounded-lg p-4">
              <div className="flex items-center gap-2 mb-3">
                <Package className="w-5 h-5 text-purple-600" />
                <h3 className="font-semibold text-gray-800">Sản phẩm vay</h3>
              </div>
              <div className="grid grid-cols-2 gap-3 text-sm">
                <div>
                  <p className="text-gray-500">Mã sản phẩm</p>
                  <p className="font-medium">#{detailData.maSanPham}</p>
                </div>
                <div>
                  <p className="text-gray-500">Tên sản phẩm</p>
                  <p className="font-medium text-gray-800">{detailData.tenSanPham}</p>
                </div>
              </div>
            </div>

            {/* Thông tin khoản vay */}
            <div className="border border-gray-200 rounded-lg p-4">
              <div className="flex items-center gap-2 mb-3">
                <DollarSign className="w-5 h-5 text-green-600" />
                <h3 className="font-semibold text-gray-800">Thông tin khoản vay</h3>
              </div>
              <div className="grid grid-cols-2 gap-3 text-sm">
                <div>
                  <p className="text-gray-500">Số tiền đề nghị</p>
                  <p className="font-semibold text-green-700 text-lg">{formatCurrency(detailData.soTienDeNghi)}</p>
                </div>
                <div>
                  <p className="text-gray-500">Kỳ hạn</p>
                  <p className="font-medium text-gray-800">{detailData.kyHanThang} tháng</p>
                </div>
                <div className="col-span-2">
                  <p className="text-gray-500 flex items-center gap-1">
                    <Briefcase className="w-3.5 h-3.5" /> Mục đích vay
                  </p>
                  <p className="font-medium text-gray-800 mt-1 p-2 bg-gray-50 rounded">
                    {detailData.mucDich || <span className="text-gray-400 italic">Không có</span>}
                  </p>
                </div>
              </div>
            </div>

            {/* Workflow tiến trình */}
            <div className="border border-gray-200 rounded-lg p-4">
              <div className="flex items-center gap-2 mb-3">
                <ClipboardCheck className="w-5 h-5 text-orange-600" />
                <h3 className="font-semibold text-gray-800">Tiến trình xử lý</h3>
              </div>
              <div className="flex items-center justify-between gap-1">
                {['MOI', 'DANG_THAM_DINH', 'CHO_PHE_DUYET', 'DA_PHE_DUYET', 'DA_LAP_HOP_DONG', 'DA_GIAI_NGAN'].map((status, idx, arr) => {
                  const currentIdx = arr.indexOf(detailData.trangThai);
                  const isReached = currentIdx >= idx;
                  const isCurrent = detailData.trangThai === status;
                  return (
                    <div key={status} className="flex-1 flex flex-col items-center text-center">
                      <div
                        className={`w-8 h-8 rounded-full flex items-center justify-center text-xs font-semibold ${
                          isCurrent
                            ? 'bg-blue-600 text-white ring-4 ring-blue-100'
                            : isReached
                            ? 'bg-green-500 text-white'
                            : 'bg-gray-200 text-gray-400'
                        }`}
                      >
                        {idx + 1}
                      </div>
                      <p className={`text-xs mt-1 ${isCurrent ? 'font-semibold text-blue-700' : isReached ? 'text-green-700' : 'text-gray-400'}`}>
                        {TRANG_THAI_HO_SO[status as keyof typeof TRANG_THAI_HO_SO]?.label || status}
                      </p>
                    </div>
                  );
                })}
              </div>
            </div>
          </div>
        ) : (
          <div className="text-center py-8 text-gray-500">Không có dữ liệu</div>
        )}
      </Modal>
    </div>
  );
};

export default LoanList;
