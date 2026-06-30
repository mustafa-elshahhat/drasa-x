import { useTranslation } from 'react-i18next'
import { Alert } from '../../../shared/ui'
import { ErrorState } from '../../../shared/feedback'
import { Head, List } from '../../../features/school/components'
import { useSchoolQuery } from '../../../features/school/helpers'
import { schoolApi } from '../../../features/school/schoolApi'
import { queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function CommunitiesPage({ userId, locale }) {
  const { t } = useTranslation()
  const query = useSchoolQuery(queryKeys.school.communities(userId), (s) => schoolApi.communities(s))
  return (<><Head view="communities" /><Alert title={t('school.notes.readOnlyTitle')}>{t('school.notes.communities')}</Alert><List query={query} empty={t('school.empty.communities')} locale={locale} /></>)
}

export default function SchoolCommunitiesPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <CommunitiesPage userId={userId} locale={locale} {...props} />
}
