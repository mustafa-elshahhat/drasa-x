import { useState } from 'react'
import { useParams } from 'react-router-dom'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { BookOpen, Layers } from 'lucide-react'
import { QuizCard, UnitCard } from '../../../shared/domain'
import { SelectField, TextField, TextareaField } from '../../../shared/form'
import { Alert, Button, Card, PageHeader } from '../../../shared/ui'
import { ErrorState } from '../../../shared/feedback'
import { Listing } from '../../../features/teacher/components'
import { useTeacherQuery } from '../../../features/teacher/helpers'
import { teacherApi } from '../../../features/teacher/teacherApi'
import { displayValue, itemId } from '../../../features/teacher/teacherUtils'
import { STALE, queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function SubjectsPage({ userId }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const { subjectId } = useParams()
  const query = useTeacherQuery(queryKeys.teacher.subjects(userId), (signal) => teacherApi.subjects(signal), { staleTime: STALE.medium })
  const grades = useTeacherQuery(queryKeys.teacher.grades(userId), (signal) => teacherApi.grades(signal), { staleTime: STALE.long })
  const [form, setForm] = useState({ name: '', description: '', gradeId: '' })
  const create = useMutation({
    mutationFn: () => teacherApi.addSubject({ name: form.name, description: form.description || undefined, gradeId: form.gradeId }),
    onSuccess: () => { setForm({ name: '', description: '', gradeId: '' }); qc.invalidateQueries({ queryKey: queryKeys.teacher.subjects(userId) }) },
  })
  const gradeItems = Array.isArray(grades.data) ? grades.data : []
  if (subjectId) return <CurriculumUnitsPage userId={userId} subjectId={subjectId} />
  return (
    <>
      <PageHeader title={t('teacher.subjects.title')} description={t('teacher.subjects.description')} />
      <Card title={t('teacher.curriculum.addSubject')}>
        {create.isSuccess && <Alert variant="success" title={t('teacher.curriculum.subjectCreated')} />}
        {create.isError && <ErrorState error={create.error} onRetry={() => create.reset()} />}
        <div className="ui-formgrid ui-formgrid--2">
          <TextField label={t('teacher.curriculum.subjectName')} value={form.name} onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))} />
          <SelectField
            label={t('teacher.curriculum.subjectGrade')}
            value={form.gradeId}
            onChange={(e) => setForm((f) => ({ ...f, gradeId: e.target.value }))}
            options={[{ value: '', label: t('teacher.curriculum.chooseGrade') }, ...gradeItems.map((g) => ({ value: itemId(g), label: displayValue(g) || itemId(g) }))]}
          />
        </div>
        <TextareaField label={t('teacher.curriculum.subjectDescription')} value={form.description} onChange={(e) => setForm((f) => ({ ...f, description: e.target.value }))} />
        <Button onClick={() => create.mutate()} loading={create.isPending} disabled={!form.name.trim() || !form.gradeId}>{t('teacher.curriculum.addSubject')}</Button>
      </Card>
      <Listing query={query} empty={t('teacher.empty.subjects')} emptyIcon={BookOpen}>
        {(items) => (
          <div className="ui-grid ui-grid--auto">
            {items.map((item) => (
              <QuizCard key={itemId(item, ['subjectId', 'SubjectId', 'id', 'Id'])} to={`/app/teacher/subjects/${itemId(item, ['subjectId', 'SubjectId', 'id', 'Id'])}`} icon={BookOpen} title={displayValue(item, ['name', 'Name'])} />
            ))}
          </div>
        )}
      </Listing>
    </>
  )
}

function CurriculumUnitsPage({ userId, subjectId }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const units = useTeacherQuery(queryKeys.teacher.units(userId, subjectId), (signal) => teacherApi.subjectUnits(subjectId, signal), { enabled: Boolean(subjectId), staleTime: STALE.medium })
  const [title, setTitle] = useState('')
  const create = useMutation({
    mutationFn: () => teacherApi.addUnit({ title, subjectId }),
    onSuccess: () => { setTitle(''); qc.invalidateQueries({ queryKey: queryKeys.teacher.units(userId, subjectId) }) },
  })
  return (
    <>
      <PageHeader title={t('teacher.curriculum.units')} description={t('teacher.curriculum.description')} />
      <Card title={t('teacher.curriculum.addUnit')}>
        {create.isSuccess && <Alert variant="success" title={t('teacher.curriculum.unitCreated')} />}
        {create.isError && <ErrorState error={create.error} onRetry={() => create.reset()} />}
        <TextField label={t('teacher.curriculum.unitTitleField')} value={title} onChange={(e) => setTitle(e.target.value)} />
        <Button onClick={() => create.mutate()} loading={create.isPending} disabled={!title.trim()}>{t('teacher.curriculum.addUnit')}</Button>
      </Card>
      <Listing query={units} empty={t('teacher.empty.units')} emptyIcon={Layers}>
        {(items) => (
          <div className="student-list">
            {items.map((item) => <UnitCard key={itemId(item)} to={`/app/teacher/units/${itemId(item)}`} icon={Layers} title={displayValue(item)} />)}
          </div>
        )}
      </Listing>
    </>
  )
}

export default function TeacherSubjectsPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <SubjectsPage userId={userId} locale={locale} {...props} />
}
