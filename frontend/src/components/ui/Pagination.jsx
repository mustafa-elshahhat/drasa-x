import { ChevronLeft, ChevronRight } from 'lucide-react'
import { useTranslation } from 'react-i18next'

// Accessible pagination (Phase 7 §10). Direction-agnostic: the chevrons are
// decorative; prev/next are labelled. The component is logical (prev/next), and
// CSS flips the visual order under RTL.
export function Pagination({ page, pageCount, onChange }) {
  const { t } = useTranslation()
  if (pageCount <= 1) return null
  return (
    <nav className="ui-pagination" aria-label={t('pagination.label', 'Pagination')}>
      <button
        type="button"
        className="ui-btn ui-btn--ghost"
        onClick={() => onChange(page - 1)}
        disabled={page <= 1}
        aria-label={t('pagination.previous', 'Previous page')}
      >
        <ChevronLeft size={16} aria-hidden="true" className="ui-pagination__chevron" />
      </button>
      <span className="ui-pagination__status" aria-current="page">
        {t('pagination.status', 'Page {{page}} of {{total}}', { page, total: pageCount })}
      </span>
      <button
        type="button"
        className="ui-btn ui-btn--ghost"
        onClick={() => onChange(page + 1)}
        disabled={page >= pageCount}
        aria-label={t('pagination.next', 'Next page')}
      >
        <ChevronRight size={16} aria-hidden="true" className="ui-pagination__chevron" />
      </button>
    </nav>
  )
}
