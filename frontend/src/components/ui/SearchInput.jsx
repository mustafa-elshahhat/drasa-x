import { Search } from 'lucide-react'
import { useTranslation } from 'react-i18next'

// Search/filter input (Phase 7 §10). Labelled for screen readers; the icon is
// decorative.
export function SearchInput({ value, onChange, placeholder, id = 'ui-search', label }) {
  const { t } = useTranslation()
  const resolvedLabel = label || t('actions.search', 'Search')
  return (
    <div className="ui-search">
      <label htmlFor={id} className="ui-visually-hidden">
        {resolvedLabel}
      </label>
      <Search className="ui-search__icon" size={16} aria-hidden="true" />
      <input
        id={id}
        type="search"
        className="ui-search__input"
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder={placeholder || resolvedLabel}
      />
    </div>
  )
}
