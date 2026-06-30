import { useTranslation } from 'react-i18next'
import { Chip } from '../ui/Chip'
import { getField, formatField, resolveStatus, autoFields } from '../../features/student/studentUtils'

// Typed key/value detail grid replacing the old generic `DetailGrid` (which used
// raw API keys as labels). Each field is translated and formatted explicitly:
//   { key, labelKey | label, format? }
//   { key, labelKey, chip: map | fn }
//   { key, labelKey, render: (item) => node }
//   { label, value }   // pre-resolved
// When `fields` is omitted, fields are derived from the record with humanized
// (Title Case) labels — an honest fallback for untyped DTOs, never raw keys.
export function DetailList({ item, fields, locale, autoOptions }) {
  const { t } = useTranslation()
  const resolved = fields || autoFields(item, autoOptions)
  if (!resolved.length) return null
  return (
    <dl className="ui-detail-list">
      {resolved.map((f, i) => {
        const label = f.labelKey ? t(f.labelKey) : f.label
        let content
        if (f.render) {
          content = f.render(item)
        } else if (f.value !== undefined) {
          content = f.value
        } else {
          const value = f.accessor ? f.accessor(item) : getField(item, f.key)
          if (f.chip) {
            const desc = typeof f.chip === 'function' ? f.chip(value, item) : resolveStatus(value, f.chip)
            content = desc ? (
              <Chip tone={desc.tone || 'muted'}>{desc.labelKey ? t(desc.labelKey) : desc.label}</Chip>
            ) : (
              formatField(value, f.format, { locale })
            )
          } else {
            content = formatField(value, f.format, { locale })
          }
        }
        return (
          <div key={f.key ?? i} className="ui-detail-list__row">
            <dt>{label}</dt>
            <dd>{content}</dd>
          </div>
        )
      })}
    </dl>
  )
}
