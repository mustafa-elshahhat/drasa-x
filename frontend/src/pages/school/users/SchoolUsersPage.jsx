import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { SelectField, TextField } from '../../../shared/form'
import { Alert, Button, Card } from '../../../shared/ui'
import { ErrorState } from '../../../shared/feedback'
import { Head, List } from '../../../features/school/components'
import { USER_ROLES } from '../../../features/school/constants'
import { useSchoolQuery } from '../../../features/school/helpers'
import { schoolApi } from '../../../features/school/schoolApi'
import { toObject } from '../../../features/student/studentSchemas'
import { displayValue, itemId } from '../../../features/student/studentUtils'
import { STALE, queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function UsersPage({ userId, locale, role, canCreate }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const view = role ? role.toLowerCase() + 's' : 'users'
  const detailBase = `/app/school/${view}`
  const query = useSchoolQuery(queryKeys.school.users(userId, role || 'all'), (s) => schoolApi.users(role, s), { staleTime: STALE.medium })
  const grades = useSchoolQuery(queryKeys.school.grades(userId), (s) => schoolApi.grades(s), { staleTime: STALE.medium, enabled: Boolean(canCreate) })
  const [form, setForm] = useState({ fullName: '', loginCode: '', role: 'Student', gradeId: '' })
  const [credential, setCredential] = useState(null)
  const create = useMutation({
    mutationFn: () => schoolApi.createUser({ fullName: form.fullName, loginCode: form.loginCode, role: form.role, gradeId: form.role === 'Student' ? form.gradeId || null : null }),
    onSuccess: (data) => { setCredential(toObject(data)); setForm({ fullName: '', loginCode: '', role: 'Student', gradeId: '' }); qc.invalidateQueries({ queryKey: queryKeys.school.users(userId, role || 'all') }) },
  })
  const set = (k) => (e) => setForm((f) => ({ ...f, [k]: e.target.value }))
  const gradeItems = Array.isArray(grades.data) ? grades.data : []
  const columns = [
    { key: 'fullName', header: t('school.common.name') },
    { key: 'loginCode', header: t('school.common.loginCode') },
    { key: 'role', header: t('school.common.role'), kind: 'role' },
  ]
  return (
    <>
      <Head view={view} />
      {canCreate && (
        <Card title={t('school.users.create')}>
          {credential && (
            <Alert variant="success" title={t('school.credential.title')}>
              {t('school.credential.body')} — {t('school.common.loginCode')}: <code>{credential.loginCode}</code> · {t('school.credential.password')}: <code>{credential.temporaryPassword}</code>
            </Alert>
          )}
          {create.isError && <ErrorState error={create.error} onRetry={() => create.reset()} />}
          <div className="ui-formgrid ui-formgrid--2">
            <TextField label={t('school.common.name')} value={form.fullName} onChange={set('fullName')} />
            <TextField label={t('school.common.loginCode')} value={form.loginCode} onChange={set('loginCode')} />
            <SelectField label={t('school.common.role')} value={form.role} onChange={set('role')} options={USER_ROLES.map((r) => ({ value: r, label: t(`roles.${r}`) }))} />
            {form.role === 'Student' && (
              <SelectField label={t('school.common.grade')} value={form.gradeId} onChange={set('gradeId')}
                options={[{ value: '', label: t('school.common.choose') }, ...gradeItems.map((g) => ({ value: itemId(g), label: displayValue(g, ['name', 'Name']) || itemId(g) }))]} />
            )}
          </div>
          <Button onClick={() => create.mutate()} loading={create.isPending} disabled={!form.fullName.trim() || !form.loginCode.trim()}>{t('school.users.create')}</Button>
        </Card>
      )}
      <List
        query={query}
        columns={columns}
        empty={t('school.empty.users')}
        locale={locale}
        rowActions={(item) => <Link className="ui-btn ui-btn--secondary" to={`${detailBase}/${encodeURIComponent(itemId(item))}`}>{t('school.common.open')}</Link>}
      />
    </>
  )
}

// ---------------------------------------------------------------------------
// Relationships (parent ↔ student)
// ---------------------------------------------------------------------------

export default function SchoolUsersPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <UsersPage userId={userId} locale={locale} {...props} />
}
