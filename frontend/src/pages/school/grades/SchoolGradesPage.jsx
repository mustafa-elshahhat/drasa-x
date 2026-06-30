import { useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { TextField } from '../../../components/form/fields'
import { Alert } from '../../../components/ui/Alert'
import { Button } from '../../../components/ui/Button'
import { Card } from '../../../components/ui/PageHeader'
import { ErrorState } from '../../../components/ui/states'
import { Head, List } from '../../../features/school/components'
import { useSchoolQuery } from '../../../features/school/helpers'
import { schoolApi } from '../../../features/school/schoolApi'
import { queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function GradesPage({ userId, locale }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const query = useSchoolQuery(queryKeys.school.grades(userId), (s) => schoolApi.grades(s))
  const [name, setName] = useState('')
  const create = useMutation({
    mutationFn: () => schoolApi.createGrade({ name }),
    onSuccess: () => { setName(''); qc.invalidateQueries({ queryKey: queryKeys.school.grades(userId) }) },
  })
  return (
    <>
      <Head view="grades" />
      <Card title={t('school.common.create')}>
        {create.isSuccess && <Alert variant="success" title={t('school.common.created')}>{t('school.common.createdBody')}</Alert>}
        {create.isError && <ErrorState error={create.error} onRetry={() => create.reset()} />}
        <TextField label={t('school.common.name')} value={name} onChange={(e) => setName(e.target.value)} />
        <Button onClick={() => create.mutate()} loading={create.isPending} disabled={!name.trim()}>{t('school.common.create')}</Button>
      </Card>
      <List query={query} empty={t('school.empty.generic')} locale={locale} />
    </>
  )
}

export default function SchoolGradesPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <GradesPage userId={userId} locale={locale} {...props} />
}
