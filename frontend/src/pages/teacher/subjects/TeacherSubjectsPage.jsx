import { useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { BookOpen, Layers } from 'lucide-react'
import { QuizCard, UnitCard } from '../../../shared/domain'
import { PageHeader } from '../../../shared/ui'
import { ErrorState } from '../../../shared/feedback'
import { Listing } from '../../../features/teacher/components'
import { useTeacherQuery } from '../../../features/teacher/helpers'
import { teacherApi } from '../../../features/teacher/teacherApi'
import { displayValue, itemId } from '../../../features/teacher/teacherUtils'
import { STALE, queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function SubjectsPage({ userId }) {
  const { t } = useTranslation()
  const { subjectId } = useParams()
  const query = useTeacherQuery(queryKeys.teacher.subjects(userId), (signal) => teacherApi.subjects(signal), { staleTime: STALE.medium })
  if (subjectId) return <CurriculumUnitsPage userId={userId} subjectId={subjectId} />
  return (
    <>
      <PageHeader title={t('teacher.subjects.title')} description={t('teacher.subjects.description')} />
      <Listing query={query} empty={t('teacher.empty.subjects')} emptyIcon={BookOpen}>
        {(items) => (
          <div className="ui-grid ui-grid--auto">
            {items.map((item) => (
              <QuizCard key={itemId(item, ['subjectId', 'SubjectId', 'id', 'Id'])} to={`/app/teacher/subjects/${itemId(item, ['subjectId', 'SubjectId', 'id', 'Id'])}`} icon={BookOpen} title={displayValue(item, ['name', 'Name'])} />
            ))}
          </div>
        )}
      </Listing>
    </>
  )
}

function CurriculumUnitsPage({ userId, subjectId }) {
  const { t } = useTranslation()
  const units = useTeacherQuery(queryKeys.teacher.units(userId, subjectId), (signal) => teacherApi.subjectUnits(subjectId, signal), { enabled: Boolean(subjectId), staleTime: STALE.medium })
  return (
    <>
      <PageHeader title={t('teacher.curriculum.units')} description={t('teacher.curriculum.description')} />
      <Listing query={units} empty={t('teacher.empty.units')} emptyIcon={Layers}>
        {(items) => (
          <div className="student-list">
            {items.map((item) => <UnitCard key={itemId(item)} to={`/app/teacher/units/${itemId(item)}`} icon={Layers} title={displayValue(item)} />)}
          </div>
        )}
      </Listing>
    </>
  )
}

export default function TeacherSubjectsPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <SubjectsPage userId={userId} locale={locale} {...props} />
}
