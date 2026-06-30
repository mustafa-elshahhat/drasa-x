import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { MessageSquare } from 'lucide-react'
import { Alert } from '../../../components/ui/Alert'
import { Button } from '../../../components/ui/Button'
import { Card, PageHeader } from '../../../components/ui/PageHeader'
import { Spinner } from '../../../components/ui/Spinner'
import { EmptyState, ErrorState } from '../../../components/ui/states'
import { useAuth } from '../../../features/auth/AuthContext'
import { notificationsApi } from '../../../features/notifications/notificationsApi'
import { STALE, queryKeys } from '../../../lib/query/keys'

function MessagesList() {
  const { t } = useTranslation()
  const { user } = useAuth()
  const userId = user?.id
  const convos = useQuery({
    queryKey: queryKeys.notifications.conversations(userId),
    queryFn: ({ signal }) => notificationsApi.conversations(signal),
    enabled: !!userId,
    staleTime: STALE.short,
  })

  return (
    <div>
      <PageHeader title={t('messages.title')} description={t('messages.description')} />
      <NewConversation />
      {convos.isLoading ? (
        <Spinner label={t('common.loading')} />
      ) : convos.isError ? (
        <ErrorState error={convos.error} onRetry={() => convos.refetch()} />
      ) : (convos.data?.length ?? 0) === 0 ? (
        <EmptyState icon={MessageSquare} title={t('messages.emptyTitle')} message={t('messages.emptyBody')} />
      ) : (
        <ul className="ui-list" data-testid="conversations-list">
          {convos.data.map((c) => (
            <li key={c.id} className="ui-list__item">
              <div className="ui-list__body">
                <Link to={`/app/messages/${c.id}`} className="ui-list__title ui-link">
                  {c.subject || t('messages.untitled')}
                </Link>
                <div className="ui-list__meta ui-muted">
                  {t('messages.participants', { count: c.participants?.length ?? 0 })}
                  {c.isClosed ? ` · ${t('messages.closed')}` : ''}
                </div>
              </div>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}

// Minimal but real "start a conversation" form. The backend enforces the
// participant-relationship policy (parent↔teacher / teacher↔student), so an
// unauthorized pairing is rejected server-side (surfaced here honestly).
function NewConversation() {
  const { t } = useTranslation()
  const { user } = useAuth()
  const qc = useQueryClient()
  const navigate = useNavigate()
  const [open, setOpen] = useState(false)
  const [form, setForm] = useState({ participantUserId: '', subject: '', firstMessage: '' })
  const [error, setError] = useState(null)

  const start = useMutation({
    mutationFn: () => notificationsApi.startConversation(form),
    onMutate: () => setError(null),
    onError: (err) => setError(err),
    onSuccess: (res) => {
      qc.invalidateQueries({ queryKey: queryKeys.notifications.conversations(user?.id) })
      setOpen(false)
      setForm({ participantUserId: '', subject: '', firstMessage: '' })
      if (res?.id) navigate(`/app/messages/${res.id}`)
    },
  })

  const submit = (e) => {
    e.preventDefault()
    start.mutate()
  }

  if (!open) {
    return (
      <div className="cluster" style={{ marginBottom: '1rem' }}>
        <Button onClick={() => setOpen(true)}>{t('messages.new')}</Button>
      </div>
    )
  }
  return (
    <Card title={t('messages.new')}>
      <form onSubmit={submit} className="stack" data-testid="new-conversation-form">
        {error && <Alert variant="error" correlationId={error.correlationId}>{error.detail || error.title || t('common.error')}</Alert>}
        <label className="ui-field">
          <span>{t('messages.recipient')}</span>
          <input
            type="text"
            required
            value={form.participantUserId}
            onChange={(e) => setForm((f) => ({ ...f, participantUserId: e.target.value }))}
          />
        </label>
        <label className="ui-field">
          <span>{t('messages.subject')}</span>
          <input type="text" value={form.subject} onChange={(e) => setForm((f) => ({ ...f, subject: e.target.value }))} />
        </label>
        <label className="ui-field">
          <span>{t('messages.message')}</span>
          <textarea
            required
            rows={3}
            value={form.firstMessage}
            onChange={(e) => setForm((f) => ({ ...f, firstMessage: e.target.value }))}
          />
        </label>
        <div className="cluster">
          <Button type="submit" disabled={start.isPending}>{t('messages.send')}</Button>
          <Button type="button" variant="ghost" onClick={() => setOpen(false)}>
            {t('common.cancel')}
          </Button>
        </div>
      </form>
    </Card>
  )
}

// --------------------------------------------------------------------------
// Messaging — single thread
// --------------------------------------------------------------------------

export default MessagesList
