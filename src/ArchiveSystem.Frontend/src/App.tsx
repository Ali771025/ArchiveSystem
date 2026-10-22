import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { ArchiveLayout } from '@/components/app/ArchiveLayout';
import { PhysicalMovementModal } from '@/components/movement-logs/PhysicalMovementModal';
import { ProtectedRoute } from '@/components/app/ProtectedRoute';
import { CreateOwnerModal } from '@/components/owners/CreateOwnerModal';
import { CaseDynamicSearch } from '@/components/features/CaseDynamicSearch';
import { AccessControlPage } from '@/pages/AccessControlPage';
import { AdvancedDocumentSearchPage } from '@/pages/AdvancedDocumentSearchPage';
import { ArchiveWorkspacePage } from '@/pages/ArchiveWorkspacePage';
import { CaseLoanManagementPage } from '@/pages/CaseLoanManagement';
import { DynamicFieldsAdminPage } from '@/pages/DynamicFieldsAdminPage';
import { ForgotPasswordPage } from '@/pages/ForgotPasswordPage';
import { LoginPage } from '@/pages/LoginPage';
import { NotFoundPage } from '@/pages/NotFoundPage';
import { RegisterPage } from '@/pages/RegisterPage';
import { ReportsPage } from '@/pages/ReportsPage';
import { TaxpayerManagementPage } from '@/pages/TaxpayerManagementPage';
import { UserManagementPage } from '@/pages/UserManagementPage';
import { useOwnerModalStore } from '@/stores/useOwnerModalStore';

function App() {
  const isCreateModalOpen = useOwnerModalStore((state) => state.isCreateModalOpen);
  const closeCreateModal = useOwnerModalStore((state) => state.closeCreateModal);

  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />
        <Route path="/forgot-password" element={<ForgotPasswordPage />} />
        <Route
          path="/"
          element={
            <ProtectedRoute>
              <ArchiveLayout />
            </ProtectedRoute>
          }
        >
          <Route index element={<ArchiveWorkspacePage />} />
          <Route path="cases/:caseId" element={<ArchiveWorkspacePage />} />
          <Route path="search/documents" element={<AdvancedDocumentSearchPage />} />
          <Route path="files/advanced-search" element={<Navigate to="/search/documents" replace />} />
          <Route path="dossiers/advanced-search" element={<CaseDynamicSearch />} />
          <Route
            path="admin/dynamic-fields"
            element={<ProtectedRoute requiredPermissions={['tabs.settings.view', 'dynamicfields.manage']}><DynamicFieldsAdminPage /></ProtectedRoute>}
          />
          <Route
            path="admin/access-control"
            element={<ProtectedRoute requiredPermissions={['tabs.accesscontrol.view', 'roles.manage']}><AccessControlPage /></ProtectedRoute>}
          />
          <Route
            path="admin/case-loans"
            element={<ProtectedRoute requiredPermissions={['tabs.caseloans.view', 'caseloans.manage']}><CaseLoanManagementPage /></ProtectedRoute>}
          />
          <Route
            path="admin/taxpayers"
            element={<ProtectedRoute requiredPermissions={['tabs.taxpayers.view', 'taxpayers.manage']}><TaxpayerManagementPage /></ProtectedRoute>}
          />
          <Route
            path="admin/users"
            element={<ProtectedRoute requiredPermissions={['tabs.users.view', 'users.manage']}><UserManagementPage /></ProtectedRoute>}
          />
          <Route
            path="reports"
            element={
              <ProtectedRoute
                allowedRoles={['Admin', 'Auditor', 'Superuser']}
                requiredPermission="tabs.reports.view"
                accessDeniedMessage="شما مجوز مشاهده گزارش‌های امنیتی و ممیزی را ندارید."
              >
                <ReportsPage />
              </ProtectedRoute>
            }
          />
        </Route>
        <Route path="*" element={<NotFoundPage />} />
        <Route path="/app" element={<Navigate to="/" replace />} />
      </Routes>

      {/* Keep modal host outside Routes so it remains mounted globally */}
      <CreateOwnerModal isOpen={isCreateModalOpen} onClose={closeCreateModal} />
      <PhysicalMovementModal />
    </BrowserRouter>
  );
}

export default App;
