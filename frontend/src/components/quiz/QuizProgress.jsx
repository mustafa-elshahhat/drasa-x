// Quiz attempt progress bar matching the prototype ("Question X of N" + fill).
export function QuizProgress({ current, total, label }) {
  const pct = Number.isFinite(current) && Number.isFinite(total) && total > 0
    ? Math.max(0, Math.min(100, (current / total) * 100))
    : 0
  return (
    <div className="quiz-progress">
      {label && <div className="quiz-progress__label">{label}</div>}
      <div
        className="quiz-progress__track"
        role="progressbar"
        aria-valuenow={current}
        aria-valuemin={0}
        aria-valuemax={total}
        aria-label={label}
      >
        <div className="quiz-progress__fill" style={{ width: `${pct}%` }} />
      </div>
    </div>
  )
}
