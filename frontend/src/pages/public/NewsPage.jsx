import { Newspaper } from 'lucide-react'
import { PublicListingPage } from './PublicListingPage'

// Marketing "News" page (reached from the homepage cards). i18n-driven; prototype
// listing container with an honest empty state (no public feed yet).
export default function NewsPage() {
  return <PublicListingPage titleKey="public.news.title" bodyKey="public.news.body" icon={Newspaper} />
}
