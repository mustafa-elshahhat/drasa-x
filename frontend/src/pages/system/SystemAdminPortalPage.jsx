import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Building2, GraduationCap, Users, UserCog, Grid3x3, CreditCard, Bot, MessageSquare, ScrollText } from 'lucide-react'
import { PageHeader, Card } from '../../components/ui/PageHeader'
import { Metric } from '../../components/ui/Metric'
import { EmptyState, ErrorState } from '../../components/ui/states'
import { Button } from '../../components/ui/Button'
import { Alert } from '../../components/ui/Alert'
import { Chip } from '../../components/ui/Chip'
import { Spinner } from '../../components/ui/Spinner'
import { Stepper } from '../../components/ui/Stepper'
import { Toggle } from '../../components/ui/Toggle'
import { DetailList } from '../../components/data/DetailList'
import { ResourceTable } from '../../components/data/ResourceTable'
import { TextField, TextareaField, SelectField, CheckboxField } from '../../components/form/fields'
import { useAuth } from '../../features/auth/AuthContext'
import { systemApi } from '../../features/system/systemApi'
import { displayValue, itemId, getField } from '../../features/student/studentUtils'
import { queryKeys, STALE } from '../../lib/query/keys'

// Numeric enum maps (the backend binds enums numerically — no JsonStringEnumConverter).
const TENANT_STATUS = ['Active', 'Suspended', 'Archived'] // value == index
const TENANT_TONE = ['success', 'danger', 'muted']
const CURRICULUM = [{ value: 0, label: 'National' }]
const SETTING_TYPE = { String: 0, Number: 1, Boolean: 2, Json: 3 }
const SUPPORT_STATUS = { Completed: 4 }

// All system-admin reads are platform-scope + SystemAdminOnly server-side; the UI only
// renders what the backend returns and never fabricates statistics or ids.
function useSystemQuery(key, fn, options = {}) {
  return useQuery({ queryKey: key, queryFn: ({ signal }) => fn(signal), staleTime: options.staleTime ?? STALE.short, enabled: options.enabled ?? true })
}

function Loading() {
  const { t } = useTranslation()
  return <Spinner label={t('states.loading')} />
}

function Head({ view }) {
  const { t } = useTranslation()
  return <PageHeader title={t(`system.pages.${view}.title`)} description={t(`system.pages.${view}.description`)} />
}

function List({ query, columns, empty, rowActions, locale }) {
  return (
    <ResourceTable
      rows={Array.isArray(query.data) ? query.data : []}
      columns={columns}
      rowActions={rowActions}
      loading={query.isLoading}
      error={query.error}
      onRetry={() => query.refetch()}
      emptyTitle={empty}
      locale={locale}
    />
  )
}

// ---------------------------------------------------------------------------
// Dashboard
// ---------------------------------------------------------------------------
function DashboardPage({ userId, locale }) {
  const { t } = useTranslation()
  const dashboard = useSystemQuery(queryKeys.system.dashboard(userId), (s) => systemApi.dashboard(s))
  const d = dashboard.data || {}
  const recent = Array.isArray(d.recentActivity) ? d.recentActivity : []
  return (
    <>
      <Head view="dashboard" />
      {dashboard.isLoading && <Loading />}
      {dashboard.isError && <ErrorState error={dashboard.error} onRetry={dashboard.refetch} />}
      {dashboard.data && (
        <>
          <Alert variant="info" title={t('system.dashboard.tenants')}>
            {t('system.dashboard.active')}: <strong>{d.tenantsActive ?? 0}</strong> ·{' '}
            {t('system.dashboard.suspended')}: <strong>{d.tenantsSuspended ?? 0}</strong> ·{' '}
            {t('system.dashboard.archived')}: <strong>{d.tenantsArchived ?? 0}</strong>
          </Alert>
          <div className="student-dashboard">
            <Metric to="/app/system/tenants" icon={Building2} accent="var(--success)" value={d.tenantsTotal ?? 0} label={t('system.pages.tenants.title')} />
            <Metric to="/app/system/usage" icon={GraduationCap} accent="var(--brand)" value={d.students ?? 0} label={t('roles.Student')} />
            <Metric to="/app/system/usage" icon={Users} accent="var(--purple)" value={d.teachers ?? 0} label={t('roles.Teacher')} />
            <Metric to="/app/system/usage" icon={UserCog} accent="var(--info)" value={d.schoolAdmins ?? 0} label={t('roles.SchoolAdmin')} />
            <Metric to="/app/system/plans" icon={Grid3x3} accent="var(--orange)" value={d.plansTotal ?? 0} label={t('system.pages.plans.title')} />
            <Metric to="/app/system/subscriptions" icon={CreditCard} accent="var(--success)" value={d.subscriptionsTotal ?? 0} label={t('system.pages.subscriptions.title')} />
            <Metric to="/app/system/ai-usage" icon={Bot} accent="var(--purple)" value={d.aiUsageRecords ?? 0} label={t('system.dashboard.aiRecords')} sub={t('system.dashboard.aiTokens', { tokens: d.aiTotalTokens ?? 0 })} />
            <Metric to="/app/system/support" icon={MessageSquare} accent="var(--warning)" value={d.supportOpen ?? 0} label={t('system.pages.support.title')} />
            <Metric to="/app/system/audit" icon={ScrollText} accent="var(--info)" value={d.recentAuditEvents ?? 0} label={t('system.pages.audit.title')} />
          </div>
          <section className="ui-section">
            <div className="ui-section__head"><h2 className="ui-section__title">{t('system.dashboard.recentActivity')}</h2></div>
            <ResourceTable rows={recent} emptyTitle={t('system.empty.audit')} locale={locale} />
          </section>
        </>
      )}
    </>
  )
}

// ---------------------------------------------------------------------------
// Tenants list (+ status filter)
// ---------------------------------------------------------------------------
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
function PlansPage({ userId, locale }) {
  const { t } = useTranslation()
  const query = useSystemQuery(queryKeys.system.plans(userId), (s) => systemApi.plans(s), { staleTime: STALE.medium })
  return (<><Head view="plans" /><List query={query} empty={t('system.empty.plans')} locale={locale} /></>)
}

function SubscriptionsPage({ userId, locale }) {
  const { t } = useTranslation()
  const query = useSystemQuery(queryKeys.system.subscriptions(userId), (s) => systemApi.subscriptions(s))
  return (<><Head view="subscriptions" /><List query={query} empty={t('system.empty.subscriptions')} locale={locale} /></>)
}

function UsagePage({ userId, locale }) {
  const { t } = useTranslation()
  const query = useSystemQuery(queryKeys.system.usage(userId), (s) => systemApi.usage(s))
  const rows = Array.isArray(query.data?.tenants) ? query.data.tenants : []
  return (
    <>
      <Head view="usage" />
      {query.isLoading && <Loading />}
      {query.isError && <ErrorState error={query.error} onRetry={query.refetch} />}
      {query.data && (
        <>
          <div className="student-dashboard">
            <Metric icon={GraduationCap} accent="var(--brand)" value={query.data.totalStudents ?? 0} label={t('roles.Student')} />
            <Metric icon={Users} accent="var(--purple)" value={query.data.totalTeachers ?? 0} label={t('roles.Teacher')} />
            <Metric icon={Bot} accent="var(--purple)" value={query.data.totalAiGenerations ?? 0} label={t('system.dashboard.aiRecords')} />
          </div>
          <section className="ui-section">
            <div className="ui-section__head"><h2 className="ui-section__title">{t('system.pages.usage.title')}</h2></div>
            <ResourceTable rows={rows} emptyTitle={t('system.empty.usage')} locale={locale} />
          </section>
        </>
      )}
    </>
  )
}

function AiUsagePage({ userId, locale }) {
  const { t } = useTranslation()
  const query = useSystemQuery(queryKeys.system.aiUsage(userId), (s) => systemApi.aiUsage(s))
  const rows = Array.isArray(query.data?.tenants) ? query.data.tenants : []
  return (
    <>
      <Head view="aiUsage" />
      {query.isLoading && <Loading />}
      {query.isError && <ErrorState error={query.error} onRetry={query.refetch} />}
      {query.data && (
        <>
          <div className="student-dashboard">
            <Metric icon={Bot} accent="var(--purple)" value={query.data.records ?? 0} label={t('system.aiUsage.records')} />
            <Metric icon={Bot} accent="var(--brand)" value={query.data.totalTokens ?? 0} label={t('system.aiUsage.tokens')} />
          </div>
          <section className="ui-section">
            <div className="ui-section__head"><h2 className="ui-section__title">{t('system.aiUsage.byTenant')}</h2></div>
            <ResourceTable rows={rows} emptyTitle={t('system.empty.generic')} locale={locale} />
          </section>
        </>
      )}
    </>
  )
}

function StoragePage({ userId, locale }) {
  const { t } = useTranslation()
  const query = useSystemQuery(queryKeys.system.storage(userId), (s) => systemApi.storage(s))
  const rows = Array.isArray(query.data?.tenants) ? query.data.tenants : []
  return (
    <>
      <Head view="storage" />
      <Alert title={t('system.notes.storageTitle')}>{query.data?.note || t('system.notes.storage')}</Alert>
      {query.isLoading && <Loading />}
      {query.isError && <ErrorState error={query.error} onRetry={query.refetch} />}
      {query.data && <ResourceTable rows={rows} emptyTitle={t('system.empty.generic')} locale={locale} />}
    </>
  )
}

// ---------------------------------------------------------------------------
// Support inbox (cross-tenant)
// ---------------------------------------------------------------------------
function SupportPage({ userId, locale }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const query = useSystemQuery(queryKeys.system.support(userId), (s) => systemApi.supportTickets({}, s))
  const [replyById, setReplyById] = useState({})
  const respond = useMutation({
    mutationFn: ({ id, responseMessage }) => systemApi.respondSupportTicket(id, { responseMessage, status: SUPPORT_STATUS.Completed }),
    onSuccess: () => qc.invalidateQueries({ queryKey: queryKeys.system.support(userId) }),
  })
  const items = Array.isArray(query.data) ? query.data : []
  return (
    <>
      <Head view="support" />
      {query.isLoading && <Loading />}
      {query.isError && <ErrorState error={query.error} onRetry={query.refetch} />}
      {query.data && (items.length === 0 ? <EmptyState icon={MessageSquare} title={t('system.empty.support')} /> : (
        <div className="student-list">
          {items.map((item) => {
            const id = itemId(item)
            return (
              <Card key={id}>
                <DetailList item={item} locale={locale} />
                <TextareaField label={t('system.support.reply')} value={replyById[id] || ''} onChange={(e) => setReplyById((m) => ({ ...m, [id]: e.target.value }))} />
                <Button onClick={() => respond.mutate({ id, responseMessage: replyById[id] || '' })} loading={respond.isPending} disabled={!(replyById[id] || '').trim()}>{t('system.support.respond')}</Button>
              </Card>
            )
          })}
        </div>
      ))}
    </>
  )
}

// ---------------------------------------------------------------------------
// Feature flags / Announcements / Settings
// ---------------------------------------------------------------------------
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
function AuditPage({ userId, locale }) {
  const { t } = useTranslation()
  const query = useSystemQuery(queryKeys.system.audit(userId), (s) => systemApi.audit(s))
  return (<><Head view="audit" /><List query={query} empty={t('system.empty.audit')} locale={locale} /></>)
}

function SecurityPage({ userId, locale }) {
  const { t } = useTranslation()
  const query = useSystemQuery(queryKeys.system.security(userId), (s) => systemApi.securityEvents(s))
  return (
    <>
      <Head view="security" />
      <Alert title={t('system.notes.securityTitle')}>{t('system.notes.security')}</Alert>
      <List query={query} empty={t('system.empty.security')} locale={locale} />
    </>
  )
}

// ---------------------------------------------------------------------------
// Operational posture: Health / Error monitoring / Backups
// ---------------------------------------------------------------------------
function PostureCard({ posture, locale }) {
  const { t } = useTranslation()
  if (!posture) return null
  return (
    <Card title={posture.configured ? t('system.posture.configured') : t('system.posture.notConfigured')}>
      <DetailList item={posture} locale={locale} />
    </Card>
  )
}

function HealthPage({ userId, locale }) {
  const { t } = useTranslation()
  const query = useSystemQuery(queryKeys.system.operational(userId), (s) => systemApi.operationalStatus(s))
  const h = query.data?.health
  return (
    <>
      <Head view="health" />
      {query.isLoading && <Loading />}
      {query.isError && <ErrorState error={query.error} onRetry={query.refetch} />}
      {query.data && (
        <>
          <Alert variant={h?.databaseReachable ? 'success' : 'error'} title={t('system.health.title')}>
            {t('system.health.api')}: <strong>{h?.api}</strong> · {t('system.health.database')}: <strong>{h?.databaseReachable ? t('system.health.reachable') : t('system.health.unreachable')}</strong>
          </Alert>
          <Card title={t('system.pages.health.title')}><DetailList item={h} locale={locale} /></Card>
          {/* Phase 19 — dependency health (storage / AI / background jobs), request metrics, deployment. */}
          {query.data.storage && <Card title={t('system.health.storage')}><DetailList item={query.data.storage} locale={locale} /></Card>}
          {query.data.aiService && <Card title={t('system.health.aiService')}><DetailList item={query.data.aiService} locale={locale} /></Card>}
          {query.data.backgroundJobs && <Card title={t('system.health.backgroundJobs')}><DetailList item={query.data.backgroundJobs} locale={locale} /></Card>}
          {query.data.metrics && <Card title={t('system.health.metrics')}><DetailList item={query.data.metrics} locale={locale} /></Card>}
          {(query.data.version || query.data.environment) && (
            <Card title={t('system.health.deployment')}>
              <DetailList item={{ version: query.data.version, environment: query.data.environment, uptimeSeconds: query.data.uptimeSeconds }} locale={locale} />
            </Card>
          )}
        </>
      )}
    </>
  )
}

function ErrorMonitoringPage({ userId, locale }) {
  const { t } = useTranslation()
  const query = useSystemQuery(queryKeys.system.operational(userId), (s) => systemApi.operationalStatus(s))
  return (
    <>
      <Head view="errorMonitoring" />
      {query.isLoading && <Loading />}
      {query.isError && <ErrorState error={query.error} onRetry={query.refetch} />}
      {query.data && (
        <>
          <Alert title={t('system.posture.deferredTitle')}>{t('system.notes.errorMonitoring')}</Alert>
          <PostureCard posture={query.data.errorMonitoring} locale={locale} />
        </>
      )}
    </>
  )
}

function BackupsPage({ userId, locale }) {
  const { t } = useTranslation()
  const query = useSystemQuery(queryKeys.system.operational(userId), (s) => systemApi.operationalStatus(s))
  return (
    <>
      <Head view="backups" />
      {query.isLoading && <Loading />}
      {query.isError && <ErrorState error={query.error} onRetry={query.refetch} />}
      {query.data && (
        <>
          <Alert title={t('system.posture.deferredTitle')}>{t('system.notes.backups')}</Alert>
          <PostureCard posture={query.data.backups} locale={locale} />
        </>
      )}
    </>
  )
}

// ---------------------------------------------------------------------------
export default function SystemAdminPortalPage({ view }) {
  const { i18n } = useTranslation()
  const { user } = useAuth()
  const userId = user?.id
  const locale = i18n.language || 'en'
  if (!userId) return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  const common = { userId, locale }
  switch (view) {
    case 'tenants': return <TenantsPage {...common} />
    case 'tenant-details': return <TenantDetailsPage {...common} />
    case 'onboarding': return <OnboardingPage {...common} />
    case 'plans': return <PlansPage {...common} />
    case 'subscriptions': return <SubscriptionsPage {...common} />
    case 'usage': return <UsagePage {...common} />
    case 'ai-usage': return <AiUsagePage {...common} />
    case 'storage': return <StoragePage {...common} />
    case 'support': return <SupportPage {...common} />
    case 'feature-flags': return <FeatureFlagsPage {...common} />
    case 'announcements': return <AnnouncementsPage {...common} />
    case 'health': return <HealthPage {...common} />
    case 'errors': return <ErrorMonitoringPage {...common} />
    case 'audit': return <AuditPage {...common} />
    case 'security': return <SecurityPage {...common} />
    case 'backups': return <BackupsPage {...common} />
    case 'settings': return <SettingsPage {...common} />
    default: return <DashboardPage {...common} />
  }
}
