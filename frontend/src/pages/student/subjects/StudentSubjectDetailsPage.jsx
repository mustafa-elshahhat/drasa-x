import { Link, useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { BookOpen, FileText, Layers, PieChart, Sparkles, Trophy } from 'lucide-react'
import { useStudentContext } from '../../../features/student/helpers'
import { Avatar } from '../../../components/ui/Avatar'
import { Chip } from '../../../components/ui/Chip'
import { QueryBoundary } from '../../../components/ui/QueryBoundary'
import { ErrorState } from '../../../components/ui/states'
import { percentOf, useStudentQuery } from '../../../features/student/helpers'
import { Loading } from '../../../features/student/Loading'
import { studentApi } from '../../../features/student/studentApi'
import { displayValue, getField, itemId } from '../../../features/student/studentUtils'
import { getSubjectTheme } from '../../../features/student/theme'
import { STALE, queryKeys } from '../../../lib/query/keys'
import { isDemoEnabled } from '../../../demo/isDemoEnabled'
import { DEMO_UNIT_PROGRESS } from '../../../demo/studentDemoData'

function LocalUnitCard({ to, title, stats, subjectColor, isAr }) {
  const content = (
    <div
      style={{
        textAlign: 'start',
        background: 'var(--surface)',
        border: '1px solid var(--border)',
        borderRadius: '16px',
        boxShadow: 'var(--shadow)',
        padding: '18px',
        display: 'flex',
        flexDirection: 'column',
        height: '100%',
        textDecoration: 'none',
        color: 'inherit',
        transition: 'box-shadow 0.15s ease, transform 0.15s ease',
        cursor: to ? 'pointer' : 'default'
      }}
      onMouseEnter={(e) => {
        if (to) {
          e.currentTarget.style.boxShadow = 'var(--shadow-lg)'
          e.currentTarget.style.transform = 'translateY(-2px)'
        }
      }}
      onMouseLeave={(e) => {
        if (to) {
          e.currentTarget.style.boxShadow = 'var(--shadow)'
          e.currentTarget.style.transform = 'none'
        }
      }}
    >
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '12px' }}>
        <div
          style={{
            width: '42px',
            height: '42px',
            borderRadius: '12px',
            background: `${subjectColor}18`,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center'
          }}
        >
          <BookOpen size={22} style={{ color: subjectColor }} />
        </div>
        <Chip tone={stats.tone}>{stats.label}</Chip>
      </div>
      <h3 style={{ margin: '0 0 6px', fontSize: '16px', fontWeight: 700, color: 'var(--text)', flexGrow: 1 }}>
        {title}
      </h3>
      {stats.total != null && (
        <div style={{ fontSize: '13px', color: 'var(--text-dim)', marginBottom: '14px' }}>
          {stats.done}/{stats.total} {isAr ? 'درس مكتمل' : 'lessons completed'}
        </div>
      )}
      <div style={{ height: '8px', borderRadius: '8px', background: 'var(--border-2)', overflow: 'hidden' }}>
        <div
          style={{
            width: `${Math.max(2, stats.progress)}%`,
            height: '100%',
            background: subjectColor,
            borderRadius: '8px',
            transition: 'width 0.4s ease'
          }}
        />
      </div>
    </div>
  )

  if (to) {
    return (
      <Link to={to} style={{ textDecoration: 'none', color: 'inherit' }}>
        {content}
      </Link>
    )
  }
  return content
}

function SubjectDetailsPage({ userId, locale }) {
  const { t } = useTranslation()
  const { subjectId } = useParams()
  const isAr = locale === 'ar'

  const subject = useStudentQuery(
    queryKeys.student.subject(userId, subjectId),
    (signal) => studentApi.subject(subjectId, signal),
    { enabled: Boolean(subjectId), staleTime: STALE.medium }
  )
  const units = useStudentQuery(
    queryKeys.student.units(userId, subjectId),
    (signal) => studentApi.units(subjectId, signal),
    { enabled: Boolean(subjectId), staleTime: STALE.medium }
  )

  const theme = getSubjectTheme(subject.data)
  const IconComponent = theme.icon
  const color = theme.color

  const subjectName = subject.data ? (isAr ? (getField(subject.data, 'nameAr') || displayValue(subject.data)) : displayValue(subject.data)) : ''
  const teacher = subject.data ? (getField(subject.data, 'teacherName') || getField(subject.data, 'teacher') || theme.teacher) : ''
  const grade = subject.data ? (getField(subject.data, 'gradeName') || getField(subject.data, 'grade') || 'Grade 11') : ''

  const unitsCount = subject.data ? (getField(subject.data, 'units') || theme.units) : 0
  const lessonsCount = subject.data ? (getField(subject.data, 'lessons') || theme.lessons) : 0
  const progressVal = subject.data ? (percentOf(subject.data) ?? theme.progress) : 0
  const avgScore = subject.data ? (getField(subject.data, 'averageScore') || getField(subject.data, 'avgScore') || '86%') : '86%'

  const getUnitProgress = (unit, index) => {
    const total = getField(unit, 'lessonsCount') || getField(unit, 'lessons')
    const done = getField(unit, 'completedLessonsCount') || getField(unit, 'done')
    const progressVal = percentOf(unit)

    if (total !== undefined && done !== undefined && progressVal !== null) {
      let tone = 'muted'
      let label = isAr ? 'لم يبدأ' : 'Not started'
      if (progressVal === 100) {
        tone = 'success'
        label = isAr ? 'مكتمل' : 'Done'
      } else if (progressVal > 0) {
        tone = 'warning'
        label = isAr ? 'جارٍ' : 'In progress'
      }
      return { total, done, progress: progressVal, tone, label }
    }

    // No real progress on this unit: demo mode shows sample progress, otherwise
    // an honest "not started" with no fabricated lesson counts.
    if (isDemoEnabled()) {
      const sample = DEMO_UNIT_PROGRESS[index % DEMO_UNIT_PROGRESS.length]
      const tone = sample.progress === 100 ? 'success' : sample.progress > 0 ? 'warning' : 'muted'
      const label =
        sample.progress === 100
          ? isAr ? 'مكتمل' : 'Done'
          : sample.progress > 0
            ? isAr ? 'جارٍ' : 'In progress'
            : isAr ? 'لم يبدأ' : 'Not started'
      return { ...sample, tone, label }
    }
    return { total: null, done: null, progress: 0, tone: 'muted', label: isAr ? 'لم يبدأ' : 'Not started' }
  }

  return (
    <>
      <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '22px', fontSize: '14px', color: 'var(--text-dim)' }}>
        <Link to="/app/student/subjects" style={{ textDecoration: 'none', color: 'var(--text-dim)' }}>
          {t('student.subjects.title')}
        </Link>
        <span style={{ color: 'var(--faint)' }}>{isAr ? '‹' : '›'}</span>
        <span style={{ fontWeight: 600, color: 'var(--text)' }}>{subjectName || '...'}</span>
      </div>

      {subject.isError && <ErrorState error={subject.error} onRetry={subject.refetch} />}

      {subject.data && (
        <>
          <div
            style={{
              height: '160px',
              borderRadius: '16px',
              background: `linear-gradient(135deg, ${color}, rgba(255, 255, 255, 0.25)), ${color}`,
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              position: 'relative',
              overflow: 'hidden',
              marginBottom: '22px'
            }}
          >
            <div style={{ position: 'absolute', inset: 0, background: 'radial-gradient(circle at 80% 20%, rgba(255, 255, 255, 0.25), transparent 60%)' }} />
            {IconComponent && <IconComponent size={44} style={{ color: 'rgba(255, 255, 255, 0.92)', position: 'relative', zIndex: 1 }} />}
          </div>

          <div
            style={{
              display: 'flex',
              flexWrap: 'wrap',
              justifyContent: 'space-between',
              gap: '14px',
              alignItems: 'center',
              marginBottom: '22px'
            }}
          >
            <div>
              <h1 style={{ margin: '0 0 6px', fontSize: '28px', fontWeight: 800, color: 'var(--text)' }}>
                {subjectName}
              </h1>
              <div style={{ display: 'flex', alignItems: 'center', gap: '8px', color: 'var(--text-dim)' }}>
                <Avatar name={teacher} size={26} color={color} />
                <span style={{ fontSize: '13px' }}>{teacher}</span>
                <span style={{ color: 'var(--faint)' }}>·</span>
                <span style={{ fontSize: '13px' }}>{grade}</span>
              </div>
            </div>
            <Link
              to="/app/student/ai-tutor"
              className="ui-btn ui-btn--primary"
              style={{
                display: 'inline-flex',
                alignItems: 'center',
                gap: '8px',
                boxShadow: 'var(--shadow-btn)',
                textDecoration: 'none',
                fontWeight: 600,
                borderRadius: '9px',
                padding: '11px 20px',
                fontSize: '14px'
              }}
            >
              <Sparkles size={16} aria-hidden="true" />
              <span>{isAr ? 'اسأل المعلم الذكي' : 'Ask AI Tutor'}</span>
            </Link>
          </div>

          <div className="student-subject-details__stats-grid">
            {[
              { label: t('student.units.title'), value: unitsCount, icon: BookOpen },
              { label: t('student.lessons.title'), value: lessonsCount, icon: FileText },
              { label: t('student.progress.title'), value: `${progressVal}%`, icon: PieChart },
              { label: isAr ? 'المعدل' : 'Avg score', value: avgScore, icon: Trophy }
            ].map((stat, idx) => {
              const StatIcon = stat.icon
              return (
                <div
                  key={idx}
                  style={{
                    background: 'var(--surface)',
                    borderRadius: '16px',
                    border: '1px solid var(--border)',
                    boxShadow: 'var(--shadow)',
                    padding: '16px',
                    display: 'flex',
                    flexDirection: 'column',
                    gap: '8px'
                  }}
                >
                  <div style={{ display: 'flex', alignItems: 'center', gap: '8px', color: color }}>
                    {StatIcon && <StatIcon size={18} />}
                    <span style={{ fontSize: '13px', color: 'var(--text-dim)', fontWeight: 600 }}>{stat.label}</span>
                  </div>
                  <div style={{ fontSize: '24px', fontWeight: 800, color: 'var(--text)' }}>{stat.value}</div>
                </div>
              )
            })}
          </div>
        </>
      )}

      <section className="ui-section" style={{ marginTop: '32px' }}>
        <div className="ui-section__head" style={{ marginBottom: '16px' }}>
          <h2 className="student-dashboard__section-title">
            {t('student.subjects.courseUnits')}
          </h2>
        </div>
        <QueryBoundary query={units} loadingFallback={<Loading />} emptyWhen={(d) => !d?.length} emptyTitle={t('student.empty.units')} emptyIcon={Layers}>
          {(items) => (
            <div className="student-dashboard__lessons-grid">
              {items.map((item, index) => {
                const id = itemId(item)
                const unitStats = getUnitProgress(item, index)
                const unitName = isAr ? (getField(item, 'nameAr') || displayValue(item)) : displayValue(item)
                
                return (
                  <LocalUnitCard
                    key={id || index}
                    to={`/app/student/units/${id}`}
                    title={unitName}
                    stats={unitStats}
                    subjectColor={color}
                    isAr={isAr}
                  />
                )
              })}
            </div>
          )}
        </QueryBoundary>
      </section>
    </>
  )
}

export default function StudentSubjectDetailsPage(props) {
  const { userId, locale } = useStudentContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <SubjectDetailsPage userId={userId} locale={locale} {...props} />
}
