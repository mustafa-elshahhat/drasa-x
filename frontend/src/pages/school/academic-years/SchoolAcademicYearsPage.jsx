import { useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { CheckboxField, DateField, TextField } from '../../../shared/form'
import { Alert, Button, Card, FormModal } from '../../../shared/ui'
import { ErrorState } from '../../../shared/feedback'
import { Head, List } from '../../../features/school/components'
import { useSchoolQuery } from '../../../features/school/helpers'
import { schoolApi } from '../../../features/school/schoolApi'
import { getField, itemId } from '../../../features/student/studentUtils'
import { queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function editFields(t) {
  return [
    { name: 'name', label: t('school.common.name'), required: true },
    { name: 'code', label: t('school.common.code'), required: true },
    { name: 'startDate', label: t('school.common.startDate'), type: 'date' },
    { name: 'endDate', label: t('school.common.endDate'), type: 'date' },
    { name: 'isCurrent', label: t('school.common.current'), type: 'checkbox' },
  ]
}

function toEditInitial(item) {
  const dateOnly = (v) => (v ? String(v).slice(0, 10) : '')
  return {
    name: getField(item, 'name') || '',
    code: getField(item, 'code') || '',
    startDate: dateOnly(getField(item, 'startDate')),
    endDate: dateOnly(getField(item, 'endDate')),
    isCurrent: Boolean(getField(item, 'isCurrent')),
  }
}

function AcademicYearsPage({ userId, locale }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const query = useSchoolQuery(queryKeys.school.academicYears(userId), (s) => schoolApi.academicYears(s))
  const [form, setForm] = useState({ name: '', code: '', startDate: '', endDate: '', isCurrent: false })
  const invalidate = () => qc.invalidateQueries({ queryKey: queryKeys.school.academicYears(userId) })
  const create = useMutation({
    mutationFn: () => schoolApi.createAcademicYear({ ...form, startDate: form.startDate || null, endDate: form.endDate || null }),
    onSuccess: () => { setForm({ name: '', code: '', startDate: '', endDate: '', isCurrent: false }); invalidate() },
  })
  const set = (k) => (e) => setForm((f) => ({ ...f, [k]: e.target.value }))

  const [editingItem, setEditingItem] = useState(null)
  const update = useMutation({
    mutationFn: (values) => schoolApi.updateAcademicYear(itemId(editingItem), {
      name: values.name, code: values.code,
      startDate: values.startDate || null, endDate: values.endDate || null,
      isCurrent: !!values.isCurrent,
    }),
    onSuccess: () => { setEditingItem(null); invalidate() },
  })
  const archive = useMutation({ mutationFn: (id) => schoolApi.archiveAcademicYear(id), onSuccess: invalidate })

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

export default function SchoolAcademicYearsPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <AcademicYearsPage userId={userId} locale={locale} {...props} />
}
