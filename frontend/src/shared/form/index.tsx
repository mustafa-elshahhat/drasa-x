import type { ReactNode } from 'react'

// =============================================================================
// shared/form — typed home for the DerasaX form controls.
//
// The existing controls in src/components/form/fields.jsx are `forwardRef`
// components that compose with react-hook-form's `register()`. We re-export them
// directly (preserving the ref-forwarding behavior) and add a typed generic
// `FormField` wrapper for ad-hoc labelled fields.
// =============================================================================
export {
  TextField,
  PasswordField,
  TextareaField,
  SelectField,
  DateField,
  CheckboxField,
  RadioGroup,
  FileField,
} from '../../components/form/fields'

export interface FormFieldProps {
  label?: ReactNode
  hint?: ReactNode
  error?: ReactNode
  required?: boolean
  htmlFor?: string
  children: ReactNode
}

/** Labelled field shell (label + hint + role="alert" error) for custom inputs. */
export function FormField({ label, hint, error, required, htmlFor, children }: FormFieldProps) {
  return (
    <div className={`ui-field${error ? ' ui-field--invalid' : ''}`}>
      {label && (
        <label htmlFor={htmlFor} className="ui-field__label">
          {label}
          {required && (
            <span aria-hidden="true" className="ui-field__req">
              {' '}
              *
            </span>
          )}
        </label>
      )}
      {children}
      {hint && <p className="ui-field__hint">{hint}</p>}
      {error && (
        <p className="ui-field__error" role="alert">
          {error}
        </p>
      )}
    </div>
  )
}
