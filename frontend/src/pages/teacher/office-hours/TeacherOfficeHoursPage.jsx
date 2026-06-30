import { useTranslation } from 'react-i18next'
import { DetailList } from '../../../shared/data-display'
import { PageHeader } from '../../../shared/ui'
import { ErrorState } from '../../../shared/feedback'
import { Listing } from '../../../features/teacher/components'
import { useTeacherQuery } from '../../../features/teacher/helpers'
import { teacherApi } from '../../../features/teacher/teacherApi'
import { displayValue, itemId } from '../../../features/teacher/teacherUtils'
import { queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function OfficeHoursPage({ userId, locale }) {
  const { t } = useTranslation()
  const query = useTeacherQuery(queryKeys.teacher.officeHours(userId), (signal) => teacherApi.officeHours(signal))
  return (
    <>
      <PageHeader title={t('teacher.officeHours.title')} description={t('teacher.officeHours.description')} />
      <Listing query={query} empty={t('teacher.empty.officeHours')}>
        {(items) => (
          <div className="student-list">
            {items.map((item, idx) => (
              <div className="student-list__item" key={itemId(item) || idx}>
                <strong className="domain-row__title">{displayValue(item)}</strong>
                <DetailList item={item} locale={locale} />
              </div>
            ))}
          </div>
        )}
      </Listing>
    </>
  )
}

export default function TeacherOfficeHoursPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <OfficeHoursPage userId={userId} locale={locale} {...props} />
}
