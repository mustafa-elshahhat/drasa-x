import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { CheckboxField, SelectField, TextField } from '../../../components/form/fields'
import { Alert } from '../../../components/ui/Alert'
import { Button } from '../../../components/ui/Button'
import { Card } from '../../../components/ui/PageHeader'
import { ErrorState } from '../../../components/ui/states'
import { Stepper } from '../../../components/ui/Stepper'
import { displayValue, itemId } from '../../../features/student/studentUtils'
import { Head } from '../../../features/system/components'
import { CURRICULUM } from '../../../features/system/constants'
import { useSystemQuery } from '../../../features/system/helpers'
import { systemApi } from '../../../features/system/systemApi'
import { STALE, queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function OnboardingPage({ userId }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const plans = useSystemQuery(queryKeys.system.plans(userId), (s) => systemApi.plans(s), { staleTime: STALE.medium })
  const [tenantForm, setTenantForm] = useState({ id: '', name: '', type: 0 })
  const [createdTenantId, setCreatedTenantId] = useState(null)
  const [planForm, setPlanForm] = useState({ planDefinitionId: '', isTrial: false })
  const [adminForm, setAdminForm] = useState({ fullName: '', loginCode: '' })
  const [credential, setCredential] = useState(null)

  const createTenant = useMutation({
    mutationFn: () => systemApi.createTenant({ id: tenantForm.id.trim(), name: tenantForm.name.trim(), type: Number(tenantForm.type) }),
    onSuccess: (data) => { setCreatedTenantId(itemId(data) || tenantForm.id.trim()); qc.invalidateQueries({ queryKey: queryKeys.system.tenants(userId, 'all') }) },
  })
  const assignPlan = useMutation({
    mutationFn: () => systemApi.assignPlan({ tenantId: createdTenantId, planDefinitionId: planForm.planDefinitionId, isTrial: planForm.isTrial }),
  })
  const createAdmin = useMutation({
    mutationFn: () => systemApi.createSchoolAdmin(createdTenantId, adminForm),
    onSuccess: (data) => setCredential(data),
  })
  const activate = useMutation({
    mutationFn: () => systemApi.setTenantStatus(createdTenantId, 'activate'),
    onSuccess: () => { qc.invalidateQueries({ queryKey: queryKeys.system.tenants(userId, 'all') }); qc.invalidateQueries({ queryKey: queryKeys.system.dashboard(userId) }) },
  })
  const planItems = Array.isArray(plans.data) ? plans.data : []
  const current = activate.isSuccess ? 4 : credential ? 3 : assignPlan.isSuccess ? 2 : createdTenantId ? 1 : 0

  return (
    <>
      <Head view="onboarding" />
      <Stepper
        current={current}
        steps={[
          { title: t('system.onboarding.step1') },
          { title: t('system.onboarding.step2') },
          { title: t('system.onboarding.step3') },
          { title: t('system.onboarding.step4') },
        ]}
      />

      <Card title={t('system.onboarding.step1')}>
        {createTenant.isSuccess && <Alert variant="success" title={t('system.common.created')}>{t('system.onboarding.tenantCreated', { id: createdTenantId })}</Alert>}
        {createTenant.isError && <ErrorState error={createTenant.error} onRetry={() => createTenant.reset()} />}
        <div className="ui-formgrid ui-formgrid--2">
          <TextField label={t('system.common.tenantId')} value={tenantForm.id} onChange={(e) => setTenantForm((f) => ({ ...f, id: e.target.value }))} />
          <TextField label={t('system.common.tenantName')} value={tenantForm.name} onChange={(e) => setTenantForm((f) => ({ ...f, name: e.target.value }))} />
          <SelectField label={t('system.common.curriculum')} value={tenantForm.type} onChange={(e) => setTenantForm((f) => ({ ...f, type: e.target.value }))} options={CURRICULUM} />
        </div>
        <Button onClick={() => createTenant.mutate()} loading={createTenant.isPending} disabled={!tenantForm.id.trim() || !tenantForm.name.trim() || Boolean(createdTenantId)}>{t('system.onboarding.createTenant')}</Button>
      </Card>

      <Card title={t('system.onboarding.step2')}>
        {assignPlan.isSuccess && <Alert variant="success" title={t('system.common.saved')}>{t('system.onboarding.planAssigned')}</Alert>}
        {assignPlan.isError && <ErrorState error={assignPlan.error} onRetry={() => assignPlan.reset()} />}
        <SelectField label={t('system.common.plan')} value={planForm.planDefinitionId} onChange={(e) => setPlanForm((f) => ({ ...f, planDefinitionId: e.target.value }))}
          options={[{ value: '', label: t('system.common.choose') }, ...planItems.map((p) => ({ value: itemId(p), label: displayValue(p, ['name', 'Name']) || itemId(p) }))]} />
        <CheckboxField label={t('system.common.trial')} checked={planForm.isTrial} onChange={(e) => setPlanForm((f) => ({ ...f, isTrial: e.target.checked }))} />
        <Button onClick={() => assignPlan.mutate()} loading={assignPlan.isPending} disabled={!createdTenantId || !planForm.planDefinitionId}>{t('system.onboarding.assignPlan')}</Button>
      </Card>

      <Card title={t('system.onboarding.step3')}>
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
        <Button onClick={() => createAdmin.mutate()} loading={createAdmin.isPending} disabled={!createdTenantId || !adminForm.fullName.trim() || !adminForm.loginCode.trim()}>{t('system.onboarding.createAdmin')}</Button>
      </Card>

      <Card title={t('system.onboarding.step4')}>
        {activate.isSuccess && <Alert variant="success" title={t('system.common.saved')}>{t('system.onboarding.activated')}</Alert>}
        {activate.isError && <ErrorState error={activate.error} onRetry={() => activate.reset()} />}
        <div className="cluster">
          <Button onClick={() => activate.mutate()} loading={activate.isPending} disabled={!createdTenantId}>{t('system.onboarding.activate')}</Button>
          {createdTenantId && <Link className="ui-btn ui-btn--secondary" to={`/app/system/tenants/${encodeURIComponent(createdTenantId)}`}>{t('system.common.open')}</Link>}
        </div>
      </Card>
    </>
  )
}

// ---------------------------------------------------------------------------
// Plans / Subscriptions / Usage / AI usage / Storage
// ---------------------------------------------------------------------------

export default function SystemOnboardingPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <OnboardingPage userId={userId} locale={locale} {...props} />
}
