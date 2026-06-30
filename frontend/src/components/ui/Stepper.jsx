import { Check } from 'lucide-react'

// Horizontal step indicator matching the prototype onboarding stepper. Purely
// presentational; the caller owns `current` (0-based) and step state.
//
//   <Stepper steps={[{ title }]} current={1} />
export function Stepper({ steps = [], current = 0 }) {
  return (
    <ol className="ui-stepper">
      {steps.map((step, idx) => {
        const state = idx < current ? 'done' : idx === current ? 'current' : 'todo'
        return (
          <li key={step.id ?? idx} className={`ui-stepper__step is-${state}`}>
            <span className="ui-stepper__marker" aria-hidden="true">
              {state === 'done' ? <Check size={14} /> : idx + 1}
            </span>
            <span className="ui-stepper__label">{step.title}</span>
            {idx < steps.length - 1 && <span className="ui-stepper__bar" aria-hidden="true" />}
          </li>
        )
      })}
    </ol>
  )
}
