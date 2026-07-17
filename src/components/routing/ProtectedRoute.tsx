import { Navigate, Outlet, useLocation } from 'react-router-dom'
import LoadingState from '../shared/LoadingState'
import { useAuth } from '../../hooks/useAuth'
import type { Role } from '../../types'
import { canAccessRole } from '../../utils/permissions'

interface ProtectedRouteProps {
  allowedRoles: Role[]
}

export default function ProtectedRoute({ allowedRoles }: ProtectedRouteProps) {
  const { user, isAuthenticated, loading } = useAuth()
  const location = useLocation()

  if (loading) return <LoadingState label="Restoring your session" fullPage />
  if (!isAuthenticated || !user) {
    return <Navigate to="/login" replace state={{ from: location.pathname }} />
  }
  if (!canAccessRole(user.role, allowedRoles)) {
    return <Navigate to="/unauthorized" replace />
  }
  return <Outlet />
}
