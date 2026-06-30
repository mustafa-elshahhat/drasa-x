import { useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import {
  Bot, FileText, BookOpen, ClipboardCheck, ClipboardList, Bell, CalendarClock, Medal, Users, Trophy,
  Layers, PlayCircle, GraduationCap, Megaphone, Lightbulb, MessageSquare, Send, Flame, Sparkles, ShieldCheck,
} from 'lucide-react'
import { PageHeader, Card } from '../../components/ui/PageHeader'
import { Metric } from '../../components/ui/Metric'
import { EmptyState, ErrorState } from '../../components/ui/states'
import { NotEnoughData } from '../../components/ui/NotEnoughData'
import { Button } from '../../components/ui/Button'
import { Alert } from '../../components/ui/Alert'
import { Chip } from '../../components/ui/Chip'
import { Spinner } from '../../components/ui/Spinner'
import { QueryBoundary } from '../../components/ui/QueryBoundary'
import { TextField, TextareaField } from '../../components/form/fields'
import { DetailList } from '../../components/data/DetailList'
import { Ring } from '../../components/viz/Ring'
import { ProgressBar } from '../../components/viz/ProgressBar'
import { Heatmap } from '../../components/viz/Heatmap'
import { StreakStrip } from '../../components/viz/StreakStrip'
import { SubjectCard } from '../../components/domain/SubjectCard'
import { UnitCard } from '../../components/domain/UnitCard'
import { LessonRow } from '../../components/domain/LessonRow'
import { QuizCard } from '../../components/domain/QuizCard'
import { LeaderboardRow } from '../../components/domain/LeaderboardRow'
import { Thumb } from '../../components/domain/Thumb'
import { AITutorMessage } from '../../components/chat/AITutorMessage'
import { OptionCard } from '../../components/quiz/OptionCard'
import { useAuth } from '../../features/auth/AuthContext'
import { studentApi } from '../../features/student/studentApi'
import {
  answerPayloadFromForm, displayValue, formatDate, getField, itemId, settledData, settledError,
} from '../../features/student/studentUtils'
import { queryKeys, STALE } from '../../lib/query/keys'
import { toItems, toObject } from '../../features/student/studentSchemas'

function useStudentQuery(key, fn, options = {}) {
  return useQuery({ queryKey: key, queryFn: ({ signal }) => fn(signal), staleTime: options.staleTime ?? STALE.short, enabled: options.enabled ?? true })
}

function Loading() {
  const { t } = useTranslation()
  return <Spinner label={t('states.loading')} />
}

// Common percentage-style fields on untyped progress records. Used to show a real
// ProgressBar where the backend returns one — and NotEnoughData where it does not.
function percentOf(item) {
  const keys = ['completionPercentage', 'CompletionPercentage', 'progress', 'Progress', 'percentage', 'Percentage', 'masteryPercent', 'MasteryPercent', 'score', 'Score', 'averageScore', 'AverageScore']
  for (const k of keys) {
    const v = getField(item, k)
    if (v != null && v !== '' && Number.isFinite(Number(v))) return Number(v)
  }
  return null
}

const ATTENDANCE_TONE = { present: 'success', late: 'warning', absent: 'danger', excused: 'info' }

// =============================================================================
// Dashboard
// =============================================================================
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
  const subjectItems = toItems(subjects.data)

  // Honest KPI value: a placeholder while loading / on error, the real count otherwise.
  const count = (q) => (q.isLoading ? '…' : q.isError ? '—' : q.data?.length ?? 0)
  return (
    <>
      <PageHeader title={t('student.dashboard.title')} description={t('student.dashboard.description')} />
      <div className="student-dashboard">
        <Metric to="/app/student/subjects" icon={BookOpen} accent="var(--brand)" value={count(subjects)} label={t('student.dashboard.assignedSubjects')} />
        <Metric to="/app/student/homework" icon={ClipboardCheck} accent="var(--orange)" value={count(homework)} label={t('student.dashboard.upcomingHomework')} />
        <Metric to="/app/student/quizzes" icon={ClipboardList} accent="var(--purple)" value={count(quizzes)} label={t('student.dashboard.assignedQuizzes')} />
        <Metric to="/app/student/notifications" icon={Bell} accent="var(--info)" value={unread ?? '—'} label={t('student.dashboard.unreadNotifications')} />
        <Metric to="/app/student/recommendations" icon={Bot} accent="var(--purple)" value={progressRecommendations.length} label={t('student.dashboard.activeRecommendations')} />
        <Metric to="/app/student/office-hours" icon={CalendarClock} accent="var(--brand)" value={officeAvailable.length} label={t('student.dashboard.availableOfficeHours')} />
        <Metric to="/app/student/badges" icon={Medal} accent="var(--warning)" value={earnedBadges.length} label={t('student.dashboard.earnedBadges')} />
        <Metric to="/app/student/communities" icon={Users} accent="var(--success)" value={count(communities)} label={t('student.dashboard.availableCommunities')} />
        <Metric to="/app/student/competitions" icon={Trophy} accent="var(--orange)" value={count(competitions)} label={t('student.dashboard.activeCompetitions')} />
      </div>

      {subjectItems.length > 0 && (
        <section className="ui-section">
          <div className="ui-section__head">
            <h2 className="ui-section__title">{t('student.subjects.title')}</h2>
            <Link to="/app/student/subjects" className="ui-btn ui-btn--soft">{t('student.viewAll', 'View all')}</Link>
          </div>
          <div className="ui-grid ui-grid--auto">
            {subjectItems.slice(0, 6).map((item) => (
              <SubjectCard key={itemId(item) || displayValue(item)} to={`/app/student/subjects/${itemId(item)}`} name={displayValue(item)} progress={percentOf(item)} progressLabel={t('student.progress.title')} />
            ))}
          </div>
        </section>
      )}

      <Card title={t('student.progress.title')} description={t('student.progress.description')}>
        {progress.isError ? <ErrorState error={progress.error} onRetry={progress.refetch} /> : progress.isLoading ? <Loading /> : <ProgressSections progress={progress.data} locale={locale} />}
      </Card>
    </>
  )
}

// =============================================================================
// Subjects / units / lessons / materials
// =============================================================================
function SubjectsPage({ userId }) {
  const { t } = useTranslation()
  const query = useStudentQuery(queryKeys.student.subjects(userId), (signal) => studentApi.subjects(signal), { staleTime: STALE.medium })
  return (
    <>
      <PageHeader title={t('student.subjects.title')} description={t('student.subjects.description')} />
      <QueryBoundary query={query} loadingFallback={<Loading />} emptyWhen={(d) => !d?.length} emptyTitle={t('student.empty.subjects')} emptyIcon={BookOpen}>
        {(items) => (
          <div className="ui-grid ui-grid--auto">
            {items.map((item) => (
              <SubjectCard key={itemId(item) || displayValue(item)} to={`/app/student/subjects/${itemId(item)}`} name={displayValue(item)} progress={percentOf(item)} progressLabel={t('student.progress.title')} />
            ))}
          </div>
        )}
      </QueryBoundary>
    </>
  )
}

function SubjectDetailsPage({ userId, locale }) {
  const { t } = useTranslation()
  const { subjectId } = useParams()
  const subject = useStudentQuery(queryKeys.student.subject(userId, subjectId), (signal) => studentApi.subject(subjectId, signal), { enabled: Boolean(subjectId), staleTime: STALE.medium })
  const units = useStudentQuery(queryKeys.student.units(userId, subjectId), (signal) => studentApi.units(subjectId, signal), { enabled: Boolean(subjectId), staleTime: STALE.medium })
  return (
    <>
      <PageHeader
        title={displayValue(subject.data) || t('student.subjects.details')}
        description={t('student.subjects.secureDescription')}
        actions={<Link to="/app/student/ai-tutor" className="ui-btn ui-btn--soft"><Sparkles size={16} aria-hidden="true" /> {t('student.tutor.title')}</Link>}
      />
      {subject.isError && <ErrorState error={subject.error} onRetry={subject.refetch} />}
      {subject.data && (
        <Card title={t('student.details')}><DetailList item={subject.data} locale={locale} /></Card>
      )}
      <section className="ui-section">
        <div className="ui-section__head"><h2 className="ui-section__title">{t('student.units.title')}</h2></div>
        <QueryBoundary query={units} loadingFallback={<Loading />} emptyWhen={(d) => !d?.length} emptyTitle={t('student.empty.units')} emptyIcon={Layers}>
          {(items) => (
            <div className="student-list">
              {items.map((item) => (
                <UnitCard key={itemId(item)} to={`/app/student/units/${itemId(item)}`} title={displayValue(item)} icon={Layers} />
              ))}
            </div>
          )}
        </QueryBoundary>
      </section>
    </>
  )
}

function UnitsPage({ userId }) {
  const { t } = useTranslation()
  const { unitId } = useParams()
  const lessons = useStudentQuery(queryKeys.student.lessons(userId, unitId), (signal) => studentApi.lessons(unitId, signal), { enabled: Boolean(unitId), staleTime: STALE.medium })
  if (!unitId) return <SubjectsPage userId={userId} />
  return (
    <>
      <PageHeader title={t('student.lessons.title')} description={t('student.lessons.description')} />
      <QueryBoundary query={lessons} loadingFallback={<Loading />} emptyWhen={(d) => !d?.length} emptyTitle={t('student.empty.lessons')} emptyIcon={PlayCircle}>
        {(items) => (
          <div className="student-list">
            {items.map((item, i) => (
              <LessonRow key={itemId(item)} to={`/app/student/lessons/${itemId(item)}`} index={i + 1} title={displayValue(item)} />
            ))}
          </div>
        )}
      </QueryBoundary>
    </>
  )
}

function LessonsPage({ userId }) {
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
      <PageHeader title={t('student.lessons.details')} description={t('student.lessons.progressRule')} />
      <div className="ui-split">
        <div>
          <Thumb seed={lessonId} icon={PlayCircle} height={200} className="student-lesson__hero" />
          <Card title={t('student.materials.title')}>
            <QueryBoundary query={materials} loadingFallback={<Loading />} emptyWhen={(d) => !d?.length} emptyTitle={t('student.empty.materials')} emptyIcon={FileText}>
              {(items) => (
                <div className="student-list">
                  {items.map((item) => (
                    <LessonRow key={itemId(item)} to={`/app/student/materials/${itemId(item)}`} title={displayValue(item)} />
                  ))}
                </div>
              )}
            </QueryBoundary>
          </Card>
        </div>
        <Card title={t('student.lessons.progress', 'Progress')}>
          <div className="student-lesson__rail">
            <Button onClick={() => complete.mutate()} loading={complete.isPending} disabled={complete.isPending || isCompleted}>
              {isCompleted ? t('student.lessons.completed') : t('student.lessons.markComplete')}
            </Button>
            {isCompleted && <Alert variant="success" title={t('student.lessons.completed')}>{t('student.lessons.completedBody')}</Alert>}
            {complete.isSuccess && <Alert variant="success" title={t('student.lessons.saved')}>{t('student.lessons.savedBody')}</Alert>}
            {complete.isError && <ErrorState error={complete.error} />}
            <Link to="/app/student/ai-tutor" className="ui-btn ui-btn--soft ui-btn--block"><Sparkles size={16} aria-hidden="true" /> {t('student.tutor.title')}</Link>
          </div>
        </Card>
      </div>
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
      <div className="ui-split">
        <div>
          <Thumb seed={materialId} icon={PlayCircle} height={220} className="student-lesson__hero" />
        </div>
        <Card title={t('student.comments.title')}>
          <form className="stack" onSubmit={(event) => { event.preventDefault(); if (body.trim()) mutation.mutate() }}>
            <TextareaField label={t('student.comments.add')} value={body} onChange={(e) => setBody(e.target.value)} maxLength={1000} />
            <Button type="submit" loading={mutation.isPending} disabled={!body.trim()}>{t('actions.submit')}</Button>
          </form>
          {mutation.isError && <ErrorState error={mutation.error} />}
          <QueryBoundary query={comments} loadingFallback={<Loading />} emptyWhen={(d) => !d?.length} emptyTitle={t('student.empty.comments')} emptyIcon={MessageSquare}>
            {(items) => (
              <ul className="ui-list">
                {items.map((item) => (
                  <li className="ui-list__item" key={itemId(item)}>
                    <div className="ui-list__body">
                      <div className="ui-list__title">{displayValue(item, ['authorName', 'AuthorName', 'author', 'Author']) || t('student.comments.title')}</div>
                      <div className="ui-list__meta">{displayValue(item, ['body', 'Body', 'content', 'Content', 'text', 'Text'])}</div>
                      <div className="ui-list__meta ui-muted">{formatDate(getField(item, 'createdAt'), locale)}</div>
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </QueryBoundary>
        </Card>
      </div>
    </>
  )
}

// =============================================================================
// Homework
// =============================================================================
function HomeworkPage({ userId, locale }) {
  const { t } = useTranslation()
  const { homeworkId } = useParams()
  const list = useStudentQuery(queryKeys.student.homework(userId), (signal) => studentApi.homework(signal))
  if (homeworkId) return <HomeworkDetails userId={userId} homeworkId={homeworkId} list={list} locale={locale} />
  return (
    <>
      <PageHeader title={t('student.homework.title')} description={t('student.homework.description')} />
      <QueryBoundary query={list} loadingFallback={<Loading />} emptyWhen={(d) => !d?.length} emptyTitle={t('student.empty.homework')} emptyIcon={ClipboardCheck}>
        {(items) => (
          <div className="ui-grid ui-grid--auto">
            {items.map((item) => (
              <QuizCard
                key={itemId(item)}
                to={`/app/student/homework/${itemId(item)}`}
                icon={ClipboardCheck}
                title={displayValue(item)}
                meta={formatDate(getField(item, 'dueDate') || getField(item, 'dueAt'), locale)}
              />
            ))}
          </div>
        )}
      </QueryBoundary>
    </>
  )
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
      <div className="ui-split">
        <Card title={t('student.homework.submission')}>
          <QueryBoundary query={submission} loadingFallback={<Loading />} emptyWhen={(d) => !d} emptyTitle={t('student.homework.noSubmission')}>
            {(data) => <DetailList item={data} locale={locale} />}
          </QueryBoundary>
          <form className="stack" onSubmit={(event) => { event.preventDefault(); if (content.trim()) mutation.mutate() }}>
            <TextareaField label={t('student.homework.answer')} value={content} onChange={(e) => setContent(e.target.value)} maxLength={4000} />
            <Button type="submit" loading={mutation.isPending} disabled={!content.trim()}>{t('student.homework.submit')}</Button>
          </form>
          {mutation.isError && <ErrorState error={mutation.error} />}
        </Card>
        {item && <Card title={t('student.details')}><DetailList item={item} locale={locale} /></Card>}
      </div>
    </>
  )
}

// =============================================================================
// Quizzes
// =============================================================================
function QuizzesPage({ userId, locale }) {
  const { t } = useTranslation()
  const { quizId } = useParams()
  const query = useStudentQuery(queryKeys.student.quizzes(userId), (signal) => studentApi.assignedQuizzes(signal))
  if (quizId) return <QuizDetails userId={userId} quizId={quizId} list={query} locale={locale} />
  return (
    <>
      <PageHeader title={t('student.quizzes.title')} description={t('student.quizzes.description')} />
      <QueryBoundary query={query} loadingFallback={<Loading />} emptyWhen={(d) => !d?.length} emptyTitle={t('student.empty.quizzes')} emptyIcon={ClipboardList}>
        {(items) => (
          <div className="ui-grid ui-grid--auto">
            {items.map((item) => (
              <QuizCard
                key={itemId(item, ['quizId', 'QuizId', 'id', 'Id'])}
                to={`/app/student/quizzes/${itemId(item, ['quizId', 'QuizId', 'id', 'Id'])}`}
                icon={ClipboardList}
                title={displayValue(item)}
                meta={formatDate(getField(item, 'dueDate') || getField(item, 'dueAt'), locale)}
              />
            ))}
          </div>
        )}
      </QueryBoundary>
    </>
  )
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
      {item && <Card title={t('student.details')}><DetailList item={item} locale={locale} /></Card>}
      <Card title={t('student.quizzes.previousAttempts')}>
        <QueryBoundary query={history} loadingFallback={<Loading />} emptyWhen={(d) => !d?.length} emptyTitle={t('student.empty.attempts')}>
          {(items) => (
            <div className="student-list">
              {items.map((attempt) => (
                <LessonRow key={itemId(attempt)} to={`/app/student/quiz-attempts/${itemId(attempt)}/result`} title={displayValue(attempt) || itemId(attempt)} icon={FileText} />
              ))}
            </div>
          )}
        </QueryBoundary>
      </Card>
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
  if (query.isLoading) return (<><PageHeader title={t('student.quizzes.attempt')} /><Loading /></>)
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
      {questions.length > 0 && <Chip tone="muted">{t('student.quizzes.questionCount', { count: questions.length })}</Chip>}
      <form className="student-quiz" onSubmit={(event) => { event.preventDefault(); const answers = answerPayloadFromForm(new FormData(event.currentTarget)); save.mutate(answers) }}>
        {questions.length ? questions.map((q, i) => <QuestionBlock key={itemId(q)} index={i} question={q} saved={savedAnswers[itemId(q)]} />) : <DetailList item={query.data} locale={locale} />}
        <div className="student-actions">
          <Button type="submit" variant="secondary" loading={save.isPending}>{t('student.quizzes.save')}</Button>
          <Button type="button" loading={submit.isPending} onClick={() => submit.mutate()}>{t('student.quizzes.submit')}</Button>
        </div>
      </form>
      {save.isSuccess && <Alert variant="success" title={t('student.quizzes.saved', 'Answers saved')} />}
      {(save.isError || submit.isError) && <ErrorState error={save.error || submit.error} />}
    </>
  )
}

const OPTION_MARKERS = ['A', 'B', 'C', 'D', 'E', 'F']

function QuestionBlock({ question, index, saved }) {
  const { t } = useTranslation()
  const questionId = itemId(question)
  const options = question.options || question.Options || []
  const savedOptionId = saved?.selectedOptionId ?? saved?.SelectedOptionId ?? null
  const savedText = saved?.answerText ?? saved?.AnswerText ?? ''
  return (
    <Card title={`${index + 1}. ${displayValue(question, ['text', 'Text', 'title', 'Title'])}`}>
      {options.length > 0 ? (
        <div className="student-quiz__options">
          {options.map((option, oi) => (
            <OptionCard
              key={itemId(option)}
              name={`q:${questionId}`}
              value={itemId(option)}
              marker={OPTION_MARKERS[oi] || oi + 1}
              label={displayValue(option, ['text', 'Text', 'label', 'Label'])}
              defaultChecked={itemId(option) === savedOptionId}
            />
          ))}
        </div>
      ) : (
        <TextareaField label={t('student.quizzes.answer', 'Answer')} name={`qt:${questionId}`} defaultValue={savedText} />
      )}
    </Card>
  )
}

function QuizResultPage({ userId, locale }) {
  const { t } = useTranslation()
  const { attemptId } = useParams()
  const query = useStudentQuery(queryKeys.student.attemptResult(userId, attemptId), (signal) => studentApi.attemptResult(attemptId, signal), { enabled: Boolean(attemptId) })
  return (
    <>
      <PageHeader title={t('student.quizzes.result')} description={t('student.quizzes.serverScore')} />
      <QueryBoundary query={query} loadingFallback={<Loading />} emptyWhen={(d) => !d}>
        {(data) => {
          const pct = percentOf(data)
          return (
            <Card>
              {pct != null ? (
                <div className="quiz-result__head">
                  <Ring value={pct} centerLabel={`${pct}%`} caption={t('student.quizzes.score', 'Score')} />
                </div>
              ) : null}
              <DetailList item={data} locale={locale} />
            </Card>
          )
        }}
      </QueryBoundary>
    </>
  )
}

// =============================================================================
// AI tutor
// =============================================================================
function TutorPage() {
  const { t, i18n } = useTranslation()
  const [message, setMessage] = useState('')
  const [subject, setSubject] = useState('')
  const mutation = useMutation({ mutationFn: () => studentApi.tutor({ message, subject: subject || undefined, language: i18n.language }) })
  const response = mutation.data
  const citations = response ? toItems(response.citations).map((c) => ({ label: displayValue(c, ['title', 'Title', 'source', 'Source', 'label', 'Label']) || displayValue(c) })) : []
  return (
    <>
      <PageHeader title={t('student.tutor.title')} description={t('student.tutor.description')} />
      <div className="student-tutor">
        <Chip tone="purple" icon={Bot}>{t('student.tutor.title')}</Chip>
        <div className="chat-thread">
          {response && (
            <AITutorMessage
              answer={response.answer || response.noAnswerReason}
              grounded={response.grounded}
              citations={citations}
              sourcesLabel={t('student.tutor.citations')}
              meta={response.correlationId ? `${t('errors.referenceId')}: ${response.correlationId}` : undefined}
            />
          )}
          {!response && !mutation.isPending && (
            <EmptyState icon={Sparkles} title={t('student.tutor.ask')} message={t('student.tutor.description')} />
          )}
          {mutation.isPending && <Loading />}
        </div>
        {mutation.isError && <ErrorState error={mutation.error} />}
        <Card>
          <form className="stack" onSubmit={(event) => { event.preventDefault(); if (message.trim()) mutation.mutate() }}>
            <TextField label={t('student.tutor.context')} value={subject} onChange={(e) => setSubject(e.target.value)} />
            <TextareaField label={t('student.tutor.question')} value={message} onChange={(e) => setMessage(e.target.value)} required maxLength={2000} />
            <Button type="submit" loading={mutation.isPending} disabled={!message.trim()}><Send size={16} aria-hidden="true" /> {t('student.tutor.send')}</Button>
          </form>
        </Card>
      </div>
    </>
  )
}

// =============================================================================
// Progress / recommendations / engagement
// =============================================================================
function ProgressPage({ userId, locale, mode = 'progress' }) {
  const { t } = useTranslation()
  const query = useStudentQuery(queryKeys.student.progress(userId), (signal) => studentApi.progress(userId, signal))
  return (
    <>
      <PageHeader title={t(`student.${mode}.title`)} description={t(`student.${mode}.description`)} />
      {query.isLoading && <Loading />}
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
  return <>{sections.map(([key, title]) => <PartialSection key={key} sectionKey={key} title={title} result={progress?.[key]} locale={locale} />)}</>
}

function PartialSection({ sectionKey, title, result, locale }) {
  const { t } = useTranslation()
  const error = settledError(result)
  const data = settledData(result)
  const items = toItems(data)
  const object = toObject(data)

  let content
  if (error) {
    content = <ErrorState error={error} />
  } else if (sectionKey === 'subjects' && items.length) {
    // Real mastery bars where the backend returns a percentage; honest otherwise.
    content = (
      <div className="domain-usage">
        {items.map((item, i) => {
          const pct = percentOf(item)
          return pct != null
            ? <ProgressBar key={i} label={displayValue(item)} value={pct} />
            : <DetailList key={i} item={item} locale={locale} />
        })}
      </div>
    )
  } else if (items.length) {
    content = (
      <div className="student-list">
        {items.map((item, i) => (
          <div className="student-list__item" key={itemId(item) || i}>
            <strong className="domain-row__title">{displayValue(item) || itemId(item) || t('states.emptyTitle')}</strong>
            <DetailList item={item} locale={locale} />
          </div>
        ))}
      </div>
    )
  } else if (object) {
    content = <DetailList item={object} locale={locale} />
  } else {
    content = <NotEnoughData />
  }
  return <Card title={title}>{content}</Card>
}

// =============================================================================
// Attendance
// =============================================================================
function AttendancePage({ userId, locale }) {
  const { t } = useTranslation()
  const query = useStudentQuery(queryKeys.student.attendance(userId), (signal) => studentApi.attendance(signal))
  return (
    <>
      <PageHeader title={t('student.attendance.title')} description={t('student.attendance.description')} />
      <Alert title={t('student.attendance.noCvTitle')}>{t('student.attendance.noCvBody')}</Alert>
      {query.isLoading && <Loading />}
      {query.isError && <ErrorState error={query.error} onRetry={query.refetch} />}
      {query.data && <AttendanceContent data={query.data} locale={locale} />}
    </>
  )
}

function AttendanceContent({ data, locale }) {
  const { t } = useTranslation()
  const records = data.records || []
  const summary = data.summary || {}
  const pct = summary.attendancePercentage ?? 0
  const legend = [
    { level: 'present', label: t('student.attendance.present') },
    { level: 'late', label: t('student.attendance.late') },
    { level: 'absent', label: t('student.attendance.absent') },
    { level: 'excused', label: t('student.attendance.excused') },
  ]
  const cells = records.map((r, i) => {
    const level = (String(r.status || '').toLowerCase()) in ATTENDANCE_TONE ? String(r.status).toLowerCase() : 'none'
    return { key: r.id ?? i, level, title: `${formatDate(r.attendanceDate, locale)} — ${r.status}` }
  })
  return (
    <>
      <Card title={t('student.attendance.summary')}>
        <div className="student-attendance">
          <Ring value={pct} centerLabel={`${pct}%`} caption={t('student.attendance.percentage')} />
          <div className="student-attendance__stats">
            <Stat label={t('student.attendance.total')} value={summary.total ?? 0} />
            <Stat label={t('student.attendance.present')} value={summary.present ?? 0} />
            <Stat label={t('student.attendance.absent')} value={summary.absent ?? 0} />
            <Stat label={t('student.attendance.late')} value={summary.late ?? 0} />
            <Stat label={t('student.attendance.excused')} value={summary.excused ?? 0} />
          </div>
        </div>
        {cells.length > 0 && <div style={{ marginTop: 18 }}><Heatmap cells={cells} legend={legend} /></div>}
      </Card>
      <Card title={t('student.attendance.records')}>
        {records.length === 0 ? (
          <EmptyState title={t('student.attendance.empty')} />
        ) : (
          <ul className="ui-list">
            {records.map((item) => {
              const tone = ATTENDANCE_TONE[String(item.status || '').toLowerCase()] || 'muted'
              return (
                <li className="ui-list__item" key={item.id || item.attendanceDate}>
                  <div className="ui-list__body">
                    <div className="ui-list__title">{formatDate(item.attendanceDate, locale)}</div>
                    {(item.source || item.notes) && (
                      <div className="ui-list__meta ui-muted">{[item.source, item.notes].filter(Boolean).join(' · ')}</div>
                    )}
                  </div>
                  <Chip tone={tone} aria-label={`${t('student.attendance.status')}: ${item.status}`}>{item.status}</Chip>
                </li>
              )
            })}
          </ul>
        )}
      </Card>
    </>
  )
}

function Stat({ label, value }) {
  return (
    <div className="domain-child__stat">
      <span className="domain-child__stat-value">{value}</span>
      <span className="domain-child__stat-label">{label}</span>
    </div>
  )
}

// =============================================================================
// Communities
// =============================================================================
function CommunitiesPage({ userId, locale }) {
  const { t } = useTranslation()
  const { communityId } = useParams()
  const query = useStudentQuery(queryKeys.student.communities(userId), (signal) => studentApi.communities(signal), { staleTime: STALE.medium })
  if (communityId) return <CommunityDetails userId={userId} communityId={communityId} locale={locale} />
  return (
    <>
      <PageHeader title={t('student.communities.title')} description={t('student.communities.description')} />
      <QueryBoundary query={query} loadingFallback={<Loading />} emptyWhen={(d) => !d?.length} emptyTitle={t('student.empty.communities')} emptyIcon={Users}>
        {(items) => (
          <div className="ui-grid ui-grid--auto">
            {items.map((item) => (
              <QuizCard key={itemId(item)} to={`/app/student/communities/${itemId(item)}`} icon={Users} title={displayValue(item)} meta={displayValue(item, ['description', 'Description'])} />
            ))}
          </div>
        )}
      </QueryBoundary>
    </>
  )
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
      <div className="ui-split">
        <Card title={t('student.communities.posts')}>
          <form className="stack" onSubmit={(event) => { event.preventDefault(); if (content.trim()) post.mutate() }}>
            <TextareaField label={t('student.communities.newPost')} value={content} onChange={(e) => setContent(e.target.value)} maxLength={2000} />
            <Button type="submit" loading={post.isPending} disabled={!content.trim()}>{t('actions.submit')}</Button>
          </form>
          {post.isError && <ErrorState error={post.error} />}
          <QueryBoundary query={posts} loadingFallback={<Loading />} emptyWhen={(d) => !d?.length} emptyTitle={t('student.empty.posts')} emptyIcon={MessageSquare}>
            {(items) => (
              <ul className="ui-list">
                {items.map((item) => (
                  <li className="ui-list__item" key={itemId(item)}>
                    <div className="ui-list__body">
                      <div className="ui-list__title">{displayValue(item, ['authorName', 'AuthorName']) || t('student.communities.posts')}</div>
                      <div className="ui-list__meta">{displayValue(item, ['body', 'Body', 'content', 'Content'])}</div>
                      <div className="ui-list__meta ui-muted">{formatDate(getField(item, 'createdAt'), locale)}</div>
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </QueryBoundary>
        </Card>
        {community.data && <Card title={t('student.details')}><DetailList item={community.data} locale={locale} /></Card>}
      </div>
    </>
  )
}

// =============================================================================
// Competitions
// =============================================================================
function CompetitionsPage({ userId, locale }) {
  const { t } = useTranslation()
  const { competitionId } = useParams()
  const query = useStudentQuery(queryKeys.student.competitions(userId), (signal) => studentApi.competitions(signal), { staleTime: STALE.medium })
  if (competitionId) return <CompetitionDetails userId={userId} competitionId={competitionId} locale={locale} />
  return (
    <>
      <PageHeader title={t('student.competitions.title')} description={t('student.competitions.description')} />
      <QueryBoundary query={query} loadingFallback={<Loading />} emptyWhen={(d) => !d?.length} emptyTitle={t('student.empty.competitions')} emptyIcon={Trophy}>
        {(items) => (
          <div className="ui-grid ui-grid--auto">
            {items.map((item) => (
              <QuizCard key={itemId(item)} to={`/app/student/competitions/${itemId(item)}`} icon={Trophy} title={displayValue(item)} meta={displayValue(item, ['description', 'Description'])} />
            ))}
          </div>
        )}
      </QueryBoundary>
    </>
  )
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
      <div className="ui-split">
        <div>
          {competition.data && <Card title={t('student.details')}><DetailList item={competition.data} locale={locale} /></Card>}
          <CompetitionSubmissionCard userId={userId} competitionId={competitionId} locale={locale} />
        </div>
        <Card title={t('student.leaderboard.title')}>
          <QueryBoundary query={leaderboard} loadingFallback={<Loading />} emptyWhen={(d) => !d?.length} emptyTitle={t('student.empty.leaderboard')} emptyIcon={Trophy}>
            {(items) => (
              <ul className="domain-lb-list">
                {items.map((item, i) => (
                  <LeaderboardRow
                    key={itemId(item) || i}
                    rank={getField(item, 'rank') ?? i + 1}
                    name={displayValue(item, ['studentName', 'StudentName', 'name', 'Name']) || itemId(item)}
                    points={getField(item, 'totalPoints') ?? getField(item, 'points')}
                    pointsLabel={t('student.points.unit', 'pts')}
                  />
                ))}
              </ul>
            )}
          </QueryBoundary>
        </Card>
      </div>
    </>
  )
}

// Phase 14 (closure) — durable competition submission.
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
        <dl className="ui-detail-list">
          <div className="ui-detail-list__row"><dt>{t('student.competitions.submission.current')}</dt><dd>{existing.content ?? existing.Content}</dd></div>
          <div className="ui-detail-list__row"><dt>{t('student.details')}</dt><dd>{formatDate(existing.submittedAt ?? existing.SubmittedAt, locale)}</dd></div>
        </dl>
      ) : !submission.isLoading && <EmptyState title={t('student.competitions.submission.empty')} />}
      <form className="stack" onSubmit={(e) => { e.preventDefault(); if (content.trim()) submit.mutate() }}>
        <TextareaField label={t('student.competitions.submission.label')} value={content} onChange={(e) => setContent(e.target.value)} maxLength={8192} placeholder={t('student.competitions.submission.placeholder')} />
        <Button type="submit" loading={submit.isPending} disabled={!content.trim()}>{hasExisting ? t('student.competitions.submission.update') : t('student.competitions.submission.submit')}</Button>
      </form>
      {submit.isError && <ErrorState error={submit.error} />}
      {submit.isSuccess && <Alert variant="success" title={t('student.competitions.submission.saved')} />}
    </Card>
  )
}

// =============================================================================
// Office hours
// =============================================================================
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
      {query.isLoading && <Loading />}
      {query.isError && <ErrorState error={query.error} onRetry={query.refetch} />}
      {book.isError && <ErrorState error={book.error} />}
      <Card title={t('student.officeHours.available')}>
        {available.length === 0 ? <EmptyState title={t('student.empty.officeHours')} /> : (
          <div className="student-list">
            {available.map((item) => (
              <div className="domain-row" key={itemId(item)}>
                <span className="domain-row__icon" aria-hidden="true"><CalendarClock size={18} /></span>
                <span className="domain-row__main">
                  <span className="domain-row__title">{displayValue(item, ['teacherName', 'TeacherName']) || displayValue(item)}</span>
                  <span className="domain-row__meta">{formatDate(getField(item, 'startsAt') || getField(item, 'startTime'), locale)}</span>
                </span>
                <Button onClick={() => book.mutate(itemId(item))} loading={book.isPending}>{t('student.officeHours.book')}</Button>
              </div>
            ))}
          </div>
        )}
      </Card>
      <Card title={t('student.officeHours.bookings')}>
        {mine.length === 0 ? <EmptyState title={t('student.officeHours.noBookings')} /> : (
          <div className="student-list">
            {mine.map((item) => (
              <div className="domain-row" key={itemId(item)}>
                <span className="domain-row__icon" aria-hidden="true"><CalendarClock size={18} /></span>
                <span className="domain-row__main">
                  <span className="domain-row__title">{displayValue(item, ['teacherName', 'TeacherName']) || displayValue(item)}</span>
                  <span className="domain-row__meta">{formatDate(getField(item, 'startsAt') || getField(item, 'startTime'), locale)}</span>
                </span>
              </div>
            ))}
          </div>
        )}
      </Card>
    </>
  )
}

// =============================================================================
// Notifications / announcements / suggestions
// =============================================================================
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
      {unread !== null && <Alert variant="info" title={t('student.notifications.unread')}>{unread}</Alert>}
      {query.isError && <ErrorState error={query.error} onRetry={query.refetch} />}
      {mark.isError && <ErrorState error={mark.error} />}
      {items.length === 0 ? <EmptyState icon={Bell} title={t('student.empty.notifications')} /> : (
        <ul className="ui-list">
          {items.map((item) => {
            const read = getField(item, 'isRead')
            return (
              <li className={`ui-list__item${read ? '' : ' ui-list__item--unread'}`} key={itemId(item)}>
                <div className="ui-list__body">
                  <div className="ui-list__title">{displayValue(item)}</div>
                  <div className="ui-list__meta">{displayValue(item, ['body', 'Body', 'message', 'Message'])}</div>
                  <div className="ui-list__meta ui-muted">{formatDate(getField(item, 'createdAt'), locale)}</div>
                </div>
                {!read && <Button variant="ghost" onClick={() => mark.mutate(itemId(item))} loading={mark.isPending}>{t('student.notifications.markRead')}</Button>}
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
  const query = useStudentQuery(queryKeys.student.announcements(userId), (signal) => studentApi.announcements(signal), { staleTime: STALE.medium })
  return (
    <>
      <PageHeader title={t('student.announcements.title')} description={t('student.announcements.description')} />
      <QueryBoundary query={query} loadingFallback={<Loading />} emptyWhen={(d) => !d?.length} emptyTitle={t('student.empty.announcements')} emptyIcon={Megaphone}>
        {(items) => (
          <div className="student-list">
            {items.map((item) => (
              <div className="ui-list__item" key={itemId(item)} style={{ borderInlineStart: '3px solid var(--accent)' }}>
                <div className="ui-list__body">
                  <div className="ui-list__title">{displayValue(item)}</div>
                  <div className="ui-list__meta">{displayValue(item, ['body', 'Body', 'message', 'Message', 'content', 'Content'])}</div>
                  <div className="ui-list__meta ui-muted">{formatDate(getField(item, 'createdAt') || getField(item, 'publishedAt'), locale)}</div>
                </div>
              </div>
            ))}
          </div>
        )}
      </QueryBoundary>
    </>
  )
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
        <form className="stack" onSubmit={(event) => { event.preventDefault(); if (title.trim() && body.trim()) mutation.mutate() }}>
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

// =============================================================================
// Badges / points / streaks
// =============================================================================
function BadgesPage({ userId, locale }) {
  const { t } = useTranslation()
  const query = useStudentQuery(queryKeys.student.badges(userId), (signal) => studentApi.badges(userId, signal), { staleTime: STALE.medium })
  const points = useStudentQuery(queryKeys.student.points(userId), (signal) => studentApi.points(userId, signal), { staleTime: STALE.short })
  const ledger = useStudentQuery(queryKeys.student.pointsLedger(userId), (signal) => studentApi.pointsLedger(userId, signal), { staleTime: STALE.short })
  const leaderboard = useStudentQuery(queryKeys.student.pointsLeaderboard(userId), (signal) => studentApi.pointsLeaderboard({}, signal), { staleTime: STALE.short })
  const earned = query.data ? toItems(settledData(query.data.earned)) : []
  const catalog = query.data ? toItems(settledData(query.data.catalog)) : []
  const streak = query.data ? toObject(settledData(query.data.streak)) : null
  const streakDays = streak ? toItems(streak.days ?? streak.Days) : []
  return (
    <>
      <PageHeader title={t('student.badges.title')} description={t('student.badges.description')} />
      {query.isError && <ErrorState error={query.error} onRetry={query.refetch} />}

      <div className="student-dashboard">
        <Metric icon={Medal} accent="var(--warning)" label={t('student.points.title')} value={points.isLoading ? '…' : points.isError ? '—' : (points.data?.totalPoints ?? 0)} />
        <Metric icon={Trophy} accent="var(--orange)" label={t('student.badges.earned')} value={earned.length} />
        <Metric icon={Flame} accent="var(--orange)" label={t('student.streaks.title')} value={streak ? (streak.current ?? streak.Current ?? '—') : '—'} />
      </div>

      <Card title={t('student.streaks.title')}>
        {streakDays.length > 0
          ? <StreakStrip days={streakDays.map((d, i) => ({ key: i, active: Boolean(getField(d, 'active') ?? getField(d, 'isActive')), label: displayValue(d, ['label', 'Label']) }))} />
          : <NotEnoughData compact title={t('student.streaks.empty')} />}
      </Card>

      <Card title={t('student.points.ledger')}>
        {ledger.isLoading ? <Loading />
          : ledger.isError ? <ErrorState error={ledger.error} onRetry={ledger.refetch} />
          : (ledger.data?.length ?? 0) === 0 ? <EmptyState title={t('student.points.empty')} />
          : (
            <ul className="ui-list">
              {ledger.data.map((item, i) => (
                <li className="ui-list__item" key={itemId(item) || i}>
                  <div className="ui-list__body">
                    <div className="ui-list__title">{displayValue(item, ['reason', 'Reason']) || displayValue(item)}</div>
                    <div className="ui-list__meta ui-muted">{formatDate(getField(item, 'createdAt'), locale)}</div>
                  </div>
                  <Chip tone="brand">{getField(item, 'points') ?? getField(item, 'Points')}</Chip>
                </li>
              ))}
            </ul>
          )}
      </Card>

      <Card title={t('student.points.leaderboard')}>
        {leaderboard.isLoading ? <Loading />
          : leaderboard.isError ? <ErrorState error={leaderboard.error} onRetry={leaderboard.refetch} />
          : (leaderboard.data?.length ?? 0) === 0 ? <EmptyState title={t('student.points.leaderboardEmpty')} />
          : (
            <ul className="domain-lb-list">
              {leaderboard.data.map((item, i) => (
                <LeaderboardRow
                  key={itemId(item) || i}
                  rank={getField(item, 'rank') ?? i + 1}
                  name={displayValue(item, ['studentName', 'StudentName', 'name', 'Name']) || itemId(item)}
                  points={getField(item, 'totalPoints') ?? getField(item, 'points')}
                  isMe={itemId(item) === userId}
                  pointsLabel={t('student.points.unit', 'pts')}
                />
              ))}
            </ul>
          )}
      </Card>

      <Card title={t('student.badges.earned')}>
        {earned.length === 0 ? <EmptyState icon={Medal} title={t('student.empty.badges')} /> : (
          <div className="ui-grid ui-grid--auto">
            {earned.map((item) => (
              <div className="domain-card" key={itemId(item)}>
                <div className="domain-card__head">
                  <span className="domain-card__icon" aria-hidden="true"><Medal size={20} /></span>
                </div>
                <h3 className="domain-card__title">{displayValue(item)}</h3>
              </div>
            ))}
          </div>
        )}
      </Card>

      {catalog.length > 0 && (
        <Card title={t('student.badges.catalog')}>
          <div className="ui-grid ui-grid--auto">
            {catalog.map((item) => (
              <div className="domain-card" key={itemId(item)}>
                <div className="domain-card__head">
                  <span className="domain-card__icon" aria-hidden="true"><Medal size={20} /></span>
                </div>
                <h3 className="domain-card__title">{displayValue(item)}</h3>
              </div>
            ))}
          </div>
        </Card>
      )}
    </>
  )
}

// =============================================================================
// Settings
// =============================================================================
function AccountSettingsPage() {
  const { t } = useTranslation()
  return (
    <>
      <PageHeader title={t('student.settings.title')} description={t('student.settings.description')} />
      <div className="ui-split ui-split--even">
        <Card title={t('student.settings.language')}><p className="ui-muted">{t('student.settings.languageLocal')}</p></Card>
        <Card title={t('nav.security')}>
          <Link className="ui-btn ui-btn--primary" to="/app/security"><ShieldCheck size={16} aria-hidden="true" /> {t('student.settings.changePassword')}</Link>
        </Card>
      </div>
    </>
  )
}

// =============================================================================
// View dispatcher
// =============================================================================
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
