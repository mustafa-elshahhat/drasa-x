import { useDocumentTitle } from '../../app/useDocumentTitle'
import { HeroSection } from './sections/HeroSection'
import { ThreeCards } from './sections/ThreeCards'

// Unified public marketing homepage (replaces the placeholder LandingPage).
// Nav + footer are supplied by PublicLayout; this page provides the <main>
// landmark and the hero/cards content.
export default function HomePage() {
  useDocumentTitle({ titleKey: 'app.name' })
  return (
    <main className="public-home" role="main">
      <HeroSection />
      <ThreeCards />
    </main>
  )
}
