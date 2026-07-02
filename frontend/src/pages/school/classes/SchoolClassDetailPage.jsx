import { useMemo, useState } from 'react'
import { useParams } from 'react-router-dom'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { SelectField, TextField } from '../../../shared/form'
import { Alert, Button, Card, Chip } from '../../../shared/ui'
import { DetailList } from '../../../shared/data-display'
import { ErrorState } from '../../../shared/feedback'
import { Head, Loading } from '../../../features/school/components'
import { useSchoolQuery } from '../../../features/school/helpers'
import { schoolApi } from '../../../features/school/schoolApi'
import { displayValue, itemId } from '../../../features/student/studentUtils'
import { STALE, queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

const ENROLLMENT_STATUS = ['Active', 'Withdrawn', 'Completed', 'Transferred', 'Suspended']
const ENROLLMENT_TONE = { 0: 'success', 1: 'muted', 2: 'brand', 3: 'warning', 4: 'danger' }

function ClassDetailPage({ userId, locale }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const { classId } = useParams()

  const klass = useSchoolQuery(queryKeys.school.class(userId, classId), (s) => schoolApi.getClass(classId, s), { enabled: Boolean(classId) })
  const enrollments = useSchoolQuery(queryKeys.school.enrollments(userId, classId), (s) => schoolApi.enrollments(classId, s), { enabled: Boolean(classId) })
  const students = useSchoolQuery(queryKeys.school.users(userId, 'Student'), (s) => schoolApi.users('Student', s), { staleTime: STALE.medium })

  const studentItems = Array.isArray(students.data) ? students.data : []
  const studentById = useMemo(() => {
    const map = new Map()
    for (const s of studentItems) map.set(itemId(s), s)
    return map
  }, [studentItems])

  const [editForm, setEditForm] = useState(null)
  const startEdit = () => setEditForm({ name: klass.data?.name || '', code: klass.data?.code || '', capacity: klass.data?.capacity ?? '' })
  const update = useMutation({
    mutationFn: () => schoolApi.updateClass(classId, { name: editForm.name, code: editForm.code, capacity: editForm.capacity ? Number(editForm.capacity) : null }),
    onSuccess: () => { setEditForm(null); qc.invalidateQueries({ queryKey: queryKeys.school.class(userId, classId) }); qc.invalidateQueries({ queryKey: queryKeys.school.classes(userId) }) },
  })

  const [studentId, setStudentId] = useState('')
  const invalidateRoster = () => {
    qc.invalidateQueries({ queryKey: queryKeys.school.enrollments(userId, classId) })
    qc.invalidateQueries({ queryKey: queryKeys.school.class(userId, classId) })
    qc.invalidateQueries({ queryKey: queryKeys.school.classes(userId) })
  }
  const enroll = useMutation({
    mutationFn: () => schoolApi.enrollStudent({ studentId, schoolClassId: classId }),
    onSuccess: () => { setStudentId(''); invalidateRoster() },
  })
  const withdraw = useMutation({ mutationFn: (enrollmentId) => schoolApi.withdrawEnrollment(enrollmentId), onSuccess: invalidateRoster })

  const enrollmentItems = Array.isArray(enrollments.data) ? enrollments.data : []
  const enrolledStudentIds = new Set(enrollmentItems.filter((e) => e.status === 0).map((e) => e.studentId))
  const availableStudents = studentItems.filter((s) => !enrolledStudentIds.has(itemId(s)))

  return (
    <>
      <Head view="classDetails" />
      {klass.isLoading && <Loading />}
      {klass.isError && <ErrorState error={klass.error} onRetry={klass.refetch} />}
      {klass.data && (
        <>
          <Card title={klass.data.name}>
            {!editForm && (
              <>
                <DetailList item={klass.data} locale={locale} />
                <Button variant="secondary" onClick={startEdit}>{t('school.common.edit', 'Edit')}</Button>
              </>
            )}
            {editForm && (
              <>
                {update.isError && <ErrorState error={update.error} onRetry={() => update.reset()} />}
                <div className="ui-formgrid ui-formgrid--2">
                  <TextField label={t('school.common.name')} value={editForm.name} onChange={(e) => setEditForm((f) => ({ ...f, name: e.target.value }))} />
                  <TextField label={t('school.common.code')} value={editForm.code} onChange={(e) => setEditForm((f) => ({ ...f, code: e.target.value }))} />
                  <TextField label={t('school.common.capacity')} type="number" value={editForm.capacity} onChange={(e) => setEditForm((f) => ({ ...f, capacity: e.target.value }))} />
                </div>
                <div className="cluster">
                  <Button onClick={() => update.mutate()} loading={update.isPending} disabled={!editForm.name.trim() || !editForm.code.trim()}>{t('school.common.save')}</Button>
                  <Button variant="secondary" onClick={() => setEditForm(null)}>{t('actions.cancel', 'Cancel')}</Button>
                </div>
              </>
            )}
          </Card>

          <Card title={t('school.pages.enrollments.title')}>
            {enroll.isError && <ErrorState error={enroll.error} onRetry={() => enroll.reset()} />}
            <div className="ui-form-row">
              <SelectField
                label={t('school.common.student')}
                value={studentId}
                onChange={(e) => setStudentId(e.target.value)}
                options={[{ value: '', label: t('school.common.choose') }, ...availableStudents.map((s) => ({ value: itemId(s), label: displayValue(s, ['fullName', 'FullName']) || itemId(s) }))]}
              />
              <Button onClick={() => enroll.mutate()} loading={enroll.isPending} disabled={!studentId}>{t('school.common.enroll')}</Button>
            </div>

            {enrollments.isLoading && <Loading />}
            {enrollments.isError && <ErrorState error={enrollments.error} onRetry={enrollments.refetch} />}
            {!enrollments.isLoading && enrollmentItems.length === 0 && <Alert title={t('school.empty.generic', 'No records yet.')} />}
            {enrollmentItems.length > 0 && (
              <ul className="ui-list">
                {enrollmentItems.map((e) => {
                  const student = studentById.get(e.studentId)
                  const label = student ? (displayValue(student, ['fullName', 'FullName']) || e.studentId) : e.studentId
                  return (
                    <li className="ui-list__item" key={itemId(e)}>
                      <div className="ui-list__body">
                        <div className="ui-list__title">{label}</div>
                      </div>
                      <Chip tone={ENROLLMENT_TONE[e.status] ?? 'muted'}>{t(`school.enrollmentStatus.${ENROLLMENT_STATUS[e.status] || 'Active'}`, ENROLLMENT_STATUS[e.status])}</Chip>
                      {e.status === 0 && (
                        <Button variant="secondary" onClick={() => withdraw.mutate(itemId(e))} loading={withdraw.isPending}>
                          {t('school.common.withdraw')}
                        </Button>
                      )}
                    </li>
                  )
                })}
              </ul>
            )}
          </Card>
        </>
      )}
    </>
  )
}

export default function SchoolClassDetailPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <ClassDetailPage userId={userId} locale={locale} {...props} />
}
