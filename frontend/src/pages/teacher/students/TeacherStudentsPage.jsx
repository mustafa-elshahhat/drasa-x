import { useState } from 'react'
import { useParams } from 'react-router-dom'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { ClipboardCheck, GraduationCap } from 'lucide-react'
import { DetailList, Metric } from '../../../shared/data-display'
import { QuizCard } from '../../../shared/domain'
import { TextField, TextareaField } from '../../../shared/form'
import { Alert, Button, Chip, Card, PageHeader } from '../../../shared/ui'
import { EmptyState, ErrorState } from '../../../shared/feedback'
import { toItems, toObject } from '../../../features/student/studentSchemas'
import { Listing } from '../../../features/teacher/components'
import { useTeacherQuery } from '../../../features/teacher/helpers'
import { teacherApi } from '../../../features/teacher/teacherApi'
import { displayValue, itemId, settledData, settledError } from '../../../features/teacher/teacherUtils'
import { STALE, queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

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

export default function TeacherStudentsPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <StudentsPage userId={userId} locale={locale} {...props} />
}
