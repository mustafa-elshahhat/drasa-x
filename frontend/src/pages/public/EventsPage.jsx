import { CalendarDays } from 'lucide-react'
import { PublicListingPage } from './PublicListingPage'

// Marketing "Events" page (reached from the homepage cards). i18n-driven; renders
// the prototype listing container with an honest empty state (no public feed yet).
export default function EventsPage() {
  return <PublicListingPage titleKey="public.events.title" bodyKey="public.events.body" icon={CalendarDays} />
}
