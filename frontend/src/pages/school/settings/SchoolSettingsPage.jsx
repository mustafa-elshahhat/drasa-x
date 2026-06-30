import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { ShieldCheck } from 'lucide-react'
import { CheckboxField, SelectField, TextField } from '../../../components/form/fields'
import { Alert } from '../../../components/ui/Alert'
import { Button } from '../../../components/ui/Button'
import { Card } from '../../../components/ui/PageHeader'
import { ErrorState } from '../../../components/ui/states'
import { Head, List } from '../../../features/school/components'
import { SETTING_TYPE } from '../../../features/school/constants'
import { useSchoolQuery } from '../../../features/school/helpers'
import { schoolApi } from '../../../features/school/schoolApi'
import { queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function SettingsPage({ userId, locale }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const query = useSchoolQuery(queryKeys.school.settings(userId), (s) => schoolApi.settings(s))
  const [form, setForm] = useState({ key: '', value: '', valueType: SETTING_TYPE.String, isSecret: false })
  const upsert = useMutation({
    mutationFn: () => schoolApi.upsertSetting({ key: form.key, value: form.value, valueType: Number(form.valueType), isSecret: form.isSecret }),
    onSuccess: () => { setForm({ key: '', value: '', valueType: SETTING_TYPE.String, isSecret: false }); qc.invalidateQueries({ queryKey: queryKeys.school.settings(userId) }) },
  })
  return (
    <>
      <Head view="settings" />
      <Card title={t('school.settings.upsert')}>
        {upsert.isSuccess && <Alert variant="success" title={t('school.common.saved')}>{t('school.settings.saved')}</Alert>}
        {upsert.isError && <ErrorState error={upsert.error} onRetry={() => upsert.reset()} />}
        <div className="ui-formgrid ui-formgrid--2">
          <TextField label={t('school.common.key')} value={form.key} onChange={(e) => setForm((f) => ({ ...f, key: e.target.value }))} />
          <TextField label={t('school.common.value')} value={form.value} onChange={(e) => setForm((f) => ({ ...f, value: e.target.value }))} />
          <SelectField label={t('school.common.valueType')} value={form.valueType} onChange={(e) => setForm((f) => ({ ...f, valueType: e.target.value }))}
            options={Object.entries(SETTING_TYPE).map(([k, v]) => ({ value: v, label: k }))} />
        </div>
        <CheckboxField label={t('school.common.secret')} checked={form.isSecret} onChange={(e) => setForm((f) => ({ ...f, isSecret: e.target.checked }))} />
        <Button onClick={() => upsert.mutate()} loading={upsert.isPending} disabled={!form.key.trim()}>{t('school.common.save')}</Button>
      </Card>
      <List query={query} empty={t('school.empty.settings')} locale={locale} />
      <Card title={t('nav.security')}><Link className="ui-btn ui-btn--primary" to="/app/security"><ShieldCheck size={16} aria-hidden="true" /> {t('security.changePassword')}</Link></Card>
    </>
  )
}

// ---------------------------------------------------------------------------

export default function SchoolSettingsPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <SettingsPage userId={userId} locale={locale} {...props} />
}
