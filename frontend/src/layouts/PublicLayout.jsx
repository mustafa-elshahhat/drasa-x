import { Outlet } from 'react-router-dom'
import { PublicNavBar } from './PublicNavBar'
import { PublicFooter } from './PublicFooter'
import '../styles/public.css'

// Marketing / public layout (Phase 20 frontend unification). Wraps the anonymous
// marketing pages in a light-themed shell with its own nav + footer. Everything
// renders inside `.public-root` so the marketing styles are isolated from the
// dark authenticated portal. Auth (/login) and portal (/app/**) routes do NOT
// use this layout.
export function PublicLayout() {
  return (
    <div className="public-root">
      <PublicNavBar />
      <Outlet />
      <PublicFooter />
    </div>
  )
}
