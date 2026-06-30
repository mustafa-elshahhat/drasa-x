import { useTranslation } from 'react-i18next'
import { BookOpen, Bot, CalendarDays, ClipboardCheck, FileText, GraduationCap, Layers, Link2, Megaphone, MessageSquare, UserCog, Users } from 'lucide-react'
import { Alert } from '../../../components/ui/Alert'
import { Metric } from '../../../components/ui/Metric'
import { ErrorState } from '../../../components/ui/states'
import { Head, Loading } from '../../../features/school/components'
import { useSchoolQuery } from '../../../features/school/helpers'
import { schoolApi } from '../../../features/school/schoolApi'
import { statusLabel } from '../../../features/student/studentUtils'
import { queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function DashboardPage({ userId }) {
  const { t } = useTranslation()
  const dashboard = useSchoolQuery(queryKeys.school.dashboard(userId), (s) => schoolApi.dashboard(s))
  const d = dashboard.data || {}
  return (
    <>
      <Head view="dashboard" />
      {dashboard.isLoading && <Loading />}
      {dashboard.isError && <ErrorState error={dashboard.error} onRetry={dashboard.refetch} />}
      {dashboard.data && (
        <>
          <Alert variant="info" title={d.tenantName || t('school.dashboard.tenant')}>
            {t('school.dashboard.status')}: <strong>{statusLabel(d.tenantStatus)}</strong>
            {d.tenantType ? ` · ${statusLabel(d.tenantType)}` : ''}
          </Alert>
          <div className="student-dashboard">
            <Metric to="/app/school/students" icon={GraduationCap} accent="var(--brand)" value={d.students ?? 0} label={t('school.pages.students.title')} />
            <Metric to="/app/school/teachers" icon={Users} accent="var(--purple)" value={d.teachers ?? 0} label={t('school.pages.teachers.title')} />
            <Metric to="/app/school/parents" icon={UserCog} accent="var(--orange)" value={d.parents ?? 0} label={t('school.pages.parents.title')} />
            <Metric to="/app/school/classes" icon={Layers} accent="var(--info)" value={d.classes ?? 0} label={t('school.pages.classes.title')} />
            <Metric to="/app/school/subjects" icon={BookOpen} accent="var(--success)" value={d.subjects ?? 0} label={t('school.pages.subjects.title')} />
            <Metric to="/app/school/relationships" icon={Link2} accent="var(--info)" value={d.parentStudentLinks ?? 0} label={t('school.pages.relationships.title')} />
            <Metric to="/app/school/teacher-assignments" icon={ClipboardCheck} accent="var(--purple)" value={d.teacherClassAssignments ?? 0} label={t('school.pages.teacherAssignments.title')} />
            <Metric to="/app/school/announcements" icon={Megaphone} accent="var(--orange)" value={d.activeAnnouncements ?? 0} label={t('school.pages.announcements.title')} />
            <Metric to="/app/school/document-requests" icon={FileText} accent="var(--brand)" value={d.openParentRequests ?? 0} label={t('school.pages.documentRequests.title')} />
            <Metric to="/app/school/support" icon={MessageSquare} accent="var(--warning)" value={d.openSupportRequests ?? 0} label={t('school.pages.support.title')} />
            <Metric to="/app/school/ai-usage" icon={Bot} accent="var(--purple)" value={d.aiUsageRecords ?? 0} label={t('school.dashboard.aiRecords')} sub={t('school.dashboard.aiTokens', { tokens: d.aiTotalTokens ?? 0 })} />
            <Metric to="/app/school/academic-years" icon={CalendarDays} accent="var(--info)" value={d.academicYears ?? 0} label={t('school.pages.academicYears.title')} />
          </div>
        </>
      )}
    </>
  )
}

// ---------------------------------------------------------------------------
// Single-object read views
// ---------------------------------------------------------------------------

export default function SchoolDashboardPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <DashboardPage userId={userId} locale={locale} {...props} />
}
