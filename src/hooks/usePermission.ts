import { useMemo } from 'react';
import { useAuthStore } from '../stores/auth.store';
import { hasPerm, type Perm } from '../utils/permissions';

/**
 * Hook phân quyền cho UI (lớp UX). Trả về `can(perm)` để ẩn/hiện nút,
 * và `role` hiện tại. Backend vẫn là lớp an toàn cuối (403 nếu vượt quyền).
 */
export function usePermission() {
  const role = useAuthStore((s) => s.user?.maVaiTro);

  const can = useMemo(
    () => (perm: Perm) => hasPerm(role, perm),
    [role],
  );

  return { role, can };
}

export default usePermission;
