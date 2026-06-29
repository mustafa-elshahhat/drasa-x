// Accessible loading spinner (Phase 7 §10/§12). Honors prefers-reduced-motion
// via CSS. Announces itself to assistive tech with role="status".
export function Spinner({ size = 24, label = 'Loading', inline = false }) {
  return (
    <span
      className={`ui-spinner${inline ? ' ui-spinner--inline' : ''}`}
      role="status"
      aria-live="polite"
    >
      <span
        className="ui-spinner__circle"
        style={{ width: size, height: size }}
        aria-hidden="true"
      />
      <span className="ui-visually-hidden">{label}</span>
    </span>
  )
}
