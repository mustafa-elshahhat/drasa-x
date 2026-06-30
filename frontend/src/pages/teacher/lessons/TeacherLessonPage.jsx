import { useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { FileText } from 'lucide-react'
import { DetailList } from '../../../shared/data-display'
import { Card, PageHeader } from '../../../shared/ui'
import { ErrorState } from '../../../shared/feedback'
import { Listing } from '../../../features/teacher/components'
import { useTeacherQuery } from '../../../features/teacher/helpers'
import { teacherApi } from '../../../features/teacher/teacherApi'
import { displayValue, itemId } from '../../../features/teacher/teacherUtils'
import { STALE, queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function LessonDetailPage({ userId, locale }) {
  const { t } = useTranslation()
  const { lessonId } = useParams()
  const materials = useTeacherQuery(queryKeys.teacher.materials(userId, lessonId), (signal) => teacherApi.lessonMaterials(lessonId, signal), { enabled: Boolean(lessonId), staleTime: STALE.medium })
  return (
    <>
      <PageHeader title={t('teacher.curriculum.lessonDetails')} description={t('teacher.curriculum.lessonDescription')} />
      <Card title={t('teacher.curriculum.materials')}>
        <Listing query={materials} empty={t('teacher.empty.materials')} emptyIcon={FileText}>
          {(items) => (
            <div className="student-list">
              {items.map((item) => (
                <div className="student-list__item" key={itemId(item)}>
                  <strong className="domain-row__title">{displayValue(item)}</strong>
                  <DetailList item={item} locale={locale} />
                </div>
              ))}
            </div>
          )}
        </Listing>
      </Card>
    </>
  )
}

// ---------------------------------------------------------------------------
// Students (gradebook) + student detail
// ---------------------------------------------------------------------------

export default function TeacherLessonPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <LessonDetailPage userId={userId} locale={locale} {...props} />
}
