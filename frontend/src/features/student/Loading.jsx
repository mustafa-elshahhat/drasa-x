import { useTranslation } from 'react-i18next'
import { Spinner } from '../../shared/ui'

/** Inline loading spinner with the localized "loading" label (student portal). */
export function Loading() {
  const { t } = useTranslation()
  return <Spinner label={t('states.loading')} />
}
