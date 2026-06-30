import { NotEnoughData } from '../ui/NotEnoughData'

// Day-streak strip matching the prototype. Each day is `{ key, label, active }`.
// Falls back to an honest empty state when no day data is available — we never
// render a fake "0-day streak" as if it were measured.
export function StreakStrip({ days = [], notEnoughMessage }) {
  if (!days || days.length === 0) {
    return <NotEnoughData compact message={notEnoughMessage} />
  }
  return (
    <div className="viz-streak" role="list">
      {days.map((d, i) => (
        <div key={d.key ?? i} className="viz-streak__day" role="listitem">
          <span
            className={`viz-streak__cell${d.active ? ' is-active' : ''}`}
            aria-label={d.title}
            title={d.title}
          />
          {d.label && <span className="viz-streak__label">{d.label}</span>}
        </div>
      ))}
    </div>
  )
}
