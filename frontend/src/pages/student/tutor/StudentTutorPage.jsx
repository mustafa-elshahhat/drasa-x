import { useState } from 'react'
import { useMutation } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { FileText, Send, Sparkles } from 'lucide-react'
import { useStudentContext } from '../../../features/student/helpers'
import { ErrorState } from '../../../components/ui/states'
import { studentApi } from '../../../features/student/studentApi'
import { toItems } from '../../../features/student/studentSchemas'
import { displayValue } from '../../../features/student/studentUtils'

function TutorPage() {
  const { t, i18n } = useTranslation()
  const [message, setMessage] = useState('')
  const [chatHistory, setChatHistory] = useState([])
  const isAr = i18n.language === 'ar'

  const mutation = useMutation({
    mutationFn: (payload) => studentApi.tutor(payload),
    onSuccess: (data) => {
      const responseCitations = data ? toItems(data.citations).map((c) => ({
        label: displayValue(c, ['title', 'Title', 'source', 'Source', 'label', 'Label']) || displayValue(c)
      })) : []
      
      setChatHistory(prev => [
        ...prev,
        {
          role: 'ai',
          text: data.answer || data.noAnswerReason || (isAr ? 'لم أتمكن من العثور على إجابة.' : 'I could not find an answer.'),
          citations: responseCitations
        }
      ])
      
      // Scroll to bottom
      setTimeout(() => {
        const chatContainer = document.getElementById('dxchat')
        if (chatContainer) {
          chatContainer.scrollTop = chatContainer.scrollHeight
        }
      }, 50)
    },
    onError: (error) => {
      setChatHistory(prev => [
        ...prev,
        {
          role: 'ai',
          text: (isAr ? 'عذراً، حدث خطأ أثناء الاتصال بالخادم.' : 'Sorry, an error occurred while connecting to the tutor server.') + ` (${error.message || error})`,
          isError: true
        }
      ])
    }
  })

  const handleSend = (textToSend) => {
    const trimmed = String(textToSend || '').trim()
    if (!trimmed || mutation.isPending) return

    // Add user message
    setChatHistory(prev => [...prev, { role: 'user', text: trimmed }])
    setMessage('')

    // Trigger AI tutor request
    mutation.mutate({
      message: trimmed,
      language: i18n.language
    })

    // Scroll to bottom
    setTimeout(() => {
      const chatContainer = document.getElementById('dxchat')
      if (chatContainer) {
        chatContainer.scrollTop = chatContainer.scrollHeight
      }
    }, 50)
  }

  const suggestions = [
    t('student.tutor.suggestions.parts', 'Explain integration by parts'),
    t('student.tutor.suggestions.chain', 'What is the chain rule?'),
    t('student.tutor.suggestions.question', 'Help me with question 7')
  ]

  return (
    <>
      {/* AI Tutor Header info */}
      <div style={{ display: 'flex', alignItems: 'center', gap: '12px', marginBottom: '18px' }}>
        <div style={{ width: '48px', height: '48px', borderRadius: '13px', background: 'var(--purple-bg)', display: 'flex', alignItems: 'center', justify: 'center', color: 'var(--purple)' }}>
          <Sparkles size={26} />
        </div>
        <div>
          <h1 style={{ margin: 0, fontSize: '24px', fontWeight: 800, color: 'var(--text)' }}>
            {t('student.tutor.title', 'AI Tutor')}
          </h1>
          <div style={{ color: 'var(--success)', fontSize: '13px', display: 'flex', alignItems: 'center', gap: '6px', marginTop: '2px' }}>
            <span style={{ width: '8px', height: '8px', borderRadius: '50%', background: 'var(--success)', display: 'inline-block' }} />
            <span>{t('student.tutor.onlineStatus', 'Online · Answers cite your curriculum')}</span>
          </div>
        </div>
      </div>

      {/* Main chat panel */}
      <div className="student-ai-tutor-container" style={{ height: '560px' }}>
        {/* Chat thread messages */}
        <div id="dxchat" className="student-ai-tutor-thread">
          {chatHistory.length === 0 ? (
            <div className="student-ai-tutor-empty">
              <div className="student-ai-tutor-empty__icon-wrapper">
                <Sparkles size={32} style={{ color: 'var(--purple)' }} />
              </div>
              <h3 className="student-ai-tutor-empty__title">
                {t('student.tutor.ask', 'Ask me anything about your courses')}
              </h3>
              <p className="student-ai-tutor-empty__subtitle">
                {t('student.tutor.description', 'I answer using your own lessons and materials, with cited sources.')}
              </p>
            </div>
          ) : (
            chatHistory.map((m, i) => {
              if (m.role === 'user') {
                return (
                  <div key={i} className="student-ai-tutor-msg-me">
                    <div className="student-ai-tutor-msg-me__bubble">{m.text}</div>
                  </div>
                )
              } else {
                return (
                  <div key={i} className="student-ai-tutor-msg-ai">
                    <div className="student-ai-tutor-msg-ai__avatar">
                      <Sparkles size={18} />
                    </div>
                    <div>
                      <div
                        className="student-ai-tutor-msg-ai__bubble"
                        style={{
                          border: m.isError ? '1px solid var(--danger)' : 'none',
                          background: m.isError ? 'var(--danger-bg)' : 'var(--surface-2)'
                        }}
                      >
                        {m.text}
                      </div>

                      {/* Source chips */}
                      {m.citations && m.citations.length > 0 && (
                        <div className="student-ai-tutor-msg-ai__sources">
                          <span className="student-ai-tutor-msg-ai__sources-label">
                            {t('student.tutor.citations', 'SOURCES:')}
                          </span>
                          {m.citations.map((c, ci) => (
                            <span key={ci} className="student-ai-tutor-msg-ai__source-chip">
                              <FileText size={12} />
                              <span>{c.label}</span>
                            </span>
                          ))}
                        </div>
                      )}
                    </div>
                  </div>
                )
              }
            })
          )}

          {/* Pending loading state */}
          {mutation.isPending && (
            <div className="student-ai-tutor-msg-ai">
              <div className="student-ai-tutor-msg-ai__avatar">
                <Sparkles size={18} />
              </div>
              <div
                className="student-ai-tutor-msg-ai__bubble"
                style={{ display: 'flex', gap: '5px', padding: '14px 18px' }}
              >
                <span className="ui-spinner__circle" style={{ width: '16px', height: '16px', borderWidth: '2px' }} />
                <span style={{ fontSize: '13px', color: 'var(--text-dim)' }}>
                  {t('student.tutor.loading', 'Thinking...')}
                </span>
              </div>
            </div>
          )}
        </div>

        {/* Suggestion Chips */}
        {chatHistory.length === 0 && (
          <div className="student-ai-tutor-suggestions">
            {suggestions.map((s, i) => (
              <button key={i} onClick={() => handleSend(s)} className="student-ai-tutor-suggestion-chip">
                {s}
              </button>
            ))}
          </div>
        )}

        {/* Input Bar */}
        <div className="student-ai-tutor-input-bar">
          <input
            id="dxtutorin"
            className="student-ai-tutor-input"
            value={message}
            onChange={(e) => setMessage(e.target.value)}
            placeholder={t('student.tutor.inputPlaceholder', 'Ask the AI Tutor...')}
            onKeyDown={(e) => {
              if (e.key === 'Enter') {
                handleSend(message)
              }
            }}
            disabled={mutation.isPending}
          />
          <button
            onClick={() => handleSend(message)}
            className="student-ai-tutor-send-btn"
            disabled={!message.trim() || mutation.isPending}
          >
            <Send size={18} />
          </button>
        </div>
      </div>
    </>
  )
}

// =============================================================================
// Progress / recommendations / engagement
// =============================================================================

export default function StudentTutorPage(props) {
  const { userId, locale } = useStudentContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <TutorPage userId={userId} locale={locale} {...props} />
}
