import { useTranslation } from 'react-i18next'
import { Flame, Medal, Trophy } from 'lucide-react'
import { useStudentContext } from '../../../features/student/helpers'
import { LeaderboardRow } from '../../../components/domain/LeaderboardRow'
import { Chip } from '../../../components/ui/Chip'
import { Metric } from '../../../components/ui/Metric'
import { NotEnoughData } from '../../../components/ui/NotEnoughData'
import { Card, PageHeader } from '../../../components/ui/PageHeader'
import { EmptyState, ErrorState } from '../../../components/ui/states'
import { StreakStrip } from '../../../components/viz/StreakStrip'
import { useStudentQuery } from '../../../features/student/helpers'
import { Loading } from '../../../features/student/Loading'
import { studentApi } from '../../../features/student/studentApi'
import { toItems, toObject } from '../../../features/student/studentSchemas'
import { displayValue, formatDate, getField, itemId, settledData } from '../../../features/student/studentUtils'
import { STALE, queryKeys } from '../../../lib/query/keys'

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

export default function StudentBadgesPage(props) {
  const { userId, locale } = useStudentContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <BadgesPage userId={userId} locale={locale} {...props} />
}
