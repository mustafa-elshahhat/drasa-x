import { Check, X } from 'lucide-react'
import { Ring } from '../viz/Ring'
import { Chip } from '../ui/Chip'

// Quiz result summary matching the prototype: a score ring, a pass/fail chip,
// up-to-three stat tiles, and a per-question review list. Every value is bound
// from the backend attempt result; tiles/stats are omitted when not provided.
//   stats:  [{ label, value }]
//   review: [{ label, correct, detail }]
export function QuizResult({ scorePercent, passed, passLabel, failLabel, stats = [], review = [], reviewTitle }) {
  return (
    <div className="quiz-result">
      <div className="quiz-result__head">
        <Ring value={scorePercent} caption={null} size={140} />
        {passed != null && (
          <Chip tone={passed ? 'success' : 'danger'}>{passed ? passLabel : failLabel}</Chip>
        )}
      </div>
      {stats.length > 0 && (
        <div className="quiz-result__stats">
          {stats.map((s, i) => (
            <div key={i} className="quiz-result__stat">
              <span className="quiz-result__stat-value">{s.value}</span>
              <span className="quiz-result__stat-label">{s.label}</span>
            </div>
          ))}
        </div>
      )}
      {review.length > 0 && (
        <div className="quiz-result__review">
          {reviewTitle && <h3 className="ui-card__title">{reviewTitle}</h3>}
          <ul className="quiz-result__review-list">
            {review.map((r, i) => (
              <li key={i} className="quiz-result__review-item">
                <span className={`quiz-result__mark${r.correct ? ' is-correct' : ' is-wrong'}`} aria-hidden="true">
                  {r.correct ? <Check size={14} /> : <X size={14} />}
                </span>
                <span className="quiz-result__review-main">
                  <span className="quiz-result__review-label">{r.label}</span>
                  {r.detail && <span className="quiz-result__review-detail">{r.detail}</span>}
                </span>
              </li>
            ))}
          </ul>
        </div>
      )}
    </div>
  )
}
