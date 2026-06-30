import { useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { FileText, Users, BookOpen, GraduationCap, ClipboardCheck, ClipboardList, Layers, PlayCircle, ShieldCheck, Sparkles, Bell } from 'lucide-react'
import { PageHeader, Card } from '../../components/ui/PageHeader'
import { Metric } from '../../components/ui/Metric'
import { EmptyState, ErrorState } from '../../components/ui/states'
import { Button } from '../../components/ui/Button'
import { Alert } from '../../components/ui/Alert'
import { Chip } from '../../components/ui/Chip'
import { Spinner } from '../../components/ui/Spinner'
import { DetailList } from '../../components/data/DetailList'
import { QuizCard } from '../../components/domain/QuizCard'
import { UnitCard } from '../../components/domain/UnitCard'
import { TextField, TextareaField, SelectField } from '../../components/form/fields'
import { useAuth } from '../../features/auth/AuthContext'
import { teacherApi } from '../../features/teacher/teacherApi'
import { displayValue, itemId, settledData, settledError, quizStatusName, isQuizPublished, submissionStatusName } from '../../features/teacher/teacherUtils'
import { queryKeys, STALE } from '../../lib/query/keys'
import { toItems, toObject } from '../../features/student/studentSchemas'

function useTeacherQuery(key, fn, options = {}) {
  // React Query forbids a query fn resolving to `undefined`; normalize to `null`.
  return useQuery({ queryKey: key, queryFn: async ({ signal }) => (await fn(signal)) ?? null, staleTime: options.staleTime ?? STALE.short, enabled: options.enabled ?? true })
}

function Loading() {
  const { t } = useTranslation()
  return <Spinner label={t('states.loading')} />
}

// Loading/error/empty boundary returning the resolved array to its children.
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
  const dashboard = useTeacherQuery(queryKeys.teacher.dashboard(userId), (signal) => teacherApi.dashboard(signal))
  const d = dashboard.data || {}
  return (
    <>
      <PageHeader title={t('teacher.dashboard.title')} description={t('teacher.dashboard.description')} />
      {dashboard.isLoading && <Loading />}
      {dashboard.isError && <ErrorState error={dashboard.error} onRetry={dashboard.refetch} />}
      {dashboard.data && (
        <div className="student-dashboard">
          <Metric to="/app/teacher/classes" icon={Users} accent="var(--purple)" value={d.assignedClassCount ?? 0} label={t('teacher.dashboard.assignedClasses')} />
          <Metric to="/app/teacher/subjects" icon={BookOpen} accent="var(--brand)" value={d.assignedSubjectCount ?? 0} label={t('teacher.dashboard.assignedSubjects')} />
          <Metric to="/app/teacher/students" icon={GraduationCap} accent="var(--orange)" value={d.studentCount ?? 0} label={t('teacher.dashboard.students')} />
          <Metric to="/app/teacher/quizzes" icon={FileText} accent="var(--info)" value={d.draftQuizCount ?? 0} label={t('teacher.dashboard.draftQuizzes')} />
          <Metric to="/app/teacher/quizzes" icon={ClipboardCheck} accent="var(--success)" value={d.publishedQuizCount ?? 0} label={t('teacher.dashboard.publishedQuizzes')} />
          <Metric to="/app/teacher/quizzes" icon={ClipboardList} accent="var(--warning)" value={d.pendingGradingCount ?? 0} label={t('teacher.dashboard.pendingGrading')} />
        </div>
      )}
    </>
  )
}

// ---------------------------------------------------------------------------
// Classes + students
// ---------------------------------------------------------------------------
function ClassesPage({ userId, locale }) {
  const { t } = useTranslation()
  const { classId } = useParams()
  const query = useTeacherQuery(queryKeys.teacher.classes(userId), (signal) => teacherApi.classes(signal), { staleTime: STALE.medium })
  if (classId) return <ClassDetailPage userId={userId} classId={classId} list={query} locale={locale} />
  return (
    <>
      <PageHeader title={t('teacher.classes.title')} description={t('teacher.classes.description')} />
      <Listing query={query} empty={t('teacher.empty.classes')} emptyIcon={Users}>
        {(items) => (
          <div className="ui-grid ui-grid--auto">
            {items.map((item) => (
              <QuizCard
                key={itemId(item, ['classId', 'ClassId', 'id', 'Id'])}
                to={`/app/teacher/classes/${itemId(item, ['classId', 'ClassId', 'id', 'Id'])}`}
                icon={Users}
                title={displayValue(item, ['name', 'Name'])}
                meta={t('teacher.classes.studentCount', { count: item.studentCount ?? item.StudentCount ?? 0 })}
              />
            ))}
          </div>
        )}
      </Listing>
    </>
  )
}

function ClassDetailPage({ userId, classId, list, locale }) {
  const { t } = useTranslation()
  const students = useTeacherQuery(queryKeys.teacher.classStudents(userId, classId), (signal) => teacherApi.classStudents(classId, signal))
  const item = list?.data?.find((c) => itemId(c, ['classId', 'ClassId', 'id', 'Id']) === classId)
  return (
    <>
      <PageHeader title={displayValue(item, ['name', 'Name']) || t('teacher.classes.details')} description={t('teacher.classes.studentsDescription')} />
      {item && <Card title={t('teacher.classes.details')}><DetailList item={item} locale={locale} /></Card>}
      <section className="ui-section">
        <div className="ui-section__head"><h2 className="ui-section__title">{t('teacher.students.title')}</h2></div>
        <Listing query={students} empty={t('teacher.empty.students')} emptyIcon={GraduationCap}>
          {(items) => (
            <div className="student-list">
              {items.map((s) => (
                <UnitCard key={itemId(s, ['studentId', 'StudentId', 'id', 'Id'])} to={`/app/teacher/students/${itemId(s, ['studentId', 'StudentId', 'id', 'Id'])}`} icon={GraduationCap} title={displayValue(s, ['fullName', 'FullName'])} />
              ))}
            </div>
          )}
        </Listing>
      </section>
    </>
  )
}

// ---------------------------------------------------------------------------
// Subjects + curriculum browse
// ---------------------------------------------------------------------------
function SubjectsPage({ userId }) {
  const { t } = useTranslation()
  const { subjectId } = useParams()
  const query = useTeacherQuery(queryKeys.teacher.subjects(userId), (signal) => teacherApi.subjects(signal), { staleTime: STALE.medium })
  if (subjectId) return <CurriculumUnitsPage userId={userId} subjectId={subjectId} />
  return (
    <>
      <PageHeader title={t('teacher.subjects.title')} description={t('teacher.subjects.description')} />
      <Listing query={query} empty={t('teacher.empty.subjects')} emptyIcon={BookOpen}>
        {(items) => (
          <div className="ui-grid ui-grid--auto">
            {items.map((item) => (
              <QuizCard key={itemId(item, ['subjectId', 'SubjectId', 'id', 'Id'])} to={`/app/teacher/subjects/${itemId(item, ['subjectId', 'SubjectId', 'id', 'Id'])}`} icon={BookOpen} title={displayValue(item, ['name', 'Name'])} />
            ))}
          </div>
        )}
      </Listing>
    </>
  )
}

function CurriculumUnitsPage({ userId, subjectId }) {
  const { t } = useTranslation()
  const units = useTeacherQuery(queryKeys.teacher.units(userId, subjectId), (signal) => teacherApi.subjectUnits(subjectId, signal), { enabled: Boolean(subjectId), staleTime: STALE.medium })
  return (
    <>
      <PageHeader title={t('teacher.curriculum.units')} description={t('teacher.curriculum.description')} />
      <Listing query={units} empty={t('teacher.empty.units')} emptyIcon={Layers}>
        {(items) => (
          <div className="student-list">
            {items.map((item) => <UnitCard key={itemId(item)} to={`/app/teacher/units/${itemId(item)}`} icon={Layers} title={displayValue(item)} />)}
          </div>
        )}
      </Listing>
    </>
  )
}

function UnitLessonsPage({ userId }) {
  const { t } = useTranslation()
  const { unitId } = useParams()
  const lessons = useTeacherQuery(queryKeys.teacher.lessons(userId, unitId), (signal) => teacherApi.unitLessons(unitId, signal), { enabled: Boolean(unitId), staleTime: STALE.medium })
  return (
    <>
      <PageHeader title={t('teacher.curriculum.lessons')} description={t('teacher.curriculum.description')} />
      <Listing query={lessons} empty={t('teacher.empty.lessons')} emptyIcon={PlayCircle}>
        {(items) => (
          <div className="student-list">
            {items.map((item) => <UnitCard key={itemId(item)} to={`/app/teacher/lessons/${itemId(item)}`} icon={PlayCircle} title={displayValue(item)} />)}
          </div>
        )}
      </Listing>
    </>
  )
}

function LessonDetailPage({ userId, locale }) {
  const { t } = useTranslation()
  const { lessonId } = useParams()
  const materials = useTeacherQuery(queryKeys.teacher.materials(userId, lessonId), (signal) => teacherApi.lessonMaterials(lessonId, signal), { enabled: Boolean(lessonId), staleTime: STALE.medium })
  return (
    <>
      <PageHeader title={t('teacher.curriculum.lessonDetails')} description={t('teacher.curriculum.lessonDescription')} />
      <Card title={t('teacher.curriculum.materials')}>
        <Listing query={materials} empty={t('teacher.empty.materials')} emptyIcon={FileText}>
          {(items) => (
            <div className="student-list">
              {items.map((item) => (
                <div className="student-list__item" key={itemId(item)}>
                  <strong className="domain-row__title">{displayValue(item)}</strong>
                  <DetailList item={item} locale={locale} />
                </div>
              ))}
            </div>
          )}
        </Listing>
      </Card>
    </>
  )
}

// ---------------------------------------------------------------------------
// Students (gradebook) + student detail
// ---------------------------------------------------------------------------
function StudentsPage({ userId }) {
  const { t } = useTranslation()
  const { studentId } = useParams()
  const query = useTeacherQuery(queryKeys.teacher.students(userId), (signal) => teacherApi.myStudents(signal), { staleTime: STALE.medium })
  if (studentId) return <StudentDetailPage userId={userId} studentId={studentId} list={query} />
  return (
    <>
      <PageHeader title={t('teacher.students.title')} description={t('teacher.students.gradebookDescription')} />
      <Listing query={query} empty={t('teacher.empty.students')} emptyIcon={GraduationCap}>
        {(items) => (
          <div className="ui-grid ui-grid--auto">
            {items.map((item) => {
              const avg = item.averageQuizPercentage ?? item.AverageQuizPercentage
              return (
                <QuizCard
                  key={itemId(item, ['studentId', 'StudentId', 'id', 'Id'])}
                  to={`/app/teacher/students/${itemId(item, ['studentId', 'StudentId', 'id', 'Id'])}`}
                  icon={GraduationCap}
                  title={displayValue(item, ['fullName', 'FullName'])}
                  meta={avg != null ? `${avg}%` : undefined}
                />
              )
            })}
          </div>
        )}
      </Listing>
    </>
  )
}

function StudentDetailPage({ userId, studentId, list, locale }) {
  const { t } = useTranslation()
  const progress = useTeacherQuery(queryKeys.teacher.studentProgress(userId, studentId), (signal) => teacherApi.studentProgress(studentId, signal), { enabled: Boolean(studentId) })
  const item = list?.data?.find((s) => itemId(s, ['studentId', 'StudentId', 'id', 'Id']) === studentId)
  const sections = [['summary', t('teacher.analytics.summary')], ['painPoints', t('teacher.analytics.painPoints')], ['insights', t('teacher.analytics.insights')], ['recommendations', t('teacher.analytics.recommendations')]]
  return (
    <>
      <PageHeader title={displayValue(item, ['fullName', 'FullName']) || t('teacher.students.details')} description={t('teacher.students.detailDescription')} />
      {item && <Card title={t('teacher.details')}><DetailList item={item} locale={locale} /></Card>}
      {progress.isError && <ErrorState error={progress.error} onRetry={progress.refetch} />}
      {sections.map(([key, title]) => {
        const result = progress.data?.[key]
        const error = settledError(result)
        const data = settledData(result)
        const items = toItems(data)
        const object = toObject(data)
        return (
          <Card key={key} title={title}>
            {error ? <ErrorState error={error} />
              : items.length ? (
                <div className="student-list">
                  {items.map((i, idx) => (
                    <div className="student-list__item" key={itemId(i) || idx}>
                      <strong className="domain-row__title">{displayValue(i) || itemId(i)}</strong>
                      <DetailList item={i} locale={locale} />
                    </div>
                  ))}
                </div>
              ) : object ? <DetailList item={object} locale={locale} /> : <EmptyState title={t('teacher.empty.noData')} />}
          </Card>
        )
      })}
      <StudentPointsCard userId={userId} studentId={studentId} locale={locale} />
    </>
  )
}

// Phase 14 (closure) — teacher/admin manual point-award control (teacherApi.awardPoints).
function StudentPointsCard({ userId, studentId }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const summary = useTeacherQuery(queryKeys.teacher.studentPoints(userId, studentId), (signal) => teacherApi.studentPoints(studentId, signal), { enabled: Boolean(studentId), staleTime: STALE.short })
  const ledger = useTeacherQuery([...queryKeys.teacher.studentPoints(userId, studentId), 'ledger'], (signal) => teacherApi.studentPointsLedger(studentId, signal), { enabled: Boolean(studentId), staleTime: STALE.short })
  const [points, setPoints] = useState('')
  const [reason, setReason] = useState('')
  const award = useMutation({
    mutationFn: () => teacherApi.awardPoints(studentId, {
      points: Number(points),
      reason: reason.trim(),
      idempotencyKey: (globalThis.crypto?.randomUUID?.() || `ui-${studentId}-${points}-${reason.length}`),
    }),
    onSuccess: () => {
      setPoints(''); setReason('')
      qc.invalidateQueries({ queryKey: queryKeys.teacher.studentPoints(userId, studentId) })
    },
  })
  const amount = Number(points)
  const valid = points !== '' && Number.isFinite(amount) && amount !== 0 && amount >= -1000 && amount <= 1000 && reason.trim().length > 0
  const total = summary.data?.totalPoints ?? summary.data?.TotalPoints ?? 0
  return (
    <Card title={t('teacher.points.title')}>
      <p className="ui-muted">{t('teacher.points.description')}</p>
      <div className="student-dashboard">
        <Metric icon={ClipboardCheck} accent="var(--purple)" label={t('teacher.points.total')} value={summary.isError ? '—' : total} />
      </div>
      {summary.isError && <ErrorState error={summary.error} onRetry={summary.refetch} />}
      <form className="stack" onSubmit={(e) => { e.preventDefault(); if (valid) award.mutate() }}>
        <div className="ui-formgrid ui-formgrid--2">
          <TextField label={t('teacher.points.amount')} type="number" value={points} onChange={(e) => setPoints(e.target.value)} hint={t('teacher.points.amountHint')} />
          <TextareaField label={t('teacher.points.reason')} value={reason} onChange={(e) => setReason(e.target.value)} maxLength={256} placeholder={t('teacher.points.reasonPlaceholder')} />
        </div>
        <Button type="submit" loading={award.isPending} disabled={!valid}>{t('teacher.points.submit')}</Button>
      </form>
      {award.isError && <ErrorState error={award.error} />}
      {award.isSuccess && <Alert variant="success" title={t('teacher.points.success')} />}
      <section className="ui-section" style={{ marginTop: 16 }}>
        <div className="ui-section__head"><h2 className="ui-section__title">{t('teacher.points.history')}</h2></div>
        <Listing query={ledger} empty={t('teacher.points.empty')}>
          {(items) => (
            <ul className="ui-list">
              {items.map((i, idx) => (
                <li className="ui-list__item" key={itemId(i) || idx}>
                  <div className="ui-list__body"><div className="ui-list__title">{displayValue(i, ['reason', 'Reason']) || displayValue(i)}</div></div>
                  <Chip tone="brand">{i.points ?? i.Points}</Chip>
                </li>
              ))}
            </ul>
          )}
        </Listing>
      </section>
    </Card>
  )
}

// ---------------------------------------------------------------------------
// Quizzes — list, authoring detail, AI draft, assign, submissions
// ---------------------------------------------------------------------------
function QuizzesPage({ userId, locale }) {
  const { t } = useTranslation()
  const { quizId } = useParams()
  const query = useTeacherQuery(queryKeys.teacher.quizzes(userId), (signal) => teacherApi.quizzes(signal))
  if (quizId) return <QuizDetailPage userId={userId} quizId={quizId} locale={locale} />
  return (
    <>
      <PageHeader title={t('teacher.quizzes.title')} description={t('teacher.quizzes.description')} actions={<Link className="ui-btn ui-btn--primary" to="/app/teacher/quiz-generate"><Sparkles size={16} aria-hidden="true" /> {t('teacher.quizzes.generate')}</Link>} />
      <Listing query={query} empty={t('teacher.empty.quizzes')} emptyIcon={ClipboardList}>
        {(items) => (
          <div className="ui-grid ui-grid--auto">
            {items.map((item) => {
              const published = isQuizPublished(item.status ?? item.Status)
              return (
                <QuizCard
                  key={itemId(item, ['quizId', 'QuizId', 'id', 'Id'])}
                  to={`/app/teacher/quizzes/${itemId(item, ['quizId', 'QuizId', 'id', 'Id'])}`}
                  icon={ClipboardList}
                  title={displayValue(item, ['title', 'Title'])}
                  status={quizStatusName(item.status ?? item.Status)}
                  statusTone={published ? 'success' : 'muted'}
                />
              )
            })}
          </div>
        )}
      </Listing>
    </>
  )
}

function QuizDetailPage({ userId, quizId, locale }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const quiz = useTeacherQuery(queryKeys.teacher.quiz(userId, quizId), (signal) => teacherApi.quiz(quizId, signal), { enabled: Boolean(quizId) })
  const submissions = useTeacherQuery(queryKeys.teacher.quizSubmissions(userId, quizId), (signal) => teacherApi.quizSubmissions(quizId, signal), { enabled: Boolean(quizId) })

  const invalidateQuiz = () => {
    qc.invalidateQueries({ queryKey: queryKeys.teacher.quiz(userId, quizId) })
    qc.invalidateQueries({ queryKey: queryKeys.teacher.quizzes(userId) })
    qc.invalidateQueries({ queryKey: queryKeys.teacher.dashboard(userId) })
  }
  const publish = useMutation({ mutationFn: () => teacherApi.publishQuiz(quizId), onSuccess: invalidateQuiz })
  const data = quiz.data || {}
  const questions = toItems(data.questions ?? data.Questions ?? [])
  const isPublished = isQuizPublished(data.status ?? data.Status)

  if (quiz.isLoading) return (<><PageHeader title={t('teacher.quizzes.details')} /><Loading /></>)
  if (quiz.isError) return <ErrorState error={quiz.error} onRetry={quiz.refetch} />

  return (
    <>
      <PageHeader
        title={displayValue(data, ['title', 'Title']) || t('teacher.quizzes.details')}
        description={t('teacher.quizzes.reviewDescription')}
        actions={!isPublished ? <Button onClick={() => publish.mutate()} loading={publish.isPending}>{t('teacher.quizzes.publish')}</Button> : null}
      />
      <Card title={t('teacher.details')}>
        <Alert variant={isPublished ? 'success' : 'info'} title={`${t('teacher.quizzes.status')}: ${quizStatusName(data.status ?? data.Status)}`}>
          {isPublished ? t('teacher.quizzes.publishedBody') : t('teacher.quizzes.draftBody')}
        </Alert>
        <DetailList item={data} locale={locale} />
      </Card>
      {publish.isError && <ErrorState error={publish.error} />}
      {publish.isSuccess && <Alert variant="success" title={t('teacher.quizzes.publishedTitle')}>{t('teacher.quizzes.publishedBody')}</Alert>}

      <Card title={t('teacher.quizzes.questions')}>
        {questions.length ? questions.map((q) => <QuestionEditor key={itemId(q)} userId={userId} quizId={quizId} question={q} editable={!isPublished} onSaved={invalidateQuiz} />) : <EmptyState title={t('teacher.empty.questions')} />}
      </Card>

      {isPublished && <AssignQuizCard userId={userId} quizId={quizId} />}

      <Card title={t('teacher.grading.submissions')}>
        <Listing query={submissions} empty={t('teacher.empty.submissions')}>
          {(items) => (
            <div className="student-list">
              {items.map((s) => (
                <UnitCard
                  key={itemId(s, ['id', 'Id', 'attemptId', 'AttemptId'])}
                  to={`/app/teacher/submissions/${itemId(s, ['id', 'Id', 'attemptId', 'AttemptId'])}`}
                  icon={FileText}
                  title={displayValue(s, ['studentName', 'StudentName', 'studentId', 'StudentId'])}
                  status={submissionStatusName(s.submissionStatus ?? s.SubmissionStatus ?? s.status ?? s.Status)}
                />
              ))}
            </div>
          )}
        </Listing>
      </Card>
    </>
  )
}

function QuestionEditor({ userId, quizId, question, editable, onSaved }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const [text, setText] = useState(displayValue(question, ['text', 'Text']))
  const [editing, setEditing] = useState(false)
  const questionId = itemId(question)
  const save = useMutation({
    mutationFn: () => teacherApi.updateQuestion(quizId, questionId, {
      id: questionId,
      text,
      type: question.type ?? question.Type ?? 0,
      order: question.order ?? question.Order ?? 1,
      points: question.points ?? question.Points ?? 1,
      correctAnswerText: question.correctAnswerText ?? question.CorrectAnswerText ?? null,
      explanation: question.explanation ?? question.Explanation ?? null,
    }),
    onSuccess: () => { setEditing(false); qc.invalidateQueries({ queryKey: queryKeys.teacher.quiz(userId, quizId) }); onSaved?.() },
  })
  return (
    <div className="student-list__item">
      {editing ? (
        <form className="stack" onSubmit={(e) => { e.preventDefault(); if (text.trim()) save.mutate() }}>
          <TextareaField label={t('teacher.quizzes.questionText')} value={text} onChange={(e) => setText(e.target.value)} maxLength={2000} />
          <div className="student-actions">
            <Button type="submit" loading={save.isPending} disabled={!text.trim()}>{t('actions.save')}</Button>
            <Button type="button" variant="secondary" onClick={() => { setEditing(false); setText(displayValue(question, ['text', 'Text'])) }}>{t('actions.cancel')}</Button>
          </div>
          {save.isError && <ErrorState error={save.error} />}
        </form>
      ) : (
        <div className="student-item">
          <strong className="domain-row__title">{displayValue(question, ['text', 'Text'])}</strong>
          {editable && <Button variant="secondary" onClick={() => setEditing(true)}>{t('teacher.quizzes.editQuestion')}</Button>}
        </div>
      )}
    </div>
  )
}

function AssignQuizCard({ userId, quizId }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const classes = useTeacherQuery(queryKeys.teacher.classes(userId), (signal) => teacherApi.classes(signal), { staleTime: STALE.medium })
  const assignments = useTeacherQuery(queryKeys.teacher.quizAssignments(userId, quizId), (signal) => teacherApi.quizAssignments(quizId, signal))
  const [classId, setClassId] = useState('')
  const assign = useMutation({
    mutationFn: () => teacherApi.assignQuiz(quizId, { schoolClassId: classId, studentIds: [] }),
    onSuccess: () => qc.invalidateQueries({ queryKey: queryKeys.teacher.quizAssignments(userId, quizId) }),
  })
  const classItems = classes.data || []
  return (
    <Card title={t('teacher.quizzes.assign')}>
      <form className="stack" onSubmit={(e) => { e.preventDefault(); if (classId) assign.mutate() }}>
        <SelectField
          label={t('teacher.quizzes.targetClass')}
          value={classId}
          onChange={(e) => setClassId(e.target.value)}
          options={[{ value: '', label: t('teacher.quizzes.chooseClass') }, ...classItems.map((c) => ({ value: itemId(c, ['classId', 'ClassId', 'id', 'Id']), label: displayValue(c, ['name', 'Name']) }))]}
        />
        <Button type="submit" loading={assign.isPending} disabled={!classId}>{t('teacher.quizzes.assignButton')}</Button>
      </form>
      {assign.isError && <ErrorState error={assign.error} />}
      {assign.isSuccess && <Alert variant="success" title={t('teacher.quizzes.assignedTitle')}>{t('teacher.quizzes.assignedBody')}</Alert>}
      <Listing query={assignments} empty={t('teacher.empty.assignments')}>
        {(items) => (
          <div className="student-list">
            {items.map((a, idx) => (
              <div className="student-list__item" key={itemId(a) || idx}><DetailList item={a} /></div>
            ))}
          </div>
        )}
      </Listing>
    </Card>
  )
}

function QuizGeneratePage({ userId }) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const qc = useQueryClient()
  const subjects = useTeacherQuery(queryKeys.teacher.subjects(userId), (signal) => teacherApi.subjects(signal), { staleTime: STALE.medium })
  const [subjectId, setSubjectId] = useState('')
  const [topic, setTopic] = useState('')
  const [numQuestions, setNumQuestions] = useState('3')
  const [difficulty, setDifficulty] = useState('core')
  const generate = useMutation({
    mutationFn: () => teacherApi.generateDraft({
      subjectId,
      topic: topic || undefined,
      numQuestions: Number(numQuestions) || 3,
      difficulty,
      questionTypes: ['mcq'],
      language: 'en',
    }),
    onSuccess: (result) => {
      qc.invalidateQueries({ queryKey: queryKeys.teacher.quizzes(userId) })
      qc.invalidateQueries({ queryKey: queryKeys.teacher.dashboard(userId) })
      const newId = result?.quizId || result?.QuizId
      if (newId) navigate(`/app/teacher/quizzes/${newId}`)
    },
  })
  const subjectItems = subjects.data || []
  return (
    <>
      <PageHeader title={t('teacher.generate.title')} description={t('teacher.generate.description')} />
      <Alert title={t('teacher.generate.draftOnlyTitle')}>{t('teacher.generate.draftOnlyBody')}</Alert>
      <Card title={t('teacher.generate.form')}>
        <form className="stack" onSubmit={(e) => { e.preventDefault(); if (subjectId) generate.mutate() }}>
          <div className="ui-formgrid ui-formgrid--2">
            <SelectField
              label={t('teacher.generate.subject')}
              value={subjectId}
              onChange={(e) => setSubjectId(e.target.value)}
              options={[{ value: '', label: t('teacher.generate.chooseSubject') }, ...subjectItems.map((s) => ({ value: itemId(s, ['subjectId', 'SubjectId', 'id', 'Id']), label: displayValue(s, ['name', 'Name']) }))]}
            />
            <TextField label={t('teacher.generate.topic')} value={topic} onChange={(e) => setTopic(e.target.value)} maxLength={200} />
            <TextField label={t('teacher.generate.numQuestions')} type="number" value={numQuestions} onChange={(e) => setNumQuestions(e.target.value)} />
            <SelectField
              label={t('teacher.generate.difficulty')}
              value={difficulty}
              onChange={(e) => setDifficulty(e.target.value)}
              options={[{ value: 'remedial', label: 'remedial' }, { value: 'core', label: 'core' }, { value: 'advanced', label: 'advanced' }]}
            />
          </div>
          <Button type="submit" loading={generate.isPending} disabled={!subjectId}>{t('teacher.generate.submit')}</Button>
        </form>
        {generate.isError && <ErrorState error={generate.error} />}
      </Card>
    </>
  )
}

// ---------------------------------------------------------------------------
// Submission detail — grade + feedback
// ---------------------------------------------------------------------------
function SubmissionDetailPage({ userId, locale }) {
  const { t } = useTranslation()
  const { attemptId } = useParams()
  const qc = useQueryClient()
  const query = useTeacherQuery(queryKeys.teacher.submission(userId, attemptId), (signal) => teacherApi.submission(attemptId, signal), { enabled: Boolean(attemptId) })
  const [feedback, setFeedback] = useState('')
  const [grades, setGrades] = useState({})

  const invalidate = () => {
    qc.invalidateQueries({ queryKey: queryKeys.teacher.submission(userId, attemptId) })
    qc.invalidateQueries({ queryKey: queryKeys.teacher.dashboard(userId) })
  }
  const buildGradePayload = (answers) => answers.map((a) => {
    const answerId = itemId(a, ['answerId', 'AnswerId', 'id', 'Id'])
    const override = grades[answerId]
    const points = override ? Number(override.points) || 0 : (a.pointsEarned ?? a.PointsEarned ?? 0)
    const correct = override ? Boolean(override.correct) : Boolean(a.isCorrect ?? a.IsCorrect)
    return { answerId, pointsEarned: points, isCorrect: correct, feedback: null }
  })
  const grade = useMutation({
    mutationFn: (answers) => teacherApi.gradeSubmission(attemptId, buildGradePayload(answers)),
    onSuccess: invalidate,
  })
  const sendFeedback = useMutation({ mutationFn: () => teacherApi.feedbackSubmission(attemptId, feedback), onSuccess: () => { setFeedback(''); invalidate() } })

  if (query.isLoading) return (<><PageHeader title={t('teacher.grading.title')} /><Loading /></>)
  if (query.isError) return <ErrorState error={query.error} onRetry={query.refetch} />
  const data = query.data || {}
  const answers = toItems(data.answers ?? data.Answers ?? [])

  return (
    <>
      <PageHeader title={t('teacher.grading.title')} description={t('teacher.grading.description')} />
      <Card title={t('teacher.details')}><DetailList item={data} locale={locale} /></Card>

      <Card title={t('teacher.grading.answers')}>
        {answers.length ? (
          <form className="stack" onSubmit={(e) => { e.preventDefault(); grade.mutate(answers) }}>
            {answers.map((a) => {
              const answerId = itemId(a, ['answerId', 'AnswerId', 'id', 'Id'])
              const g = grades[answerId] || { points: a.pointsEarned ?? a.PointsEarned ?? 0, correct: Boolean(a.isCorrect ?? a.IsCorrect) }
              return (
                <div className="student-list__item" key={answerId}>
                  <strong className="domain-row__title">{displayValue(a, ['questionText', 'QuestionText', 'answerText', 'AnswerText']) || answerId}</strong>
                  <DetailList item={a} locale={locale} />
                  <div className="ui-formgrid ui-formgrid--2">
                    <TextField label={t('teacher.grading.points')} type="number" value={String(g.points)} onChange={(e) => setGrades({ ...grades, [answerId]: { ...g, points: e.target.value } })} />
                    <label className="ui-checkbox"><input type="checkbox" checked={Boolean(g.correct)} onChange={(e) => setGrades({ ...grades, [answerId]: { ...g, correct: e.target.checked } })} /> <span>{t('teacher.grading.correct')}</span></label>
                  </div>
                </div>
              )
            })}
            <Button type="submit" loading={grade.isPending}>{t('teacher.grading.saveGrade')}</Button>
            {grade.isError && <ErrorState error={grade.error} />}
            {grade.isSuccess && <Alert variant="success" title={t('teacher.grading.gradedTitle')}>{t('teacher.grading.gradedBody')}</Alert>}
          </form>
        ) : <EmptyState title={t('teacher.empty.answers')} />}
      </Card>

      <Card title={t('teacher.grading.feedback')}>
        <form className="stack" onSubmit={(e) => { e.preventDefault(); if (feedback.trim()) sendFeedback.mutate() }}>
          <TextareaField label={t('teacher.grading.feedbackLabel')} value={feedback} onChange={(e) => setFeedback(e.target.value)} maxLength={2000} />
          <Button type="submit" loading={sendFeedback.isPending} disabled={!feedback.trim()}>{t('teacher.grading.sendFeedback')}</Button>
        </form>
        {sendFeedback.isError && <ErrorState error={sendFeedback.error} />}
        {sendFeedback.isSuccess && <Alert variant="success" title={t('teacher.grading.feedbackSentTitle')}>{t('teacher.grading.feedbackSentBody')}</Alert>}
      </Card>
    </>
  )
}

// ---------------------------------------------------------------------------
// Notifications / office hours / settings
// ---------------------------------------------------------------------------
function NotificationsPage({ userId }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const query = useTeacherQuery(queryKeys.teacher.notifications(userId), (signal) => teacherApi.notifications(signal))
  const mark = useMutation({ mutationFn: (id) => teacherApi.markNotificationRead(id), onSuccess: () => qc.invalidateQueries({ queryKey: queryKeys.teacher.notifications(userId) }) })
  const items = query.data ? toItems(settledData(query.data.items)) : []
  const unread = query.data ? toObject(settledData(query.data.count))?.unreadCount : null
  return (
    <>
      <PageHeader title={t('teacher.notifications.title')} description={t('teacher.notifications.description')} />
      {unread !== null && unread !== undefined && <Alert variant="info" title={t('teacher.notifications.unread')}>{unread}</Alert>}
      {query.isError && <ErrorState error={query.error} onRetry={query.refetch} />}
      {items.length === 0 ? <EmptyState icon={Bell} title={t('teacher.empty.notifications')} /> : (
        <ul className="ui-list">
          {items.map((item) => {
            const read = item.isRead ?? item.IsRead
            return (
              <li className={`ui-list__item${read ? '' : ' ui-list__item--unread'}`} key={itemId(item)}>
                <div className="ui-list__body">
                  <div className="ui-list__title">{displayValue(item)}</div>
                  <div className="ui-list__meta">{displayValue(item, ['body', 'Body', 'message', 'Message'])}</div>
                </div>
                {!read && <Button variant="ghost" onClick={() => mark.mutate(itemId(item))} loading={mark.isPending}>{t('teacher.notifications.markRead')}</Button>}
              </li>
            )
          })}
        </ul>
      )}
    </>
  )
}

function OfficeHoursPage({ userId, locale }) {
  const { t } = useTranslation()
  const query = useTeacherQuery(queryKeys.teacher.officeHours(userId), (signal) => teacherApi.officeHours(signal))
  return (
    <>
      <PageHeader title={t('teacher.officeHours.title')} description={t('teacher.officeHours.description')} />
      <Listing query={query} empty={t('teacher.empty.officeHours')}>
        {(items) => (
          <div className="student-list">
            {items.map((item, idx) => (
              <div className="student-list__item" key={itemId(item) || idx}>
                <strong className="domain-row__title">{displayValue(item)}</strong>
                <DetailList item={item} locale={locale} />
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
      <PageHeader title={t('teacher.settings.title')} description={t('teacher.settings.description')} />
      <Card title={t('nav.security')}>
        <Link className="ui-btn ui-btn--primary" to="/app/security"><ShieldCheck size={16} aria-hidden="true" /> {t('teacher.settings.changePassword')}</Link>
      </Card>
    </>
  )
}

export default function TeacherPortalPage({ view }) {
  const { i18n } = useTranslation()
  const { user } = useAuth()
  const userId = user?.id
  const locale = i18n.language || 'en'
  if (!userId) return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  const common = { userId, locale }
  switch (view) {
    case 'classes': return <ClassesPage {...common} />
    case 'subjects': return <SubjectsPage {...common} />
    case 'units': return <UnitLessonsPage {...common} />
    case 'lesson': return <LessonDetailPage {...common} />
    case 'students': return <StudentsPage {...common} />
    case 'quizzes': return <QuizzesPage {...common} />
    case 'quiz-generate': return <QuizGeneratePage {...common} />
    case 'submission': return <SubmissionDetailPage {...common} />
    case 'notifications': return <NotificationsPage {...common} />
    case 'office-hours': return <OfficeHoursPage {...common} />
    case 'settings': return <SettingsPage />
    default: return <DashboardPage {...common} />
  }
}
