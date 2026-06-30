import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Plus } from 'lucide-react'
import { PageHeader } from '../ui/PageHeader'
import { Button } from '../ui/Button'
import { Toolbar } from '../ui/Toolbar'
import { FormModal } from '../ui/FormModal'
import { ResourceTable } from './ResourceTable'

// Faithful port of the prototype `crud()` builder: a page header with a create
// action, an optional search/filter toolbar, and a typed responsive table.
// All data + handlers come from the caller (queries/mutations stay in the page);
// this only owns the create-modal open state.
//
//   <Crud title description rows loading error columns
//         createLabel createTitle createFields onCreate creating createError
//         rowActions search filters onFilter />
export function Crud({
  title,
  description,
  actions,
  rows,
  loading,
  error,
  onRetry,
  columns,
  rowKey,
  rowActions,
  emptyTitle,
  emptyMessage,
  locale,
  // create
  createLabel,
  createTitle,
  createFields,
  createInitial,
  onCreate,
  creating,
  createError,
  // toolbar
  search,
  filters,
  onFilter,
  banner,
}) {
  const { t } = useTranslation()
  const [open, setOpen] = useState(false)

  const submit = async (values) => {
    try {
      await onCreate(values)
      setOpen(false)
    } catch {
      // Error is surfaced through `createError`; keep the modal open.
    }
  }

  const allColumns = rowActions
    ? [...columns, { key: '__actions', header: '', align: 'end', render: rowActions }]
    : columns

  return (
    <div>
      <PageHeader
        title={title}
        description={description}
        actions={
          <div className="cluster">
            {actions}
            {createFields && onCreate && (
              <Button onClick={() => setOpen(true)}>
                <Plus size={16} aria-hidden="true" /> {createLabel || t('actions.add', 'Add')}
              </Button>
            )}
          </div>
        }
      />

      {banner}

      {(search || (filters && filters.length > 0)) && (
        <Toolbar search={search} filters={filters} onFilter={onFilter} />
      )}

      <ResourceTable
        rows={rows}
        columns={allColumns}
        loading={loading}
        error={error}
        onRetry={onRetry}
        rowKey={rowKey}
        emptyTitle={emptyTitle}
        emptyMessage={emptyMessage}
        locale={locale}
      />

      {createFields && onCreate && (
        <FormModal
          open={open}
          onClose={() => setOpen(false)}
          title={createTitle || createLabel}
          fields={createFields}
          initialValues={createInitial}
          onSubmit={submit}
          submitting={creating}
          error={createError}
        />
      )}
    </div>
  )
}
