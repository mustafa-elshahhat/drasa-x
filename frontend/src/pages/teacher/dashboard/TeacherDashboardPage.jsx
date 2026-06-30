import { useTranslation } from 'react-i18next'
import { BookOpen, ClipboardCheck, ClipboardList, FileText, GraduationCap, Users } from 'lucide-react'
import { Metric } from '../../../components/ui/Metric'
import { PageHeader } from '../../../components/ui/PageHeader'
import { ErrorState } from '../../../components/ui/states'
import { Loading } from '../../../features/teacher/components'
import { useTeacherQuery } from '../../../features/teacher/helpers'
import { teacherApi } from '../../../features/teacher/teacherApi'
import { queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function DashboardPage({ userId }) {
  const { t } = useTranslation()
  const dashboard = useTeacherQuery(queryKeys.teacher.dashboard(userId), (signal) => teacherApi.dashboard(signal))
  const d = dashboard.data || {}
  return (
    <>
      <PageHeader title={t('teacher.dashboard.title')} description={t('teacher.dashboard.description')} />
      {dashboard.isLoading && <Loading />}
      {dashboard.isError && <ErrorState error={dashboard.error} onRetry={dashboard.refetch} />}
      {dashboard.data && (
        <div className="student-dashboard">
          <Metric to="/app/teacher/classes" icon={Users} accent="var(--purple)" value={d.assignedClassCount ?? 0} label={t('teacher.dashboard.assignedClasses')} />
          <Metric to="/app/teacher/subjects" icon={BookOpen} accent="var(--brand)" value={d.assignedSubjectCount ?? 0} label={t('teacher.dashboard.assignedSubjects')} />
          <Metric to="/app/teacher/students" icon={GraduationCap} accent="var(--orange)" value={d.studentCount ?? 0} label={t('teacher.dashboard.students')} />
          <Metric to="/app/teacher/quizzes" icon={FileText} accent="var(--info)" value={d.draftQuizCount ?? 0} label={t('teacher.dashboard.draftQuizzes')} />
          <Metric to="/app/teacher/quizzes" icon={ClipboardCheck} accent="var(--success)" value={d.publishedQuizCount ?? 0} label={t('teacher.dashboard.publishedQuizzes')} />
          <Metric to="/app/teacher/quizzes" icon={ClipboardList} accent="var(--warning)" value={d.pendingGradingCount ?? 0} label={t('teacher.dashboard.pendingGrading')} />
        </div>
      )}
    </>
  )
}

// ---------------------------------------------------------------------------
// Classes + students
// ---------------------------------------------------------------------------

export default function TeacherDashboardPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <DashboardPage userId={userId} locale={locale} {...props} />
}
