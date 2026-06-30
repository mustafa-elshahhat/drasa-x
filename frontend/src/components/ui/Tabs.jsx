import { useRef } from 'react'

// Underline tab strip matching the prototype `tabs()` primitive. Controlled:
// the caller owns `active` and `onChange`. Accessible as a tablist with roving
// arrow-key navigation; the panel content is rendered by the caller.
//
//   <Tabs tabs={[{ id, label }]} active={tab} onChange={setTab} ariaLabel="…" />
export function Tabs({ tabs = [], active, onChange, ariaLabel }) {
  const refs = useRef([])

  const focusTab = (idx) => {
    const next = (idx + tabs.length) % tabs.length
    refs.current[next]?.focus()
    onChange?.(tabs[next].id)
  }

  const onKeyDown = (e, idx) => {
    if (e.key === 'ArrowRight' || e.key === 'ArrowDown') {
      e.preventDefault()
      focusTab(idx + 1)
    } else if (e.key === 'ArrowLeft' || e.key === 'ArrowUp') {
      e.preventDefault()
      focusTab(idx - 1)
    } else if (e.key === 'Home') {
      e.preventDefault()
      focusTab(0)
    } else if (e.key === 'End') {
      e.preventDefault()
      focusTab(tabs.length - 1)
    }
  }

  return (
    <div className="ui-tabs" role="tablist" aria-label={ariaLabel}>
      {tabs.map((tab, idx) => {
        const selected = tab.id === active
        return (
          <button
            key={tab.id}
            ref={(el) => (refs.current[idx] = el)}
            type="button"
            role="tab"
            id={`tab-${tab.id}`}
            aria-selected={selected}
            aria-controls={`tabpanel-${tab.id}`}
            tabIndex={selected ? 0 : -1}
            className={`ui-tabs__tab${selected ? ' is-active' : ''}`}
            onClick={() => onChange?.(tab.id)}
            onKeyDown={(e) => onKeyDown(e, idx)}
          >
            {tab.label}
            {tab.count != null && <span className="ui-tabs__count">{tab.count}</span>}
          </button>
        )
      })}
    </div>
  )
}
