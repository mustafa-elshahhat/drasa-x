import { Sparkles } from 'lucide-react'
import { ChatBubble } from './ChatBubble'
import { Chip } from '../ui/Chip'

// AI tutor answer bubble matching the prototype: a sparkle avatar, the answer
// text, and a "sources" row of citation chips. `citations` is a list of
// { label } drawn from the grounded/normalized tutor response — shown only when
// the answer is actually grounded in real sources.
export function AITutorMessage({ answer, grounded = false, citations = [], sourcesLabel, meta }) {
  return (
    <ChatBubble
      avatar={
        <span className="chat-bubble__ai-mark" aria-hidden="true">
          <Sparkles size={16} />
        </span>
      }
      meta={meta}
    >
      <div className="chat-ai__answer">{answer}</div>
      {grounded && citations.length > 0 && (
        <div className="chat-ai__sources">
          {sourcesLabel && <span className="chat-ai__sources-label">{sourcesLabel}</span>}
          {citations.map((c, i) => (
            <Chip key={i} tone="purple">{c.label}</Chip>
          ))}
        </div>
      )}
    </ChatBubble>
  )
}
