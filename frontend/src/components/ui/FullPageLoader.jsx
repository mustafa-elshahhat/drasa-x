// Full-viewport loading state used while the session is resolving (Phase 7
// §4/§12). role="status" announces the wait to assistive tech.
export function FullPageLoader({ label = 'Loading' }) {
  return (
    <div className="ui-fullpage-loader" role="status" aria-live="polite">
      <span className="ui-spinner__circle ui-spinner__circle--lg" aria-hidden="true" />
      <p className="ui-fullpage-loader__label">{label}</p>
    </div>
  )
}
