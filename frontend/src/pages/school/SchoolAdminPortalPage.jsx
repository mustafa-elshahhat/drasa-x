import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Building2 } from 'lucide-react'
import { PageHeader, Card } from '../../components/ui/PageHeader'
import { EmptyState, ErrorState } from '../../components/ui/states'
import { Button } from '../../components/ui/Button'
import { Alert } from '../../components/ui/Alert'
import { TextField, TextareaField, SelectField, DateField, CheckboxField } from '../../components/form/fields'
import { useAuth } from '../../features/auth/AuthContext'
import { schoolApi } from '../../features/school/schoolApi'
import { displayValue, itemId, statusLabel, formatDate, settledData } from '../../features/student/studentUtils'
import { queryKeys, STALE } from '../../lib/query/keys'
import { toItems, toObject } from '../../features/student/studentSchemas'

// Numeric enum maps (the backend binds enums numerically — no JsonStringEnumConverter).
const GUARDIAN = ['Mother', 'Father', 'Guardian', 'Sibling', 'Other'] // value == index
const CLASS_ROLE = ['SubjectTeacher', 'HomeroomTeacher', 'Assistant'] // value == index
const AUDIENCE = { Students: 1, Parents: 2, Teachers: 4, All: 7 }
const REQ_STATUS = { Open: 0, InProgress: 1, Resolved: 2, Rejected: 3, Closed: 4 }
const SUPPORT_STATUS = { Pending: 1, Approved: 2, Rejected: 3, Completed: 4 }
const SETTING_TYPE = { String: 0, Number: 1, Boolean: 2, Json: 3 }
const USER_ROLES = ['Student', 'Teacher', 'Parent']

// All school-admin reads are tenant-scoped + SchoolAdmin-guarded server-side; the UI
// only renders what the backend returns and never fabricates statistics or ids.
function useSchoolQuery(key, fn, options = {}) {
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

function DetailGrid({ item, locale }) {
  if (!item || typeof item !== 'object') return null
  const fields = Object.entries(item).filter(([, v]) => v !== null && v !== undefined && typeof v !== 'object')
  return (
    <dl className="student-grid">
      {fields.slice(0, 16).map(([key, value]) => (
        <Field key={key} label={key} value={String(key).toLowerCase().includes('date') || String(key).toLowerCase().endsWith('at') ? formatDate(value, locale) : statusLabel(value)} />
      ))}
    </dl>
  )
}

function ItemsList({ items, empty, renderItem }) {
  if (!items?.length) return <EmptyState title={empty} />
  return <div className="student-list">{items.map((item, i) => <div className="student-list__item" key={itemId(item) || i}>{renderItem(item)}</div>)}</div>
}

function QueryResult({ query, empty, children }) {
  const { t } = useTranslation()
  if (query.isLoading) return <p role="status">{t('states.loading')}</p>
  if (query.isError) return <ErrorState error={query.error} onRetry={query.refetch} />
  const data = Array.isArray(query.data) ? query.data : query.data ? query.data : []
  if (Array.isArray(data) && !data.length) return <EmptyState title={empty} />
  return children(data)
}

function MetricLink({ to, value, label }) {
  return <Link className="student-metric" to={to}><strong>{value}</strong><span>{label}</span></Link>
}

function Head({ view }) {
  const { t } = useTranslation()
  return <PageHeader title={t(`school.pages.${view}.title`)} description={t(`school.pages.${view}.description`)} />
}

// ---------------------------------------------------------------------------
// Dashboard
// ---------------------------------------------------------------------------
function DashboardPage({ userId }) {
  const { t } = useTranslation()
  const dashboard = useSchoolQuery(queryKeys.school.dashboard(userId), (s) => schoolApi.dashboard(s))
  const d = dashboard.data || {}
  return (
    <>
      <Head view="dashboard" />
      {dashboard.isLoading && <p role="status">{t('states.loading')}</p>}
      {dashboard.isError && <ErrorState error={dashboard.error} onRetry={dashboard.refetch} />}
      {dashboard.data && (
        <>
          <Alert title={d.tenantName || t('school.dashboard.tenant')}>
            {t('school.dashboard.status')}: <strong>{statusLabel(d.tenantStatus)}</strong>
            {d.tenantType ? ` · ${statusLabel(d.tenantType)}` : ''}
          </Alert>
          <div className="student-dashboard">
            <Card title={t('school.dashboard.students')}><MetricLink to="/app/school/students" value={d.students ?? 0} label={t('school.pages.students.title')} /></Card>
            <Card title={t('school.dashboard.teachers')}><MetricLink to="/app/school/teachers" value={d.teachers ?? 0} label={t('school.pages.teachers.title')} /></Card>
            <Card title={t('school.dashboard.parents')}><MetricLink to="/app/school/parents" value={d.parents ?? 0} label={t('school.pages.parents.title')} /></Card>
            <Card title={t('school.dashboard.classes')}><MetricLink to="/app/school/classes" value={d.classes ?? 0} label={t('school.pages.classes.title')} /></Card>
            <Card title={t('school.dashboard.subjects')}><MetricLink to="/app/school/subjects" value={d.subjects ?? 0} label={t('school.pages.subjects.title')} /></Card>
            <Card title={t('school.dashboard.links')}><MetricLink to="/app/school/relationships" value={d.parentStudentLinks ?? 0} label={t('school.pages.relationships.title')} /></Card>
            <Card title={t('school.dashboard.assignments')}><MetricLink to="/app/school/teacher-assignments" value={d.teacherClassAssignments ?? 0} label={t('school.pages.teacherAssignments.title')} /></Card>
            <Card title={t('school.dashboard.announcements')}><MetricLink to="/app/school/announcements" value={d.activeAnnouncements ?? 0} label={t('school.pages.announcements.title')} /></Card>
            <Card title={t('school.dashboard.openRequests')}><MetricLink to="/app/school/document-requests" value={d.openParentRequests ?? 0} label={t('school.pages.documentRequests.title')} /></Card>
            <Card title={t('school.dashboard.openSupport')}><MetricLink to="/app/school/support" value={d.openSupportRequests ?? 0} label={t('school.pages.support.title')} /></Card>
            <Card title={t('school.dashboard.aiRecords')}><MetricLink to="/app/school/ai-usage" value={d.aiUsageRecords ?? 0} label={t('school.dashboard.aiTokens', { tokens: d.aiTotalTokens ?? 0 })} /></Card>
            <Card title={t('school.dashboard.academicYears')}><MetricLink to="/app/school/academic-years" value={d.academicYears ?? 0} label={t('school.pages.academicYears.title')} /></Card>
          </div>
        </>
      )}
    </>
  )
}

// ---------------------------------------------------------------------------
// Profile / Subscription / Usage / Storage
// ---------------------------------------------------------------------------
function ProfilePage({ userId, locale }) {
  const query = useSchoolQuery(queryKeys.school.profile(userId), (s) => schoolApi.profile(s))
  const { t } = useTranslation()
  return (
    <>
      <Head view="profile" />
      {query.isLoading && <p role="status">{t('states.loading')}</p>}
      {query.isError && <ErrorState error={query.error} onRetry={query.refetch} />}
      {query.data && <Card title={query.data.name || t('school.pages.profile.title')}><DetailGrid item={query.data} locale={locale} /></Card>}
    </>
  )
}

function SubscriptionPage({ userId, locale }) {
  const sub = useSchoolQuery(queryKeys.school.subscription(userId), (s) => schoolApi.subscription(s))
  const { t } = useTranslation()
  return (
    <>
      <Head view="subscription" />
      {sub.isLoading && <p role="status">{t('states.loading')}</p>}
      {sub.isError && <ErrorState error={sub.error} onRetry={sub.refetch} />}
      {sub.data && <Card title={t('school.pages.subscription.title')}><DetailGrid item={sub.data} locale={locale} /></Card>}
    </>
  )
}

function StoragePage({ userId, locale }) {
  const usage = useSchoolQuery(queryKeys.school.usage(userId), (s) => schoolApi.usage(s))
  const { t } = useTranslation()
  return (
    <>
      <Head view="storage" />
      <Alert title={t('school.notes.storageTitle')}>{t('school.notes.storage')}</Alert>
      {usage.isLoading && <p role="status">{t('states.loading')}</p>}
      {usage.isError && <ErrorState error={usage.error} onRetry={usage.refetch} />}
      {usage.data && <Card title={t('school.pages.storage.title')}><DetailGrid item={usage.data} locale={locale} /></Card>}
    </>
  )
}

// ---------------------------------------------------------------------------
// Generic list + create scaffolding
// ---------------------------------------------------------------------------
function ListSection({ title, query, empty, locale }) {
  return (
    <Card title={title}>
      <QueryResult query={query} empty={empty}>
        {(items) => <ItemsList items={items} empty={empty} renderItem={(item) => <DetailGrid item={item} locale={locale} />} />}
      </QueryResult>
    </Card>
  )
}

function AcademicYearsPage({ userId, locale }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const query = useSchoolQuery(queryKeys.school.academicYears(userId), (s) => schoolApi.academicYears(s))
  const [form, setForm] = useState({ name: '', code: '', startDate: '', endDate: '', isCurrent: false })
  const create = useMutation({
    mutationFn: () => schoolApi.createAcademicYear({ ...form, startDate: form.startDate || null, endDate: form.endDate || null }),
    onSuccess: () => { setForm({ name: '', code: '', startDate: '', endDate: '', isCurrent: false }); qc.invalidateQueries({ queryKey: queryKeys.school.academicYears(userId) }) },
  })
  const set = (k) => (e) => setForm((f) => ({ ...f, [k]: e.target.value }))
  return (
    <>
      <Head view="academicYears" />
      <Card title={t('school.common.create')}>
        {create.isSuccess && <Alert variant="success" title={t('school.common.created')}>{t('school.common.createdBody')}</Alert>}
        {create.isError && <ErrorState error={create.error} onRetry={() => create.reset()} />}
        <TextField label={t('school.common.name')} value={form.name} onChange={set('name')} />
        <TextField label={t('school.common.code')} value={form.code} onChange={set('code')} />
        <DateField label={t('school.common.startDate')} value={form.startDate} onChange={set('startDate')} />
        <DateField label={t('school.common.endDate')} value={form.endDate} onChange={set('endDate')} />
        <CheckboxField label={t('school.common.current')} checked={form.isCurrent} onChange={(e) => setForm((f) => ({ ...f, isCurrent: e.target.checked }))} />
        <Button onClick={() => create.mutate()} loading={create.isPending} disabled={!form.name.trim() || !form.code.trim()}>{t('school.common.create')}</Button>
      </Card>
      <ListSection title={t('school.pages.academicYears.title')} query={query} empty={t('school.empty.generic')} locale={locale} />
    </>
  )
}

function TermsPage({ userId, locale }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const query = useSchoolQuery(queryKeys.school.terms(userId), (s) => schoolApi.terms(s))
  const years = useSchoolQuery(queryKeys.school.academicYears(userId), (s) => schoolApi.academicYears(s), { staleTime: STALE.medium })
  const [form, setForm] = useState({ name: '', code: '', order: 1, academicYearId: '', startDate: '', endDate: '' })
  const create = useMutation({
    mutationFn: () => schoolApi.createTerm({ ...form, order: Number(form.order) || 1, startDate: form.startDate || null, endDate: form.endDate || null }),
    onSuccess: () => { setForm({ name: '', code: '', order: 1, academicYearId: '', startDate: '', endDate: '' }); qc.invalidateQueries({ queryKey: queryKeys.school.terms(userId) }) },
  })
  const set = (k) => (e) => setForm((f) => ({ ...f, [k]: e.target.value }))
  const yearItems = Array.isArray(years.data) ? years.data : []
  return (
    <>
      <Head view="terms" />
      <Card title={t('school.common.create')}>
        {create.isSuccess && <Alert variant="success" title={t('school.common.created')}>{t('school.common.createdBody')}</Alert>}
        {create.isError && <ErrorState error={create.error} onRetry={() => create.reset()} />}
        <TextField label={t('school.common.name')} value={form.name} onChange={set('name')} />
        <TextField label={t('school.common.code')} value={form.code} onChange={set('code')} />
        <TextField label={t('school.common.order')} type="number" value={form.order} onChange={set('order')} />
        <SelectField label={t('school.common.academicYear')} value={form.academicYearId} onChange={set('academicYearId')}
          options={[{ value: '', label: t('school.common.choose') }, ...yearItems.map((y) => ({ value: itemId(y), label: displayValue(y, ['name', 'Name']) || itemId(y) }))]} />
        <DateField label={t('school.common.startDate')} value={form.startDate} onChange={set('startDate')} />
        <DateField label={t('school.common.endDate')} value={form.endDate} onChange={set('endDate')} />
        <Button onClick={() => create.mutate()} loading={create.isPending} disabled={!form.name.trim() || !form.code.trim() || !form.academicYearId}>{t('school.common.create')}</Button>
      </Card>
      <ListSection title={t('school.pages.terms.title')} query={query} empty={t('school.empty.generic')} locale={locale} />
    </>
  )
}

function GradesPage({ userId, locale }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const query = useSchoolQuery(queryKeys.school.grades(userId), (s) => schoolApi.grades(s))
  const [name, setName] = useState('')
  const create = useMutation({
    mutationFn: () => schoolApi.createGrade({ name }),
    onSuccess: () => { setName(''); qc.invalidateQueries({ queryKey: queryKeys.school.grades(userId) }) },
  })
  return (
    <>
      <Head view="grades" />
      <Card title={t('school.common.create')}>
        {create.isSuccess && <Alert variant="success" title={t('school.common.created')}>{t('school.common.createdBody')}</Alert>}
        {create.isError && <ErrorState error={create.error} onRetry={() => create.reset()} />}
        <TextField label={t('school.common.name')} value={name} onChange={(e) => setName(e.target.value)} />
        <Button onClick={() => create.mutate()} loading={create.isPending} disabled={!name.trim()}>{t('school.common.create')}</Button>
      </Card>
      <ListSection title={t('school.pages.grades.title')} query={query} empty={t('school.empty.generic')} locale={locale} />
    </>
  )
}

function ClassesPage({ userId, locale }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const query = useSchoolQuery(queryKeys.school.classes(userId), (s) => schoolApi.classes(s))
  const grades = useSchoolQuery(queryKeys.school.grades(userId), (s) => schoolApi.grades(s), { staleTime: STALE.medium })
  const years = useSchoolQuery(queryKeys.school.academicYears(userId), (s) => schoolApi.academicYears(s), { staleTime: STALE.medium })
  const [form, setForm] = useState({ name: '', code: '', capacity: '', gradeId: '', academicYearId: '' })
  const create = useMutation({
    mutationFn: () => schoolApi.createClass({ ...form, capacity: form.capacity ? Number(form.capacity) : null }),
    onSuccess: () => { setForm({ name: '', code: '', capacity: '', gradeId: '', academicYearId: '' }); qc.invalidateQueries({ queryKey: queryKeys.school.classes(userId) }) },
  })
  const set = (k) => (e) => setForm((f) => ({ ...f, [k]: e.target.value }))
  const gradeItems = Array.isArray(grades.data) ? grades.data : []
  const yearItems = Array.isArray(years.data) ? years.data : []
  return (
    <>
      <Head view="classes" />
      <Card title={t('school.common.create')}>
        {create.isSuccess && <Alert variant="success" title={t('school.common.created')}>{t('school.common.createdBody')}</Alert>}
        {create.isError && <ErrorState error={create.error} onRetry={() => create.reset()} />}
        <TextField label={t('school.common.name')} value={form.name} onChange={set('name')} />
        <TextField label={t('school.common.code')} value={form.code} onChange={set('code')} />
        <TextField label={t('school.common.capacity')} type="number" value={form.capacity} onChange={set('capacity')} />
        <SelectField label={t('school.common.grade')} value={form.gradeId} onChange={set('gradeId')}
          options={[{ value: '', label: t('school.common.choose') }, ...gradeItems.map((g) => ({ value: itemId(g), label: displayValue(g, ['name', 'Name']) || itemId(g) }))]} />
        <SelectField label={t('school.common.academicYear')} value={form.academicYearId} onChange={set('academicYearId')}
          options={[{ value: '', label: t('school.common.choose') }, ...yearItems.map((y) => ({ value: itemId(y), label: displayValue(y, ['name', 'Name']) || itemId(y) }))]} />
        <Button onClick={() => create.mutate()} loading={create.isPending} disabled={!form.name.trim() || !form.code.trim() || !form.gradeId || !form.academicYearId}>{t('school.common.create')}</Button>
      </Card>
      <ListSection title={t('school.pages.classes.title')} query={query} empty={t('school.empty.generic')} locale={locale} />
    </>
  )
}

function SubjectsPage({ userId, locale }) {
  const { t } = useTranslation()
  const query = useSchoolQuery(queryKeys.school.subjects(userId), (s) => schoolApi.subjects(s))
  return (
    <>
      <Head view="subjects" />
      <Alert title={t('school.notes.subjectsTitle')}>{t('school.notes.subjects')}</Alert>
      <ListSection title={t('school.pages.subjects.title')} query={query} empty={t('school.empty.generic')} locale={locale} />
    </>
  )
}

// ---------------------------------------------------------------------------
// Users / Students / Teachers / Parents
// ---------------------------------------------------------------------------
function UsersPage({ userId, locale, role, canCreate }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const view = role ? role.toLowerCase() + 's' : 'users'
  const query = useSchoolQuery(queryKeys.school.users(userId, role || 'all'), (s) => schoolApi.users(role, s), { staleTime: STALE.medium })
  const grades = useSchoolQuery(queryKeys.school.grades(userId), (s) => schoolApi.grades(s), { staleTime: STALE.medium, enabled: Boolean(canCreate) })
  const [form, setForm] = useState({ fullName: '', loginCode: '', role: 'Student', gradeId: '' })
  const [credential, setCredential] = useState(null)
  const create = useMutation({
    mutationFn: () => schoolApi.createUser({ fullName: form.fullName, loginCode: form.loginCode, role: form.role, gradeId: form.role === 'Student' ? form.gradeId || null : null }),
    onSuccess: (data) => { setCredential(toObject(data)); setForm({ fullName: '', loginCode: '', role: 'Student', gradeId: '' }); qc.invalidateQueries({ queryKey: queryKeys.school.users(userId, role || 'all') }) },
  })
  const set = (k) => (e) => setForm((f) => ({ ...f, [k]: e.target.value }))
  const gradeItems = Array.isArray(grades.data) ? grades.data : []
  return (
    <>
      <Head view={view} />
      {canCreate && (
        <Card title={t('school.users.create')}>
          {credential && (
            <Alert variant="success" title={t('school.credential.title')}>
              {t('school.credential.body')} — {t('school.common.loginCode')}: <code>{credential.loginCode}</code> · {t('school.credential.password')}: <code>{credential.temporaryPassword}</code>
            </Alert>
          )}
          {create.isError && <ErrorState error={create.error} onRetry={() => create.reset()} />}
          <TextField label={t('school.common.name')} value={form.fullName} onChange={set('fullName')} />
          <TextField label={t('school.common.loginCode')} value={form.loginCode} onChange={set('loginCode')} />
          <SelectField label={t('school.common.role')} value={form.role} onChange={set('role')} options={USER_ROLES.map((r) => ({ value: r, label: t(`roles.${r}`) }))} />
          {form.role === 'Student' && (
            <SelectField label={t('school.common.grade')} value={form.gradeId} onChange={set('gradeId')}
              options={[{ value: '', label: t('school.common.choose') }, ...gradeItems.map((g) => ({ value: itemId(g), label: displayValue(g, ['name', 'Name']) || itemId(g) }))]} />
          )}
          <Button onClick={() => create.mutate()} loading={create.isPending} disabled={!form.fullName.trim() || !form.loginCode.trim()}>{t('school.users.create')}</Button>
        </Card>
      )}
      <ListSection title={t(`school.pages.${view}.title`)} query={query} empty={t('school.empty.users')} locale={locale} />
    </>
  )
}

// ---------------------------------------------------------------------------
// Relationships (parent ↔ student)
// ---------------------------------------------------------------------------
function RelationshipsPage({ userId, locale }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const query = useSchoolQuery(queryKeys.school.relationships(userId), (s) => schoolApi.relationships(s))
  const parents = useSchoolQuery(queryKeys.school.users(userId, 'Parent'), (s) => schoolApi.users('Parent', s), { staleTime: STALE.medium })
  const students = useSchoolQuery(queryKeys.school.users(userId, 'Student'), (s) => schoolApi.users('Student', s), { staleTime: STALE.medium })
  const [form, setForm] = useState({ parentId: '', studentId: '', relationship: 2, isPrimary: false, canViewProgress: true })
  const create = useMutation({
    mutationFn: () => schoolApi.createRelationship({ parentId: form.parentId, studentId: form.studentId, relationship: Number(form.relationship), isPrimary: form.isPrimary, canViewProgress: form.canViewProgress }),
    onSuccess: () => { setForm({ parentId: '', studentId: '', relationship: 2, isPrimary: false, canViewProgress: true }); qc.invalidateQueries({ queryKey: queryKeys.school.relationships(userId) }) },
  })
  const deactivate = useMutation({ mutationFn: (id) => schoolApi.deactivateRelationship(id), onSuccess: () => qc.invalidateQueries({ queryKey: queryKeys.school.relationships(userId) }) })
  const set = (k) => (e) => setForm((f) => ({ ...f, [k]: e.target.value }))
  const parentItems = Array.isArray(parents.data) ? parents.data : []
  const studentItems = Array.isArray(students.data) ? students.data : []
  return (
    <>
      <Head view="relationships" />
      <Card title={t('school.relationships.link')}>
        {create.isSuccess && <Alert variant="success" title={t('school.common.created')}>{t('school.relationships.linked')}</Alert>}
        {create.isError && <ErrorState error={create.error} onRetry={() => create.reset()} />}
        <SelectField label={t('school.common.parent')} value={form.parentId} onChange={set('parentId')}
          options={[{ value: '', label: t('school.common.choose') }, ...parentItems.map((p) => ({ value: itemId(p), label: displayValue(p, ['fullName', 'FullName']) || itemId(p) }))]} />
        <SelectField label={t('school.common.student')} value={form.studentId} onChange={set('studentId')}
          options={[{ value: '', label: t('school.common.choose') }, ...studentItems.map((sx) => ({ value: itemId(sx), label: displayValue(sx, ['fullName', 'FullName']) || itemId(sx) }))]} />
        <SelectField label={t('school.common.relationship')} value={form.relationship} onChange={set('relationship')} options={GUARDIAN.map((g, i) => ({ value: i, label: t(`school.guardian.${g}`) }))} />
        <CheckboxField label={t('school.common.isPrimary')} checked={form.isPrimary} onChange={(e) => setForm((f) => ({ ...f, isPrimary: e.target.checked }))} />
        <CheckboxField label={t('school.common.canViewProgress')} checked={form.canViewProgress} onChange={(e) => setForm((f) => ({ ...f, canViewProgress: e.target.checked }))} />
        <Button onClick={() => create.mutate()} loading={create.isPending} disabled={!form.parentId || !form.studentId}>{t('school.relationships.link')}</Button>
      </Card>
      <Card title={t('school.pages.relationships.title')}>
        <QueryResult query={query} empty={t('school.empty.relationships')}>
          {(items) => <ItemsList items={items} empty={t('school.empty.relationships')} renderItem={(item) => (
            <div className="student-item">
              <div><DetailGrid item={item} locale={locale} /></div>
              {item.isActive && <div><Button variant="secondary" onClick={() => deactivate.mutate(itemId(item))} loading={deactivate.isPending}>{t('school.common.deactivate')}</Button></div>}
            </div>
          )} />}
        </QueryResult>
      </Card>
    </>
  )
}

// ---------------------------------------------------------------------------
// Teacher assignments (subject + class)
// ---------------------------------------------------------------------------
function TeacherAssignmentsPage({ userId, locale }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const subjectAsg = useSchoolQuery(queryKeys.school.subjectAssignments(userId), (s) => schoolApi.subjectAssignments(s))
  const classAsg = useSchoolQuery(queryKeys.school.classAssignments(userId), (s) => schoolApi.classAssignments(s))
  const teachers = useSchoolQuery(queryKeys.school.users(userId, 'Teacher'), (s) => schoolApi.users('Teacher', s), { staleTime: STALE.medium })
  const subjects = useSchoolQuery(queryKeys.school.subjects(userId), (s) => schoolApi.subjects(s), { staleTime: STALE.medium })
  const classes = useSchoolQuery(queryKeys.school.classes(userId), (s) => schoolApi.classes(s), { staleTime: STALE.medium })
  const [subjForm, setSubjForm] = useState({ teacherId: '', subjectId: '' })
  const [classForm, setClassForm] = useState({ teacherId: '', schoolClassId: '', subjectId: '', role: 0 })

  const createSubj = useMutation({
    mutationFn: () => schoolApi.createSubjectAssignment({ teacherId: subjForm.teacherId, subjectId: subjForm.subjectId }),
    onSuccess: () => { setSubjForm({ teacherId: '', subjectId: '' }); qc.invalidateQueries({ queryKey: queryKeys.school.subjectAssignments(userId) }) },
  })
  const createClass = useMutation({
    mutationFn: () => schoolApi.createClassAssignment({ teacherId: classForm.teacherId, schoolClassId: classForm.schoolClassId, subjectId: classForm.subjectId || null, role: Number(classForm.role) }),
    onSuccess: () => { setClassForm({ teacherId: '', schoolClassId: '', subjectId: '', role: 0 }); qc.invalidateQueries({ queryKey: queryKeys.school.classAssignments(userId) }) },
  })
  const teacherItems = Array.isArray(teachers.data) ? teachers.data : []
  const subjectItems = Array.isArray(subjects.data) ? subjects.data : []
  const classItems = Array.isArray(classes.data) ? classes.data : []
  const teacherOpts = [{ value: '', label: t('school.common.choose') }, ...teacherItems.map((x) => ({ value: itemId(x), label: displayValue(x, ['fullName', 'FullName']) || itemId(x) }))]
  const subjectOpts = (withEmpty) => [...(withEmpty ? [{ value: '', label: t('school.common.none') }] : [{ value: '', label: t('school.common.choose') }]), ...subjectItems.map((x) => ({ value: itemId(x), label: displayValue(x, ['name', 'Name']) || itemId(x) }))]
  const classOpts = [{ value: '', label: t('school.common.choose') }, ...classItems.map((x) => ({ value: itemId(x), label: displayValue(x, ['name', 'Name']) || itemId(x) }))]
  return (
    <>
      <Head view="teacherAssignments" />
      <Card title={t('school.assignments.classCreate')}>
        {createClass.isSuccess && <Alert variant="success" title={t('school.common.created')}>{t('school.common.createdBody')}</Alert>}
        {createClass.isError && <ErrorState error={createClass.error} onRetry={() => createClass.reset()} />}
        <SelectField label={t('school.common.teacher')} value={classForm.teacherId} onChange={(e) => setClassForm((f) => ({ ...f, teacherId: e.target.value }))} options={teacherOpts} />
        <SelectField label={t('school.common.class')} value={classForm.schoolClassId} onChange={(e) => setClassForm((f) => ({ ...f, schoolClassId: e.target.value }))} options={classOpts} />
        <SelectField label={t('school.common.subject')} value={classForm.subjectId} onChange={(e) => setClassForm((f) => ({ ...f, subjectId: e.target.value }))} options={subjectOpts(true)} />
        <SelectField label={t('school.common.role')} value={classForm.role} onChange={(e) => setClassForm((f) => ({ ...f, role: e.target.value }))} options={CLASS_ROLE.map((r, i) => ({ value: i, label: t(`school.classRole.${r}`) }))} />
        <Button onClick={() => createClass.mutate()} loading={createClass.isPending} disabled={!classForm.teacherId || !classForm.schoolClassId}>{t('school.assignments.classCreate')}</Button>
      </Card>
      <Card title={t('school.pages.teacherAssignments.classList')}>
        <QueryResult query={classAsg} empty={t('school.empty.assignments')}>
          {(items) => <ItemsList items={items} empty={t('school.empty.assignments')} renderItem={(item) => <DetailGrid item={item} locale={locale} />} />}
        </QueryResult>
      </Card>
      <Card title={t('school.assignments.subjectCreate')}>
        {createSubj.isSuccess && <Alert variant="success" title={t('school.common.created')}>{t('school.common.createdBody')}</Alert>}
        {createSubj.isError && <ErrorState error={createSubj.error} onRetry={() => createSubj.reset()} />}
        <SelectField label={t('school.common.teacher')} value={subjForm.teacherId} onChange={(e) => setSubjForm((f) => ({ ...f, teacherId: e.target.value }))} options={teacherOpts} />
        <SelectField label={t('school.common.subject')} value={subjForm.subjectId} onChange={(e) => setSubjForm((f) => ({ ...f, subjectId: e.target.value }))} options={subjectOpts(false)} />
        <Button onClick={() => createSubj.mutate()} loading={createSubj.isPending} disabled={!subjForm.teacherId || !subjForm.subjectId}>{t('school.assignments.subjectCreate')}</Button>
      </Card>
      <Card title={t('school.pages.teacherAssignments.subjectList')}>
        <QueryResult query={subjectAsg} empty={t('school.empty.assignments')}>
          {(items) => <ItemsList items={items} empty={t('school.empty.assignments')} renderItem={(item) => <DetailGrid item={item} locale={locale} />} />}
        </QueryResult>
      </Card>
    </>
  )
}

function CurriculumPage({ userId, locale }) {
  const { t } = useTranslation()
  const grades = useSchoolQuery(queryKeys.school.grades(userId), (s) => schoolApi.grades(s))
  const subjects = useSchoolQuery(queryKeys.school.subjects(userId), (s) => schoolApi.subjects(s))
  return (
    <>
      <Head view="curriculum" />
      <Alert title={t('school.notes.curriculumTitle')}>{t('school.notes.curriculum')}</Alert>
      <ListSection title={t('school.pages.grades.title')} query={grades} empty={t('school.empty.generic')} locale={locale} />
      <ListSection title={t('school.pages.subjects.title')} query={subjects} empty={t('school.empty.generic')} locale={locale} />
    </>
  )
}

// ---------------------------------------------------------------------------
// Announcements
// ---------------------------------------------------------------------------
function AnnouncementsPage({ userId, locale }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const query = useSchoolQuery(queryKeys.school.announcements(userId), (s) => schoolApi.announcements(s))
  const [form, setForm] = useState({ title: '', body: '', targetAudience: AUDIENCE.All })
  const create = useMutation({
    mutationFn: () => schoolApi.createAnnouncement({ title: form.title, body: form.body, targetAudience: Number(form.targetAudience) }),
    onSuccess: () => { setForm({ title: '', body: '', targetAudience: AUDIENCE.All }); qc.invalidateQueries({ queryKey: queryKeys.school.announcements(userId) }) },
  })
  const publish = useMutation({ mutationFn: ({ id, on }) => schoolApi.publishAnnouncement(id, on), onSuccess: () => qc.invalidateQueries({ queryKey: queryKeys.school.announcements(userId) }) })
  return (
    <>
      <Head view="announcements" />
      <Card title={t('school.announcements.create')}>
        {create.isSuccess && <Alert variant="success" title={t('school.common.created')}>{t('school.common.createdBody')}</Alert>}
        {create.isError && <ErrorState error={create.error} onRetry={() => create.reset()} />}
        <TextField label={t('school.common.title')} value={form.title} onChange={(e) => setForm((f) => ({ ...f, title: e.target.value }))} />
        <TextareaField label={t('school.common.message')} value={form.body} onChange={(e) => setForm((f) => ({ ...f, body: e.target.value }))} />
        <SelectField label={t('school.announcements.audience')} value={form.targetAudience} onChange={(e) => setForm((f) => ({ ...f, targetAudience: e.target.value }))}
          options={Object.entries(AUDIENCE).map(([k, v]) => ({ value: v, label: t(`school.audiences.${k}`) }))} />
        <Button onClick={() => create.mutate()} loading={create.isPending} disabled={!form.title.trim() || !form.body.trim()}>{t('school.announcements.create')}</Button>
      </Card>
      <Card title={t('school.pages.announcements.title')}>
        <QueryResult query={query} empty={t('school.empty.announcements')}>
          {(items) => <ItemsList items={items} empty={t('school.empty.announcements')} renderItem={(item) => {
            const active = item.isActive ?? item.IsActive
            return (
              <div className="student-item">
                <div><DetailGrid item={item} locale={locale} /></div>
                <div><Button variant="secondary" onClick={() => publish.mutate({ id: itemId(item), on: !active })} loading={publish.isPending}>{active ? t('school.common.unpublish') : t('school.common.publish')}</Button></div>
              </div>
            )
          }} />}
        </QueryResult>
      </Card>
    </>
  )
}

// ---------------------------------------------------------------------------
// Document requests (SchoolAdmin responds + transitions status)
// ---------------------------------------------------------------------------
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
  return (
    <>
      <Head view="documentRequests" />
      {respond.isSuccess && <Alert variant="success" title={t('school.requests.responded')}>{t('school.requests.respondedBody')}</Alert>}
      <Card title={t('school.pages.documentRequests.title')}>
        <QueryResult query={query} empty={t('school.empty.requests')}>
          {(items) => <ItemsList items={items} empty={t('school.empty.requests')} renderItem={(item) => {
            const id = itemId(item)
            return (
              <div>
                <DetailGrid item={item} locale={locale} />
                <TextareaField label={t('school.requests.reply')} value={replyById[id] || ''} onChange={(e) => setReplyById((m) => ({ ...m, [id]: e.target.value }))} />
                <div className="student-item">
                  <Button onClick={() => respond.mutate({ id, body: replyById[id] || '' })} loading={respond.isPending} disabled={!(replyById[id] || '').trim()}>{t('school.common.respond')}</Button>
                  <Button variant="secondary" onClick={() => transition.mutate({ id, status: REQ_STATUS.Resolved })} loading={transition.isPending}>{t('school.requests.markResolved')}</Button>
                </div>
              </div>
            )
          }} />}
        </QueryResult>
      </Card>
    </>
  )
}

// ---------------------------------------------------------------------------
// Read-only engagement / reports / ai-usage / audit
// ---------------------------------------------------------------------------
function CommunitiesPage({ userId, locale }) {
  const { t } = useTranslation()
  const query = useSchoolQuery(queryKeys.school.communities(userId), (s) => schoolApi.communities(s))
  return (<><Head view="communities" /><Alert title={t('school.notes.readOnlyTitle')}>{t('school.notes.communities')}</Alert><ListSection title={t('school.pages.communities.title')} query={query} empty={t('school.empty.communities')} locale={locale} /></>)
}

function CompetitionsPage({ userId, locale }) {
  const { t } = useTranslation()
  const query = useSchoolQuery(queryKeys.school.competitions(userId), (s) => schoolApi.competitions(s))
  return (<><Head view="competitions" /><Alert title={t('school.notes.readOnlyTitle')}>{t('school.notes.competitions')}</Alert><ListSection title={t('school.pages.competitions.title')} query={query} empty={t('school.empty.competitions')} locale={locale} /></>)
}

function ReportsPage({ userId, locale }) {
  const { t } = useTranslation()
  const query = useSchoolQuery(queryKeys.school.reports(userId), (s) => schoolApi.reports(s))
  const users = query.data ? toObject(settledData(query.data.users)) : null
  const ai = query.data ? toObject(settledData(query.data.ai)) : null
  return (
    <>
      <Head view="reports" />
      {query.isLoading && <p role="status">{t('states.loading')}</p>}
      {query.isError && <ErrorState error={query.error} onRetry={query.refetch} />}
      {query.data && (
        <>
          <Card title={t('school.reports.users')}>{users ? <DetailGrid item={users} locale={locale} /> : <EmptyState title={t('school.empty.reports')} />}</Card>
          <Card title={t('school.reports.ai')}>{ai ? <DetailGrid item={ai} locale={locale} /> : <EmptyState title={t('school.empty.reports')} />}</Card>
        </>
      )}
    </>
  )
}

function AiUsagePage({ userId, locale }) {
  const { t } = useTranslation()
  const query = useSchoolQuery(queryKeys.school.aiUsage(userId), (s) => schoolApi.aiUsage(s))
  const summary = query.data ? toObject(settledData(query.data.summary)) : null
  const list = query.data ? toItems(settledData(query.data.list)) : []
  return (
    <>
      <Head view="aiUsage" />
      {query.isLoading && <p role="status">{t('states.loading')}</p>}
      {query.isError && <ErrorState error={query.error} onRetry={query.refetch} />}
      {query.data && (
        <>
          <Card title={t('school.aiUsage.summary')}>{summary ? <DetailGrid item={summary} locale={locale} /> : <EmptyState title={t('school.empty.generic')} />}</Card>
          <Card title={t('school.aiUsage.records')}><ItemsList items={list} empty={t('school.empty.generic')} renderItem={(item) => <DetailGrid item={item} locale={locale} />} /></Card>
        </>
      )}
    </>
  )
}

function SupportPage({ userId, locale }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const query = useSchoolQuery(queryKeys.school.support(userId), (s) => schoolApi.support(s))
  const [replyById, setReplyById] = useState({})
  const respond = useMutation({
    mutationFn: ({ id, responseMessage }) => schoolApi.respondSupport(id, { responseMessage, status: SUPPORT_STATUS.Completed }),
    onSuccess: () => qc.invalidateQueries({ queryKey: queryKeys.school.support(userId) }),
  })
  return (
    <>
      <Head view="support" />
      <Card title={t('school.pages.support.title')}>
        <QueryResult query={query} empty={t('school.empty.support')}>
          {(items) => <ItemsList items={items} empty={t('school.empty.support')} renderItem={(item) => {
            const id = itemId(item)
            return (
              <div>
                <DetailGrid item={item} locale={locale} />
                <TextareaField label={t('school.requests.reply')} value={replyById[id] || ''} onChange={(e) => setReplyById((m) => ({ ...m, [id]: e.target.value }))} />
                <Button onClick={() => respond.mutate({ id, responseMessage: replyById[id] || '' })} loading={respond.isPending} disabled={!(replyById[id] || '').trim()}>{t('school.common.respond')}</Button>
              </div>
            )
          }} />}
        </QueryResult>
      </Card>
    </>
  )
}

function AuditPage({ userId, locale }) {
  const { t } = useTranslation()
  const query = useSchoolQuery(queryKeys.school.audit(userId), (s) => schoolApi.audit(s))
  return (<><Head view="audit" /><ListSection title={t('school.pages.audit.title')} query={query} empty={t('school.empty.audit')} locale={locale} /></>)
}

function SettingsPage({ userId, locale }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const query = useSchoolQuery(queryKeys.school.settings(userId), (s) => schoolApi.settings(s))
  const [form, setForm] = useState({ key: '', value: '', valueType: SETTING_TYPE.String, isSecret: false })
  const upsert = useMutation({
    mutationFn: () => schoolApi.upsertSetting({ key: form.key, value: form.value, valueType: Number(form.valueType), isSecret: form.isSecret }),
    onSuccess: () => { setForm({ key: '', value: '', valueType: SETTING_TYPE.String, isSecret: false }); qc.invalidateQueries({ queryKey: queryKeys.school.settings(userId) }) },
  })
  return (
    <>
      <Head view="settings" />
      <Card title={t('school.settings.upsert')}>
        {upsert.isSuccess && <Alert variant="success" title={t('school.common.saved')}>{t('school.settings.saved')}</Alert>}
        {upsert.isError && <ErrorState error={upsert.error} onRetry={() => upsert.reset()} />}
        <TextField label={t('school.common.key')} value={form.key} onChange={(e) => setForm((f) => ({ ...f, key: e.target.value }))} />
        <TextField label={t('school.common.value')} value={form.value} onChange={(e) => setForm((f) => ({ ...f, value: e.target.value }))} />
        <SelectField label={t('school.common.valueType')} value={form.valueType} onChange={(e) => setForm((f) => ({ ...f, valueType: e.target.value }))}
          options={Object.entries(SETTING_TYPE).map(([k, v]) => ({ value: v, label: k }))} />
        <CheckboxField label={t('school.common.secret')} checked={form.isSecret} onChange={(e) => setForm((f) => ({ ...f, isSecret: e.target.checked }))} />
        <Button onClick={() => upsert.mutate()} loading={upsert.isPending} disabled={!form.key.trim()}>{t('school.common.save')}</Button>
      </Card>
      <ListSection title={t('school.pages.settings.title')} query={query} empty={t('school.empty.settings')} locale={locale} />
      <Card title={t('nav.security')}><Link className="ui-btn ui-btn--primary" to="/app/security">{t('security.changePassword')}</Link></Card>
    </>
  )
}

// ---------------------------------------------------------------------------
export default function SchoolAdminPortalPage({ view }) {
  const { i18n } = useTranslation()
  const { user } = useAuth()
  const userId = user?.id
  const locale = i18n.language || 'en'
  if (!userId) return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  const common = { userId, locale }
  switch (view) {
    case 'profile': return <ProfilePage {...common} />
    case 'academic-years': return <AcademicYearsPage {...common} />
    case 'terms': return <TermsPage {...common} />
    case 'grades': return <GradesPage {...common} />
    case 'classes': return <ClassesPage {...common} />
    case 'subjects': return <SubjectsPage {...common} />
    case 'users': return <UsersPage {...common} role={null} canCreate />
    case 'students': return <UsersPage {...common} role="Student" />
    case 'teachers': return <UsersPage {...common} role="Teacher" />
    case 'parents': return <UsersPage {...common} role="Parent" />
    case 'relationships': return <RelationshipsPage {...common} />
    case 'teacher-assignments': return <TeacherAssignmentsPage {...common} />
    case 'curriculum': return <CurriculumPage {...common} />
    case 'announcements': return <AnnouncementsPage {...common} />
    case 'document-requests': return <DocumentRequestsPage {...common} />
    case 'communities': return <CommunitiesPage {...common} />
    case 'competitions': return <CompetitionsPage {...common} />
    case 'reports': return <ReportsPage {...common} />
    case 'ai-usage': return <AiUsagePage {...common} />
    case 'storage': return <StoragePage {...common} />
    case 'subscription': return <SubscriptionPage {...common} />
    case 'support': return <SupportPage {...common} />
    case 'audit-log': return <AuditPage {...common} />
    case 'settings': return <SettingsPage {...common} />
    default: return <DashboardPage {...common} />
  }
}
