import { useTranslation } from 'react-i18next'
import { Users } from 'lucide-react'
import { ChildCard } from '../../../shared/domain'
import { PageHeader } from '../../../shared/ui'
import { ErrorState } from '../../../shared/feedback'
import { Listing } from '../../../features/parent/components'
import { useParentQuery } from '../../../features/parent/helpers'
import { parentApi } from '../../../features/parent/parentApi'
import { displayValue, itemId } from '../../../features/student/studentUtils'
import { STALE, queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function ChildrenPage({ userId }) {
  const { t } = useTranslation()
  const query = useParentQuery(queryKeys.parent.children(userId), (signal) => parentApi.children(signal), { staleTime: STALE.medium })
  return (
    <>
      <PageHeader title={t('parent.children.title')} description={t('parent.children.description')} />
      <Listing query={query} empty={t('parent.empty.children')} emptyIcon={Users}>
        {(items) => (
          <div className="ui-grid ui-grid--auto">
            {items.map((item) => {
              const id = itemId(item, ['studentId', 'StudentId', 'id', 'Id'])
              return <ChildCard key={id} to={`/app/parent/children/${id}`} name={displayValue(item, ['fullName', 'FullName', 'name', 'Name']) || id} meta={item.className || item.ClassName} />
            })}
          </div>
        )}
      </Listing>
    </>
  )
}

export default function ParentChildrenPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <ChildrenPage userId={userId} locale={locale} {...props} />
}
