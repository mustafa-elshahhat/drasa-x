import { useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { CheckboxField, DateField, TextField } from '../../../components/form/fields'
import { Alert } from '../../../components/ui/Alert'
import { Button } from '../../../components/ui/Button'
import { Card } from '../../../components/ui/PageHeader'
import { ErrorState } from '../../../components/ui/states'
import { Head, List } from '../../../features/school/components'
import { useSchoolQuery } from '../../../features/school/helpers'
import { schoolApi } from '../../../features/school/schoolApi'
import { queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function AcademicYearsPage({ userId, locale }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const query = useSchoolQuery(queryKeys.school.academicYears(userId), (s) => schoolApi.academicYears(s))
  const [form, setForm] = useState({ name: '', code: '', startDate: '', endDate: '', isCurrent: false })
  const create = useMutation({
    mutationFn: () => schoolApi.createAcademicYear({ ...form, startDate: form.startDate || null, endDate: form.endDate || null }),
    onSuccess: () => { setForm({ name: '', code: '', startDate: '', endDate: '', isCurrent: false }); qc.invalidateQueries({ queryKey: queryKeys.school.academicYears(userId) }) },
  })
  const set = (k) => (e) => setForm((f) => ({ ...f, [k]: e.target.value }))
  return (
    <>
      <Head view="academicYears" />
      <Card title={t('school.common.create')}>
        {create.isSuccess && <Alert variant="success" title={t('school.common.created')}>{t('school.common.createdBody')}</Alert>}
        {create.isError && <ErrorState error={create.error} onRetry={() => create.reset()} />}
        <div className="ui-formgrid ui-formgrid--2">
          <TextField label={t('school.common.name')} value={form.name} onChange={set('name')} />
          <TextField label={t('school.common.code')} value={form.code} onChange={set('code')} />
          <DateField label={t('school.common.startDate')} value={form.startDate} onChange={set('startDate')} />
          <DateField label={t('school.common.endDate')} value={form.endDate} onChange={set('endDate')} />
        </div>
        <CheckboxField label={t('school.common.current')} checked={form.isCurrent} onChange={(e) => setForm((f) => ({ ...f, isCurrent: e.target.checked }))} />
        <Button onClick={() => create.mutate()} loading={create.isPending} disabled={!form.name.trim() || !form.code.trim()}>{t('school.common.create')}</Button>
      </Card>
      <List query={query} empty={t('school.empty.generic')} locale={locale} />
    </>
  )
}

export default function SchoolAcademicYearsPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <AcademicYearsPage userId={userId} locale={locale} {...props} />
}
