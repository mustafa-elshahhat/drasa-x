import { ProgressBar } from '../viz/ProgressBar'

// Usage meter list matching the prototype `usageScreen` bars. Each row is
// { label, value, limit, display }. When a row has no limit the ProgressBar
// degrades to a readout rather than a fake percentage.
export function UsageBars({ rows = [] }) {
  return (
    <div className="domain-usage">
      {rows.map((r, i) => (
        <ProgressBar
          key={r.key ?? i}
          label={r.label}
          value={r.value}
          max={r.limit}
          sublabel={r.display}
          showValue={r.limit != null}
        />
      ))}
    </div>
  )
}
