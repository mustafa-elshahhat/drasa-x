import { useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { DateField, SelectField, TextField } from '../../../shared/form'
import { Alert, Button, Card } from '../../../shared/ui'
import { ErrorState } from '../../../shared/feedback'
import { Head, List } from '../../../features/school/components'
import { useSchoolQuery } from '../../../features/school/helpers'
import { schoolApi } from '../../../features/school/schoolApi'
import { displayValue, itemId } from '../../../features/student/studentUtils'
import { STALE, queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function TermsPage({ userId, locale }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const query = useSchoolQuery(queryKeys.school.terms(userId), (s) => schoolApi.terms(s))
  const years = useSchoolQuery(queryKeys.school.academicYears(userId), (s) => schoolApi.academicYears(s), { staleTime: STALE.medium })
  const [form, setForm] = useState({ name: '', code: '', order: 1, academicYearId: '', startDate: '', endDate: '' })
  const create = useMutation({
    mutationFn: () => schoolApi.createTerm({ ...form, order: Number(form.order) || 1, startDate: form.startDate || null, endDate: form.endDate || null }),
    onSuccess: () => { setForm({ name: '', code: '', order: 1, academicYearId: '', startDate: '', endDate: '' }); qc.invalidateQueries({ queryKey: queryKeys.school.terms(userId) }) },
  })
  const set = (k) => (e) => setForm((f) => ({ ...f, [k]: e.target.value }))
  const yearItems = Array.isArray(years.data) ? years.data : []
  return (
    <>
      <Head view="terms" />
      <Card title={t('school.common.create')}>
        {create.isSuccess && <Alert variant="success" title={t('school.common.created')}>{t('school.common.createdBody')}</Alert>}
        {create.isError && <ErrorState error={create.error} onRetry={() => create.reset()} />}
        <div className="ui-formgrid ui-formgrid--2">
          <TextField label={t('school.common.name')} value={form.name} onChange={set('name')} />
          <TextField label={t('school.common.code')} value={form.code} onChange={set('code')} />
          <TextField label={t('school.common.order')} type="number" value={form.order} onChange={set('order')} />
          <SelectField label={t('school.common.academicYear')} value={form.academicYearId} onChange={set('academicYearId')}
            options={[{ value: '', label: t('school.common.choose') }, ...yearItems.map((y) => ({ value: itemId(y), label: displayValue(y, ['name', 'Name']) || itemId(y) }))]} />
          <DateField label={t('school.common.startDate')} value={form.startDate} onChange={set('startDate')} />
          <DateField label={t('school.common.endDate')} value={form.endDate} onChange={set('endDate')} />
        </div>
        <Button onClick={() => create.mutate()} loading={create.isPending} disabled={!form.name.trim() || !form.code.trim() || !form.academicYearId}>{t('school.common.create')}</Button>
      </Card>
      <List query={query} empty={t('school.empty.generic')} locale={locale} />
    </>
  )
}

export default function SchoolTermsPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <TermsPage userId={userId} locale={locale} {...props} />
}
