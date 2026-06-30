import { useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { PlayCircle } from 'lucide-react'
import { UnitCard } from '../../../components/domain/UnitCard'
import { PageHeader } from '../../../components/ui/PageHeader'
import { ErrorState } from '../../../components/ui/states'
import { Listing } from '../../../features/teacher/components'
import { useTeacherQuery } from '../../../features/teacher/helpers'
import { teacherApi } from '../../../features/teacher/teacherApi'
import { displayValue, itemId } from '../../../features/teacher/teacherUtils'
import { STALE, queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function UnitLessonsPage({ userId }) {
  const { t } = useTranslation()
  const { unitId } = useParams()
  const lessons = useTeacherQuery(queryKeys.teacher.lessons(userId, unitId), (signal) => teacherApi.unitLessons(unitId, signal), { enabled: Boolean(unitId), staleTime: STALE.medium })
  return (
    <>
      <PageHeader title={t('teacher.curriculum.lessons')} description={t('teacher.curriculum.description')} />
      <Listing query={lessons} empty={t('teacher.empty.lessons')} emptyIcon={PlayCircle}>
        {(items) => (
          <div className="student-list">
            {items.map((item) => <UnitCard key={itemId(item)} to={`/app/teacher/lessons/${itemId(item)}`} icon={PlayCircle} title={displayValue(item)} />)}
          </div>
        )}
      </Listing>
    </>
  )
}

export default function TeacherUnitsPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <UnitLessonsPage userId={userId} locale={locale} {...props} />
}
