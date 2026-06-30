import { useState } from 'react'
import { useMutation } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { useStudentContext } from '../../../features/student/helpers'
import { TextField, TextareaField } from '../../../components/form/fields'
import { Alert } from '../../../components/ui/Alert'
import { Button } from '../../../components/ui/Button'
import { Card, PageHeader } from '../../../components/ui/PageHeader'
import { ErrorState } from '../../../components/ui/states'
import { studentApi } from '../../../features/student/studentApi'

function SuggestionsPage() {
  const { t } = useTranslation()
  const [title, setTitle] = useState('')
  const [body, setBody] = useState('')
  const mutation = useMutation({ mutationFn: () => studentApi.submitSuggestion(title, body), onSuccess: () => { setTitle(''); setBody('') } })
  return (
    <>
      <PageHeader title={t('student.suggestions.title')} description={t('student.suggestions.description')} />
      <Alert title={t('student.suggestions.privacyTitle')}>{t('student.suggestions.privacyBody')}</Alert>
      <Card>
        <form className="stack" onSubmit={(event) => { event.preventDefault(); if (title.trim() && body.trim()) mutation.mutate() }}>
          <TextField label={t('student.suggestions.subject')} value={title} onChange={(e) => setTitle(e.target.value)} maxLength={120} required />
          <TextareaField label={t('student.suggestions.body')} value={body} onChange={(e) => setBody(e.target.value)} maxLength={2000} required />
          <Button type="submit" loading={mutation.isPending} disabled={!title.trim() || !body.trim()}>{t('actions.submit')}</Button>
        </form>
      </Card>
      {mutation.isSuccess && <Alert variant="success" title={t('student.suggestions.sent')} />}
      {mutation.isError && <ErrorState error={mutation.error} />}
    </>
  )
}

// =============================================================================
// Badges / points / streaks
// =============================================================================

export default function StudentSuggestionsPage(props) {
  const { userId, locale } = useStudentContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <SuggestionsPage userId={userId} locale={locale} {...props} />
}
