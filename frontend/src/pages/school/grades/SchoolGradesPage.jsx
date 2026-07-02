import { useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { TextField } from '../../../shared/form'
import { Alert, Button, Card, FormModal } from '../../../shared/ui'
import { ErrorState } from '../../../shared/feedback'
import { Head, List } from '../../../features/school/components'
import { useSchoolQuery } from '../../../features/school/helpers'
import { schoolApi } from '../../../features/school/schoolApi'
import { getField, itemId } from '../../../features/student/studentUtils'
import { queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function editFields(t) {
  return [{ name: 'name', label: t('school.common.name'), required: true }]
}

function GradesPage({ userId, locale }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const query = useSchoolQuery(queryKeys.school.grades(userId), (s) => schoolApi.grades(s))
  const invalidate = () => qc.invalidateQueries({ queryKey: queryKeys.school.grades(userId) })
  const [name, setName] = useState('')
  const create = useMutation({
    mutationFn: () => schoolApi.createGrade({ name }),
    onSuccess: () => { setName(''); invalidate() },
  })

  const [editingItem, setEditingItem] = useState(null)
  const update = useMutation({
    mutationFn: (values) => schoolApi.updateGrade({ id: itemId(editingItem), name: values.name }),
    onSuccess: () => { setEditingItem(null); invalidate() },
  })
  // A hard delete (not an archive) — the row disappears for good, so it is labeled honestly.
  const remove = useMutation({ mutationFn: (id) => schoolApi.deleteGrade(id), onSuccess: invalidate })

  return (
    <>
      <Head view="grades" />
      <Card title={t('school.common.create')}>
        {create.isSuccess && <Alert variant="success" title={t('school.common.created')}>{t('school.common.createdBody')}</Alert>}
        {create.isError && <ErrorState error={create.error} onRetry={() => create.reset()} />}
        <TextField label={t('school.common.name')} value={name} onChange={(e) => setName(e.target.value)} />
        <Button onClick={() => create.mutate()} loading={create.isPending} disabled={!name.trim()}>{t('school.common.create')}</Button>
      </Card>
      {remove.isError && <ErrorState error={remove.error} onRetry={() => remove.reset()} />}
      <List
        query={query}
        empty={t('school.empty.generic')}
        locale={locale}
        rowActions={(item) => (
          <span className="cluster">
            <Button variant="secondary" onClick={() => setEditingItem(item)}>{t('school.common.edit')}</Button>
            <Button variant="danger" onClick={() => remove.mutate(itemId(item))} loading={remove.isPending}>{t('school.common.delete')}</Button>
          </span>
        )}
      />
      <FormModal
        open={Boolean(editingItem)}
        onClose={() => setEditingItem(null)}
        title={t('school.common.edit')}
        fields={editingItem ? editFields(t) : []}
        initialValues={editingItem ? { name: getField(editingItem, 'name') || '' } : {}}
        onSubmit={(values) => update.mutate(values)}
        submitting={update.isPending}
        error={update.error}
        submitLabel={t('school.common.save')}
      />
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
