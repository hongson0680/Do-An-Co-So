import { useState, useEffect } from 'react';
import { Plus, Search, Edit2, Trash2 } from 'lucide-react';
import { khachHangApi, KhachHang, CreateKhachHangDTO } from '../../../api/khachhang.api';
import { exportApi } from '../../../api/export.api';
import Table, { Pagination } from '../../../components/common/Table';
import Button from '../../../components/common/Button';
import Input from '../../../components/common/Input';
import Modal from '../../../components/common/Modal';
import StatusBadge from '../../../components/common/StatusBadge';
import ExportButton from '../../../components/common/ExportButton';
import { formatDate } from '../../../utils/formatters';
import { LOAI_KHACH_HANG, MUC_DO_RUI_RO } from '../../../utils/constants';
import { PERM } from '../../../utils/permissions';
import { usePermission } from '../../../hooks/usePermission';

const CustomerList = () => {
  const { can } = usePermission();
  const [customers, setCustomers] = useState<KhachHang[]>([]);
  const [loading, setLoading] = useState(true);
  const [pagination, setPagination] = useState({
    pageNumber: 1,
    pageSize: 10,
    totalCount: 0,
    totalPages: 0,
  });
  const [search, setSearch] = useState('');
  const [filterLoai, setFilterLoai] = useState('');
  const [modalOpen, setModalOpen] = useState(false);
  const [editingCustomer, setEditingCustomer] = useState<KhachHang | null>(null);
  const [formData, setFormData] = useState<CreateKhachHangDTO>({
    loaiKhachHang: 'CA_NHAN',
    hoTen: '',
    soGiayTo: '',
  });
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    loadCustomers();
  }, [pagination.pageNumber, pagination.pageSize, search, filterLoai]);

  const loadCustomers = async () => {
    setLoading(true);
    try {
      const response = await khachHangApi.getAll({
        pageNumber: pagination.pageNumber,
        pageSize: pagination.pageSize,
        search: search || undefined,
        loaiKhachHang: filterLoai || undefined,
      });
      setCustomers(response.data.items);
      setPagination((prev) => ({
        ...prev,
        totalCount: response.data.totalCount,
        totalPages: Math.ceil(response.data.totalCount / prev.pageSize),
      }));
    } catch (error) {
      console.error('Failed to load customers:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleSave = async (e?: React.FormEvent) => {
    if (e) e.preventDefault();
    setSaving(true);
    try {
      if (editingCustomer) {
        await khachHangApi.update(editingCustomer.maKhachHang, formData);
      } else {
        await khachHangApi.create(formData);
      }
      setModalOpen(false);
      loadCustomers();
    } catch (error) {
      console.error('Failed to save:', error);
      alert('Lỗi khi lưu khách hàng: ' + (error as any)?.response?.data?.message || 'Vui lòng thử lại');
    } finally {
      setSaving(false);
    }
  };

  const handleEdit = (customer: KhachHang) => {
    setEditingCustomer(customer);
    setFormData({
      loaiKhachHang: customer.loaiKhachHang,
      hoTen: customer.hoTen,
      soGiayTo: customer.soGiayTo,
      gioiTinh: customer.gioiTinh,
      ngaySinh: customer.ngaySinh,
      soDienThoai: customer.soDienThoai,
      email: customer.email,
      diaChi: customer.diaChi,
      ngheNghiep: customer.ngheNghiep,
    });
    setModalOpen(true);
  };

  const handleDelete = async (id: number) => {
    if (confirm('Bạn có chắc chắn muốn xóa?')) {
      try {
        await khachHangApi.delete(id);
        loadCustomers();
      } catch (error) {
        console.error('Failed to delete:', error);
      }
    }
  };

  const columns = [
    {
      key: 'hoTen',
      title: 'Họ tên',
      render: (r: KhachHang) => (
        <div>
          <p className="font-medium">{r.hoTen}</p>
          <p className="text-xs text-gray-500">{r.soGiayTo}</p>
        </div>
      ),
    },
    {
      key: 'loaiKhachHang',
      title: 'Loại',
      render: (r: KhachHang) => (
        <StatusBadge
          status={r.loaiKhachHang}
          label={LOAI_KHACH_HANG[r.loaiKhachHang]?.label}
          color={LOAI_KHACH_HANG[r.loaiKhachHang]?.color as 'blue' | 'purple'}
        />
      ),
    },
    { key: 'soDienThoai', title: 'Điện thoại' },
    { key: 'email', title: 'Email' },
    {
      key: 'mucDoRuiRo',
      title: 'Rủi ro',
      render: (r: KhachHang) => {
        const config = MUC_DO_RUI_RO[r.mucDoRuiRo as keyof typeof MUC_DO_RUI_RO];
        return (
          <StatusBadge
            status={r.mucDoRuiRo}
            label={config?.label}
            color={config?.color as 'green' | 'yellow' | 'red'}
          />
        );
      },
    },
    {
      key: 'ngayTao',
      title: 'Ngày tạo',
      render: (r: KhachHang) => formatDate(r.ngayTao),
    },
    {
      key: 'actions',
      title: 'Thao tác',
      width: '120px',
      align: 'center' as const,
      render: (r: KhachHang) => (
        <div className="flex items-center justify-center gap-2">
          {can(PERM.EDIT_CUSTOMER) && (
            <button
              onClick={() => handleEdit(r)}
              className="p-1.5 hover:bg-gray-100 rounded"
              title="Sửa"
              aria-label="Sửa khách hàng"
            >
              <Edit2 size={16} className="text-blue-600" />
            </button>
          )}
          {can(PERM.DELETE_CUSTOMER) && (
            <button
              onClick={() => handleDelete(r.maKhachHang)}
              className="p-1.5 hover:bg-gray-100 rounded"
              title="Xóa"
              aria-label="Xóa khách hàng"
            >
              <Trash2 size={16} className="text-red-600" />
            </button>
          )}
        </div>
      ),
    },
  ];

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-800">Khách hàng</h1>
        <div className="flex items-center gap-2">
          <ExportButton
            label="KH"
            onExport={(format) => exportApi.exportKhachHang({
              format,
              search: search || undefined,
              loaiKhachHang: filterLoai || undefined,
            })}
          />
          {can(PERM.CREATE_CUSTOMER) && (
            <Button
              leftIcon={<Plus size={18} />}
              onClick={() => {
                setEditingCustomer(null);
                setFormData({
                  loaiKhachHang: 'CA_NHAN',
                  hoTen: '',
                  soGiayTo: '',
                });
                setModalOpen(true);
              }}
            >
              Thêm khách hàng
            </Button>
          )}
        </div>
      </div>

      {/* Filters */}
      <div className="bg-white rounded-xl shadow-sm p-4 border border-gray-100 flex flex-wrap gap-4">
        <div className="flex-1 min-w-[200px]">
          <Input
            placeholder="Tìm kiếm theo tên, CCCD..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            leftIcon={<Search size={18} />}
          />
        </div>
        <div className="w-48">
          <select
            aria-label="Lọc theo loại khách hàng"
            value={filterLoai}
            onChange={(e) => setFilterLoai(e.target.value)}
            className="block w-full rounded-lg border border-gray-300 px-3 py-2 text-gray-900 focus:outline-none focus:ring-2 focus:ring-blue-500"
          >
            <option value="">Tất cả</option>
            <option value="CA_NHAN">Cá nhân</option>
            <option value="DOANH_NGHIEP">Doanh nghiệp</option>
          </select>
        </div>
      </div>

      {/* Table */}
      <div className="bg-white rounded-xl shadow-sm border border-gray-100">
        <Table
          columns={columns}
          data={customers}
          loading={loading}
          emptyText="Không có khách hàng nào"
        />
        <Pagination
          currentPage={pagination.pageNumber}
          totalPages={pagination.totalPages}
          totalItems={pagination.totalCount}
          pageSize={pagination.pageSize}
          onPageChange={(page) =>
            setPagination((prev) => ({ ...prev, pageNumber: page }))
          }
          onPageSizeChange={(size) =>
            setPagination((prev) => ({ ...prev, pageSize: size, pageNumber: 1 }))
          }
        />
      </div>

      {/* Modal */}
      <Modal
        open={modalOpen}
        onClose={() => setModalOpen(false)}
        title={editingCustomer ? 'Sửa khách hàng' : 'Thêm khách hàng'}
        size="lg"
      >
        <form onSubmit={handleSave} className="flex flex-col gap-4" noValidate>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Loại khách hàng</label>
              <select
                aria-label="Loại khách hàng"
                value={formData.loaiKhachHang}
                onChange={(e) =>
                  setFormData({ ...formData, loaiKhachHang: e.target.value as 'CA_NHAN' | 'DOANH_NGHIEP' })
                }
                className="block w-full rounded-lg border border-gray-300 px-3 py-2 text-gray-900 focus:outline-none focus:ring-2 focus:ring-blue-500"
              >
                <option value="CA_NHAN">Cá nhân</option>
                <option value="DOANH_NGHIEP">Doanh nghiệp</option>
              </select>
            </div>
            <Input
              label="Họ tên"
              value={formData.hoTen}
              onChange={(e) => setFormData({ ...formData, hoTen: e.target.value })}
              required
            />
            <Input
              label="Số CCCD/MST"
              value={formData.soGiayTo}
              onChange={(e) => setFormData({ ...formData, soGiayTo: e.target.value })}
              required
            />
            <Input
              label="Số điện thoại"
              value={formData.soDienThoai || ''}
              onChange={(e) => setFormData({ ...formData, soDienThoai: e.target.value })}
            />
            <Input
              label="Email"
              type="email"
              value={formData.email || ''}
              onChange={(e) => setFormData({ ...formData, email: e.target.value })}
            />
            <Input
              label="Ngày sinh"
              type="date"
              value={formData.ngaySinh || ''}
              onChange={(e) => setFormData({ ...formData, ngaySinh: e.target.value })}
            />
            <Input
              label="Địa chỉ"
              value={formData.diaChi || ''}
              onChange={(e) => setFormData({ ...formData, diaChi: e.target.value })}
              className="col-span-2"
            />
            <Input
              label="Nghề nghiệp"
              value={formData.ngheNghiep || ''}
              onChange={(e) => setFormData({ ...formData, ngheNghiep: e.target.value })}
            />
          </div>

          {/* Footer buttons INSIDE the form */}
          <div className="flex justify-end gap-3 pt-4 border-t">
            <Button
              type="button"
              variant="outline"
              onClick={() => setModalOpen(false)}
            >
              Hủy
            </Button>
            <Button
              type="submit"
              loading={saving}
              disabled={saving || !formData.hoTen.trim() || !formData.soGiayTo.trim()}
            >
              Lưu
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  );
};

export default CustomerList;
