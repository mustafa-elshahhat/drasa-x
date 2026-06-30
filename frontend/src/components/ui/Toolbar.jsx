import { Search, SlidersHorizontal } from 'lucide-react'
import { useTranslation } from 'react-i18next'

// List toolbar matching the prototype `toolbar()` primitive: an optional search
// box, a row of filter pills, and an optional right-aligned action slot. Search
// and filters are controlled by the caller; nothing here fabricates data.
export function Toolbar({ search, filters = [], onFilter, action }) {
  const { t } = useTranslation()
  return (
    <div className="ui-toolbar">
      {search && (
        <div className="ui-toolbar__search">
          <Search className="ui-toolbar__search-icon" size={16} aria-hidden="true" />
          <input
            type="search"
            className="ui-input"
            value={search.value}
            onChange={(e) => search.onChange(e.target.value)}
            placeholder={search.placeholder || t('actions.search', 'Search')}
            aria-label={search.label || search.placeholder || t('actions.search', 'Search')}
          />
        </div>
      )}
      {filters.length > 0 && (
        <div className="ui-toolbar__filters">
          {filters.map((f) => (
            <button
              key={f.id}
              type="button"
              className={`ui-toolbar__pill${f.active ? ' is-active' : ''}`}
              aria-pressed={!!f.active}
              onClick={() => onFilter?.(f)}
            >
              <SlidersHorizontal size={14} aria-hidden="true" />
              {f.label}
            </button>
          ))}
        </div>
      )}
      {action && <div className="ui-toolbar__action">{action}</div>}
    </div>
  )
}
