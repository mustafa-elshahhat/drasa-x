import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { MessageThread } from '../../../shared/chat'
import { Alert, Button, Card, PageHeader, Spinner } from '../../../shared/ui'
import { EmptyState, ErrorState } from '../../../shared/feedback'
import { useAuth } from '../../../features/auth/AuthContext'
import { formatWhen } from '../../../features/communication/helpers'
import { notificationsApi } from '../../../features/notifications/notificationsApi'
import { STALE, queryKeys } from '../../../lib/query/keys'

function ThreadView() {
  const { t } = useTranslation()
  const { user } = useAuth()
  const { conversationId } = useParams()
  const qc = useQueryClient()
  const userId = user?.id
  const [draft, setDraft] = useState('')

  const conversation = useQuery({
    queryKey: queryKeys.notifications.conversation(userId, conversationId),
    queryFn: ({ signal }) => notificationsApi.conversation(conversationId, signal),
    enabled: !!userId && !!conversationId,
    retry: false,
  })
  const messages = useQuery({
    queryKey: queryKeys.notifications.messages(userId, conversationId),
    queryFn: ({ signal }) => notificationsApi.messages(conversationId, signal),
    enabled: !!userId && !!conversationId && !conversation.isError,
    staleTime: STALE.short,
  })

  const send = useMutation({
    mutationFn: (body) => notificationsApi.postMessage(conversationId, body),
    onSuccess: () => {
      setDraft('')
      qc.invalidateQueries({ queryKey: queryKeys.notifications.messages(userId, conversationId) })
    },
  })

  if (conversation.isError) {
    // Non-participant / cross-tenant → backend returns 404; show forbidden honestly.
    return (
      <div>
        <PageHeader title={t('messages.thread')} />
        <Alert variant="warning">{t('messages.forbidden')}</Alert>
        <Link to="/app/messages" className="ui-link">
          {t('messages.backToList')}
        </Link>
      </div>
    )
  }

  return (
    <div>
      <PageHeader
        title={conversation.data?.subject || t('messages.thread')}
        actions={
          <Link to="/app/messages" className="ui-btn ui-btn--ghost">
            {t('messages.backToList')}
          </Link>
        }
      />
      {messages.isLoading ? (
        <Spinner label={t('common.loading')} />
      ) : messages.isError ? (
        <ErrorState error={messages.error} onRetry={() => messages.refetch()} />
      ) : (messages.data?.length ?? 0) === 0 ? (
        <EmptyState title={t('messages.threadEmptyTitle')} message={t('messages.threadEmptyBody')} />
      ) : (
        <Card>
          <MessageThread messages={messages.data} currentUserId={userId} formatWhen={formatWhen} />
        </Card>
      )}

      {!conversation.data?.isClosed && (
        <form
          className="cluster"
          data-testid="reply-form"
          onSubmit={(e) => {
            e.preventDefault()
            if (draft.trim()) send.mutate(draft.trim())
          }}
        >
          <textarea
            rows={2}
            value={draft}
            placeholder={t('messages.replyPlaceholder')}
            onChange={(e) => setDraft(e.target.value)}
            aria-label={t('messages.reply')}
          />
          <Button type="submit" disabled={send.isPending || !draft.trim()}>
            {t('messages.send')}
          </Button>
        </form>
      )}
    </div>
  )
}

// --------------------------------------------------------------------------
// View dispatcher
// --------------------------------------------------------------------------

export default ThreadView
