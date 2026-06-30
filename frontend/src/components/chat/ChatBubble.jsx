// Single chat bubble matching the prototype message bubbles. `mine` right-aligns
// with the brand fill; otherwise it is a received bubble with an optional avatar.
export function ChatBubble({ mine = false, children, meta, avatar }) {
  return (
    <div className={`chat-bubble${mine ? ' chat-bubble--mine' : ''}`}>
      {!mine && avatar && <span className="chat-bubble__avatar">{avatar}</span>}
      <div className="chat-bubble__col">
        <div className="chat-bubble__body">{children}</div>
        {meta && <div className="chat-bubble__meta">{meta}</div>}
      </div>
    </div>
  )
}
