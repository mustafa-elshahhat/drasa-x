// Status / label chip matching the prototype `chip(text, tone)` primitive.
// Tone maps onto the `.ui-chip--*` classes already defined in app.css.
const TONE_CLASS = {
  muted: 'ui-chip--muted',
  brand: 'ui-chip--brand',
  accent: 'ui-chip--accent',
  success: 'ui-chip--success',
  danger: 'ui-chip--danger',
  warning: 'ui-chip--warning',
  info: 'ui-chip--info',
  purple: 'ui-chip--purple',
  orange: 'ui-chip--orange',
}

export function Chip({ tone = 'muted', icon: Icon, children, className = '', ...rest }) {
  const toneClass = TONE_CLASS[tone] || TONE_CLASS.muted
  return (
    <span className={`ui-chip ${toneClass} ${className}`.trim()} {...rest}>
      {Icon && <Icon size={13} aria-hidden="true" />}
      {children}
    </span>
  )
}
