import { Link, useNavigate, useParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { ArrowRight, Check, Download, FileText, PlayCircle, Sparkles } from 'lucide-react'
import { useStudentContext } from '../../../features/student/helpers'
import { Thumb } from '../../../shared/domain'
import { Alert, Button, Card, PageHeader } from '../../../shared/ui'
import { QueryBoundary, ErrorState } from '../../../shared/feedback'
import { Ring } from '../../../shared/charts'
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
      <div className="flex items-center gap-2 mb-[22px] text-sm text-muted">
        <Link to="/app/student/subjects" style={{ textDecoration: 'none', color: 'var(--text-dim)' }}>
          {t('student.subjects.title', 'Subjects')}
        </Link>
        <span className="text-faint">{isAr ? '‹' : '›'}</span>
        <Link to={`/app/student/subjects/${subjectId}`} style={{ textDecoration: 'none', color: 'var(--text-dim)' }}>
          {subjectName}
        </Link>
        <span className="text-faint">{isAr ? '‹' : '›'}</span>
        <Link to={`/app/student/units/${unitId}`} style={{ textDecoration: 'none', color: 'var(--text-dim)' }}>
          {unitName}
        </Link>
        <span className="text-faint">{isAr ? '‹' : '›'}</span>
        <span className="font-semibold text-ink">{lessonName}</span>
      </div>

      <div className="ui-split">
        {/* Left Column: Lesson video card, Lesson details, Lesson materials */}
        <div>
          {/* Lesson Video player placeholder */}
          <div
            className="rounded-card overflow-hidden mb-[18px] relative cursor-pointer shadow-card"
            onClick={() => firstMaterial && navigate(`/app/student/materials/${itemId(firstMaterial)}`)}
          >
            <Thumb seed={lessonId} icon={PlayCircle} height={300} className="student-lesson__hero" style={{ marginBottom: 0, width: '100%', objectFit: 'cover' }} />
            <div className="absolute inset-0 flex items-center justify-center bg-[rgba(12,_114,_136,_0.15)]">
              <div className="student-video-player__btn">
                <PlayCircle size={30} className="text-brand" />
              </div>
            </div>
          </div>

          <h1 className="[margin:0_0_10px] text-[26px] font-extrabold text-ink">
            {lessonName}
          </h1>

          <p className="text-muted leading-[1.7] text-[15px] mb-6">
            {isAr
              ? 'في هذا الدرس ستتعلم كيفية حساب التكاملات المحددة وفهم النظرية الأساسية في التفاضل والتكامل وتطبيق التكامل لإيجاد المساحات. شاهد المحاضرة، وراجع الأمثلة المحلولة، ثم أكمل الدرس.'
              : 'In this lesson you will learn how to evaluate definite integrals, understand the fundamental theorem of calculus, and apply integration to find areas under curves. Watch the lecture, review the worked examples, then complete the lesson.'}
          </p>

          <h2 className="[margin:24px_0_12px] text-lg font-bold text-ink">
            {t('student.materials.title', 'Lesson materials')}
          </h2>

          <div className="flex flex-col gap-2.5">
            <QueryBoundary query={materials} loadingFallback={<Loading />} emptyWhen={(d) => !d?.length} emptyTitle={t('student.empty.materials')} emptyIcon={FileText}>
              {(items) => (
                <div className="flex flex-col gap-2.5">
                  {items.map((item) => {
                    const type = getField(item, 'type') || getField(item, 'materialType') || 'video'
                    const dur = getField(item, 'dur') || getField(item, 'duration') || (type === 'pdf' ? '8 pages' : type === 'slides' ? '22 slides' : '18:24')
                    return (
                      <Link
                        key={itemId(item)}
                        to={`/app/student/materials/${itemId(item)}`}
                        className="student-material-row no-underline"
                       
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
            <h3 className="[margin:0_0_14px] text-base font-bold text-ink">
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
              <div className="text-center text-success font-bold mb-3.5 flex items-center justify-center gap-2">
                <Check size={18} className="text-success" />
                <span>{t('student.lessons.completed', 'Lesson completed!')}</span>
              </div>
            ) : (
              <Button
                onClick={() => complete.mutate()}
                loading={complete.isPending}
                disabled={complete.isPending}
                className="w-full mb-3.5"
              >
                {t('student.lessons.markComplete', 'Mark as complete')}
              </Button>
            )}

            {complete.isSuccess && <Alert variant="success" title={t('student.lessons.saved', 'Progress saved')} />}
            {complete.isError && <ErrorState error={complete.error} />}

            <div className="h-[1px] bg-line [margin:16px_0]" />

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
          <div className="mt-4">
            <Card>
              <h3 className="[margin:0_0_12px] text-base font-bold text-ink">
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
                <div className="text-[13px] text-muted">
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
