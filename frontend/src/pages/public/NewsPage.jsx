import { useTranslation } from 'react-i18next'
import { useDocumentTitle } from '../../app/useDocumentTitle'
import '../../styles/public-pages.css'

// Marketing "News" page (reached from the homepage cards). Minimal, i18n-driven.
export default function NewsPage() {
  const { t } = useTranslation()
  useDocumentTitle({ titleKey: 'public.news.title' })
  return (
    <main className="public-page" role="main">
      <h1 className="public-page__title">{t('public.news.title')}</h1>
      <p className="public-page__body">{t('public.news.body')}</p>
    </main>
  )
}
