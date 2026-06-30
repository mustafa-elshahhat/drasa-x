import { Chip } from '../ui/Chip'

// Operational-health list matching the prototype system health screen. Each
// service is { name, status, statusTone, meta }. Status text/tone come from the
// backend health payload.
export function ServiceStatusList({ services = [] }) {
  return (
    <ul className="domain-services">
      {services.map((s, i) => (
        <li key={s.key ?? i} className="domain-services__row">
          <span className={`domain-services__dot domain-services__dot--${s.statusTone || 'muted'}`} aria-hidden="true" />
          <span className="domain-services__name">{s.name}</span>
          {s.meta && <span className="domain-services__meta">{s.meta}</span>}
          {s.status && <Chip tone={s.statusTone || 'muted'}>{s.status}</Chip>}
        </li>
      ))}
    </ul>
  )
}
