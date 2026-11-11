import { useState, useEffect } from 'react';
import { Plus, Trash2, Search } from 'lucide-react';
import { nguoiDungApi, NguoiDung, CreateNguoiDungDTO } from '../../../api/nguoidung.api';
import { vaiTroApi, VaiTro } from '../../../api/vaitro.api';
import { chiNhanhApi } from '../../../api/chinhanh.api';
import Table, { Pagination } from '../../../components/common/Table';
import Button from '../../../components/common/Button';
import Input from '../../../components/common/Input';
import Select from '../../../components/common/Select';
import Modal from '../../../components/common/Modal';
import StatusBadge from '../../../components/common/StatusBadge';
import Loading from '../../../components/common/Loading';

const UserList = () => {
  const [users, setUsers] = useState<NguoiDung[]>([]);
  const [loading, setLoading] = useState(true);
  const [modalOpen, setModalOpen] = useState(false);
  const [branches, setBranches] = useState<{ maChiNhanh: number; tenChiNhanh: string }[]>([]);
  const [vaiTros, setVaiTros] = useState<VaiTro[]>([]);
  const [pagination, setPagination] = useState({
    pageNumber: 1,
    pageSize: 10,
    totalCount: 0,
    totalPages: 0,
  });
  const [search, setSearch] = useState('');
  const [formData, setFormData] = useState<CreateNguoiDungDTO>({
    tenDangNhap: '',
    matKhau: '',
    hoTen: '',
    email: '',
    soDienThoai: '',
    maVaiTro: 0,
    maChiNhanh: 0,
  });
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    loadBranches();
    loadVaiTros();
  }, []);

  useEffect(() => {
    loadUsers();
  }, [pagination.pageNumber, pagination.pageSize, search]);

  const loadUsers = async () => {
    setLoading(true);
    try {
      const response = await nguoiDungApi.getAll({
        pageNumber: pagination.pageNumber,
        pageSize: pagination.pageSize,
        search: search || undefined,
      });
      setUsers(response.data.items);
      setPagination((prev) => ({
        ...prev,
        totalCount: response.data.totalCount,
        totalPages: Math.ceil(response.data.totalCount / prev.pageSize),
      }));
    } catch (error) {
      console.error('Failed:', error);
    } finally {
      setLoading(false);
    }
  };

  const loadBranches = async () => {
    try {
      const response = await chiNhanhApi.getAll();
      setBranches(response.data.map((b) => ({ maChiNhanh: b.maChiNhanh, tenChiNhanh: b.tenChiNhanh })));
    } catch (error) {
      console.error('Failed:', error);
    }
  };

  const loadVaiTros = async () => {
    try {
      const response = await vaiTroApi.getAll({ activeOnly: true });
      setVaiTros(response.data);
      // Set default to first available role
      if (response.data.length > 0 && formData.maVaiTro === 0) {
        setFormData((prev) => ({ ...prev, maVaiTro: response.data[0].maVaiTro, maChiNhanh: prev.maChiNhanh || 0 }));
      }
    } catch (error) {
      console.error('Failed to load vai tro:', error);
    }
  };

  const roleOptions = vaiTros.map((v) => ({
    value: v.maVaiTro,
    label: `${v.tenVaiTro}${v.laVaiTroHeThong ? ' 🔒' : ''}`,
  }));

  const handleSave = async () => {
    setSaving(true);
    setError('');
    try {
      // Chuyển đổi chuỗi rỗng thành undefined cho soDienThoai (optional field)
      const dataToSubmit: CreateNguoiDungDTO = {
        tenDangNhap: formData.tenDangNhap,
        matKhau: formData.matKhau,
        hoTen: formData.hoTen,
        email: formData.email,
        soDienThoai: formData.soDienThoai?.trim() || undefined,
        maVaiTro: Number(formData.maVaiTro),
        maChiNhanh: Number(formData.maChiNhanh),
      };
      console.log('Submitting:', dataToSubmit);
      await nguoiDungApi.create(dataToSubmit);
      setModalOpen(false);
      loadUsers();
      setFormData({
        tenDangNhap: '',
        matKhau: '',
        hoTen: '',
        email: '',
        soDienThoai: '',
        maVaiTro: vaiTros[0]?.maVaiTro ?? 0,
        maChiNhanh: branches[0]?.maChiNhanh ?? 0,
      });
    } catch (err: unknown) {
      const error = err as { response?: { data?: { message?: string } } };
      console.error('Save error:', err);
      setError(error.response?.data?.message || 'Lưu thất bại');
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async (id: number) => {
    if (confirm('Xóa người dùng này?')) {
      try {
        await nguoiDungApi.delete(id);
        loadUsers();
      } catch (error) {
        console.error('Failed:', error);
      }
    }
  };

  const columns = [
    { key: 'tenDangNhap', title: 'Tên đăng nhập', render: (r: NguoiDung) => <span className="font-medium">{r.tenDangNhap}</span> },
    { key: 'hoTen', title: 'Họ tên' },
    { key: 'email', title: 'Email' },
    { key: 'soDienThoai', title: 'Điện thoại' },
    { key: 'tenVaiTro', title: 'Vai trò' },
    { key: 'tenChiNhanh', title: 'Chi nhánh' },
    { key: 'trangThai', title: 'Trạng thái', render: (r: NguoiDung) => (
      <StatusBadge status={r.trangThai} label={r.trangThai === 'HOAT_DONG' ? 'Hoạt động' : 'Không'} color={r.trangThai === 'HOAT_DONG' ? 'green' : 'gray'} />
    )},
    { key: 'actions', title: '', width: '100px', align: 'center' as const, render: (r: NguoiDung) => (
      <div className="flex gap-2 justify-center">
        <button
          type="button"
          onClick={() => handleDelete(r.maNguoiDung)}
          className="p-1 hover:bg-gray-100 rounded"
          aria-label={`Xóa người dùng ${r.tenDangNhap}`}
          title="Xóa"
        >
          <Trash2 size={16} className="text-red-600" />
        </button>
      </div>
    )},
  ];

  if (loading) return <Loading text="Đang tải..." />;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-800">Người dùng</h1>
        <Button leftIcon={<Plus size={18} />} onClick={() => setModalOpen(true)}>Thêm người dùng</Button>
      </div>
      <div className="bg-white rounded-xl shadow-sm border border-gray-100 p-4">
        <div className="relative max-w-sm">
          <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
          <input
            type="text"
            value={search}
            onChange={(e) => {
              setSearch(e.target.value);
              setPagination((prev) => ({ ...prev, pageNumber: 1 }));
            }}
            placeholder="Tìm theo tên hoặc tên đăng nhập..."
            className="w-full pl-9 pr-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>
      </div>
      <div className="bg-white rounded-xl shadow-sm border border-gray-100">
        <Table columns={columns} data={users} emptyText="Chưa có người dùng" />
        <Pagination
          currentPage={pagination.pageNumber}
          totalPages={pagination.totalPages}
          totalItems={pagination.totalCount}
          pageSize={pagination.pageSize}
          onPageChange={(page) => setPagination((prev) => ({ ...prev, pageNumber: page }))}
          onPageSizeChange={(size) => setPagination((prev) => ({ ...prev, pageSize: size, pageNumber: 1 }))}
        />
      </div>
      <Modal open={modalOpen} onClose={() => setModalOpen(false)} title="Thêm người dùng" size="lg"
        footer={<><Button variant="outline" onClick={() => setModalOpen(false)}>Hủy</Button><Button loading={saving} onClick={handleSave}>Lưu</Button></>}>
        {error && <div className="mb-4 bg-red-50 border border-red-200 text-red-600 px-4 py-3 rounded-lg text-sm">{error}</div>}
        <div className="grid grid-cols-2 gap-4">
          <Input label="Tên đăng nhập" value={formData.tenDangNhap} onChange={(e) => setFormData({ ...formData, tenDangNhap: e.target.value })} />
          <Input label="Mật khẩu" type="password" value={formData.matKhau} onChange={(e) => setFormData({ ...formData, matKhau: e.target.value })} />
          <Input label="Họ tên" value={formData.hoTen} onChange={(e) => setFormData({ ...formData, hoTen: e.target.value })} />
          <Input label="Email" type="email" value={formData.email} onChange={(e) => setFormData({ ...formData, email: e.target.value })} />
          <Input label="Số điện thoại" value={formData.soDienThoai || ''} onChange={(e) => setFormData({ ...formData, soDienThoai: e.target.value })} />
          <Select label="Vai trò" value={formData.maVaiTro} onChange={(value) => setFormData({ ...formData, maVaiTro: Number(value) })} options={roleOptions} />
          <Select label="Chi nhánh" value={formData.maChiNhanh} onChange={(value) => setFormData({ ...formData, maChiNhanh: Number(value) })} options={branches.map((b) => ({ value: b.maChiNhanh, label: b.tenChiNhanh }))} />
        </div>
      </Modal>
    </div>
  );
};

export default UserList;
