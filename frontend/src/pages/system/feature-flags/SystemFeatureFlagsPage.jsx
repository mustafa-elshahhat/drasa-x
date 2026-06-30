import { useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { CheckboxField, TextField } from '../../../shared/form'
import { Alert, Button, Card, Toggle } from '../../../shared/ui'
import { ErrorState } from '../../../shared/feedback'
import { getField } from '../../../features/student/studentUtils'
import { Head, List } from '../../../features/system/components'
import { useSystemQuery } from '../../../features/system/helpers'
import { systemApi } from '../../../features/system/systemApi'
import { queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function FeatureFlagsPage({ userId, locale }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const query = useSystemQuery(queryKeys.system.featureFlags(userId), (s) => systemApi.featureFlags(s))
  const [form, setForm] = useState({ key: '', isEnabled: true, targetTenantId: '' })
  const upsert = useMutation({
    mutationFn: (payload) => systemApi.upsertFeatureFlag(payload),
    onSuccess: () => { qc.invalidateQueries({ queryKey: queryKeys.system.featureFlags(userId) }) },
  })
  const createFlag = () => upsert.mutate({ key: form.key.trim(), isEnabled: form.isEnabled, targetTenantId: form.targetTenantId.trim() || null }, { onSuccess: () => { setForm({ key: '', isEnabled: true, targetTenantId: '' }); qc.invalidateQueries({ queryKey: queryKeys.system.featureFlags(userId) }) } })
  const columns = [
    { key: 'key', header: t('system.common.key') },
    { key: 'targetTenantId', header: t('system.featureFlags.targetTenant') },
    { key: 'isEnabled', header: t('system.featureFlags.enabled'), kind: 'bool' },
  ]
  return (
    <>
      <Head view="featureFlags" />
      <Card title={t('system.featureFlags.upsert')}>
        {upsert.isSuccess && <Alert variant="success" title={t('system.common.saved')}>{t('system.featureFlags.saved')}</Alert>}
        {upsert.isError && <ErrorState error={upsert.error} onRetry={() => upsert.reset()} />}
        <div className="ui-formgrid ui-formgrid--2">
          <TextField label={t('system.common.key')} value={form.key} onChange={(e) => setForm((f) => ({ ...f, key: e.target.value }))} />
          <TextField label={t('system.featureFlags.targetTenant')} value={form.targetTenantId} onChange={(e) => setForm((f) => ({ ...f, targetTenantId: e.target.value }))} />
        </div>
        <CheckboxField label={t('system.featureFlags.enabled')} checked={form.isEnabled} onChange={(e) => setForm((f) => ({ ...f, isEnabled: e.target.checked }))} />
        <Button onClick={createFlag} loading={upsert.isPending} disabled={!form.key.trim()}>{t('system.common.save')}</Button>
      </Card>
      <List
        query={query}
        columns={columns}
        empty={t('system.empty.featureFlags')}
        locale={locale}
        rowActions={(item) => {
          const enabled = Boolean(getField(item, 'isEnabled'))
          const key = getField(item, 'key')
          return (
            <Toggle
              checked={enabled}
              label={key}
              disabled={upsert.isPending}
              onChange={(next) => upsert.mutate({ key, isEnabled: next, targetTenantId: getField(item, 'targetTenantId') || null })}
            />
          )
        }}
      />
    </>
  )
}

export default function SystemFeatureFlagsPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <FeatureFlagsPage userId={userId} locale={locale} {...props} />
}
