import { useTranslation } from 'react-i18next'
import { Languages } from 'lucide-react'
import { SUPPORTED_LANGUAGES } from '../i18n'

// Accessible language switcher (Phase 7 §14/§15). Changing the value updates
// i18next, which persists the choice and flips <html dir> for RTL/LTR.
export function LanguageSwitcher() {
  const { i18n, t } = useTranslation()
  const current = (i18n.resolvedLanguage || i18n.language || 'en').split('-')[0]

  return (
    <div className="ui-lang-switcher">
      <Languages size={16} aria-hidden="true" />
      <label htmlFor="lang-select" className="ui-visually-hidden">
        {t('language.switch', 'Language')}
      </label>
      <select
        id="lang-select"
        value={SUPPORTED_LANGUAGES.includes(current) ? current : 'en'}
        onChange={(e) => i18n.changeLanguage(e.target.value)}
        className="ui-lang-switcher__select"
      >
        <option value="en">{t('language.english', 'English')}</option>
        <option value="ar">{t('language.arabic', 'العربية')}</option>
      </select>
    </div>
  )
}
