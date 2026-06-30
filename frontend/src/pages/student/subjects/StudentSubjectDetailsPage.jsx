import { Link, useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { BookOpen, FileText, Layers, PieChart, Sparkles, Trophy } from 'lucide-react'
import { useStudentContext } from '../../../features/student/helpers'
import { Avatar, Chip } from '../../../shared/ui'
import { QueryBoundary, ErrorState } from '../../../shared/feedback'
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
      <div className="flex justify-between items-start mb-3">
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
      <h3 className="[margin:0_0_6px] text-base font-bold text-ink grow">
        {title}
      </h3>
      {stats.total != null && (
        <div className="text-[13px] text-muted mb-3.5">
          {stats.done}/{stats.total} {isAr ? 'درس مكتمل' : 'lessons completed'}
        </div>
      )}
      <div className="h-2 rounded-lg bg-line-2 overflow-hidden">
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
      <div className="flex items-center gap-2 mb-[22px] text-sm text-muted">
        <Link to="/app/student/subjects" style={{ textDecoration: 'none', color: 'var(--text-dim)' }}>
          {t('student.subjects.title')}
        </Link>
        <span className="text-faint">{isAr ? '‹' : '›'}</span>
        <span className="font-semibold text-ink">{subjectName || '...'}</span>
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
            {IconComponent && <IconComponent size={44} className="text-[rgba(255,_255,_255,_0.92)] relative z-[1]" />}
          </div>

          <div
            className="flex flex-wrap justify-between gap-3.5 items-center mb-[22px]"
          >
            <div>
              <h1 className="[margin:0_0_6px] text-[28px] font-extrabold text-ink">
                {subjectName}
              </h1>
              <div className="flex items-center gap-2 text-muted">
                <Avatar name={teacher} size={26} color={color} />
                <span className="text-[13px]">{teacher}</span>
                <span className="text-faint">·</span>
                <span className="text-[13px]">{grade}</span>
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
                    <span className="text-[13px] text-muted font-semibold">{stat.label}</span>
                  </div>
                  <div className="text-2xl font-extrabold text-ink">{stat.value}</div>
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
