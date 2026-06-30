import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { ClipboardList, FileText, Sparkles } from 'lucide-react'
import { DetailList } from '../../../shared/data-display'
import { QuizCard, UnitCard } from '../../../shared/domain'
import { SelectField, TextareaField } from '../../../shared/form'
import { Alert, Button, Card, PageHeader } from '../../../shared/ui'
import { EmptyState, ErrorState } from '../../../shared/feedback'
import { toItems } from '../../../features/student/studentSchemas'
import { Listing, Loading } from '../../../features/teacher/components'
import { useTeacherQuery } from '../../../features/teacher/helpers'
import { teacherApi } from '../../../features/teacher/teacherApi'
import { displayValue, isQuizPublished, itemId, quizStatusName, submissionStatusName } from '../../../features/teacher/teacherUtils'
import { STALE, queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

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

export default function TeacherQuizzesPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <QuizzesPage userId={userId} locale={locale} {...props} />
}
