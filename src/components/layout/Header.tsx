import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { LogOut, User, Menu, KeyRound } from 'lucide-react';
import { useAuthStore } from '../../stores/auth.store';
import NotificationBell from './NotificationBell';
import ChangePasswordModal from './ChangePasswordModal';

interface HeaderProps {
  onToggleSidebar: () => void;
}

const Header = ({ onToggleSidebar }: HeaderProps) => {
  const { user, logout } = useAuthStore();
  const [userMenuOpen, setUserMenuOpen] = useState(false);
  const [changePwOpen, setChangePwOpen] = useState(false);
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  const handleChangePassword = () => {
    setUserMenuOpen(false);
    setChangePwOpen(true);
  };

  return (
    <header className="h-16 bg-white border-b border-gray-200 flex items-center justify-between px-4">
      {/* Left */}
      <div className="flex items-center gap-4">
        <button
          type="button"
          onClick={onToggleSidebar}
          className="p-2 hover:bg-gray-100 rounded-lg lg:hidden"
          title="Mở/đóng menu"
          aria-label="Mở/đóng menu"
        >
          <Menu size={20} />
        </button>
        <h1 className="text-lg font-semibold text-gray-800">
          Hệ thống Quản lý Tín dụng
        </h1>
      </div>

      {/* Right */}
      <div className="flex items-center gap-3">
        {/* Notifications - thật, kết nối API */}
        <NotificationBell />

        {/* User Menu */}
        <div className="relative">
          <button
            type="button"
            onClick={() => setUserMenuOpen(!userMenuOpen)}
            className="flex items-center gap-2 p-2 hover:bg-gray-100 rounded-lg"
            aria-label="Menu người dùng"
          >
            <div className="w-8 h-8 bg-blue-600 rounded-full flex items-center justify-center">
              <User size={16} className="text-white" />
            </div>
            <div className="hidden md:block text-left">
              <p className="text-sm font-medium text-gray-800">
                {user?.hoTen || 'User'}
              </p>
              <p className="text-xs text-gray-500">{user?.tenVaiTro}</p>
            </div>
          </button>

          {userMenuOpen && (
            <>
              <div
                className="fixed inset-0 z-10"
                onClick={() => setUserMenuOpen(false)}
              />
              <div className="absolute right-0 mt-2 w-56 bg-white rounded-lg shadow-lg border border-gray-200 py-2 z-20">
                <div className="px-4 py-3 border-b">
                  <p className="font-medium text-gray-800">{user?.hoTen}</p>
                  <p className="text-sm text-gray-500">{user?.email}</p>
                </div>
                <button
                  type="button"
                  onClick={handleChangePassword}
                  className="w-full flex items-center gap-2 px-4 py-2 text-left text-gray-700 hover:bg-gray-50"
                >
                  <KeyRound size={16} />
                  Đổi mật khẩu
                </button>
                <button
                  type="button"
                  onClick={handleLogout}
                  className="w-full flex items-center gap-2 px-4 py-2 text-left text-red-600 hover:bg-red-50"
                >
                  <LogOut size={16} />
                  Đăng xuất
                </button>
              </div>
            </>
          )}
        </div>
      </div>

      {/* Change Password Modal */}
      <ChangePasswordModal
        open={changePwOpen}
        onClose={() => setChangePwOpen(false)}
      />
    </header>
  );
};

export default Header;
