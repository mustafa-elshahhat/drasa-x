import { useState } from 'react'
import { useParams } from 'react-router-dom'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { PlayCircle } from 'lucide-react'
import { UnitCard } from '../../../shared/domain'
import { TextField, TextareaField } from '../../../shared/form'
import { Alert, Button, Card, PageHeader } from '../../../shared/ui'
import { ErrorState } from '../../../shared/feedback'
import { Listing } from '../../../features/teacher/components'
import { useTeacherQuery } from '../../../features/teacher/helpers'
import { teacherApi } from '../../../features/teacher/teacherApi'
import { displayValue, itemId } from '../../../features/teacher/teacherUtils'
import { STALE, queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function UnitLessonsPage({ userId }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const { unitId } = useParams()
  const lessons = useTeacherQuery(queryKeys.teacher.lessons(userId, unitId), (signal) => teacherApi.unitLessons(unitId, signal), { enabled: Boolean(unitId), staleTime: STALE.medium })
  const [form, setForm] = useState({ title: '', content: '' })
  const create = useMutation({
    mutationFn: () => teacherApi.addLesson({ title: form.title, content: form.content, unitId }),
    onSuccess: () => { setForm({ title: '', content: '' }); qc.invalidateQueries({ queryKey: queryKeys.teacher.lessons(userId, unitId) }) },
  })
  return (
    <>
      <PageHeader title={t('teacher.curriculum.lessons')} description={t('teacher.curriculum.description')} />
      <Card title={t('teacher.curriculum.addLesson')}>
        {create.isSuccess && <Alert variant="success" title={t('teacher.curriculum.lessonCreated')} />}
        {create.isError && <ErrorState error={create.error} onRetry={() => create.reset()} />}
        <TextField label={t('teacher.curriculum.lessonTitleField')} value={form.title} onChange={(e) => setForm((f) => ({ ...f, title: e.target.value }))} />
        <TextareaField label={t('teacher.curriculum.lessonContentField')} value={form.content} onChange={(e) => setForm((f) => ({ ...f, content: e.target.value }))} />
        <Button onClick={() => create.mutate()} loading={create.isPending} disabled={!form.title.trim() || !form.content.trim()}>{t('teacher.curriculum.addLesson')}</Button>
      </Card>
      <Listing query={lessons} empty={t('teacher.empty.lessons')} emptyIcon={PlayCircle}>
        {(items) => (
          <div className="student-list">
            {items.map((item) => <UnitCard key={itemId(item)} to={`/app/teacher/lessons/${itemId(item)}`} icon={PlayCircle} title={displayValue(item)} />)}
          </div>
        )}
      </Listing>
    </>
  )
}

export default function TeacherUnitsPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <UnitLessonsPage userId={userId} locale={locale} {...props} />
}
