import { Avatar } from '../ui/Avatar'
import { ChatBubble } from './ChatBubble'

// Conversation thread matching the prototype chat panel. Sorts messages by sent
// time and renders each as a bubble; the current user's messages align to the
// end. `formatWhen` formats the timestamp (passed in so locale handling stays
// with the caller).
export function MessageThread({ messages = [], currentUserId, formatWhen, senderName }) {
  const ordered = [...messages].sort((a, b) => new Date(a.sentAt) - new Date(b.sentAt))
  return (
    <div className="chat-thread" data-testid="thread-messages">
      {ordered.map((m) => {
        const mine = m.senderId === currentUserId
        const name = senderName ? senderName(m) : undefined
        return (
          <ChatBubble
            key={m.id}
            mine={mine}
            avatar={!mine && name ? <Avatar name={name} size={30} /> : undefined}
            meta={formatWhen ? formatWhen(m.sentAt) : undefined}
          >
            {m.body}
          </ChatBubble>
        )
      })}
    </div>
  )
}
