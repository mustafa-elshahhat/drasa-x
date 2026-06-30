// Initials avatar (DerasaX visual system). Decorative by default (aria-hidden);
// pass `alt` when the avatar conveys information on its own. Size is data-driven
// so it stays an inline style; color defaults to the current --accent.
function initials(name) {
  return (name || '?')
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0])
    .join('')
    .toUpperCase()
}

export function Avatar({ name, size = 38, color, src, alt }) {
  const style = { width: size, height: size, fontSize: Math.round(size * 0.38) }
  if (color) style.background = color
  return (
    <span className="ui-avatar" style={style} aria-hidden={alt ? undefined : true}>
      {src ? <img src={src} alt={alt || ''} /> : initials(name)}
    </span>
  )
}
