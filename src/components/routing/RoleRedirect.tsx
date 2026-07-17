import { Navigate } from 'react-router-dom'
import LoadingState from '../shared/LoadingState'
import { useAuth } from '../../hooks/useAuth'
import { getHomeForRole } from '../../utils/permissions'

export default function RoleRedirect() {
  const { user, loading } = useAuth()
  if (loading) return <LoadingState label="Loading" fullPage />
  return <Navigate to={user ? getHomeForRole(user.role) : '/login'} replace />
}
