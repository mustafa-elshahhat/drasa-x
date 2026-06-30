import { useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { CheckboxField, SelectField } from '../../../components/form/fields'
import { Alert } from '../../../components/ui/Alert'
import { Button } from '../../../components/ui/Button'
import { Card } from '../../../components/ui/PageHeader'
import { ErrorState } from '../../../components/ui/states'
import { Head, List } from '../../../features/school/components'
import { GUARDIAN } from '../../../features/school/constants'
import { useSchoolQuery } from '../../../features/school/helpers'
import { schoolApi } from '../../../features/school/schoolApi'
import { displayValue, itemId } from '../../../features/student/studentUtils'
import { STALE, queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function RelationshipsPage({ userId, locale }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const query = useSchoolQuery(queryKeys.school.relationships(userId), (s) => schoolApi.relationships(s))
  const parents = useSchoolQuery(queryKeys.school.users(userId, 'Parent'), (s) => schoolApi.users('Parent', s), { staleTime: STALE.medium })
  const students = useSchoolQuery(queryKeys.school.users(userId, 'Student'), (s) => schoolApi.users('Student', s), { staleTime: STALE.medium })
  const [form, setForm] = useState({ parentId: '', studentId: '', relationship: 2, isPrimary: false, canViewProgress: true })
  const create = useMutation({
    mutationFn: () => schoolApi.createRelationship({ parentId: form.parentId, studentId: form.studentId, relationship: Number(form.relationship), isPrimary: form.isPrimary, canViewProgress: form.canViewProgress }),
    onSuccess: () => { setForm({ parentId: '', studentId: '', relationship: 2, isPrimary: false, canViewProgress: true }); qc.invalidateQueries({ queryKey: queryKeys.school.relationships(userId) }) },
  })
  const deactivate = useMutation({ mutationFn: (id) => schoolApi.deactivateRelationship(id), onSuccess: () => qc.invalidateQueries({ queryKey: queryKeys.school.relationships(userId) }) })
  const set = (k) => (e) => setForm((f) => ({ ...f, [k]: e.target.value }))
  const parentItems = Array.isArray(parents.data) ? parents.data : []
  const studentItems = Array.isArray(students.data) ? students.data : []
  return (
    <>
      <Head view="relationships" />
      <Card title={t('school.relationships.link')}>
        {create.isSuccess && <Alert variant="success" title={t('school.common.created')}>{t('school.relationships.linked')}</Alert>}
        {create.isError && <ErrorState error={create.error} onRetry={() => create.reset()} />}
        <div className="ui-formgrid ui-formgrid--2">
          <SelectField label={t('school.common.parent')} value={form.parentId} onChange={set('parentId')}
            options={[{ value: '', label: t('school.common.choose') }, ...parentItems.map((p) => ({ value: itemId(p), label: displayValue(p, ['fullName', 'FullName']) || itemId(p) }))]} />
          <SelectField label={t('school.common.student')} value={form.studentId} onChange={set('studentId')}
            options={[{ value: '', label: t('school.common.choose') }, ...studentItems.map((sx) => ({ value: itemId(sx), label: displayValue(sx, ['fullName', 'FullName']) || itemId(sx) }))]} />
          <SelectField label={t('school.common.relationship')} value={form.relationship} onChange={set('relationship')} options={GUARDIAN.map((g, i) => ({ value: i, label: t(`school.guardian.${g}`) }))} />
        </div>
        <CheckboxField label={t('school.common.isPrimary')} checked={form.isPrimary} onChange={(e) => setForm((f) => ({ ...f, isPrimary: e.target.checked }))} />
        <CheckboxField label={t('school.common.canViewProgress')} checked={form.canViewProgress} onChange={(e) => setForm((f) => ({ ...f, canViewProgress: e.target.checked }))} />
        <Button onClick={() => create.mutate()} loading={create.isPending} disabled={!form.parentId || !form.studentId}>{t('school.relationships.link')}</Button>
      </Card>
      <List
        query={query}
        empty={t('school.empty.relationships')}
        locale={locale}
        rowActions={(item) => item.isActive && (
          <Button variant="secondary" onClick={() => deactivate.mutate(itemId(item))} loading={deactivate.isPending}>{t('school.common.deactivate')}</Button>
        )}
      />
    </>
  )
}

// ---------------------------------------------------------------------------
// Teacher assignments (subject + class)
// ---------------------------------------------------------------------------

export default function SchoolRelationshipsPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <RelationshipsPage userId={userId} locale={locale} {...props} />
}
