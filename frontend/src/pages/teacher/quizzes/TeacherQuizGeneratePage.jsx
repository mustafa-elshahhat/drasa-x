import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { SelectField, TextField } from '../../../components/form/fields'
import { Alert } from '../../../components/ui/Alert'
import { Button } from '../../../components/ui/Button'
import { Card, PageHeader } from '../../../components/ui/PageHeader'
import { ErrorState } from '../../../components/ui/states'
import { useTeacherQuery } from '../../../features/teacher/helpers'
import { teacherApi } from '../../../features/teacher/teacherApi'
import { displayValue, itemId } from '../../../features/teacher/teacherUtils'
import { STALE, queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function QuizGeneratePage({ userId }) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const qc = useQueryClient()
  const subjects = useTeacherQuery(queryKeys.teacher.subjects(userId), (signal) => teacherApi.subjects(signal), { staleTime: STALE.medium })
  const [subjectId, setSubjectId] = useState('')
  const [topic, setTopic] = useState('')
  const [numQuestions, setNumQuestions] = useState('3')
  const [difficulty, setDifficulty] = useState('core')
  const generate = useMutation({
    mutationFn: () => teacherApi.generateDraft({
      subjectId,
      topic: topic || undefined,
      numQuestions: Number(numQuestions) || 3,
      difficulty,
      questionTypes: ['mcq'],
      language: 'en',
    }),
    onSuccess: (result) => {
      qc.invalidateQueries({ queryKey: queryKeys.teacher.quizzes(userId) })
      qc.invalidateQueries({ queryKey: queryKeys.teacher.dashboard(userId) })
      const newId = result?.quizId || result?.QuizId
      if (newId) navigate(`/app/teacher/quizzes/${newId}`)
    },
  })
  const subjectItems = subjects.data || []
  return (
    <>
      <PageHeader title={t('teacher.generate.title')} description={t('teacher.generate.description')} />
      <Alert title={t('teacher.generate.draftOnlyTitle')}>{t('teacher.generate.draftOnlyBody')}</Alert>
      <Card title={t('teacher.generate.form')}>
        <form className="stack" onSubmit={(e) => { e.preventDefault(); if (subjectId) generate.mutate() }}>
          <div className="ui-formgrid ui-formgrid--2">
            <SelectField
              label={t('teacher.generate.subject')}
              value={subjectId}
              onChange={(e) => setSubjectId(e.target.value)}
              options={[{ value: '', label: t('teacher.generate.chooseSubject') }, ...subjectItems.map((s) => ({ value: itemId(s, ['subjectId', 'SubjectId', 'id', 'Id']), label: displayValue(s, ['name', 'Name']) }))]}
            />
            <TextField label={t('teacher.generate.topic')} value={topic} onChange={(e) => setTopic(e.target.value)} maxLength={200} />
            <TextField label={t('teacher.generate.numQuestions')} type="number" value={numQuestions} onChange={(e) => setNumQuestions(e.target.value)} />
            <SelectField
              label={t('teacher.generate.difficulty')}
              value={difficulty}
              onChange={(e) => setDifficulty(e.target.value)}
              options={[{ value: 'remedial', label: 'remedial' }, { value: 'core', label: 'core' }, { value: 'advanced', label: 'advanced' }]}
            />
          </div>
          <Button type="submit" loading={generate.isPending} disabled={!subjectId}>{t('teacher.generate.submit')}</Button>
        </form>
        {generate.isError && <ErrorState error={generate.error} />}
      </Card>
    </>
  )
}

// ---------------------------------------------------------------------------
// Submission detail — grade + feedback
// ---------------------------------------------------------------------------

export default function TeacherQuizGeneratePage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <QuizGeneratePage userId={userId} locale={locale} {...props} />
}
