import { useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { DetailList } from '../../../shared/data-display'
import { Card, PageHeader } from '../../../shared/ui'
import { EmptyState, ErrorState } from '../../../shared/feedback'
import { Loading } from '../../../features/parent/components'
import { useParentQuery } from '../../../features/parent/helpers'
import { parentApi } from '../../../features/parent/parentApi'
import { toItems, toObject } from '../../../features/student/studentSchemas'
import { displayValue, itemId, settledData } from '../../../features/student/studentUtils'
import { queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function ChildProgressPage({ userId, locale }) {
  const { t } = useTranslation()
  const { childId } = useParams()
  const query = useParentQuery(queryKeys.parent.childProgress(userId, childId), (signal) => parentApi.childProgress(childId, signal))
  const data = query.data
  const summary = data ? toObject(settledData(data.summary)) : null
  const subjects = data ? toItems(settledData(data.subjects)) : []
  const attempts = data ? toItems(settledData(data.attempts)) : []
  const insights = data ? toItems(settledData(data.insights)) : []
  const recommendations = data ? toItems(settledData(data.recommendations)) : []
  // The backend returns an approved-only, internal-free safe projection for a
  // Parent caller (StudentProgressService.PainPointsAsync) — never unreviewed
  // items or staff-only evidence/model details (decision #7).
  const painPoints = data ? toItems(settledData(data.painPoints)) : []
  const listCard = (title, items, empty) => (
    <Card title={title}>
      {items.length ? (
        <div className="student-list">
          {items.map((item, idx) => (
            <div className="student-list__item" key={itemId(item) || idx}>
              <strong className="domain-row__title">{displayValue(item) || itemId(item)}</strong>
              <DetailList item={item} locale={locale} />
            </div>
          ))}
        </div>
      ) : <EmptyState title={empty} />}
    </Card>
  )
  return (
    <>
      <PageHeader title={t('parent.progress.title')} description={t('parent.progress.description')} />
      {query.isLoading && <Loading />}
      {query.isError && <ErrorState error={query.error} onRetry={query.refetch} />}
      {data && (
        <>
          <Card title={t('parent.progress.summary')}>
            {summary ? <DetailList item={summary} locale={locale} /> : <EmptyState title={t('parent.empty.progress')} />}
          </Card>
          {listCard(t('parent.progress.subjectProgress'), subjects, t('parent.empty.progress'))}
          {listCard(t('parent.progress.attemptHistory'), attempts, t('parent.empty.grades'))}
          <Card title={t('parent.progress.painPoints')}>
            <p className="ui-muted">{t('parent.progress.painPointsNote')}</p>
            {painPoints.length ? (
              <div className="student-list">
                {painPoints.map((item, idx) => (
                  <div className="student-list__item" key={itemId(item) || idx}>
                    <strong className="domain-row__title">{displayValue(item) || itemId(item)}</strong>
                    <DetailList item={item} locale={locale} />
                  </div>
                ))}
              </div>
            ) : <EmptyState title={t('parent.empty.painPoints')} />}
          </Card>
          <Card title={t('parent.progress.insights')}>
            <p className="ui-muted">{t('parent.progress.aiProvenance')}</p>
            {insights.length ? (
              <div className="student-list">
                {insights.map((item, idx) => <div className="student-list__item" key={idx}><DetailList item={item} locale={locale} /></div>)}
              </div>
            ) : <EmptyState title={t('parent.empty.insights')} />}
          </Card>
          {listCard(t('parent.progress.recommendations'), recommendations, t('parent.empty.insights'))}
        </>
      )}
    </>
  )
}

export default function ParentChildProgressPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <ChildProgressPage userId={userId} locale={locale} {...props} />
}
