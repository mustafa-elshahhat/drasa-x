import { useState } from 'react'
import { Link, useLocation, useNavigate, useParams } from 'react-router-dom'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { ClipboardCheck, FileText, Plus } from 'lucide-react'
import { DetailList } from '../../../shared/data-display'
import { UnitCard } from '../../../shared/domain'
import { SelectField, TextField, TextareaField } from '../../../shared/form'
import { Alert, Button, Card, Chip, PageHeader } from '../../../shared/ui'
import { EmptyState, ErrorState } from '../../../shared/feedback'
import { Listing, Loading } from '../../../features/teacher/components'
import { useTeacherQuery } from '../../../features/teacher/helpers'
import { teacherApi } from '../../../features/teacher/teacherApi'
import { assignmentStatusName, displayValue, itemId, submissionStatusName } from '../../../features/teacher/teacherUtils'
import { STALE, queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

const TYPE_OPTIONS = ['Homework', 'Reading', 'Project', 'Practice']

function statusTone(name) {
  const n = String(name).toLowerCase()
  if (n === 'published') return 'success'
  if (n === 'closed') return 'muted'
  if (n === 'archived') return 'danger'
  return 'warning' // draft
}

// ---- List ------------------------------------------------------------------

function HomeworkList({ userId }) {
  const { t } = useTranslation()
  const query = useTeacherQuery(queryKeys.teacher.homework(userId), (signal) => teacherApi.homeworkList(signal))
  return (
    <>
      <PageHeader
        title={t('teacher.homework.title', 'Homework')}
        description={t('teacher.homework.description', 'Create, publish and grade assignments')}
        actions={<Link className="ui-btn ui-btn--primary" to="/app/teacher/homework/new"><Plus size={16} aria-hidden="true" /> {t('teacher.homework.new', 'New homework')}</Link>}
      />
      <Listing query={query} empty={t('teacher.empty.homework', 'No homework yet')} emptyIcon={ClipboardCheck}>
        {(items) => (
          <div className="student-list">
            {items.map((item) => {
              const id = itemId(item, ['id', 'Id'])
              const name = assignmentStatusName(item.status ?? item.Status)
              return (
                <UnitCard
                  key={id}
                  to={`/app/teacher/homework/${id}`}
                  icon={FileText}
                  title={displayValue(item, ['title', 'Title'])}
                  status={name}
                  statusTone={statusTone(name)}
                />
              )
            })}
          </div>
        )}
      </Listing>
    </>
  )
}

// ---- Create ----------------------------------------------------------------

function HomeworkNew({ userId }) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const qc = useQueryClient()
  const [form, setForm] = useState({ title: '', description: '', type: 'Homework', maxScore: '', dueDate: '' })
  const create = useMutation({
    mutationFn: () => teacherApi.createHomework({
      title: form.title.trim(),
      description: form.description.trim() || null,
      type: form.type,
      maxScore: form.maxScore === '' ? null : Number(form.maxScore),
      dueDate: form.dueDate ? new Date(form.dueDate).toISOString() : null,
    }),
    onSuccess: (created) => {
      qc.invalidateQueries({ queryKey: queryKeys.teacher.homework(userId) })
      const id = itemId(created, ['id', 'Id'])
      if (id) navigate(`/app/teacher/homework/${id}`)
    },
  })
  return (
    <>
      <PageHeader title={t('teacher.homework.new', 'New homework')} description={t('teacher.homework.newDescription', 'Create a draft you can publish later')} />
      <Card>
        <form className="stack" onSubmit={(e) => { e.preventDefault(); if (form.title.trim()) create.mutate() }}>
          <TextField label={t('teacher.homework.field.title', 'Title')} value={form.title} onChange={(e) => setForm((f) => ({ ...f, title: e.target.value }))} required />
          <TextareaField label={t('teacher.homework.field.description', 'Description')} value={form.description} onChange={(e) => setForm((f) => ({ ...f, description: e.target.value }))} maxLength={4000} />
          <SelectField
            label={t('teacher.homework.field.type', 'Type')}
            value={form.type}
            onChange={(e) => setForm((f) => ({ ...f, type: e.target.value }))}
            options={TYPE_OPTIONS.map((v) => ({ value: v, label: t(`teacher.homework.types.${v.toLowerCase()}`, v) }))}
          />
          <TextField label={t('teacher.homework.field.maxScore', 'Max score')} type="number" min="0" value={form.maxScore} onChange={(e) => setForm((f) => ({ ...f, maxScore: e.target.value }))} />
          <TextField label={t('teacher.homework.field.dueDate', 'Due date')} type="datetime-local" value={form.dueDate} onChange={(e) => setForm((f) => ({ ...f, dueDate: e.target.value }))} />
          <div className="student-actions">
            <Button type="submit" loading={create.isPending} disabled={!form.title.trim()}>{t('teacher.homework.createDraft', 'Save draft')}</Button>
            <Link to="/app/teacher/homework" className="ui-btn ui-btn--secondary">{t('actions.cancel', 'Cancel')}</Link>
          </div>
          {create.isError && <ErrorState error={create.error} />}
        </form>
      </Card>
    </>
  )
}

// ---- Detail ----------------------------------------------------------------

function HomeworkDetail({ userId, homeworkId, locale }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const hw = useTeacherQuery(queryKeys.teacher.homeworkItem(userId, homeworkId), (signal) => teacherApi.homework(homeworkId, signal), { enabled: Boolean(homeworkId) })
  const data = hw.data || {}
  const statusName = assignmentStatusName(data.status ?? data.Status)
  const isDraft = statusName.toLowerCase() === 'draft'

  const invalidate = () => {
    qc.invalidateQueries({ queryKey: queryKeys.teacher.homeworkItem(userId, homeworkId) })
    qc.invalidateQueries({ queryKey: queryKeys.teacher.homework(userId) })
    qc.invalidateQueries({ queryKey: queryKeys.teacher.dashboard(userId) })
  }

  if (hw.isLoading) return (<><PageHeader title={t('teacher.homework.details', 'Homework')} /><Loading /></>)
  if (hw.isError) return <ErrorState error={hw.error} onRetry={hw.refetch} />

  return (
    <>
      <PageHeader title={displayValue(data, ['title', 'Title']) || t('teacher.homework.details', 'Homework')} description={t('teacher.homework.reviewDescription', 'Edit, publish and grade')} />
      <Card title={t('teacher.details', 'Details')}>
        <Alert variant={isDraft ? 'info' : 'success'} title={`${t('teacher.homework.status', 'Status')}: ${statusName}`} />
        <DetailList item={data} locale={locale} />
      </Card>

      {isDraft && <HomeworkEditCard homeworkId={homeworkId} data={data} onSaved={invalidate} />}
      {isDraft && <HomeworkPublishCard userId={userId} homeworkId={homeworkId} onPublished={invalidate} />}

      <SubmissionsCard userId={userId} homeworkId={homeworkId} maxScore={data.maxScore ?? data.MaxScore} />
    </>
  )
}

function HomeworkEditCard({ homeworkId, data, onSaved }) {
  const { t } = useTranslation()
  const toLocalInput = (v) => {
    if (!v) return ''
    const d = new Date(v)
    if (Number.isNaN(d.getTime())) return ''
    const pad = (n) => String(n).padStart(2, '0')
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`
  }
  const [form, setForm] = useState({
    title: data.title ?? data.Title ?? '',
    description: data.description ?? data.Description ?? '',
    maxScore: (data.maxScore ?? data.MaxScore) ?? '',
    dueDate: toLocalInput(data.dueDate ?? data.DueDate),
  })
  const save = useMutation({
    mutationFn: () => teacherApi.updateHomework(homeworkId, {
      title: form.title.trim(),
      description: form.description.trim() || null,
      maxScore: form.maxScore === '' ? null : Number(form.maxScore),
      dueDate: form.dueDate ? new Date(form.dueDate).toISOString() : null,
    }),
    onSuccess: onSaved,
  })
  return (
    <Card title={t('teacher.homework.edit', 'Edit draft')}>
      <form className="stack" onSubmit={(e) => { e.preventDefault(); if (form.title.trim()) save.mutate() }}>
        <TextField label={t('teacher.homework.field.title', 'Title')} value={form.title} onChange={(e) => setForm((f) => ({ ...f, title: e.target.value }))} required />
        <TextareaField label={t('teacher.homework.field.description', 'Description')} value={form.description} onChange={(e) => setForm((f) => ({ ...f, description: e.target.value }))} maxLength={4000} />
        <TextField label={t('teacher.homework.field.maxScore', 'Max score')} type="number" min="0" value={form.maxScore} onChange={(e) => setForm((f) => ({ ...f, maxScore: e.target.value }))} />
        <TextField label={t('teacher.homework.field.dueDate', 'Due date')} type="datetime-local" value={form.dueDate} onChange={(e) => setForm((f) => ({ ...f, dueDate: e.target.value }))} />
        <Button type="submit" loading={save.isPending} disabled={!form.title.trim()}>{t('actions.save', 'Save')}</Button>
        {save.isError && <ErrorState error={save.error} />}
        {save.isSuccess && <Alert variant="success" title={t('teacher.homework.saved', 'Saved')} />}
      </form>
    </Card>
  )
}

function HomeworkPublishCard({ userId, homeworkId, onPublished }) {
  const { t } = useTranslation()
  const classes = useTeacherQuery(queryKeys.teacher.classes(userId), (signal) => teacherApi.classes(signal), { staleTime: STALE.medium })
  const [classId, setClassId] = useState('')
  const publish = useMutation({
    mutationFn: () => teacherApi.publishHomework(homeworkId, { schoolClassId: classId, studentIds: [] }),
    onSuccess: onPublished,
  })
  const classItems = classes.data || []
  return (
    <Card title={t('teacher.homework.publish', 'Publish to a class')}>
      <form className="stack" onSubmit={(e) => { e.preventDefault(); if (classId) publish.mutate() }}>
        <SelectField
          label={t('teacher.homework.targetClass', 'Class')}
          value={classId}
          onChange={(e) => setClassId(e.target.value)}
          options={[{ value: '', label: t('teacher.homework.chooseClass', 'Choose a class…') }, ...classItems.map((c) => ({ value: itemId(c, ['classId', 'ClassId', 'id', 'Id']), label: displayValue(c, ['name', 'Name']) }))]}
        />
        <Button type="submit" loading={publish.isPending} disabled={!classId}>{t('teacher.homework.publishButton', 'Publish')}</Button>
        {publish.isError && <ErrorState error={publish.error} />}
        {publish.isSuccess && <Alert variant="success" title={t('teacher.homework.publishedTitle', 'Published')}>{t('teacher.homework.publishedBody', 'Students in the class can now see and submit this homework.')}</Alert>}
      </form>
    </Card>
  )
}

function SubmissionsCard({ userId, homeworkId, maxScore }) {
  const { t } = useTranslation()
  const submissions = useTeacherQuery(queryKeys.teacher.homeworkSubmissions(userId, homeworkId), (signal) => teacherApi.homeworkSubmissions(homeworkId, signal), { enabled: Boolean(homeworkId) })
  return (
    <Card title={t('teacher.homework.submissions', 'Submissions')}>
      <Listing query={submissions} empty={t('teacher.empty.submissions', 'No submissions yet')}>
        {(items) => (
          <div className="student-list">
            {items.map((s) => (
              <SubmissionRow key={itemId(s, ['id', 'Id'])} userId={userId} homeworkId={homeworkId} submission={s} maxScore={maxScore} />
            ))}
          </div>
        )}
      </Listing>
    </Card>
  )
}

function SubmissionRow({ userId, homeworkId, submission, maxScore }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const submissionId = itemId(submission, ['id', 'Id'])
  const statusName = submissionStatusName(submission.status ?? submission.Status)
  const isGraded = statusName.toLowerCase() === 'graded'
  const attachmentFileId = submission.attachmentFileId ?? submission.AttachmentFileId
  const [open, setOpen] = useState(false)
  const [score, setScore] = useState(submission.score ?? submission.Score ?? '')
  const [feedback, setFeedback] = useState(submission.feedback ?? submission.Feedback ?? '')
  const grade = useMutation({
    mutationFn: () => teacherApi.gradeHomeworkSubmission(submissionId, { score: Number(score), feedback: feedback.trim() || null }),
    onSuccess: () => {
      setOpen(false)
      qc.invalidateQueries({ queryKey: queryKeys.teacher.homeworkSubmissions(userId, homeworkId) })
      qc.invalidateQueries({ queryKey: queryKeys.teacher.homeworkItem(userId, homeworkId) })
    },
  })
  return (
    <div className="student-list__item">
      <div className="student-item">
        <div>
          <strong className="domain-row__title">{displayValue(submission, ['studentName', 'StudentName', 'studentId', 'StudentId'])}</strong>
          <div className="cluster">
            <Chip tone={isGraded ? 'success' : 'info'}>{statusName}{isGraded && (submission.score ?? submission.Score) != null ? ` · ${submission.score ?? submission.Score}${maxScore != null ? `/${maxScore}` : ''}` : ''}</Chip>
            {attachmentFileId && <Chip tone="muted">{t('teacher.homework.hasAttachment', 'File attached')}</Chip>}
          </div>
        </div>
        <Button variant="secondary" onClick={() => setOpen((o) => !o)}>{isGraded ? t('teacher.homework.regrade', 'Update grade') : t('teacher.homework.grade', 'Grade')}</Button>
      </div>
      {(submission.content ?? submission.Content) && <p className="ui-muted">{submission.content ?? submission.Content}</p>}
      {open && (
        <form className="stack" onSubmit={(e) => { e.preventDefault(); if (score !== '') grade.mutate() }}>
          <TextField label={t('teacher.homework.score', 'Score')} type="number" min="0" value={score} onChange={(e) => setScore(e.target.value)} required />
          <TextareaField label={t('teacher.homework.feedback', 'Feedback')} value={feedback} onChange={(e) => setFeedback(e.target.value)} maxLength={2000} />
          <div className="student-actions">
            <Button type="submit" loading={grade.isPending} disabled={score === ''}>{t('actions.save', 'Save')}</Button>
            <Button type="button" variant="secondary" onClick={() => setOpen(false)}>{t('actions.cancel', 'Cancel')}</Button>
          </div>
          {grade.isError && <ErrorState error={grade.error} />}
        </form>
      )}
    </div>
  )
}

// ---- Router shell -----------------------------------------------------------

function HomeworkRouter({ userId, locale }) {
  const { homeworkId } = useParams()
  const { pathname } = useLocation()
  if (homeworkId) return <HomeworkDetail userId={userId} homeworkId={homeworkId} locale={locale} />
  if (pathname.endsWith('/homework/new')) return <HomeworkNew userId={userId} />
  return <HomeworkList userId={userId} />
}

export default function TeacherHomeworkPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <HomeworkRouter userId={userId} locale={locale} {...props} />
}
