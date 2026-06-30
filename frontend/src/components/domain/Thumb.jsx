// Decorative gradient banner matching the prototype `thumb()` helper. The color
// is derived deterministically from a seed string so the same subject/lesson
// always gets the same hue. Purely decorative — carries no data meaning.
const PALETTE = ['#0c7288', '#8a38f5', '#ff6636', '#2f6fed', '#00a824', '#e0a000']

function pickColor(seed) {
  const s = String(seed || '')
  let h = 0
  for (let i = 0; i < s.length; i += 1) h = (h * 31 + s.charCodeAt(i)) >>> 0
  return PALETTE[h % PALETTE.length]
}

export function Thumb({ seed = '', icon: Icon, label, height = 120, className = '' }) {
  const color = pickColor(seed)
  return (
    <div className={`domain-thumb ${className}`.trim()} style={{ height, '--thumb-color': color }}>
      {Icon && <Icon size={34} aria-hidden="true" />}
      {label && <span className="domain-thumb__label">{label}</span>}
    </div>
  )
}
