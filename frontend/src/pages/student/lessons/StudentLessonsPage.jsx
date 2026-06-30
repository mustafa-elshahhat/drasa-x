import { Link, useNavigate, useParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { ArrowRight, Check, Download, FileText, PlayCircle, Sparkles } from 'lucide-react'
import { useStudentContext } from '../../../features/student/helpers'
import { Thumb } from '../../../components/domain/Thumb'
import { Alert } from '../../../components/ui/Alert'
import { Button } from '../../../components/ui/Button'
import { Card, PageHeader } from '../../../components/ui/PageHeader'
import { QueryBoundary } from '../../../components/ui/QueryBoundary'
import { ErrorState } from '../../../components/ui/states'
import { Ring } from '../../../components/viz/Ring'
import { percentOf, useStudentQuery } from '../../../features/student/helpers'
import { Loading } from '../../../features/student/Loading'
import { studentApi } from '../../../features/student/studentApi'
import { toItems } from '../../../features/student/studentSchemas'
import { displayValue, getField, itemId, settledData } from '../../../features/student/studentUtils'
import { STALE, queryKeys } from '../../../lib/query/keys'

function LessonsPage({ userId, locale }) {
  const { t } = useTranslation()
  const { lessonId } = useParams()
  const navigate = useNavigate()
  const qc = useQueryClient()
  const isAr = locale === 'ar'

  // Fetch the context of this lesson (parent subject and unit detail, plus lesson itself)
  const lessonContextQuery = useQuery({
    queryKey: ['student', userId, 'lesson-context', lessonId],
    queryFn: async ({ signal }) => {
      const subjects = await studentApi.subjects(signal)
      for (const subj of subjects) {
        const subjId = itemId(subj)
        try {
          const units = await studentApi.units(subjId, signal)
          for (const u of units) {
            const uId = itemId(u)
            const lessons = await studentApi.lessons(uId, signal)
            const foundLesson = lessons.find((l) => itemId(l) === lessonId)
            if (foundLesson) {
              return { subject: subj, unit: u, lesson: foundLesson }
            }
          }
        } catch {
          // ignore
        }
      }
      return null
    },
    enabled: Boolean(lessonId && userId),
    staleTime: STALE.medium,
  })

  const subject = lessonContextQuery.data?.subject
  const unit = lessonContextQuery.data?.unit
  const lesson = lessonContextQuery.data?.lesson

  const subjectId = subject ? itemId(subject) : 'math'
  const unitId = unit ? itemId(unit) : 'u3'

  const subjectName = subject ? (isAr ? (getField(subject, 'nameAr') || displayValue(subject)) : displayValue(subject)) : 'Mathematics'
  const unitName = unit ? (isAr ? (getField(unit, 'nameAr') || displayValue(unit)) : displayValue(unit)) : 'Integration'
  const lessonName = lesson ? (isAr ? (getField(lesson, 'nameAr') || displayValue(lesson)) : displayValue(lesson)) : 'Intro to Integration'

  // Fetch sibling lessons in this unit to find "Up Next"
  const { data: lessonsInUnit } = useQuery({
    queryKey: ['student', userId, 'lessons-in-unit', unitId],
    queryFn: ({ signal }) => studentApi.lessons(unitId, signal),
    enabled: Boolean(unitId),
    staleTime: STALE.medium
  })

  const currentIndex = lessonsInUnit ? lessonsInUnit.findIndex(l => itemId(l) === lessonId) : -1
  const nextLesson = currentIndex !== -1 && lessonsInUnit && currentIndex < lessonsInUnit.length - 1
    ? lessonsInUnit[currentIndex + 1]
    : null

  const materials = useStudentQuery(
    queryKeys.student.materials(userId, lessonId),
    (signal) => studentApi.materials(lessonId, signal),
    { enabled: Boolean(lessonId), staleTime: STALE.medium }
  )

  const progress = useStudentQuery(
    queryKeys.student.progress(userId),
    (signal) => studentApi.progress(userId, signal),
    { enabled: Boolean(lessonId) }
  )

  const complete = useMutation({
    mutationFn: () => studentApi.completeLesson(lessonId),
    onSuccess: () => qc.invalidateQueries({ queryKey: queryKeys.student.root(userId) }),
  })

  const lessonProgress = toItems(settledData(progress.data?.lessons)).find(
    (item) => item.lessonId === lessonId || item.LessonId === lessonId
  )
  const isCompleted = Boolean(lessonProgress?.isCompleted ?? lessonProgress?.IsCompleted ?? complete.data?.isCompleted)

  if (!lessonId) return <PageHeader title={t('student.lessons.title')} description={t('student.lessons.chooseUnit')} />

  const materialItems = toItems(materials.data)
  const firstMaterial = materialItems.length > 0 ? materialItems[0] : null

  // Fallback info for next lesson resources count
  const nextLessonName = nextLesson ? (isAr ? (getField(nextLesson, 'nameAr') || displayValue(nextLesson)) : displayValue(nextLesson)) : ''

  return (
    <>
      {/* Breadcrumb */}
      <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '22px', fontSize: '14px', color: 'var(--text-dim)' }}>
        <Link to="/app/student/subjects" style={{ textDecoration: 'none', color: 'var(--text-dim)' }}>
          {t('student.subjects.title', 'Subjects')}
        </Link>
        <span style={{ color: 'var(--faint)' }}>{isAr ? '‹' : '›'}</span>
        <Link to={`/app/student/subjects/${subjectId}`} style={{ textDecoration: 'none', color: 'var(--text-dim)' }}>
          {subjectName}
        </Link>
        <span style={{ color: 'var(--faint)' }}>{isAr ? '‹' : '›'}</span>
        <Link to={`/app/student/units/${unitId}`} style={{ textDecoration: 'none', color: 'var(--text-dim)' }}>
          {unitName}
        </Link>
        <span style={{ color: 'var(--faint)' }}>{isAr ? '‹' : '›'}</span>
        <span style={{ fontWeight: 600, color: 'var(--text)' }}>{lessonName}</span>
      </div>

      <div className="ui-split">
        {/* Left Column: Lesson video card, Lesson details, Lesson materials */}
        <div>
          {/* Lesson Video player placeholder */}
          <div
            style={{
              borderRadius: 'var(--radius)',
              overflow: 'hidden',
              marginBottom: '18px',
              position: 'relative',
              cursor: 'pointer',
              boxShadow: 'var(--shadow)',
            }}
            onClick={() => firstMaterial && navigate(`/app/student/materials/${itemId(firstMaterial)}`)}
          >
            <Thumb seed={lessonId} icon={PlayCircle} height={300} className="student-lesson__hero" style={{ marginBottom: 0, width: '100%', objectFit: 'cover' }} />
            <div style={{ position: 'absolute', inset: 0, display: 'flex', alignItems: 'center', justifyContent: 'center', background: 'rgba(12, 114, 136, 0.15)' }}>
              <div className="student-video-player__btn">
                <PlayCircle size={30} style={{ color: 'var(--brand)' }} />
              </div>
            </div>
          </div>

          <h1 style={{ margin: '0 0 10px', fontSize: '26px', fontWeight: 800, color: 'var(--text)' }}>
            {lessonName}
          </h1>

          <p style={{ color: 'var(--text-dim)', lineHeight: 1.7, fontSize: '15px', marginBottom: '24px' }}>
            {isAr
              ? 'في هذا الدرس ستتعلم كيفية حساب التكاملات المحددة وفهم النظرية الأساسية في التفاضل والتكامل وتطبيق التكامل لإيجاد المساحات. شاهد المحاضرة، وراجع الأمثلة المحلولة، ثم أكمل الدرس.'
              : 'In this lesson you will learn how to evaluate definite integrals, understand the fundamental theorem of calculus, and apply integration to find areas under curves. Watch the lecture, review the worked examples, then complete the lesson.'}
          </p>

          <h2 style={{ margin: '24px 0 12px', fontSize: '18px', fontWeight: 700, color: 'var(--text)' }}>
            {t('student.materials.title', 'Lesson materials')}
          </h2>

          <div style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
            <QueryBoundary query={materials} loadingFallback={<Loading />} emptyWhen={(d) => !d?.length} emptyTitle={t('student.empty.materials')} emptyIcon={FileText}>
              {(items) => (
                <div style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
                  {items.map((item) => {
                    const type = getField(item, 'type') || getField(item, 'materialType') || 'video'
                    const dur = getField(item, 'dur') || getField(item, 'duration') || (type === 'pdf' ? '8 pages' : type === 'slides' ? '22 slides' : '18:24')
                    return (
                      <Link
                        key={itemId(item)}
                        to={`/app/student/materials/${itemId(item)}`}
                        className="student-material-row"
                        style={{ textDecoration: 'none' }}
                      >
                        <div className="student-material-row__icon-tile">
                          <FileText size={20} />
                        </div>
                        <div className="student-material-row__content">
                          <div className="student-material-row__title">{displayValue(item)}</div>
                          <div className="student-material-row__subtitle">{dur}</div>
                        </div>
                        <div className="student-material-row__action">
                          <Download size={18} />
                        </div>
                      </Link>
                    )
                  })}
                </div>
              )}
            </QueryBoundary>
          </div>
        </div>

        {/* Right Column: Progress Card & Up Next Card */}
        <div>
          {/* Progress Card */}
          <Card>
            <h3 style={{ margin: '0 0 14px', fontSize: '16px', fontWeight: 700, color: 'var(--text)' }}>
              {t('student.lessons.progress', 'Your progress')}
            </h3>

            <div className="student-progress-ring-container">
              <Ring
                value={isCompleted ? 100 : (percentOf(lessonProgress) ?? 0)}
                size={110}
                centerLabel={isCompleted ? '100%' : `${percentOf(lessonProgress) ?? 0}%`}
                stroke={12}
                color={isCompleted ? 'var(--success)' : 'var(--brand)'}
              />
            </div>

            {isCompleted ? (
              <div style={{ textAlign: 'center', color: 'var(--success)', fontWeight: 700, marginBottom: '14px', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '8px' }}>
                <Check size={18} style={{ color: 'var(--success)' }} />
                <span>{t('student.lessons.completed', 'Lesson completed!')}</span>
              </div>
            ) : (
              <Button
                onClick={() => complete.mutate()}
                loading={complete.isPending}
                disabled={complete.isPending}
                style={{ width: '100%', marginBottom: '14px' }}
              >
                {t('student.lessons.markComplete', 'Mark as complete')}
              </Button>
            )}

            {complete.isSuccess && <Alert variant="success" title={t('student.lessons.saved', 'Progress saved')} />}
            {complete.isError && <ErrorState error={complete.error} />}

            <div style={{ height: '1px', background: 'var(--border)', margin: '16px 0' }} />

            <Link
              to="/app/student/ai-tutor"
              className="ui-btn"
              style={{
                width: '100%',
                backgroundColor: 'var(--brand-soft)',
                color: 'var(--brand-strong)',
                border: 'none',
                display: 'flex',
                gap: '8px',
                justifyContent: 'center',
                alignItems: 'center',
                textDecoration: 'none',
                marginBottom: '10px',
              }}
            >
              <Sparkles size={16} />
              <span>{t('student.tutor.title', 'Ask AI Tutor about this')}</span>
            </Link>

            <Link
              to="/app/student/quizzes"
              className="ui-btn ui-btn--secondary"
              style={{
                width: '100%',
                display: 'flex',
                gap: '8px',
                justifyContent: 'center',
                alignItems: 'center',
                textDecoration: 'none',
              }}
            >
              <Check size={16} />
              <span>{t('student.quizzes.takeRelated', 'Take related quiz')}</span>
            </Link>
          </Card>

          {/* Up Next Card */}
          <div style={{ marginTop: '16px' }}>
            <Card>
              <h3 style={{ margin: '0 0 12px', fontSize: '16px', fontWeight: 700, color: 'var(--text)' }}>
                {t('student.lessons.upNext', 'Up next')}
              </h3>

              {nextLesson ? (
                <button
                  onClick={() => navigate(`/app/student/lessons/${itemId(nextLesson)}`)}
                  className="student-up-next-row"
                >
                  <div className="student-up-next-row__icon-tile">
                    <ArrowRight size={18} />
                  </div>
                  <div>
                    <div className="student-up-next-row__title">{nextLessonName}</div>
                    <div className="student-up-next-row__meta">
                      {getField(nextLesson, 'dur') || getField(nextLesson, 'duration') || '45 Min'} &middot; {getField(nextLesson, 'res') || getField(nextLesson, 'resources') || 3} {t('student.lessons.resources', 'resources')}
                    </div>
                  </div>
                </button>
              ) : (
                <div style={{ fontSize: '13px', color: 'var(--text-dim)' }}>
                  {isAr ? 'لا توجد دروس تالية في هذه الوحدة.' : 'No more lessons in this unit.'}
                </div>
              )}
            </Card>
          </div>
        </div>
      </div>
    </>
  )
}

export default function StudentLessonsPage(props) {
  const { userId, locale } = useStudentContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <LessonsPage userId={userId} locale={locale} {...props} />
}
