import { useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { MessageSquare } from 'lucide-react'
import { DetailList } from '../../../components/data/DetailList'
import { TextareaField } from '../../../components/form/fields'
import { Button } from '../../../components/ui/Button'
import { Card } from '../../../components/ui/PageHeader'
import { EmptyState, ErrorState } from '../../../components/ui/states'
import { itemId } from '../../../features/student/studentUtils'
import { Head, Loading } from '../../../features/system/components'
import { SUPPORT_STATUS } from '../../../features/system/constants'
import { useSystemQuery } from '../../../features/system/helpers'
import { systemApi } from '../../../features/system/systemApi'
import { queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function SupportPage({ userId, locale }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const query = useSystemQuery(queryKeys.system.support(userId), (s) => systemApi.supportTickets({}, s))
  const [replyById, setReplyById] = useState({})
  const respond = useMutation({
    mutationFn: ({ id, responseMessage }) => systemApi.respondSupportTicket(id, { responseMessage, status: SUPPORT_STATUS.Completed }),
    onSuccess: () => qc.invalidateQueries({ queryKey: queryKeys.system.support(userId) }),
  })
  const items = Array.isArray(query.data) ? query.data : []
  return (
    <>
      <Head view="support" />
      {query.isLoading && <Loading />}
      {query.isError && <ErrorState error={query.error} onRetry={query.refetch} />}
      {query.data && (items.length === 0 ? <EmptyState icon={MessageSquare} title={t('system.empty.support')} /> : (
        <div className="student-list">
          {items.map((item) => {
            const id = itemId(item)
            return (
              <Card key={id}>
                <DetailList item={item} locale={locale} />
                <TextareaField label={t('system.support.reply')} value={replyById[id] || ''} onChange={(e) => setReplyById((m) => ({ ...m, [id]: e.target.value }))} />
                <Button onClick={() => respond.mutate({ id, responseMessage: replyById[id] || '' })} loading={respond.isPending} disabled={!(replyById[id] || '').trim()}>{t('system.support.respond')}</Button>
              </Card>
            )
          })}
        </div>
      ))}
    </>
  )
}

// ---------------------------------------------------------------------------
// Feature flags / Announcements / Settings
// ---------------------------------------------------------------------------

export default function SystemSupportPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <SupportPage userId={userId} locale={locale} {...props} />
}
