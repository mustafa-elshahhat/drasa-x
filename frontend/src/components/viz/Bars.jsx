import { NotEnoughData } from '../ui/NotEnoughData'

// Simple column chart matching the prototype `bars()` primitive. Renders only
// the data points that carry a finite numeric value; if none do, it shows an
// honest "not enough data" state instead of an empty axis.
export function Bars({ series = [], max, height = 150, notEnoughMessage }) {
  const points = (series || []).filter(
    (d) => d?.value !== null && d?.value !== undefined && d?.value !== '' && Number.isFinite(Number(d.value))
  )
  if (points.length === 0) {
    return <NotEnoughData compact message={notEnoughMessage} />
  }
  const peak = Number.isFinite(Number(max)) && Number(max) > 0
    ? Number(max)
    : Math.max(...points.map((d) => Number(d.value)), 1)
  return (
    <div className="viz-bars" style={{ height }}>
      {points.map((d, i) => {
        const h = Math.max(2, (Number(d.value) / peak) * 100)
        return (
          <div key={d.label ?? i} className="viz-bars__col">
            <span className="viz-bars__value">{d.display ?? d.value}</span>
            <div className="viz-bars__bar" style={{ height: `${h}%` }} aria-hidden="true" />
            <span className="viz-bars__label">{d.label}</span>
          </div>
        )
      })}
    </div>
  )
}
