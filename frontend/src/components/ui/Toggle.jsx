// Switch toggle matching the prototype `toggle` primitive. Uses role="switch"
// so it is announced correctly; the visual knob is driven by CSS logical
// properties so it flips for RTL automatically.
export function Toggle({ checked = false, onChange, label, disabled = false, id }) {
  return (
    <button
      type="button"
      role="switch"
      id={id}
      aria-checked={checked}
      aria-label={label}
      disabled={disabled}
      className={`ui-toggle${checked ? ' is-on' : ''}`}
      onClick={() => onChange?.(!checked)}
    >
      <span className="ui-toggle__knob" aria-hidden="true" />
    </button>
  )
}
