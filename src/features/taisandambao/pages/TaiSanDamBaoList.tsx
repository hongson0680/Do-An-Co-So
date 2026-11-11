import { useState, useEffect, useMemo } from 'react';
import { Plus, Edit2, Trash2, Filter, Shield, ChevronRight } from 'lucide-react';
import { taiSanDamBaoApi, TaiSanDamBao, CreateTaiSanDamBaoDTO, LOAI_TAI_SAN, TINH_TRANG_PHAP_LY } from '../../../api/taisandambao.api';
import { hoSoVayApi, HoSoVay } from '../../../api/hosovay.api';
import Table, { Pagination } from '../../../components/common/Table';
import Button from '../../../components/common/Button';
import Input from '../../../components/common/Input';
import Select from '../../../components/common/Select';
import Modal from '../../../components/common/Modal';
import Loading from '../../../components/common/Loading';
import { formatCurrency, formatPercent } from '../../../utils/formatters';
import { PERM } from '../../../utils/permissions';
import { usePermission } from '../../../hooks/usePermission';

const TaiSanDamBaoList = () => {
  const { can } = usePermission();
  const [assets, setAssets] = useState<TaiSanDamBao[]>([]);
  const [loans, setLoans] = useState<HoSoVay[]>([]);
  const [loading, setLoading] = useState(true);
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<TaiSanDamBao | null>(null);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [loanLoading, setLoanLoading] = useState(true);
  const [loanError, setLoanError] = useState('');

  const [pagination, setPagination] = useState({ pageNumber: 1, pageSize: 50, totalCount: 0 });
  const [filterLoai, setFilterLoai] = useState('');
  const [filterHoSo, setFilterHoSo] = useState('');

  const [form, setForm] = useState<CreateTaiSanDamBaoDTO>({
    maHoSo: 0,
    loaiTaiSan: 'BĐS - Nhà ở',
    giaTriUocTinh: 0,
    tyLeVayGiaTriTS: 0.7,
  });

  useEffect(() => {
    loadAssets();
  }, [pagination.pageNumber, filterLoai, filterHoSo]);

  useEffect(() => {
    loadLoans();
  }, []);

  const loadAssets = async () => {
    setLoading(true);
    try {
      const assetRes = await taiSanDamBaoApi.getAll({
        pageNumber: pagination.pageNumber,
        pageSize: pagination.pageSize,
        loaiTaiSan: filterLoai || undefined,
        maHoSo: filterHoSo ? Number(filterHoSo) : undefined,
      });
      setAssets(assetRes.data.items);
      setPagination((p) => ({ ...p, totalCount: assetRes.data.totalCount }));
    } catch (err) {
      console.error('Failed to load assets:', err);
      setError('Không tải được danh sách tài sản đảm bảo.');
    } finally {
      setLoading(false);
    }
  };

  const loadLoans = async () => {
    setLoanLoading(true);
    setLoanError('');
    try {
      const loanRes = await hoSoVayApi.getAll({ pageSize: 200 });
      setLoans(loanRes.data.items);
    } catch (err) {
      console.error('Failed to load loans:', err);
      setLoanError('Không tải được danh sách hồ sơ vay.');
    } finally {
      setLoanLoading(false);
    }
  };

  const loanOptions = useMemo(() => loans.map((l) => ({
    value: l.maHoSo,
    label: `${l.maHoSoHienThi} - ${l.tenKhachHang} - ${formatCurrency(l.soTienDeNghi)}`,
  })), [loans]);

  const totalValue = useMemo(() => assets.reduce((s, a) => s + a.giaTriUocTinh, 0), [assets]);
  const totalMaxLoan = useMemo(() => assets.reduce((s, a) => s + a.giaTriChoVayToiDa, 0), [assets]);

  const openCreate = () => {
    setEditing(null);
    setError('');
    setForm({ maHoSo: 0, loaiTaiSan: 'BĐS - Nhà ở', giaTriUocTinh: 0, tyLeVayGiaTriTS: 0.7 });
    setModalOpen(true);
  };

  const openEdit = (a: TaiSanDamBao) => {
    setEditing(a);
    setError('');
    setForm({
      maHoSo: a.maHoSo,
      loaiTaiSan: a.loaiTaiSan,
      chuSoHuu: a.chuSoHuu,
      soGiayChungNhan: a.soGiayChungNhan,
      diaChiTaiSan: a.diaChiTaiSan,
      giaTriUocTinh: a.giaTriUocTinh,
      tyLeVayGiaTriTS: a.tyLeVayGiaTriTS,
      tinhTrangPhapLy: a.tinhTrangPhapLy,
      ngayDinhGia: a.ngayDinhGia,
      donViDinhGia: a.donViDinhGia,
      soHopDongBaoHiem: a.soHopDongBaoHiem,
      ngayHetHanBaoHiem: a.ngayHetHanBaoHiem,
    });
    setModalOpen(true);
  };

  const handleSave = async () => {
    if (!form.maHoSo || !form.loaiTaiSan || form.giaTriUocTinh <= 0 || form.tyLeVayGiaTriTS <= 0) {
      setError('Vui lòng điền đầy đủ thông tin bắt buộc');
      return;
    }
    setSaving(true);
    setError('');
    try {
      if (editing) {
        await taiSanDamBaoApi.update(editing.maTaiSan, form);
      } else {
        await taiSanDamBaoApi.create(form);
      }
      setModalOpen(false);
      loadAssets();
    } catch (err: any) {
      setError(err.response?.data?.message || 'Lưu thất bại');
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async (id: number) => {
    if (!confirm('Xóa tài sản bảo đảm này?')) return;
    try {
      await taiSanDamBaoApi.delete(id);
      loadAssets();
    } catch (err) {
      console.error('Failed:', err);
    }
  };

  const columns = [
    {
      key: 'maTaiSan',
      title: 'Mã TSBĐ',
      width: '100px',
      render: (r: TaiSanDamBao) => <span className="font-mono text-xs text-gray-600">#{r.maTaiSan}</span>,
    },
    {
      key: 'maHoSo',
      title: 'Hồ sơ',
      render: (r: TaiSanDamBao) => <span className="font-medium text-blue-600">{r.maHoSoHienThi}</span>,
    },
    { key: 'loaiTaiSan', title: 'Loại tài sản' },
    {
      key: 'chuSoHuu',
      title: 'Chủ sở hữu',
      render: (r: TaiSanDamBao) => r.chuSoHuu || <span className="text-gray-400">-</span>,
    },
    {
      key: 'giaTriUocTinh',
      title: 'Giá trị ước tính',
      align: 'right' as const,
      render: (r: TaiSanDamBao) => <span className="font-semibold">{formatCurrency(r.giaTriUocTinh)}</span>,
    },
    {
      key: 'tyLeVayGiaTriTS',
      title: 'LTV',
      align: 'center' as const,
      render: (r: TaiSanDamBao) => (
        <span className="text-sm">{formatPercent(r.tyLeVayGiaTriTS)}</span>
      ),
    },
    {
      key: 'giaTriChoVayToiDa',
      title: 'Cho vay tối đa',
      align: 'right' as const,
      render: (r: TaiSanDamBao) => (
        <span className="text-green-700 font-semibold">{formatCurrency(r.giaTriChoVayToiDa)}</span>
      ),
    },
    {
      key: 'actions',
      title: '',
      width: '100px',
      render: (r: TaiSanDamBao) => (
        <div className="flex gap-1 justify-center">
          {can(PERM.EDIT_COLLATERAL) && (
            <button
              type="button"
              onClick={() => openEdit(r)}
              className="p-1 hover:bg-gray-100 rounded"
              title="Sửa"
              aria-label="Sửa tài sản"
            >
              <Edit2 size={16} className="text-blue-600" />
            </button>
          )}
          {can(PERM.DELETE_COLLATERAL) && (
            <button
              type="button"
              onClick={() => handleDelete(r.maTaiSan)}
              className="p-1 hover:bg-red-100 rounded"
              title="Xóa"
              aria-label="Xóa tài sản"
            >
              <Trash2 size={16} className="text-red-600" />
            </button>
          )}
        </div>
      ),
    },
  ];

  if (loading) return <Loading text="Đang tải tài sản đảm bảo..." />;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <Shield className="w-6 h-6 text-indigo-600" />
          <h1 className="text-2xl font-bold text-gray-800">Quản lý tài sản đảm bảo</h1>
        </div>
        {can(PERM.CREATE_COLLATERAL) && (
          <Button leftIcon={<Plus size={18} />} onClick={openCreate}>
            Thêm TSBĐ
          </Button>
        )}
      </div>

      {/* Thống kê tổng quan */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div className="bg-white rounded-xl shadow-sm p-5 border border-gray-100">
          <p className="text-gray-500 text-sm">Số lượng TSBĐ</p>
          <p className="text-2xl font-bold text-gray-800 mt-1">{assets.length}</p>
        </div>
        <div className="bg-white rounded-xl shadow-sm p-5 border border-gray-100">
          <p className="text-gray-500 text-sm">Tổng giá trị ước tính</p>
          <p className="text-xl font-bold text-blue-700 mt-1">{formatCurrency(totalValue)}</p>
        </div>
        <div className="bg-white rounded-xl shadow-sm p-5 border border-gray-100">
          <p className="text-gray-500 text-sm">Tổng giá trị cho vay tối đa</p>
          <p className="text-xl font-bold text-green-700 mt-1">{formatCurrency(totalMaxLoan)}</p>
        </div>
      </div>

      {/* Filters */}
      <div className="bg-white rounded-xl shadow-sm p-4 border border-gray-100">
        <div className="flex items-center gap-2 mb-3">
          <Filter size={16} className="text-gray-500" />
          <h3 className="font-medium text-gray-700">Bộ lọc</h3>
        </div>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
          <Select
            label="Loại tài sản"
            value={filterLoai}
            onChange={(v) => setFilterLoai(String(v))}
            options={[
              { value: '', label: 'Tất cả' },
              ...LOAI_TAI_SAN.map((l) => ({ value: l.value, label: l.label })),
            ]}
          />
          <Select
            label="Hồ sơ vay"
            value={filterHoSo}
            onChange={(v) => setFilterHoSo(String(v))}
            options={[
              { value: '', label: 'Tất cả' },
              ...loanOptions,
            ]}
          />
        </div>
      </div>

      {/* Table */}
      <div className="bg-white rounded-xl shadow-sm border border-gray-100">
        <Table columns={columns} data={assets} emptyText="Chưa có tài sản bảo đảm nào" />
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
        title={editing ? 'Sửa tài sản đảm bảo' : 'Thêm tài sản đảm bảo'}
        size="lg"
        footer={
          <>
            <Button variant="outline" onClick={() => setModalOpen(false)} disabled={saving}>Hủy</Button>
            <Button onClick={handleSave} loading={saving} leftIcon={<ChevronRight size={16} />}>
              {editing ? 'Cập nhật' : 'Lưu'}
            </Button>
          </>
        }
      >
        {error && <div className="mb-4 bg-red-50 border border-red-200 text-red-600 px-4 py-3 rounded-lg text-sm">{error}</div>}
        <div className="grid grid-cols-2 gap-4">
          <Select
            label="Hồ sơ vay"
            value={form.maHoSo}
            onChange={(v) => setForm({ ...form, maHoSo: Number(v) })}
            options={loanOptions}
            placeholder={loanLoading ? 'Đang tải hồ sơ vay...' : '-- Chọn hồ sơ --'}
            className="col-span-2"
            disabled={loanLoading || loanOptions.length === 0}
          />
          {loanError && (
            <div className="col-span-2 text-sm text-red-600">{loanError}</div>
          )}
          <Select
            label="Loại tài sản"
            value={form.loaiTaiSan}
            onChange={(v) => setForm({ ...form, loaiTaiSan: String(v) })}
            options={LOAI_TAI_SAN.map((l) => ({ value: l.value, label: l.label }))}
          />
          <Select
            label="Tình trạng pháp lý"
            value={form.tinhTrangPhapLy || ''}
            onChange={(v) => setForm({ ...form, tinhTrangPhapLy: String(v) || undefined })}
            options={[{ value: '', label: '-- Chọn --' }, ...TINH_TRANG_PHAP_LY.map((t) => ({ value: t.value, label: t.label }))]}
          />
          <Input
            label="Chủ sở hữu"
            value={form.chuSoHuu || ''}
            onChange={(e) => setForm({ ...form, chuSoHuu: e.target.value })}
          />
          <Input
            label="Số GCN / Số đăng ký"
            value={form.soGiayChungNhan || ''}
            onChange={(e) => setForm({ ...form, soGiayChungNhan: e.target.value })}
          />
          <Input
            label="Địa chỉ tài sản"
            value={form.diaChiTaiSan || ''}
            onChange={(e) => setForm({ ...form, diaChiTaiSan: e.target.value })}
            className="col-span-2"
          />
          <Input
            label="Giá trị ước tính (VNĐ)"
            type="number"
            value={form.giaTriUocTinh}
            onChange={(e) => setForm({ ...form, giaTriUocTinh: Number(e.target.value) })}
          />
          <Input
            label="Tỷ lệ LTV (0-1)"
            type="number"
            step="0.01"
            min="0"
            max="1"
            value={form.tyLeVayGiaTriTS}
            onChange={(e) => setForm({ ...form, tyLeVayGiaTriTS: Number(e.target.value) })}
          />
          {form.giaTriUocTinh > 0 && form.tyLeVayGiaTriTS > 0 && (
            <div className="col-span-2 p-3 bg-green-50 border border-green-200 rounded-lg">
              <p className="text-xs text-gray-600">Giá trị cho vay tối đa:</p>
              <p className="text-xl font-bold text-green-700">
                {formatCurrency(form.giaTriUocTinh * form.tyLeVayGiaTriTS)}
              </p>
            </div>
          )}
          <Input
            label="Ngày định giá"
            type="date"
            value={form.ngayDinhGia || ''}
            onChange={(e) => setForm({ ...form, ngayDinhGia: e.target.value || undefined })}
          />
          <Input
            label="Đơn vị định giá"
            value={form.donViDinhGia || ''}
            onChange={(e) => setForm({ ...form, donViDinhGia: e.target.value })}
          />
          <Input
            label="Số HĐ bảo hiểm"
            value={form.soHopDongBaoHiem || ''}
            onChange={(e) => setForm({ ...form, soHopDongBaoHiem: e.target.value })}
          />
          <Input
            label="Ngày hết hạn bảo hiểm"
            type="date"
            value={form.ngayHetHanBaoHiem || ''}
            onChange={(e) => setForm({ ...form, ngayHetHanBaoHiem: e.target.value || undefined })}
          />
        </div>
      </Modal>
    </div>
  );
};

export default TaiSanDamBaoList;
