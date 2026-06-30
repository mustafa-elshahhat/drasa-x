import { Clock } from 'lucide-react'

// Countdown pill matching the prototype quiz timer. This is purely a display of
// a value the caller computes; it is only rendered when the quiz actually has a
// time limit (otherwise the caller omits it — no fake clock).
function fmt(totalSeconds) {
  const s = Math.max(0, Math.floor(Number(totalSeconds) || 0))
  const mm = Math.floor(s / 60)
  const ss = s % 60
  return `${String(mm).padStart(2, '0')}:${String(ss).padStart(2, '0')}`
}

export function Timer({ secondsLeft, label }) {
  return (
    <span className="quiz-timer" role="timer" aria-label={label}>
      <Clock size={15} aria-hidden="true" />
      {fmt(secondsLeft)}
    </span>
  )
}
