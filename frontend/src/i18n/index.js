// =============================================================================
// i18n bootstrap (Phase 7 §14). English + Arabic, with RTL/LTR direction applied
// to <html> on every language change. The chosen language is persisted via a
// browser language-detector (localStorage key below) — a UI preference, never
// identity or domain data.
// =============================================================================
import i18n from 'i18next'
import { initReactI18next } from 'react-i18next'
import LanguageDetector from 'i18next-browser-languagedetector'
import en from './locales/en.js'
import ar from './locales/ar.js'
import { applyDocumentDirection } from './direction.js'

export const SUPPORTED_LANGUAGES = ['en', 'ar']
export const LANGUAGE_STORAGE_KEY = 'derasax_lang'

i18n
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    resources: { en, ar },
    fallbackLng: 'en',
    supportedLngs: SUPPORTED_LANGUAGES,
    nonExplicitSupportedLngs: true,
    interpolation: { escapeValue: false }, // React already escapes
    detection: {
      order: ['localStorage', 'navigator', 'htmlTag'],
      lookupLocalStorage: LANGUAGE_STORAGE_KEY,
      caches: ['localStorage'],
    },
  })

// Apply direction now and whenever the language changes.
applyDocumentDirection(i18n.resolvedLanguage || i18n.language)
i18n.on('languageChanged', (lng) => applyDocumentDirection(lng))

export default i18n
