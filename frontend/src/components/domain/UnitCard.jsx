import { Link } from 'react-router-dom'
import { ChevronRight, Layers } from 'lucide-react'
import { Chip } from '../ui/Chip'

// Unit row/card matching the prototype `unitCard`. Presentational; meta + status
// are optional and only rendered when supplied.
export function UnitCard({ to, title, meta, status, statusTone = 'muted', icon: Icon = Layers }) {
  const inner = (
    <>
      <span className="domain-row__icon" aria-hidden="true">
        {Icon && <Icon size={18} />}
      </span>
      <span className="domain-row__main">
        <span className="domain-row__title">{title}</span>
        {meta && <span className="domain-row__meta">{meta}</span>}
      </span>
      {status && <Chip tone={statusTone}>{status}</Chip>}
      {to && <ChevronRight className="domain-row__chev" size={18} aria-hidden="true" />}
    </>
  )
  return to ? (
    <Link to={to} className="domain-row">{inner}</Link>
  ) : (
    <div className="domain-row">{inner}</div>
  )
}
