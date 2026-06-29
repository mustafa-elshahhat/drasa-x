// Skeleton placeholder for background loading (Phase 7 §12). Decorative, so it
// is hidden from assistive tech; pair with a role="status" label nearby.
export function Skeleton({ width = '100%', height = 16, radius = 6, className = '' }) {
  return (
    <span
      className={`ui-skeleton ${className}`}
      aria-hidden="true"
      style={{ width, height, borderRadius: radius }}
    />
  )
}

export function SkeletonText({ lines = 3 }) {
  return (
    <span className="ui-skeleton-group" aria-hidden="true">
      {Array.from({ length: lines }).map((_, i) => (
        <Skeleton key={i} width={i === lines - 1 ? '60%' : '100%'} />
      ))}
    </span>
  )
}
