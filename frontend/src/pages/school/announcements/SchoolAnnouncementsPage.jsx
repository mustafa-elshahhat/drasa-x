import { useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { SelectField, TextField, TextareaField } from '../../../shared/form'
import { Alert, Button, Card, Chip } from '../../../shared/ui'
import { ErrorState } from '../../../shared/feedback'
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
      <Card title={t('school.announcements.create', 'New announcement')}>
        {create.isSuccess && <Alert variant="success" title={t('school.announcements.draftSaved', 'Draft saved')}>{t('school.announcements.draftSavedBody', 'The announcement was saved as a draft. Publish it to notify the audience.')}</Alert>}
        {create.isError && <ErrorState error={create.error} onRetry={() => create.reset()} />}
        <TextField label={t('school.common.title')} value={form.title} onChange={(e) => setForm((f) => ({ ...f, title: e.target.value }))} />
        <TextareaField label={t('school.common.message')} value={form.body} onChange={(e) => setForm((f) => ({ ...f, body: e.target.value }))} />
        <SelectField label={t('school.announcements.audience')} value={form.targetAudience} onChange={(e) => setForm((f) => ({ ...f, targetAudience: e.target.value }))}
          options={Object.entries(AUDIENCE).map(([k, v]) => ({ value: v, label: t(`school.audiences.${k}`) }))} />
        {/* Create stores an inactive DRAFT (no notifications). Publishing it from the list below is
            what makes it visible and fans out notifications. */}
        <Button onClick={() => create.mutate()} loading={create.isPending} disabled={!form.title.trim() || !form.body.trim()}>{t('school.announcements.saveDraft', 'Save draft')}</Button>
      </Card>
      <List
        query={query}
        empty={t('school.empty.announcements')}
        locale={locale}
        rowActions={(item) => {
          const active = item.isActive ?? item.IsActive
          return (
            <span className="cluster">
              <Chip tone={active ? 'success' : 'muted'}>{active ? t('school.announcements.statusPublished', 'Published') : t('school.announcements.statusDraft', 'Draft')}</Chip>
              <Button variant="secondary" onClick={() => publish.mutate({ id: itemId(item), on: !active })} loading={publish.isPending}>{active ? t('school.common.unpublish') : t('school.common.publish')}</Button>
            </span>
          )
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
