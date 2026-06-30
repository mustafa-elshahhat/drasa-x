import { useTranslation } from 'react-i18next'
import { useStudentContext } from '../../../features/student/helpers'
import { DetailList } from '../../../shared/data-display'
import { NotEnoughData, ErrorState } from '../../../shared/feedback'
import { Card, PageHeader } from '../../../shared/ui'
import { ProgressBar } from '../../../shared/charts'
import { percentOf, useStudentQuery } from '../../../features/student/helpers'
import { Loading } from '../../../features/student/Loading'
import { studentApi } from '../../../features/student/studentApi'
import { toItems, toObject } from '../../../features/student/studentSchemas'
import { displayValue, itemId, settledData, settledError } from '../../../features/student/studentUtils'
import { queryKeys } from '../../../lib/query/keys'

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

export default function StudentProgressPage(props) {
  const { userId, locale } = useStudentContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <ProgressPage userId={userId} locale={locale} {...props} />
}
