import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { FileText } from 'lucide-react'
import { PageHeader, Card } from '../../components/ui/PageHeader'
import { EmptyState, ErrorState } from '../../components/ui/states'
import { Button } from '../../components/ui/Button'
import { Alert } from '../../components/ui/Alert'
import { TextField, TextareaField, SelectField } from '../../components/form/fields'
import { FileUpload } from '../../components/files/FileUpload'
import { FileDownloadButton } from '../../components/files/FileDownloadButton'
import { useAuth } from '../../features/auth/AuthContext'
import { parentApi } from '../../features/parent/parentApi'
import { displayValue, itemId, statusLabel, formatDate, settledData } from '../../features/student/studentUtils'
import { queryKeys, STALE } from '../../lib/query/keys'
import { toItems, toObject } from '../../features/student/studentSchemas'

// All parent reads are relationship-authorized server-side; the UI only renders
// what the backend returns and never fabricates child data.
function useParentQuery(key, fn, options = {}) {
  return useQuery({ queryKey: key, queryFn: ({ signal }) => fn(signal), staleTime: options.staleTime ?? STALE.short, enabled: options.enabled ?? true })
}

function Field({ label, value }) {
  return (
    <div className="student-kv">
      <dt>{label}</dt>
      <dd>{value ?? '—'}</dd>
    </div>
  )
}

function ItemsList({ items, empty, renderItem }) {
  if (!items?.length) return <EmptyState title={empty} />
  return <div className="student-list">{items.map((item, index) => <div className="student-list__item" key={itemId(item) || index}>{renderItem(item)}</div>)}</div>
}

function QueryResult({ query, empty, children }) {
  if (query.isLoading) return <p role="status">{query.loadingLabel || 'Loading…'}</p>
  if (query.isError) return <ErrorState error={query.error} onRetry={query.refetch} />
  const data = Array.isArray(query.data) ? query.data : query.data ? query.data : []
  if (Array.isArray(data) && !data.length) return <EmptyState title={empty} />
  return children(data)
}

function MetricLink({ to, value, label }) {
  return <Link className="student-metric" to={to}><strong>{value}</strong><span>{label}</span></Link>
}

function DetailGrid({ item, locale }) {
  if (!item) return null
  const fields = Object.entries(item).filter(([, value]) => value !== null && value !== undefined && typeof value !== 'object')
  return (
    <dl className="student-grid">
      {fields.slice(0, 16).map(([key, value]) => (
        <Field key={key} label={key} value={String(key).toLowerCase().includes('date') || String(key).toLowerCase().endsWith('at') ? formatDate(value, locale) : statusLabel(value)} />
      ))}
    </dl>
  )
}

// ---------------------------------------------------------------------------
// Dashboard
// ---------------------------------------------------------------------------
function DashboardPage({ userId }) {
  const { t } = useTranslation()
  const dashboard = useParentQuery(queryKeys.parent.dashboard(userId), (signal) => parentApi.dashboard(signal))
  const d = dashboard.data || {}
  return (
    <>
      <PageHeader title={t('parent.dashboard.title')} description={t('parent.dashboard.description')} />
      {dashboard.isLoading && <p role="status">{t('states.loading')}</p>}
      {dashboard.isError && <ErrorState error={dashboard.error} onRetry={dashboard.refetch} />}
      {dashboard.data && (
        <div className="student-dashboard">
          <Card title={t('parent.children.title')}><MetricLink to="/app/parent/children" value={d.linkedChildrenCount ?? 0} label={t('parent.dashboard.linkedChildren')} /></Card>
          <Card title={t('parent.documents.title')}><MetricLink to="/app/parent/documents" value="›" label={t('parent.dashboard.documents')} /></Card>
          <Card title={t('parent.conversations.title')}><MetricLink to="/app/parent/conversations" value="›" label={t('parent.dashboard.conversations')} /></Card>
          <Card title={t('parent.notifications.title')}><MetricLink to="/app/parent/notifications" value="›" label={t('parent.dashboard.notifications')} /></Card>
          <Card title={t('parent.announcements.title')}><MetricLink to="/app/parent/announcements" value="›" label={t('parent.dashboard.announcements')} /></Card>
        </div>
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
      <QueryResult query={query} empty={t('parent.empty.children')}>
        {(items) => (
          <ItemsList
            items={items}
            empty={t('parent.empty.children')}
            renderItem={(item) => {
              const id = itemId(item, ['studentId', 'StudentId', 'id', 'Id'])
              const name = displayValue(item, ['fullName', 'FullName', 'name', 'Name']) || id
              const cls = item.className || item.ClassName
              return (
                <Link className="student-row-link" to={`/app/parent/children/${id}`}>
                  <FileText size={18} aria-hidden="true" />
                  <span>{cls ? `${name} — ${cls}` : name}</span>
                </Link>
              )
            }}
          />
        )}
      </QueryResult>
    </>
  )
}

function ChildOverviewPage({ userId }) {
  const { t } = useTranslation()
  const { childId } = useParams()
  const query = useParentQuery(queryKeys.parent.child(userId, childId), (signal) => parentApi.child(childId, signal))
  const child = query.data || null
  return (
    <>
      <PageHeader title={child ? (child.fullName || t('parent.child.title')) : t('parent.child.title')} description={t('parent.child.description')} />
      {query.isLoading && <p role="status">{t('states.loading')}</p>}
      {query.isError && <ErrorState error={query.error} onRetry={query.refetch} />}
      {child && (
        <>
          <Card title={t('parent.child.profile')}>
            <dl className="student-grid">
              <Field label={t('parent.child.name')} value={child.fullName} />
              <Field label={t('parent.child.grade')} value={child.gradeId} />
              <Field label={t('parent.child.class')} value={child.className} />
              <Field label={t('parent.child.relationship')} value={child.relationship} />
            </dl>
          </Card>
          <Card title={t('parent.progress.summary')}>
            {child.summary ? (
              <dl className="student-grid">
                <Field label={t('parent.progress.lessonsCompleted')} value={`${child.summary.lessonsCompleted ?? 0}/${child.summary.lessonsTracked ?? 0}`} />
                <Field label={t('parent.progress.averageLesson')} value={child.summary.averageLessonCompletion} />
                <Field label={t('parent.progress.quizAttempts')} value={child.summary.quizAttempts} />
                <Field label={t('parent.progress.averageQuiz')} value={child.summary.averageQuizPercentage} />
                <Field label={t('parent.progress.subjects')} value={child.summary.subjectsTracked} />
              </dl>
            ) : <EmptyState title={t('parent.empty.progress')} />}
          </Card>
          <div className="student-dashboard">
            <Card title={t('parent.progress.title')}><MetricLink to={`/app/parent/children/${childId}/progress`} value="›" label={t('parent.child.viewProgress')} /></Card>
            <Card title={t('parent.attendance.title')}><MetricLink to={`/app/parent/children/${childId}/attendance`} value="›" label={t('parent.child.viewAttendance')} /></Card>
            <Card title={t('parent.points.title')}><MetricLink to={`/app/parent/children/${childId}/points`} value="›" label={t('parent.child.viewPoints')} /></Card>
          </div>
        </>
      )}
    </>
  )
}

// Phase 14 (closure) — parent read-only gamification view for a LINKED child only. Reads the
// relationship-authorized student points endpoints; the backend returns 404 for any non-linked child.
function ChildPointsPage({ userId, locale }) {
  const { t } = useTranslation()
  const { childId } = useParams()
  const summary = useParentQuery(queryKeys.parent.childPoints(userId, childId), (signal) => parentApi.childPoints(childId, signal))
  const ledger = useParentQuery([...queryKeys.parent.childPoints(userId, childId), 'ledger'], (signal) => parentApi.childPointsLedger(childId, signal))
  const total = summary.data?.totalPoints ?? summary.data?.TotalPoints ?? 0
  return (
    <>
      <PageHeader title={t('parent.points.title')} description={t('parent.points.description')} />
      {summary.isLoading && <p role="status">{t('states.loading')}</p>}
      {summary.isError && <ErrorState error={summary.error} onRetry={summary.refetch} />}
      {summary.data && (
        <Card title={t('parent.points.title')}>
          <dl className="student-grid"><Field label={t('parent.points.total')} value={total} /></dl>
        </Card>
      )}
      <Card title={t('parent.points.ledger')}>
        {ledger.isError ? <ErrorState error={ledger.error} onRetry={ledger.refetch} />
          : <ItemsList items={ledger.data || []} empty={t('parent.points.empty')} renderItem={(item) => <DetailGrid item={item} locale={locale} />} />}
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
  return (
    <>
      <PageHeader title={t('parent.progress.title')} description={t('parent.progress.description')} />
      {query.isLoading && <p role="status">{t('states.loading')}</p>}
      {query.isError && <ErrorState error={query.error} onRetry={query.refetch} />}
      {data && (
        <>
          <Card title={t('parent.progress.summary')}>
            {summary ? <DetailGrid item={summary} locale={locale} /> : <EmptyState title={t('parent.empty.progress')} />}
          </Card>
          <Card title={t('parent.progress.subjectProgress')}>
            <ItemsList items={subjects} empty={t('parent.empty.progress')} renderItem={(item) => <DetailGrid item={item} locale={locale} />} />
          </Card>
          <Card title={t('parent.progress.attemptHistory')}>
            <ItemsList items={attempts} empty={t('parent.empty.grades')} renderItem={(item) => <DetailGrid item={item} locale={locale} />} />
          </Card>
          <Card title={t('parent.progress.insights')}>
            <p className="ui-muted">{t('parent.progress.aiProvenance')}</p>
            <ItemsList items={insights} empty={t('parent.empty.insights')} renderItem={(item) => <DetailGrid item={item} locale={locale} />} />
          </Card>
          <Card title={t('parent.progress.recommendations')}>
            <ItemsList items={recommendations} empty={t('parent.empty.insights')} renderItem={(item) => <DetailGrid item={item} locale={locale} />} />
          </Card>
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
  return (
    <>
      <PageHeader title={t('parent.attendance.title')} description={t('parent.attendance.description')} />
      <Alert title={t('parent.attendance.noCvTitle')}>{t('parent.attendance.noCvBody')}</Alert>
      {query.isLoading && <p role="status">{t('states.loading')}</p>}
      {query.isError && <ErrorState error={query.error} onRetry={query.refetch} />}
      {att && (
        <>
          <Card title={t('parent.attendance.summary')}>
            <dl className="student-grid">
              <Field label={t('parent.attendance.total')} value={att.summary.total} />
              <Field label={t('parent.attendance.present')} value={att.summary.present} />
              <Field label={t('parent.attendance.absent')} value={att.summary.absent} />
              <Field label={t('parent.attendance.late')} value={att.summary.late} />
              <Field label={t('parent.attendance.excused')} value={att.summary.excused} />
              <Field label={t('parent.attendance.percentage')} value={att.summary.attendancePercentage} />
            </dl>
          </Card>
          <Card title={t('parent.attendance.records')}>
            <ItemsList
              items={att.records}
              empty={t('parent.attendance.empty')}
              renderItem={(row) => (
                <dl className="student-grid">
                  <Field label={t('parent.attendance.date')} value={formatDate(row.attendanceDate, locale)} />
                  <Field label={t('parent.attendance.status')} value={row.status} />
                  <Field label={t('parent.attendance.source')} value={row.source} />
                </dl>
              )}
            />
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

// Phase 19 — a document-request row with a durable attachment workflow embedded:
// the parent attaches a sensitive document (backend-mediated, relationship-authorized)
// and downloads it back. No raw storage URL is ever exposed to the browser.
function DocumentRequestRow({ item, userId, locale }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const id = itemId(item, ['id', 'Id'])
  const hasDoc = Boolean(item.fileRecordId || item.FileRecordId || item.requestDocumentId || item.hasRequestDocument)
  return (
    <>
      <DetailGrid item={item} locale={locale} />
      <FileUpload
        label={t('files.parentDocTitle')}
        hint={t('files.parentDocHint')}
        accept=".pdf,.doc,.docx,.png,.jpg,.jpeg"
        upload={(file) => parentApi.attachRequestDocument(id, file)}
        onUploaded={() => qc.invalidateQueries({ queryKey: queryKeys.parent.documentRequests(userId) })}
      />
      {hasDoc && <FileDownloadButton download={() => parentApi.downloadRequestDocument(id)} />}
    </>
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
        <TextareaField label={t('parent.documents.message')} value={body} onChange={(e) => setBody(e.target.value)} />
        <Button onClick={() => create.mutate()} loading={create.isPending} disabled={!canSubmit}>{t('parent.documents.submit')}</Button>
      </Card>
      <Card title={t('parent.documents.mine')}>
        <QueryResult query={requests} empty={t('parent.empty.documents')}>
          {(items) => <ItemsList items={items} empty={t('parent.empty.documents')} renderItem={(item) => <DocumentRequestRow item={item} userId={userId} locale={locale} />} />}
        </QueryResult>
      </Card>
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
      <QueryResult query={query} empty={t('parent.empty.conversations')}>
        {(items) => <ItemsList items={items} empty={t('parent.empty.conversations')} renderItem={(item) => <DetailGrid item={item} locale={locale} />} />}
      </QueryResult>
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
      {unread !== null && unread !== undefined && <Alert title={t('parent.notifications.unread')}>{unread}</Alert>}
      {query.isError && <ErrorState error={query.error} onRetry={query.refetch} />}
      <ItemsList items={items} empty={t('parent.empty.notifications')} renderItem={(item) => (
        <div className="student-item">
          <div><strong>{displayValue(item) || itemId(item)}</strong><DetailGrid item={item} locale={locale} /></div>
          <div><Button variant="secondary" onClick={() => mark.mutate(itemId(item))} loading={mark.isPending}>{t('parent.notifications.markRead')}</Button></div>
        </div>
      )} />
    </>
  )
}

function AnnouncementsPage({ userId, locale }) {
  const { t } = useTranslation()
  const query = useParentQuery(queryKeys.parent.announcements(userId), (signal) => parentApi.announcements(signal))
  return (
    <>
      <PageHeader title={t('parent.announcements.title')} description={t('parent.announcements.description')} />
      <QueryResult query={query} empty={t('parent.empty.announcements')}>
        {(items) => <ItemsList items={items} empty={t('parent.empty.announcements')} renderItem={(item) => <DetailGrid item={item} locale={locale} />} />}
      </QueryResult>
    </>
  )
}

function SettingsPage() {
  const { t } = useTranslation()
  return (
    <>
      <PageHeader title={t('parent.settings.title')} description={t('parent.settings.description')} />
      <Card title={t('nav.security')}><Link className="ui-btn ui-btn--primary" to="/app/security">{t('parent.settings.changePassword')}</Link></Card>
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
