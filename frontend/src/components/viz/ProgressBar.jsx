// Thin progress bar matching the prototype `progress()` primitive. Degrades
// gracefully: when `value` is not a finite number the track renders empty with a
// "—" readout rather than inventing a percentage.
function toPct(value, max) {
  if (value === null || value === undefined || value === '') return null
  const v = Number(value)
  const m = Number(max)
  if (!Number.isFinite(v) || !Number.isFinite(m) || m <= 0) return null
  return Math.max(0, Math.min(100, (v / m) * 100))
}

export function ProgressBar({ value, max = 100, label, sublabel, showValue = true, ariaLabel }) {
  const pct = toPct(value, max)
  const readout = pct == null ? '—' : `${Math.round(pct)}%`
  return (
    <div className="viz-bar">
      {(label || showValue) && (
        <div className="viz-bar__head">
          {label && <span className="viz-bar__label">{label}</span>}
          {showValue && <span className="viz-bar__value">{readout}</span>}
        </div>
      )}
      <div
        className="viz-bar__track"
        role="progressbar"
        aria-valuenow={pct == null ? undefined : Math.round(pct)}
        aria-valuemin={0}
        aria-valuemax={100}
        aria-label={ariaLabel || label}
      >
        <div className="viz-bar__fill" style={{ width: `${pct ?? 0}%` }} />
      </div>
      {sublabel && <div className="viz-bar__sub">{sublabel}</div>}
    </div>
  )
}
