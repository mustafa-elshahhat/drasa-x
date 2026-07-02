import { useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { DateField, SelectField, TextField } from '../../../shared/form'
import { Alert, Button, Card, FormModal } from '../../../shared/ui'
import { ErrorState } from '../../../shared/feedback'
import { Head, List } from '../../../features/school/components'
import { useSchoolQuery } from '../../../features/school/helpers'
import { schoolApi } from '../../../features/school/schoolApi'
import { displayValue, getField, itemId } from '../../../features/student/studentUtils'
import { STALE, queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

// The update endpoint has no AcademicYearId field (a term cannot be moved to a
// different year via update), so the edit form intentionally omits it.
function editFields(t) {
  return [
    { name: 'name', label: t('school.common.name'), required: true },
    { name: 'code', label: t('school.common.code'), required: true },
    { name: 'order', label: t('school.common.order'), type: 'number' },
    { name: 'startDate', label: t('school.common.startDate'), type: 'date' },
    { name: 'endDate', label: t('school.common.endDate'), type: 'date' },
  ]
}

function toEditInitial(item) {
  const dateOnly = (v) => (v ? String(v).slice(0, 10) : '')
  return {
    name: getField(item, 'name') || '',
    code: getField(item, 'code') || '',
    order: getField(item, 'order') ?? 1,
    startDate: dateOnly(getField(item, 'startDate')),
    endDate: dateOnly(getField(item, 'endDate')),
  }
}

function TermsPage({ userId, locale }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const query = useSchoolQuery(queryKeys.school.terms(userId), (s) => schoolApi.terms(s))
  const years = useSchoolQuery(queryKeys.school.academicYears(userId), (s) => schoolApi.academicYears(s), { staleTime: STALE.medium })
  const [form, setForm] = useState({ name: '', code: '', order: 1, academicYearId: '', startDate: '', endDate: '' })
  const invalidate = () => qc.invalidateQueries({ queryKey: queryKeys.school.terms(userId) })
  const create = useMutation({
    mutationFn: () => schoolApi.createTerm({ ...form, order: Number(form.order) || 1, startDate: form.startDate || null, endDate: form.endDate || null }),
    onSuccess: () => { setForm({ name: '', code: '', order: 1, academicYearId: '', startDate: '', endDate: '' }); invalidate() },
  })
  const set = (k) => (e) => setForm((f) => ({ ...f, [k]: e.target.value }))
  const yearItems = Array.isArray(years.data) ? years.data : []

  const [editingItem, setEditingItem] = useState(null)
  const update = useMutation({
    mutationFn: (values) => schoolApi.updateTerm(itemId(editingItem), {
      name: values.name, code: values.code, order: Number(values.order) || 1,
      startDate: values.startDate || null, endDate: values.endDate || null,
    }),
    onSuccess: () => { setEditingItem(null); invalidate() },
  })
  const archive = useMutation({ mutationFn: (id) => schoolApi.archiveTerm(id), onSuccess: invalidate })

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
      {archive.isError && <ErrorState error={archive.error} onRetry={() => archive.reset()} />}
      <List
        query={query}
        empty={t('school.empty.generic')}
        locale={locale}
        rowActions={(item) => (
          <span className="cluster">
            <Button variant="secondary" onClick={() => setEditingItem(item)}>{t('school.common.edit')}</Button>
            <Button variant="danger" onClick={() => archive.mutate(itemId(item))} loading={archive.isPending}>{t('school.common.archive')}</Button>
          </span>
        )}
      />
      <FormModal
        open={Boolean(editingItem)}
        onClose={() => setEditingItem(null)}
        title={t('school.common.edit')}
        fields={editingItem ? editFields(t) : []}
        initialValues={editingItem ? toEditInitial(editingItem) : {}}
        onSubmit={(values) => update.mutate(values)}
        submitting={update.isPending}
        error={update.error}
        submitLabel={t('school.common.save')}
      />
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
