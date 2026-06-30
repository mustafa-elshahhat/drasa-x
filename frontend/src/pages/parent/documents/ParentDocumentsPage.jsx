import { useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { FileText } from 'lucide-react'
import { DetailList } from '../../../components/data/DetailList'
import { FileDownloadButton } from '../../../components/files/FileDownloadButton'
import { FileUpload } from '../../../components/files/FileUpload'
import { SelectField, TextField, TextareaField } from '../../../components/form/fields'
import { Alert } from '../../../components/ui/Alert'
import { Button } from '../../../components/ui/Button'
import { Card, PageHeader } from '../../../components/ui/PageHeader'
import { ErrorState } from '../../../components/ui/states'
import { Listing } from '../../../features/parent/components'
import { useParentQuery } from '../../../features/parent/helpers'
import { parentApi } from '../../../features/parent/parentApi'
import { displayValue, itemId } from '../../../features/student/studentUtils'
import { STALE, queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

const REQUEST_TYPES = ['Document', 'Meeting', 'ProgressFollowUp', 'TeacherContact', 'Other']
const REQUEST_TYPE_VALUE = { Document: 0, Meeting: 1, ProgressFollowUp: 2, TeacherContact: 3, Other: 4 }

// A document-request row with a durable attachment workflow embedded.
function DocumentRequestRow({ item, userId, locale }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const id = itemId(item, ['id', 'Id'])
  const hasDoc = Boolean(item.fileRecordId || item.FileRecordId || item.requestDocumentId || item.hasRequestDocument)
  return (
    <Card>
      <DetailList item={item} locale={locale} />
      <FileUpload
        label={t('files.parentDocTitle')}
        hint={t('files.parentDocHint')}
        accept=".pdf,.doc,.docx,.png,.jpg,.jpeg"
        upload={(file) => parentApi.attachRequestDocument(id, file)}
        onUploaded={() => qc.invalidateQueries({ queryKey: queryKeys.parent.documentRequests(userId) })}
      />
      {hasDoc && <FileDownloadButton download={() => parentApi.downloadRequestDocument(id)} />}
    </Card>
  )
}

function DocumentsPage({ userId, locale }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const children = useParentQuery(queryKeys.parent.children(userId), (signal) => parentApi.children(signal), { staleTime: STALE.medium })
  const requests = useParentQuery(queryKeys.parent.documentRequests(userId), (signal) => parentApi.documentRequests(signal))
  const [studentId, setStudentId] = useState('')
  const [type, setType] = useState('Document')
  const [title, setTitle] = useState('')
  const [body, setBody] = useState('')

  const create = useMutation({
    mutationFn: () => parentApi.createDocumentRequest({ studentId, type: REQUEST_TYPE_VALUE[type] ?? 0, title, body }),
    onSuccess: () => {
      setTitle(''); setBody('')
      qc.invalidateQueries({ queryKey: queryKeys.parent.documentRequests(userId) })
    },
  })

  const childItems = Array.isArray(children.data) ? children.data : []
  const canSubmit = studentId && title.trim() && body.trim()

  return (
    <>
      <PageHeader title={t('parent.documents.title')} description={t('parent.documents.description')} />
      <Card title={t('parent.documents.new')}>
        {create.isSuccess && <Alert variant="success" title={t('parent.documents.submittedTitle')}>{t('parent.documents.submittedBody')}</Alert>}
        {create.isError && <ErrorState error={create.error} onRetry={() => create.reset()} />}
        <div className="ui-formgrid ui-formgrid--2">
          <SelectField
            label={t('parent.documents.child')}
            value={studentId}
            onChange={(e) => setStudentId(e.target.value)}
            options={[{ value: '', label: t('parent.documents.chooseChild') }, ...childItems.map((c) => ({ value: itemId(c, ['studentId', 'StudentId', 'id', 'Id']), label: displayValue(c, ['fullName', 'FullName']) || itemId(c) }))]}
          />
          <SelectField
            label={t('parent.documents.type')}
            value={type}
            onChange={(e) => setType(e.target.value)}
            options={REQUEST_TYPES.map((tp) => ({ value: tp, label: t(`parent.documents.types.${tp}`) }))}
          />
          <TextField label={t('parent.documents.subject')} value={title} onChange={(e) => setTitle(e.target.value)} />
        </div>
        <TextareaField label={t('parent.documents.message')} value={body} onChange={(e) => setBody(e.target.value)} />
        <Button onClick={() => create.mutate()} loading={create.isPending} disabled={!canSubmit}>{t('parent.documents.submit')}</Button>
      </Card>
      <section className="ui-section">
        <div className="ui-section__head"><h2 className="ui-section__title">{t('parent.documents.mine')}</h2></div>
        <Listing query={requests} empty={t('parent.empty.documents')} emptyIcon={FileText}>
          {(items) => (
            <div className="student-list">
              {items.map((item) => <DocumentRequestRow key={itemId(item, ['id', 'Id'])} item={item} userId={userId} locale={locale} />)}
            </div>
          )}
        </Listing>
      </section>
    </>
  )
}

// ---------------------------------------------------------------------------
// Conversations / notifications / announcements / settings
// ---------------------------------------------------------------------------

export default function ParentDocumentsPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <DocumentsPage userId={userId} locale={locale} {...props} />
}
