import { useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { TextField, TextareaField } from '../../../components/form/fields'
import { Alert } from '../../../components/ui/Alert'
import { Button } from '../../../components/ui/Button'
import { Card } from '../../../components/ui/PageHeader'
import { ErrorState } from '../../../components/ui/states'
import { Head, List } from '../../../features/system/components'
import { useSystemQuery } from '../../../features/system/helpers'
import { systemApi } from '../../../features/system/systemApi'
import { queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function AnnouncementsPage({ userId, locale }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const query = useSystemQuery(queryKeys.system.announcements(userId), (s) => systemApi.announcements(s))
  const [form, setForm] = useState({ title: '', body: '' })
  const create = useMutation({
    mutationFn: () => systemApi.createAnnouncement({ title: form.title.trim(), body: form.body.trim() }),
    onSuccess: () => { setForm({ title: '', body: '' }); qc.invalidateQueries({ queryKey: queryKeys.system.announcements(userId) }) },
  })
  return (
    <>
      <Head view="announcements" />
      <Card title={t('system.announcements.create')}>
        {create.isSuccess && <Alert variant="success" title={t('system.common.created')}>{t('system.announcements.published')}</Alert>}
        {create.isError && <ErrorState error={create.error} onRetry={() => create.reset()} />}
        <TextField label={t('system.common.title')} value={form.title} onChange={(e) => setForm((f) => ({ ...f, title: e.target.value }))} />
        <TextareaField label={t('system.common.message')} value={form.body} onChange={(e) => setForm((f) => ({ ...f, body: e.target.value }))} />
        <Button onClick={() => create.mutate()} loading={create.isPending} disabled={!form.title.trim() || !form.body.trim()}>{t('system.announcements.create')}</Button>
      </Card>
      <List query={query} empty={t('system.empty.announcements')} locale={locale} />
    </>
  )
}

export default function SystemAnnouncementsPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <AnnouncementsPage userId={userId} locale={locale} {...props} />
}
