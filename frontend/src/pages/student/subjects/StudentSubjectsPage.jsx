import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { BookOpen, Search } from 'lucide-react'
import { useStudentContext } from '../../../features/student/helpers'
import { Avatar } from '../../../components/ui/Avatar'
import { QueryBoundary } from '../../../components/ui/QueryBoundary'
import { EmptyState, ErrorState } from '../../../components/ui/states'
import { percentOf, useStudentQuery } from '../../../features/student/helpers'
import { Loading } from '../../../features/student/Loading'
import { studentApi } from '../../../features/student/studentApi'
import { displayValue, getField, itemId } from '../../../features/student/studentUtils'
import { getSubjectTheme } from '../../../features/student/theme'
import { STALE, queryKeys } from '../../../lib/query/keys'
import { isDemoEnabled } from '../../../demo/isDemoEnabled'
import { DEMO_SUBJECTS } from '../../../demo/studentDemoData'

function SubjectsPage({ userId }) {
  const { t, i18n } = useTranslation()
  const locale = i18n.language || 'en'
  const isAr = locale === 'ar'
  const [search, setSearch] = useState('')
  const [activeFilter, setActiveFilter] = useState(null) // 'in-progress' | 'completed' | null

  const query = useStudentQuery(
    queryKeys.student.subjects(userId),
    (signal) => studentApi.subjects(signal),
    { staleTime: STALE.medium }
  )

  const handleFilterClick = (filterType) => {
    setActiveFilter((prev) => (prev === filterType ? null : filterType))
  }

  const getFilteredItems = (rawItems) => {
    // Honest fallback: nothing when the API is empty (demo mode shows samples).
    const baseItems = rawItems && rawItems.length > 0 ? rawItems : isDemoEnabled() ? DEMO_SUBJECTS : []
    return baseItems.filter((item) => {
      const theme = getSubjectTheme(item)
      const name = isAr ? (getField(item, 'nameAr') || displayValue(item)) : displayValue(item)
      const teacher = getField(item, 'teacherName') || getField(item, 'teacher') || theme.teacher
      const progress = percentOf(item) ?? theme.progress

      // 1. Search term filter
      const matchesSearch =
        name.toLowerCase().includes(search.toLowerCase()) ||
        teacher.toLowerCase().includes(search.toLowerCase())

      if (!matchesSearch) return false

      // 2. Tab filter
      if (activeFilter === 'in-progress') {
        return progress > 0 && progress < 100
      }
      if (activeFilter === 'completed') {
        return progress === 100
      }

      return true
    })
  }

  return (
    <>
      <div style={{ display: 'flex', flexWrap: 'wrap', gap: '14px', alignItems: 'flex-end', justifyContent: 'space-between', marginBottom: '22px' }}>
        <div>
          <h1 style={{ margin: 0, fontSize: '30px', fontWeight: 800, color: 'var(--text)', letterSpacing: '-.02em' }}>
            {t('student.subjects.title')}
          </h1>
          <p style={{ margin: '6px 0 0', color: 'var(--brand)', fontSize: '15px' }}>
            {t('student.subjects.subtitle')}
          </p>
        </div>
      </div>

      <div className="ui-toolbar">
        <div className="ui-toolbar__search">
          <Search size={18} aria-hidden="true" className="ui-toolbar__search-icon" />
          <input
            type="text"
            className="ui-input"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder={t('actions.search') + '…'}
          />
        </div>
        <div className="ui-toolbar__filters">
          <button
            type="button"
            className={`ui-toolbar__pill${activeFilter === 'in-progress' ? ' is-active' : ''}`}
            onClick={() => handleFilterClick('in-progress')}
          >
            {t('student.subjects.inProgress')}
          </button>
          <button
            type="button"
            className={`ui-toolbar__pill${activeFilter === 'completed' ? ' is-active' : ''}`}
            onClick={() => handleFilterClick('completed')}
          >
            {t('student.subjects.completed')}
          </button>
        </div>
      </div>

      <QueryBoundary query={query} loadingFallback={<Loading />} emptyWhen={() => false} emptyTitle={t('student.empty.subjects')} emptyIcon={BookOpen}>
        {(items) => {
          const filtered = getFilteredItems(items)
          if (filtered.length === 0) {
            return (
              <EmptyState
                title={t('student.empty.subjects')}
                description={t('student.empty.subjectsBody', 'No subjects match your filters.')}
                icon={BookOpen}
              />
            )
          }
          return (
            <div className="student-dashboard__lessons-grid">
              {filtered.map((item) => {
                const theme = getSubjectTheme(item)
                const id = itemId(item)
                const name = isAr ? (getField(item, 'nameAr') || displayValue(item)) : displayValue(item)
                const teacher = getField(item, 'teacherName') || getField(item, 'teacher') || theme.teacher
                const units = getField(item, 'units') || theme.units
                const progress = percentOf(item) ?? theme.progress
                const IconComponent = theme.icon

                return (
                  <Link key={id || name} to={`/app/student/subjects/${id}`} className="assigned-lesson-card">
                    <div
                      className="assigned-lesson-card__banner"
                      style={{
                        background: `linear-gradient(135deg, ${theme.color}, rgba(255, 255, 255, 0.25)), ${theme.color}`
                      }}
                    >
                      <div className="assigned-lesson-card__banner-overlay" />
                      <IconComponent size={44} className="assigned-lesson-card__banner-icon" />
                    </div>
                    <div className="assigned-lesson-card__body">
                      <div className="assigned-lesson-card__header">
                        <h3 className="assigned-lesson-card__title" title={name}>{name}</h3>
                        <span className="assigned-lesson-card__units-pill">
                          {units} {t('student.subjects.unitsLabel')}
                        </span>
                      </div>
                      <div className="assigned-lesson-card__teacher">
                        <Avatar name={teacher} size={26} color={theme.color} />
                        <span className="assigned-lesson-card__teacher-name">{teacher}</span>
                      </div>
                      <div className="assigned-lesson-card__progress-info">
                        <span className="assigned-lesson-card__progress-label">{t('student.progress.title')}</span>
                        <span className="assigned-lesson-card__progress-pct" style={{ color: theme.color }}>{progress}%</span>
                      </div>
                      <div className="assigned-lesson-card__progress-bar-container">
                        <div
                          className="assigned-lesson-card__progress-bar"
                          style={{
                            width: `${Math.max(2, progress)}%`,
                            backgroundColor: theme.color
                          }}
                        />
                      </div>
                    </div>
                  </Link>
                )
              })}
            </div>
          )
        }}
      </QueryBoundary>
    </>
  )
}

export default function StudentSubjectsPage(props) {
  const { userId, locale } = useStudentContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <SubjectsPage userId={userId} locale={locale} {...props} />
}
