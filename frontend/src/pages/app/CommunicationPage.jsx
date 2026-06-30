// =============================================================================
// Phase 13 — shared communication surfaces for every authenticated tenant role:
//   view 'notifications' → notification centre (real unread count, mark read/all)
//   view 'preferences'   → per-category in-app/e-mail preferences (mandatory locked)
//   view 'messages'      → conversation threads (list)
//   view 'thread'        → one conversation (messages + reply + start-new)
// All data comes from DerasaX-backend via the central client; real-time refresh is
// provided by NotificationsProvider (SignalR). Loading / empty / error / forbidden /
// success states are handled; no fabricated counts, attachments, or presence.
// =============================================================================
import { useState } from 'react'
import { Link, useParams, useNavigate } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Bell, MessageSquare, Settings } from 'lucide-react'
import { PageHeader, Card } from '../../components/ui/PageHeader'
import { EmptyState, ErrorState } from '../../components/ui/states'
import { Spinner } from '../../components/ui/Spinner'
import { Button } from '../../components/ui/Button'
import { Alert } from '../../components/ui/Alert'
import { MessageThread } from '../../components/chat/MessageThread'
import { useAuth } from '../../features/auth/AuthContext'
import { queryKeys, STALE } from '../../lib/query/keys'
import { notificationsApi } from '../../features/notifications/notificationsApi'
import { useNotificationsRealtime } from '../../features/notifications/NotificationsProvider'

const CATEGORY_NAMES = ['General', 'Warning', 'Informational', 'QuizAssigned', 'QuizGraded', 'DeadlineReminder', 'Announcement']
const categoryName = (c) => (typeof c === 'string' ? c : CATEGORY_NAMES[c] ?? 'General')

function formatWhen(value) {
  if (!value) return ''
  const d = new Date(value)
  return Number.isNaN(d.getTime()) ? '' : d.toLocaleString()
}

// --------------------------------------------------------------------------
// Notification centre
// --------------------------------------------------------------------------
function NotificationCentre() {
  const { t } = useTranslation()
  const { user } = useAuth()
  const { connected } = useNotificationsRealtime()
  const qc = useQueryClient()
  const userId = user?.id

  const items = useQuery({
    queryKey: queryKeys.notifications.list(userId),
    queryFn: ({ signal }) => notificationsApi.list(signal),
    enabled: !!userId,
    staleTime: STALE.short,
  })
  const unread = useQuery({
    queryKey: queryKeys.notifications.unread(userId),
    queryFn: ({ signal }) => notificationsApi.unreadCount(signal),
    enabled: !!userId,
    staleTime: STALE.short,
  })

  const refresh = () => {
    qc.invalidateQueries({ queryKey: queryKeys.notifications.list(userId) })
    qc.invalidateQueries({ queryKey: queryKeys.notifications.unread(userId) })
  }
  const markRead = useMutation({ mutationFn: (id) => notificationsApi.markRead(id), onSuccess: refresh })
  const markAll = useMutation({ mutationFn: () => notificationsApi.markAllRead(), onSuccess: refresh })

  return (
    <div>
      <PageHeader
        title={t('notifications.title')}
        description={t('notifications.description')}
        actions={
          <div className="cluster">
            <Link to="/app/notifications/preferences" className="ui-btn ui-btn--ghost">
              <Settings size={16} aria-hidden="true" /> {t('notifications.preferences')}
            </Link>
            <Button variant="secondary" onClick={() => markAll.mutate()} disabled={markAll.isPending || (unread.data ?? 0) === 0}>
              {t('notifications.markAll')}
            </Button>
          </div>
        }
      />

      <Alert variant="info">
        {t('notifications.unread', { count: unread.data ?? 0 })}
        {' · '}
        <span data-testid="notifications-realtime">
          {connected ? t('notifications.realtimeOn') : t('notifications.realtimeOff')}
        </span>
      </Alert>

      {items.isLoading ? (
        <Spinner label={t('common.loading')} />
      ) : items.isError ? (
        <ErrorState error={items.error} onRetry={() => items.refetch()} />
      ) : (items.data?.length ?? 0) === 0 ? (
        <EmptyState icon={Bell} title={t('notifications.emptyTitle')} message={t('notifications.emptyBody')} />
      ) : (
        <ul className="ui-list" data-testid="notifications-list">
          {items.data.map((n) => (
            <li key={n.id} className={`ui-list__item${n.isRead ? '' : ' ui-list__item--unread'}`}>
              <div className="ui-list__body">
                <div className="ui-list__title">
                  <span className="ui-chip">{t(`notifications.category.${categoryName(n.category)}`, categoryName(n.category))}</span>{' '}
                  {n.title}
                </div>
                <div className="ui-list__meta">{n.body}</div>
                <div className="ui-list__meta ui-muted">{formatWhen(n.createdAt)}</div>
                {n.actionUrl && (
                  <Link to={n.actionUrl} className="ui-link">
                    {t('notifications.open')}
                  </Link>
                )}
              </div>
              {!n.isRead && (
                <Button variant="ghost" onClick={() => markRead.mutate(n.id)} disabled={markRead.isPending}>
                  {t('notifications.markRead')}
                </Button>
              )}
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}

// --------------------------------------------------------------------------
// Notification preferences
// --------------------------------------------------------------------------
function PreferencesView() {
  const { t } = useTranslation()
  const { user } = useAuth()
  const qc = useQueryClient()
  const userId = user?.id
  const [error, setError] = useState(null)

  const prefs = useQuery({
    queryKey: queryKeys.notifications.preferences(userId),
    queryFn: ({ signal }) => notificationsApi.preferences(signal),
    enabled: !!userId,
    staleTime: STALE.medium,
  })

  const update = useMutation({
    mutationFn: (body) => notificationsApi.updatePreference(body),
    onMutate: () => setError(null),
    onError: (e) => setError(e),
    onSuccess: () => qc.invalidateQueries({ queryKey: queryKeys.notifications.preferences(userId) }),
  })

  const toggle = (p, field, value) =>
    update.mutate({ category: p.category, inAppEnabled: field === 'inApp' ? value : p.inAppEnabled, emailEnabled: field === 'email' ? value : p.emailEnabled })

  return (
    <div>
      <PageHeader title={t('notifications.preferencesTitle')} description={t('notifications.preferencesDescription')} />
      {error && <Alert variant="error" correlationId={error.correlationId}>{error.detail || error.title || t('common.error')}</Alert>}
      {prefs.isLoading ? (
        <Spinner label={t('common.loading')} />
      ) : prefs.isError ? (
        <ErrorState error={prefs.error} onRetry={() => prefs.refetch()} />
      ) : (
        <Card>
          <table className="ui-table" data-testid="preferences-table">
            <thead>
              <tr>
                <th>{t('notifications.category.heading')}</th>
                <th>{t('notifications.inApp')}</th>
                <th>{t('notifications.email')}</th>
              </tr>
            </thead>
            <tbody>
              {(prefs.data ?? []).map((p) => (
                <tr key={p.category} data-testid={`pref-${categoryName(p.category)}`}>
                  <td>
                    {t(`notifications.category.${p.categoryName}`, p.categoryName)}
                    {p.mandatory && <span className="ui-chip ui-chip--muted"> {t('notifications.mandatory')}</span>}
                  </td>
                  <td>
                    <input
                      type="checkbox"
                      aria-label={`${p.categoryName} ${t('notifications.inApp')}`}
                      checked={!!p.inAppEnabled}
                      disabled={p.mandatory || update.isPending}
                      onChange={(e) => toggle(p, 'inApp', e.target.checked)}
                    />
                  </td>
                  <td>
                    <input
                      type="checkbox"
                      aria-label={`${p.categoryName} ${t('notifications.email')}`}
                      checked={!!p.emailEnabled}
                      disabled={update.isPending}
                      onChange={(e) => toggle(p, 'email', e.target.checked)}
                    />
                    {!p.emailConfigured && <span className="ui-muted"> {t('notifications.emailNotConfigured')}</span>}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </Card>
      )}
    </div>
  )
}

// --------------------------------------------------------------------------
// Messaging — conversation list
// --------------------------------------------------------------------------
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
export default function CommunicationPage({ view }) {
  switch (view) {
    case 'preferences':
      return <PreferencesView />
    case 'messages':
      return <MessagesList />
    case 'thread':
      return <ThreadView />
    case 'notifications':
    default:
      return <NotificationCentre />
  }
}
