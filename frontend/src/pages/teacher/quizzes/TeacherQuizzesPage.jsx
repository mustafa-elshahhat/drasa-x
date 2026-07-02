import { useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { ClipboardList, FileText, Sparkles } from 'lucide-react'
import { DetailList } from '../../../shared/data-display'
import { QuizCard, UnitCard } from '../../../shared/domain'
import { SelectField, TextField, TextareaField } from '../../../shared/form'
import { Alert, Button, Card, PageHeader } from '../../../shared/ui'
import { EmptyState, ErrorState } from '../../../shared/feedback'
import { toItems } from '../../../features/student/studentSchemas'
import { Listing, Loading } from '../../../features/teacher/components'
import { useTeacherQuery } from '../../../features/teacher/helpers'
import { teacherApi } from '../../../features/teacher/teacherApi'
import { displayValue, isQuizPublished, itemId, quizStatusName, submissionStatusName } from '../../../features/teacher/teacherUtils'
import { STALE, queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function QuizzesPage({ userId, locale }) {
  const { t } = useTranslation()
  const { quizId } = useParams()
  const query = useTeacherQuery(queryKeys.teacher.quizzes(userId), (signal) => teacherApi.quizzes(signal))
  if (quizId) return <QuizDetailPage userId={userId} quizId={quizId} locale={locale} />
  return (
    <>
      <PageHeader
        title={t('teacher.quizzes.title')}
        description={t('teacher.quizzes.description')}
        actions={<Link className="ui-btn ui-btn--primary" to="/app/teacher/quiz-generate"><Sparkles size={16} aria-hidden="true" /> {t('teacher.quizzes.generate')}</Link>}
      />
      <ManualCreateCard userId={userId} />
      <Listing query={query} empty={t('teacher.empty.quizzes')} emptyIcon={ClipboardList}>
        {(items) => (
          <div className="ui-grid ui-grid--auto">
            {items.map((item) => {
              const published = isQuizPublished(item.status ?? item.Status)
              return (
                <QuizCard
                  key={itemId(item, ['quizId', 'QuizId', 'id', 'Id'])}
                  to={`/app/teacher/quizzes/${itemId(item, ['quizId', 'QuizId', 'id', 'Id'])}`}
                  icon={ClipboardList}
                  title={displayValue(item, ['title', 'Title'])}
                  status={quizStatusName(item.status ?? item.Status)}
                  statusTone={published ? 'success' : 'muted'}
                />
              )
            })}
          </div>
        )}
      </Listing>
    </>
  )
}

// Manual (non-AI) quiz creation — complements the AI-draft generator; both
// produce an editable draft the teacher builds questions into.
function ManualCreateCard({ userId }) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const qc = useQueryClient()
  const subjects = useTeacherQuery(queryKeys.teacher.subjects(userId), (signal) => teacherApi.subjects(signal), { staleTime: STALE.medium })
  const [form, setForm] = useState({ title: '', subjectId: '', type: '1', difficulty: '2', timeLimitMinutes: 30 })
  const create = useMutation({
    mutationFn: () => teacherApi.createQuiz({
      title: form.title.trim(), subjectId: form.subjectId || null, type: Number(form.type),
      difficulty: Number(form.difficulty), timeLimitMinutes: Number(form.timeLimitMinutes) || 0,
    }),
    onSuccess: (created) => {
      qc.invalidateQueries({ queryKey: queryKeys.teacher.quizzes(userId) })
      const id = itemId(created, ['quizId', 'QuizId', 'id', 'Id'])
      if (id) navigate(`/app/teacher/quizzes/${id}`)
    },
  })
  const subjectItems = subjects.data || []
  const valid = form.title.trim() && form.subjectId
  return (
    <Card title={t('teacher.quizzes.manualCreate', 'Create a quiz manually')}>
      <form className="ui-formgrid ui-formgrid--2" onSubmit={(e) => { e.preventDefault(); if (valid) create.mutate() }}>
        <TextField label={t('teacher.quizzes.field.title', 'Title')} value={form.title} onChange={(e) => setForm((f) => ({ ...f, title: e.target.value }))} required />
        <SelectField
          label={t('teacher.subjects.title')}
          value={form.subjectId}
          onChange={(e) => setForm((f) => ({ ...f, subjectId: e.target.value }))}
          options={[{ value: '', label: t('teacher.quizzes.chooseClass') }, ...subjectItems.map((s) => ({ value: itemId(s, ['subjectId', 'SubjectId', 'id', 'Id']), label: displayValue(s, ['name', 'Name']) }))]}
        />
        <SelectField
          label={t('teacher.quizzes.field.type', 'Type')}
          value={form.type}
          onChange={(e) => setForm((f) => ({ ...f, type: e.target.value }))}
          options={[{ value: '1', label: t('teacher.quizzes.quizType.lesson', 'Lesson') }, { value: '2', label: t('teacher.quizzes.quizType.practice', 'Practice') }, { value: '3', label: t('teacher.quizzes.quizType.final', 'Final') }]}
        />
        <TextField label={t('teacher.quizzes.field.timeLimit', 'Time limit (minutes)')} type="number" min="0" value={form.timeLimitMinutes} onChange={(e) => setForm((f) => ({ ...f, timeLimitMinutes: e.target.value }))} />
        <Button type="submit" loading={create.isPending} disabled={!valid}>{t('teacher.quizzes.manualCreate', 'Create a quiz manually')}</Button>
      </form>
      {create.isError && <ErrorState error={create.error} />}
    </Card>
  )
}

function QuizDetailPage({ userId, quizId, locale }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const quiz = useTeacherQuery(queryKeys.teacher.quiz(userId, quizId), (signal) => teacherApi.quiz(quizId, signal), { enabled: Boolean(quizId) })
  const submissions = useTeacherQuery(queryKeys.teacher.quizSubmissions(userId, quizId), (signal) => teacherApi.quizSubmissions(quizId, signal), { enabled: Boolean(quizId) })

  const invalidateQuiz = () => {
    qc.invalidateQueries({ queryKey: queryKeys.teacher.quiz(userId, quizId) })
    qc.invalidateQueries({ queryKey: queryKeys.teacher.quizzes(userId) })
    qc.invalidateQueries({ queryKey: queryKeys.teacher.dashboard(userId) })
  }
  const publish = useMutation({ mutationFn: () => teacherApi.publishQuiz(quizId), onSuccess: invalidateQuiz })
  const archive = useMutation({ mutationFn: () => teacherApi.archiveQuiz(quizId), onSuccess: invalidateQuiz })
  const data = quiz.data || {}
  const questions = toItems(data.questions ?? data.Questions ?? [])
  const isPublished = isQuizPublished(data.status ?? data.Status)
  const isArchived = quizStatusName(data.status ?? data.Status).toLowerCase() === 'archived'

  if (quiz.isLoading) return (<><PageHeader title={t('teacher.quizzes.details')} /><Loading /></>)
  if (quiz.isError) return <ErrorState error={quiz.error} onRetry={quiz.refetch} />

  // Client-side publish readiness — mirrors the backend rules so the teacher sees WHY publish is blocked.
  const publishBlockers = []
  if (!questions.length) publishBlockers.push(t('teacher.quizzes.validation.noQuestions', 'Add at least one question.'))
  for (const q of questions) {
    const qText = displayValue(q, ['text', 'Text'])
    if ((q.points ?? q.Points ?? 0) <= 0) publishBlockers.push(t('teacher.quizzes.validation.points', 'Question "{{q}}" needs a positive point value.', { q: qText }))
    if (isObjectiveType(q.type ?? q.Type)) {
      const opts = toItems(q.options ?? q.Options ?? [])
      if (opts.length < 2) publishBlockers.push(t('teacher.quizzes.validation.options', 'Question "{{q}}" needs at least two options.', { q: qText }))
      if (!opts.some((o) => (o.isCorrect ?? o.IsCorrect) === true)) publishBlockers.push(t('teacher.quizzes.validation.correct', 'Question "{{q}}" needs a correct option.', { q: qText }))
    }
  }
  const canPublish = publishBlockers.length === 0

  return (
    <>
      <PageHeader
        title={displayValue(data, ['title', 'Title']) || t('teacher.quizzes.details')}
        description={t('teacher.quizzes.reviewDescription')}
        actions={
          <span className="cluster">
            {!isPublished && <Button onClick={() => publish.mutate()} loading={publish.isPending}>{t('teacher.quizzes.publish')}</Button>}
            {!isArchived && <Button variant="secondary" onClick={() => archive.mutate()} loading={archive.isPending}>{t('teacher.quizzes.archive', 'Archive')}</Button>}
          </span>
        }
      />
      <Card title={t('teacher.details')}>
        <Alert variant={isPublished ? 'success' : 'info'} title={`${t('teacher.quizzes.status')}: ${quizStatusName(data.status ?? data.Status)}`}>
          {isPublished ? t('teacher.quizzes.publishedBody') : t('teacher.quizzes.draftBody')}
        </Alert>
        <DetailList item={data} locale={locale} />
      </Card>
      {publish.isError && <ErrorState error={publish.error} />}
      {publish.isSuccess && <Alert variant="success" title={t('teacher.quizzes.publishedTitle')}>{t('teacher.quizzes.publishedBody')}</Alert>}
      {archive.isError && <ErrorState error={archive.error} />}

      {!isPublished && <QuizMetadataCard userId={userId} quizId={quizId} data={data} onSaved={invalidateQuiz} />}

      {!isPublished && !canPublish && (
        <Alert variant="warning" title={t('teacher.quizzes.validation.title', 'Before you can publish')}>
          <ul className="ui-list">
            {publishBlockers.map((b, i) => <li key={i}>{b}</li>)}
          </ul>
        </Alert>
      )}

      <Card title={t('teacher.quizzes.questions')}>
        {questions.length ? questions.map((q) => <QuestionEditor key={itemId(q)} userId={userId} quizId={quizId} question={q} editable={!isPublished} onSaved={invalidateQuiz} />) : <EmptyState title={t('teacher.empty.questions')} />}
        {!isPublished && <AddQuestionForm userId={userId} quizId={quizId} nextOrder={questions.length + 1} onSaved={invalidateQuiz} />}
      </Card>

      {isPublished && <AssignQuizCard userId={userId} quizId={quizId} />}
      {isPublished && <AnalyticsCard userId={userId} quizId={quizId} />}

      <Card title={t('teacher.grading.submissions')}>
        <Listing query={submissions} empty={t('teacher.empty.submissions')}>
          {(items) => (
            <div className="student-list">
              {items.map((s) => (
                <UnitCard
                  key={itemId(s, ['id', 'Id', 'attemptId', 'AttemptId'])}
                  to={`/app/teacher/submissions/${itemId(s, ['id', 'Id', 'attemptId', 'AttemptId'])}`}
                  icon={FileText}
                  title={displayValue(s, ['studentName', 'StudentName', 'studentId', 'StudentId'])}
                  status={submissionStatusName(s.submissionStatus ?? s.SubmissionStatus ?? s.status ?? s.Status)}
                />
              ))}
            </div>
          )}
        </Listing>
      </Card>
    </>
  )
}

// QuestionType enum (backend serializes as int): MCQ=1, TrueFalse=2, MultiSelect=3, Essay=4.
const Q_MCQ = 1, Q_TRUEFALSE = 2, Q_MULTISELECT = 3, Q_ESSAY = 4
function isObjectiveType(type) {
  const n = typeof type === 'number' ? type : Number(type)
  return n === Q_MCQ || n === Q_TRUEFALSE || n === Q_MULTISELECT
}
function normalizeOptions(question) {
  return toItems(question.options ?? question.Options ?? []).map((o) => ({
    text: o.text ?? o.Text ?? '',
    isCorrect: (o.isCorrect ?? o.IsCorrect) === true,
  }))
}

function QuestionEditor({ userId, quizId, question, editable, onSaved }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const questionId = itemId(question)
  const type = question.type ?? question.Type ?? Q_MCQ
  const objective = isObjectiveType(type)
  const [editing, setEditing] = useState(false)
  const [text, setText] = useState(displayValue(question, ['text', 'Text']))
  const [points, setPoints] = useState(question.points ?? question.Points ?? 1)
  const [correctAnswerText, setCorrectAnswerText] = useState(question.correctAnswerText ?? question.CorrectAnswerText ?? '')
  const [explanation, setExplanation] = useState(question.explanation ?? question.Explanation ?? '')
  // Preserve existing options so editing the text never wipes them (backend replaces options on update).
  const [options, setOptions] = useState(normalizeOptions(question))
  const multi = type === Q_MULTISELECT

  const save = useMutation({
    mutationFn: () => teacherApi.updateQuestion(quizId, questionId, {
      id: questionId,
      text: text.trim(),
      type,
      order: question.order ?? question.Order ?? 1,
      points: Number(points) || 1,
      correctAnswerText: objective ? null : (correctAnswerText.trim() || null),
      explanation: explanation.trim() || null,
      options: objective ? options.filter((o) => o.text.trim()).map((o) => ({ text: o.text.trim(), isCorrect: o.isCorrect })) : [],
    }),
    onSuccess: () => { setEditing(false); qc.invalidateQueries({ queryKey: queryKeys.teacher.quiz(userId, quizId) }); onSaved?.() },
  })
  const remove = useMutation({
    mutationFn: () => teacherApi.deleteQuestion(quizId, questionId),
    onSuccess: () => { qc.invalidateQueries({ queryKey: queryKeys.teacher.quiz(userId, quizId) }); onSaved?.() },
  })

  const reset = () => {
    setText(displayValue(question, ['text', 'Text']))
    setPoints(question.points ?? question.Points ?? 1)
    setCorrectAnswerText(question.correctAnswerText ?? question.CorrectAnswerText ?? '')
    setExplanation(question.explanation ?? question.Explanation ?? '')
    setOptions(normalizeOptions(question))
    setEditing(false)
  }

  return (
    <div className="student-list__item">
      {editing ? (
        <form className="stack" onSubmit={(e) => { e.preventDefault(); if (text.trim()) save.mutate() }}>
          <TextareaField label={t('teacher.quizzes.questionText')} value={text} onChange={(e) => setText(e.target.value)} maxLength={2000} />
          <TextField label={t('teacher.quizzes.points', 'Points')} type="number" min="1" value={points} onChange={(e) => setPoints(e.target.value)} />
          {objective ? (
            <OptionsEditor options={options} setOptions={setOptions} multi={multi} />
          ) : (
            <TextareaField label={t('teacher.quizzes.modelAnswer', 'Model answer (optional)')} value={correctAnswerText} onChange={(e) => setCorrectAnswerText(e.target.value)} maxLength={2000} />
          )}
          <TextareaField label={t('teacher.quizzes.explanation', 'Explanation (optional)')} value={explanation} onChange={(e) => setExplanation(e.target.value)} maxLength={2000} />
          <div className="student-actions">
            <Button type="submit" loading={save.isPending} disabled={!text.trim()}>{t('actions.save')}</Button>
            <Button type="button" variant="secondary" onClick={reset}>{t('actions.cancel')}</Button>
          </div>
          {save.isError && <ErrorState error={save.error} />}
        </form>
      ) : (
        <div>
          <div className="student-item">
            <strong className="domain-row__title">{displayValue(question, ['text', 'Text'])}</strong>
            {editable && (
              <span className="cluster">
                <Button variant="secondary" onClick={() => setEditing(true)}>{t('teacher.quizzes.editQuestion')}</Button>
                <Button variant="secondary" onClick={() => remove.mutate()} loading={remove.isPending}>{t('teacher.quizzes.deleteQuestion', 'Delete question')}</Button>
              </span>
            )}
          </div>
          {objective && (
            <ul className="ui-list">
              {normalizeOptions(question).map((o, i) => (
                <li key={i} className={o.isCorrect ? 'text-success' : 'ui-muted'}>{o.isCorrect ? '✓ ' : '• '}{o.text}</li>
              ))}
            </ul>
          )}
          {remove.isError && <ErrorState error={remove.error} />}
        </div>
      )}
    </div>
  )
}

// Editable option list for objective questions. `multi` allows several correct answers.
function OptionsEditor({ options, setOptions, multi }) {
  const { t } = useTranslation()
  const update = (i, patch) => setOptions(options.map((o, idx) => idx === i ? { ...o, ...patch } : o))
  const setCorrect = (i) => setOptions(options.map((o, idx) => multi ? (idx === i ? { ...o, isCorrect: !o.isCorrect } : o) : { ...o, isCorrect: idx === i }))
  return (
    <div className="stack">
      <span className="ui-field__label">{t('teacher.quizzes.options', 'Options (mark the correct one)')}</span>
      {options.map((o, i) => (
        <div className="cluster" key={i} style={{ alignItems: 'center', gap: 8 }}>
          <input
            type={multi ? 'checkbox' : 'radio'}
            name="correct-option"
            checked={o.isCorrect}
            onChange={() => setCorrect(i)}
            aria-label={t('teacher.quizzes.markCorrect', 'Mark correct')}
          />
          <input
            type="text"
            className="ui-input"
            style={{ flex: 1 }}
            value={o.text}
            onChange={(e) => update(i, { text: e.target.value })}
            placeholder={t('teacher.quizzes.optionText', 'Option text')}
          />
          <Button type="button" variant="ghost" onClick={() => setOptions(options.filter((_, idx) => idx !== i))}>{t('actions.remove', 'Remove')}</Button>
        </div>
      ))}
      <Button type="button" variant="secondary" onClick={() => setOptions([...options, { text: '', isCorrect: false }])}>{t('teacher.quizzes.addOption', 'Add option')}</Button>
    </div>
  )
}

// Add a new question (objective with options, or an essay) to a draft quiz.
function AddQuestionForm({ userId, quizId, nextOrder, onSaved }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const [open, setOpen] = useState(false)
  const [type, setType] = useState(Q_MCQ)
  const [text, setText] = useState('')
  const [points, setPoints] = useState(1)
  const [explanation, setExplanation] = useState('')
  const [correctAnswerText, setCorrectAnswerText] = useState('')
  const [options, setOptions] = useState([{ text: '', isCorrect: true }, { text: '', isCorrect: false }])
  const objective = isObjectiveType(type)
  const add = useMutation({
    mutationFn: () => teacherApi.addQuestion(quizId, {
      text: text.trim(),
      type,
      order: nextOrder,
      points: Number(points) || 1,
      correctAnswerText: objective ? null : (correctAnswerText.trim() || null),
      explanation: explanation.trim() || null,
      options: objective ? options.filter((o) => o.text.trim()).map((o) => ({ text: o.text.trim(), isCorrect: o.isCorrect })) : [],
    }),
    onSuccess: () => {
      setOpen(false); setText(''); setPoints(1); setExplanation(''); setCorrectAnswerText('')
      setOptions([{ text: '', isCorrect: true }, { text: '', isCorrect: false }]); setType(Q_MCQ)
      qc.invalidateQueries({ queryKey: queryKeys.teacher.quiz(userId, quizId) }); onSaved?.()
    },
  })
  if (!open) {
    return <div className="student-actions" style={{ marginTop: 12 }}><Button variant="secondary" onClick={() => setOpen(true)}>{t('teacher.quizzes.addQuestion', 'Add question')}</Button></div>
  }
  return (
    <form className="stack" style={{ marginTop: 12 }} onSubmit={(e) => { e.preventDefault(); if (text.trim()) add.mutate() }}>
      <SelectField
        label={t('teacher.quizzes.questionType', 'Question type')}
        value={String(type)}
        onChange={(e) => setType(Number(e.target.value))}
        options={[
          { value: String(Q_MCQ), label: t('teacher.quizzes.type.mcq', 'Multiple choice') },
          { value: String(Q_TRUEFALSE), label: t('teacher.quizzes.type.truefalse', 'True / False') },
          { value: String(Q_MULTISELECT), label: t('teacher.quizzes.type.multiselect', 'Multiple select') },
          { value: String(Q_ESSAY), label: t('teacher.quizzes.type.essay', 'Essay') },
        ]}
      />
      <TextareaField label={t('teacher.quizzes.questionText')} value={text} onChange={(e) => setText(e.target.value)} maxLength={2000} />
      <TextField label={t('teacher.quizzes.points', 'Points')} type="number" min="1" value={points} onChange={(e) => setPoints(e.target.value)} />
      {objective ? (
        <OptionsEditor options={options} setOptions={setOptions} multi={type === Q_MULTISELECT} />
      ) : (
        <TextareaField label={t('teacher.quizzes.modelAnswer', 'Model answer (optional)')} value={correctAnswerText} onChange={(e) => setCorrectAnswerText(e.target.value)} maxLength={2000} />
      )}
      <TextareaField label={t('teacher.quizzes.explanation', 'Explanation (optional)')} value={explanation} onChange={(e) => setExplanation(e.target.value)} maxLength={2000} />
      <div className="student-actions">
        <Button type="submit" loading={add.isPending} disabled={!text.trim()}>{t('teacher.quizzes.addQuestion', 'Add question')}</Button>
        <Button type="button" variant="secondary" onClick={() => setOpen(false)}>{t('actions.cancel')}</Button>
      </div>
      {add.isError && <ErrorState error={add.error} />}
    </form>
  )
}

// Edit quiz metadata (title, type, time limit, attempts, due date) on a draft.
function QuizMetadataCard({ userId, quizId, data, onSaved }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const toLocalInput = (v) => {
    if (!v) return ''
    const d = new Date(v)
    if (Number.isNaN(d.getTime())) return ''
    const pad = (n) => String(n).padStart(2, '0')
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`
  }
  const [form, setForm] = useState({
    title: data.title ?? data.Title ?? '',
    type: String(data.type ?? data.Type ?? 1),
    difficulty: String(data.difficulty ?? data.Difficulty ?? 2),
    timeLimitMinutes: data.timeLimitMinutes ?? data.TimeLimitMinutes ?? 30,
    maxAttempts: (data.maxAttempts ?? data.MaxAttempts) ?? '',
    dueDate: toLocalInput(data.dueDate ?? data.DueDate),
  })
  const save = useMutation({
    mutationFn: () => teacherApi.updateQuiz(quizId, {
      id: quizId,
      title: form.title.trim(),
      type: Number(form.type),
      difficulty: Number(form.difficulty),
      timeLimitMinutes: Number(form.timeLimitMinutes) || 0,
      maxAttempts: form.maxAttempts === '' ? null : Number(form.maxAttempts),
      dueDate: form.dueDate ? new Date(form.dueDate).toISOString() : null,
    }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: queryKeys.teacher.quiz(userId, quizId) }); onSaved?.() },
  })
  return (
    <Card title={t('teacher.quizzes.editMetadata', 'Edit quiz details')}>
      <form className="stack" onSubmit={(e) => { e.preventDefault(); if (form.title.trim()) save.mutate() }}>
        <TextField label={t('teacher.quizzes.field.title', 'Title')} value={form.title} onChange={(e) => setForm((f) => ({ ...f, title: e.target.value }))} required />
        <SelectField
          label={t('teacher.quizzes.field.type', 'Type')}
          value={form.type}
          onChange={(e) => setForm((f) => ({ ...f, type: e.target.value }))}
          options={[{ value: '1', label: t('teacher.quizzes.quizType.lesson', 'Lesson') }, { value: '2', label: t('teacher.quizzes.quizType.practice', 'Practice') }, { value: '3', label: t('teacher.quizzes.quizType.final', 'Final') }]}
        />
        <TextField label={t('teacher.quizzes.field.timeLimit', 'Time limit (minutes)')} type="number" min="0" value={form.timeLimitMinutes} onChange={(e) => setForm((f) => ({ ...f, timeLimitMinutes: e.target.value }))} />
        <TextField label={t('teacher.quizzes.field.maxAttempts', 'Max attempts (blank = unlimited)')} type="number" min="1" value={form.maxAttempts} onChange={(e) => setForm((f) => ({ ...f, maxAttempts: e.target.value }))} />
        <TextField label={t('teacher.quizzes.field.dueDate', 'Due date')} type="datetime-local" value={form.dueDate} onChange={(e) => setForm((f) => ({ ...f, dueDate: e.target.value }))} />
        <Button type="submit" loading={save.isPending} disabled={!form.title.trim()}>{t('actions.save')}</Button>
        {save.isError && <ErrorState error={save.error} />}
        {save.isSuccess && <Alert variant="success" title={t('teacher.quizzes.metadataSaved', 'Saved')} />}
      </form>
    </Card>
  )
}

function AssignQuizCard({ userId, quizId }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const classes = useTeacherQuery(queryKeys.teacher.classes(userId), (signal) => teacherApi.classes(signal), { staleTime: STALE.medium })
  const assignments = useTeacherQuery(queryKeys.teacher.quizAssignments(userId, quizId), (signal) => teacherApi.quizAssignments(quizId, signal))
  const [classId, setClassId] = useState('')
  const assign = useMutation({
    mutationFn: () => teacherApi.assignQuiz(quizId, { schoolClassId: classId, studentIds: [] }),
    onSuccess: () => qc.invalidateQueries({ queryKey: queryKeys.teacher.quizAssignments(userId, quizId) }),
  })
  const classItems = classes.data || []
  return (
    <Card title={t('teacher.quizzes.assign')}>
      <form className="stack" onSubmit={(e) => { e.preventDefault(); if (classId) assign.mutate() }}>
        <SelectField
          label={t('teacher.quizzes.targetClass')}
          value={classId}
          onChange={(e) => setClassId(e.target.value)}
          options={[{ value: '', label: t('teacher.quizzes.chooseClass') }, ...classItems.map((c) => ({ value: itemId(c, ['classId', 'ClassId', 'id', 'Id']), label: displayValue(c, ['name', 'Name']) }))]}
        />
        <Button type="submit" loading={assign.isPending} disabled={!classId}>{t('teacher.quizzes.assignButton')}</Button>
      </form>
      {assign.isError && <ErrorState error={assign.error} />}
      {assign.isSuccess && <Alert variant="success" title={t('teacher.quizzes.assignedTitle')}>{t('teacher.quizzes.assignedBody')}</Alert>}
      <Listing query={assignments} empty={t('teacher.empty.assignments')}>
        {(items) => (
          <div className="student-list">
            {items.map((a, idx) => (
              <div className="student-list__item" key={itemId(a) || idx}><DetailList item={a} /></div>
            ))}
          </div>
        )}
      </Listing>
    </Card>
  )
}

function AnalyticsCard({ userId, quizId }) {
  const { t } = useTranslation()
  const analytics = useTeacherQuery(queryKeys.teacher.quizAnalytics(userId, quizId), (signal) => teacherApi.quizAnalytics(quizId, signal))
  if (analytics.isLoading) return <Card title={t('teacher.quizzes.analytics', 'Analytics')}><Loading /></Card>
  if (analytics.isError) return <Card title={t('teacher.quizzes.analytics', 'Analytics')}><ErrorState error={analytics.error} onRetry={analytics.refetch} /></Card>
  const data = analytics.data || {}
  const questions = toItems(data.questions ?? data.Questions ?? [])
  return (
    <Card title={t('teacher.quizzes.analytics', 'Analytics')}>
      <div className="domain-child__stats">
        <div className="domain-child__stat"><span className="domain-child__stat-value">{data.totalSubmissions ?? data.TotalSubmissions ?? 0}</span><span className="domain-child__stat-label">{t('teacher.quizzes.totalSubmissions', 'Total submissions')}</span></div>
        <div className="domain-child__stat"><span className="domain-child__stat-value">{(data.averageScorePercentage ?? data.AverageScorePercentage ?? 0).toFixed(1)}%</span><span className="domain-child__stat-label">{t('teacher.quizzes.averageScore', 'Average score')}</span></div>
      </div>
      {questions.length === 0 ? <EmptyState title={t('teacher.empty.questions')} /> : (
        <ul className="ui-list">
          {questions.map((q, i) => (
            <li className="ui-list__item" key={itemId(q, ['questionId', 'QuestionId', 'id', 'Id']) || i}>
              <div className="ui-list__body">
                <div className="ui-list__title">{displayValue(q, ['text', 'Text'])}</div>
                <div className="ui-list__meta">
                  {t('teacher.quizzes.answeredCount', '{{answered}} answered', { answered: q.answered ?? q.Answered ?? 0 })}
                  {' · '}
                  {t('teacher.quizzes.correctRate', '{{rate}}% correct', { rate: Math.round((q.correctRate ?? q.CorrectRate ?? 0) * 100) })}
                </div>
              </div>
            </li>
          ))}
        </ul>
      )}
    </Card>
  )
}

export default function TeacherQuizzesPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <QuizzesPage userId={userId} locale={locale} {...props} />
}
