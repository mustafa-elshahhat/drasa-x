import { useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { SelectField, TextField, TextareaField } from '../../../components/form/fields'
import { Alert } from '../../../components/ui/Alert'
import { Button } from '../../../components/ui/Button'
import { Card } from '../../../components/ui/PageHeader'
import { ErrorState } from '../../../components/ui/states'
import { Head, List } from '../../../features/school/components'
import { AUDIENCE } from '../../../features/school/constants'
import { useSchoolQuery } from '../../../features/school/helpers'
import { schoolApi } from '../../../features/school/schoolApi'
import { itemId } from '../../../features/student/studentUtils'
import { queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function AnnouncementsPage({ userId, locale }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const query = useSchoolQuery(queryKeys.school.announcements(userId), (s) => schoolApi.announcements(s))
  const [form, setForm] = useState({ title: '', body: '', targetAudience: AUDIENCE.All })
  const create = useMutation({
    mutationFn: () => schoolApi.createAnnouncement({ title: form.title, body: form.body, targetAudience: Number(form.targetAudience) }),
    onSuccess: () => { setForm({ title: '', body: '', targetAudience: AUDIENCE.All }); qc.invalidateQueries({ queryKey: queryKeys.school.announcements(userId) }) },
  })
  const publish = useMutation({ mutationFn: ({ id, on }) => schoolApi.publishAnnouncement(id, on), onSuccess: () => qc.invalidateQueries({ queryKey: queryKeys.school.announcements(userId) }) })
  return (
    <>
      <Head view="announcements" />
      <Card title={t('school.announcements.create')}>
        {create.isSuccess && <Alert variant="success" title={t('school.common.created')}>{t('school.common.createdBody')}</Alert>}
        {create.isError && <ErrorState error={create.error} onRetry={() => create.reset()} />}
        <TextField label={t('school.common.title')} value={form.title} onChange={(e) => setForm((f) => ({ ...f, title: e.target.value }))} />
        <TextareaField label={t('school.common.message')} value={form.body} onChange={(e) => setForm((f) => ({ ...f, body: e.target.value }))} />
        <SelectField label={t('school.announcements.audience')} value={form.targetAudience} onChange={(e) => setForm((f) => ({ ...f, targetAudience: e.target.value }))}
          options={Object.entries(AUDIENCE).map(([k, v]) => ({ value: v, label: t(`school.audiences.${k}`) }))} />
        <Button onClick={() => create.mutate()} loading={create.isPending} disabled={!form.title.trim() || !form.body.trim()}>{t('school.announcements.create')}</Button>
      </Card>
      <List
        query={query}
        empty={t('school.empty.announcements')}
        locale={locale}
        rowActions={(item) => {
          const active = item.isActive ?? item.IsActive
          return <Button variant="secondary" onClick={() => publish.mutate({ id: itemId(item), on: !active })} loading={publish.isPending}>{active ? t('school.common.unpublish') : t('school.common.publish')}</Button>
        }}
      />
    </>
  )
}

// ---------------------------------------------------------------------------
// Document requests (SchoolAdmin responds + transitions status)
// ---------------------------------------------------------------------------

export default function SchoolAnnouncementsPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <AnnouncementsPage userId={userId} locale={locale} {...props} />
}
