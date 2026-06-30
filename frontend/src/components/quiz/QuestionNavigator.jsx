// Question jump grid matching the prototype. Answered questions are tinted;
// the current one is outlined. `items` is [{ index, answered }].
export function QuestionNavigator({ items = [], current, onJump, label }) {
  return (
    <nav className="quiz-nav" aria-label={label}>
      {items.map((it) => (
        <button
          key={it.index}
          type="button"
          className={`quiz-nav__cell${it.answered ? ' is-answered' : ''}${it.index === current ? ' is-current' : ''}`}
          aria-current={it.index === current ? 'true' : undefined}
          onClick={() => onJump?.(it.index)}
        >
          {it.index + 1}
        </button>
      ))}
    </nav>
  )
}
