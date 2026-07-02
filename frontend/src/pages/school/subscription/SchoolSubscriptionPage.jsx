import { useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { DateField, TextareaField } from '../../../shared/form'
import { DetailList } from '../../../shared/data-display'
import { Alert, Button, Card } from '../../../shared/ui'
import { ErrorState } from '../../../shared/feedback'
import { Head, Loading } from '../../../features/school/components'
import { useSchoolQuery } from '../../../features/school/helpers'
import { schoolApi } from '../../../features/school/schoolApi'
import { queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function SubscriptionPage({ userId, locale }) {
  const sub = useSchoolQuery(queryKeys.school.subscription(userId), (s) => schoolApi.subscription(s))
  const { t } = useTranslation()
  const qc = useQueryClient()
  const [requestedExpiresAt, setRequestedExpiresAt] = useState('')
  const [notes, setNotes] = useState('')
  const requestRenewal = useMutation({
    mutationFn: () => schoolApi.requestRenewal({ requestedExpiresAt: requestedExpiresAt || null, notes: notes || null }),
    onSuccess: () => {
      setRequestedExpiresAt('')
      setNotes('')
      qc.invalidateQueries({ queryKey: queryKeys.school.subscription(userId) })
    },
  })
  return (
    <>
      <Head view="subscription" />
      {sub.isLoading && <Loading />}
      {sub.isError && <ErrorState error={sub.error} onRetry={sub.refetch} />}
      {sub.data && <Card title={t('school.pages.subscription.title')}><DetailList item={sub.data} locale={locale} /></Card>}
      <Card title={t('school.subscription.requestRenewal')}>
        {requestRenewal.isSuccess && <Alert variant="success" title={t('school.subscription.requested')}>{t('school.subscription.requestedBody')}</Alert>}
        {requestRenewal.isError && <ErrorState error={requestRenewal.error} onRetry={() => requestRenewal.reset()} />}
        <div className="ui-formgrid ui-formgrid--2">
          <DateField label={t('school.subscription.requestedExpiresAt')} value={requestedExpiresAt} onChange={(e) => setRequestedExpiresAt(e.target.value)} />
        </div>
        <TextareaField label={t('school.subscription.notes')} value={notes} onChange={(e) => setNotes(e.target.value)} />
        <Button onClick={() => requestRenewal.mutate()} loading={requestRenewal.isPending}>{t('school.subscription.requestRenewal')}</Button>
      </Card>
    </>
  )
}

export default function SchoolSubscriptionPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <SubscriptionPage userId={userId} locale={locale} {...props} />
}
