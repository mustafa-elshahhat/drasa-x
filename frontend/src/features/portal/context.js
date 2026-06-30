import { useTranslation } from 'react-i18next'
import { useAuth } from '../auth/AuthContext'

// =============================================================================
// Shared portal context. Every role portal's page modules resolve the
// authenticated user id + active locale the same way the old dispatchers did.
// Pure hook (no JSX/component exports) so it never trips the fast-refresh rule.
// =============================================================================
export function usePortalContext() {
  const { i18n } = useTranslation()
  const { user } = useAuth()
  return { userId: user?.id, locale: i18n.language || 'en' }
}
