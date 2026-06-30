import { useTranslation } from 'react-i18next'
import { MessageSquare } from 'lucide-react'
import { DetailList } from '../../../components/data/DetailList'
import { PageHeader } from '../../../components/ui/PageHeader'
import { ErrorState } from '../../../components/ui/states'
import { Listing } from '../../../features/parent/components'
import { useParentQuery } from '../../../features/parent/helpers'
import { parentApi } from '../../../features/parent/parentApi'
import { displayValue, itemId } from '../../../features/student/studentUtils'
import { queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function ConversationsPage({ userId, locale }) {
  const { t } = useTranslation()
  const query = useParentQuery(queryKeys.parent.conversations(userId), (signal) => parentApi.conversations(signal))
  return (
    <>
      <PageHeader title={t('parent.conversations.title')} description={t('parent.conversations.description')} />
      <Listing query={query} empty={t('parent.empty.conversations')} emptyIcon={MessageSquare}>
        {(items) => (
          <div className="student-list">
            {items.map((item, idx) => (
              <div className="student-list__item" key={itemId(item) || idx}>
                <strong className="domain-row__title">{displayValue(item) || itemId(item)}</strong>
                <DetailList item={item} locale={locale} />
              </div>
            ))}
          </div>
        )}
      </Listing>
    </>
  )
}

export default function ParentConversationsPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <ConversationsPage userId={userId} locale={locale} {...props} />
}
