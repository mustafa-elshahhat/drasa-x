import { useState } from 'react'
import { Link, useLocation, useNavigate, useParams } from 'react-router-dom'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Trophy, FileText, Plus } from 'lucide-react'
import { DetailList } from '../../../shared/data-display'
import { UnitCard, LeaderboardRow } from '../../../shared/domain'
import { TextField, TextareaField } from '../../../shared/form'
import { Alert, Button, Card, PageHeader } from '../../../shared/ui'
import { ErrorState } from '../../../shared/feedback'
import { Listing, Loading } from '../../../features/teacher/components'
import { useTeacherQuery } from '../../../features/teacher/helpers'
import { teacherApi } from '../../../features/teacher/teacherApi'
import { competitionStatusName, displayValue, getField, itemId } from '../../../features/teacher/teacherUtils'
import { queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function statusTone(name) {
  const n = String(name).toLowerCase()
  if (n === 'published' || n === 'active') return 'success'
  if (n === 'closed') return 'info'
  if (n === 'archived') return 'danger'
  return 'warning' // draft
}

const toLocalInput = (v) => {
  if (!v) return ''
  const d = new Date(v)
  if (Number.isNaN(d.getTime())) return ''
  const pad = (n) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`
}

// ---- List ------------------------------------------------------------------

function CompetitionList({ userId }) {
  const { t } = useTranslation()
  const query = useTeacherQuery(queryKeys.teacher.competitions(userId), (signal) => teacherApi.competitions(signal))
  return (
    <>
      <PageHeader
        title={t('teacher.competitions.title', 'Competitions')}
        description={t('teacher.competitions.description', 'Create, publish, judge and close competitions')}
        actions={<Link className="ui-btn ui-btn--primary" to="/app/teacher/competitions/new"><Plus size={16} aria-hidden="true" /> {t('teacher.competitions.new', 'New competition')}</Link>}
      />
      <Listing query={query} empty={t('teacher.empty.competitions', 'No competitions yet')} emptyIcon={Trophy}>
        {(items) => (
          <div className="student-list">
            {items.map((item) => {
              const id = itemId(item, ['id', 'Id'])
              const name = competitionStatusName(item.status ?? item.Status)
              return (
                <UnitCard
                  key={id}
                  to={`/app/teacher/competitions/${id}`}
                  icon={Trophy}
                  title={displayValue(item, ['title', 'Title'])}
                  status={name}
                  statusTone={statusTone(name)}
                />
              )
            })}
          </div>
        )}
      </Listing>
    </>
  )
}

// ---- Create ----------------------------------------------------------------

function CompetitionNew({ userId }) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const qc = useQueryClient()
  const [form, setForm] = useState({ title: '', description: '', startsAt: '', endsAt: '' })
  const create = useMutation({
    mutationFn: () => teacherApi.createCompetition({
      title: form.title.trim(),
      description: form.description.trim() || null,
      startsAt: form.startsAt ? new Date(form.startsAt).toISOString() : null,
      endsAt: form.endsAt ? new Date(form.endsAt).toISOString() : null,
    }),
    onSuccess: (created) => {
      qc.invalidateQueries({ queryKey: queryKeys.teacher.competitions(userId) })
      const id = itemId(created, ['id', 'Id'])
      if (id) navigate(`/app/teacher/competitions/${id}`)
    },
  })
  const valid = form.title.trim() && form.startsAt && form.endsAt
  return (
    <>
      <PageHeader title={t('teacher.competitions.new', 'New competition')} description={t('teacher.competitions.newDescription', 'Create a draft you can publish later')} />
      <Card>
        <form className="stack" onSubmit={(e) => { e.preventDefault(); if (valid) create.mutate() }}>
          <TextField label={t('teacher.competitions.field.title', 'Title')} value={form.title} onChange={(e) => setForm((f) => ({ ...f, title: e.target.value }))} required />
          <TextareaField label={t('teacher.competitions.field.description', 'Description')} value={form.description} onChange={(e) => setForm((f) => ({ ...f, description: e.target.value }))} maxLength={4000} />
          <TextField label={t('teacher.competitions.field.startsAt', 'Starts at')} type="datetime-local" value={form.startsAt} onChange={(e) => setForm((f) => ({ ...f, startsAt: e.target.value }))} required />
          <TextField label={t('teacher.competitions.field.endsAt', 'Ends at')} type="datetime-local" value={form.endsAt} onChange={(e) => setForm((f) => ({ ...f, endsAt: e.target.value }))} required />
          <div className="student-actions">
            <Button type="submit" loading={create.isPending} disabled={!valid}>{t('teacher.competitions.createDraft', 'Save draft')}</Button>
            <Link to="/app/teacher/competitions" className="ui-btn ui-btn--secondary">{t('actions.cancel', 'Cancel')}</Link>
          </div>
          {create.isError && <ErrorState error={create.error} />}
        </form>
      </Card>
    </>
  )
}

// ---- Detail ----------------------------------------------------------------

function CompetitionDetail({ userId, competitionId, locale }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const comp = useTeacherQuery(queryKeys.teacher.competition(userId, competitionId), (signal) => teacherApi.competition(competitionId, signal), { enabled: Boolean(competitionId) })
  const data = comp.data || {}
  const statusName = competitionStatusName(data.status ?? data.Status)
  const sLower = statusName.toLowerCase()
  const isDraft = sLower === 'draft'
  const isPublishedOrActive = sLower === 'published' || sLower === 'active'
  const isClosed = sLower === 'closed'

  const invalidate = () => {
    qc.invalidateQueries({ queryKey: queryKeys.teacher.competition(userId, competitionId) })
    qc.invalidateQueries({ queryKey: queryKeys.teacher.competitions(userId) })
  }
  const publish = useMutation({ mutationFn: () => teacherApi.publishCompetition(competitionId), onSuccess: invalidate })
  const close = useMutation({ mutationFn: () => teacherApi.closeCompetition(competitionId), onSuccess: invalidate })
  const archive = useMutation({ mutationFn: () => teacherApi.archiveCompetition(competitionId), onSuccess: invalidate })

  if (comp.isLoading) return (<><PageHeader title={t('teacher.competitions.details', 'Competition')} /><Loading /></>)
  if (comp.isError) return <ErrorState error={comp.error} onRetry={comp.refetch} />

  return (
    <>
      <PageHeader
        title={displayValue(data, ['title', 'Title']) || t('teacher.competitions.details', 'Competition')}
        description={t('teacher.competitions.reviewDescription', 'Manage the competition lifecycle')}
        actions={
          <span className="cluster">
            {isDraft && <Button onClick={() => publish.mutate()} loading={publish.isPending}>{t('teacher.competitions.publish', 'Publish')}</Button>}
            {isPublishedOrActive && <Button onClick={() => close.mutate()} loading={close.isPending}>{t('teacher.competitions.close', 'Close & publish results')}</Button>}
            {!isClosed && sLower !== 'archived' && <Button variant="secondary" onClick={() => archive.mutate()} loading={archive.isPending}>{t('teacher.competitions.archive', 'Archive')}</Button>}
          </span>
        }
      />
      <Card title={t('teacher.details', 'Details')}>
        <Alert variant={isDraft ? 'info' : 'success'} title={`${t('teacher.competitions.status', 'Status')}: ${statusName}`} />
        <DetailList item={data} locale={locale} />
      </Card>
      {(publish.isError || close.isError || archive.isError) && <ErrorState error={publish.error || close.error || archive.error} />}

      {isDraft && <CompetitionEditCard competitionId={competitionId} data={data} onSaved={invalidate} />}

      <SubmissionsCard userId={userId} competitionId={competitionId} locale={locale} />
      <ScoreCard userId={userId} competitionId={competitionId} />
      <LeaderboardCard userId={userId} competitionId={competitionId} />
    </>
  )
}

function CompetitionEditCard({ competitionId, data, onSaved }) {
  const { t } = useTranslation()
  const [form, setForm] = useState({
    title: data.title ?? data.Title ?? '',
    description: data.description ?? data.Description ?? '',
    startsAt: toLocalInput(data.startsAt ?? data.StartsAt),
    endsAt: toLocalInput(data.endsAt ?? data.EndsAt),
  })
  const save = useMutation({
    mutationFn: () => teacherApi.updateCompetition(competitionId, {
      title: form.title.trim(),
      description: form.description.trim() || null,
      startsAt: form.startsAt ? new Date(form.startsAt).toISOString() : null,
      endsAt: form.endsAt ? new Date(form.endsAt).toISOString() : null,
    }),
    onSuccess: onSaved,
  })
  const valid = form.title.trim() && form.startsAt && form.endsAt
  return (
    <Card title={t('teacher.competitions.edit', 'Edit draft')}>
      <form className="stack" onSubmit={(e) => { e.preventDefault(); if (valid) save.mutate() }}>
        <TextField label={t('teacher.competitions.field.title', 'Title')} value={form.title} onChange={(e) => setForm((f) => ({ ...f, title: e.target.value }))} required />
        <TextareaField label={t('teacher.competitions.field.description', 'Description')} value={form.description} onChange={(e) => setForm((f) => ({ ...f, description: e.target.value }))} maxLength={4000} />
        <TextField label={t('teacher.competitions.field.startsAt', 'Starts at')} type="datetime-local" value={form.startsAt} onChange={(e) => setForm((f) => ({ ...f, startsAt: e.target.value }))} required />
        <TextField label={t('teacher.competitions.field.endsAt', 'Ends at')} type="datetime-local" value={form.endsAt} onChange={(e) => setForm((f) => ({ ...f, endsAt: e.target.value }))} required />
        <Button type="submit" loading={save.isPending} disabled={!valid}>{t('actions.save', 'Save')}</Button>
        {save.isError && <ErrorState error={save.error} />}
        {save.isSuccess && <Alert variant="success" title={t('teacher.competitions.saved', 'Saved')} />}
      </form>
    </Card>
  )
}

function SubmissionsCard({ userId, competitionId, locale }) {
  const { t } = useTranslation()
  const submissions = useTeacherQuery(queryKeys.teacher.competitionSubmissions(userId, competitionId), (signal) => teacherApi.competitionSubmissions(competitionId, signal), { enabled: Boolean(competitionId) })
  return (
    <Card title={t('teacher.competitions.submissions', 'Submissions')}>
      <Listing query={submissions} empty={t('teacher.empty.submissions', 'No submissions yet')}>
        {(items) => (
          <div className="student-list">
            {items.map((s) => (
              <div className="student-list__item" key={itemId(s, ['id', 'Id'])}>
                <strong className="domain-row__title">{displayValue(s, ['studentName', 'StudentName', 'studentId', 'StudentId'])}</strong>
                <DetailList item={s} locale={locale} />
              </div>
            ))}
          </div>
        )}
      </Listing>
    </Card>
  )
}

function ScoreCard({ userId, competitionId }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const [entryId, setEntryId] = useState('')
  const [score, setScore] = useState('')
  const record = useMutation({
    mutationFn: () => teacherApi.scoreCompetitionEntry(competitionId, entryId.trim(), Number(score)),
    onSuccess: () => {
      setScore('')
      qc.invalidateQueries({ queryKey: queryKeys.teacher.competitionLeaderboard(userId, competitionId) })
    },
  })
  return (
    <Card title={t('teacher.competitions.recordScore', 'Record a score')}>
      <p className="ui-muted">{t('teacher.competitions.recordScoreHint', 'Award a score to a competition entry (by entry id).')}</p>
      <form className="stack" onSubmit={(e) => { e.preventDefault(); if (entryId.trim() && score !== '') record.mutate() }}>
        <TextField label={t('teacher.competitions.entryId', 'Entry id')} value={entryId} onChange={(e) => setEntryId(e.target.value)} required />
        <TextField label={t('teacher.competitions.score', 'Score')} type="number" min="0" value={score} onChange={(e) => setScore(e.target.value)} required />
        <Button type="submit" loading={record.isPending} disabled={!entryId.trim() || score === ''}>{t('teacher.competitions.recordScoreButton', 'Record score')}</Button>
        {record.isError && <ErrorState error={record.error} />}
        {record.isSuccess && <Alert variant="success" title={t('teacher.competitions.scoreRecorded', 'Score recorded')} />}
      </form>
    </Card>
  )
}

function LeaderboardCard({ userId, competitionId }) {
  const { t } = useTranslation()
  const leaderboard = useTeacherQuery(queryKeys.teacher.competitionLeaderboard(userId, competitionId), (signal) => teacherApi.competitionLeaderboard(competitionId, signal), { enabled: Boolean(competitionId) })
  return (
    <Card title={t('teacher.competitions.leaderboard', 'Leaderboard')}>
      <Listing query={leaderboard} empty={t('teacher.empty.leaderboard', 'No scored entries yet')}>
        {(items) => (
          <ul className="domain-lb-list">
            {items.map((item, i) => (
              <LeaderboardRow
                key={itemId(item) || i}
                rank={getField(item, 'rank') ?? i + 1}
                name={displayValue(item, ['studentName', 'StudentName', 'studentId', 'StudentId'])}
                points={getField(item, 'score') ?? getField(item, 'Score')}
                pointsLabel={t('teacher.competitions.points', 'pts')}
              />
            ))}
          </ul>
        )}
      </Listing>
    </Card>
  )
}

// ---- Router shell -----------------------------------------------------------

function CompetitionRouter({ userId, locale }) {
  const { competitionId } = useParams()
  const { pathname } = useLocation()
  if (competitionId) return <CompetitionDetail userId={userId} competitionId={competitionId} locale={locale} />
  if (pathname.endsWith('/competitions/new')) return <CompetitionNew userId={userId} />
  return <CompetitionList userId={userId} />
}

export default function TeacherCompetitionsPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <CompetitionRouter userId={userId} locale={locale} {...props} />
}
