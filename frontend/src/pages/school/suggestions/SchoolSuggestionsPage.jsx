import { useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Button, FormModal } from '../../../shared/ui'
import { ErrorState } from '../../../shared/feedback'
import { Head, List } from '../../../features/school/components'
import { SUGGESTION_STATUS } from '../../../features/school/constants'
import { useSchoolQuery } from '../../../features/school/helpers'
import { schoolApi } from '../../../features/school/schoolApi'
import { getField, itemId } from '../../../features/student/studentUtils'
import { queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

// Numeric SuggestionStatus enum (the backend binds enums numerically, no
// JsonStringEnumConverter): Submitted=0, UnderReview=1, Accepted=2, Rejected=3,
// Implemented=4. NOTE: SuggestionDto never includes author identity — anonymous
// submission is intentional (Phase 5 §12) and this page never shows/joins one.
const STATUS_TONE = { 0: 'muted', 1: 'info', 2: 'success', 3: 'danger', 4: 'brand' }

function SuggestionsPage({ userId, locale }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const query = useSchoolQuery(queryKeys.school.suggestions(userId), (s) => schoolApi.suggestions(s))
  const [moderating, setModerating] = useState(null)
  const moderate = useMutation({
    mutationFn: ({ id, body }) => schoolApi.moderateSuggestion(id, body),
    onSuccess: () => {
      setModerating(null)
      qc.invalidateQueries({ queryKey: queryKeys.school.suggestions(userId) })
    },
  })

  const statusOptions = SUGGESTION_STATUS.map((s, i) => ({ value: i, label: t(`school.suggestions.status.${s}`) }))
  const columns = [
    { key: 'title', header: t('school.common.title') },
    { key: 'body', header: t('school.suggestions.body') },
    { key: 'status', header: t('school.common.status'), chip: (v) => ({ tone: STATUS_TONE[v] ?? 'muted', label: t(`school.suggestions.status.${SUGGESTION_STATUS[v] || 'Submitted'}`) }) },
    { key: 'submittedAt', header: t('school.suggestions.submittedAt'), format: 'date' },
  ]

  return (
    <>
      <Head view="suggestions" />
      <List
        query={query}
        columns={columns}
        empty={t('school.empty.suggestions')}
        locale={locale}
        rowActions={(item) => (
          <Button variant="secondary" onClick={() => setModerating(item)}>{t('school.suggestions.moderate')}</Button>
        )}
      />
      <FormModal
        open={Boolean(moderating)}
        onClose={() => setModerating(null)}
        title={t('school.suggestions.moderateTitle')}
        fields={moderating ? [
          { name: 'status', label: t('school.common.status'), type: 'select', options: statusOptions },
          { name: 'reviewNotes', label: t('school.suggestions.reviewNotes'), type: 'textarea' },
        ] : []}
        initialValues={moderating ? {
          status: getField(moderating, 'status') ?? 0,
          reviewNotes: getField(moderating, 'reviewNotes') || '',
        } : {}}
        onSubmit={(values) => moderate.mutate({
          id: itemId(moderating),
          body: { status: Number(values.status), reviewNotes: values.reviewNotes || null },
        })}
        submitting={moderate.isPending}
        error={moderate.error}
        submitLabel={t('school.common.save')}
      />
    </>
  )
}

export default function SchoolSuggestionsPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <SuggestionsPage userId={userId} locale={locale} {...props} />
}
