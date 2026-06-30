import { Sparkles } from 'lucide-react'
import { PublicListingPage } from './PublicListingPage'

// Marketing "Activities" page (reached from the homepage cards). i18n-driven;
// prototype listing container with an honest empty state (no public feed yet).
export default function ActivitiesPage() {
  return <PublicListingPage titleKey="public.activities.title" bodyKey="public.activities.body" icon={Sparkles} />
}
