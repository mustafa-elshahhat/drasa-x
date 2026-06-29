/* eslint-disable react-refresh/only-export-components */
// =============================================================================
// In-app toast/notification system (Phase 7 §13).
//   * Accessible live region (aria-live polite/assertive by severity).
//   * Auto-dismiss + manual dismiss.
//   * Deduplicates identical messages within a short window.
//   * Never renders raw tokens/secrets — callers pass safe, user-facing copy.
// =============================================================================
import { createContext, useContext, useCallback, useRef, useState } from 'react'
import { X } from 'lucide-react'

const ToastContext = createContext(null)

let seq = 0
const nextId = () => `t${++seq}`

export function ToastProvider({ children }) {
  const [toasts, setToasts] = useState([])
  const recent = useRef(new Map()) // message -> timestamp, for dedup

  const dismiss = useCallback((id) => {
    setToasts((list) => list.filter((t) => t.id !== id))
  }, [])

  const push = useCallback(
    (toast) => {
      const message = toast.message || ''
      const key = `${toast.variant || 'info'}:${message}`
      const now = Date.now()
      const last = recent.current.get(key)
      // Dedup identical toasts fired within 4s.
      if (last && now - last < 4000) return null
      recent.current.set(key, now)

      const id = nextId()
      const entry = { id, variant: 'info', duration: 5000, ...toast }
      setToasts((list) => [...list, entry])
      if (entry.duration > 0) {
        setTimeout(() => dismiss(id), entry.duration)
      }
      return id
    },
    [dismiss]
  )

  const api = {
    push,
    dismiss,
    success: (message, opts) => push({ ...opts, message, variant: 'success' }),
    error: (message, opts) => push({ ...opts, message, variant: 'error' }),
    info: (message, opts) => push({ ...opts, message, variant: 'info' }),
    warning: (message, opts) => push({ ...opts, message, variant: 'warning' }),
  }

  return (
    <ToastContext.Provider value={api}>
      {children}
      <div className="ui-toast-region" aria-live="polite" aria-atomic="false">
        {toasts.map((t) => (
          <div
            key={t.id}
            className={`ui-toast ui-toast--${t.variant}`}
            role={t.variant === 'error' ? 'alert' : 'status'}
          >
            <div className="ui-toast__body">
              {t.title && <p className="ui-toast__title">{t.title}</p>}
              <p className="ui-toast__msg">{t.message}</p>
              {t.correlationId && (
                <p className="ui-toast__meta">
                  Ref: <code>{t.correlationId}</code>
                </p>
              )}
            </div>
            <button
              type="button"
              className="ui-toast__close"
              onClick={() => dismiss(t.id)}
              aria-label="Dismiss notification"
            >
              <X size={14} aria-hidden="true" />
            </button>
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  )
}

export function useToast() {
  const ctx = useContext(ToastContext)
  if (!ctx) throw new Error('useToast must be used within <ToastProvider>')
  return ctx
}
