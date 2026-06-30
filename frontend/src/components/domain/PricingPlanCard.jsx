import { Check } from 'lucide-react'
import { Chip } from '../ui/Chip'
import { Button } from '../ui/Button'

// Subscription plan card matching the prototype pricing cards. `current`
// highlights the active plan. Price/features come from the backend plan record;
// nothing is fabricated when a field is missing (it is simply omitted).
export function PricingPlanCard({ name, price, period, features = [], current = false, currentLabel, cta, onSelect }) {
  return (
    <div className={`domain-plan${current ? ' is-current' : ''}`}>
      <div className="domain-plan__head">
        <h3 className="domain-plan__name">{name}</h3>
        {current && <Chip tone="brand">{currentLabel}</Chip>}
      </div>
      {price != null && price !== '' && (
        <div className="domain-plan__price">
          {price}
          {period && <span className="domain-plan__period"> / {period}</span>}
        </div>
      )}
      {features.length > 0 && (
        <ul className="domain-plan__features">
          {features.map((f, i) => (
            <li key={i}>
              <Check size={15} aria-hidden="true" /> {f}
            </li>
          ))}
        </ul>
      )}
      {cta && (
        <Button variant={current ? 'secondary' : 'primary'} onClick={onSelect} disabled={current}>
          {cta}
        </Button>
      )}
    </div>
  )
}
