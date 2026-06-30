import { useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { Modal } from './Modal'
import { Button } from './Button'
import { Alert } from './Alert'
import { TextField, TextareaField, SelectField, DateField, CheckboxField } from '../form/fields'

// Field config: { name, label, type, options?, required?, hint?, full?, rows?, placeholder? }
// type ∈ text|email|tel|number|password|date|textarea|select|checkbox (default text)
function blankValues(fields, initialValues = {}) {
  const out = { ...initialValues }
  for (const f of fields) {
    if (out[f.name] === undefined) out[f.name] = f.type === 'checkbox' ? false : ''
  }
  return out
}

// Modal form matching the prototype `formModal()` builder: auto-lays fields in a
// 1- or 2-column grid (2-col when more than 4 non-full fields), with a sticky
// Cancel / Save footer. Manages its own field state, seeded from initialValues
// and reset each time it opens. Submits the collected values to `onSubmit`.
export function FormModal({
  open,
  onClose,
  title,
  fields = [],
  initialValues,
  onSubmit,
  submitting = false,
  error,
  submitLabel,
}) {
  const { t } = useTranslation()
  const [values, setValues] = useState(() => blankValues(fields, initialValues))

  useEffect(() => {
    if (open) setValues(blankValues(fields, initialValues))
    // Reset only when the dialog transitions open; field/initial identity is stable per open.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open])

  const set = (name, value) => setValues((v) => ({ ...v, [name]: value }))
  const submit = (e) => {
    e.preventDefault()
    onSubmit?.(values)
  }

  const twoCol = fields.filter((f) => !f.full).length > 4

  const renderField = (f) => {
    const common = {
      label: f.label,
      required: f.required,
      hint: f.hint,
      placeholder: f.placeholder,
      value: values[f.name] ?? '',
      onChange: (e) => set(f.name, e.target.value),
    }
    switch (f.type) {
      case 'textarea':
        return <TextareaField {...common} rows={f.rows || 3} />
      case 'select':
        return <SelectField {...common} options={f.options} />
      case 'date':
        return <DateField {...common} />
      case 'checkbox':
        return (
          <CheckboxField
            label={f.label}
            checked={!!values[f.name]}
            onChange={(e) => set(f.name, e.target.checked)}
          />
        )
      default:
        return <TextField {...common} type={f.type || 'text'} />
    }
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title={title}
      footer={
        <>
          <Button variant="ghost" type="button" onClick={onClose}>
            {t('actions.cancel', 'Cancel')}
          </Button>
          <Button type="submit" form="ui-formmodal" loading={submitting}>
            {submitLabel || t('actions.save', 'Save')}
          </Button>
        </>
      }
    >
      <form id="ui-formmodal" onSubmit={submit} className="ui-formmodal">
        {error && (
          <Alert variant="error" correlationId={error.correlationId}>
            {error.detail || error.title || t('common.error', 'Something went wrong')}
          </Alert>
        )}
        <div className={`ui-formgrid${twoCol ? ' ui-formgrid--2' : ''}`}>
          {fields.map((f) => (
            <div key={f.name} className={f.full || !twoCol ? 'ui-formgrid__full' : undefined}>
              {renderField(f)}
            </div>
          ))}
        </div>
      </form>
    </Modal>
  )
}
