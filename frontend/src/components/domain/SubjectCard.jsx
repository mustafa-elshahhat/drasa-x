import { Link } from 'react-router-dom'
import { BookOpen } from 'lucide-react'
import { Thumb } from './Thumb'
import { Chip } from '../ui/Chip'
import { ProgressBar } from '../viz/ProgressBar'

// Subject tile matching the prototype `subjThumbCard`. `metaChips` and
// `progress` are optional — only rendered when the caller has real values for
// them, so an unknown progress never shows as 0%.
export function SubjectCard({ to, name, icon = BookOpen, seed, metaChips = [], progress, progressLabel }) {
  const inner = (
    <>
      <Thumb seed={seed ?? name} icon={icon} />
      <div className="domain-card__body">
        <h3 className="domain-card__title">{name}</h3>
        {metaChips.length > 0 && (
          <div className="domain-card__chips">
            {metaChips.map((c, i) => (
              <Chip key={i} tone={c.tone}>{c.label}</Chip>
            ))}
          </div>
        )}
        {progress != null && <ProgressBar value={progress} label={progressLabel} />}
      </div>
    </>
  )
  return to ? (
    <Link to={to} className="domain-card domain-card--subject">{inner}</Link>
  ) : (
    <div className="domain-card domain-card--subject">{inner}</div>
  )
}
