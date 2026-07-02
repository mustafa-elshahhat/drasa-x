import { Navigate, useParams } from 'react-router-dom'

// Generic redirect for retired/duplicate route aliases (D3/D4). `to` is a
// target path template (e.g. '/app/student/materials/:materialId') supplied
// via the route registry's `props.to` (see routes.*.tsx) — any `:param`
// segments are substituted from the CURRENT route's matched params so a
// param-carrying alias lands on the equivalent canonical URL, not a literal
// unsubstituted placeholder.
export default function RedirectPage({ to }) {
  const params = useParams()
  const target = to.replace(/:([A-Za-z0-9_]+)/g, (_, key) => encodeURIComponent(params[key] ?? ''))
  return <Navigate to={target} replace />
}
