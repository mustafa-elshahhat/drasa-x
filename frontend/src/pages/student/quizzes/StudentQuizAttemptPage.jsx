import React, { useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Clock } from 'lucide-react'
import { useStudentContext } from '../../../features/student/helpers'
import { DetailList } from '../../../shared/data-display'
import { TextareaField } from '../../../shared/form'
import { OptionCard } from '../../../shared/quiz'
import { Alert, Button, Card, PageHeader } from '../../../shared/ui'
import { ErrorState } from '../../../shared/feedback'
import { useStudentQuery } from '../../../features/student/helpers'
import { Loading } from '../../../features/student/Loading'
import { studentApi } from '../../../features/student/studentApi'
import { displayValue, itemId } from '../../../features/student/studentUtils'
import { queryKeys } from '../../../lib/query/keys'

function QuizAttemptPage({ userId, locale }) {
  const { t } = useTranslation()
  const { attemptId } = useParams()
  const navigate = useNavigate()
  const qc = useQueryClient()
  
  const query = useStudentQuery(queryKeys.student.attempt(userId, attemptId), (signal) => studentApi.attempt(attemptId, signal), { enabled: Boolean(attemptId) })
  const save = useMutation({ mutationFn: (answers) => studentApi.saveAttempt(attemptId, answers) })
  const submit = useMutation({ mutationFn: () => studentApi.submitAttempt(attemptId), onSuccess: () => { qc.invalidateQueries({ queryKey: queryKeys.student.attemptResult(userId, attemptId) }); navigate(`/app/student/quiz-attempts/${attemptId}/result`) } })

  const [currentQuestionIndex, setCurrentQuestionIndex] = useState(0)
  const [selectedAnswers, setSelectedAnswers] = useState({})

  const isAr = locale === 'ar'

  // Authoritative timer source from the backend AttemptDetailDto: prefer the server-computed
  // ExpiresAt (StartedAt + limit); fall back to TimeLimitMinutes. A quiz with no time limit is
  // untimed and shows no countdown — we NEVER guess a 30-minute default.
  const expiresAtRaw = query.data?.expiresAt ?? query.data?.ExpiresAt ?? null
  const timeLimitMinutes = query.data?.timeLimitMinutes ?? query.data?.TimeLimitMinutes ?? 0
  const isTimed = Boolean(expiresAtRaw) || timeLimitMinutes > 0
  const [timeLeft, setTimeLeft] = useState(null)

  React.useEffect(() => {
    if (expiresAtRaw) {
      const ms = new Date(expiresAtRaw).getTime() - Date.now()
      setTimeLeft(Math.max(0, Math.floor(ms / 1000)))
    } else if (timeLimitMinutes > 0) {
      setTimeLeft(timeLimitMinutes * 60)
    } else {
      setTimeLeft(null)
    }
  }, [expiresAtRaw, timeLimitMinutes])

  // Timer effect — only runs for a timed quiz with remaining time.
  const activeTimer = isTimed && timeLeft != null && timeLeft > 0

  React.useEffect(() => {
    if (!activeTimer) return
    const timer = setInterval(() => {
      setTimeLeft(prev => (prev == null ? prev : Math.max(0, prev - 1)))
    }, 1000)
    return () => clearInterval(timer)
  }, [activeTimer])

  // Initialize selected answers from attempt query
  React.useEffect(() => {
    if (query.data) {
      const initial = {}
      const answersList = query.data.answers || query.data.Answers || []
      for (const a of answersList) {
        const qid = a.questionId || a.QuestionId
        if (qid) {
          initial[qid] = {
            selectedOptionId: a.selectedOptionId ?? a.SelectedOptionId ?? null,
            answerText: a.answerText ?? a.AnswerText ?? ''
          }
        }
      }
      setSelectedAnswers(initial)
    }
  }, [query.data])

  if (query.isLoading) return (<><PageHeader title={t('student.quizzes.attempt')} /><Loading /></>)
  if (query.isError) return <ErrorState error={query.error} onRetry={query.refetch} />

  const questions = Array.isArray(query.data?.questions || query.data?.Questions) ? query.data.questions || query.data.Questions : []
  const quizTitle = query.data?.quizTitle || query.data?.QuizTitle || t('student.quizzes.attempt')

  const handleSelectOption = (qId, optionId) => {
    setSelectedAnswers(prev => ({
      ...prev,
      [qId]: { selectedOptionId: optionId, answerText: '' }
    }))
  }

  const handleTextChange = (qId, text) => {
    setSelectedAnswers(prev => ({
      ...prev,
      [qId]: { selectedOptionId: null, answerText: text }
    }))
  }

  const formatAnswersPayload = () => {
    const payload = []
    for (const [qId, ans] of Object.entries(selectedAnswers)) {
      if (ans.answerText) {
        payload.push({ questionId: qId, answerText: ans.answerText })
      } else if (ans.selectedOptionId) {
        payload.push({ questionId: qId, selectedOptionId: ans.selectedOptionId })
      }
    }
    return payload
  }

  const handleSave = () => {
    const payload = formatAnswersPayload()
    save.mutate(payload)
  }

  const handleSubmit = () => {
    const payload = formatAnswersPayload()
    save.mutate(payload, {
      onSuccess: () => {
        submit.mutate()
      }
    })
  }

  const currentQuestion = questions[currentQuestionIndex]
  const totalQuestions = questions.length

  const progressPercent = totalQuestions > 0 ? ((currentQuestionIndex + 1) / totalQuestions) * 100 : 0

  const formatTime = (seconds) => {
    const m = Math.floor(seconds / 60)
    const s = seconds % 60
    return `${m}:${s < 10 ? '0' : ''}${s}`
  }

  return (
    <>
      <div className="flex justify-between items-center mb-[18px] flex-wrap gap-2.5">
        <div>
          <div className="font-extrabold text-lg text-ink">
            {quizTitle}
          </div>
          <div className="text-muted text-[13px]">
            {t('student.quizzes.questionIndex', 'Question')} {currentQuestionIndex + 1} {t('student.quizzes.of', 'of')} {totalQuestions}
          </div>
        </div>
        {isTimed && timeLeft != null && (
          <div className="quiz-timer">
            <Clock size={16} />
            <span>{formatTime(timeLeft)}</span>
          </div>
        )}
      </div>

      {/* Progress Bar */}
      <div className="quiz-progress" style={{ margin: '12px 0 24px' }}>
        <div className="quiz-progress__track">
          <div className="quiz-progress__fill" style={{ width: `${progressPercent}%` }} />
        </div>
      </div>

      <div className="max-w-[720px]">
        {currentQuestion ? (
          <QuestionBlock
            index={currentQuestionIndex}
            question={currentQuestion}
            saved={selectedAnswers[itemId(currentQuestion)]}
            onSelectOption={(optId) => handleSelectOption(itemId(currentQuestion), optId)}
            onChangeText={(text) => handleTextChange(itemId(currentQuestion), text)}
          />
        ) : (
          <DetailList item={query.data} locale={locale} />
        )}

        {/* Bottom actions row */}
        <div className="flex justify-between mt-[26px] gap-2.5 flex-wrap">
          <Button
            variant="ghost"
            disabled={currentQuestionIndex === 0}
            onClick={() => setCurrentQuestionIndex(prev => Math.max(0, prev - 1))}
          >
            {isAr ? 'السابق' : 'Previous'}
          </Button>

          <div className="flex gap-2.5">
            <Button variant="secondary" onClick={handleSave} loading={save.isPending}>
              {t('student.quizzes.save', 'Save answers')}
            </Button>
            {currentQuestionIndex < totalQuestions - 1 ? (
              <Button onClick={() => setCurrentQuestionIndex(prev => prev + 1)}>
                {isAr ? 'التالي' : 'Next'}
              </Button>
            ) : (
              <Button onClick={handleSubmit} loading={submit.isPending}>
                {t('student.quizzes.submit', 'Submit quiz')}
              </Button>
            )}
          </div>
        </div>

        {/* Pagination buttons */}
        <div className="quiz-nav mt-[18px]">
          {questions.map((_, qi) => {
            const hasAns = Boolean(selectedAnswers[itemId(questions[qi])]?.selectedOptionId || selectedAnswers[itemId(questions[qi])]?.answerText)
            const isCurrent = qi === currentQuestionIndex
            let className = 'quiz-nav__cell'
            if (isCurrent) className += ' is-current'
            else if (hasAns) className += ' is-answered'
            return (
              <button
                key={qi}
                onClick={() => setCurrentQuestionIndex(qi)}
                className={className}
              >
                {qi + 1}
              </button>
            )
          })}
        </div>
      </div>

      {save.isSuccess && <Alert variant="success" title={t('student.quizzes.saved', 'Answers saved')} />}
      {(save.isError || submit.isError) && <ErrorState error={save.error || submit.error} />}
    </>
  )
}

const OPTION_MARKERS = ['A', 'B', 'C', 'D', 'E', 'F']

function QuestionBlock({ question, index, saved, onSelectOption, onChangeText }) {
  const { t } = useTranslation()
  const questionId = itemId(question)
  const options = question.options || question.Options || []
  const savedOptionId = saved?.selectedOptionId ?? null
  const savedText = saved?.answerText ?? ''

  return (
    <Card title={`${index + 1}. ${displayValue(question, ['text', 'Text', 'title', 'Title'])}`}>
      {options.length > 0 ? (
        <div className="student-quiz__options">
          {options.map((option, oi) => (
            <OptionCard
              key={itemId(option)}
              name={`q:${questionId}`}
              value={itemId(option)}
              marker={OPTION_MARKERS[oi] || oi + 1}
              label={displayValue(option, ['text', 'Text', 'label', 'Label'])}
              checked={itemId(option) === savedOptionId}
              onChange={() => onSelectOption(itemId(option))}
            />
          ))}
        </div>
      ) : (
        <TextareaField
          label={t('student.quizzes.answer', 'Answer')}
          name={`qt:${questionId}`}
          value={savedText}
          onChange={(e) => onChangeText(e.target.value)}
        />
      )}
    </Card>
  )
}

export default function StudentQuizAttemptPage(props) {
  const { userId, locale } = useStudentContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <QuizAttemptPage userId={userId} locale={locale} {...props} />
}
