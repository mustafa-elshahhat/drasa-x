import { useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { SelectField, TextField } from '../../../shared/form'
import { Alert, Button, Card } from '../../../shared/ui'
import { ErrorState } from '../../../shared/feedback'
import { Head, List } from '../../../features/school/components'
import { useSchoolQuery } from '../../../features/school/helpers'
import { schoolApi } from '../../../features/school/schoolApi'
import { displayValue, itemId } from '../../../features/student/studentUtils'
import { STALE, queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function ClassesPage({ userId, locale }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const query = useSchoolQuery(queryKeys.school.classes(userId), (s) => schoolApi.classes(s))
  const grades = useSchoolQuery(queryKeys.school.grades(userId), (s) => schoolApi.grades(s), { staleTime: STALE.medium })
  const years = useSchoolQuery(queryKeys.school.academicYears(userId), (s) => schoolApi.academicYears(s), { staleTime: STALE.medium })
  const [form, setForm] = useState({ name: '', code: '', capacity: '', gradeId: '', academicYearId: '' })
  const create = useMutation({
    mutationFn: () => schoolApi.createClass({ ...form, capacity: form.capacity ? Number(form.capacity) : null }),
    onSuccess: () => { setForm({ name: '', code: '', capacity: '', gradeId: '', academicYearId: '' }); qc.invalidateQueries({ queryKey: queryKeys.school.classes(userId) }) },
  })
  const set = (k) => (e) => setForm((f) => ({ ...f, [k]: e.target.value }))
  const gradeItems = Array.isArray(grades.data) ? grades.data : []
  const yearItems = Array.isArray(years.data) ? years.data : []
  return (
    <>
      <Head view="classes" />
      <Card title={t('school.common.create')}>
        {create.isSuccess && <Alert variant="success" title={t('school.common.created')}>{t('school.common.createdBody')}</Alert>}
        {create.isError && <ErrorState error={create.error} onRetry={() => create.reset()} />}
        <div className="ui-formgrid ui-formgrid--2">
          <TextField label={t('school.common.name')} value={form.name} onChange={set('name')} />
          <TextField label={t('school.common.code')} value={form.code} onChange={set('code')} />
          <TextField label={t('school.common.capacity')} type="number" value={form.capacity} onChange={set('capacity')} />
          <SelectField label={t('school.common.grade')} value={form.gradeId} onChange={set('gradeId')}
            options={[{ value: '', label: t('school.common.choose') }, ...gradeItems.map((g) => ({ value: itemId(g), label: displayValue(g, ['name', 'Name']) || itemId(g) }))]} />
          <SelectField label={t('school.common.academicYear')} value={form.academicYearId} onChange={set('academicYearId')}
            options={[{ value: '', label: t('school.common.choose') }, ...yearItems.map((y) => ({ value: itemId(y), label: displayValue(y, ['name', 'Name']) || itemId(y) }))]} />
        </div>
        <Button onClick={() => create.mutate()} loading={create.isPending} disabled={!form.name.trim() || !form.code.trim() || !form.gradeId || !form.academicYearId}>{t('school.common.create')}</Button>
      </Card>
      <List query={query} empty={t('school.empty.generic')} locale={locale} />
    </>
  )
}

export default function SchoolClassesPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <ClassesPage userId={userId} locale={locale} {...props} />
}
