import { useState } from 'react'
import { useParams } from 'react-router-dom'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { FileText, Image as ImageIcon, MessageSquare, Music, PlayCircle, Presentation } from 'lucide-react'
import { useStudentContext } from '../../../features/student/helpers'
import { Thumb } from '../../../shared/domain'
import { FileDownloadButton } from '../../../shared/files'
import { TextareaField } from '../../../shared/form'
import { Button, Card, Chip, PageHeader } from '../../../shared/ui'
import { QueryBoundary, ErrorState, EmptyState } from '../../../shared/feedback'
import { useStudentQuery } from '../../../features/student/helpers'
import { Loading } from '../../../features/student/Loading'
import { studentApi } from '../../../features/student/studentApi'
import { filesApi } from '../../../features/files/filesApi'
import { attachmentTypeName, displayValue, formatDate, getField, itemId } from '../../../features/student/studentUtils'
import { queryKeys } from '../../../lib/query/keys'

// Honest per-type icon (replaces the previous hardcoded PlayCircle "video player" for every type).
const TYPE_ICON = { Video: PlayCircle, Document: FileText, Image: ImageIcon, Slides: Presentation, Audio: Music }

function MaterialsPage({ userId, locale }) {
  const { t } = useTranslation()
  const { materialId } = useParams()
  const material = useStudentQuery(queryKeys.student.material(userId, materialId), (signal) => studentApi.material(materialId, signal), { enabled: Boolean(materialId) })
  const comments = useStudentQuery(queryKeys.student.comments(userId, materialId), (signal) => studentApi.resourceComments(materialId, signal), { enabled: Boolean(materialId) })
  const qc = useQueryClient()
  const [body, setBody] = useState('')
  const mutation = useMutation({ mutationFn: () => studentApi.addResourceComment(materialId, body), onSuccess: () => { setBody(''); qc.invalidateQueries({ queryKey: queryKeys.student.comments(userId, materialId) }) } })

  const typeName = material.data ? attachmentTypeName(getField(material.data, 'type')) : ''
  const materialIcon = TYPE_ICON[typeName] || FileText
  const fileRecordId = material.data ? getField(material.data, 'fileRecordId') : null
  const materialUrl = material.data ? getField(material.data, 'url') : null
  const title = material.data ? displayValue(material.data) : ''

  return (
    <>
      <PageHeader title={t('student.materials.details')} description={t('student.materials.description')} />
      <div className="ui-split">
        <div>
          {material.isLoading && <Loading />}
          {material.isError && <ErrorState error={material.error} onRetry={material.refetch} />}
          {!material.isLoading && !material.isError && !material.data && (
            <EmptyState title={t('student.materials.notFound')} message={t('student.materials.notFoundBody')} icon={FileText} />
          )}
          {material.data && (
            <Card>
              <Thumb seed={materialId} icon={materialIcon} height={220} className="student-lesson__hero" />
              <h2 className="[margin:14px_0_6px] text-lg font-bold text-ink">{title}</h2>
              {typeName && <Chip tone="info">{typeName}</Chip>}
              <div className="cluster" style={{ marginTop: '12px' }}>
                {fileRecordId ? (
                  <FileDownloadButton download={() => filesApi.download(fileRecordId, title)} />
                ) : materialUrl ? (
                  <a href={materialUrl} target="_blank" rel="noreferrer" className="ui-btn ui-btn--secondary">{t('student.materials.openLink')}</a>
                ) : null}
              </div>
            </Card>
          )}
        </div>
        <Card title={t('student.comments.title')}>
          <form className="stack" onSubmit={(event) => { event.preventDefault(); if (body.trim()) mutation.mutate() }}>
            <TextareaField label={t('student.comments.add')} value={body} onChange={(e) => setBody(e.target.value)} maxLength={1000} />
            <Button type="submit" loading={mutation.isPending} disabled={!body.trim()}>{t('actions.submit')}</Button>
          </form>
          {mutation.isError && <ErrorState error={mutation.error} />}
          <QueryBoundary query={comments} loadingFallback={<Loading />} emptyWhen={(d) => !d?.length} emptyTitle={t('student.empty.comments')} emptyIcon={MessageSquare}>
            {(items) => (
              <ul className="ui-list">
                {items.map((item) => (
                  <li className="ui-list__item" key={itemId(item)}>
                    <div className="ui-list__body">
                      <div className="ui-list__title">{displayValue(item, ['authorName', 'AuthorName', 'author', 'Author']) || t('student.comments.title')}</div>
                      <div className="ui-list__meta">{displayValue(item, ['body', 'Body', 'content', 'Content', 'text', 'Text'])}</div>
                      <div className="ui-list__meta ui-muted">{formatDate(getField(item, 'createdAt'), locale)}</div>
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </QueryBoundary>
        </Card>
      </div>
    </>
  )
}

// =============================================================================
// Homework
// =============================================================================

export default function StudentMaterialsPage(props) {
  const { userId, locale } = useStudentContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <MaterialsPage userId={userId} locale={locale} {...props} />
}
