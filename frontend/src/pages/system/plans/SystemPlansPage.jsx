import { useTranslation } from 'react-i18next'
import { ErrorState } from '../../../components/ui/states'
import { Head, List } from '../../../features/system/components'
import { useSystemQuery } from '../../../features/system/helpers'
import { systemApi } from '../../../features/system/systemApi'
import { STALE, queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function PlansPage({ userId, locale }) {
  const { t } = useTranslation()
  const query = useSystemQuery(queryKeys.system.plans(userId), (s) => systemApi.plans(s), { staleTime: STALE.medium })
  return (<><Head view="plans" /><List query={query} empty={t('system.empty.plans')} locale={locale} /></>)
}

export default function SystemPlansPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <PlansPage userId={userId} locale={locale} {...props} />
}
