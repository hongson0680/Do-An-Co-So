import { Routes, Route, Navigate } from 'react-router-dom';
import { useAuthStore } from '../stores/auth.store';
import PrivateLayout from '../layouts/PrivateLayout';
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
        <Route path="dashboard" element={<Dashboard />} />
        <Route path="customers" element={<CustomerList />} />
        <Route path="loans" element={<LoanList />} />
        <Route path="appraisal" element={<AppraisalList />} />
        <Route path="approval" element={<ApprovalList />} />
        <Route path="contracts" element={<ContractList />} />
        <Route path="payments" element={<PaymentList />} />
        <Route path="products" element={<ProductList />} />
        <Route path="branches" element={<BranchList />} />
        <Route path="users" element={<UserList />} />
        <Route path="audit-log" element={<AuditLogList />} />
        <Route path="notifications" element={<NotificationList />} />
        <Route path="tai-san-dam-bao" element={<TaiSanDamBaoList />} />
        <Route path="tai-lieu-vay" element={<TaiLieuVayList />} />
      </Route>

      {/* Catch all */}
      <Route path="*" element={<Navigate to="/dashboard" replace />} />
    </Routes>
  );
};

export default AppRoutes;
