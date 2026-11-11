import { Link, useLocation } from 'react-router-dom';
import {
  Home,
  Users,
  FileText,
  ClipboardCheck,
  CheckCircle,
  FileSignature,
  Receipt,
  CreditCard,
  Building2,
  User,
  History,
  Bell,
  Shield,
  ChevronLeft,
  ChevronRight,
} from 'lucide-react';
import { PERM, type Perm } from '../../utils/permissions';
import { usePermission } from '../../hooks/usePermission';

interface SidebarProps {
  collapsed: boolean;
  onToggle: () => void;
}

const menuItems: { path: string; label: string; icon: typeof Home; perm: Perm }[] = [
  { path: '/dashboard', label: 'Dashboard', icon: Home, perm: PERM.VIEW_DASHBOARD },
  { path: '/customers', label: 'Khách hàng', icon: Users, perm: PERM.VIEW_CUSTOMERS },
  { path: '/loans', label: 'Hồ sơ vay', icon: FileText, perm: PERM.VIEW_LOANS },
  { path: '/tai-san-dam-bao', label: 'Tài sản đảm bảo', icon: Shield, perm: PERM.VIEW_COLLATERAL },
  { path: '/tai-lieu-vay', label: 'Tài liệu vay', icon: FileText, perm: PERM.VIEW_DOCUMENTS },
  { path: '/appraisal', label: 'Thẩm định', icon: ClipboardCheck, perm: PERM.VIEW_APPRAISAL },
  { path: '/approval', label: 'Phê duyệt', icon: CheckCircle, perm: PERM.VIEW_APPROVAL },
  { path: '/contracts', label: 'Hợp đồng', icon: FileSignature, perm: PERM.VIEW_CONTRACTS },
  { path: '/payments', label: 'Thanh toán', icon: Receipt, perm: PERM.VIEW_PAYMENTS },
  { path: '/products', label: 'Sản phẩm vay', icon: CreditCard, perm: PERM.VIEW_PRODUCTS },
  { path: '/branches', label: 'Chi nhánh', icon: Building2, perm: PERM.VIEW_BRANCHES },
  { path: '/users', label: 'Người dùng', icon: User, perm: PERM.MANAGE_USERS },
  { path: '/notifications', label: 'Thông báo', icon: Bell, perm: PERM.VIEW_NOTIFICATIONS },
  { path: '/audit-log', label: 'Nhật ký', icon: History, perm: PERM.VIEW_AUDIT_LOG },
];

const Sidebar = ({ collapsed, onToggle }: SidebarProps) => {
  const location = useLocation();
  const { can } = usePermission();

  // Chỉ hiển thị mục mà vai hiện tại được phép (backend vẫn chặn nếu gọi tay)
  const visibleItems = menuItems.filter((item) => can(item.perm));

  return (
    <aside
      className={`
        fixed left-0 top-0 h-screen bg-white border-r border-gray-200
        transition-all duration-300 z-30 flex flex-col
        ${collapsed ? 'w-16' : 'w-64'}
      `}
    >
      {/* Logo */}
      <div className="h-16 flex items-center justify-center border-b border-gray-200">
        {!collapsed && (
          <span className="text-xl font-bold text-blue-600">QLTD Bank</span>
        )}
        {collapsed && (
          <span className="text-xl font-bold text-blue-600">Q</span>
        )}
      </div>

      {/* Menu */}
      <nav className="flex-1 py-4 overflow-y-auto">
        {visibleItems.map((item) => {
          const Icon = item.icon;
          const isActive = location.pathname === item.path;

          return (
            <Link
              key={item.path}
              to={item.path}
              className={`
                flex items-center gap-3 mx-2 px-3 py-3 rounded-lg mb-1
                transition-colors duration-200
                ${isActive
                  ? 'bg-blue-50 text-blue-600 font-medium'
                  : 'text-gray-600 hover:bg-gray-100'
                }
              `}
              title={collapsed ? item.label : undefined}
            >
              <Icon size={20} className="flex-shrink-0" />
              {!collapsed && <span>{item.label}</span>}
            </Link>
          );
        })}
      </nav>

      {/* Collapse Button */}
      <button
        onClick={onToggle}
        className="h-12 flex items-center justify-center border-t border-gray-200 text-gray-500 hover:bg-gray-50"
      >
        {collapsed ? <ChevronRight size={20} /> : <ChevronLeft size={20} />}
      </button>
    </aside>
  );
};

export default Sidebar;
