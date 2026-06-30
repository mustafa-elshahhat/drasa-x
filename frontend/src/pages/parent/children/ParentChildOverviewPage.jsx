import { useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { CalendarDays, TrendingUp, Trophy } from 'lucide-react'
import { Metric } from '../../../shared/data-display'
import { Card, PageHeader } from '../../../shared/ui'
import { EmptyState, ErrorState } from '../../../shared/feedback'
import { Loading, Stat } from '../../../features/parent/components'
import { useParentQuery } from '../../../features/parent/helpers'
import { parentApi } from '../../../features/parent/parentApi'
import { queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function ChildOverviewPage({ userId }) {
  const { t } = useTranslation()
  const { childId } = useParams()
  const query = useParentQuery(queryKeys.parent.child(userId, childId), (signal) => parentApi.child(childId, signal))
  const child = query.data || null
  const s = child?.summary
  return (
    <>
      <PageHeader title={child ? (child.fullName || t('parent.child.title')) : t('parent.child.title')} description={t('parent.child.description')} />
      {query.isLoading && <Loading />}
      {query.isError && <ErrorState error={query.error} onRetry={query.refetch} />}
      {child && (
        <>
          <Card title={t('parent.child.profile')}>
            <dl className="ui-detail-list">
              <div className="ui-detail-list__row"><dt>{t('parent.child.name')}</dt><dd>{child.fullName ?? '—'}</dd></div>
              <div className="ui-detail-list__row"><dt>{t('parent.child.grade')}</dt><dd>{child.gradeId ?? '—'}</dd></div>
              <div className="ui-detail-list__row"><dt>{t('parent.child.class')}</dt><dd>{child.className ?? '—'}</dd></div>
              <div className="ui-detail-list__row"><dt>{t('parent.child.relationship')}</dt><dd>{child.relationship ?? '—'}</dd></div>
            </dl>
          </Card>
          <Card title={t('parent.progress.summary')}>
            {s ? (
              <div className="domain-child__stats">
                <Stat label={t('parent.progress.lessonsCompleted')} value={`${s.lessonsCompleted ?? 0}/${s.lessonsTracked ?? 0}`} />
                <Stat label={t('parent.progress.averageLesson')} value={s.averageLessonCompletion} />
                <Stat label={t('parent.progress.quizAttempts')} value={s.quizAttempts} />
                <Stat label={t('parent.progress.averageQuiz')} value={s.averageQuizPercentage} />
                <Stat label={t('parent.progress.subjects')} value={s.subjectsTracked} />
              </div>
            ) : <EmptyState title={t('parent.empty.progress')} />}
          </Card>
          <div className="student-dashboard">
            <Metric to={`/app/parent/children/${childId}/progress`} icon={TrendingUp} accent="var(--brand)" label={t('parent.child.viewProgress')} />
            <Metric to={`/app/parent/children/${childId}/attendance`} icon={CalendarDays} accent="var(--success)" label={t('parent.child.viewAttendance')} />
            <Metric to={`/app/parent/children/${childId}/points`} icon={Trophy} accent="var(--purple)" label={t('parent.child.viewPoints')} />
          </div>
        </>
      )}
    </>
  )
}

// Phase 14 (closure) — parent read-only gamification view for a LINKED child only.

export default function ParentChildOverviewPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <ChildOverviewPage userId={userId} locale={locale} {...props} />
}
