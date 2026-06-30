import { NotEnoughData } from '../ui/NotEnoughData'

// SVG donut gauge matching the prototype `ring()` primitive. When the value is
// not a finite number it falls back to an honest "not enough data" state rather
// than drawing a 0% ring that reads as a real measurement.
export function Ring({ value, max = 100, size = 140, stroke = 12, caption, centerLabel, notEnoughMessage }) {
  const v = value === null || value === undefined || value === '' ? NaN : Number(value)
  const m = Number(max)
  if (!Number.isFinite(v) || !Number.isFinite(m) || m <= 0) {
    return <NotEnoughData compact message={notEnoughMessage} />
  }
  const pct = Math.max(0, Math.min(100, (v / m) * 100))
  const r = (size - stroke) / 2
  const circumference = 2 * Math.PI * r
  const offset = circumference * (1 - pct / 100)
  return (
    <div className="viz-ring">
      <svg width={size} height={size} viewBox={`0 0 ${size} ${size}`} role="img" aria-label={caption || `${Math.round(pct)}%`}>
        <circle cx={size / 2} cy={size / 2} r={r} fill="none" stroke="var(--border-2)" strokeWidth={stroke} />
        <circle
          cx={size / 2}
          cy={size / 2}
          r={r}
          fill="none"
          stroke="var(--accent)"
          strokeWidth={stroke}
          strokeLinecap="round"
          strokeDasharray={circumference}
          strokeDashoffset={offset}
          transform={`rotate(-90 ${size / 2} ${size / 2})`}
          className="viz-ring__arc"
        />
        <text x="50%" y="50%" dominantBaseline="central" textAnchor="middle" className="viz-ring__text">
          {centerLabel ?? `${Math.round(pct)}%`}
        </text>
      </svg>
      {caption && <div className="viz-ring__caption">{caption}</div>}
    </div>
  )
}
