import { useTranslation } from 'react-i18next'
import { Megaphone } from 'lucide-react'
import { PageHeader } from '../../../components/ui/PageHeader'
import { ErrorState } from '../../../components/ui/states'
import { Listing } from '../../../features/parent/components'
import { useParentQuery } from '../../../features/parent/helpers'
import { parentApi } from '../../../features/parent/parentApi'
import { displayValue, formatDate, itemId } from '../../../features/student/studentUtils'
import { queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function AnnouncementsPage({ userId, locale }) {
  const { t } = useTranslation()
  const query = useParentQuery(queryKeys.parent.announcements(userId), (signal) => parentApi.announcements(signal))
  return (
    <>
      <PageHeader title={t('parent.announcements.title')} description={t('parent.announcements.description')} />
      <Listing query={query} empty={t('parent.empty.announcements')} emptyIcon={Megaphone}>
        {(items) => (
          <div className="student-list">
            {items.map((item, idx) => (
              <div className="ui-list__item" key={itemId(item) || idx} style={{ borderInlineStart: '3px solid var(--accent)' }}>
                <div className="ui-list__body">
                  <div className="ui-list__title">{displayValue(item)}</div>
                  <div className="ui-list__meta">{displayValue(item, ['body', 'Body', 'message', 'Message', 'content', 'Content'])}</div>
                  <div className="ui-list__meta ui-muted">{formatDate(item.createdAt ?? item.CreatedAt ?? item.publishedAt, locale)}</div>
                </div>
              </div>
            ))}
          </div>
        )}
      </Listing>
    </>
  )
}

export default function ParentAnnouncementsPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <AnnouncementsPage userId={userId} locale={locale} {...props} />
}
