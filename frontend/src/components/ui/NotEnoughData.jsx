import { BarChart3 } from 'lucide-react'
import { useTranslation } from 'react-i18next'

// Honest "not enough data" state. Used inside a prototype widget container when
// the backend does not (yet) return the data a visual needs. We never fabricate
// numbers — the card frame still renders so the layout matches the prototype,
// but the value area shows this state instead of an invented figure.
export function NotEnoughData({ title, message, icon: Icon = BarChart3, compact = false }) {
  const { t } = useTranslation()
  return (
    <div className={`ui-state ui-state--not-enough${compact ? ' ui-state--compact' : ''}`} role="status">
      {Icon && <Icon className="ui-state__icon" size={compact ? 28 : 36} aria-hidden="true" />}
      <h3 className="ui-state__title">{title || t('states.notEnoughTitle', 'Not enough data yet')}</h3>
      <p className="ui-state__msg">
        {message || t('states.notEnoughBody', 'There is not enough data to show this yet.')}
      </p>
    </div>
  )
}
