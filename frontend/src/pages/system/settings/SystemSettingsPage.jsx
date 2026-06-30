import { useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { CheckboxField, SelectField, TextField } from '../../../components/form/fields'
import { Alert } from '../../../components/ui/Alert'
import { Button } from '../../../components/ui/Button'
import { Card } from '../../../components/ui/PageHeader'
import { ErrorState } from '../../../components/ui/states'
import { Head, List } from '../../../features/system/components'
import { SETTING_TYPE } from '../../../features/system/constants'
import { useSystemQuery } from '../../../features/system/helpers'
import { systemApi } from '../../../features/system/systemApi'
import { queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function SettingsPage({ userId, locale }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const query = useSystemQuery(queryKeys.system.settings(userId), (s) => systemApi.settings(s))
  const [form, setForm] = useState({ key: '', value: '', valueType: SETTING_TYPE.String, isSecret: false })
  const upsert = useMutation({
    mutationFn: () => systemApi.upsertSetting({ key: form.key.trim(), value: form.value, valueType: Number(form.valueType), isSecret: form.isSecret }),
    onSuccess: () => { setForm({ key: '', value: '', valueType: SETTING_TYPE.String, isSecret: false }); qc.invalidateQueries({ queryKey: queryKeys.system.settings(userId) }) },
  })
  return (
    <>
      <Head view="settings" />
      <Card title={t('system.settings.upsert')}>
        {upsert.isSuccess && <Alert variant="success" title={t('system.common.saved')}>{t('system.settings.saved')}</Alert>}
        {upsert.isError && <ErrorState error={upsert.error} onRetry={() => upsert.reset()} />}
        <div className="ui-formgrid ui-formgrid--2">
          <TextField label={t('system.common.key')} value={form.key} onChange={(e) => setForm((f) => ({ ...f, key: e.target.value }))} />
          <TextField label={t('system.common.value')} value={form.value} onChange={(e) => setForm((f) => ({ ...f, value: e.target.value }))} />
          <SelectField label={t('system.common.valueType')} value={form.valueType} onChange={(e) => setForm((f) => ({ ...f, valueType: e.target.value }))}
            options={Object.entries(SETTING_TYPE).map(([k, v]) => ({ value: v, label: k }))} />
        </div>
        <CheckboxField label={t('system.common.secret')} checked={form.isSecret} onChange={(e) => setForm((f) => ({ ...f, isSecret: e.target.checked }))} />
        <Button onClick={() => upsert.mutate()} loading={upsert.isPending} disabled={!form.key.trim()}>{t('system.common.save')}</Button>
      </Card>
      <List query={query} empty={t('system.empty.settings')} locale={locale} />
    </>
  )
}

// ---------------------------------------------------------------------------
// Audit / Security
// ---------------------------------------------------------------------------

export default function SystemSettingsPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <SettingsPage userId={userId} locale={locale} {...props} />
}
