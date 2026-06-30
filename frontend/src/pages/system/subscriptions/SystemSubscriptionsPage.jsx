import { useTranslation } from 'react-i18next'
import { ErrorState } from '../../../shared/feedback'
import { Head, List } from '../../../features/system/components'
import { useSystemQuery } from '../../../features/system/helpers'
import { systemApi } from '../../../features/system/systemApi'
import { queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function SubscriptionsPage({ userId, locale }) {
  const { t } = useTranslation()
  const query = useSystemQuery(queryKeys.system.subscriptions(userId), (s) => systemApi.subscriptions(s))
  return (<><Head view="subscriptions" /><List query={query} empty={t('system.empty.subscriptions')} locale={locale} /></>)
}

export default function SystemSubscriptionsPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <SubscriptionsPage userId={userId} locale={locale} {...props} />
}
