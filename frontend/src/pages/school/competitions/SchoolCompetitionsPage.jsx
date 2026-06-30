import { useTranslation } from 'react-i18next'
import { Alert } from '../../../shared/ui'
import { ErrorState } from '../../../shared/feedback'
import { Head, List } from '../../../features/school/components'
import { useSchoolQuery } from '../../../features/school/helpers'
import { schoolApi } from '../../../features/school/schoolApi'
import { queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function CompetitionsPage({ userId, locale }) {
  const { t } = useTranslation()
  const query = useSchoolQuery(queryKeys.school.competitions(userId), (s) => schoolApi.competitions(s))
  return (<><Head view="competitions" /><Alert title={t('school.notes.readOnlyTitle')}>{t('school.notes.competitions')}</Alert><List query={query} empty={t('school.empty.competitions')} locale={locale} /></>)
}

export default function SchoolCompetitionsPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <CompetitionsPage userId={userId} locale={locale} {...props} />
}
