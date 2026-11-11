import { useState, useEffect } from 'react';
import { Plus, Edit2, Trash2 } from 'lucide-react';
import { chiNhanhApi, ChiNhanh, CreateChiNhanhDTO } from '../../../api/chinhanh.api';
import Table from '../../../components/common/Table';
import Button from '../../../components/common/Button';
import Input from '../../../components/common/Input';
import Modal from '../../../components/common/Modal';
import StatusBadge from '../../../components/common/StatusBadge';
import Loading from '../../../components/common/Loading';

const BranchList = () => {
  const [branches, setBranches] = useState<ChiNhanh[]>([]);
  const [loading, setLoading] = useState(true);
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<ChiNhanh | null>(null);
  const [formData, setFormData] = useState<CreateChiNhanhDTO>({
    maChiNhanhHienThi: '',
    tenChiNhanh: '',
    diaChi: '',
    soDienThoai: '',
    truongChiNhanh: '',
    vungDiaLy: '',
  });
  const [saving, setSaving] = useState(false);

  useEffect(() => { loadBranches(); }, []);

  const loadBranches = async () => {
    setLoading(true);
    try {
      const response = await chiNhanhApi.getAll();
      setBranches(response.data);
    } catch (error) {
      console.error('Failed:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleSave = async () => {
    // Validate phía client trước khi gọi API
    if (!formData.maChiNhanhHienThi.trim()) {
      alert('Vui lòng nhập mã chi nhánh');
      return;
    }
    if (!formData.tenChiNhanh.trim()) {
      alert('Vui lòng nhập tên chi nhánh');
      return;
    }
    if (formData.soDienThoai && !/^\d{10}$/.test(formData.soDienThoai)) {
      alert('Số điện thoại phải đúng 10 chữ số');
      return;
    }

    setSaving(true);
    try {
      if (editing) {
        await chiNhanhApi.update(editing.maChiNhanh, formData);
      } else {
        await chiNhanhApi.create(formData);
      }
      setModalOpen(false);
      loadBranches();
    } catch (error: any) {
      console.error('Failed:', error);
      const message = error?.response?.data?.message || 'Lỗi khi lưu chi nhánh. Vui lòng thử lại.';
      alert('Lỗi: ' + message);
    } finally {
      setSaving(false);
    }
  };

  const handleEdit = (b: ChiNhanh) => {
    setEditing(b);
    setFormData({
      maChiNhanhHienThi: b.maChiNhanhHienThi,
      tenChiNhanh: b.tenChiNhanh,
      diaChi: b.diaChi || '',
      soDienThoai: b.soDienThoai || '',
      truongChiNhanh: b.truongChiNhanh || '',
      vungDiaLy: b.vungDiaLy || '',
    });
    setModalOpen(true);
  };

  const handleDelete = async (id: number) => {
    if (confirm('Xóa (ngừng hoạt động) chi nhánh này?')) {
      try {
        await chiNhanhApi.delete(id);
        loadBranches();
      } catch (error: any) {
        console.error('Failed:', error);
        const message = error?.response?.data?.message || 'Lỗi khi xóa chi nhánh.';
        alert('Lỗi: ' + message);
      }
    }
  };

  const columns = [
    { key: 'maChiNhanhHienThi', title: 'Mã CN', render: (r: ChiNhanh) => <span className="font-medium">{r.maChiNhanhHienThi}</span> },
    { key: 'tenChiNhanh', title: 'Tên chi nhánh' },
    { key: 'diaChi', title: 'Địa chỉ' },
    { key: 'soDienThoai', title: 'Điện thoại' },
    { key: 'truongChiNhanh', title: 'Trưởng CN' },
    { key: 'vungDiaLy', title: 'Vùng' },
    { key: 'trangThai', title: 'Trạng thái', render: (r: ChiNhanh) => (
      <StatusBadge status={r.trangThai} label={r.trangThai === 'HOAT_DONG' ? 'Hoạt động' : 'Không'} color={r.trangThai === 'HOAT_DONG' ? 'green' : 'gray'} />
    )},
    { key: 'actions', title: '', width: '100px', align: 'center' as const, render: (r: ChiNhanh) => (
      <div className="flex gap-2 justify-center">
        <button onClick={() => handleEdit(r)} className="p-1 hover:bg-gray-100 rounded"><Edit2 size={16} className="text-blue-600" /></button>
        <button onClick={() => handleDelete(r.maChiNhanh)} className="p-1 hover:bg-gray-100 rounded"><Trash2 size={16} className="text-red-600" /></button>
      </div>
    )},
  ];

  if (loading) return <Loading text="Đang tải..." />;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-800">Chi nhánh</h1>
        <Button leftIcon={<Plus size={18} />} onClick={() => { setEditing(null); setModalOpen(true); }}>Thêm chi nhánh</Button>
      </div>
      <div className="bg-white rounded-xl shadow-sm border border-gray-100">
        <Table columns={columns} data={branches} emptyText="Chưa có chi nhánh" />
      </div>
      <Modal open={modalOpen} onClose={() => setModalOpen(false)} title={editing ? 'Sửa chi nhánh' : 'Thêm chi nhánh'} size="lg"
        footer={<><Button variant="outline" onClick={() => setModalOpen(false)}>Hủy</Button><Button loading={saving} onClick={handleSave}>Lưu</Button></>}>
        <div className="grid grid-cols-2 gap-4">
          <Input label="Mã chi nhánh" value={formData.maChiNhanhHienThi} onChange={(e) => setFormData({ ...formData, maChiNhanhHienThi: e.target.value })} />
          <Input label="Tên chi nhánh" value={formData.tenChiNhanh} onChange={(e) => setFormData({ ...formData, tenChiNhanh: e.target.value })} />
          <Input label="Địa chỉ" value={formData.diaChi || ''} onChange={(e) => setFormData({ ...formData, diaChi: e.target.value })} className="col-span-2" />
          <Input label="Số điện thoại" value={formData.soDienThoai || ''} onChange={(e) => setFormData({ ...formData, soDienThoai: e.target.value })} />
          <Input label="Trưởng chi nhánh" value={formData.truongChiNhanh || ''} onChange={(e) => setFormData({ ...formData, truongChiNhanh: e.target.value })} />
          <Input label="Vùng địa lý" value={formData.vungDiaLy || ''} onChange={(e) => setFormData({ ...formData, vungDiaLy: e.target.value })} />
        </div>
      </Modal>
    </div>
  );
};

export default BranchList;
