import { NotEnoughData } from '../ui/NotEnoughData'

// Attendance-style heatmap matching the prototype calendar grid. Each cell is
// `{ key, label, level, title }` where level ∈ present|late|absent|excused|none.
// Renders honest empty state when there are no cells to plot.
export function Heatmap({ cells = [], legend = [], notEnoughMessage }) {
  if (!cells || cells.length === 0) {
    return <NotEnoughData compact message={notEnoughMessage} />
  }
  return (
    <div className="viz-heatmap">
      <div className="viz-heatmap__grid">
        {cells.map((c, i) => (
          <span
            key={c.key ?? i}
            className={`viz-heatmap__cell viz-heatmap__cell--${c.level || 'none'}`}
            title={c.title}
            aria-label={c.title}
          />
        ))}
      </div>
      {legend.length > 0 && (
        <ul className="viz-heatmap__legend">
          {legend.map((l) => (
            <li key={l.level}>
              <span className={`viz-heatmap__cell viz-heatmap__cell--${l.level}`} aria-hidden="true" />
              {l.label}
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
