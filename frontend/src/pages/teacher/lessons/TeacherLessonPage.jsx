import { useState } from 'react'
import { useParams } from 'react-router-dom'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { FileText } from 'lucide-react'
import { DetailList } from '../../../shared/data-display'
import { FileDownloadButton, FileUpload } from '../../../shared/files'
import { SelectField, TextField } from '../../../shared/form'
import { Alert, Button, Card, PageHeader } from '../../../shared/ui'
import { ErrorState } from '../../../shared/feedback'
import { Listing } from '../../../features/teacher/components'
import { useTeacherQuery } from '../../../features/teacher/helpers'
import { teacherApi } from '../../../features/teacher/teacherApi'
import { filesApi } from '../../../features/files/filesApi'
import { attachmentTypeName, displayValue, getField, itemId } from '../../../features/teacher/teacherUtils'
import { STALE, queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

// AttachmentType enum values (backend Domain.Enums.AttachmentType).
const MATERIAL_TYPES = [1, 2, 3, 4, 5]

function LessonDetailPage({ userId, locale }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const { lessonId } = useParams()
  const materials = useTeacherQuery(queryKeys.teacher.materials(userId, lessonId), (signal) => teacherApi.lessonMaterials(lessonId, signal), { enabled: Boolean(lessonId), staleTime: STALE.medium })
  const invalidateMaterials = () => qc.invalidateQueries({ queryKey: queryKeys.teacher.materials(userId, lessonId) })

  // One shared title/type pair feeds both actions below: pick a file to upload it as a durable
  // attachment, or fill the URL field to add a legacy external link instead. Kept as a single form
  // (rather than two parallel ones) so there is exactly one "Material title"/"Type" field pair.
  const [meta, setMeta] = useState({ title: '', type: 2, url: '' })

  const addLink = useMutation({
    mutationFn: () => teacherApi.addMaterial({ lessonId, title: meta.title, url: meta.url, type: Number(meta.type) }),
    onSuccess: () => { setMeta({ title: '', type: 2, url: '' }); invalidateMaterials() },
  })
  const removeMaterial = useMutation({
    mutationFn: (id) => teacherApi.deleteMaterial(id),
    onSuccess: invalidateMaterials,
  })

  const typeOptions = MATERIAL_TYPES.map((value) => ({ value, label: t(`teacher.curriculum.types.${attachmentTypeName(value)}`) }))

  return (
    <>
      <PageHeader title={t('teacher.curriculum.lessonDetails')} description={t('teacher.curriculum.lessonDescription')} />

      <Card title={t('teacher.curriculum.addMaterial')}>
        <div className="ui-formgrid ui-formgrid--2">
          <TextField label={t('teacher.curriculum.materialTitleField')} value={meta.title} onChange={(e) => setMeta((f) => ({ ...f, title: e.target.value }))} />
          <SelectField label={t('teacher.curriculum.materialType')} value={meta.type} onChange={(e) => setMeta((f) => ({ ...f, type: e.target.value }))} options={typeOptions} />
        </div>
        <FileUpload
          label={t('files.lessonMaterialTitle')}
          hint={t('files.lessonMaterialHint')}
          upload={(file) => teacherApi.uploadMaterial({ lessonId, file, title: meta.title.trim() || file.name, type: Number(meta.type) })}
          onUploaded={() => { setMeta((f) => ({ ...f, title: '' })); invalidateMaterials() }}
        />

        <div className="h-[1px] bg-line [margin:16px_0]" />

        <TextField label={t('teacher.curriculum.materialUrl')} value={meta.url} onChange={(e) => setMeta((f) => ({ ...f, url: e.target.value }))} />
        {addLink.isSuccess && <Alert variant="success" title={t('teacher.curriculum.materialAdded')} />}
        {addLink.isError && <ErrorState error={addLink.error} onRetry={() => addLink.reset()} />}
        <Button variant="secondary" onClick={() => addLink.mutate()} loading={addLink.isPending} disabled={!meta.title.trim() || !meta.url.trim()}>{t('teacher.curriculum.addMaterialLink')}</Button>
      </Card>

      <Card title={t('teacher.curriculum.materials')}>
        <Listing query={materials} empty={t('teacher.empty.materials')} emptyIcon={FileText}>
          {(items) => (
            <div className="student-list">
              {items.map((item) => {
                const id = itemId(item)
                const fileRecordId = getField(item, 'fileRecordId')
                const url = getField(item, 'url')
                const title = displayValue(item)
                return (
                  <div className="student-list__item" key={id}>
                    <strong className="domain-row__title">{title}</strong>
                    <DetailList item={item} locale={locale} />
                    <div className="cluster" style={{ marginTop: '8px' }}>
                      {fileRecordId ? (
                        <FileDownloadButton download={() => filesApi.download(fileRecordId, title)} />
                      ) : url ? (
                        <a href={url} target="_blank" rel="noreferrer" className="ui-btn ui-btn--secondary">{t('teacher.curriculum.openLink')}</a>
                      ) : null}
                      <Button variant="secondary" onClick={() => removeMaterial.mutate(id)} loading={removeMaterial.isPending}>{t('teacher.curriculum.deleteMaterial')}</Button>
                    </div>
                  </div>
                )
              })}
            </div>
          )}
        </Listing>
      </Card>
    </>
  )
}

// ---------------------------------------------------------------------------
// Students (gradebook) + student detail
// ---------------------------------------------------------------------------

export default function TeacherLessonPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <LessonDetailPage userId={userId} locale={locale} {...props} />
}
