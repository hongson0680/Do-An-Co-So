import { Routes, Route, Navigate } from 'react-router-dom';
import { useAuthStore } from '../stores/auth.store';
import PrivateLayout from '../layouts/PrivateLayout';
import RoleRoute from '../components/layout/RoleRoute';
import { PERM } from '../utils/permissions';
import Login from '../features/auth/pages/Login';
import Dashboard from '../features/dashboard/pages/Dashboard';
import CustomerList from '../features/customers/pages/CustomerList';
import LoanList from '../features/loans/pages/LoanList';
import AppraisalList from '../features/appraisal/pages/AppraisalList';
import ApprovalList from '../features/approval/pages/ApprovalList';
import ContractList from '../features/contracts/pages/ContractList';
import PaymentList from '../features/payments/pages/PaymentList';
import ProductList from '../features/products/pages/ProductList';
import BranchList from '../features/branches/pages/BranchList';
import UserList from '../features/users/pages/UserList';
import AuditLogList from '../features/audit/pages/AuditLogList';
import NotificationList from '../features/notifications/pages/NotificationList';
import TaiSanDamBaoList from '../features/taisandambao/pages/TaiSanDamBaoList';
import TaiLieuVayList from '../features/tailieuvay/pages/TaiLieuVayList';

const ProtectedRoute = ({ children }: { children: React.ReactNode }) => {
  const { isAuthenticated } = useAuthStore();

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  return <>{children}</>;
};

const AppRoutes = () => {
  return (
    <Routes>
      {/* Public routes */}
      <Route path="/login" element={<Login />} />

      {/* Protected routes */}
      <Route
        path="/"
        element={
          <ProtectedRoute>
            <PrivateLayout />
          </ProtectedRoute>
        }
      >
        <Route index element={<Navigate to="/dashboard" replace />} />
        <Route path="dashboard" element={<RoleRoute perm={PERM.VIEW_DASHBOARD}><Dashboard /></RoleRoute>} />
        <Route path="customers" element={<RoleRoute perm={PERM.VIEW_CUSTOMERS}><CustomerList /></RoleRoute>} />
        <Route path="loans" element={<RoleRoute perm={PERM.VIEW_LOANS}><LoanList /></RoleRoute>} />
        <Route path="appraisal" element={<RoleRoute perm={PERM.VIEW_APPRAISAL}><AppraisalList /></RoleRoute>} />
        <Route path="approval" element={<RoleRoute perm={PERM.VIEW_APPROVAL}><ApprovalList /></RoleRoute>} />
        <Route path="contracts" element={<RoleRoute perm={PERM.VIEW_CONTRACTS}><ContractList /></RoleRoute>} />
        <Route path="payments" element={<RoleRoute perm={PERM.VIEW_PAYMENTS}><PaymentList /></RoleRoute>} />
        <Route path="products" element={<RoleRoute perm={PERM.VIEW_PRODUCTS}><ProductList /></RoleRoute>} />
        <Route path="branches" element={<RoleRoute perm={PERM.VIEW_BRANCHES}><BranchList /></RoleRoute>} />
        <Route path="users" element={<RoleRoute perm={PERM.MANAGE_USERS}><UserList /></RoleRoute>} />
        <Route path="audit-log" element={<RoleRoute perm={PERM.VIEW_AUDIT_LOG}><AuditLogList /></RoleRoute>} />
        <Route path="notifications" element={<RoleRoute perm={PERM.VIEW_NOTIFICATIONS}><NotificationList /></RoleRoute>} />
        <Route path="tai-san-dam-bao" element={<RoleRoute perm={PERM.VIEW_COLLATERAL}><TaiSanDamBaoList /></RoleRoute>} />
        <Route path="tai-lieu-vay" element={<RoleRoute perm={PERM.VIEW_DOCUMENTS}><TaiLieuVayList /></RoleRoute>} />
      </Route>

      {/* Catch all */}
      <Route path="*" element={<Navigate to="/dashboard" replace />} />
    </Routes>
  );
};

export default AppRoutes;
