import { useTranslation } from 'react-i18next'
import { Spinner } from '../../components/ui/Spinner'
import { PageHeader } from '../../components/ui/PageHeader'
import { ResourceTable } from '../../components/data/ResourceTable'

// Shared school-admin presentational helpers (components only).

export function Loading() {
  const { t } = useTranslation()
  return <Spinner label={t('states.loading')} />
}

/** Page header sourced from the `school.pages.<view>.*` i18n keys. */
export function Head({ view }) {
  const { t } = useTranslation()
  return <PageHeader title={t(`school.pages.${view}.title`)} description={t(`school.pages.${view}.description`)} />
}

/** Resource table bound to a query's loading/error/empty/data states. */
export function List({ query, columns, empty, rowActions, locale }) {
  return (
    <ResourceTable
      rows={Array.isArray(query.data) ? query.data : []}
      columns={columns}
      rowActions={rowActions}
      loading={query.isLoading}
      error={query.error}
      onRetry={() => query.refetch()}
      emptyTitle={empty}
      locale={locale}
    />
  )
}
