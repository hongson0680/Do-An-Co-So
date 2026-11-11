import { useState } from 'react';
import { Lock, Eye, EyeOff, Loader2, CheckCircle2 } from 'lucide-react';
import { authApi } from '../../api/auth.api';
import Modal from '../common/Modal';
import Button from '../common/Button';
import Input from '../common/Input';

interface ChangePasswordModalProps {
  open: boolean;
  onClose: () => void;
  onSuccess?: () => void;
}

const ChangePasswordModal = ({ open, onClose, onSuccess }: ChangePasswordModalProps) => {
  const [matKhauCu, setMatKhauCu] = useState('');
  const [matKhauMoi, setMatKhauMoi] = useState('');
  const [xacNhanMatKhau, setXacNhanMatKhau] = useState('');
  const [showCu, setShowCu] = useState(false);
  const [showMoi, setShowMoi] = useState(false);
  const [showXacNhan, setShowXacNhan] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState(false);

  const reset = () => {
    setMatKhauCu('');
    setMatKhauMoi('');
    setXacNhanMatKhau('');
    setError('');
    setSuccess(false);
    setShowCu(false);
    setShowMoi(false);
    setShowXacNhan(false);
  };

  const handleClose = () => {
    if (saving) return;
    reset();
    onClose();
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    if (!matKhauCu || !matKhauMoi || !xacNhanMatKhau) {
      setError('Vui lòng điền đầy đủ thông tin');
      return;
    }
    if (matKhauMoi.length < 6) {
      setError('Mật khẩu mới phải có ít nhất 6 ký tự');
      return;
    }
    if (matKhauMoi !== xacNhanMatKhau) {
      setError('Mật khẩu xác nhận không khớp');
      return;
    }
    if (matKhauCu === matKhauMoi) {
      setError('Mật khẩu mới phải khác mật khẩu hiện tại');
      return;
    }

    setSaving(true);
    try {
      await authApi.changePassword({ matKhauCu, matKhauMoi, xacNhanMatKhau });
      setSuccess(true);
      // Reset form
      setMatKhauCu('');
      setMatKhauMoi('');
      setXacNhanMatKhau('');
      onSuccess?.();
      // Tự đóng sau 2 giây
      setTimeout(() => {
        reset();
        onClose();
      }, 2000);
    } catch (err: any) {
      const msg =
        err.response?.data?.message ||
        err.response?.data?.errors?.MatKhauMoi?.[0] ||
        err.response?.data?.errors?.MatKhauCu?.[0] ||
        'Đổi mật khẩu thất bại. Vui lòng thử lại.';
      setError(msg);
    } finally {
      setSaving(false);
    }
  };

  // Tính độ mạnh mật khẩu
  const passwordStrength = (() => {
    if (!matKhauMoi) return { level: 0, label: '', color: 'gray' };
    let score = 0;
    if (matKhauMoi.length >= 6) score++;
    if (matKhauMoi.length >= 10) score++;
    if (/[A-Z]/.test(matKhauMoi)) score++;
    if (/[0-9]/.test(matKhauMoi)) score++;
    if (/[^A-Za-z0-9]/.test(matKhauMoi)) score++;
    if (score <= 1) return { level: 1, label: 'Yếu', color: 'red' };
    if (score <= 3) return { level: 2, label: 'Trung bình', color: 'yellow' };
    return { level: 3, label: 'Mạnh', color: 'green' };
  })();

  return (
    <Modal
      open={open}
      onClose={handleClose}
      title="Đổi mật khẩu"
      size="md"
    >
      <form onSubmit={handleSubmit} className="space-y-4">
        {success && (
          <div className="bg-green-50 border border-green-200 text-green-700 px-4 py-3 rounded-lg flex items-center gap-2 text-sm">
            <CheckCircle2 size={18} />
            <span>Đổi mật khẩu thành công!</span>
          </div>
        )}

        {error && (
          <div className="bg-red-50 border border-red-200 text-red-600 px-4 py-3 rounded-lg text-sm">
            {error}
          </div>
        )}

        {/* Mật khẩu hiện tại */}
        <div className="relative">
          <Input
            label="Mật khẩu hiện tại"
            type={showCu ? 'text' : 'password'}
            value={matKhauCu}
            onChange={(e) => setMatKhauCu(e.target.value)}
            placeholder="Nhập mật khẩu hiện tại"
            leftIcon={<Lock size={18} />}
            disabled={saving || success}
            required
          />
          <button
            type="button"
            onClick={() => setShowCu(!showCu)}
            className="absolute right-3 top-9 text-gray-400 hover:text-gray-600"
            tabIndex={-1}
            aria-label={showCu ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'}
          >
            {showCu ? <EyeOff size={18} /> : <Eye size={18} />}
          </button>
        </div>

        {/* Mật khẩu mới */}
        <div className="relative">
          <Input
            label="Mật khẩu mới"
            type={showMoi ? 'text' : 'password'}
            value={matKhauMoi}
            onChange={(e) => setMatKhauMoi(e.target.value)}
            placeholder="Tối thiểu 6 ký tự"
            leftIcon={<Lock size={18} />}
            disabled={saving || success}
            required
          />
          <button
            type="button"
            onClick={() => setShowMoi(!showMoi)}
            className="absolute right-3 top-9 text-gray-400 hover:text-gray-600"
            tabIndex={-1}
            aria-label={showMoi ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'}
          >
            {showMoi ? <EyeOff size={18} /> : <Eye size={18} />}
          </button>
        </div>

        {/* Strength meter */}
        {matKhauMoi && (
          <div className="space-y-1">
            <div className="flex gap-1">
              {[1, 2, 3].map((i) => (
                <div
                  key={i}
                  className={`h-1 flex-1 rounded ${
                    i <= passwordStrength.level
                      ? passwordStrength.color === 'red'
                        ? 'bg-red-500'
                        : passwordStrength.color === 'yellow'
                        ? 'bg-yellow-500'
                        : 'bg-green-500'
                      : 'bg-gray-200'
                  }`}
                />
              ))}
            </div>
            <p className={`text-xs ${
              passwordStrength.color === 'red' ? 'text-red-600' :
              passwordStrength.color === 'yellow' ? 'text-yellow-600' :
              'text-green-600'
            }`}>
              Độ mạnh: {passwordStrength.label}
            </p>
          </div>
        )}

        {/* Xác nhận mật khẩu */}
        <div className="relative">
          <Input
            label="Xác nhận mật khẩu mới"
            type={showXacNhan ? 'text' : 'password'}
            value={xacNhanMatKhau}
            onChange={(e) => setXacNhanMatKhau(e.target.value)}
            placeholder="Nhập lại mật khẩu mới"
            leftIcon={<Lock size={18} />}
            disabled={saving || success}
            required
          />
          <button
            type="button"
            onClick={() => setShowXacNhan(!showXacNhan)}
            className="absolute right-3 top-9 text-gray-400 hover:text-gray-600"
            tabIndex={-1}
            aria-label={showXacNhan ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'}
          >
            {showXacNhan ? <EyeOff size={18} /> : <Eye size={18} />}
          </button>
        </div>

        {/* Hint khi xác nhận khớp */}
        {matKhauMoi && xacNhanMatKhau && matKhauMoi === xacNhanMatKhau && (
          <p className="text-xs text-green-600 flex items-center gap-1">
            <CheckCircle2 size={14} />
            Mật khẩu xác nhận khớp
          </p>
        )}

        {/* Buttons */}
        <div className="flex justify-end gap-2 pt-2">
          <Button
            type="button"
            variant="outline"
            onClick={handleClose}
            disabled={saving}
          >
            Hủy
          </Button>
          <Button
            type="submit"
            disabled={saving || success}
            leftIcon={saving ? <Loader2 size={16} className="animate-spin" /> : <Lock size={16} />}
          >
            {saving ? 'Đang xử lý...' : 'Đổi mật khẩu'}
          </Button>
        </div>
      </form>
    </Modal>
  );
};

export default ChangePasswordModal;
