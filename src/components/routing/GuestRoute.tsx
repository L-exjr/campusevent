import { Navigate, Outlet } from 'react-router-dom'
import LoadingState from '../shared/LoadingState'
import { useAuth } from '../../hooks/useAuth'
import { getHomeForRole } from '../../utils/permissions'

export default function GuestRoute() {
  const { user, isAuthenticated, loading } = useAuth()
  if (loading) return <LoadingState label="Loading" fullPage />
  if (isAuthenticated && user) {
    return <Navigate to={getHomeForRole(user.role)} replace />
  }
  return <Outlet />
}
