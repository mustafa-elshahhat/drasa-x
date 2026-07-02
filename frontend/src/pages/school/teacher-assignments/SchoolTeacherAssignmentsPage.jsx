import { useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { SelectField } from '../../../shared/form'
import { Alert, Button, Card } from '../../../shared/ui'
import { ErrorState } from '../../../shared/feedback'
import { Head, List } from '../../../features/school/components'
import { CLASS_ROLE } from '../../../features/school/constants'
import { useSchoolQuery } from '../../../features/school/helpers'
import { schoolApi } from '../../../features/school/schoolApi'
import { displayValue, itemId } from '../../../features/student/studentUtils'
import { STALE, queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function TeacherAssignmentsPage({ userId, locale }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const subjectAsg = useSchoolQuery(queryKeys.school.subjectAssignments(userId), (s) => schoolApi.subjectAssignments(s))
  const classAsg = useSchoolQuery(queryKeys.school.classAssignments(userId), (s) => schoolApi.classAssignments(s))
  const teachers = useSchoolQuery(queryKeys.school.users(userId, 'Teacher'), (s) => schoolApi.users('Teacher', s), { staleTime: STALE.medium })
  const subjects = useSchoolQuery(queryKeys.school.subjects(userId), (s) => schoolApi.subjects(s), { staleTime: STALE.medium })
  const classes = useSchoolQuery(queryKeys.school.classes(userId), (s) => schoolApi.classes(s), { staleTime: STALE.medium })
  const [subjForm, setSubjForm] = useState({ teacherId: '', subjectId: '' })
  const [classForm, setClassForm] = useState({ teacherId: '', schoolClassId: '', subjectId: '', role: 0 })

  const createSubj = useMutation({
    mutationFn: () => schoolApi.createSubjectAssignment({ teacherId: subjForm.teacherId, subjectId: subjForm.subjectId }),
    onSuccess: () => { setSubjForm({ teacherId: '', subjectId: '' }); qc.invalidateQueries({ queryKey: queryKeys.school.subjectAssignments(userId) }) },
  })
  const createClass = useMutation({
    mutationFn: () => schoolApi.createClassAssignment({ teacherId: classForm.teacherId, schoolClassId: classForm.schoolClassId, subjectId: classForm.subjectId || null, role: Number(classForm.role) }),
    onSuccess: () => { setClassForm({ teacherId: '', schoolClassId: '', subjectId: '', role: 0 }); qc.invalidateQueries({ queryKey: queryKeys.school.classAssignments(userId) }) },
  })
  const deactivateClass = useMutation({
    mutationFn: (id) => schoolApi.deactivateClassAssignment(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: queryKeys.school.classAssignments(userId) }),
  })
  const teacherItems = Array.isArray(teachers.data) ? teachers.data : []
  const subjectItems = Array.isArray(subjects.data) ? subjects.data : []
  const classItems = Array.isArray(classes.data) ? classes.data : []
  const teacherOpts = [{ value: '', label: t('school.common.choose') }, ...teacherItems.map((x) => ({ value: itemId(x), label: displayValue(x, ['fullName', 'FullName']) || itemId(x) }))]
  const subjectOpts = (withEmpty) => [...(withEmpty ? [{ value: '', label: t('school.common.none') }] : [{ value: '', label: t('school.common.choose') }]), ...subjectItems.map((x) => ({ value: itemId(x), label: displayValue(x, ['name', 'Name']) || itemId(x) }))]
  const classOpts = [{ value: '', label: t('school.common.choose') }, ...classItems.map((x) => ({ value: itemId(x), label: displayValue(x, ['name', 'Name']) || itemId(x) }))]
  return (
    <>
      <Head view="teacherAssignments" />
      <Card title={t('school.assignments.classCreate')}>
        {createClass.isSuccess && <Alert variant="success" title={t('school.common.created')}>{t('school.common.createdBody')}</Alert>}
        {createClass.isError && <ErrorState error={createClass.error} onRetry={() => createClass.reset()} />}
        <div className="ui-formgrid ui-formgrid--2">
          <SelectField label={t('school.common.teacher')} value={classForm.teacherId} onChange={(e) => setClassForm((f) => ({ ...f, teacherId: e.target.value }))} options={teacherOpts} />
          <SelectField label={t('school.common.class')} value={classForm.schoolClassId} onChange={(e) => setClassForm((f) => ({ ...f, schoolClassId: e.target.value }))} options={classOpts} />
          <SelectField label={t('school.common.subject')} value={classForm.subjectId} onChange={(e) => setClassForm((f) => ({ ...f, subjectId: e.target.value }))} options={subjectOpts(true)} />
          <SelectField label={t('school.common.role')} value={classForm.role} onChange={(e) => setClassForm((f) => ({ ...f, role: e.target.value }))} options={CLASS_ROLE.map((r, i) => ({ value: i, label: t(`school.classRole.${r}`) }))} />
        </div>
        <Button onClick={() => createClass.mutate()} loading={createClass.isPending} disabled={!classForm.teacherId || !classForm.schoolClassId}>{t('school.assignments.classCreate')}</Button>
      </Card>
      <section className="ui-section">
        <div className="ui-section__head"><h2 className="ui-section__title">{t('school.pages.teacherAssignments.classList')}</h2></div>
        {deactivateClass.isError && <ErrorState error={deactivateClass.error} onRetry={() => deactivateClass.reset()} />}
        <List
          query={classAsg}
          empty={t('school.empty.assignments')}
          locale={locale}
          rowActions={(item) => item.isActive && (
            <Button variant="secondary" onClick={() => deactivateClass.mutate(itemId(item))} loading={deactivateClass.isPending}>{t('school.common.deactivate')}</Button>
          )}
        />
      </section>
      <Card title={t('school.assignments.subjectCreate')}>
        {createSubj.isSuccess && <Alert variant="success" title={t('school.common.created')}>{t('school.common.createdBody')}</Alert>}
        {createSubj.isError && <ErrorState error={createSubj.error} onRetry={() => createSubj.reset()} />}
        <div className="ui-formgrid ui-formgrid--2">
          <SelectField label={t('school.common.teacher')} value={subjForm.teacherId} onChange={(e) => setSubjForm((f) => ({ ...f, teacherId: e.target.value }))} options={teacherOpts} />
          <SelectField label={t('school.common.subject')} value={subjForm.subjectId} onChange={(e) => setSubjForm((f) => ({ ...f, subjectId: e.target.value }))} options={subjectOpts(false)} />
        </div>
        <Button onClick={() => createSubj.mutate()} loading={createSubj.isPending} disabled={!subjForm.teacherId || !subjForm.subjectId}>{t('school.assignments.subjectCreate')}</Button>
      </Card>
      <section className="ui-section">
        <div className="ui-section__head"><h2 className="ui-section__title">{t('school.pages.teacherAssignments.subjectList')}</h2></div>
        <List query={subjectAsg} empty={t('school.empty.assignments')} locale={locale} />
      </section>
    </>
  )
}

export default function SchoolTeacherAssignmentsPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <TeacherAssignmentsPage userId={userId} locale={locale} {...props} />
}
