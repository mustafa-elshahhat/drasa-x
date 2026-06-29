import { Navigate } from 'react-router-dom'
import { useAuth } from '../../features/auth/AuthContext'
import { homeRouteForRole } from '../../features/auth/roles'

// /app entry — routes the authenticated user to their role's landing area.
// If the role is unknown, falls back to the profile page (always permitted).
export default function AppHome() {
  const { role } = useAuth()
  const target = homeRouteForRole(role)
  return <Navigate to={target === '/app' ? '/app/profile' : target} replace />
}
