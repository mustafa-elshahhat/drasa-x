import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { CalendarClock } from 'lucide-react'
import { useStudentContext } from '../../../features/student/helpers'
import { Button, Card, PageHeader } from '../../../shared/ui'
import { EmptyState, ErrorState } from '../../../shared/feedback'
import { useStudentQuery } from '../../../features/student/helpers'
import { Loading } from '../../../features/student/Loading'
import { studentApi } from '../../../features/student/studentApi'
import { toItems } from '../../../features/student/studentSchemas'
import { displayValue, formatDate, getField, itemId, settledData } from '../../../features/student/studentUtils'
import { queryKeys } from '../../../lib/query/keys'

function OfficeHoursPage({ userId, locale }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const query = useStudentQuery(queryKeys.student.officeHours(userId), (signal) => studentApi.officeHours(signal))
  const book = useMutation({ mutationFn: (id) => studentApi.bookOfficeHour(id), onSuccess: () => qc.invalidateQueries({ queryKey: queryKeys.student.officeHours(userId) }) })
  const cancelBooking = useMutation({ mutationFn: (bookingId) => studentApi.cancelBooking(bookingId), onSuccess: () => qc.invalidateQueries({ queryKey: queryKeys.student.officeHours(userId) }) })
  const available = query.data ? toItems(settledData(query.data.available)) : []
  const mine = query.data ? toItems(settledData(query.data.mine)) : []
  return (
    <>
      <PageHeader title={t('student.officeHours.title')} description={t('student.officeHours.description')} />
      {query.isLoading && <Loading />}
      {query.isError && <ErrorState error={query.error} onRetry={query.refetch} />}
      {book.isError && <ErrorState error={book.error} />}
      <Card title={t('student.officeHours.available')}>
        {available.length === 0 ? <EmptyState title={t('student.empty.officeHours')} /> : (
          <div className="student-list">
            {available.map((item) => (
              <div className="domain-row" key={itemId(item)}>
                <span className="domain-row__icon" aria-hidden="true"><CalendarClock size={18} /></span>
                <span className="domain-row__main">
                  <span className="domain-row__title">{displayValue(item, ['teacherName', 'TeacherName']) || displayValue(item)}</span>
                  <span className="domain-row__meta">{formatDate(getField(item, 'startsAt') || getField(item, 'startTime'), locale)}</span>
                </span>
                <Button onClick={() => book.mutate(itemId(item))} loading={book.isPending}>{t('student.officeHours.book')}</Button>
              </div>
            ))}
          </div>
        )}
      </Card>
      {cancelBooking.isError && <ErrorState error={cancelBooking.error} onRetry={() => cancelBooking.reset()} />}
      <Card title={t('student.officeHours.bookings')}>
        {mine.length === 0 ? <EmptyState title={t('student.officeHours.noBookings')} /> : (
          <div className="student-list">
            {mine.map((item) => {
              const bookingId = getField(item, 'myBookingId')
              return (
                <div className="domain-row" key={itemId(item)}>
                  <span className="domain-row__icon" aria-hidden="true"><CalendarClock size={18} /></span>
                  <span className="domain-row__main">
                    <span className="domain-row__title">{displayValue(item, ['teacherName', 'TeacherName']) || displayValue(item)}</span>
                    <span className="domain-row__meta">{formatDate(getField(item, 'startsAt') || getField(item, 'startTime'), locale)}</span>
                  </span>
                  {bookingId && (
                    <Button variant="secondary" onClick={() => cancelBooking.mutate(bookingId)} loading={cancelBooking.isPending}>
                      {t('student.officeHours.cancelBooking')}
                    </Button>
                  )}
                </div>
              )
            })}
          </div>
        )}
      </Card>
    </>
  )
}

// =============================================================================
// Notifications / announcements / suggestions
// =============================================================================

export default function StudentOfficeHoursPage(props) {
  const { userId, locale } = useStudentContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <OfficeHoursPage userId={userId} locale={locale} {...props} />
}
