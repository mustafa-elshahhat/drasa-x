import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { SelectField } from '../../../components/form/fields'
import { Card } from '../../../components/ui/PageHeader'
import { ErrorState } from '../../../components/ui/states'
import { itemId } from '../../../features/student/studentUtils'
import { Head, List } from '../../../features/system/components'
import { TENANT_STATUS, TENANT_TONE } from '../../../features/system/constants'
import { useSystemQuery } from '../../../features/system/helpers'
import { systemApi } from '../../../features/system/systemApi'
import { queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function TenantsPage({ userId, locale }) {
  const { t } = useTranslation()
  const [status, setStatus] = useState('')
  const query = useSystemQuery(queryKeys.system.tenants(userId, status || 'all'), (s) => systemApi.tenants(status === '' ? undefined : Number(status), s))
  const columns = [
    { key: 'name', header: t('system.common.tenantName') },
    {
      key: 'status',
      header: t('system.common.status'),
      chip: (v) => ({ tone: TENANT_TONE[v] || 'muted', label: t(`system.status.${TENANT_STATUS[v] || 'Active'}`) }),
    },
  ]
  return (
    <>
      <Head view="tenants" />
      <Card title={t('system.tenants.filter')}>
        <div className="ui-form-row">
          <SelectField label={t('system.common.status')} value={status} onChange={(e) => setStatus(e.target.value)}
            options={[{ value: '', label: t('system.tenants.all') }, ...TENANT_STATUS.map((s, i) => ({ value: i, label: t(`system.status.${s}`) }))]} />
          <Link className="ui-btn ui-btn--primary" to="/app/system/onboarding">{t('system.tenants.onboard')}</Link>
        </div>
      </Card>
      <List
        query={query}
        columns={columns}
        empty={t('system.empty.tenants')}
        locale={locale}
        rowActions={(item) => <Link className="ui-btn ui-btn--secondary" to={`/app/system/tenants/${encodeURIComponent(itemId(item))}`}>{t('system.common.open')}</Link>}
      />
    </>
  )
}

// ---------------------------------------------------------------------------
// Tenant details (+ lifecycle actions + initial admin + data workflow)
// ---------------------------------------------------------------------------

export default function SystemTenantsPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <TenantsPage userId={userId} locale={locale} {...props} />
}
