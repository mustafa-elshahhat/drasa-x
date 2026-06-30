import { useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { MessageSquare } from 'lucide-react'
import { DetailList } from '../../../shared/data-display'
import { TextareaField } from '../../../shared/form'
import { Button, Card } from '../../../shared/ui'
import { EmptyState, ErrorState } from '../../../shared/feedback'
import { Head, Loading } from '../../../features/school/components'
import { SUPPORT_STATUS } from '../../../features/school/constants'
import { useSchoolQuery } from '../../../features/school/helpers'
import { schoolApi } from '../../../features/school/schoolApi'
import { itemId } from '../../../features/student/studentUtils'
import { queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function SupportPage({ userId, locale }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const query = useSchoolQuery(queryKeys.school.support(userId), (s) => schoolApi.support(s))
  const [replyById, setReplyById] = useState({})
  const respond = useMutation({
    mutationFn: ({ id, responseMessage }) => schoolApi.respondSupport(id, { responseMessage, status: SUPPORT_STATUS.Completed }),
    onSuccess: () => qc.invalidateQueries({ queryKey: queryKeys.school.support(userId) }),
  })
  const items = Array.isArray(query.data) ? query.data : []
  return (
    <>
      <Head view="support" />
      {query.isLoading && <Loading />}
      {query.isError && <ErrorState error={query.error} onRetry={query.refetch} />}
      {query.data && (items.length === 0 ? <EmptyState icon={MessageSquare} title={t('school.empty.support')} /> : (
        <div className="student-list">
          {items.map((item) => {
            const id = itemId(item)
            return (
              <Card key={id}>
                <DetailList item={item} locale={locale} />
                <TextareaField label={t('school.requests.reply')} value={replyById[id] || ''} onChange={(e) => setReplyById((m) => ({ ...m, [id]: e.target.value }))} />
                <Button onClick={() => respond.mutate({ id, responseMessage: replyById[id] || '' })} loading={respond.isPending} disabled={!(replyById[id] || '').trim()}>{t('school.common.respond')}</Button>
              </Card>
            )
          })}
        </div>
      ))}
    </>
  )
}

export default function SchoolSupportPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <SupportPage userId={userId} locale={locale} {...props} />
}
