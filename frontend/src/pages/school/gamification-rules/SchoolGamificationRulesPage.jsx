import { useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { CheckboxField, SelectField, TextField, TextareaField } from '../../../shared/form'
import { Card, FormModal, Button } from '../../../shared/ui'
import { ErrorState } from '../../../shared/feedback'
import { Head, List } from '../../../features/school/components'
import { useSchoolQuery } from '../../../features/school/helpers'
import { schoolApi } from '../../../features/school/schoolApi'
import { getField } from '../../../features/student/studentUtils'
import { queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

// GamificationTrigger enum (numeric on the wire, no JsonStringEnumConverter):
// OfficeHourAttended=0, CompetitionTopRank=1, CompetitionParticipation=2, CommunityPost=3,
// ManualAward=4. `GamificationController.RulesAsync` allows Teacher OR SchoolAdmin to read, but
// `UpsertRuleAsync` (PUT gamification/rules) is SchoolAdmin-only server-side — so this editor lives
// on schoolApi.js (see schoolApi.gamificationRules/upsertGamificationRule), NOT on the pre-existing
// teacherApi.gamificationRules/upsertGamificationRule pair, which stay as-is (a separate, already
// dead-but-tested surface owned by another workstream).
const TRIGGER = ['OfficeHourAttended', 'CompetitionTopRank', 'CompetitionParticipation', 'CommunityPost', 'ManualAward']

function triggerOptions(t, existingTriggers) {
  return TRIGGER.map((name, value) => ({
    value,
    label: t(`school.gamificationRules.triggerValue.${name}`) + (existingTriggers.has(value) ? ` (${t('school.gamificationRules.hasRule')})` : ''),
  }))
}

// Code is the upsert key (PUT gamification/rules carries no {id} in its route — the backend looks
// the row up by Code) and Trigger is its fixed classification (which automatic event the rule
// prices). The "create a rule for a trigger with none yet" flow below assumes at most one rule per
// trigger, and changing either on an EXISTING rule would silently detach it from the trigger admins
// expect it to price. So both are locked once a rule exists: the edit form only exposes the levers
// admins actually need to touch (name/description/points/enabled); Code+Trigger are chosen once, at
// create time, where Code is pre-filled from the trigger's canonical name but stays editable.
function editFields(t) {
  return [
    { name: 'name', label: t('school.common.name'), required: true },
    { name: 'description', label: t('school.gamificationRules.description'), type: 'textarea' },
    { name: 'points', label: t('school.gamificationRules.points'), type: 'number', required: true, hint: t('school.gamificationRules.pointsHint') },
    { name: 'enabled', label: t('school.gamificationRules.enabled'), type: 'checkbox' },
  ]
}

function CreateRuleCard({ userId, existingTriggers, t }) {
  const qc = useQueryClient()
  const defaultTrigger = (() => {
    const free = TRIGGER.findIndex((_, value) => !existingTriggers.has(value))
    return free === -1 ? 0 : free
  })()
  const [trigger, setTrigger] = useState(defaultTrigger)
  const [code, setCode] = useState(TRIGGER[defaultTrigger])
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [points, setPoints] = useState('')
  const [enabled, setEnabled] = useState(true)

  const create = useMutation({
    mutationFn: () => schoolApi.upsertGamificationRule({
      code: code.trim(),
      name: name.trim(),
      description: description.trim() || null,
      trigger: Number(trigger),
      points: Number(points) || 0,
      enabled,
    }),
    onSuccess: () => {
      setName(''); setDescription(''); setPoints(''); setEnabled(true)
      qc.invalidateQueries({ queryKey: queryKeys.school.gamificationRules(userId) })
    },
  })

  const onTriggerChange = (e) => {
    const value = Number(e.target.value)
    setTrigger(value)
    setCode(TRIGGER[value])
  }

  return (
    <Card title={t('school.gamificationRules.create')}>
      {create.isError && <ErrorState error={create.error} onRetry={() => create.reset()} />}
      <div className="ui-formgrid ui-formgrid--2">
        <SelectField label={t('school.gamificationRules.trigger')} value={trigger} onChange={onTriggerChange} options={triggerOptions(t, existingTriggers)} />
        <TextField label={t('school.gamificationRules.code')} value={code} onChange={(e) => setCode(e.target.value)} hint={t('school.gamificationRules.codeHint')} />
        <TextField label={t('school.common.name')} value={name} onChange={(e) => setName(e.target.value)} />
        <TextField label={t('school.gamificationRules.points')} type="number" value={points} onChange={(e) => setPoints(e.target.value)} hint={t('school.gamificationRules.pointsHint')} />
      </div>
      <TextareaField label={t('school.gamificationRules.description')} value={description} onChange={(e) => setDescription(e.target.value)} />
      <CheckboxField label={t('school.gamificationRules.enabled')} checked={enabled} onChange={(e) => setEnabled(e.target.checked)} />
      <Button onClick={() => create.mutate()} loading={create.isPending} disabled={!code.trim() || !name.trim() || points === ''}>
        {t('school.gamificationRules.create')}
      </Button>
    </Card>
  )
}

function GamificationRulesPage({ userId, locale }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const query = useSchoolQuery(queryKeys.school.gamificationRules(userId), (s) => schoolApi.gamificationRules(s))
  const rules = Array.isArray(query.data) ? query.data : []
  const existingTriggers = new Set(rules.map((r) => Number(getField(r, 'trigger'))))

  const [editingRule, setEditingRule] = useState(null)
  const update = useMutation({
    mutationFn: (values) => schoolApi.upsertGamificationRule({
      code: getField(editingRule, 'code'),
      trigger: Number(getField(editingRule, 'trigger')),
      name: values.name.trim(),
      description: values.description.trim() || null,
      points: Number(values.points) || 0,
      enabled: !!values.enabled,
      badgeId: getField(editingRule, 'badgeId') || null,
    }),
    onSuccess: () => { setEditingRule(null); qc.invalidateQueries({ queryKey: queryKeys.school.gamificationRules(userId) }) },
  })

  const columns = [
    { key: 'code', header: t('school.gamificationRules.code') },
    { key: 'name', header: t('school.common.name') },
    { key: 'trigger', header: t('school.gamificationRules.trigger'), chip: (v) => ({ tone: 'info', label: t(`school.gamificationRules.triggerValue.${TRIGGER[v] || 'ManualAward'}`) }) },
    { key: 'points', header: t('school.gamificationRules.points') },
    { key: 'enabled', header: t('school.gamificationRules.enabled'), bool: {} },
  ]

  return (
    <>
      <Head view="gamificationRules" />
      {/* Gated on the rules query settling (not rendered during the initial load): CreateRuleCard
          seeds its Trigger/Code state once, from `existingTriggers`, via useState's initial value.
          Mounting it before the first fetch resolves would freeze that default at an empty set and
          never reflect the real "which triggers already have a rule" data. The key re-derives that
          default (re-mounting the card) whenever the set of already-ruled triggers actually changes
          — e.g. right after creating a rule, so the next suggestion skips the trigger just used. */}
      {!query.isLoading && (
        <CreateRuleCard key={[...existingTriggers].sort().join(',')} userId={userId} existingTriggers={existingTriggers} t={t} />
      )}
      <List
        query={query}
        columns={columns}
        empty={t('school.empty.gamificationRules')}
        locale={locale}
        rowActions={(item) => (
          <Button variant="secondary" onClick={() => setEditingRule(item)}>{t('school.common.edit')}</Button>
        )}
      />
      <FormModal
        open={Boolean(editingRule)}
        onClose={() => setEditingRule(null)}
        title={editingRule ? `${t('school.gamificationRules.editTitle')} — ${getField(editingRule, 'code')}` : t('school.gamificationRules.editTitle')}
        fields={editingRule ? editFields(t) : []}
        initialValues={editingRule ? {
          name: getField(editingRule, 'name') || '',
          description: getField(editingRule, 'description') || '',
          points: String(getField(editingRule, 'points') ?? ''),
          enabled: Boolean(getField(editingRule, 'enabled')),
        } : {}}
        onSubmit={(values) => update.mutate(values)}
        submitting={update.isPending}
        error={update.error}
        submitLabel={t('school.common.save')}
      />
    </>
  )
}

export default function SchoolGamificationRulesPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <GamificationRulesPage userId={userId} locale={locale} {...props} />
}
