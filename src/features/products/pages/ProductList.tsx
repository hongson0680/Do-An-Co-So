import { useState, useEffect } from 'react';
import { Plus, Edit2, Trash2 } from 'lucide-react';
import { sanPhamVayApi, SanPhamVay, CreateSanPhamVayDTO } from '../../../api/sanpham.api';
import Table from '../../../components/common/Table';
import Button from '../../../components/common/Button';
import Input from '../../../components/common/Input';
import Modal from '../../../components/common/Modal';
import StatusBadge from '../../../components/common/StatusBadge';
import Loading from '../../../components/common/Loading';
import { formatCurrency } from '../../../utils/formatters';

const ProductList = () => {
  const [products, setProducts] = useState<SanPhamVay[]>([]);
  const [loading, setLoading] = useState(true);
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<SanPhamVay | null>(null);
  const [formData, setFormData] = useState<CreateSanPhamVayDTO>({
    maSanPhamVay: '',
    tenSanPham: '',
    laiSuat: 0.1,
    kyHanToiThieu: 1,
    kyHanToiDa: 12,
    hanMucToiThieu: 10000000,
    hanMucToiDa: 100000000,
    yeuCauTsdb: false,
  });
  const [saving, setSaving] = useState(false);

  useEffect(() => { loadProducts(); }, []);

  const loadProducts = async () => {
    setLoading(true);
    try {
      const response = await sanPhamVayApi.getAll({ pageSize: 100 });
      setProducts(response.data.items);
    } catch (error) {
      console.error('Failed:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleSave = async () => {
    setSaving(true);
    try {
      if (editing) {
        await sanPhamVayApi.update(editing.maSanPham, formData);
      } else {
        await sanPhamVayApi.create(formData);
      }
      setModalOpen(false);
      loadProducts();
    } catch (error) {
      console.error('Failed:', error);
    } finally {
      setSaving(false);
    }
  };

  const handleEdit = (p: SanPhamVay) => {
    setEditing(p);
    setFormData({
      maSanPhamVay: p.maSanPhamVay,
      tenSanPham: p.tenSanPham,
      moTa: p.moTa,
      laiSuat: p.laiSuat,
      kyHanToiThieu: p.kyHanToiThieu,
      kyHanToiDa: p.kyHanToiDa,
      hanMucToiThieu: p.hanMucToiThieu,
      hanMucToiDa: p.hanMucToiDa,
      doiTuongKh: p.doiTuongKh,
      yeuCauTsdb: p.yeuCauTsdb,
    });
    setModalOpen(true);
  };

  const handleDelete = async (id: number) => {
    if (confirm('Xóa sản phẩm này?')) {
      try {
        await sanPhamVayApi.delete(id);
        loadProducts();
      } catch (error) {
        console.error('Failed:', error);
      }
    }
  };

  const columns = [
    { key: 'maSanPhamVay', title: 'Mã SP', render: (r: SanPhamVay) => <span className="font-medium">{r.maSanPhamVay}</span> },
    { key: 'tenSanPham', title: 'Tên sản phẩm' },
    { key: 'laiSuat', title: 'Lãi suất', align: 'center' as const, render: (r: SanPhamVay) => `${(r.laiSuat * 100).toFixed(2)}%` },
    { key: 'kyHanToiThieu', title: 'Kỳ hạn min', align: 'center' as const, render: (r: SanPhamVay) => `${r.kyHanToiThieu} tháng` },
    { key: 'kyHanToiDa', title: 'Kỳ hạn max', align: 'center' as const, render: (r: SanPhamVay) => `${r.kyHanToiDa} tháng` },
    { key: 'hanMucToiThieu', title: 'Hạn mức min', align: 'right' as const, render: (r: SanPhamVay) => formatCurrency(r.hanMucToiThieu) },
    { key: 'hanMucToiDa', title: 'Hạn mức max', align: 'right' as const, render: (r: SanPhamVay) => r.hanMucToiDa ? formatCurrency(r.hanMucToiDa) : 'Không giới hạn' },
    { key: 'trangThai', title: 'Trạng thái', render: (r: SanPhamVay) => <StatusBadge status={r.trangThai} label={r.trangThai === 'HOAT_DONG' ? 'Hoạt động' : 'Không'} color={r.trangThai === 'HOAT_DONG' ? 'green' : 'gray'} /> },
    { key: 'actions', title: '', width: '100px', align: 'center' as const, render: (r: SanPhamVay) => (
      <div className="flex gap-2 justify-center">
        <button onClick={() => handleEdit(r)} className="p-1 hover:bg-gray-100 rounded"><Edit2 size={16} className="text-blue-600" /></button>
        <button onClick={() => handleDelete(r.maSanPham)} className="p-1 hover:bg-gray-100 rounded"><Trash2 size={16} className="text-red-600" /></button>
      </div>
    )},
  ];

  if (loading) return <Loading text="Đang tải..." />;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-800">Sản phẩm vay</h1>
        <Button leftIcon={<Plus size={18} />} onClick={() => { setEditing(null); setModalOpen(true); }}>Thêm sản phẩm</Button>
      </div>
      <div className="bg-white rounded-xl shadow-sm border border-gray-100">
        <Table columns={columns} data={products} emptyText="Chưa có sản phẩm" />
      </div>
      <Modal open={modalOpen} onClose={() => setModalOpen(false)} title={editing ? 'Sửa sản phẩm' : 'Thêm sản phẩm'} size="lg"
        footer={<><Button variant="outline" onClick={() => setModalOpen(false)}>Hủy</Button><Button loading={saving} onClick={handleSave}>Lưu</Button></>}>
        <div className="grid grid-cols-2 gap-4">
          <Input label="Mã sản phẩm" value={formData.maSanPhamVay} onChange={(e) => setFormData({ ...formData, maSanPhamVay: e.target.value })} />
          <Input label="Tên sản phẩm" value={formData.tenSanPham} onChange={(e) => setFormData({ ...formData, tenSanPham: e.target.value })} />
          <Input label="Lãi suất (0.01 = 1%)" type="number" step="0.001" value={formData.laiSuat} onChange={(e) => setFormData({ ...formData, laiSuat: Number(e.target.value) })} />
          <Input label="Kỳ hạn tối thiểu (tháng)" type="number" value={formData.kyHanToiThieu} onChange={(e) => setFormData({ ...formData, kyHanToiThieu: Number(e.target.value) })} />
          <Input label="Kỳ hạn tối đa (tháng)" type="number" value={formData.kyHanToiDa} onChange={(e) => setFormData({ ...formData, kyHanToiDa: Number(e.target.value) })} />
          <Input label="Hạn mức tối thiểu" type="number" value={formData.hanMucToiThieu} onChange={(e) => setFormData({ ...formData, hanMucToiThieu: Number(e.target.value) })} />
          <Input label="Hạn mức tối đa" type="number" value={formData.hanMucToiDa || 0} onChange={(e) => setFormData({ ...formData, hanMucToiDa: Number(e.target.value) || undefined })} />
          <Input label="Đối tượng KH" value={formData.doiTuongKh || ''} onChange={(e) => setFormData({ ...formData, doiTuongKh: e.target.value })} />
          <Input label="Mô tả" value={formData.moTa || ''} onChange={(e) => setFormData({ ...formData, moTa: e.target.value })} className="col-span-2" />
        </div>
      </Modal>
    </div>
  );
};

export default ProductList;
