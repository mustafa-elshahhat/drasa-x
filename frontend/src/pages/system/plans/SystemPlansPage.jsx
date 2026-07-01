import { useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Crud } from '../../../shared/data-display'
import { FormModal, Button } from '../../../shared/ui'
import { ErrorState } from '../../../shared/feedback'
import { itemId, getField } from '../../../features/student/studentUtils'
import { PLAN_TIER, BILLING_PERIOD } from '../../../features/system/constants'
import { useSystemQuery } from '../../../features/system/helpers'
import { systemApi } from '../../../features/system/systemApi'
import { STALE, queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

// Shared field config for both the create modal (Crud) and the edit modal
// (FormModal) — mirrors CreatePlanDto/UpdatePlanDto on the backend one-to-one.
// Numbers are collected as strings by the underlying inputs; toPlanPayload()
// converts them and applies the "blank = unlimited" convention.
function planFields(t) {
  return [
    { name: 'name', label: t('system.plans.name'), required: true },
    { name: 'code', label: t('system.plans.code'), required: true, hint: t('system.plans.codeHint') },
    { name: 'description', label: t('system.plans.description'), type: 'textarea', full: true },
    { name: 'tier', label: t('system.plans.tier'), type: 'select', options: PLAN_TIER.map((label, value) => (label ? { value, label: t(`system.plans.tierValue.${label}`) } : null)).filter(Boolean) },
    { name: 'billingPeriod', label: t('system.plans.billingPeriod'), type: 'select', options: BILLING_PERIOD.map((label, value) => ({ value, label: t(`system.plans.billingPeriodValue.${label}`) })) },
    { name: 'price', label: t('system.plans.price'), type: 'number', required: true },
    { name: 'currency', label: t('system.plans.currency'), required: true, hint: t('system.plans.currencyHint') },
    { name: 'trialDays', label: t('system.plans.trialDays'), type: 'number' },
    { name: 'isActive', label: t('system.plans.isActive'), type: 'checkbox' },
    { name: 'maxStudents', label: t('system.plans.maxStudents'), type: 'number', hint: t('system.plans.unlimitedHint') },
    { name: 'maxTeachers', label: t('system.plans.maxTeachers'), type: 'number', hint: t('system.plans.unlimitedHint') },
    { name: 'maxParents', label: t('system.plans.maxParents'), type: 'number', hint: t('system.plans.unlimitedHint') },
    { name: 'maxSchoolAdmins', label: t('system.plans.maxSchoolAdmins'), type: 'number', hint: t('system.plans.unlimitedHint') },
    { name: 'maxClasses', label: t('system.plans.maxClasses'), type: 'number', hint: t('system.plans.unlimitedHint') },
    { name: 'maxSubjects', label: t('system.plans.maxSubjects'), type: 'number', hint: t('system.plans.unlimitedHint') },
    { name: 'maxLessonMaterials', label: t('system.plans.maxLessonMaterials'), type: 'number', hint: t('system.plans.unlimitedHint') },
    { name: 'maxStorageMb', label: t('system.plans.maxStorageMb'), type: 'number', hint: t('system.plans.unlimitedHint') },
    { name: 'maxAiGenerationsPerMonth', label: t('system.plans.maxAiGenerationsPerMonth'), type: 'number', hint: t('system.plans.unlimitedHint') },
    { name: 'maxAiTokensPerMonth', label: t('system.plans.maxAiTokensPerMonth'), type: 'number', hint: t('system.plans.unlimitedHint') },
  ]
}

const createInitial = { tier: 1, billingPeriod: 0, currency: 'USD', trialDays: 0, isActive: true }

function toPlanPayload(values) {
  const int = (v) => (v === '' || v === null || v === undefined ? null : Number(v))
  return {
    code: (values.code || '').trim(),
    name: (values.name || '').trim(),
    description: (values.description || '').trim() || null,
    tier: Number(values.tier) || 1, // SubscriptionPlan is 1-indexed (Free=1); 0 is never valid
    billingPeriod: Number(values.billingPeriod) || 0,
    price: Number(values.price) || 0,
    currency: (values.currency || '').trim().toUpperCase(),
    trialDays: Number(values.trialDays) || 0,
    isActive: !!values.isActive,
    maxStudents: int(values.maxStudents),
    maxTeachers: int(values.maxTeachers),
    maxParents: int(values.maxParents),
    maxSchoolAdmins: int(values.maxSchoolAdmins),
    maxClasses: int(values.maxClasses),
    maxSubjects: int(values.maxSubjects),
    maxLessonMaterials: int(values.maxLessonMaterials),
    maxStorageMb: int(values.maxStorageMb),
    maxAiGenerationsPerMonth: int(values.maxAiGenerationsPerMonth),
    maxAiTokensPerMonth: int(values.maxAiTokensPerMonth),
  }
}

// Mirrors the backend's own guard clauses so invalid submissions never reach the
// API and the admin sees the same class of error either way. Returned as a plain
// { detail } object so it flows through the exact same FormModal error display
// path as a real server ProblemDetails rejection.
function validatePlanValues(values, t) {
  if (!values.name?.trim()) return t('system.plans.validation.nameRequired')
  if (!values.code?.trim()) return t('system.plans.validation.codeRequired')
  if (values.price === '' || values.price === null || values.price === undefined || Number.isNaN(Number(values.price)) || Number(values.price) < 0)
    return t('system.plans.validation.priceInvalid')
  if (!values.currency?.trim() || values.currency.trim().length !== 3) return t('system.plans.validation.currencyInvalid')
  return null
}

function planToInitialValues(plan) {
  const str = (v) => (v === null || v === undefined ? '' : String(v))
  return {
    name: getField(plan, 'name') || '',
    code: getField(plan, 'code') || '',
    description: getField(plan, 'description') || '',
    tier: getField(plan, 'tier') ?? 1,
    billingPeriod: getField(plan, 'billingPeriod') ?? 0,
    price: str(getField(plan, 'price')),
    currency: getField(plan, 'currency') || 'USD',
    trialDays: str(getField(plan, 'trialDays')),
    isActive: Boolean(getField(plan, 'isActive')),
    maxStudents: str(getField(plan, 'maxStudents')),
    maxTeachers: str(getField(plan, 'maxTeachers')),
    maxParents: str(getField(plan, 'maxParents')),
    maxSchoolAdmins: str(getField(plan, 'maxSchoolAdmins')),
    maxClasses: str(getField(plan, 'maxClasses')),
    maxSubjects: str(getField(plan, 'maxSubjects')),
    maxLessonMaterials: str(getField(plan, 'maxLessonMaterials')),
    maxStorageMb: str(getField(plan, 'maxStorageMb')),
    maxAiGenerationsPerMonth: str(getField(plan, 'maxAiGenerationsPerMonth')),
    maxAiTokensPerMonth: str(getField(plan, 'maxAiTokensPerMonth')),
  }
}

function PlansPage({ userId, locale }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const query = useSystemQuery(queryKeys.system.plans(userId), (s) => systemApi.plans(s), { staleTime: STALE.medium })
  const [editingPlan, setEditingPlan] = useState(null)

  const invalidate = () => qc.invalidateQueries({ queryKey: queryKeys.system.plans(userId) })

  const createPlan = useMutation({
    mutationFn: (values) => {
      const validationError = validatePlanValues(values, t)
      if (validationError) return Promise.reject({ detail: validationError })
      return systemApi.createPlan(toPlanPayload(values))
    },
    onSuccess: invalidate,
  })
  const updatePlan = useMutation({
    mutationFn: (values) => {
      const validationError = validatePlanValues(values, t)
      if (validationError) return Promise.reject({ detail: validationError })
      return systemApi.updatePlan(itemId(editingPlan), toPlanPayload(values))
    },
    onSuccess: () => { invalidate(); setEditingPlan(null) },
  })

  const columns = [
    { key: 'name', header: t('system.plans.name') },
    { key: 'code', header: t('system.plans.code') },
    { key: 'tier', header: t('system.plans.tier'), chip: (v) => ({ tone: 'info', label: t(`system.plans.tierValue.${PLAN_TIER[v] || 'Free'}`) }) },
    { key: 'price', header: t('system.plans.price'), render: (row) => `${getField(row, 'price')} ${getField(row, 'currency')}` },
    { key: 'billingPeriod', header: t('system.plans.billingPeriod'), chip: (v) => ({ tone: 'muted', label: t(`system.plans.billingPeriodValue.${BILLING_PERIOD[v] || 'Monthly'}`) }) },
    { key: 'isActive', header: t('system.plans.isActive'), bool: { trueKey: 'system.plans.active', falseKey: 'system.plans.inactive' } },
  ]

  return (
    <>
      <Crud
        title={t('system.pages.plans.title')}
        description={t('system.pages.plans.description')}
        rows={Array.isArray(query.data) ? query.data : []}
        loading={query.isLoading}
        error={query.error}
        onRetry={query.refetch}
        columns={columns}
        emptyTitle={t('system.empty.plans')}
        locale={locale}
        createLabel={t('system.plans.add')}
        createTitle={t('system.plans.addTitle')}
        createFields={planFields(t)}
        createInitial={createInitial}
        onCreate={(values) => createPlan.mutateAsync(values)}
        creating={createPlan.isPending}
        createError={createPlan.error}
        rowActions={(item) => (
          <Button variant="secondary" onClick={() => setEditingPlan(item)}>{t('system.common.edit')}</Button>
        )}
      />

      <FormModal
        open={Boolean(editingPlan)}
        onClose={() => setEditingPlan(null)}
        title={t('system.plans.editTitle')}
        fields={editingPlan ? planFields(t) : []}
        initialValues={editingPlan ? planToInitialValues(editingPlan) : {}}
        onSubmit={(values) => updatePlan.mutate(values)}
        submitting={updatePlan.isPending}
        error={updatePlan.error}
        submitLabel={t('system.common.save')}
      />
    </>
  )
}

export default function SystemPlansPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <PlansPage userId={userId} locale={locale} {...props} />
}
