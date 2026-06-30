import { Link, useParams } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Check, ChevronRight, Clock, FileText, PlayCircle } from 'lucide-react'
import { useStudentContext } from '../../../features/student/helpers'
import { Chip } from '../../../components/ui/Chip'
import { EmptyState, ErrorState } from '../../../components/ui/states'
import { useStudentQuery } from '../../../features/student/helpers'
import { Loading } from '../../../features/student/Loading'
import { studentApi } from '../../../features/student/studentApi'
import { toItems } from '../../../features/student/studentSchemas'
import { displayValue, getField, itemId, settledData } from '../../../features/student/studentUtils'
import { STALE, queryKeys } from '../../../lib/query/keys'
import { isDemoEnabled } from '../../../demo/isDemoEnabled'
import { DEMO_LESSON_METAS } from '../../../demo/studentDemoData'

function UnitsPage({ userId, locale }) {
  const { t } = useTranslation()
  const { unitId } = useParams()
  const isAr = locale === 'ar'

  // Fetch the context of this unit (parent subject and the unit detail itself)
  const unitContextQuery = useQuery({
    queryKey: ['student', userId, 'unit-context', unitId],
    queryFn: async ({ signal }) => {
      const subjects = await studentApi.subjects(signal)
      const results = await Promise.all(
        subjects.map(async (subj) => {
          const subjId = itemId(subj)
          try {
            const units = await studentApi.units(subjId, signal)
            const foundUnit = units.find((u) => itemId(u) === unitId)
            if (foundUnit) {
              return { subject: subj, unit: foundUnit }
            }
          } catch {
            // ignore
          }
          return null
        })
      )
      return results.find((r) => r !== null) || null
    },
    enabled: Boolean(unitId && userId),
    staleTime: STALE.medium,
  })

  // Fetch the lessons for this unit
  const lessons = useStudentQuery(
    queryKeys.student.lessons(userId, unitId),
    (signal) => studentApi.lessons(unitId, signal),
    { enabled: Boolean(unitId), staleTime: STALE.medium }
  )

  // Fetch overall student progress to determine completion
  const progress = useStudentQuery(
    queryKeys.student.progress(userId),
    (signal) => studentApi.progress(userId, signal)
  )

  if (!unitId) return <SubjectsPage userId={userId} locale={locale} />

  const isLoading = unitContextQuery.isLoading || lessons.isLoading || progress.isLoading
  const isError = unitContextQuery.isError || lessons.isError || progress.isError
  const error = unitContextQuery.error || lessons.error || progress.error

  if (isLoading) return <Loading />

  if (isError) {
    return (
      <ErrorState
        error={error}
        onRetry={() => {
          unitContextQuery.refetch()
          lessons.refetch()
          progress.refetch()
        }}
      />
    )
  }

  const context = unitContextQuery.data
  if (!context) {
    return (
      <EmptyState
        title={t('student.empty.lessons')}
        description={t('student.lessons.chooseUnit')}
        icon={PlayCircle}
      />
    )
  }

  const subject = context.subject
  const unit = context.unit

  const subjectName = isAr
    ? (getField(subject, 'nameAr') || displayValue(subject))
    : displayValue(subject)
  const subjectId = itemId(subject)

  const unitName = isAr
    ? (getField(unit, 'nameAr') || displayValue(unit))
    : displayValue(unit)

  const lessonItems = lessons.data || []
  const progressItems = toItems(settledData(progress.data?.lessons))

  const totalLessons = lessonItems.length
  const completedLessons = lessonItems.filter((item) => {
    const lessonId = itemId(item)
    const lp = progressItems.find((p) => getField(p, 'lessonId') === lessonId || getField(p, 'LessonId') === lessonId)
    return Boolean(getField(lp, 'isCompleted') || getField(lp, 'IsCompleted'))
  }).length

  const subtitleText = isAr
    ? `${completedLessons}/${totalLessons} درس مكتمل`
    : `${completedLessons}/${totalLessons} lessons completed`

  return (
    <>
      {/* Breadcrumb */}
      <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '22px', fontSize: '14px', color: 'var(--text-dim)', flexWrap: 'wrap' }}>
        <Link to="/app/student/subjects" style={{ textDecoration: 'none', color: 'var(--text-dim)' }}>
          {t('student.subjects.title')}
        </Link>
        <span style={{ color: 'var(--faint)' }}>{isAr ? '‹' : '›'}</span>
        <Link to={`/app/student/subjects/${subjectId}`} style={{ textDecoration: 'none', color: 'var(--text-dim)' }}>
          {subjectName}
        </Link>
        <span style={{ color: 'var(--faint)' }}>{isAr ? '‹' : '›'}</span>
        <span style={{ fontWeight: 600, color: 'var(--text)' }}>{unitName}</span>
      </div>

      {/* Header */}
      <div style={{ marginBottom: '22px' }}>
        <h1 style={{ margin: '0 0 6px', fontSize: '28px', fontWeight: 800, color: 'var(--text)' }}>
          {unitName}
        </h1>
        <div style={{ color: 'var(--brand)', fontWeight: 600, fontSize: '15px' }}>
          {subtitleText}
        </div>
      </div>

      {/* Lessons List */}
      {totalLessons === 0 ? (
        <EmptyState
          title={t('student.empty.lessons')}
          description={isAr ? 'لم يتم إضافة دروس بعد.' : 'No lessons have been added yet.'}
          icon={PlayCircle}
        />
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
          {lessonItems.map((item, i) => {
            const lessonId = itemId(item)
            const lessonName = isAr
              ? (getField(item, 'nameAr') || displayValue(item))
              : displayValue(item)

            const lp = progressItems.find((p) => getField(p, 'lessonId') === lessonId || getField(p, 'LessonId') === lessonId)
            const isCompleted = Boolean(getField(lp, 'isCompleted') || getField(lp, 'IsCompleted'))
            const percent = getField(lp, 'completionPercentage') || 0

            // Lesson duration/resource counts are sample metadata (no backend
            // field yet); shown only in demo mode, hidden otherwise.
            const meta = isDemoEnabled() ? DEMO_LESSON_METAS[i % DEMO_LESSON_METAS.length] : null
            const durationText = meta ? (isAr ? `${meta.mins} دقيقة` : `${meta.mins} Min`) : ''
            const resourcesText = meta
              ? isAr
                ? `${meta.res} مصادر`
                : `${meta.res} ${meta.res === 1 ? 'resource' : 'resources'}`
              : ''

            return (
              <Link
                key={lessonId}
                to={`/app/student/lessons/${lessonId}`}
                style={{
                  display: 'flex',
                  alignItems: 'center',
                  gap: '16px',
                  background: 'var(--surface)',
                  border: '1px solid var(--border)',
                  borderRadius: 'var(--radius-sm)',
                  boxShadow: 'var(--shadow)',
                  padding: '16px',
                  textDecoration: 'none',
                  color: 'inherit',
                  cursor: 'pointer',
                  transition: 'box-shadow 0.15s ease, transform 0.15s ease',
                }}
                onMouseEnter={(e) => {
                  e.currentTarget.style.boxShadow = 'var(--shadow-lg)'
                  e.currentTarget.style.transform = 'translateY(-2px)'
                }}
                onMouseLeave={(e) => {
                  e.currentTarget.style.boxShadow = 'var(--shadow)'
                  e.currentTarget.style.transform = 'none'
                }}
              >
                {/* Left Circular Indicator */}
                <div
                  style={{
                    width: '40px',
                    height: '40px',
                    borderRadius: '50%',
                    background: isCompleted ? 'var(--success-bg)' : 'var(--brand-soft)',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    flexShrink: 0,
                  }}
                >
                  {isCompleted ? (
                    <Check size={20} style={{ color: 'var(--success)' }} />
                  ) : (
                    <span style={{ fontWeight: 800, color: 'var(--brand)', fontSize: '15px' }}>
                      {i + 1}
                    </span>
                  )}
                </div>

                {/* Lesson Info */}
                <div style={{ flex: 1, minWidth: 0 }}>
                  <div style={{ fontWeight: 700, color: 'var(--text)', fontSize: '15px' }}>
                    {lessonName}
                  </div>
                  {meta && (
                    <div style={{ display: 'flex', gap: '14px', color: 'var(--text-dim)', fontSize: '12px', marginTop: '4px', flexWrap: 'wrap' }}>
                      <span style={{ display: 'flex', gap: '5px', alignItems: 'center' }}>
                        <Clock size={13} style={{ color: 'var(--text-dim)' }} />
                        {durationText}
                      </span>
                      <span style={{ display: 'flex', gap: '5px', alignItems: 'center' }}>
                        <FileText size={13} style={{ color: 'var(--text-dim)' }} />
                        {resourcesText}
                      </span>
                    </div>
                  )}
                </div>

                {/* Status Chip / Pill */}
                <div style={{ display: 'flex', alignItems: 'center', flexShrink: 0 }}>
                  {isCompleted ? (
                    <Chip tone="success">{isAr ? 'مكتمل' : 'Completed'}</Chip>
                  ) : percent > 0 ? (
                    <Chip tone="warning">{percent}%</Chip>
                  ) : (
                    <Chip tone="brand">{isAr ? 'ابدأ' : 'Start'}</Chip>
                  )}
                </div>

                {/* Chevron Icon */}
                <ChevronRight className="domain-row__chev" size={20} aria-hidden="true" style={{ flexShrink: 0 }} />
              </Link>
            )
          })}
        </div>
      )}
    </>
  )
}

export default function StudentUnitsPage(props) {
  const { userId, locale } = useStudentContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <UnitsPage userId={userId} locale={locale} {...props} />
}
