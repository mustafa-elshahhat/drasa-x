import { useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { GraduationCap, Users } from 'lucide-react'
import { DetailList } from '../../../components/data/DetailList'
import { QuizCard } from '../../../components/domain/QuizCard'
import { UnitCard } from '../../../components/domain/UnitCard'
import { Card, PageHeader } from '../../../components/ui/PageHeader'
import { ErrorState } from '../../../components/ui/states'
import { Listing } from '../../../features/teacher/components'
import { useTeacherQuery } from '../../../features/teacher/helpers'
import { teacherApi } from '../../../features/teacher/teacherApi'
import { displayValue, itemId } from '../../../features/teacher/teacherUtils'
import { STALE, queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function ClassesPage({ userId, locale }) {
  const { t } = useTranslation()
  const { classId } = useParams()
  const query = useTeacherQuery(queryKeys.teacher.classes(userId), (signal) => teacherApi.classes(signal), { staleTime: STALE.medium })
  if (classId) return <ClassDetailPage userId={userId} classId={classId} list={query} locale={locale} />
  return (
    <>
      <PageHeader title={t('teacher.classes.title')} description={t('teacher.classes.description')} />
      <Listing query={query} empty={t('teacher.empty.classes')} emptyIcon={Users}>
        {(items) => (
          <div className="ui-grid ui-grid--auto">
            {items.map((item) => (
              <QuizCard
                key={itemId(item, ['classId', 'ClassId', 'id', 'Id'])}
                to={`/app/teacher/classes/${itemId(item, ['classId', 'ClassId', 'id', 'Id'])}`}
                icon={Users}
                title={displayValue(item, ['name', 'Name'])}
                meta={t('teacher.classes.studentCount', { count: item.studentCount ?? item.StudentCount ?? 0 })}
              />
            ))}
          </div>
        )}
      </Listing>
    </>
  )
}

function ClassDetailPage({ userId, classId, list, locale }) {
  const { t } = useTranslation()
  const students = useTeacherQuery(queryKeys.teacher.classStudents(userId, classId), (signal) => teacherApi.classStudents(classId, signal))
  const item = list?.data?.find((c) => itemId(c, ['classId', 'ClassId', 'id', 'Id']) === classId)
  return (
    <>
      <PageHeader title={displayValue(item, ['name', 'Name']) || t('teacher.classes.details')} description={t('teacher.classes.studentsDescription')} />
      {item && <Card title={t('teacher.classes.details')}><DetailList item={item} locale={locale} /></Card>}
      <section className="ui-section">
        <div className="ui-section__head"><h2 className="ui-section__title">{t('teacher.students.title')}</h2></div>
        <Listing query={students} empty={t('teacher.empty.students')} emptyIcon={GraduationCap}>
          {(items) => (
            <div className="student-list">
              {items.map((s) => (
                <UnitCard key={itemId(s, ['studentId', 'StudentId', 'id', 'Id'])} to={`/app/teacher/students/${itemId(s, ['studentId', 'StudentId', 'id', 'Id'])}`} icon={GraduationCap} title={displayValue(s, ['fullName', 'FullName'])} />
              ))}
            </div>
          )}
        </Listing>
      </section>
    </>
  )
}

// ---------------------------------------------------------------------------
// Subjects + curriculum browse
// ---------------------------------------------------------------------------

export default function TeacherClassesPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <ClassesPage userId={userId} locale={locale} {...props} />
}
