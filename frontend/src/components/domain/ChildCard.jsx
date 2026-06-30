import { Link } from 'react-router-dom'
import { Avatar } from '../ui/Avatar'

// Parent "my children" card matching the prototype `childCard`. `stats` is a
// list of { label, value } shown as mini stat tiles — pass only the stats the
// backend actually returns (an unknown average is simply omitted, never zeroed).
export function ChildCard({ to, name, meta, stats = [] }) {
  const inner = (
    <>
      <div className="domain-child__head">
        <Avatar name={name} size={44} />
        <div>
          <h3 className="domain-card__title">{name}</h3>
          {meta && <div className="domain-card__meta">{meta}</div>}
        </div>
      </div>
      {stats.length > 0 && (
        <div className="domain-child__stats">
          {stats.map((s, i) => (
            <div key={i} className="domain-child__stat">
              <span className="domain-child__stat-value">{s.value}</span>
              <span className="domain-child__stat-label">{s.label}</span>
            </div>
          ))}
        </div>
      )}
    </>
  )
  return to ? (
    <Link to={to} className="domain-card domain-card--child">{inner}</Link>
  ) : (
    <div className="domain-card domain-card--child">{inner}</div>
  )
}
