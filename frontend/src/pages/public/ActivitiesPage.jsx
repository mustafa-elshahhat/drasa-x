import { useTranslation } from 'react-i18next'
import { useDocumentTitle } from '../../app/useDocumentTitle'
import '../../styles/public-pages.css'

// Marketing "Activities" page (reached from the homepage cards). Minimal, i18n-driven.
export default function ActivitiesPage() {
  const { t } = useTranslation()
  useDocumentTitle({ titleKey: 'public.activities.title' })
  return (
    <main className="public-page" role="main">
      <h1 className="public-page__title">{t('public.activities.title')}</h1>
      <p className="public-page__body">{t('public.activities.body')}</p>
    </main>
  )
}
