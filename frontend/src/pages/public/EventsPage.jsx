import { useTranslation } from 'react-i18next'
import { useDocumentTitle } from '../../app/useDocumentTitle'
import '../../styles/public-pages.css'

// Marketing "Events" page (reached from the homepage cards). Minimal, i18n-driven.
export default function EventsPage() {
  const { t } = useTranslation()
  useDocumentTitle({ titleKey: 'public.events.title' })
  return (
    <main className="public-page" role="main">
      <h1 className="public-page__title">{t('public.events.title')}</h1>
      <p className="public-page__body">{t('public.events.body')}</p>
    </main>
  )
}
