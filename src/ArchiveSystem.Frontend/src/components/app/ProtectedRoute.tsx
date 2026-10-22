import { Navigate } from 'react-router-dom';
import { useEffect } from 'react';
import { useSessionStore } from '@/stores/useSessionStore';
import { useToastStore } from '@/stores/useToastStore';
import type { PermissionKey } from '@/types/archive';

interface ProtectedRouteProps {
  children: JSX.Element;
  allowedRoles?: string[];
  requiredPermission?: PermissionKey;
  requiredPermissions?: PermissionKey[];
  accessDeniedMessage?: string;
}

export const ProtectedRoute = ({
  children,
  allowedRoles,
  requiredPermission,
  requiredPermissions,
  accessDeniedMessage = 'شما مجوز دسترسی به این بخش را ندارید.',
}: ProtectedRouteProps) => {
  const isAuthenticated = useSessionStore((state) => state.isAuthenticated);
  const user = useSessionStore((state) => state.user);
  const hasPermission = useSessionStore((state) => state.hasPermission);
  const pushToast = useToastStore((state) => state.push);

  const roleDenied = Boolean(allowedRoles && !allowedRoles.some((allowedRole) =>
    user?.roles.some((role) => role.toLowerCase() === allowedRole.toLowerCase()),
  ));
  const permissionDenied = Boolean(
    (requiredPermission && !hasPermission(requiredPermission)) ||
      requiredPermissions?.some((permission) => !hasPermission(permission)),
  );

  useEffect(() => {
    if (isAuthenticated && (roleDenied || permissionDenied)) {
      pushToast(accessDeniedMessage, 'warning');
    }
  }, [accessDeniedMessage, isAuthenticated, permissionDenied, pushToast, roleDenied]);

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  if (roleDenied) {
    return <Navigate to="/" replace />;
  }

  if (permissionDenied) {
    return <Navigate to="/" replace />;
  }

  return children;
};
