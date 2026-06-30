import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { FileText, Users, MessageSquare, Bell, Megaphone, TrendingUp, CalendarDays, Trophy, ShieldCheck } from 'lucide-react'
import { PageHeader, Card } from '../../components/ui/PageHeader'
import { Metric } from '../../components/ui/Metric'
import { EmptyState, ErrorState } from '../../components/ui/states'
import { Button } from '../../components/ui/Button'
import { Alert } from '../../components/ui/Alert'
import { Chip } from '../../components/ui/Chip'
import { Spinner } from '../../components/ui/Spinner'
import { DetailList } from '../../components/data/DetailList'
import { Heatmap } from '../../components/viz/Heatmap'
import { ChildCard } from '../../components/domain/ChildCard'
import { TextField, TextareaField, SelectField } from '../../components/form/fields'
import { FileUpload } from '../../components/files/FileUpload'
import { FileDownloadButton } from '../../components/files/FileDownloadButton'
import { useAuth } from '../../features/auth/AuthContext'
import { parentApi } from '../../features/parent/parentApi'
import { displayValue, itemId, formatDate, settledData } from '../../features/student/studentUtils'
import { queryKeys, STALE } from '../../lib/query/keys'
import { toItems, toObject } from '../../features/student/studentSchemas'

const ATTENDANCE_TONE = { present: 'success', late: 'warning', absent: 'danger', excused: 'info' }

// All parent reads are relationship-authorized server-side; the UI only renders
// what the backend returns and never fabricates child data.
function useParentQuery(key, fn, options = {}) {
  // React Query forbids a query fn resolving to `undefined`; normalize to `null`.
  return useQuery({ queryKey: key, queryFn: async ({ signal }) => (await fn(signal)) ?? null, staleTime: options.staleTime ?? STALE.short, enabled: options.enabled ?? true })
}

function Loading() {
  const { t } = useTranslation()
  return <Spinner label={t('states.loading')} />
}

function Stat({ label, value }) {
  return (
    <div className="domain-child__stat">
      <span className="domain-child__stat-value">{value ?? '—'}</span>
      <span className="domain-child__stat-label">{label}</span>
    </div>
  )
}

function Listing({ query, empty, emptyIcon, children }) {
  if (query.isLoading) return <Loading />
  if (query.isError) return <ErrorState error={query.error} onRetry={() => query.refetch()} />
  const items = Array.isArray(query.data) ? query.data : []
  if (!items.length) return <EmptyState icon={emptyIcon} title={empty} />
  return children(items)
}

// ---------------------------------------------------------------------------
// Dashboard
// ---------------------------------------------------------------------------
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

function ChildOverviewPage({ userId }) {
  const { t } = useTranslation()
  const { childId } = useParams()
  const query = useParentQuery(queryKeys.parent.child(userId, childId), (signal) => parentApi.child(childId, signal))
  const child = query.data || null
  const s = child?.summary
  return (
    <>
      <PageHeader title={child ? (child.fullName || t('parent.child.title')) : t('parent.child.title')} description={t('parent.child.description')} />
      {query.isLoading && <Loading />}
      {query.isError && <ErrorState error={query.error} onRetry={query.refetch} />}
      {child && (
        <>
          <Card title={t('parent.child.profile')}>
            <dl className="ui-detail-list">
              <div className="ui-detail-list__row"><dt>{t('parent.child.name')}</dt><dd>{child.fullName ?? '—'}</dd></div>
              <div className="ui-detail-list__row"><dt>{t('parent.child.grade')}</dt><dd>{child.gradeId ?? '—'}</dd></div>
              <div className="ui-detail-list__row"><dt>{t('parent.child.class')}</dt><dd>{child.className ?? '—'}</dd></div>
              <div className="ui-detail-list__row"><dt>{t('parent.child.relationship')}</dt><dd>{child.relationship ?? '—'}</dd></div>
            </dl>
          </Card>
          <Card title={t('parent.progress.summary')}>
            {s ? (
              <div className="domain-child__stats">
                <Stat label={t('parent.progress.lessonsCompleted')} value={`${s.lessonsCompleted ?? 0}/${s.lessonsTracked ?? 0}`} />
                <Stat label={t('parent.progress.averageLesson')} value={s.averageLessonCompletion} />
                <Stat label={t('parent.progress.quizAttempts')} value={s.quizAttempts} />
                <Stat label={t('parent.progress.averageQuiz')} value={s.averageQuizPercentage} />
                <Stat label={t('parent.progress.subjects')} value={s.subjectsTracked} />
              </div>
            ) : <EmptyState title={t('parent.empty.progress')} />}
          </Card>
          <div className="student-dashboard">
            <Metric to={`/app/parent/children/${childId}/progress`} icon={TrendingUp} accent="var(--brand)" label={t('parent.child.viewProgress')} />
            <Metric to={`/app/parent/children/${childId}/attendance`} icon={CalendarDays} accent="var(--success)" label={t('parent.child.viewAttendance')} />
            <Metric to={`/app/parent/children/${childId}/points`} icon={Trophy} accent="var(--purple)" label={t('parent.child.viewPoints')} />
          </div>
        </>
      )}
    </>
  )
}

// Phase 14 (closure) — parent read-only gamification view for a LINKED child only.
function ChildPointsPage({ userId, locale }) {
  const { t } = useTranslation()
  const { childId } = useParams()
  const summary = useParentQuery(queryKeys.parent.childPoints(userId, childId), (signal) => parentApi.childPoints(childId, signal))
  const ledger = useParentQuery([...queryKeys.parent.childPoints(userId, childId), 'ledger'], (signal) => parentApi.childPointsLedger(childId, signal))
  const total = summary.data?.totalPoints ?? summary.data?.TotalPoints ?? 0
  return (
    <>
      <PageHeader title={t('parent.points.title')} description={t('parent.points.description')} />
      {summary.isLoading && <Loading />}
      {summary.isError && <ErrorState error={summary.error} onRetry={summary.refetch} />}
      {summary.data && (
        <div className="student-dashboard">
          <Metric icon={Trophy} accent="var(--purple)" label={t('parent.points.total')} value={total} />
        </div>
      )}
      <Card title={t('parent.points.ledger')}>
        {ledger.isError ? <ErrorState error={ledger.error} onRetry={ledger.refetch} />
          : (ledger.data?.length ?? 0) === 0 ? <EmptyState title={t('parent.points.empty')} />
          : (
            <ul className="ui-list">
              {(ledger.data || []).map((item, idx) => (
                <li className="ui-list__item" key={itemId(item) || idx}>
                  <div className="ui-list__body">
                    <div className="ui-list__title">{displayValue(item, ['reason', 'Reason']) || displayValue(item)}</div>
                    <div className="ui-list__meta ui-muted">{formatDate(item.createdAt ?? item.CreatedAt, locale)}</div>
                  </div>
                  <Chip tone="brand">{item.points ?? item.Points}</Chip>
                </li>
              ))}
            </ul>
          )}
      </Card>
    </>
  )
}

function ChildProgressPage({ userId, locale }) {
  const { t } = useTranslation()
  const { childId } = useParams()
  const query = useParentQuery(queryKeys.parent.childProgress(userId, childId), (signal) => parentApi.childProgress(childId, signal))
  const data = query.data
  const summary = data ? toObject(settledData(data.summary)) : null
  const subjects = data ? toItems(settledData(data.subjects)) : []
  const attempts = data ? toItems(settledData(data.attempts)) : []
  const insights = data ? toItems(settledData(data.insights)) : []
  const recommendations = data ? toItems(settledData(data.recommendations)) : []
  const listCard = (title, items, empty) => (
    <Card title={title}>
      {items.length ? (
        <div className="student-list">
          {items.map((item, idx) => (
            <div className="student-list__item" key={itemId(item) || idx}>
              <strong className="domain-row__title">{displayValue(item) || itemId(item)}</strong>
              <DetailList item={item} locale={locale} />
            </div>
          ))}
        </div>
      ) : <EmptyState title={empty} />}
    </Card>
  )
  return (
    <>
      <PageHeader title={t('parent.progress.title')} description={t('parent.progress.description')} />
      {query.isLoading && <Loading />}
      {query.isError && <ErrorState error={query.error} onRetry={query.refetch} />}
      {data && (
        <>
          <Card title={t('parent.progress.summary')}>
            {summary ? <DetailList item={summary} locale={locale} /> : <EmptyState title={t('parent.empty.progress')} />}
          </Card>
          {listCard(t('parent.progress.subjectProgress'), subjects, t('parent.empty.progress'))}
          {listCard(t('parent.progress.attemptHistory'), attempts, t('parent.empty.grades'))}
          <Card title={t('parent.progress.insights')}>
            <p className="ui-muted">{t('parent.progress.aiProvenance')}</p>
            {insights.length ? (
              <div className="student-list">
                {insights.map((item, idx) => <div className="student-list__item" key={idx}><DetailList item={item} locale={locale} /></div>)}
              </div>
            ) : <EmptyState title={t('parent.empty.insights')} />}
          </Card>
          {listCard(t('parent.progress.recommendations'), recommendations, t('parent.empty.insights'))}
        </>
      )}
    </>
  )
}

function ChildAttendancePage({ userId, locale }) {
  const { t } = useTranslation()
  const { childId } = useParams()
  const query = useParentQuery(queryKeys.parent.childAttendance(userId, childId), (signal) => parentApi.childAttendance(childId, signal))
  const att = query.data
  const legend = [
    { level: 'present', label: t('parent.attendance.present') },
    { level: 'late', label: t('parent.attendance.late') },
    { level: 'absent', label: t('parent.attendance.absent') },
    { level: 'excused', label: t('parent.attendance.excused') },
  ]
  const cells = (att?.records || []).map((r, i) => {
    const level = (String(r.status || '').toLowerCase()) in ATTENDANCE_TONE ? String(r.status).toLowerCase() : 'none'
    return { key: r.id ?? i, level, title: `${formatDate(r.attendanceDate, locale)} — ${r.status}` }
  })
  return (
    <>
      <PageHeader title={t('parent.attendance.title')} description={t('parent.attendance.description')} />
      <Alert title={t('parent.attendance.noCvTitle')}>{t('parent.attendance.noCvBody')}</Alert>
      {query.isLoading && <Loading />}
      {query.isError && <ErrorState error={query.error} onRetry={query.refetch} />}
      {att && (
        <>
          <Card title={t('parent.attendance.summary')}>
            <div className="domain-child__stats">
              <Stat label={t('parent.attendance.total')} value={att.summary.total} />
              <Stat label={t('parent.attendance.present')} value={att.summary.present} />
              <Stat label={t('parent.attendance.absent')} value={att.summary.absent} />
              <Stat label={t('parent.attendance.late')} value={att.summary.late} />
              <Stat label={t('parent.attendance.excused')} value={att.summary.excused} />
              <Stat label={t('parent.attendance.percentage')} value={att.summary.attendancePercentage} />
            </div>
            {cells.length > 0 && <div style={{ marginTop: 18 }}><Heatmap cells={cells} legend={legend} /></div>}
          </Card>
          <Card title={t('parent.attendance.records')}>
            {(att.records || []).length === 0 ? <EmptyState title={t('parent.attendance.empty')} /> : (
              <ul className="ui-list">
                {att.records.map((row, i) => {
                  const tone = ATTENDANCE_TONE[String(row.status || '').toLowerCase()] || 'muted'
                  return (
                    <li className="ui-list__item" key={row.id || i}>
                      <div className="ui-list__body">
                        <div className="ui-list__title">{formatDate(row.attendanceDate, locale)}</div>
                        {row.source && <div className="ui-list__meta ui-muted">{row.source}</div>}
                      </div>
                      <Chip tone={tone}>{row.status}</Chip>
                    </li>
                  )
                })}
              </ul>
            )}
          </Card>
        </>
      )}
    </>
  )
}

// ---------------------------------------------------------------------------
// Document requests (create + track)
// ---------------------------------------------------------------------------
// ParentRequestType enum values (the backend binds the enum numerically).
const REQUEST_TYPES = ['Document', 'Meeting', 'ProgressFollowUp', 'TeacherContact', 'Other']
const REQUEST_TYPE_VALUE = { Document: 0, Meeting: 1, ProgressFollowUp: 2, TeacherContact: 3, Other: 4 }

// A document-request row with a durable attachment workflow embedded.
function DocumentRequestRow({ item, userId, locale }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const id = itemId(item, ['id', 'Id'])
  const hasDoc = Boolean(item.fileRecordId || item.FileRecordId || item.requestDocumentId || item.hasRequestDocument)
  return (
    <Card>
      <DetailList item={item} locale={locale} />
      <FileUpload
        label={t('files.parentDocTitle')}
        hint={t('files.parentDocHint')}
        accept=".pdf,.doc,.docx,.png,.jpg,.jpeg"
        upload={(file) => parentApi.attachRequestDocument(id, file)}
        onUploaded={() => qc.invalidateQueries({ queryKey: queryKeys.parent.documentRequests(userId) })}
      />
      {hasDoc && <FileDownloadButton download={() => parentApi.downloadRequestDocument(id)} />}
    </Card>
  )
}

function DocumentsPage({ userId, locale }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const children = useParentQuery(queryKeys.parent.children(userId), (signal) => parentApi.children(signal), { staleTime: STALE.medium })
  const requests = useParentQuery(queryKeys.parent.documentRequests(userId), (signal) => parentApi.documentRequests(signal))
  const [studentId, setStudentId] = useState('')
  const [type, setType] = useState('Document')
  const [title, setTitle] = useState('')
  const [body, setBody] = useState('')

  const create = useMutation({
    mutationFn: () => parentApi.createDocumentRequest({ studentId, type: REQUEST_TYPE_VALUE[type] ?? 0, title, body }),
    onSuccess: () => {
      setTitle(''); setBody('')
      qc.invalidateQueries({ queryKey: queryKeys.parent.documentRequests(userId) })
    },
  })

  const childItems = Array.isArray(children.data) ? children.data : []
  const canSubmit = studentId && title.trim() && body.trim()

  return (
    <>
      <PageHeader title={t('parent.documents.title')} description={t('parent.documents.description')} />
      <Card title={t('parent.documents.new')}>
        {create.isSuccess && <Alert variant="success" title={t('parent.documents.submittedTitle')}>{t('parent.documents.submittedBody')}</Alert>}
        {create.isError && <ErrorState error={create.error} onRetry={() => create.reset()} />}
        <div className="ui-formgrid ui-formgrid--2">
          <SelectField
            label={t('parent.documents.child')}
            value={studentId}
            onChange={(e) => setStudentId(e.target.value)}
            options={[{ value: '', label: t('parent.documents.chooseChild') }, ...childItems.map((c) => ({ value: itemId(c, ['studentId', 'StudentId', 'id', 'Id']), label: displayValue(c, ['fullName', 'FullName']) || itemId(c) }))]}
          />
          <SelectField
            label={t('parent.documents.type')}
            value={type}
            onChange={(e) => setType(e.target.value)}
            options={REQUEST_TYPES.map((tp) => ({ value: tp, label: t(`parent.documents.types.${tp}`) }))}
          />
          <TextField label={t('parent.documents.subject')} value={title} onChange={(e) => setTitle(e.target.value)} />
        </div>
        <TextareaField label={t('parent.documents.message')} value={body} onChange={(e) => setBody(e.target.value)} />
        <Button onClick={() => create.mutate()} loading={create.isPending} disabled={!canSubmit}>{t('parent.documents.submit')}</Button>
      </Card>
      <section className="ui-section">
        <div className="ui-section__head"><h2 className="ui-section__title">{t('parent.documents.mine')}</h2></div>
        <Listing query={requests} empty={t('parent.empty.documents')} emptyIcon={FileText}>
          {(items) => (
            <div className="student-list">
              {items.map((item) => <DocumentRequestRow key={itemId(item, ['id', 'Id'])} item={item} userId={userId} locale={locale} />)}
            </div>
          )}
        </Listing>
      </section>
    </>
  )
}

// ---------------------------------------------------------------------------
// Conversations / notifications / announcements / settings
// ---------------------------------------------------------------------------
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

function NotificationsPage({ userId, locale }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const query = useParentQuery(queryKeys.parent.notifications(userId), (signal) => parentApi.notifications(signal))
  const mark = useMutation({ mutationFn: (id) => parentApi.markNotificationRead(id), onSuccess: () => qc.invalidateQueries({ queryKey: queryKeys.parent.notifications(userId) }) })
  const items = query.data ? toItems(settledData(query.data.items)) : []
  const unread = query.data ? toObject(settledData(query.data.count))?.unreadCount : null
  return (
    <>
      <PageHeader title={t('parent.notifications.title')} description={t('parent.notifications.description')} />
      {unread !== null && unread !== undefined && <Alert variant="info" title={t('parent.notifications.unread')}>{unread}</Alert>}
      {query.isError && <ErrorState error={query.error} onRetry={query.refetch} />}
      {items.length === 0 ? <EmptyState icon={Bell} title={t('parent.empty.notifications')} /> : (
        <ul className="ui-list">
          {items.map((item) => {
            const read = item.isRead ?? item.IsRead
            return (
              <li className={`ui-list__item${read ? '' : ' ui-list__item--unread'}`} key={itemId(item)}>
                <div className="ui-list__body">
                  <div className="ui-list__title">{displayValue(item) || itemId(item)}</div>
                  <div className="ui-list__meta">{displayValue(item, ['body', 'Body', 'message', 'Message'])}</div>
                  <div className="ui-list__meta ui-muted">{formatDate(item.createdAt ?? item.CreatedAt, locale)}</div>
                </div>
                {!read && <Button variant="ghost" onClick={() => mark.mutate(itemId(item))} loading={mark.isPending}>{t('parent.notifications.markRead')}</Button>}
              </li>
            )
          })}
        </ul>
      )}
    </>
  )
}

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

function SettingsPage() {
  const { t } = useTranslation()
  return (
    <>
      <PageHeader title={t('parent.settings.title')} description={t('parent.settings.description')} />
      <Card title={t('nav.security')}>
        <Link className="ui-btn ui-btn--primary" to="/app/security"><ShieldCheck size={16} aria-hidden="true" /> {t('parent.settings.changePassword')}</Link>
      </Card>
    </>
  )
}

export default function ParentPortalPage({ view }) {
  const { i18n } = useTranslation()
  const { user } = useAuth()
  const userId = user?.id
  const locale = i18n.language || 'en'
  if (!userId) return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  const common = { userId, locale }
  switch (view) {
    case 'children': return <ChildrenPage {...common} />
    case 'child': return <ChildOverviewPage {...common} />
    case 'child-progress': return <ChildProgressPage {...common} />
    case 'child-attendance': return <ChildAttendancePage {...common} />
    case 'child-points': return <ChildPointsPage {...common} />
    case 'documents': return <DocumentsPage {...common} />
    case 'conversations': return <ConversationsPage {...common} />
    case 'notifications': return <NotificationsPage {...common} />
    case 'announcements': return <AnnouncementsPage {...common} />
    case 'settings': return <SettingsPage />
    default: return <DashboardPage {...common} />
  }
}
