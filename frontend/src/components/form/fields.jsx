import { forwardRef, useId, useState } from 'react'
import { Eye, EyeOff } from 'lucide-react'

// =============================================================================
// Accessible, localization-ready form controls (Phase 7 §9). Each control:
//   * pairs a real <label htmlFor> with the input,
//   * wires aria-invalid + aria-describedby to its error/hint,
//   * renders field-level errors with role="alert",
//   * forwards refs so it composes with react-hook-form `register`.
// Labels/placeholders are passed in already-translated by the caller.
// =============================================================================

function FieldShell({ id, label, hint, error, required, children }) {
  const hintId = hint ? `${id}-hint` : undefined
  const errorId = error ? `${id}-error` : undefined
  return (
    <div className={`ui-field${error ? ' ui-field--invalid' : ''}`}>
      {label && (
        <label htmlFor={id} className="ui-field__label">
          {label}
          {required && <span aria-hidden="true" className="ui-field__req"> *</span>}
        </label>
      )}
      {children({ describedBy: [hintId, errorId].filter(Boolean).join(' ') || undefined })}
      {hint && (
        <p id={hintId} className="ui-field__hint">
          {hint}
        </p>
      )}
      {error && (
        <p id={errorId} className="ui-field__error" role="alert">
          {error}
        </p>
      )}
    </div>
  )
}

export const TextField = forwardRef(function TextField(
  { label, hint, error, required, type = 'text', id, ...rest },
  ref
) {
  const generatedId = useId()
  const fieldId = id || generatedId
  return (
    <FieldShell id={fieldId} label={label} hint={hint} error={error} required={required}>
      {({ describedBy }) => (
        <input
          id={fieldId}
          ref={ref}
          type={type}
          className="ui-input"
          aria-invalid={error ? 'true' : undefined}
          aria-describedby={describedBy}
          {...rest}
        />
      )}
    </FieldShell>
  )
})

export const PasswordField = forwardRef(function PasswordField(
  { label, hint, error, required, id, ...rest },
  ref
) {
  const generatedId = useId()
  const fieldId = id || generatedId
  const [show, setShow] = useState(false)
  return (
    <FieldShell id={fieldId} label={label} hint={hint} error={error} required={required}>
      {({ describedBy }) => (
        <div className="ui-input-group">
          <input
            id={fieldId}
            ref={ref}
            type={show ? 'text' : 'password'}
            className="ui-input"
            aria-invalid={error ? 'true' : undefined}
            aria-describedby={describedBy}
            {...rest}
          />
          <button
            type="button"
            className="ui-input-group__btn"
            onClick={() => setShow((s) => !s)}
            aria-label={show ? 'Hide password' : 'Show password'}
            aria-pressed={show}
          >
            {show ? <EyeOff size={16} aria-hidden="true" /> : <Eye size={16} aria-hidden="true" />}
          </button>
        </div>
      )}
    </FieldShell>
  )
})

export const TextareaField = forwardRef(function TextareaField(
  { label, hint, error, required, id, rows = 4, ...rest },
  ref
) {
  const generatedId = useId()
  const fieldId = id || generatedId
  return (
    <FieldShell id={fieldId} label={label} hint={hint} error={error} required={required}>
      {({ describedBy }) => (
        <textarea
          id={fieldId}
          ref={ref}
          rows={rows}
          className="ui-input ui-textarea"
          aria-invalid={error ? 'true' : undefined}
          aria-describedby={describedBy}
          {...rest}
        />
      )}
    </FieldShell>
  )
})

export const SelectField = forwardRef(function SelectField(
  { label, hint, error, required, id, options = [], children, ...rest },
  ref
) {
  const generatedId = useId()
  const fieldId = id || generatedId
  return (
    <FieldShell id={fieldId} label={label} hint={hint} error={error} required={required}>
      {({ describedBy }) => (
        <select
          id={fieldId}
          ref={ref}
          className="ui-input ui-select"
          aria-invalid={error ? 'true' : undefined}
          aria-describedby={describedBy}
          {...rest}
        >
          {children ||
            options.map((o) => (
              <option key={o.value} value={o.value}>
                {o.label}
              </option>
            ))}
        </select>
      )}
    </FieldShell>
  )
})

export const DateField = forwardRef(function DateField(props, ref) {
  return <TextField ref={ref} type="date" {...props} />
})

export const CheckboxField = forwardRef(function CheckboxField(
  { label, error, id, ...rest },
  ref
) {
  const generatedId = useId()
  const fieldId = id || generatedId
  const errorId = error ? `${fieldId}-error` : undefined
  return (
    <div className={`ui-checkbox${error ? ' ui-field--invalid' : ''}`}>
      <input
        id={fieldId}
        ref={ref}
        type="checkbox"
        aria-invalid={error ? 'true' : undefined}
        aria-describedby={errorId}
        {...rest}
      />
      <label htmlFor={fieldId}>{label}</label>
      {error && (
        <p id={errorId} className="ui-field__error" role="alert">
          {error}
        </p>
      )}
    </div>
  )
})

export function RadioGroup({ legend, name, options = [], value, onChange, error }) {
  const groupId = useId()
  const errorId = error ? `${groupId}-error` : undefined
  return (
    <fieldset className="ui-radio-group" aria-describedby={errorId} aria-invalid={error ? 'true' : undefined}>
      <legend className="ui-field__label">{legend}</legend>
      {options.map((o) => (
        <label key={o.value} className="ui-radio">
          <input
            type="radio"
            name={name}
            value={o.value}
            checked={value === o.value}
            onChange={() => onChange?.(o.value)}
          />
          <span>{o.label}</span>
        </label>
      ))}
      {error && (
        <p id={errorId} className="ui-field__error" role="alert">
          {error}
        </p>
      )}
    </fieldset>
  )
}

export const FileField = forwardRef(function FileField(
  { label, hint, error, required, id, accept, ...rest },
  ref
) {
  const generatedId = useId()
  const fieldId = id || generatedId
  return (
    <FieldShell id={fieldId} label={label} hint={hint} error={error} required={required}>
      {({ describedBy }) => (
        <input
          id={fieldId}
          ref={ref}
          type="file"
          accept={accept}
          className="ui-input ui-file"
          aria-invalid={error ? 'true' : undefined}
          aria-describedby={describedBy}
          {...rest}
        />
      )}
    </FieldShell>
  )
})
