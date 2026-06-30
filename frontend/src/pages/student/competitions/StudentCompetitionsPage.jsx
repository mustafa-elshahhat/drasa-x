import { useState } from 'react'
import { useParams } from 'react-router-dom'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Trophy } from 'lucide-react'
import { useStudentContext } from '../../../features/student/helpers'
import { DetailList } from '../../../shared/data-display'
import { LeaderboardRow, QuizCard } from '../../../shared/domain'
import { TextareaField } from '../../../shared/form'
import { Alert, Button, Card, PageHeader } from '../../../shared/ui'
import { QueryBoundary, EmptyState, ErrorState } from '../../../shared/feedback'
import { useStudentQuery } from '../../../features/student/helpers'
import { Loading } from '../../../features/student/Loading'
import { studentApi } from '../../../features/student/studentApi'
import { displayValue, formatDate, getField, itemId } from '../../../features/student/studentUtils'
import { STALE, queryKeys } from '../../../lib/query/keys'

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
  const data = competition.data
  // Authoritative student flags from the backend — never inferred from raw status.
  const canEnter = getField(data, 'canEnter') === true
  const hasEntered = getField(data, 'hasEntered') === true
  const canSubmit = getField(data, 'canSubmit') === true
  const canViewLeaderboard = getField(data, 'canViewLeaderboard') === true

  // Only fetch the leaderboard once the backend says results are released (avoids a 403 while live).
  const leaderboard = useStudentQuery(
    queryKeys.student.leaderboard(userId, competitionId),
    (signal) => studentApi.leaderboard(competitionId, signal),
    { enabled: canViewLeaderboard },
  )
  const enter = useMutation({
    mutationFn: () => studentApi.enterCompetition(competitionId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKeys.student.competition(userId, competitionId) })
      qc.invalidateQueries({ queryKey: queryKeys.student.competitions(userId) })
    },
  })
  return (
    <>
      <PageHeader
        title={displayValue(data) || t('student.competitions.details')}
        description={t('student.competitions.authoritative')}
        actions={canEnter ? <Button onClick={() => enter.mutate()} loading={enter.isPending}>{t('student.competitions.enter')}</Button> : null}
      />
      {(competition.isError || enter.isError) && <ErrorState error={competition.error || enter.error} onRetry={competition.refetch} />}
      {hasEntered && <Alert variant="success" title={t('student.competitions.entered', 'You have entered this competition.')} />}
      <div className="ui-split">
        <div>
          {data && <Card title={t('student.details')}><DetailList item={data} locale={locale} /></Card>}
          {canSubmit && <CompetitionSubmissionCard userId={userId} competitionId={competitionId} locale={locale} />}
        </div>
        <Card title={t('student.leaderboard.title')}>
          {canViewLeaderboard ? (
            <QueryBoundary query={leaderboard} loadingFallback={<Loading />} emptyWhen={(d) => !d?.length} emptyTitle={t('student.empty.leaderboard')} emptyIcon={Trophy}>
              {(items) => (
                <ul className="domain-lb-list">
                  {items.map((item, i) => (
                    <LeaderboardRow
                      key={itemId(item) || i}
                      rank={getField(item, 'rank') ?? i + 1}
                      name={displayValue(item, ['studentName', 'StudentName', 'name', 'Name']) || itemId(item)}
                      points={getField(item, 'score') ?? getField(item, 'Score')}
                      pointsLabel={t('student.points.unit', 'pts')}
                    />
                  ))}
                </ul>
              )}
            </QueryBoundary>
          ) : (
            <EmptyState icon={Trophy} title={t('student.competitions.resultsPending', 'Results are not available yet.')} />
          )}
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

export default function StudentCompetitionsPage(props) {
  const { userId, locale } = useStudentContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <CompetitionsPage userId={userId} locale={locale} {...props} />
}
