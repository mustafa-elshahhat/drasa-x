import { useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { FileText } from 'lucide-react'
import { DetailList } from '../../../shared/data-display'
import { TextareaField } from '../../../shared/form'
import { Alert, Button, Card } from '../../../shared/ui'
import { EmptyState, ErrorState } from '../../../shared/feedback'
import { Head, Loading } from '../../../features/school/components'
import { REQ_STATUS } from '../../../features/school/constants'
import { useSchoolQuery } from '../../../features/school/helpers'
import { schoolApi } from '../../../features/school/schoolApi'
import { itemId } from '../../../features/student/studentUtils'
import { queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function DocumentRequestsPage({ userId, locale }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const query = useSchoolQuery(queryKeys.school.documentRequests(userId), (s) => schoolApi.documentRequests(s))
  const [replyById, setReplyById] = useState({})
  const respond = useMutation({
    mutationFn: ({ id, body }) => schoolApi.respondDocumentRequest(id, { body }),
    onSuccess: () => qc.invalidateQueries({ queryKey: queryKeys.school.documentRequests(userId) }),
  })
  const transition = useMutation({
    mutationFn: ({ id, status }) => schoolApi.transitionDocumentRequest(id, { status }),
    onSuccess: () => qc.invalidateQueries({ queryKey: queryKeys.school.documentRequests(userId) }),
  })
  const items = Array.isArray(query.data) ? query.data : []
  return (
    <>
      <Head view="documentRequests" />
      {respond.isSuccess && <Alert variant="success" title={t('school.requests.responded')}>{t('school.requests.respondedBody')}</Alert>}
      {query.isLoading && <Loading />}
      {query.isError && <ErrorState error={query.error} onRetry={query.refetch} />}
      {query.data && (items.length === 0 ? <EmptyState icon={FileText} title={t('school.empty.requests')} /> : (
        <div className="student-list">
          {items.map((item) => {
            const id = itemId(item)
            return (
              <Card key={id}>
                <DetailList item={item} locale={locale} />
                <TextareaField label={t('school.requests.reply')} value={replyById[id] || ''} onChange={(e) => setReplyById((m) => ({ ...m, [id]: e.target.value }))} />
                <div className="cluster">
                  <Button onClick={() => respond.mutate({ id, body: replyById[id] || '' })} loading={respond.isPending} disabled={!(replyById[id] || '').trim()}>{t('school.common.respond')}</Button>
                  <Button variant="secondary" onClick={() => transition.mutate({ id, status: REQ_STATUS.Resolved })} loading={transition.isPending}>{t('school.requests.markResolved')}</Button>
                </div>
              </Card>
            )
          })}
        </div>
      ))}
    </>
  )
}

// ---------------------------------------------------------------------------
// Read-only engagement / reports / ai-usage / audit
// ---------------------------------------------------------------------------

export default function SchoolDocumentRequestsPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <DocumentRequestsPage userId={userId} locale={locale} {...props} />
}
