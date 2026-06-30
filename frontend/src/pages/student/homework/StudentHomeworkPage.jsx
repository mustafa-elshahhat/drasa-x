import { useRef, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Check, ClipboardCheck, Clock, Download, FileText } from 'lucide-react'
import { useStudentContext } from '../../../features/student/helpers'
import { DetailList } from '../../../shared/data-display'
import { TextareaField } from '../../../shared/form'
import { Button, Chip, Card } from '../../../shared/ui'
import { QueryBoundary, EmptyState, ErrorState } from '../../../shared/feedback'
import { useStudentQuery } from '../../../features/student/helpers'
import { Loading } from '../../../features/student/Loading'
import { studentApi } from '../../../features/student/studentApi'
import { filesApi } from '../../../features/files/filesApi'
import { displayValue, formatDate, getField, itemId } from '../../../features/student/studentUtils'
import { getSubjectTheme } from '../../../features/student/theme'
import { queryKeys } from '../../../lib/query/keys'

// The backend AssignedHomeworkDto exposes `submissionStatus` (SubmissionStatus
// enum) + `hasSubmitted`, NOT a `status` string. Derive the UI bucket from those
// real fields so submitted/graded homework lands in the right tab and pill.
function homeworkStatus(item) {
  const raw = String(getField(item, 'submissionStatus') ?? '').toLowerCase()
  if (raw === 'graded' || raw === '2') return 'graded'
  if (getField(item, 'hasSubmitted') || ['submitted', '1', 'late', '3'].includes(raw)) return 'submitted'
  return 'pending'
}

// Max points come from the assignment's MaxScore. Returns null when the backend did
// not set one — the UI must not invent a number.
function homeworkPoints(item) {
  const v = getField(item, 'maxScore')
  return v == null ? null : v
}

function HomeworkPage({ userId, locale }) {
  const { t } = useTranslation()
  const { homeworkId } = useParams()
  const list = useStudentQuery(queryKeys.student.homework(userId), (signal) => studentApi.homework(signal))
  const [activeTab, setActiveTab] = useState('all')

  if (homeworkId) return <HomeworkDetails userId={userId} homeworkId={homeworkId} list={list} locale={locale} />

  const tabs = [
    { key: 'all', label: t('student.homework.tabs.all', 'All') },
    { key: 'pending', label: t('student.homework.tabs.pending', 'Pending') },
    { key: 'submitted', label: t('student.homework.tabs.submitted', 'Submitted') },
  ]

  return (
    <>
      <div className="student-dashboard__welcome-header" style={{ marginBottom: '20px' }}>
        <h1 className="student-dashboard__welcome-title" style={{ fontSize: '28px', fontWeight: 800, margin: '0 0 6px' }}>
          {t('student.homework.title', 'Homework')}
        </h1>
        <p className="student-dashboard__welcome-subtitle" style={{ color: 'var(--text-dim)', margin: 0 }}>
          {t('student.homework.description', 'Track and submit your assignments')}
        </p>
      </div>

      {/* Tabs list */}
      <div style={{ display: 'flex', gap: '6px', marginBottom: '20px', borderBottom: '1px solid var(--border)', overflowX: 'auto' }}>
        {tabs.map((tab) => {
          const active = activeTab === tab.key
          return (
            <button
              key={tab.key}
              onClick={() => setActiveTab(tab.key)}
              style={{
                background: 'none',
                border: 'none',
                borderBottom: `2px solid ${active ? 'var(--brand)' : 'transparent'}`,
                color: active ? 'var(--brand)' : 'var(--text-dim)',
                fontWeight: active ? 700 : 500,
                padding: '10px 14px',
                cursor: 'pointer',
                fontSize: '14px',
                whiteSpace: 'nowrap',
                marginBottom: '-1px',
                transition: 'all 0.12s ease',
              }}
            >
              {tab.label}
            </button>
          )
        })}
      </div>

      <QueryBoundary query={list} loadingFallback={<Loading />} emptyWhen={(d) => !d?.length} emptyTitle={t('student.empty.homework')} emptyIcon={ClipboardCheck}>
        {(items) => {
          const filteredItems = items.filter((item) => {
            const status = homeworkStatus(item)
            if (activeTab === 'pending') return status === 'pending'
            if (activeTab === 'submitted') return status === 'submitted' || status === 'graded'
            return true
          })

          if (filteredItems.length === 0) {
            return <EmptyState icon={ClipboardCheck} title={t('student.empty.homework', 'Nothing here')} message={t('student.homework.emptyCategory', 'You have no homework in this category.')} />
          }

          return (
            <div className="flex flex-col gap-3">
              {filteredItems.map((item) => {
                const theme = getSubjectTheme(item)
                const status = homeworkStatus(item)
                const points = homeworkPoints(item)
                const score = getField(item, 'score') || getField(item, 'earnedPoints')
                const isPending = status === 'pending'

                let statusPillText = t('student.homework.status.pending', 'Pending')
                let statusPillTone = 'warning'
                if (status === 'graded') {
                  statusPillText = score != null && points != null
                    ? `${t('student.homework.status.graded', 'Graded')} ${score}/${points}`
                    : t('student.homework.status.graded', 'Graded')
                  statusPillTone = 'success'
                } else if (status === 'submitted') {
                  statusPillText = t('student.homework.status.submitted', 'Submitted')
                  statusPillTone = 'info'
                }

                return (
                  <div key={itemId(item)} className="student-homework-row">
                    <div
                      className="student-homework-row__icon-tile"
                      style={{ backgroundColor: `${theme.color}18`, color: theme.color }}
                    >
                      <FileText size={22} style={{ color: theme.color }} />
                    </div>

                    <div className="student-homework-row__content">
                      <div className="student-homework-row__title">{displayValue(item)}</div>
                      <div className="student-homework-row__meta">
                        <span className="student-homework-row__meta-item">
                          <Clock size={13} style={{ color: isPending ? 'var(--orange)' : 'var(--text-dim)' }} />
                          {formatDate(getField(item, 'dueDate') || getField(item, 'dueAt'), locale)}
                        </span>
                        {points != null && (
                          <span>
                            {points} {t('student.homework.pointsText', 'points')}
                          </span>
                        )}
                      </div>
                    </div>

                    <div className="flex items-center gap-3 flex-wrap">
                      <Chip tone={statusPillTone}>{statusPillText}</Chip>
                      <Link
                        to={`/app/student/homework/${itemId(item)}`}
                        className={`ui-btn ${isPending ? 'ui-btn--primary' : 'ui-btn--secondary'}`}
                        style={{ padding: '8px 16px', fontSize: '13px', textDecoration: 'none' }}
                      >
                        {isPending ? t('actions.open', 'Open') : t('actions.view', 'View')}
                      </Link>
                    </div>
                  </div>
                )
              })}
            </div>
          )
        }}
      </QueryBoundary>
    </>
  )
}

function HomeworkDetails({ userId, homeworkId, list, locale }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const [content, setContent] = useState('')
  const [file, setFile] = useState(null)
  const fileInputRef = useRef(null)

  const isAr = locale === 'ar'

  const submission = useStudentQuery(
    queryKeys.student.homeworkSubmission(userId, homeworkId),
    (signal) => studentApi.homeworkSubmission(homeworkId, signal)
  )

  // Real submission flow: if a file is selected, upload it via the files API first, then
  // submit the homework with the returned AttachmentFileId (never inject the filename into text).
  const mutation = useMutation({
    mutationFn: async () => {
      let attachmentFileId
      if (file) {
        const uploaded = await filesApi.upload({ file, purpose: 'SubmissionAttachment', relatedEntityType: 'Assignment', relatedEntityId: homeworkId })
        attachmentFileId = getField(uploaded, 'id') ?? getField(uploaded, 'fileId')
      }
      return studentApi.submitHomework(homeworkId, { content: content.trim() || null, attachmentFileId })
    },
    onSuccess: () => {
      setContent('')
      setFile(null)
      qc.invalidateQueries({ queryKey: queryKeys.student.homework(userId) })
      qc.invalidateQueries({ queryKey: queryKeys.student.homeworkSubmission(userId, homeworkId) })
    }
  })

  const item = list.data?.find((h) => itemId(h) === homeworkId)
  const status = item ? homeworkStatus(item) : 'pending'
  const points = item ? homeworkPoints(item) : null
  const desc = item ? (getField(item, 'description') || getField(item, 'desc') || '') : ''

  const subData = submission.data
  const hasSubmitted = Boolean(subData || mutation.isSuccess || status === 'submitted' || status === 'graded')
  const isGraded = status === 'graded'

  const handleUploadClick = () => {
    if (fileInputRef.current) {
      fileInputRef.current.click()
    }
  }

  const handleFileChange = (e) => {
    if (e.target.files && e.target.files.length > 0) {
      setFile(e.target.files[0])
    }
  }

  const handleSubmit = (event) => {
    event.preventDefault()
    if (content.trim() || file) {
      mutation.mutate()
    }
  }

  return (
    <>
      {/* Breadcrumb */}
      <div className="flex items-center gap-2 mb-[22px] text-sm text-muted">
        <Link to="/app/student/homework" style={{ textDecoration: 'none', color: 'var(--text-dim)' }}>
          {t('student.homework.title', 'Homework')}
        </Link>
        <span className="text-faint">{isAr ? '‹' : '›'}</span>
        <span className="font-semibold text-ink">{displayValue(item) || 'Homework Detail'}</span>
      </div>

      {list.isError && <ErrorState error={list.error} onRetry={list.refetch} />}

      <div className="ui-split">
        {/* Left Column: Main Homework Details & Submission Card */}
        <Card>
          <div className="flex justify-between items-start flex-wrap gap-2.5 mb-3.5">
            <h1 className="m-0 text-2xl font-extrabold text-ink">
              {displayValue(item)}
            </h1>
            {points != null && <Chip tone="purple">{points} {t('student.homework.pointsText', 'points')}</Chip>}
          </div>

          {(getField(item, 'dueDate') || getField(item, 'dueAt')) && (
            <div className="flex gap-2.5 mb-[18px]">
              <Chip tone="warning">
                {t('student.homework.due', 'Due')}: {formatDate(getField(item, 'dueDate') || getField(item, 'dueAt'), locale)}
              </Chip>
            </div>
          )}

          {desc && (
            <p className="text-muted leading-[1.7] text-[15px]">
              {desc}
            </p>
          )}

          <div className="h-[1px] bg-line [margin:18px_0]" />

          {hasSubmitted ? (
            <div className="text-center p-5">
              <div style={{ width: '64px', height: '64px', borderRadius: '50%', background: 'var(--success-bg)', display: 'flex', alignItems: 'center', justify: 'center', margin: '0 auto 14px' }}>
                <Check size={32} className="text-success" />
              </div>
              <h3 className="[margin:0_0_6px] text-ink font-extrabold">
                {isGraded ? t('student.homework.graded', 'Graded') : t('student.homework.submissionReceived', 'Submission received')}
              </h3>
              <p className="text-muted text-sm m-0">
                {isGraded
                  ? (isAr ? 'تم تقييم تسليمك.' : 'Your submission has been graded.')
                  : (isAr ? 'تم استلام تسليمك بنجاح. سيقوم معلمك بمراجعته قريباً.' : 'Submitted. Your teacher will review it soon.')}
              </p>
              {isGraded && getField(subData, 'score') != null && (
                <div className="mt-3 text-[22px] font-extrabold text-success">
                  {getField(subData, 'score')}{points != null ? `/${points}` : ''}
                </div>
              )}
              {isGraded && getField(subData, 'feedback') && (
                <p className="mt-2 text-sm text-ink-2">{getField(subData, 'feedback')}</p>
              )}
              {subData && (
                <div className="mt-4 bg-surface-2 p-3 rounded-lg text-start">
                  <DetailList item={subData} locale={locale} />
                </div>
              )}
            </div>
          ) : (
            <div>
              <div className="font-bold text-ink-2 mb-2.5">
                {t('student.homework.yourSubmission', 'Your submission')}
              </div>

              {/* Upload Dropzone */}
              <input
                type="file"
                ref={fileInputRef}
                onChange={handleFileChange}
                className="hidden"
                accept=".pdf,.docx,.png,.jpg,.jpeg"
              />
              <div onClick={handleUploadClick} className="student-upload-area">
                <Download size={30} className="student-upload-area__icon" />
                <div className="student-upload-area__text">
                  {file ? (
                    <span className="text-success flex items-center gap-1.5">
                      <Check size={16} /> {file.name}
                    </span>
                  ) : (
                    t('student.homework.clickToUpload', 'Click to upload your file')
                  )}
                </div>
                <div className="student-upload-area__hint">
                  {t('student.homework.acceptedFormats', 'PDF, DOCX or images up to 10MB')}
                </div>
              </div>

              {/* Notes field */}
              <div className="mt-3.5">
                <TextareaField
                  label={t('student.homework.notes', 'Notes (optional)')}
                  value={content}
                  onChange={(e) => setContent(e.target.value)}
                  placeholder={t('student.homework.notesPlaceholder', 'Add a note for your teacher...')}
                  maxLength={1000}
                />
              </div>

              {/* Submission actions */}
              <div className="mt-4 flex gap-2.5">
                <Button
                  onClick={handleSubmit}
                  loading={mutation.isPending}
                  disabled={mutation.isPending || (!content.trim() && !file)}
                >
                  <Check size={16} className="me-1" />
                  {t('student.homework.submitAction', 'Submit homework')}
                </Button>
              </div>
              {mutation.isError && <ErrorState error={mutation.error} />}
            </div>
          )}
        </Card>

        {/* Right Column: Status Card */}
        <Card>
          <h3 className="[margin:0_0_16px] font-bold text-ink text-base">
            {t('student.homework.statusCardTitle', 'Status')}
          </h3>

          <div className="student-status-steps">
            {[
              { label: t('student.homework.statusStep.assigned', 'Assigned'), active: true },
              { label: t('student.homework.statusStep.submitted', 'Submitted'), active: hasSubmitted },
              { label: t('student.homework.statusStep.graded', 'Graded'), active: isGraded },
            ].map((step, i) => (
              <div key={i} className="student-status-step">
                <div
                  className={`student-status-step__marker ${
                    step.active ? 'student-status-step__marker--done' : 'student-status-step__marker--pending'
                  }`}
                >
                  {step.active ? (
                    <Check size={15} />
                  ) : (
                    <span className="student-status-step__dot" />
                  )}
                </div>
                <span
                  className={`student-status-step__label ${
                    step.active ? 'student-status-step__label--active' : 'student-status-step__label--inactive'
                  }`}
                >
                  {step.label}
                </span>
              </div>
            ))}
          </div>
        </Card>
      </div>
    </>
  )
}

// =============================================================================
// Quizzes
// =============================================================================

export default function StudentHomeworkPage(props) {
  const { userId, locale } = useStudentContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <HomeworkPage userId={userId} locale={locale} {...props} />
}
