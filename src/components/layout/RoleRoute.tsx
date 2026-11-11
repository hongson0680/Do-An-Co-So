import { Navigate } from 'react-router-dom';
import { usePermission } from '../../hooks/usePermission';
import { type Perm } from '../../utils/permissions';

interface RoleRouteProps {
  perm: Perm;
  children: React.ReactNode;
}

/**
 * Chặn route theo quyền: user không có `perm` thì được chuyển về /dashboard.
 * Đây là lớp UX — backend vẫn là thẩm quyền cuối (trả 403 nếu gọi API vượt quyền).
 *
 * Dùng cho route gõ tay vào URL: menu đã ẩn theo quyền (Sidebar), nhưng nếu
 * user gõ thẳng /users khi không phải ADMIN thì UI hiện trang rồi mới ăn 403
 * → khó chịu. RoleRoute chặn sớm hơn, không để hiện trang sai quyền.
 */
const RoleRoute = ({ perm, children }: RoleRouteProps) => {
  const { can } = usePermission();

  if (!can(perm)) {
    return <Navigate to="/dashboard" replace />;
  }

  return <>{children}</>;
};

export default RoleRoute;
