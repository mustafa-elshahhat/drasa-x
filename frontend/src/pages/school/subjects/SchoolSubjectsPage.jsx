import { useTranslation } from 'react-i18next'
import { Users } from 'lucide-react'
import { Alert } from '../../../components/ui/Alert'
import { ErrorState } from '../../../components/ui/states'
import { Head, List } from '../../../features/school/components'
import { useSchoolQuery } from '../../../features/school/helpers'
import { schoolApi } from '../../../features/school/schoolApi'
import { queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function SubjectsPage({ userId, locale }) {
  const { t } = useTranslation()
  const query = useSchoolQuery(queryKeys.school.subjects(userId), (s) => schoolApi.subjects(s))
  return (
    <>
      <Head view="subjects" />
      <Alert title={t('school.notes.subjectsTitle')}>{t('school.notes.subjects')}</Alert>
      <List query={query} empty={t('school.empty.generic')} locale={locale} />
    </>
  )
}

// ---------------------------------------------------------------------------
// Users / Students / Teachers / Parents
// ---------------------------------------------------------------------------

export default function SchoolSubjectsPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <SubjectsPage userId={userId} locale={locale} {...props} />
}
