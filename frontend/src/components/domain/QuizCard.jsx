import { Link } from 'react-router-dom'
import { FileQuestion } from 'lucide-react'
import { Chip } from '../ui/Chip'

// Quiz / assignment card matching the prototype `quizCard`. Presentational;
// status chip + meta are optional.
export function QuizCard({ to, title, meta, status, statusTone = 'muted', icon: Icon = FileQuestion, footer }) {
  const inner = (
    <>
      <div className="domain-card__head">
        <span className="domain-card__icon" aria-hidden="true">
          {Icon && <Icon size={20} />}
        </span>
        {status && <Chip tone={statusTone}>{status}</Chip>}
      </div>
      <h3 className="domain-card__title">{title}</h3>
      {meta && <div className="domain-card__meta">{meta}</div>}
      {footer && <div className="domain-card__footer">{footer}</div>}
    </>
  )
  return to ? (
    <Link to={to} className="domain-card domain-card--quiz">{inner}</Link>
  ) : (
    <div className="domain-card domain-card--quiz">{inner}</div>
  )
}
