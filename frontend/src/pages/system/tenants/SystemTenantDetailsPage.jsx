import { useState } from 'react'
import { useParams } from 'react-router-dom'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { DetailList } from '../../../components/data/DetailList'
import { TextField } from '../../../components/form/fields'
import { Alert } from '../../../components/ui/Alert'
import { Button } from '../../../components/ui/Button'
import { Chip } from '../../../components/ui/Chip'
import { Card } from '../../../components/ui/PageHeader'
import { EmptyState, ErrorState } from '../../../components/ui/states'
import { Head, Loading } from '../../../features/system/components'
import { TENANT_STATUS, TENANT_TONE } from '../../../features/system/constants'
import { useSystemQuery } from '../../../features/system/helpers'
import { systemApi } from '../../../features/system/systemApi'
import { queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function TenantDetailsPage({ userId, locale }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const { tenantId } = useParams()
  const tenant = useSystemQuery(queryKeys.system.tenant(userId, tenantId), (s) => systemApi.tenant(tenantId, s), { enabled: Boolean(tenantId) })
  const subscription = useSystemQuery(queryKeys.system.tenantSubscription(userId, tenantId), (s) => systemApi.tenantSubscription(tenantId, s), { enabled: Boolean(tenantId) })
  const usage = useSystemQuery(queryKeys.system.tenantUsage(userId, tenantId), (s) => systemApi.tenantUsage(tenantId, s), { enabled: Boolean(tenantId) })

  const [adminForm, setAdminForm] = useState({ fullName: '', loginCode: '' })
  const [credential, setCredential] = useState(null)
  const [dataResult, setDataResult] = useState(null)

  const lifecycle = useMutation({
    mutationFn: (action) => systemApi.setTenantStatus(tenantId, action),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKeys.system.tenant(userId, tenantId) })
      qc.invalidateQueries({ queryKey: queryKeys.system.dashboard(userId) })
    },
  })
  const createAdmin = useMutation({
    mutationFn: () => systemApi.createSchoolAdmin(tenantId, adminForm),
    onSuccess: (data) => { setCredential(data); setAdminForm({ fullName: '', loginCode: '' }) },
  })
  const exportData = useMutation({ mutationFn: () => systemApi.exportTenantData(tenantId), onSuccess: (data) => setDataResult(data) })
  const deletionRequest = useMutation({ mutationFn: () => systemApi.requestTenantDeletion(tenantId), onSuccess: (data) => setDataResult(data) })

  const status = tenant.data?.status
  return (
    <>
      <Head view="tenantDetails" />
      {tenant.isLoading && <Loading />}
      {tenant.isError && <ErrorState error={tenant.error} onRetry={tenant.refetch} />}
      {tenant.data && (
        <>
          <Card title={tenant.data.name || tenantId}>
            <div className="cluster" style={{ marginBottom: 12 }}>
              {status != null && <Chip tone={TENANT_TONE[status] || 'muted'}>{t(`system.status.${TENANT_STATUS[status] || 'Active'}`)}</Chip>}
            </div>
            <DetailList item={tenant.data} locale={locale} />
          </Card>

          <Card title={t('system.tenant.lifecycle')}>
            {lifecycle.isError && <ErrorState error={lifecycle.error} onRetry={() => lifecycle.reset()} />}
            {lifecycle.isSuccess && <Alert variant="success" title={t('system.common.saved')}>{t('system.tenant.lifecycleSaved')}</Alert>}
            <div className="cluster">
              <Button onClick={() => lifecycle.mutate('activate')} loading={lifecycle.isPending} disabled={status === 0}>{t('system.tenant.activate')}</Button>
              <Button variant="secondary" onClick={() => lifecycle.mutate('suspend')} loading={lifecycle.isPending} disabled={status === 1}>{t('system.tenant.suspend')}</Button>
              <Button variant="secondary" onClick={() => lifecycle.mutate('reactivate')} loading={lifecycle.isPending} disabled={status === 0}>{t('system.tenant.reactivate')}</Button>
            </div>
          </Card>

          <div className="ui-split ui-split--even">
            <Card title={t('system.tenant.subscription')}>
              {subscription.isLoading && <Loading />}
              {subscription.isError && <EmptyState title={t('system.empty.subscription')} />}
              {subscription.data && <DetailList item={subscription.data} locale={locale} />}
            </Card>
            <Card title={t('system.tenant.usage')}>
              {usage.isLoading && <Loading />}
              {usage.data && <DetailList item={usage.data} locale={locale} />}
            </Card>
          </div>

          <Card title={t('system.tenant.createAdmin')}>
            {credential && (
              <Alert variant="success" title={t('system.credential.title')}>
                {t('system.credential.body')} — {t('system.common.loginCode')}: <code>{credential.loginCode}</code> · {t('system.credential.password')}: <code>{credential.temporaryPassword}</code>
              </Alert>
            )}
            {createAdmin.isError && <ErrorState error={createAdmin.error} onRetry={() => createAdmin.reset()} />}
            <div className="ui-formgrid ui-formgrid--2">
              <TextField label={t('system.common.name')} value={adminForm.fullName} onChange={(e) => setAdminForm((f) => ({ ...f, fullName: e.target.value }))} />
              <TextField label={t('system.common.loginCode')} value={adminForm.loginCode} onChange={(e) => setAdminForm((f) => ({ ...f, loginCode: e.target.value }))} />
            </div>
            <Button onClick={() => createAdmin.mutate()} loading={createAdmin.isPending} disabled={!adminForm.fullName.trim() || !adminForm.loginCode.trim()}>{t('system.tenant.createAdmin')}</Button>
          </Card>

          <Card title={t('system.tenant.dataWorkflow')}>
            <Alert title={t('system.notes.dataTitle')}>{t('system.notes.data')}</Alert>
            {(exportData.isError || deletionRequest.isError) && <ErrorState error={exportData.error || deletionRequest.error} />}
            {dataResult && (
              <Alert variant="success" title={dataResult.requestType === 'deletion-request' ? t('system.tenant.deletionRecorded') : t('system.tenant.exportPreview')}>
                {dataResult.note}
                <DetailList item={dataResult.preview} locale={locale} />
              </Alert>
            )}
            <div className="cluster">
              <Button onClick={() => exportData.mutate()} loading={exportData.isPending}>{t('system.tenant.exportData')}</Button>
              <Button variant="secondary" onClick={() => deletionRequest.mutate()} loading={deletionRequest.isPending}>{t('system.tenant.requestDeletion')}</Button>
            </div>
          </Card>
        </>
      )}
    </>
  )
}

// ---------------------------------------------------------------------------
// Onboarding (create tenant → assign plan → create admin → activate)
// ---------------------------------------------------------------------------

export default function SystemTenantDetailsPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <TenantDetailsPage userId={userId} locale={locale} {...props} />
}
