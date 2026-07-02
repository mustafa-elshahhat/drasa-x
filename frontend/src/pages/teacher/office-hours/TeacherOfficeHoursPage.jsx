import { useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { DetailList } from '../../../shared/data-display'
import { Button, Card, Chip, PageHeader } from '../../../shared/ui'
import { TextField } from '../../../shared/form'
import { ErrorState } from '../../../shared/feedback'
import { Listing, Loading } from '../../../features/teacher/components'
import { useTeacherQuery } from '../../../features/teacher/helpers'
import { useAuth } from '../../../features/auth/AuthContext'
import { teacherApi } from '../../../features/teacher/teacherApi'
import { displayValue, itemId } from '../../../features/teacher/teacherUtils'
import { queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

const STATUS = ['Scheduled', 'Cancelled', 'Completed']
const STATUS_TONE = { 0: 'success', 1: 'danger', 2: 'muted' }
const BOOKING_STATUS = ['Requested', 'Confirmed', 'Cancelled', 'Attended', 'NoShow']
const BOOKING_TONE = { 0: 'warning', 1: 'brand', 2: 'muted', 3: 'success', 4: 'danger' }

const EMPTY_FORM = { title: '', startsAt: '', endsAt: '', capacity: 1 }

function CreateSessionCard({ userId, t }) {
  const qc = useQueryClient()
  const [form, setForm] = useState(EMPTY_FORM)
  const create = useMutation({
    mutationFn: () => teacherApi.createOfficeHour({ title: form.title, startsAt: new Date(form.startsAt).toISOString(), endsAt: new Date(form.endsAt).toISOString(), capacity: Number(form.capacity) || 1 }),
    onSuccess: () => { setForm(EMPTY_FORM); qc.invalidateQueries({ queryKey: queryKeys.teacher.officeHours(userId) }) },
  })
  return (
    <Card title={t('teacher.officeHours.create')}>
      {create.isError && <ErrorState error={create.error} onRetry={() => create.reset()} />}
      <div className="ui-formgrid ui-formgrid--2">
        <TextField label={t('teacher.officeHours.field.title')} value={form.title} onChange={(e) => setForm((f) => ({ ...f, title: e.target.value }))} />
        <TextField label={t('teacher.officeHours.field.capacity')} type="number" min="1" value={form.capacity} onChange={(e) => setForm((f) => ({ ...f, capacity: e.target.value }))} />
        <TextField label={t('teacher.officeHours.field.startsAt')} type="datetime-local" value={form.startsAt} onChange={(e) => setForm((f) => ({ ...f, startsAt: e.target.value }))} />
        <TextField label={t('teacher.officeHours.field.endsAt')} type="datetime-local" value={form.endsAt} onChange={(e) => setForm((f) => ({ ...f, endsAt: e.target.value }))} />
      </div>
      <Button onClick={() => create.mutate()} loading={create.isPending} disabled={!form.title.trim() || !form.startsAt || !form.endsAt}>
        {t('teacher.officeHours.create')}
      </Button>
    </Card>
  )
}

function BookingsPanel({ userId, sessionId, t }) {
  const qc = useQueryClient()
  const bookings = useTeacherQuery(queryKeys.teacher.officeHourBookings(userId, sessionId), (signal) => teacherApi.officeHourBookings(sessionId, signal))
  const mark = useMutation({
    mutationFn: ({ bookingId, status }) => teacherApi.markAttendance(bookingId, status),
    onSuccess: () => qc.invalidateQueries({ queryKey: queryKeys.teacher.officeHourBookings(userId, sessionId) }),
  })
  if (bookings.isLoading) return <Loading />
  if (bookings.isError) return <ErrorState error={bookings.error} onRetry={bookings.refetch} />
  const items = Array.isArray(bookings.data) ? bookings.data : []
  if (!items.length) return <p className="ui-muted">{t('teacher.officeHours.noBookings')}</p>
  return (
    <ul className="ui-list">
      {items.map((b) => (
        <li className="ui-list__item" key={itemId(b)}>
          <div className="ui-list__body">
            <div className="ui-list__title">{displayValue(b, ['studentId', 'StudentId'])}</div>
          </div>
          <Chip tone={BOOKING_TONE[b.status] ?? 'muted'}>{t(`teacher.officeHours.bookingStatus.${BOOKING_STATUS[b.status] || 'Requested'}`, BOOKING_STATUS[b.status])}</Chip>
          {(b.status === 0 || b.status === 1) && (
            <span className="cluster">
              <Button variant="secondary" onClick={() => mark.mutate({ bookingId: itemId(b), status: 3 })} loading={mark.isPending}>
                {t('teacher.officeHours.markAttended')}
              </Button>
              <Button variant="secondary" onClick={() => mark.mutate({ bookingId: itemId(b), status: 4 })} loading={mark.isPending}>
                {t('teacher.officeHours.markNoShow')}
              </Button>
            </span>
          )}
        </li>
      ))}
    </ul>
  )
}

function SessionRow({ userId, locale, item, t }) {
  const qc = useQueryClient()
  const [editing, setEditing] = useState(false)
  const [showBookings, setShowBookings] = useState(false)
  const [form, setForm] = useState(null)
  const invalidate = () => qc.invalidateQueries({ queryKey: queryKeys.teacher.officeHours(userId) })
  const update = useMutation({
    mutationFn: () => teacherApi.updateOfficeHour(itemId(item), { title: form.title, startsAt: new Date(form.startsAt).toISOString(), endsAt: new Date(form.endsAt).toISOString(), capacity: Number(form.capacity) || 1 }),
    onSuccess: () => { setEditing(false); invalidate() },
  })
  const cancel = useMutation({ mutationFn: () => teacherApi.cancelOfficeHour(itemId(item)), onSuccess: invalidate })

  const startEdit = () => {
    setForm({
      title: item.title || '',
      startsAt: item.startsAt ? item.startsAt.slice(0, 16) : '',
      endsAt: item.endsAt ? item.endsAt.slice(0, 16) : '',
      capacity: item.capacity ?? 1,
    })
    setEditing(true)
  }

  const isScheduled = item.status === 0

  return (
    <div className="student-list__item">
      {!editing && (
        <>
          <div className="cluster mb-2">
            <strong className="domain-row__title">{item.title || displayValue(item)}</strong>
            <Chip tone={STATUS_TONE[item.status] ?? 'muted'}>{t(`teacher.officeHours.status.${STATUS[item.status] || 'Scheduled'}`, STATUS[item.status])}</Chip>
          </div>
          <DetailList item={item} locale={locale} autoOptions={{ exclude: ['id', 'teacherId', 'myBookingId'] }} />
          {update.isError && <ErrorState error={update.error} onRetry={() => update.reset()} />}
          {cancel.isError && <ErrorState error={cancel.error} onRetry={() => cancel.reset()} />}
          <div className="cluster">
            {isScheduled && <Button variant="secondary" onClick={startEdit}>{t('teacher.officeHours.edit')}</Button>}
            {isScheduled && <Button variant="secondary" onClick={() => cancel.mutate()} loading={cancel.isPending}>{t('teacher.officeHours.cancelSession')}</Button>}
            <Button variant="secondary" onClick={() => setShowBookings((s) => !s)}>
              {showBookings ? t('teacher.officeHours.hideBookings') : t('teacher.officeHours.viewBookings')}
            </Button>
          </div>
          {showBookings && <BookingsPanel userId={userId} sessionId={itemId(item)} t={t} />}
        </>
      )}
      {editing && form && (
        <>
          <div className="ui-formgrid ui-formgrid--2">
            <TextField label={t('teacher.officeHours.field.title')} value={form.title} onChange={(e) => setForm((f) => ({ ...f, title: e.target.value }))} />
            <TextField label={t('teacher.officeHours.field.capacity')} type="number" min="1" value={form.capacity} onChange={(e) => setForm((f) => ({ ...f, capacity: e.target.value }))} />
            <TextField label={t('teacher.officeHours.field.startsAt')} type="datetime-local" value={form.startsAt} onChange={(e) => setForm((f) => ({ ...f, startsAt: e.target.value }))} />
            <TextField label={t('teacher.officeHours.field.endsAt')} type="datetime-local" value={form.endsAt} onChange={(e) => setForm((f) => ({ ...f, endsAt: e.target.value }))} />
          </div>
          <div className="cluster">
            <Button onClick={() => update.mutate()} loading={update.isPending} disabled={!form.title.trim()}>{t('actions.save')}</Button>
            <Button variant="secondary" onClick={() => setEditing(false)}>{t('actions.cancel')}</Button>
          </div>
        </>
      )}
    </div>
  )
}

function OfficeHoursPage({ userId, locale }) {
  const { t } = useTranslation()
  const { role } = useAuth()
  const query = useTeacherQuery(queryKeys.teacher.officeHours(userId), (signal) => teacherApi.officeHours(signal))
  return (
    <>
      <PageHeader title={t('teacher.officeHours.title')} description={t('teacher.officeHours.description')} />
      {/* D8: session creation stays Teacher-owned in the backend (OfficeHourService.CreateAsync
          throws Forbidden for SchoolAdmin) — hide the dead action rather than expose a 403. */}
      {role === 'Teacher' && <CreateSessionCard userId={userId} t={t} />}
      <Listing query={query} empty={t('teacher.empty.officeHours')}>
        {(items) => (
          <div className="student-list">
            {items.map((item, idx) => (
              <SessionRow key={itemId(item) || idx} userId={userId} locale={locale} item={item} t={t} />
            ))}
          </div>
        )}
      </Listing>
    </>
  )
}

export default function TeacherOfficeHoursPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <OfficeHoursPage userId={userId} locale={locale} {...props} />
}
