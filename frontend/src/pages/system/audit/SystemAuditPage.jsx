import { useTranslation } from 'react-i18next'
import { ErrorState } from '../../../shared/feedback'
import { Head, List } from '../../../features/system/components'
import { useSystemQuery } from '../../../features/system/helpers'
import { systemApi } from '../../../features/system/systemApi'
import { queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function AuditPage({ userId, locale }) {
  const { t } = useTranslation()
  const query = useSystemQuery(queryKeys.system.audit(userId), (s) => systemApi.audit(s))
  return (<><Head view="audit" /><List query={query} empty={t('system.empty.audit')} locale={locale} /></>)
}

export default function SystemAuditPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <AuditPage userId={userId} locale={locale} {...props} />
}
