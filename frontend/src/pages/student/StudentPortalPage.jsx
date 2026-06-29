import { useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Bot, CheckCircle2, FileText } from 'lucide-react'
import { PageHeader, Card } from '../../components/ui/PageHeader'
import { EmptyState, ErrorState } from '../../components/ui/states'
import { Button } from '../../components/ui/Button'
import { Alert } from '../../components/ui/Alert'
import { TextField, TextareaField } from '../../components/form/fields'
import { useAuth } from '../../features/auth/AuthContext'
import { studentApi } from '../../features/student/studentApi'
import { answerPayloadFromForm, displayValue, formatDate, itemId, settledData, settledError, statusLabel } from '../../features/student/studentUtils'
import { queryKeys, STALE } from '../../lib/query/keys'
import { toItems, toObject } from '../../features/student/studentSchemas'

function useStudentQuery(key, fn, options = {}) {
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

function QueryCard({ title, query, empty, children }) {
  if (query.isLoading) return <Card title={title}><p role="status">Loading...</p></Card>
  if (query.isError) return <Card title={title}><ErrorState error={query.error} onRetry={query.refetch} /></Card>
  return <Card title={title}>{children(query.data) || <EmptyState title={empty} />}</Card>
}

function DetailGrid({ item, locale }) {
  if (!item) return null
  const fields = Object.entries(item).filter(([, value]) => value !== null && value !== undefined && typeof value !== 'object')
  return (
    <dl className="student-grid">
      {fields.slice(0, 12).map(([key, value]) => (
        <Field key={key} label={key} value={String(key).toLowerCase().includes('date') || String(key).toLowerCase().includes('at') ? formatDate(value, locale) : statusLabel(value)} />
      ))}
    </dl>
  )
}

function DashboardPage({ userId, locale }) {
  const { t } = useTranslation()
  const subjects = useStudentQuery(queryKeys.student.subjects(userId), (signal) => studentApi.subjects(signal), { staleTime: STALE.medium })
  const homework = useStudentQuery(queryKeys.student.homework(userId), (signal) => studentApi.homework(signal))
  const quizzes = useStudentQuery(queryKeys.student.quizzes(userId), (signal) => studentApi.assignedQuizzes(signal))
  const progress = useStudentQuery(queryKeys.student.progress(userId), (signal) => studentApi.progress(userId, signal))
  const notifications = useStudentQuery(queryKeys.student.notifications(userId), (signal) => studentApi.notifications(signal))
  const office = useStudentQuery(queryKeys.student.officeHours(userId), (signal) => studentApi.officeHours(signal))
  const badges = useStudentQuery(queryKeys.student.badges(userId), (signal) => studentApi.badges(userId, signal), { staleTime: STALE.medium })
  const communities = useStudentQuery(queryKeys.student.communities(userId), (signal) => studentApi.communities(signal), { staleTime: STALE.medium })
  const competitions = useStudentQuery(queryKeys.student.competitions(userId), (signal) => studentApi.competitions(signal), { staleTime: STALE.medium })

  const progressRecommendations = progress.data ? toItems(settledData(progress.data.recommendations)) : []
  const unread = notifications.data ? toObject(settledData(notifications.data.count))?.unreadCount : null
  const officeAvailable = office.data ? toItems(settledData(office.data.available)) : []
  const earnedBadges = badges.data ? toItems(settledData(badges.data.earned)) : []

  return (
    <>
      <PageHeader title={t('student.dashboard.title')} description={t('student.dashboard.description')} />
      <div className="student-dashboard">
        <QueryCard title={t('student.subjects.title')} query={subjects} empty={t('student.empty.subjects')}>{(items) => <MetricLink to="/app/student/subjects" value={items.length} label={t('student.dashboard.assignedSubjects')} />}</QueryCard>
        <QueryCard title={t('student.homework.title')} query={homework} empty={t('student.empty.homework')}>{(items) => <MetricLink to="/app/student/homework" value={items.length} label={t('student.dashboard.upcomingHomework')} />}</QueryCard>
        <QueryCard title={t('student.quizzes.title')} query={quizzes} empty={t('student.empty.quizzes')}>{(items) => <MetricLink to="/app/student/quizzes" value={items.length} label={t('student.dashboard.assignedQuizzes')} />}</QueryCard>
        <Card title={t('student.notifications.title')}><MetricLink to="/app/student/notifications" value={unread ?? '—'} label={t('student.dashboard.unreadNotifications')} /></Card>
        <Card title={t('student.recommendations.title')}><MetricLink to="/app/student/recommendations" value={progressRecommendations.length} label={t('student.dashboard.activeRecommendations')} /></Card>
        <Card title={t('student.officeHours.title')}><MetricLink to="/app/student/office-hours" value={officeAvailable.length} label={t('student.dashboard.availableOfficeHours')} /></Card>
        <Card title={t('student.badges.title')}><MetricLink to="/app/student/badges" value={earnedBadges.length} label={t('student.dashboard.earnedBadges')} /></Card>
        <QueryCard title={t('student.communities.title')} query={communities} empty={t('student.empty.communities')}>{(items) => <MetricLink to="/app/student/communities" value={items.length} label={t('student.dashboard.availableCommunities')} />}</QueryCard>
        <QueryCard title={t('student.competitions.title')} query={competitions} empty={t('student.empty.competitions')}>{(items) => <MetricLink to="/app/student/competitions" value={items.length} label={t('student.dashboard.activeCompetitions')} />}</QueryCard>
      </div>
      <Card title={t('student.progress.title')} description={t('student.progress.description')}>
        {progress.isError ? <ErrorState error={progress.error} onRetry={progress.refetch} /> : <ProgressSections progress={progress.data} locale={locale} />}
      </Card>
    </>
  )
}

function MetricLink({ to, value, label }) {
  return <Link className="student-metric" to={to}><strong>{value}</strong><span>{label}</span></Link>
}

function SubjectsPage({ userId }) {
  const { t } = useTranslation()
  const query = useStudentQuery(queryKeys.student.subjects(userId), (signal) => studentApi.subjects(signal), { staleTime: STALE.medium })
  return <ListPage title={t('student.subjects.title')} description={t('student.subjects.description')} query={query} empty={t('student.empty.subjects')} itemLink={(item) => `/app/student/subjects/${itemId(item)}`} />
}

function SubjectDetailsPage({ userId, locale }) {
  const { t } = useTranslation()
  const { subjectId } = useParams()
  const subject = useStudentQuery(queryKeys.student.subject(userId, subjectId), (signal) => studentApi.subject(subjectId, signal), { enabled: Boolean(subjectId), staleTime: STALE.medium })
  const units = useStudentQuery(queryKeys.student.units(userId, subjectId), (signal) => studentApi.units(subjectId, signal), { enabled: Boolean(subjectId), staleTime: STALE.medium })
  return (
    <>
      <PageHeader title={displayValue(subject.data) || t('student.subjects.details')} description={t('student.subjects.secureDescription')} />
      {subject.isError && <ErrorState error={subject.error} onRetry={subject.refetch} />}
      <Card title={t('student.details')}><DetailGrid item={subject.data} locale={locale} /></Card>
      <QueryCard title={t('student.units.title')} query={units} empty={t('student.empty.units')}>{(items) => <ItemsList items={items} empty={t('student.empty.units')} renderItem={(item) => <LinkedItem item={item} to={`/app/student/units/${itemId(item)}`} />} />}</QueryCard>
    </>
  )
}

function UnitsPage({ userId }) {
  const { t } = useTranslation()
  const { unitId } = useParams()
  const lessons = useStudentQuery(queryKeys.student.lessons(userId, unitId), (signal) => studentApi.lessons(unitId, signal), { enabled: Boolean(unitId), staleTime: STALE.medium })
  if (!unitId) return <SubjectsPage userId={userId} />
  return <ListPage title={t('student.lessons.title')} description={t('student.lessons.description')} query={lessons} empty={t('student.empty.lessons')} itemLink={(item) => `/app/student/lessons/${itemId(item)}`} />
}

function LessonsPage({ userId, locale }) {
  const { t } = useTranslation()
  const { lessonId } = useParams()
  const qc = useQueryClient()
  const materials = useStudentQuery(queryKeys.student.materials(userId, lessonId), (signal) => studentApi.materials(lessonId, signal), { enabled: Boolean(lessonId), staleTime: STALE.medium })
  const progress = useStudentQuery(queryKeys.student.progress(userId), (signal) => studentApi.progress(userId, signal), { enabled: Boolean(lessonId) })
  const complete = useMutation({
    mutationFn: () => studentApi.completeLesson(lessonId),
    onSuccess: () => qc.invalidateQueries({ queryKey: queryKeys.student.root(userId) }),
  })
  const lessonProgress = toItems(settledData(progress.data?.lessons)).find((item) => item.lessonId === lessonId || item.LessonId === lessonId)
  const isCompleted = Boolean(lessonProgress?.isCompleted ?? lessonProgress?.IsCompleted ?? complete.data?.isCompleted)
  if (!lessonId) return <PageHeader title={t('student.lessons.title')} description={t('student.lessons.chooseUnit')} />
  return (
    <>
      <PageHeader title={t('student.lessons.details')} description={t('student.lessons.progressRule')} actions={<Button onClick={() => complete.mutate()} loading={complete.isPending} disabled={complete.isPending || isCompleted}>{isCompleted ? t('student.lessons.completed') : t('student.lessons.markComplete')}</Button>} />
      <Card title={t('student.details')}><DetailGrid item={{ lessonId }} locale={locale} /></Card>
      {isCompleted && <Alert variant="success" title={t('student.lessons.completed')}>{t('student.lessons.completedBody')}</Alert>}
      {complete.isSuccess && <Alert variant="success" title={t('student.lessons.saved')}>{t('student.lessons.savedBody')}</Alert>}
      {complete.isError && <ErrorState error={complete.error} />}
      <QueryCard title={t('student.materials.title')} query={materials} empty={t('student.empty.materials')}>{(items) => <ItemsList items={items} empty={t('student.empty.materials')} renderItem={(item) => <LinkedItem item={item} to={`/app/student/materials/${itemId(item)}`} />} />}</QueryCard>
    </>
  )
}

function MaterialsPage({ userId, locale }) {
  const { t } = useTranslation()
  const { materialId } = useParams()
  const comments = useStudentQuery(queryKeys.student.comments(userId, materialId), (signal) => studentApi.resourceComments(materialId, signal), { enabled: Boolean(materialId) })
  const qc = useQueryClient()
  const [body, setBody] = useState('')
  const mutation = useMutation({ mutationFn: () => studentApi.addResourceComment(materialId, body), onSuccess: () => { setBody(''); qc.invalidateQueries({ queryKey: queryKeys.student.comments(userId, materialId) }) } })
  return (
    <>
      <PageHeader title={t('student.materials.details')} description={t('student.materials.description')} />
      <Card title={t('student.details')}><DetailGrid item={{ materialId }} locale={locale} /></Card>
      <Card title={t('student.comments.title')}>
        <form onSubmit={(event) => { event.preventDefault(); if (body.trim()) mutation.mutate() }}>
          <TextareaField label={t('student.comments.add')} value={body} onChange={(e) => setBody(e.target.value)} maxLength={1000} />
          <Button type="submit" loading={mutation.isPending} disabled={!body.trim()}>{t('actions.submit')}</Button>
        </form>
        {mutation.isError && <ErrorState error={mutation.error} />}
        <QueryResult query={comments} empty={t('student.empty.comments')}>{(items) => <ItemsList items={items} empty={t('student.empty.comments')} renderItem={(item) => <PlainItem item={item} locale={locale} />} />}</QueryResult>
      </Card>
    </>
  )
}

function HomeworkPage({ userId, locale }) {
  const { t } = useTranslation()
  const { homeworkId } = useParams()
  const list = useStudentQuery(queryKeys.student.homework(userId), (signal) => studentApi.homework(signal))
  if (!homeworkId) return <ListPage title={t('student.homework.title')} description={t('student.homework.description')} query={list} empty={t('student.empty.homework')} itemLink={(item) => `/app/student/homework/${itemId(item)}`} />
  return <HomeworkDetails userId={userId} homeworkId={homeworkId} list={list} locale={locale} />
}

function HomeworkDetails({ userId, homeworkId, list, locale }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const [content, setContent] = useState('')
  const submission = useStudentQuery(queryKeys.student.homeworkSubmission(userId, homeworkId), (signal) => studentApi.homeworkSubmission(homeworkId, signal))
  const mutation = useMutation({ mutationFn: () => studentApi.submitHomework(homeworkId, content), onSuccess: () => { setContent(''); qc.invalidateQueries({ queryKey: queryKeys.student.homework(userId) }); qc.invalidateQueries({ queryKey: queryKeys.student.homeworkSubmission(userId, homeworkId) }) } })
  const item = list.data?.find((h) => itemId(h) === homeworkId)
  return (
    <>
      <PageHeader title={displayValue(item) || t('student.homework.details')} description={t('student.homework.submitDescription')} />
      {list.isError && <ErrorState error={list.error} onRetry={list.refetch} />}
      <Card title={t('student.details')}><DetailGrid item={item || { homeworkId }} locale={locale} /></Card>
      <Card title={t('student.homework.submission')}>
        <QueryResult query={submission} empty={t('student.homework.noSubmission')}>{(data) => <DetailGrid item={data} locale={locale} />}</QueryResult>
        <form onSubmit={(event) => { event.preventDefault(); if (content.trim()) mutation.mutate() }}>
          <TextareaField label={t('student.homework.answer')} value={content} onChange={(e) => setContent(e.target.value)} maxLength={4000} />
          <Button type="submit" loading={mutation.isPending} disabled={!content.trim()}>{t('student.homework.submit')}</Button>
        </form>
        {mutation.isError && <ErrorState error={mutation.error} />}
      </Card>
    </>
  )
}

function QuizzesPage({ userId, locale }) {
  const { t } = useTranslation()
  const { quizId } = useParams()
  const query = useStudentQuery(queryKeys.student.quizzes(userId), (signal) => studentApi.assignedQuizzes(signal))
  if (!quizId) return <ListPage title={t('student.quizzes.title')} description={t('student.quizzes.description')} query={query} empty={t('student.empty.quizzes')} itemLink={(item) => `/app/student/quizzes/${itemId(item, ['quizId', 'QuizId', 'id', 'Id'])}`} />
  return <QuizDetails userId={userId} quizId={quizId} list={query} locale={locale} />
}

function QuizDetails({ userId, quizId, list, locale }) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const qc = useQueryClient()
  const history = useStudentQuery(queryKeys.student.previousAttempts(userId, quizId), (signal) => studentApi.previousAttempts(quizId, signal))
  const start = useMutation({ mutationFn: () => studentApi.startQuiz(quizId), onSuccess: (attempt) => { qc.invalidateQueries({ queryKey: queryKeys.student.quizzes(userId) }); navigate(`/app/student/quiz-attempts/${itemId(attempt)}`) } })
  const item = list.data?.find((q) => itemId(q, ['quizId', 'QuizId', 'id', 'Id']) === quizId)
  return (
    <>
      <PageHeader title={displayValue(item) || t('student.quizzes.details')} description={t('student.quizzes.instructions')} actions={<Button onClick={() => start.mutate()} loading={start.isPending}>{t('student.quizzes.start')}</Button>} />
      {start.isError && <ErrorState error={start.error} />}
      <Card title={t('student.details')}><DetailGrid item={item || { quizId }} locale={locale} /></Card>
      <QueryCard title={t('student.quizzes.previousAttempts')} query={history} empty={t('student.empty.attempts')}>{(items) => <ItemsList items={items} empty={t('student.empty.attempts')} renderItem={(item) => <LinkedItem item={item} to={`/app/student/quiz-attempts/${itemId(item)}/result`} />} />}</QueryCard>
    </>
  )
}

function QuizAttemptPage({ userId, locale }) {
  const { t } = useTranslation()
  const { attemptId } = useParams()
  const navigate = useNavigate()
  const qc = useQueryClient()
  const query = useStudentQuery(queryKeys.student.attempt(userId, attemptId), (signal) => studentApi.attempt(attemptId, signal), { enabled: Boolean(attemptId) })
  const save = useMutation({ mutationFn: (answers) => studentApi.saveAttempt(attemptId, answers) })
  const submit = useMutation({ mutationFn: () => studentApi.submitAttempt(attemptId), onSuccess: () => { qc.invalidateQueries({ queryKey: queryKeys.student.attemptResult(userId, attemptId) }); navigate(`/app/student/quiz-attempts/${attemptId}/result`) } })
  if (query.isLoading) return <PageHeader title={t('student.quizzes.attempt')} description="Loading..." />
  if (query.isError) return <ErrorState error={query.error} onRetry={query.refetch} />
  const questions = Array.isArray(query.data?.questions || query.data?.Questions) ? query.data.questions || query.data.Questions : []
  // Previously-saved answers, keyed by question id, so a reloaded attempt shows
  // the student's work (the backend remains the source of truth).
  const savedAnswers = {}
  for (const a of query.data?.answers || query.data?.Answers || []) {
    const qid = a.questionId || a.QuestionId
    if (qid) savedAnswers[qid] = a
  }
  return (
    <>
      <PageHeader title={t('student.quizzes.attempt')} description={t('student.quizzes.noCorrectAnswers')} />
      <form onSubmit={(event) => { event.preventDefault(); const answers = answerPayloadFromForm(new FormData(event.currentTarget)); save.mutate(answers) }}>
        {questions.length ? questions.map((q) => <QuestionBlock key={itemId(q)} question={q} saved={savedAnswers[itemId(q)]} />) : <DetailGrid item={query.data} locale={locale} />}
        <div className="student-actions">
          <Button type="submit" variant="secondary" loading={save.isPending}>{t('student.quizzes.save')}</Button>
          <Button type="button" loading={submit.isPending} onClick={() => submit.mutate()}>{t('student.quizzes.submit')}</Button>
        </div>
      </form>
      {(save.isError || submit.isError) && <ErrorState error={save.error || submit.error} />}
    </>
  )
}

function QuestionBlock({ question, saved }) {
  const questionId = itemId(question)
  const options = question.options || question.Options || []
  const savedOptionId = saved?.selectedOptionId ?? saved?.SelectedOptionId ?? null
  const savedText = saved?.answerText ?? saved?.AnswerText ?? ''
  return (
    <Card title={displayValue(question, ['text', 'Text', 'title', 'Title'])}>
      {options.map((option) => <label className="student-answer" key={itemId(option)}><input type="radio" name={`q:${questionId}`} value={itemId(option)} defaultChecked={itemId(option) === savedOptionId} /> <span>{displayValue(option, ['text', 'Text', 'label', 'Label'])}</span></label>)}
      {!options.length && <TextareaField label="Answer" name={`qt:${questionId}`} defaultValue={savedText} />}
    </Card>
  )
}

function QuizResultPage({ userId, locale }) {
  const { t } = useTranslation()
  const { attemptId } = useParams()
  const query = useStudentQuery(queryKeys.student.attemptResult(userId, attemptId), (signal) => studentApi.attemptResult(attemptId, signal), { enabled: Boolean(attemptId) })
  return <SingleDataPage title={t('student.quizzes.result')} description={t('student.quizzes.serverScore')} query={query} locale={locale} />
}

function TutorPage() {
  const { t, i18n } = useTranslation()
  const [message, setMessage] = useState('')
  const [subject, setSubject] = useState('')
  const mutation = useMutation({ mutationFn: () => studentApi.tutor({ message, subject: subject || undefined, language: i18n.language }) })
  return (
    <>
      <PageHeader title={t('student.tutor.title')} description={t('student.tutor.description')} />
      <Card title={t('student.tutor.ask')}>
        <form onSubmit={(event) => { event.preventDefault(); if (message.trim()) mutation.mutate() }}>
          <TextField label={t('student.tutor.context')} value={subject} onChange={(e) => setSubject(e.target.value)} />
          <TextareaField label={t('student.tutor.question')} value={message} onChange={(e) => setMessage(e.target.value)} required maxLength={2000} />
          <Button type="submit" loading={mutation.isPending} disabled={!message.trim()}>{t('student.tutor.send')}</Button>
        </form>
      </Card>
      {mutation.isError && <ErrorState error={mutation.error} />}
      {mutation.data && <TutorAnswer response={mutation.data} />}
    </>
  )
}

function TutorAnswer({ response }) {
  const { t } = useTranslation()
  return (
    <Card title={response.grounded ? t('student.tutor.grounded') : t('student.tutor.noAnswer')}>
      <p>{response.answer || response.noAnswerReason}</p>
      {response.correlationId && <p className="ui-alert__meta">{t('errors.referenceId')}: <code>{response.correlationId}</code></p>}
      <h3>{t('student.tutor.citations')}</h3>
      <ItemsList items={response.citations} empty={t('student.tutor.noCitations')} renderItem={(item) => <PlainItem item={item} />} />
    </Card>
  )
}

function ProgressPage({ userId, locale, mode = 'progress' }) {
  const { t } = useTranslation()
  const query = useStudentQuery(queryKeys.student.progress(userId), (signal) => studentApi.progress(userId, signal))
  return (
    <>
      <PageHeader title={t(`student.${mode}.title`)} description={t(`student.${mode}.description`)} />
      {query.isLoading && <p role="status">Loading...</p>}
      {query.isError && <ErrorState error={query.error} onRetry={query.refetch} />}
      {query.data && <ProgressSections progress={query.data} locale={locale} mode={mode} />}
    </>
  )
}

function ProgressSections({ progress, locale, mode }) {
  const { t } = useTranslation()
  const sections = mode === 'recommendations'
    ? [['recommendations', t('student.recommendations.title')], ['predictions', t('student.predictions')]]
    : mode === 'engagement'
      ? [['metrics', t('student.engagement.metrics')], ['insights', t('student.engagement.insights')], ['painPoints', t('student.engagement.painPoints')]]
      : [['summary', t('student.progress.summary')], ['subjects', t('student.progress.subjectProgress')], ['lessons', t('student.progress.lessonProgress')], ['attempts', t('student.progress.attemptHistory')]]
  return <>{sections.map(([key, title]) => <PartialSection key={key} title={title} result={progress?.[key]} locale={locale} />)}</>
}

function PartialSection({ title, result, locale }) {
  const { t } = useTranslation()
  const error = settledError(result)
  const data = settledData(result)
  const items = toItems(data)
  const object = toObject(data)
  return <Card title={title}>{error ? <ErrorState error={error} /> : items.length ? <ItemsList items={items} empty={t('states.emptyTitle')} renderItem={(item) => <PlainItem item={item} locale={locale} />} /> : object ? <DetailGrid item={object} locale={locale} /> : <EmptyState title={t('student.empty.noData')} />}</Card>
}

function AttendancePage({ userId, locale }) {
  const { t } = useTranslation()
  const query = useStudentQuery(queryKeys.student.attendance(userId), (signal) => studentApi.attendance(signal))
  return (
    <>
      <PageHeader title={t('student.attendance.title')} description={t('student.attendance.description')} />
      <Alert title={t('student.attendance.noCvTitle')}>{t('student.attendance.noCvBody')}</Alert>
      {query.isLoading && <p role="status">{t('states.loading')}</p>}
      {query.isError && <ErrorState error={query.error} onRetry={query.refetch} />}
      {query.data && <AttendanceContent data={query.data} locale={locale} />}
    </>
  )
}

function AttendanceContent({ data, locale }) {
  const { t } = useTranslation()
  const records = data.records || []
  const summary = data.summary || {}
  return (
    <>
      <Card title={t('student.attendance.summary')}>
        <dl className="student-grid">
          <Field label={t('student.attendance.total')} value={summary.total ?? 0} />
          <Field label={t('student.attendance.present')} value={summary.present ?? 0} />
          <Field label={t('student.attendance.absent')} value={summary.absent ?? 0} />
          <Field label={t('student.attendance.late')} value={summary.late ?? 0} />
          <Field label={t('student.attendance.excused')} value={summary.excused ?? 0} />
          <Field label={t('student.attendance.percentage')} value={`${summary.attendancePercentage ?? 0}%`} />
        </dl>
      </Card>
      <Card title={t('student.attendance.records')}>
        <ItemsList items={records} empty={t('student.attendance.empty')} renderItem={(item) => <AttendanceItem item={item} locale={locale} />} />
      </Card>
    </>
  )
}

function AttendanceItem({ item, locale }) {
  const { t } = useTranslation()
  return (
    <div className="student-item">
      <div>
        <strong>{formatDate(item.attendanceDate, locale)}</strong>
        <p><span className="student-status" aria-label={`${t('student.attendance.status')}: ${item.status}`}>{item.status}</span></p>
        <DetailGrid item={{ source: item.source, sessionKey: item.sessionKey, schoolClassId: item.schoolClassId, recordedAt: item.recordedAt, notes: item.notes }} locale={locale} />
      </div>
    </div>
  )
}

function CommunitiesPage({ userId, locale }) {
  const { t } = useTranslation()
  const { communityId } = useParams()
  const query = useStudentQuery(queryKeys.student.communities(userId), (signal) => studentApi.communities(signal), { staleTime: STALE.medium })
  if (!communityId) return <ListPage title={t('student.communities.title')} description={t('student.communities.description')} query={query} empty={t('student.empty.communities')} itemLink={(item) => `/app/student/communities/${itemId(item)}`} />
  return <CommunityDetails userId={userId} communityId={communityId} locale={locale} />
}

function CommunityDetails({ userId, communityId, locale }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const [content, setContent] = useState('')
  const community = useStudentQuery(queryKeys.student.community(userId, communityId), (signal) => studentApi.community(communityId, signal), { staleTime: STALE.medium })
  const posts = useStudentQuery(queryKeys.student.communityPosts(userId, communityId), (signal) => studentApi.communityPosts(communityId, signal))
  const join = useMutation({ mutationFn: () => studentApi.joinCommunity(communityId), onSuccess: () => qc.invalidateQueries({ queryKey: queryKeys.student.community(userId, communityId) }) })
  const post = useMutation({ mutationFn: () => studentApi.createCommunityPost(communityId, content), onSuccess: () => { setContent(''); qc.invalidateQueries({ queryKey: queryKeys.student.communityPosts(userId, communityId) }) } })
  return (
    <>
      <PageHeader title={displayValue(community.data) || t('student.communities.details')} description={t('student.communities.safeRendering')} actions={<Button onClick={() => join.mutate()} loading={join.isPending}>{t('student.communities.join')}</Button>} />
      {(community.isError || join.isError) && <ErrorState error={community.error || join.error} onRetry={community.refetch} />}
      <Card title={t('student.details')}><DetailGrid item={community.data || { communityId }} locale={locale} /></Card>
      <Card title={t('student.communities.posts')}>
        <form onSubmit={(event) => { event.preventDefault(); if (content.trim()) post.mutate() }}>
          <TextareaField label={t('student.communities.newPost')} value={content} onChange={(e) => setContent(e.target.value)} maxLength={2000} />
          <Button type="submit" loading={post.isPending} disabled={!content.trim()}>{t('actions.submit')}</Button>
        </form>
        {post.isError && <ErrorState error={post.error} />}
        <QueryResult query={posts} empty={t('student.empty.posts')}>{(items) => <ItemsList items={items} empty={t('student.empty.posts')} renderItem={(item) => <PlainItem item={item} locale={locale} />} />}</QueryResult>
      </Card>
    </>
  )
}

function CompetitionsPage({ userId, locale }) {
  const { t } = useTranslation()
  const { competitionId } = useParams()
  const query = useStudentQuery(queryKeys.student.competitions(userId), (signal) => studentApi.competitions(signal), { staleTime: STALE.medium })
  if (!competitionId) return <ListPage title={t('student.competitions.title')} description={t('student.competitions.description')} query={query} empty={t('student.empty.competitions')} itemLink={(item) => `/app/student/competitions/${itemId(item)}`} />
  return <CompetitionDetails userId={userId} competitionId={competitionId} locale={locale} />
}

function CompetitionDetails({ userId, competitionId, locale }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const competition = useStudentQuery(queryKeys.student.competition(userId, competitionId), (signal) => studentApi.competition(competitionId, signal), { staleTime: STALE.medium })
  const leaderboard = useStudentQuery(queryKeys.student.leaderboard(userId, competitionId), (signal) => studentApi.leaderboard(competitionId, signal))
  const enter = useMutation({ mutationFn: () => studentApi.enterCompetition(competitionId), onSuccess: () => qc.invalidateQueries({ queryKey: queryKeys.student.competition(userId, competitionId) }) })
  return (
    <>
      <PageHeader title={displayValue(competition.data) || t('student.competitions.details')} description={t('student.competitions.authoritative')} actions={<Button onClick={() => enter.mutate()} loading={enter.isPending}>{t('student.competitions.enter')}</Button>} />
      {(competition.isError || enter.isError) && <ErrorState error={competition.error || enter.error} onRetry={competition.refetch} />}
      <Card title={t('student.details')}><DetailGrid item={competition.data || { competitionId }} locale={locale} /></Card>
      <CompetitionSubmissionCard userId={userId} competitionId={competitionId} locale={locale} />
      <QueryCard title={t('student.leaderboard.title')} query={leaderboard} empty={t('student.empty.leaderboard')}>{(items) => <ItemsList items={items} empty={t('student.empty.leaderboard')} renderItem={(item) => <PlainItem item={item} locale={locale} />} />}</QueryCard>
    </>
  )
}

// Phase 14 (closure) — durable competition submission. The student submits/updates their work for an
// entered competition; the backend enforces entry, the open window and tenant/role rules.
function CompetitionSubmissionCard({ userId, competitionId, locale }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const submission = useStudentQuery(queryKeys.student.competitionSubmission(userId, competitionId), (signal) => studentApi.competitionSubmission(competitionId, signal), { staleTime: STALE.short })
  const [content, setContent] = useState('')
  const submit = useMutation({
    mutationFn: () => studentApi.submitCompetition(competitionId, content.trim()),
    onSuccess: () => { setContent(''); qc.invalidateQueries({ queryKey: queryKeys.student.competitionSubmission(userId, competitionId) }) },
  })
  const existing = submission.data
  const hasExisting = Boolean(existing && (existing.content ?? existing.Content))
  return (
    <Card title={t('student.competitions.submission.title')}>
      <p className="ui-muted">{t('student.competitions.submission.description')}</p>
      {submission.isError && <ErrorState error={submission.error} onRetry={submission.refetch} />}
      {hasExisting ? (
        <dl className="student-grid">
          <Field label={t('student.competitions.submission.current')} value={existing.content ?? existing.Content} />
          <Field label={t('student.details')} value={formatDate(existing.submittedAt ?? existing.SubmittedAt, locale)} />
        </dl>
      ) : !submission.isLoading && <EmptyState title={t('student.competitions.submission.empty')} />}
      <form onSubmit={(e) => { e.preventDefault(); if (content.trim()) submit.mutate() }}>
        <TextareaField label={t('student.competitions.submission.label')} value={content} onChange={(e) => setContent(e.target.value)} maxLength={8192} placeholder={t('student.competitions.submission.placeholder')} />
        <Button type="submit" loading={submit.isPending} disabled={!content.trim()}>{hasExisting ? t('student.competitions.submission.update') : t('student.competitions.submission.submit')}</Button>
      </form>
      {submit.isError && <ErrorState error={submit.error} />}
      {submit.isSuccess && <Alert variant="success" title={t('student.competitions.submission.saved')} />}
    </Card>
  )
}

function OfficeHoursPage({ userId, locale }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const query = useStudentQuery(queryKeys.student.officeHours(userId), (signal) => studentApi.officeHours(signal))
  const book = useMutation({ mutationFn: (id) => studentApi.bookOfficeHour(id), onSuccess: () => qc.invalidateQueries({ queryKey: queryKeys.student.officeHours(userId) }) })
  const available = query.data ? toItems(settledData(query.data.available)) : []
  const mine = query.data ? toItems(settledData(query.data.mine)) : []
  return (
    <>
      <PageHeader title={t('student.officeHours.title')} description={t('student.officeHours.description')} />
      {query.isLoading && <p role="status">Loading...</p>}
      {query.isError && <ErrorState error={query.error} onRetry={query.refetch} />}
      {book.isError && <ErrorState error={book.error} />}
      <Card title={t('student.officeHours.available')}><ItemsList items={available} empty={t('student.empty.officeHours')} renderItem={(item) => <PlainItem item={item} locale={locale} action={<Button onClick={() => book.mutate(itemId(item))} loading={book.isPending}>{t('student.officeHours.book')}</Button>} />} /></Card>
      <Card title={t('student.officeHours.bookings')}><ItemsList items={mine} empty={t('student.officeHours.noBookings')} renderItem={(item) => <PlainItem item={item} locale={locale} />} /></Card>
    </>
  )
}

function NotificationsPage({ userId, locale }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const query = useStudentQuery(queryKeys.student.notifications(userId), (signal) => studentApi.notifications(signal))
  const mark = useMutation({ mutationFn: (id) => studentApi.markNotificationRead(id), onSuccess: () => qc.invalidateQueries({ queryKey: queryKeys.student.notifications(userId) }) })
  const markAll = useMutation({ mutationFn: () => studentApi.markAllNotificationsRead(), onSuccess: () => qc.invalidateQueries({ queryKey: queryKeys.student.notifications(userId) }) })
  const items = query.data ? toItems(settledData(query.data.items)) : []
  const unread = query.data ? toObject(settledData(query.data.count))?.unreadCount : null
  return (
    <>
      <PageHeader title={t('student.notifications.title')} description={t('student.notifications.description')} actions={<Button variant="secondary" onClick={() => markAll.mutate()} loading={markAll.isPending}>{t('student.notifications.markAll')}</Button>} />
      {unread !== null && <Alert title={t('student.notifications.unread')}>{unread}</Alert>}
      {query.isError && <ErrorState error={query.error} onRetry={query.refetch} />}
      {mark.isError && <ErrorState error={mark.error} />}
      <ItemsList items={items} empty={t('student.empty.notifications')} renderItem={(item) => <PlainItem item={item} locale={locale} action={<Button variant="secondary" onClick={() => mark.mutate(itemId(item))} loading={mark.isPending}>{t('student.notifications.markRead')}</Button>} />} />
    </>
  )
}

function AnnouncementsPage({ userId }) {
  const { t } = useTranslation()
  const query = useStudentQuery(queryKeys.student.announcements(userId), (signal) => studentApi.announcements(signal), { staleTime: STALE.medium })
  return <ListPage title={t('student.announcements.title')} description={t('student.announcements.description')} query={query} empty={t('student.empty.announcements')} />
}

function SuggestionsPage() {
  const { t } = useTranslation()
  const [title, setTitle] = useState('')
  const [body, setBody] = useState('')
  const mutation = useMutation({ mutationFn: () => studentApi.submitSuggestion(title, body), onSuccess: () => { setTitle(''); setBody('') } })
  return (
    <>
      <PageHeader title={t('student.suggestions.title')} description={t('student.suggestions.description')} />
      <Alert title={t('student.suggestions.privacyTitle')}>{t('student.suggestions.privacyBody')}</Alert>
      <Card>
        <form onSubmit={(event) => { event.preventDefault(); if (title.trim() && body.trim()) mutation.mutate() }}>
          <TextField label={t('student.suggestions.subject')} value={title} onChange={(e) => setTitle(e.target.value)} maxLength={120} required />
          <TextareaField label={t('student.suggestions.body')} value={body} onChange={(e) => setBody(e.target.value)} maxLength={2000} required />
          <Button type="submit" loading={mutation.isPending} disabled={!title.trim() || !body.trim()}>{t('actions.submit')}</Button>
        </form>
      </Card>
      {mutation.isSuccess && <Alert variant="success" title={t('student.suggestions.sent')} />}
      {mutation.isError && <ErrorState error={mutation.error} />}
    </>
  )
}

function BadgesPage({ userId, locale }) {
  const { t } = useTranslation()
  const query = useStudentQuery(queryKeys.student.badges(userId), (signal) => studentApi.badges(userId, signal), { staleTime: STALE.medium })
  const points = useStudentQuery(queryKeys.student.points(userId), (signal) => studentApi.points(userId, signal), { staleTime: STALE.short })
  const ledger = useStudentQuery(queryKeys.student.pointsLedger(userId), (signal) => studentApi.pointsLedger(userId, signal), { staleTime: STALE.short })
  const leaderboard = useStudentQuery(queryKeys.student.pointsLeaderboard(userId), (signal) => studentApi.pointsLeaderboard({}, signal), { staleTime: STALE.short })
  const earned = query.data ? toItems(settledData(query.data.earned)) : []
  const catalog = query.data ? toItems(settledData(query.data.catalog)) : []
  const streak = query.data ? toObject(settledData(query.data.streak)) : null
  return (
    <>
      <PageHeader title={t('student.badges.title')} description={t('student.badges.description')} />
      {query.isError && <ErrorState error={query.error} onRetry={query.refetch} />}
      <Card title={t('student.points.title')}>
        {points.isLoading ? <p role="status">{t('common.loading')}</p>
          : points.isError ? <ErrorState error={points.error} onRetry={points.refetch} />
          : <Alert variant="info">{t('student.points.total', { count: points.data?.totalPoints ?? 0 })}</Alert>}
      </Card>
      <Card title={t('student.points.ledger')}>
        {ledger.isLoading ? <p role="status">{t('common.loading')}</p>
          : ledger.isError ? <ErrorState error={ledger.error} onRetry={ledger.refetch} />
          : <ItemsList items={ledger.data || []} empty={t('student.points.empty')} renderItem={(item) => <PlainItem item={item} locale={locale} />} />}
      </Card>
      <Card title={t('student.points.leaderboard')}>
        {leaderboard.isLoading ? <p role="status">{t('common.loading')}</p>
          : leaderboard.isError ? <ErrorState error={leaderboard.error} onRetry={leaderboard.refetch} />
          : <ItemsList items={leaderboard.data || []} empty={t('student.points.leaderboardEmpty')} renderItem={(item) => <PlainItem item={item} locale={locale} />} />}
      </Card>
      <Card title={t('student.badges.earned')}><ItemsList items={earned} empty={t('student.empty.badges')} renderItem={(item) => <PlainItem item={item} locale={locale} />} /></Card>
      <Card title={t('student.streaks.title')}>{streak ? <DetailGrid item={streak} locale={locale} /> : <EmptyState title={t('student.streaks.empty')} />}</Card>
      <Card title={t('student.badges.catalog')}><ItemsList items={catalog} empty={t('student.empty.badges')} renderItem={(item) => <PlainItem item={item} locale={locale} />} /></Card>
    </>
  )
}

function AccountSettingsPage() {
  const { t } = useTranslation()
  return (
    <>
      <PageHeader title={t('student.settings.title')} description={t('student.settings.description')} />
      <Card title={t('student.settings.language')}><p>{t('student.settings.languageLocal')}</p></Card>
      <Card title={t('nav.security')}><Link className="ui-btn ui-btn--primary" to="/app/security">{t('student.settings.changePassword')}</Link></Card>
    </>
  )
}

function ListPage({ title, description, query, empty, itemLink }) {
  return (
    <>
      <PageHeader title={title} description={description} />
      <QueryResult query={query} empty={empty}>{(items) => <ItemsList items={items} empty={empty} renderItem={(item) => itemLink ? <LinkedItem item={item} to={itemLink(item)} /> : <PlainItem item={item} />} />}</QueryResult>
    </>
  )
}

function SingleDataPage({ title, description, query, locale }) {
  return (
    <>
      <PageHeader title={title} description={description} />
      <QueryResult query={query} empty="No data">{(data) => <DetailGrid item={data} locale={locale} />}</QueryResult>
    </>
  )
}

function QueryResult({ query, empty, children }) {
  if (query.isLoading) return <p role="status">Loading...</p>
  if (query.isError) return <ErrorState error={query.error} onRetry={query.refetch} />
  const data = Array.isArray(query.data) ? query.data : query.data ? query.data : []
  if (Array.isArray(data) && !data.length) return <EmptyState title={empty} />
  return children(data)
}

function LinkedItem({ item, to }) {
  return <Link className="student-row-link" to={to}><FileText size={18} aria-hidden="true" /><span>{displayValue(item) || to}</span></Link>
}

function PlainItem({ item, locale, action }) {
  return (
    <div className="student-item">
      <div><strong>{displayValue(item) || itemId(item) || 'Record'}</strong><DetailGrid item={item} locale={locale} /></div>
      {action && <div>{action}</div>}
    </div>
  )
}

export default function StudentPortalPage({ view }) {
  const { i18n } = useTranslation()
  const { user } = useAuth()
  const userId = user?.id
  const locale = i18n.language || 'en'
  if (!userId) return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  const common = { userId, locale }
  switch (view) {
    case 'subjects': return <SubjectsPage {...common} />
    case 'subject-details': return <SubjectDetailsPage {...common} />
    case 'units': return <UnitsPage {...common} />
    case 'lessons': return <LessonsPage {...common} />
    case 'materials': return <MaterialsPage {...common} />
    case 'homework': return <HomeworkPage {...common} />
    case 'quizzes': return <QuizzesPage {...common} />
    case 'quiz-attempt': return <QuizAttemptPage {...common} />
    case 'quiz-result': return <QuizResultPage {...common} />
    case 'tutor': return <TutorPage />
    case 'recommendations': return <ProgressPage {...common} mode="recommendations" />
    case 'progress': return <ProgressPage {...common} mode="progress" />
    case 'attendance': return <AttendancePage {...common} />
    case 'engagement': return <ProgressPage {...common} mode="engagement" />
    case 'communities': return <CommunitiesPage {...common} />
    case 'competitions': return <CompetitionsPage {...common} />
    case 'office-hours': return <OfficeHoursPage {...common} />
    case 'notifications': return <NotificationsPage {...common} />
    case 'announcements': return <AnnouncementsPage {...common} />
    case 'suggestions': return <SuggestionsPage />
    case 'badges': return <BadgesPage {...common} />
    case 'settings': return <AccountSettingsPage />
    default: return <DashboardPage {...common} />
  }
}
