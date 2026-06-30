import { useState } from 'react'
import { useParams } from 'react-router-dom'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { DetailList } from '../../../components/data/DetailList'
import { TextField, TextareaField } from '../../../components/form/fields'
import { Alert } from '../../../components/ui/Alert'
import { Button } from '../../../components/ui/Button'
import { Card, PageHeader } from '../../../components/ui/PageHeader'
import { EmptyState, ErrorState } from '../../../components/ui/states'
import { toItems } from '../../../features/student/studentSchemas'
import { Loading } from '../../../features/teacher/components'
import { useTeacherQuery } from '../../../features/teacher/helpers'
import { teacherApi } from '../../../features/teacher/teacherApi'
import { displayValue, itemId } from '../../../features/teacher/teacherUtils'
import { queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

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

export default function TeacherSubmissionPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <SubmissionDetailPage userId={userId} locale={locale} {...props} />
}
