import { useEffect } from 'react'
import { useTranslation } from 'react-i18next'

// Updates document.title for screen-reader page-change announcements (Phase 7
// §15). Pass an already-resolved title, or a titleKey to translate.
export function useDocumentTitle({ title, titleKey }) {
  const { t } = useTranslation()
  useEffect(() => {
    const resolved = title || (titleKey ? t(titleKey) : null)
    document.title = resolved ? `${resolved} · DerasaX` : 'DerasaX'
  }, [title, titleKey, t])
}
