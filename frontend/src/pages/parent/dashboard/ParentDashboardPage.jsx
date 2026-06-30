import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Bell, FileText, Megaphone, MessageSquare, Users } from 'lucide-react'
import { ChildCard } from '../../../components/domain/ChildCard'
import { Metric } from '../../../components/ui/Metric'
import { PageHeader } from '../../../components/ui/PageHeader'
import { ErrorState } from '../../../components/ui/states'
import { Loading } from '../../../features/parent/components'
import { useParentQuery } from '../../../features/parent/helpers'
import { parentApi } from '../../../features/parent/parentApi'
import { toItems } from '../../../features/student/studentSchemas'
import { displayValue, itemId } from '../../../features/student/studentUtils'
import { STALE, queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function DashboardPage({ userId }) {
  const { t } = useTranslation()
  const dashboard = useParentQuery(queryKeys.parent.dashboard(userId), (signal) => parentApi.dashboard(signal))
  const children = useParentQuery(queryKeys.parent.children(userId), (signal) => parentApi.children(signal), { staleTime: STALE.medium })
  const d = dashboard.data || {}
  const childItems = toItems(children.data)
  return (
    <>
      <PageHeader title={t('parent.dashboard.title')} description={t('parent.dashboard.description')} />
      {dashboard.isLoading && <Loading />}
      {dashboard.isError && <ErrorState error={dashboard.error} onRetry={dashboard.refetch} />}
      {dashboard.data && (
        <div className="student-dashboard">
          <Metric to="/app/parent/children" icon={Users} accent="var(--orange)" value={d.linkedChildrenCount ?? 0} label={t('parent.dashboard.linkedChildren')} />
          <Metric to="/app/parent/documents" icon={FileText} accent="var(--brand)" label={t('parent.dashboard.documents')} />
          <Metric to="/app/parent/conversations" icon={MessageSquare} accent="var(--info)" label={t('parent.dashboard.conversations')} />
          <Metric to="/app/parent/notifications" icon={Bell} accent="var(--purple)" label={t('parent.dashboard.notifications')} />
          <Metric to="/app/parent/announcements" icon={Megaphone} accent="var(--orange)" label={t('parent.dashboard.announcements')} />
        </div>
      )}
      {childItems.length > 0 && (
        <section className="ui-section">
          <div className="ui-section__head">
            <h2 className="ui-section__title">{t('parent.children.title')}</h2>
            <Link to="/app/parent/children" className="ui-btn ui-btn--soft">{t('parent.viewAll', 'View all')}</Link>
          </div>
          <div className="ui-grid ui-grid--auto">
            {childItems.map((c) => {
              const id = itemId(c, ['studentId', 'StudentId', 'id', 'Id'])
              return <ChildCard key={id} to={`/app/parent/children/${id}`} name={displayValue(c, ['fullName', 'FullName'])} meta={c.className || c.ClassName} />
            })}
          </div>
        </section>
      )}
    </>
  )
}

// ---------------------------------------------------------------------------
// Children list + child overview
// ---------------------------------------------------------------------------

export default function ParentDashboardPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <DashboardPage userId={userId} locale={locale} {...props} />
}
