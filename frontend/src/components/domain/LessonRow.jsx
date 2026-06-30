import { Link } from 'react-router-dom'
import { Check, ChevronRight, PlayCircle } from 'lucide-react'
import { Chip } from '../ui/Chip'

// Lesson list row matching the prototype lesson list: a numbered/checked marker,
// title, optional meta, optional status chip, and a chevron. `completed` swaps
// the marker for a check.
export function LessonRow({ to, index, title, meta, completed = false, status, statusTone = 'muted' }) {
  const inner = (
    <>
      <span className={`domain-row__marker${completed ? ' is-done' : ''}`} aria-hidden="true">
        {completed ? <Check size={15} /> : index != null ? index : <PlayCircle size={16} />}
      </span>
      <span className="domain-row__main">
        <span className="domain-row__title">{title}</span>
        {meta && <span className="domain-row__meta">{meta}</span>}
      </span>
      {status && <Chip tone={statusTone}>{status}</Chip>}
      {to && <ChevronRight className="domain-row__chev" size={18} aria-hidden="true" />}
    </>
  )
  return to ? (
    <Link to={to} className="domain-row domain-row--lesson">{inner}</Link>
  ) : (
    <div className="domain-row domain-row--lesson">{inner}</div>
  )
}
