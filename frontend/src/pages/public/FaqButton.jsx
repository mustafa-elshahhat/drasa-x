import { useTranslation } from 'react-i18next'
import '../../styles/public-faq.css'

// Lightweight marketing FAQ call-to-action (the original public app shipped a
// label-only button with no modal; preserved here, internationalized).
export function FaqButton() {
  const { t } = useTranslation()
  return (
    <div className="public-faq">
      <button type="button" className="public-faq__btn">
        {t('public.faq.button')}
      </button>
    </div>
  )
}
