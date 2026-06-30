import { useTranslation } from 'react-i18next'
import { ErrorState } from '../../../shared/feedback'
import { Head, List } from '../../../features/school/components'
import { useSchoolQuery } from '../../../features/school/helpers'
import { schoolApi } from '../../../features/school/schoolApi'
import { queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function AuditPage({ userId, locale }) {
  const { t } = useTranslation()
  const query = useSchoolQuery(queryKeys.school.audit(userId), (s) => schoolApi.audit(s))
  return (<><Head view="audit" /><List query={query} empty={t('school.empty.audit')} locale={locale} /></>)
}

export default function SchoolAuditPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <AuditPage userId={userId} locale={locale} {...props} />
}
