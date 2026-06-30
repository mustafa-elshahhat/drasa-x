import { Link } from 'react-router-dom'

// KPI metric card matching the prototype `metric()` primitive: a colored
// icon + label row, a large value, and a muted sub-line, with an accent
// inline-start bar. Renders as a link when `to` is provided (hover-lift comes
// from the `.student-dashboard > .ui-card` rule). Purely presentational — every
// value passed in must come from real data or an honest placeholder.
export function Metric({ to, icon: Icon, label, value, sub, accent }) {
  const style = accent ? { '--metric-accent': accent } : undefined
  const body = (
    <>
      <span className="ui-metric__head">
        {Icon && <Icon size={18} aria-hidden="true" />}
        <span>{label}</span>
      </span>
      {value != null && value !== '' && <strong className="ui-metric__value">{value}</strong>}
      {sub != null && sub !== '' && <span className="ui-metric__sub">{sub}</span>}
    </>
  )
  if (to) {
    return (
      <Link to={to} className="ui-card ui-metric" style={style}>
        {body}
      </Link>
    )
  }
  return (
    <div className="ui-card ui-metric" style={style}>
      {body}
    </div>
  )
}
