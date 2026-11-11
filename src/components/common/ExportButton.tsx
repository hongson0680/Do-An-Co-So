import { useState } from 'react';
import { FileSpreadsheet, FileText, Download, Loader2 } from 'lucide-react';
import { ExportFormat } from '../../api/export.api';
import { PERM } from '../../utils/permissions';
import { usePermission } from '../../hooks/usePermission';

interface ExportButtonProps {
  /** Hàm export từ exportApi */
  onExport: (format: ExportFormat) => Promise<void>;
  /** Tên hiển thị (vd: "Danh sách khách hàng") */
  label?: string;
  /** Chỉ hiển thị 1 nút (true) hoặc 2 nút dropdown (false) */
  variant?: 'split' | 'menu';
  /** Variant đơn giản: 1 button duy nhất, mặc định excel */
  size?: 'sm' | 'md';
  /** Custom class */
  className?: string;
}

/**
 * Component nút xuất Excel/PDF dùng chung.
 * variant="split": 1 nút dropdown có cả 2 option
 * variant="menu":   1 nút với icon chung, mở menu khi click
 */
const ExportButton = ({ onExport, label = 'Xuất', size = 'md', className = '' }: ExportButtonProps) => {
  const [open, setOpen] = useState(false);
  const [loading, setLoading] = useState<ExportFormat | null>(null);
  const { can } = usePermission();

  // Chỉ người có quyền EXPORT_DATA (mọi vai trừ CREDIT_OFFICER) thấy nút
  if (!can(PERM.EXPORT_DATA)) return null;

  const handleExport = async (format: ExportFormat) => {
    setLoading(format);
    setOpen(false);
    try {
      await onExport(format);
    } catch (err) {
      console.error('Export failed:', err);
      alert('Xuất file thất bại. Vui lòng thử lại.');
    } finally {
      setLoading(null);
    }
  };

  const sizeClass = size === 'sm' ? 'px-2.5 py-1.5 text-sm' : 'px-3 py-2 text-sm';

  return (
    <div className="relative inline-block">
      <button
        type="button"
        onClick={() => setOpen(!open)}
        disabled={loading !== null}
        className={`inline-flex items-center gap-2 ${sizeClass} bg-green-600 text-white rounded-lg hover:bg-green-700 transition-colors disabled:opacity-50 ${className}`}
        title={`Xuất ${label}`}
      >
        {loading ? (
          <Loader2 size={size === 'sm' ? 14 : 16} className="animate-spin" />
        ) : (
          <Download size={size === 'sm' ? 14 : 16} />
        )}
        {loading ? `Đang xuất ${loading.toUpperCase()}...` : `Xuất ${label}`}
      </button>

      {open && !loading && (
        <>
          <div className="fixed inset-0 z-10" onClick={() => setOpen(false)} />
          <div className="absolute right-0 mt-1 w-40 bg-white rounded-lg shadow-lg border border-gray-200 py-1 z-20">
            <button
              type="button"
              onClick={() => handleExport('excel')}
              className="w-full flex items-center gap-2 px-3 py-2 text-sm text-gray-700 hover:bg-gray-50"
            >
              <FileSpreadsheet size={16} className="text-green-600" />
              Xuất Excel
            </button>
            <button
              type="button"
              onClick={() => handleExport('pdf')}
              className="w-full flex items-center gap-2 px-3 py-2 text-sm text-gray-700 hover:bg-gray-50"
            >
              <FileText size={16} className="text-red-600" />
              Xuất PDF
            </button>
          </div>
        </>
      )}
    </div>
  );
};

export default ExportButton;
