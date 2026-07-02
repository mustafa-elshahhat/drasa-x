import { useState } from 'react'
import { useParams } from 'react-router-dom'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { DetailList } from '../../../shared/data-display'
import { CheckboxField, DateField, SelectField, TextareaField, TextField } from '../../../shared/form'
import { Alert, Button, Chip, Card } from '../../../shared/ui'
import { EmptyState, ErrorState } from '../../../shared/feedback'
import { Head, Loading } from '../../../features/system/components'
import { TENANT_STATUS, TENANT_TONE, RENEWAL_STATUS } from '../../../features/system/constants'
import { useSystemQuery } from '../../../features/system/helpers'
import { systemApi } from '../../../features/system/systemApi'
import { displayValue, itemId } from '../../../features/student/studentUtils'
import { STALE, queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

// A processed renewal must move OUT of "Requested" (index 0, the state a school's
// request starts in) into a real decision. "Applied" immediately extends the
// subscription's expiry and reactivates it (see TenantAdminService.ProcessRenewalAsync);
// "Approved"/"Rejected"/"Cancelled" record a decision without touching the subscription.
const RENEWAL_DECISIONS = [1, 2, 3, 4]

function TenantDetailsPage({ userId, locale }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const { tenantId } = useParams()
  const tenant = useSystemQuery(queryKeys.system.tenant(userId, tenantId), (s) => systemApi.tenant(tenantId, s), { enabled: Boolean(tenantId) })
  const subscription = useSystemQuery(queryKeys.system.tenantSubscription(userId, tenantId), (s) => systemApi.tenantSubscription(tenantId, s), { enabled: Boolean(tenantId) })
  const usage = useSystemQuery(queryKeys.system.tenantUsage(userId, tenantId), (s) => systemApi.tenantUsage(tenantId, s), { enabled: Boolean(tenantId) })
  const plans = useSystemQuery(queryKeys.system.plans(userId), (s) => systemApi.plans(s), { staleTime: STALE.medium })

  const [adminForm, setAdminForm] = useState({ fullName: '', loginCode: '' })
  const [credential, setCredential] = useState(null)
  const [dataResult, setDataResult] = useState(null)
  const [planForm, setPlanForm] = useState({ planDefinitionId: '', isTrial: false })
  const [renewalForm, setRenewalForm] = useState({ renewalId: '', status: '', newExpiresAt: '', notes: '' })

  const lifecycle = useMutation({
    mutationFn: (action) => systemApi.setTenantStatus(tenantId, action),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKeys.system.tenant(userId, tenantId) })
      qc.invalidateQueries({ queryKey: queryKeys.system.dashboard(userId) })
    },
  })
  const assignPlan = useMutation({
    mutationFn: () => systemApi.assignPlan({ tenantId, planDefinitionId: planForm.planDefinitionId, isTrial: planForm.isTrial }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKeys.system.tenantSubscription(userId, tenantId) })
      qc.invalidateQueries({ queryKey: queryKeys.system.tenantUsage(userId, tenantId) })
      qc.invalidateQueries({ queryKey: queryKeys.system.tenant(userId, tenantId) })
      setPlanForm({ planDefinitionId: '', isTrial: false })
    },
  })
  const processRenewal = useMutation({
    mutationFn: () => systemApi.processRenewal(renewalForm.renewalId.trim(), {
      status: Number(renewalForm.status),
      newExpiresAt: renewalForm.newExpiresAt || undefined,
      notes: renewalForm.notes.trim() || undefined,
    }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKeys.system.tenantSubscription(userId, tenantId) })
      qc.invalidateQueries({ queryKey: queryKeys.system.tenantUsage(userId, tenantId) })
      qc.invalidateQueries({ queryKey: queryKeys.system.tenant(userId, tenantId) })
      setRenewalForm({ renewalId: '', status: '', newExpiresAt: '', notes: '' })
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
            <div className="cluster mb-3">
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
              <Button variant="secondary" onClick={() => lifecycle.mutate('archive')} loading={lifecycle.isPending} disabled={status === 2}>{t('system.tenant.archive')}</Button>
            </div>
          </Card>

          <div className="ui-split ui-split--even">
            <Card title={t('system.tenant.subscription')}>
              {subscription.isLoading && <Loading />}
              {subscription.isError && <EmptyState title={t('system.empty.subscription')} />}
              {subscription.data && <DetailList item={subscription.data} locale={locale} />}

              {assignPlan.isSuccess && <Alert variant="success" title={t('system.common.saved')}>{t('system.tenant.planAssigned')}</Alert>}
              {assignPlan.isError && <ErrorState error={assignPlan.error} onRetry={() => assignPlan.reset()} />}
              <div className="ui-form-row">
                <SelectField
                  label={t('system.tenant.assignPlan')}
                  value={planForm.planDefinitionId}
                  onChange={(e) => setPlanForm((f) => ({ ...f, planDefinitionId: e.target.value }))}
                  options={[
                    { value: '', label: t('system.common.choose') },
                    ...(Array.isArray(plans.data) ? plans.data : []).map((p) => ({ value: itemId(p), label: displayValue(p, ['name', 'Name']) || itemId(p) })),
                  ]}
                />
                <CheckboxField label={t('system.common.trial')} checked={planForm.isTrial} onChange={(e) => setPlanForm((f) => ({ ...f, isTrial: e.target.checked }))} />
              </div>
              <Button onClick={() => assignPlan.mutate()} loading={assignPlan.isPending} disabled={!planForm.planDefinitionId}>
                {t('system.tenant.assignPlan')}
              </Button>
            </Card>
            <Card title={t('system.tenant.usage')}>
              {usage.isLoading && <Loading />}
              {usage.data && <DetailList item={usage.data} locale={locale} />}
            </Card>
          </div>

          <Card title={t('system.tenant.processRenewal')}>
            {processRenewal.isSuccess && <Alert variant="success" title={t('system.common.saved')}>{t('system.tenant.renewalProcessed')}</Alert>}
            {processRenewal.isError && <ErrorState error={processRenewal.error} onRetry={() => processRenewal.reset()} />}
            <p className="ui-muted">{t('system.tenant.renewalIdHint')}</p>
            <div className="ui-formgrid ui-formgrid--2">
              <TextField
                label={t('system.tenant.renewalId')}
                value={renewalForm.renewalId}
                onChange={(e) => setRenewalForm((f) => ({ ...f, renewalId: e.target.value }))}
              />
              <SelectField
                label={t('system.tenant.renewalDecision')}
                value={renewalForm.status}
                onChange={(e) => setRenewalForm((f) => ({ ...f, status: e.target.value }))}
                options={[
                  { value: '', label: t('system.common.choose') },
                  ...RENEWAL_DECISIONS.map((v) => ({ value: String(v), label: t(`system.tenant.renewalStatusValue.${RENEWAL_STATUS[v]}`) })),
                ]}
              />
              <DateField
                label={t('system.tenant.renewalNewExpiresAt')}
                value={renewalForm.newExpiresAt}
                onChange={(e) => setRenewalForm((f) => ({ ...f, newExpiresAt: e.target.value }))}
              />
            </div>
            <TextareaField
              label={t('system.tenant.renewalNotes')}
              value={renewalForm.notes}
              onChange={(e) => setRenewalForm((f) => ({ ...f, notes: e.target.value }))}
            />
            <Button
              onClick={() => processRenewal.mutate()}
              loading={processRenewal.isPending}
              disabled={!renewalForm.renewalId.trim() || renewalForm.status === ''}
            >
              {t('system.tenant.processRenewal')}
            </Button>
          </Card>

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
