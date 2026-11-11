import { useState, useEffect, useMemo } from 'react';
import { FileText, Plus, Edit2, Trash2, Filter, ChevronRight, ExternalLink } from 'lucide-react';
import { taiLieuVayApi, TaiLieuVay, CreateTaiLieuVayDTO, LOAI_TAI_LIEU, TRANG_THAI_XAC_MINH } from '../../../api/tailieuvay.api';
import { hoSoVayApi, HoSoVay } from '../../../api/hosovay.api';
import Table, { Pagination } from '../../../components/common/Table';
import Button from '../../../components/common/Button';
import Input from '../../../components/common/Input';
import Select from '../../../components/common/Select';
import Modal from '../../../components/common/Modal';
import StatusBadge from '../../../components/common/StatusBadge';
import Loading from '../../../components/common/Loading';
import { formatDateTime } from '../../../utils/formatters';
import { PERM } from '../../../utils/permissions';
import { usePermission } from '../../../hooks/usePermission';

const TaiLieuVayList = () => {
  const { can } = usePermission();
  const [docs, setDocs] = useState<TaiLieuVay[]>([]);
  const [loans, setLoans] = useState<HoSoVay[]>([]);
  const [loading, setLoading] = useState(true);
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<TaiLieuVay | null>(null);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [loanError, setLoanError] = useState('');

  const [pagination, setPagination] = useState({ pageNumber: 1, pageSize: 50, totalCount: 0 });
  const [filterLoai, setFilterLoai] = useState('');
  const [filterTrangThai, setFilterTrangThai] = useState('');
  const [filterHoSo, setFilterHoSo] = useState('');

  const [form, setForm] = useState<CreateTaiLieuVayDTO>({
    maHoSo: 0,
    loaiTaiLieu: 'CCCD',
    tenTapTin: '',
    duongDanTapTin: '',
  });

  useEffect(() => {
    loadDocs();
    loadLoans();
  }, [pagination.pageNumber, filterLoai, filterTrangThai, filterHoSo]);

  const loadDocs = async () => {
    setLoading(true);
    try {
      const docRes = await taiLieuVayApi.getAll({
        pageNumber: pagination.pageNumber,
        pageSize: pagination.pageSize,
        loaiTaiLieu: filterLoai || undefined,
        trangThai: filterTrangThai || undefined,
        maHoSo: filterHoSo ? Number(filterHoSo) : undefined,
      });
      setDocs(docRes.data.items);
      setPagination((p) => ({ ...p, totalCount: docRes.data.totalCount }));
    } catch (err) {
      console.error('Failed to load documents:', err);
    } finally {
      setLoading(false);
    }
  };

  const loadLoans = async () => {
    setLoanError('');
    try {
      const loanRes = await hoSoVayApi.getAll({ pageSize: 200 });
      setLoans(loanRes.data.items);
    } catch (err) {
      console.error('Failed to load loan options:', err);
      setLoanError('Không tải được danh sách hồ sơ vay. Vui lòng thử lại.');
    }
  };

  const loanOptions = useMemo(() => loans.map((l) => ({
    value: l.maHoSo,
    label: `${l.maHoSoHienThi} - ${l.tenKhachHang}`,
  })), [loans]);

  const openCreate = () => {
    setEditing(null);
    setError('');
    setForm({ maHoSo: 0, loaiTaiLieu: 'CCCD', tenTapTin: '', duongDanTapTin: '' });
    setModalOpen(true);
  };

  const openEdit = (d: TaiLieuVay) => {
    setEditing(d);
    setError('');
    setForm({
      maHoSo: d.maHoSo,
      loaiTaiLieu: d.loaiTaiLieu,
      tenTapTin: d.tenTapTin,
      duongDanTapTin: d.duongDanTapTin,
      phienBan: d.phienBan,
      trangThaiXacMinh: d.trangThaiXacMinh,
      ghiChu: d.ghiChu,
    });
    setModalOpen(true);
  };

  const handleSave = async () => {
    if (!form.maHoSo || !form.loaiTaiLieu || !form.tenTapTin || !form.duongDanTapTin) {
      setError('Vui lòng điền đầy đủ thông tin bắt buộc');
      return;
    }
    if (!editing && loanOptions.length === 0) {
      setError('Không có hồ sơ vay để liên kết. Vui lòng tạo hồ sơ vay trước khi thêm tài liệu.');
      return;
    }
    setSaving(true);
    setError('');
    try {
      if (editing) {
        await taiLieuVayApi.update(editing.maTaiLieu, form);
      } else {
        await taiLieuVayApi.create(form);
      }
      setModalOpen(false);
      loadDocs();
      loadLoans();
    } catch (err: any) {
      setError(err.response?.data?.message || 'Lưu thất bại');
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async (id: number) => {
    if (!confirm('Xóa tài liệu này?')) return;
    try {
      await taiLieuVayApi.delete(id);
      loadDocs();
    } catch (err) {
      console.error('Failed:', err);
    }
  };

  const columns = [
    {
      key: 'maTaiLieu',
      title: 'Mã TL',
      width: '90px',
      render: (r: TaiLieuVay) => <span className="font-mono text-xs">#{r.maTaiLieu}</span>,
    },
    {
      key: 'maHoSo',
      title: 'Hồ sơ',
      render: (r: TaiLieuVay) => <span className="font-medium text-blue-600">{r.maHoSoHienThi}</span>,
    },
    { key: 'loaiTaiLieu', title: 'Loại tài liệu' },
    {
      key: 'tenTapTin',
      title: 'Tên tệp',
      render: (r: TaiLieuVay) => (
        <a
          href={r.duongDanTapTin}
          target="_blank"
          rel="noopener noreferrer"
          className="text-blue-600 hover:underline flex items-center gap-1"
          title="Mở file"
        >
          <FileText size={14} />
          {r.tenTapTin}
          <ExternalLink size={12} className="text-gray-400" />
        </a>
      ),
    },
    {
      key: 'trangThaiXacMinh',
      title: 'Trạng thái',
      render: (r: TaiLieuVay) => {
        const cfg = TRANG_THAI_XAC_MINH.find((t) => t.value === r.trangThaiXacMinh);
        return <StatusBadge status={r.trangThaiXacMinh} label={cfg?.label || r.trangThaiXacMinh} color={(cfg?.color as any) || 'gray'} />;
      },
    },
    {
      key: 'ngayTao',
      title: 'Ngày upload',
      render: (r: TaiLieuVay) => <span className="text-xs text-gray-600">{formatDateTime(r.ngayTao)}</span>,
    },
    {
      key: 'actions',
      title: '',
      width: '100px',
      render: (r: TaiLieuVay) => (
        <div className="flex gap-1 justify-center">
          {can(PERM.EDIT_DOCUMENT) && (
            <button
              type="button"
              onClick={() => openEdit(r)}
              className="p-1 hover:bg-gray-100 rounded"
              title="Sửa"
              aria-label="Sửa tài liệu"
            >
              <Edit2 size={16} className="text-blue-600" />
            </button>
          )}
          {can(PERM.DELETE_DOCUMENT) && (
            <button
              type="button"
              onClick={() => handleDelete(r.maTaiLieu)}
              className="p-1 hover:bg-red-100 rounded"
              title="Xóa"
              aria-label="Xóa tài liệu"
            >
              <Trash2 size={16} className="text-red-600" />
            </button>
          )}
        </div>
      ),
    },
  ];

  if (loading) return <Loading text="Đang tải tài liệu..." />;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <FileText className="w-6 h-6 text-blue-600" />
          <h1 className="text-2xl font-bold text-gray-800">Quản lý tài liệu vay</h1>
        </div>
        {can(PERM.CREATE_DOCUMENT) && (
          <Button leftIcon={<Plus size={18} />} onClick={openCreate}>
            Thêm tài liệu
          </Button>
        )}
      </div>

      {/* Filters */}
      <div className="bg-white rounded-xl shadow-sm p-4 border border-gray-100">
        <div className="flex items-center gap-2 mb-3">
          <Filter size={16} className="text-gray-500" />
          <h3 className="font-medium text-gray-700">Bộ lọc</h3>
        </div>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
          <Select
            label="Loại tài liệu"
            value={filterLoai}
            onChange={(v) => setFilterLoai(String(v))}
            options={[
              { value: '', label: 'Tất cả' },
              ...LOAI_TAI_LIEU.map((l) => ({ value: l.value, label: l.label })),
            ]}
          />
          <Select
            label="Trạng thái xác minh"
            value={filterTrangThai}
            onChange={(v) => setFilterTrangThai(String(v))}
            options={[
              { value: '', label: 'Tất cả' },
              ...TRANG_THAI_XAC_MINH.map((t) => ({ value: t.value, label: t.label })),
            ]}
          />
          <Select
            label="Hồ sơ vay"
            value={filterHoSo}
            onChange={(v) => setFilterHoSo(String(v))}
            options={[{ value: '', label: 'Tất cả' }, ...loanOptions]}
          />
        </div>
      </div>

      {/* Table */}
      <div className="bg-white rounded-xl shadow-sm border border-gray-100">
        <Table columns={columns} data={docs} emptyText="Chưa có tài liệu nào" />
        <Pagination
          currentPage={pagination.pageNumber}
          totalPages={Math.max(1, Math.ceil(pagination.totalCount / pagination.pageSize))}
          totalItems={pagination.totalCount}
          pageSize={pagination.pageSize}
          onPageChange={(page) => setPagination((p) => ({ ...p, pageNumber: page }))}
          onPageSizeChange={(size) => setPagination((p) => ({ ...p, pageSize: size, pageNumber: 1 }))}
        />
      </div>

      {/* Modal */}
      <Modal
        open={modalOpen}
        onClose={() => !saving && setModalOpen(false)}
        title={editing ? 'Sửa tài liệu' : 'Thêm tài liệu'}
        size="md"
        footer={
          <>
            <Button variant="outline" onClick={() => setModalOpen(false)} disabled={saving}>Hủy</Button>
            <Button
              onClick={handleSave}
              loading={saving}
              disabled={saving || (!editing && loanOptions.length === 0)}
              leftIcon={<ChevronRight size={16} />}
            >
              {editing ? 'Cập nhật' : 'Lưu'}
            </Button>
          </>
        }
      >
        {error && <div className="mb-4 bg-red-50 border border-red-200 text-red-600 px-4 py-3 rounded-lg text-sm">{error}</div>}
        <div className="space-y-3">
          <Select
            label="Hồ sơ vay *"
            value={form.maHoSo}
            onChange={(v) => setForm({ ...form, maHoSo: Number(v) })}
            options={[{ value: 0, label: '-- Chọn hồ sơ --' }, ...loanOptions]}
          />
          {loanError && (
            <div className="text-sm text-red-600">{loanError}</div>
          )}
          {!loanError && loanOptions.length === 0 && (
            <div className="text-sm text-yellow-600">Chưa có hồ sơ vay nào. Vui lòng tạo hồ sơ vay trước khi thêm tài liệu.</div>
          )}
          <Select
            label="Loại tài liệu *"
            value={form.loaiTaiLieu}
            onChange={(v) => setForm({ ...form, loaiTaiLieu: String(v) })}
            options={LOAI_TAI_LIEU.map((l) => ({ value: l.value, label: l.label }))}
          />
          <Input
            label="Tên tệp *"
            value={form.tenTapTin}
            onChange={(e) => setForm({ ...form, tenTapTin: e.target.value })}
            placeholder="CCCD_NguyenVanA.pdf"
          />
          <Input
            label="Đường dẫn tệp *"
            value={form.duongDanTapTin}
            onChange={(e) => setForm({ ...form, duongDanTapTin: e.target.value })}
            placeholder="https://example.com/files/..."
          />
          <Input
            label="Phiên bản"
            type="number"
            value={form.phienBan || 1}
            onChange={(e) => setForm({ ...form, phienBan: Number(e.target.value) })}
          />
          <Select
            label="Trạng thái xác minh"
            value={form.trangThaiXacMinh || 'CHO_KIEM_TRA'}
            onChange={(v) => setForm({ ...form, trangThaiXacMinh: v as any })}
            options={TRANG_THAI_XAC_MINH.map((t) => ({ value: t.value, label: t.label }))}
          />
          <Input
            label="Ghi chú"
            value={form.ghiChu || ''}
            onChange={(e) => setForm({ ...form, ghiChu: e.target.value })}
          />
        </div>
      </Modal>
    </div>
  );
};

export default TaiLieuVayList;
