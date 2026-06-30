import { useTranslation } from 'react-i18next'
import { Alert } from '../../../shared/ui'
import { ErrorState } from '../../../shared/feedback'
import { Head, List } from '../../../features/school/components'
import { useSchoolQuery } from '../../../features/school/helpers'
import { schoolApi } from '../../../features/school/schoolApi'
import { queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function CurriculumPage({ userId, locale }) {
  const { t } = useTranslation()
  const grades = useSchoolQuery(queryKeys.school.grades(userId), (s) => schoolApi.grades(s))
  const subjects = useSchoolQuery(queryKeys.school.subjects(userId), (s) => schoolApi.subjects(s))
  return (
    <>
      <Head view="curriculum" />
      <Alert title={t('school.notes.curriculumTitle')}>{t('school.notes.curriculum')}</Alert>
      <section className="ui-section">
        <div className="ui-section__head"><h2 className="ui-section__title">{t('school.pages.grades.title')}</h2></div>
        <List query={grades} empty={t('school.empty.generic')} locale={locale} />
      </section>
      <section className="ui-section">
        <div className="ui-section__head"><h2 className="ui-section__title">{t('school.pages.subjects.title')}</h2></div>
        <List query={subjects} empty={t('school.empty.generic')} locale={locale} />
      </section>
    </>
  )
}

// ---------------------------------------------------------------------------
// Announcements
// ---------------------------------------------------------------------------

export default function SchoolCurriculumPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <CurriculumPage userId={userId} locale={locale} {...props} />
}
