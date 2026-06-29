import { useEffect, useRef, useCallback } from 'react'
import { X } from 'lucide-react'

// Accessible modal dialog (Phase 7 §10/§15):
//   * role="dialog" aria-modal, labelled by its title.
//   * Focus moves into the dialog on open and is restored to the trigger on close.
//   * Focus is trapped within the dialog (Tab/Shift+Tab wrap).
//   * Escape closes; clicking the backdrop closes.
const FOCUSABLE =
  'a[href], button:not([disabled]), textarea, input, select, [tabindex]:not([tabindex="-1"])'

export function Modal({ open, onClose, title, children, footer, labelledById = 'ui-modal-title' }) {
  const dialogRef = useRef(null)
  const previouslyFocused = useRef(null)

  const handleKeyDown = useCallback(
    (e) => {
      if (e.key === 'Escape') {
        e.stopPropagation()
        onClose?.()
        return
      }
      if (e.key !== 'Tab') return
      const nodes = dialogRef.current?.querySelectorAll(FOCUSABLE)
      if (!nodes || nodes.length === 0) return
      const list = Array.from(nodes)
      const first = list[0]
      const last = list[list.length - 1]
      if (e.shiftKey && document.activeElement === first) {
        e.preventDefault()
        last.focus()
      } else if (!e.shiftKey && document.activeElement === last) {
        e.preventDefault()
        first.focus()
      }
    },
    [onClose]
  )

  useEffect(() => {
    if (!open) return
    previouslyFocused.current = document.activeElement
    const node = dialogRef.current
    // Focus the first focusable element, else the dialog container.
    const focusable = node?.querySelector(FOCUSABLE)
    ;(focusable || node)?.focus()
    return () => {
      // Restore focus to whatever opened the dialog.
      if (previouslyFocused.current instanceof HTMLElement) previouslyFocused.current.focus()
    }
  }, [open])

  if (!open) return null

  return (
    <div className="ui-modal-overlay" onMouseDown={(e) => e.target === e.currentTarget && onClose?.()}>
      <div
        ref={dialogRef}
        className="ui-modal"
        role="dialog"
        aria-modal="true"
        aria-labelledby={labelledById}
        tabIndex={-1}
        onKeyDown={handleKeyDown}
      >
        <div className="ui-modal__header">
          <h2 id={labelledById} className="ui-modal__title">
            {title}
          </h2>
          <button type="button" className="ui-modal__close" onClick={onClose} aria-label="Close dialog">
            <X size={18} aria-hidden="true" />
          </button>
        </div>
        <div className="ui-modal__body">{children}</div>
        {footer && <div className="ui-modal__footer">{footer}</div>}
      </div>
    </div>
  )
}
